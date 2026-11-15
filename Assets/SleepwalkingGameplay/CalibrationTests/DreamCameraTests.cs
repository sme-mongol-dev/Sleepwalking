using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Sleepwalking.Tests.PlayMode
{
    public sealed class DreamCameraTests
    {
        [UnityTest]
        public IEnumerator SevenDreamsFitInStaticFrameAtBothAspects()
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
            const BindingFlags privateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
            Camera camera = (Camera)type.GetField("GameCamera").GetValue(game);
            MethodInfo startDream = type.GetMethod("StartDream");
            MethodInfo completeDream = type.GetMethod("CompleteDream");
            MethodInfo restartDream = type.GetMethod("RestartDream");
            type.GetField("tutorialStage", privateInstance).SetValue(game, 2);

            for (int levelIndex = 1; levelIndex <= 7; levelIndex++)
            {
                startDream.Invoke(game, null);
                float deadline = Time.realtimeSinceStartup + 10f;
                while ((!(bool)type.GetProperty("InDream").GetValue(game) ||
                    (bool)type.GetField("transitioning", privateInstance).GetValue(game)) &&
                    Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That((bool)type.GetProperty("InDream").GetValue(game), Is.True);

                GameObject level = GameObject.Find("Dream L" + levelIndex.ToString("00"));
                GameObject dreamer = GameObject.Find("Dreamer");
                Transform start = GameObject.Find("PlayerStart")?.transform;
                GameObject exit = GameObject.Find("BedExit");
                Assert.That(level, Is.Not.Null);
                Assert.That(dreamer, Is.Not.Null);
                Assert.That(start, Is.Not.Null);
                Assert.That(exit, Is.Not.Null);
                float unit = Mathf.Abs(level.transform.lossyScale.x);
                Rigidbody2D body = dreamer.GetComponent<Rigidbody2D>();
                body.simulated = false;
                float minX = (float)type.GetField("dreamCameraMinX", privateInstance).GetValue(game);
                float maxX = (float)type.GetField("dreamCameraMaxX", privateInstance).GetValue(game);

                for (int aspectIndex = 0; aspectIndex < 2; aspectIndex++)
                {
                    int width = aspectIndex == 0 ? 960 : 800;
                    int height = aspectIndex == 0 ? 540 : 600;
                    string aspect = aspectIndex == 0 ? "16x9" : "4x3";
                    RenderTexture target = new RenderTexture(width, height, 24);
                    target.Create();
                    camera.targetTexture = target;
                    restartDream.Invoke(game, null);
                    yield return null;

                    Vector3 cameraStart = camera.transform.position;
                    float sizeStart = camera.orthographicSize;
                    float halfWidth = sizeStart * (float)width / height;
                    Assert.That(cameraStart.x - halfWidth, Is.LessThanOrEqualTo(minX - .1f * unit),
                        "Left edge outside " + aspect + " L" + levelIndex);
                    Assert.That(cameraStart.x + halfWidth, Is.GreaterThanOrEqualTo(maxX + .1f * unit),
                        "Right edge outside " + aspect + " L" + levelIndex);
                    Assert.That(camera.WorldToViewportPoint(start.position).x, Is.InRange(0f, 1f));
                    Assert.That(camera.WorldToViewportPoint(exit.transform.position).x, Is.InRange(0f, 1f));

                    body.simulated = false;
                    dreamer.transform.position = exit.transform.position + Vector3.up * (.5f * unit);
                    yield return new WaitForSeconds(.4f);
                    Assert.That((bool)type.GetProperty("InDream").GetValue(game), Is.True);
                    Assert.That(camera.transform.position.x, Is.EqualTo(cameraStart.x).Within(.01f),
                        "Camera moved horizontally in " + aspect + " L" + levelIndex);
                    Assert.That(camera.transform.position.y, Is.EqualTo(cameraStart.y).Within(.01f));
                    Assert.That(camera.orthographicSize, Is.EqualTo(sizeStart).Within(.01f));

                    camera.targetTexture = null;
                    target.Release();
                    UnityEngine.Object.Destroy(target);
                }

                completeDream.Invoke(game, null);
                deadline = Time.realtimeSinceStartup + 10f;
                while (((bool)type.GetProperty("InDream").GetValue(game) ||
                    (bool)type.GetField("transitioning", privateInstance).GetValue(game)) &&
                    Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That((bool)type.GetProperty("InDream").GetValue(game), Is.False);
            }
        }
    }
}
