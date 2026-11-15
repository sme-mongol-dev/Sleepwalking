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
        public IEnumerator WakingIntroLeadsIntoOpenEyeLoopAndRestartsOnReturn()
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
            float introSeconds = (float)type.GetField("IntroFrameSeconds").GetValue(animation);
            float loopSeconds = (float)type.GetField("LoopFrameSeconds").GetValue(animation);
            MethodInfo restart = type.GetMethod("RestartAnimation");
            MethodInfo advance = type.GetMethod("Advance");
            PropertyInfo current = type.GetProperty("CurrentFrameIndex");
            Assert.That(frames.Length, Is.EqualTo(8));
            Assert.That(artwork, Is.Not.Null);
            Assert.That(restart, Is.Not.Null);
            Assert.That(advance, Is.Not.Null);
            Assert.That(current, Is.Not.Null);

            animation.enabled = false;
            restart.Invoke(animation, null);
            AssertFrame(artwork, frames, current, animation, 7);
            CaptureRoom("MenuBedAwakeStart.png");

            for (int q = 6; q >= 0; q--)
            {
                advance.Invoke(animation, new object[] { introSeconds });
                AssertFrame(artwork, frames, current, animation, q);
            }
            advance.Invoke(animation, new object[] { introSeconds });
            AssertFrame(artwork, frames, current, animation, 2);
            CaptureRoom("MenuBedAwakeLoop.png");

            foreach (int frame in new[] { 3, 4, 5, 6, 2, 3 })
            {
                advance.Invoke(animation, new object[] { loopSeconds });
                AssertFrame(artwork, frames, current, animation, frame);
            }

            MonoBehaviour hotspot = null;
            foreach (MonoBehaviour component in bed.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().FullName ==
                    "Sleepwalking.Visual.RoomHotspot") hotspot = component;
            Assert.That(hotspot, Is.Not.Null);
            MethodInfo highlight = hotspot.GetType().GetMethod("SetHighlighted");
            highlight.Invoke(hotspot, new object[] { true });
            advance.Invoke(animation, new object[] { loopSeconds });
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
            AssertFrame(artwork, frames, current, animation, 4);

            animation.enabled = true;
            advance.Invoke(animation, new object[] { introSeconds * 8f });
            room.SetActive(false);
            room.SetActive(true);
            AssertFrame(artwork, frames, current, animation, 7);
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
