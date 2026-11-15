using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Sleepwalking.Tests.PlayMode
{
    public sealed class RoomBedAwakeAnimationTests
    {
        [UnityTest]
        public IEnumerator BedBreathesBeforeFirstDreamThenLooksBackAndForth()
        {
            AsyncOperation loading = SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
            while (!loading.isDone) yield return null;
            yield return null;

            GameObject room = GameObject.Find("Waking room");
            Assert.That(room, Is.Not.Null);
            Transform bed = room.transform.Find("Bed");
            Assert.That(bed, Is.Not.Null);

            MonoBehaviour animation = null;
            foreach (MonoBehaviour component in bed.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().FullName ==
                    "Sleepwalking.Visual.RoomBedAwakeAnimation") animation = component;
            Assert.That(animation, Is.Not.Null, "The actual menu prefab must contain the waking animation.");

            Type type = animation.GetType();
            Texture2D[] frames = (Texture2D[])type.GetField("Frames").GetValue(animation);
            MeshRenderer artwork = (MeshRenderer)type.GetField("Artwork").GetValue(animation);
            Texture2D[] sleeping = (Texture2D[])type.GetField("SleepingFrames").GetValue(animation);
            float sleepingSeconds = (float)type.GetField("SleepingFrameSeconds").GetValue(animation);
            float awakeSeconds = (float)type.GetField("AwakeFrameSeconds").GetValue(animation);
            MethodInfo restart = type.GetMethod("RestartAnimation");
            MethodInfo advance = type.GetMethod("Advance");
            PropertyInfo current = type.GetProperty("CurrentFrameIndex");
            Assert.That(frames.Length, Is.EqualTo(8));
            Assert.That(sleeping.Length, Is.EqualTo(8));
            Assert.That(artwork, Is.Not.Null);
            Assert.That(restart, Is.Not.Null);
            Assert.That(advance, Is.Not.Null);
            Assert.That(current, Is.Not.Null);

            animation.enabled = false;
            restart.Invoke(animation, null);
            Assert.That((bool)type.GetProperty("IsSleeping").GetValue(animation), Is.True);
            AssertFrame(artwork, sleeping, current, animation, 0);
            CaptureRoom("MenuBedSleeping.png");

            for (int q = 1; q < 8; q++)
            {
                advance.Invoke(animation, new object[] { sleepingSeconds });
                AssertFrame(artwork, sleeping, current, animation, q);
            }
            advance.Invoke(animation, new object[] { sleepingSeconds });
            AssertFrame(artwork, sleeping, current, animation, 0);

            GameObject game = GameObject.Find("Sleepwalking Game");
            Assert.That(game, Is.Not.Null);
            MonoBehaviour controller = null;
            foreach (MonoBehaviour component in game.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().Name == "SleepwalkingGame") controller = component;
            Assert.That(controller, Is.Not.Null);
            controller.GetType().GetMethod("StartDream").Invoke(controller, null);
            controller.GetType().GetMethod("ReturnToRoom").Invoke(controller, null);
            restart.Invoke(animation, null);
            Assert.That((bool)type.GetProperty("IsSleeping").GetValue(animation), Is.False);
            AssertFrame(artwork, frames, current, animation, 0);
            CaptureRoom("MenuBedAwakeStart.png");

            foreach (int frame in new[] { 1, 2, 3, 4, 5, 6, 5, 4, 3, 2, 1, 0, 1 })
            {
                advance.Invoke(animation, new object[] { awakeSeconds });
                AssertFrame(artwork, frames, current, animation, frame);
            }
            CaptureRoom("MenuBedAwakeLoop.png");

            MonoBehaviour hotspot = null;
            foreach (MonoBehaviour component in bed.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().FullName ==
                    "Sleepwalking.Visual.RoomHotspot") hotspot = component;
            Assert.That(hotspot, Is.Not.Null);
            MethodInfo highlight = hotspot.GetType().GetMethod("SetHighlighted");
            highlight.Invoke(hotspot, new object[] { true });
            advance.Invoke(animation, new object[] { awakeSeconds });
            var highlighted = new MaterialPropertyBlock();
            artwork.GetPropertyBlock(highlighted);
            Assert.That(highlighted.GetFloat("_Highlight"), Is.EqualTo(.14f).Within(.005f));
            highlight.Invoke(hotspot, new object[] { false });

            // Смена Odradek не должна оставлять на кровати статичный кадр вместо анимации.
            MonoBehaviour environment = null;
            foreach (MonoBehaviour component in room.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().FullName ==
                    "Sleepwalking.Visual.RoomEnvironment") environment = component;
            Assert.That(environment, Is.Not.Null);
            environment.GetType().GetMethod("SetOdradek").Invoke(environment, new object[] { 100 });
            advance.Invoke(animation, new object[] { 0f });
            AssertFrame(artwork, frames, current, animation, 2);

            animation.enabled = true;
            advance.Invoke(animation, new object[] { awakeSeconds * 4f });
            room.SetActive(false);
            room.SetActive(true);
            AssertFrame(artwork, frames, current, animation, 0);
        }

        static void AssertFrame(MeshRenderer artwork, Texture2D[] frames, PropertyInfo current,
            MonoBehaviour animation, int expected)
        {
            Assert.That((int)current.GetValue(animation), Is.EqualTo(expected));
            var properties = new MaterialPropertyBlock();
            artwork.GetPropertyBlock(properties);
            Assert.That(properties.GetTexture("_MainTex"), Is.EqualTo(frames[expected]));
        }

        static void CaptureRoom(string file)
        {
            Camera camera = Camera.main;
            Assert.That(camera, Is.Not.Null);
            RenderTexture target = new RenderTexture(1280, 720, 24);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            Texture2D image = new Texture2D(1280, 720, TextureFormat.RGBA32, false);
            try
            {
                Assert.That(target.Create(), Is.True);
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                image.Apply();
                string folder = Path.Combine(Directory.GetCurrentDirectory(), "Docs/QA/VisualLogs");
                Directory.CreateDirectory(folder);
                File.WriteAllBytes(Path.Combine(folder, file), image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.Destroy(image);
                target.Release();
                UnityEngine.Object.Destroy(target);
            }
        }
    }
}
