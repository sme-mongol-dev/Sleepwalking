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
    public sealed class SleepwalkingDreamBedTests
    {
        [UnityTest]
        public IEnumerator CaptureL02MeatSpikeSeating()
        {
            AsyncOperation loading = SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
            Assert.That(loading, Is.Not.Null);
            while (!loading.isDone) yield return null;
            yield return null;

            GameObject gameObject = GameObject.Find("Sleepwalking Game");
            Assert.That(gameObject, Is.Not.Null);
            MonoBehaviour game = null;
            foreach (MonoBehaviour component in gameObject.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().FullName == "Sleepwalking.SleepwalkingGame")
                    game = component;
            Assert.That(game, Is.Not.Null);

            Type type = game.GetType();
            type.GetField("completedDreams", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(game, 1);
            type.GetMethod("StartDream", BindingFlags.Instance | BindingFlags.Public).Invoke(game, null);
            PropertyInfo inDream = type.GetProperty("InDream", BindingFlags.Instance | BindingFlags.Public);
            FieldInfo transitioning = type.GetField("transitioning", BindingFlags.Instance | BindingFlags.NonPublic);
            float deadline = Time.realtimeSinceStartup + 10f;
            while ((!((bool)inDream.GetValue(game)) || (bool)transitioning.GetValue(game)) &&
                Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That((bool)inDream.GetValue(game), Is.True);

            GameObject level = GameObject.Find("Dream L02");
            Assert.That(level, Is.Not.Null);
            Transform previousTheme = level.transform.Find("Dream style");
            if (previousTheme != null)
            {
                UnityEngine.Object.Destroy(previousTheme.gameObject);
                yield return null;
            }
            type.GetField("style", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(game, 0);
            type.GetMethod("ApplyStyle", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(game, null);
            yield return null;

            foreach (string name in new[] { "L02_PitSpikes_1", "L02_FloorSpikes_1" })
            {
                GameObject spike = GameObject.Find(name);
                Assert.That(spike, Is.Not.Null);
                SpriteRenderer sprite = spike.transform.Find("Dream Hazard Art")?.GetComponent<SpriteRenderer>();
                Assert.That(sprite, Is.Not.Null);
                Assert.That(sprite.sprite.name, Is.EqualTo("Meat_Spikes"));
                Assert.That(sprite.color, Is.EqualTo(Color.white));
                Assert.That(spike.GetComponent<MeshRenderer>().forceRenderingOff, Is.True);
            }

            Camera camera = (Camera)type.GetField("GameCamera", BindingFlags.Instance | BindingFlags.Public).GetValue(game);
            Assert.That(camera, Is.Not.Null);
            camera.orthographicSize = 2f * 32f;
            camera.transform.position = new Vector3(5.5f * 32f, -.1f * 32f, -20f);
            Capture(camera, "dream-spikes-L02-meat-closeup");
        }

        [UnityTest]
        public IEnumerator CaptureSpikeSeatingMatrix()
        {
            int[] levels = { 2, 5, 7 };
            float[] cameraX = { 5.5f, 8f, 15f };
            string[] names = { "Meat", "PostApoc", "DarkWater", "Heaven" };
            for (int levelIndex = 0; levelIndex < levels.Length; levelIndex++)
            {
                AsyncOperation loading = SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
                Assert.That(loading, Is.Not.Null);
                while (!loading.isDone) yield return null;
                yield return null;

                GameObject gameObject = GameObject.Find("Sleepwalking Game");
                Assert.That(gameObject, Is.Not.Null);
                MonoBehaviour game = null;
                foreach (MonoBehaviour component in gameObject.GetComponents<MonoBehaviour>())
                    if (component != null && component.GetType().FullName == "Sleepwalking.SleepwalkingGame")
                        game = component;
                Assert.That(game, Is.Not.Null);
                Type type = game.GetType();
                type.GetField("completedDreams", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(game, levels[levelIndex] - 1);
                type.GetMethod("StartDream", BindingFlags.Instance | BindingFlags.Public).Invoke(game, null);
                PropertyInfo inDream = type.GetProperty("InDream", BindingFlags.Instance | BindingFlags.Public);
                FieldInfo transitioning = type.GetField("transitioning", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo style = type.GetField("style", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo applyStyle = type.GetMethod("ApplyStyle", BindingFlags.Instance | BindingFlags.NonPublic);
                Camera camera = (Camera)type.GetField("GameCamera", BindingFlags.Instance | BindingFlags.Public).GetValue(game);
                float deadline = Time.realtimeSinceStartup + 10f;
                while ((!((bool)inDream.GetValue(game)) || (bool)transitioning.GetValue(game)) &&
                    Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That((bool)inDream.GetValue(game), Is.True);

                GameObject level = GameObject.Find("Dream L" + levels[levelIndex].ToString("00"));
                Assert.That(level, Is.Not.Null);
                for (int q = 0; q < names.Length; q++)
                {
                    Transform previousTheme = level.transform.Find("Dream style");
                    if (previousTheme != null)
                    {
                        UnityEngine.Object.Destroy(previousTheme.gameObject);
                        yield return null;
                    }
                    style.SetValue(game, q);
                    applyStyle.Invoke(game, null);
                    yield return null;

                    string spikeName = "L" + levels[levelIndex].ToString("00") + "_PitSpikes_1";
                    GameObject spike = GameObject.Find(spikeName);
                    Assert.That(spike, Is.Not.Null);
                    SpriteRenderer sprite = spike.transform.Find("Dream Hazard Art")?.GetComponent<SpriteRenderer>();
                    Assert.That(sprite, Is.Not.Null);
                    Assert.That(sprite.color, Is.EqualTo(Color.white), spikeName + " " + names[q]);
                    Assert.That(spike.GetComponent<MeshRenderer>().forceRenderingOff, Is.True);

                    camera.orthographicSize = 2f * 32f;
                    camera.transform.position = new Vector3(cameraX[levelIndex] * 32f, -.8f * 32f, -20f);
                    Capture(camera, "dream-spikes-L" + levels[levelIndex].ToString("00") + "-" +
                        names[q].ToLowerInvariant() + "-close", 1280, 720);
                }
            }
        }

        [UnityTest]
        public IEnumerator BedExitUsesCurrentDreamStyleWithoutChangingTrigger()
        {
            AsyncOperation loading = SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
            Assert.That(loading, Is.Not.Null);
            while (!loading.isDone) yield return null;
            yield return null;

            GameObject gameObject = GameObject.Find("Sleepwalking Game");
            Assert.That(gameObject, Is.Not.Null);
            MonoBehaviour game = null;
            foreach (MonoBehaviour component in gameObject.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().FullName == "Sleepwalking.SleepwalkingGame")
                    game = component;
            Assert.That(game, Is.Not.Null);

            Type type = game.GetType();
            type.GetField("completedDreams", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(game, 6);
            type.GetMethod("StartDream", BindingFlags.Instance | BindingFlags.Public).Invoke(game, null);
            PropertyInfo inDream = type.GetProperty("InDream", BindingFlags.Instance | BindingFlags.Public);
            FieldInfo transitioning = type.GetField("transitioning", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo style = type.GetField("style", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo applyStyle = type.GetMethod("ApplyStyle", BindingFlags.Instance | BindingFlags.NonPublic);
            Camera camera = (Camera)type.GetField("GameCamera", BindingFlags.Instance | BindingFlags.Public).GetValue(game);
            float deadline = Time.realtimeSinceStartup + 10f;
            while ((!((bool)inDream.GetValue(game)) || (bool)transitioning.GetValue(game)) &&
                Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That((bool)inDream.GetValue(game), Is.True);

            GameObject level = GameObject.Find("Dream L07");
            GameObject bed = GameObject.Find("BedExit");
            Assert.That(level, Is.Not.Null);
            Assert.That(bed, Is.Not.Null);
            Assert.That(camera, Is.Not.Null);
            Collider2D trigger = bed.GetComponent<Collider2D>();
            Assert.That(trigger, Is.Not.Null);
            Assert.That(trigger.isTrigger, Is.True);
            Bounds originalBounds = trigger.bounds;
            float originalSize = camera.orthographicSize;
            string[] names = { "Meat", "PostApoc", "DarkWater", "Heaven" };

            for (int q = 0; q < names.Length; q++)
            {
                Transform previousTheme = level.transform.Find("Dream style");
                if (previousTheme != null)
                {
                    UnityEngine.Object.Destroy(previousTheme.gameObject);
                    yield return null;
                }
                style.SetValue(game, q);
                applyStyle.Invoke(game, null);
                yield return null;

                Transform art = bed.transform.Find("Dream Bed Art");
                Assert.That(art, Is.Not.Null, names[q]);
                SpriteRenderer sprite = art.GetComponent<SpriteRenderer>();
                Assert.That(sprite, Is.Not.Null);
                Assert.That(sprite.enabled, Is.True);
                Assert.That(sprite.sprite.name, Is.EqualTo("T_" + names[q] + "Bed"));
                Assert.That(sprite.color, Is.EqualTo(Color.white));
                Assert.That(sprite.bounds.size.x, Is.EqualTo(64f).Within(.01f));
                Assert.That(sprite.bounds.size.y, Is.EqualTo(32f).Within(.01f));
                Assert.That(Vector3.Distance(trigger.bounds.center, originalBounds.center), Is.LessThan(.001f));
                Assert.That(Vector3.Distance(trigger.bounds.size, originalBounds.size), Is.LessThan(.001f));
                Assert.That(trigger.isTrigger, Is.True);

                foreach (Renderer oldRenderer in bed.GetComponentsInChildren<Renderer>(true))
                    if (oldRenderer != sprite) Assert.That(oldRenderer.enabled, Is.False, oldRenderer.name);

                camera.orthographicSize = originalSize;
                camera.transform.position = new Vector3(20.5f * 32f, 4f * 32f, -20f);
                Capture(camera, "dream-bed-L07-" + names[q].ToLowerInvariant());
                camera.orthographicSize = 1.8f * 32f;
                camera.transform.position = new Vector3(21.5f * 32f, 3.6f * 32f, -20f);
                Capture(camera, "dream-bed-close-L07-" + names[q].ToLowerInvariant(), 1280, 720);
            }
        }

        static void Capture(Camera camera, string name, int width = 320, int height = 180)
        {
            RenderTexture originalTarget = camera.targetTexture;
            RenderTexture target = originalTarget;
            bool temporaryTarget = target == null || target.width != width || target.height != height;
            if (temporaryTarget)
            {
                target = new RenderTexture(width, height, 24);
                target.Create();
                camera.targetTexture = target;
            }
            RenderTexture previous = RenderTexture.active;
            Texture2D image = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            try
            {
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                image.Apply();
                string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string path = Path.Combine(projectRoot, "Docs", "QA", name + ".png");
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                UnityEngine.Object.Destroy(image);
                if (temporaryTarget)
                {
                    camera.targetTexture = originalTarget;
                    target.Release();
                    UnityEngine.Object.Destroy(target);
                }
            }
        }
    }
}
