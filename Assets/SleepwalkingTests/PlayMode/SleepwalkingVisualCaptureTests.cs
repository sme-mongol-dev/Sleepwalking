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
    public sealed class SleepwalkingVisualCaptureTests
    {
        [UnityTest]
        public IEnumerator CaptureFinalL01AndL07Far()
        {
            AsyncOperation loading = SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
            Assert.That(loading, Is.Not.Null);
            while (!loading.isDone)
                yield return null;
            yield return null;

            GameObject gameObject = GameObject.Find("Sleepwalking Game");
            Assert.That(gameObject, Is.Not.Null);
            MonoBehaviour game = null;
            foreach (MonoBehaviour component in gameObject.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().FullName == "Sleepwalking.SleepwalkingGame")
                    game = component;
            Assert.That(game, Is.Not.Null);

            Type type = game.GetType();
            MethodInfo startDream = type.GetMethod("StartDream", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo completeDream = type.GetMethod("CompleteDream", BindingFlags.Instance | BindingFlags.Public);
            PropertyInfo inDream = type.GetProperty("InDream", BindingFlags.Instance | BindingFlags.Public);
            FieldInfo transitioning = type.GetField("transitioning", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo style = type.GetField("style", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo cameraField = type.GetField("GameCamera", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(startDream, Is.Not.Null);
            Assert.That(completeDream, Is.Not.Null);
            Assert.That(inDream, Is.Not.Null);
            Assert.That(transitioning, Is.Not.Null);
            Assert.That(style, Is.Not.Null);
            Assert.That(cameraField, Is.Not.Null);
            Camera gameCamera = (Camera)cameraField.GetValue(game);
            Assert.That(gameCamera, Is.Not.Null);

            for (int q = 1; q <= 7; q++)
            {
                startDream.Invoke(game, null);
                float deadline = Time.realtimeSinceStartup + 10f;
                while ((!((bool)inDream.GetValue(game)) || (bool)transitioning.GetValue(game)) && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.That((bool)inDream.GetValue(game), Is.True, "Dream " + q + " did not start.");
                Assert.That((bool)transitioning.GetValue(game), Is.False);
                Assert.That(GameObject.Find("Dream L" + q.ToString("00")), Is.Not.Null);

                if (q == 1 || q == 7)
                {
                    yield return new WaitForSecondsRealtime(1.2f);
                    yield return null;
                    yield return null;
                    if (q == 7)
                    {
                        // Фиксированный дальний ракурс проверяет покрытие фона за концом маршрута.
                        gameCamera.transform.position = new Vector3(20.5f, 1.8f, -20f);
                        gameCamera.Render();
                    }
                    string name = q == 1 ? "dream-L01-final-main" : "dream-L07-far-final-main";
                    CaptureFinalFrame(gameCamera, (int)style.GetValue(game), name, q == 7);
                }

                if (q == 7) break;
                completeDream.Invoke(game, null);
                deadline = Time.realtimeSinceStartup + 10f;
                while (((bool)inDream.GetValue(game) || (bool)transitioning.GetValue(game)) && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.That((bool)inDream.GetValue(game), Is.False);
                Assert.That((bool)transitioning.GetValue(game), Is.False);
            }
            LogAssert.NoUnexpectedReceived();
        }

        static void CaptureFinalFrame(Camera gameCamera, int style, string name, bool forcedCamera)
        {
            RenderTexture target = gameCamera.targetTexture;
            Assert.That(target, Is.Not.Null);
            Assert.That(target.IsCreated(), Is.True);
            GameObject dreamer = GameObject.Find("Dreamer");
            Assert.That(dreamer, Is.Not.Null);
            Vector3 playerViewport = gameCamera.WorldToViewportPoint(dreamer.transform.position);
            RenderTexture previous = RenderTexture.active;
            Texture2D image = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                image.Apply();
                string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string path = Path.Combine(projectRoot, "Docs", "QA", name + ".png");
                File.WriteAllBytes(path, image.EncodeToPNG());
                File.WriteAllText(Path.ChangeExtension(path, ".txt"),
                    "Camera=" + gameCamera.transform.position + " Size=" + gameCamera.orthographicSize +
                    " Player=" + dreamer.transform.position + " PlayerViewport=" + playerViewport +
                    " Target=" + target.width + "x" + target.height + " Style=" + style +
                    " ForcedCamera=" + forcedCamera + " TimeScale=" + Time.timeScale);
            }
            finally
            {
                RenderTexture.active = previous;
                UnityEngine.Object.Destroy(image);
            }
        }

        [UnityTest]
        public IEnumerator CaptureFirstCycleAssignedStyles()
        {
            AsyncOperation loading = SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
            Assert.That(loading, Is.Not.Null);
            while (!loading.isDone)
                yield return null;
            yield return null;

            GameObject gameObject = GameObject.Find("Sleepwalking Game");
            Assert.That(gameObject, Is.Not.Null);
            MonoBehaviour game = null;
            foreach (MonoBehaviour component in gameObject.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().FullName == "Sleepwalking.SleepwalkingGame")
                    game = component;
            Assert.That(game, Is.Not.Null);

            Type type = game.GetType();
            MethodInfo startDream = type.GetMethod("StartDream", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo completeDream = type.GetMethod("CompleteDream", BindingFlags.Instance | BindingFlags.Public);
            PropertyInfo inDream = type.GetProperty("InDream", BindingFlags.Instance | BindingFlags.Public);
            FieldInfo transitioning = type.GetField("transitioning", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo style = type.GetField("style", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo cameraField = type.GetField("GameCamera", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(startDream, Is.Not.Null);
            Assert.That(completeDream, Is.Not.Null);
            Assert.That(inDream, Is.Not.Null);
            Assert.That(transitioning, Is.Not.Null);
            Assert.That(style, Is.Not.Null);
            Assert.That(cameraField, Is.Not.Null);

            Camera gameCamera = (Camera)cameraField.GetValue(game);
            Assert.That(gameCamera, Is.Not.Null);
            string[] names = { "Meat", "PostApocalypse", "DeepWater", "Heaven" };
            bool[] captured = new bool[names.Length];
            int captureCount = 0;

            // Сохраняем только реально назначенные стили первого цикла, не подменяя конфигурацию уровня.
            for (int q = 0; q < 7; q++)
            {
                startDream.Invoke(game, null);
                float deadline = Time.realtimeSinceStartup + 10f;
                while ((!((bool)inDream.GetValue(game)) || (bool)transitioning.GetValue(game)) && Time.realtimeSinceStartup < deadline)
                    yield return null;

                Assert.That((bool)inDream.GetValue(game), Is.True, "Dream " + (q + 1) + " did not start.");
                Assert.That((bool)transitioning.GetValue(game), Is.False, "Dream " + (q + 1) + " transition did not finish.");
                Assert.That(GameObject.Find("Dream L" + (q + 1).ToString("00")), Is.Not.Null);

                int selectedStyle = (int)style.GetValue(game);
                Assert.That(selectedStyle, Is.InRange(0, names.Length - 1));
                if (!captured[selectedStyle])
                {
                    // Даём камере и физике стабилизироваться после окончания fade.
                    yield return new WaitForSecondsRealtime(1.2f);
                    yield return null;
                    yield return null;
                    RenderTexture target = gameCamera.targetTexture;
                    Assert.That(target, Is.Not.Null, "PixelRoomView must assign a RenderTexture to the game camera.");
                    Assert.That(target.IsCreated(), Is.True);
                    GameObject dreamer = GameObject.Find("Dreamer");
                    Assert.That(dreamer, Is.Not.Null);
                    Vector3 playerViewport = gameCamera.WorldToViewportPoint(dreamer.transform.position);

                    RenderTexture previous = RenderTexture.active;
                    Texture2D image = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
                    try
                    {
                        RenderTexture.active = target;
                        image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                        image.Apply();
                        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                        string tag = Environment.GetEnvironmentVariable("SLEEPWALKING_QA_CAPTURE_TAG") ?? "capture";
                        string path = Path.Combine(projectRoot, "Docs", "QA",
                            "dream-L" + (q + 1).ToString("00") + "-" + names[selectedStyle] + "-" + tag + ".png");
                        File.WriteAllBytes(path, image.EncodeToPNG());
                        File.WriteAllText(Path.ChangeExtension(path, ".txt"),
                            "Camera=" + gameCamera.transform.position + " Size=" + gameCamera.orthographicSize +
                            " Player=" + dreamer.transform.position + " PlayerViewport=" + playerViewport +
                            " Target=" + target.width + "x" + target.height + " TimeScale=" + Time.timeScale);
                    }
                    finally
                    {
                        RenderTexture.active = previous;
                        UnityEngine.Object.Destroy(image);
                    }
                    captured[selectedStyle] = true;
                    captureCount++;
                }

                completeDream.Invoke(game, null);
                deadline = Time.realtimeSinceStartup + 10f;
                while (((bool)inDream.GetValue(game) || (bool)transitioning.GetValue(game)) && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.That((bool)inDream.GetValue(game), Is.False);
                Assert.That((bool)transitioning.GetValue(game), Is.False);
            }

            Assert.That(captureCount, Is.GreaterThan(0));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
