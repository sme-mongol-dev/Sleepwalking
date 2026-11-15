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
    public sealed class StaticCameraRenderTests
    {
        [UnityTest]
        public IEnumerator SevenDreamBackdropsCoverFollowFramesInBothAspects()
        {
            AsyncOperation loading = SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
            while (!loading.isDone) yield return null;
            yield return null;

            GameObject root = GameObject.Find("Sleepwalking Game");
            Assert.That(root, Is.Not.Null);
            MonoBehaviour game = null;
            foreach (MonoBehaviour component in root.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().Name == "SleepwalkingGame") game = component;
            Assert.That(game, Is.Not.Null);
            Type type = game.GetType();
            Camera camera = (Camera)type.GetField("GameCamera").GetValue(game);
            FieldInfo completed = type.GetField("completedDreams", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo transitioning = type.GetField("transitioning", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo startDream = type.GetMethod("StartDream");
            MethodInfo returnToRoom = type.GetMethod("ReturnToRoom");
            MethodInfo restartDream = type.GetMethod("RestartDream");

            for (int levelIndex = 1; levelIndex <= 7; levelIndex++)
            {
                type.GetField("tutorialStage", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(game, 2);
                completed.SetValue(game, levelIndex - 1);
                startDream.Invoke(game, null);
                float deadline = Time.realtimeSinceStartup + 10f;
                while ((!(bool)type.GetProperty("InDream").GetValue(game) ||
                    (bool)transitioning.GetValue(game)) && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.That((bool)type.GetProperty("InDream").GetValue(game), Is.True);
                GameObject level = GameObject.Find("Dream L" + levelIndex.ToString("00"));
                Assert.That(level, Is.Not.Null);
                Transform theme = level.transform.Find("Dream style");
                Assert.That(theme, Is.Not.Null);
                Renderer backdrop = theme.Find("Distant Background Left")?.GetComponent<Renderer>();
                Assert.That(backdrop, Is.Not.Null);
                GameObject dreamer = GameObject.Find("Dreamer");
                Rigidbody2D body = dreamer.GetComponent<Rigidbody2D>();
                Transform exit = GameObject.Find("BedExit").transform;
                float unit = Mathf.Abs(level.transform.lossyScale.x);

                for (int aspectIndex = 0; aspectIndex < 2; aspectIndex++)
                {
                    int width = aspectIndex == 0 ? 960 : 800;
                    int height = aspectIndex == 0 ? 540 : 600;
                    RenderTexture target = new RenderTexture(width, height, 24);
                    target.Create();
                    camera.targetTexture = target;
                    restartDream.Invoke(game, null);
                    body.simulated = false;
                    yield return null;

                    AssertCover(backdrop, camera, width, height, levelIndex);
                    string suffix = aspectIndex == 0 ? "-16x9" : "-4x3";
                    Capture(camera, "follow-camera-L" + levelIndex.ToString("00") + suffix + "-start");

                    dreamer.transform.position = exit.position + Vector3.up * (.5f * unit);
                    yield return new WaitForSeconds(.8f);
                    AssertCover(backdrop, camera, width, height, levelIndex);
                    Capture(camera, "follow-camera-L" + levelIndex.ToString("00") + suffix + "-end");
                    camera.targetTexture = null;
                    target.Release();
                    UnityEngine.Object.Destroy(target);
                }

                returnToRoom.Invoke(game, null);
                yield return null;
            }
        }

        static void AssertCover(Renderer backdrop, Camera camera, int width, int height, int levelIndex)
        {
            float halfWidth = camera.orthographicSize * width / height;
            float halfHeight = camera.orthographicSize;
            Vector3 center = camera.transform.position;
            Assert.That(backdrop.bounds.min.x, Is.LessThanOrEqualTo(center.x - halfWidth),
                "L" + levelIndex + " backdrop left edge");
            Assert.That(backdrop.bounds.max.x, Is.GreaterThanOrEqualTo(center.x + halfWidth),
                "L" + levelIndex + " backdrop right edge");
            Assert.That(backdrop.bounds.min.y, Is.LessThanOrEqualTo(center.y - halfHeight),
                "L" + levelIndex + " backdrop bottom edge");
            Assert.That(backdrop.bounds.max.y, Is.GreaterThanOrEqualTo(center.y + halfHeight),
                "L" + levelIndex + " backdrop top edge");
        }

        static void Capture(Camera camera, string name)
        {
            RenderTexture target = camera.targetTexture;
            RenderTexture previous = RenderTexture.active;
            Texture2D image = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            try
            {
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                image.Apply();
                string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                File.WriteAllBytes(Path.Combine(project, "Docs", "QA", name + ".png"), image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                UnityEngine.Object.Destroy(image);
            }
        }
    }
}
