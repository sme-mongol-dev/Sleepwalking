using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Sleepwalking.Tests.PlayMode
{
    public sealed class RoomOdradekDecorationTests
    {
        [UnityTest]
        public IEnumerator DecorationsFollowOdradekAndHotspotsStayAvailable()
        {
            AsyncOperation loading = SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
            Assert.That(loading, Is.Not.Null);
            while (!loading.isDone) yield return null;
            yield return null;

            GameObject room = GameObject.Find("Waking room");
            GameObject gameObject = GameObject.Find("Sleepwalking Game");
            Assert.That(room, Is.Not.Null);
            Assert.That(gameObject, Is.Not.Null);

            MonoBehaviour environment = FindComponent(room, "Sleepwalking.Visual.RoomEnvironment");
            MonoBehaviour game = FindComponent(gameObject, "Sleepwalking.SleepwalkingGame");
            Assert.That(environment, Is.Not.Null);
            Assert.That(game, Is.Not.Null);

            IList decorations = (IList)environment.GetType().GetField("Decorations").GetValue(environment);
            Assert.That(decorations.Count, Is.GreaterThanOrEqualTo(9));
            var names = new HashSet<string>();
            foreach (object decoration in decorations)
                names.Add((string)decoration.GetType().GetField("Name").GetValue(decoration));
            foreach (string required in new[] { "Window", "Nightstand", "Calendar", "Poster",
                "Big Clock Arrow", "Small Clock Arrow", "Slippers", "Eye", "Eyeball" })
                Assert.That(names, Does.Contain(required));

            FieldInfo odradek = game.GetType().GetField("odradek", BindingFlags.Instance | BindingFlags.NonPublic);
            Camera camera = (Camera)game.GetType().GetField("GameCamera").GetValue(game);
            Assert.That(odradek, Is.Not.Null);
            Assert.That(camera, Is.Not.Null);

            foreach (int value in new[] { 1, 100 })
            {
                odradek.SetValue(game, value);
                yield return null;
                Assert.That((int)environment.GetType().GetProperty("CurrentOdradek").GetValue(environment), Is.EqualTo(value));
                foreach (object decoration in decorations)
                {
                    Type type = decoration.GetType();
                    int minimum = (int)type.GetField("MinOdradek").GetValue(decoration);
                    GameObject instance = (GameObject)type.GetField("Instance").GetValue(decoration);
                    string name = (string)type.GetField("Name").GetValue(decoration);
                    Assert.That(instance, Is.Not.Null, name);
                    Assert.That(instance.activeSelf, Is.EqualTo(value >= minimum), name + " at " + value);
                }

                int hotspots = 0;
                foreach (MonoBehaviour component in room.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (component == null || component.GetType().FullName != "Sleepwalking.Visual.RoomHotspot") continue;
                    Assert.That(component.gameObject.activeInHierarchy, Is.True, component.gameObject.name);
                    hotspots++;
                }
                Assert.That(hotspots, Is.EqualTo(4));
                CaptureRoom(camera, value);
            }
        }

        static MonoBehaviour FindComponent(GameObject root, string name)
        {
            foreach (MonoBehaviour component in root.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().FullName == name)
                    return component;
            return null;
        }

        static void CaptureRoom(Camera camera, int odradek)
        {
            RenderTexture target = new RenderTexture(1280, 720, 24);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            Texture2D image = new Texture2D(1280, 720, TextureFormat.RGBA32, false);
            try
            {
                Assert.That(target.Create(), Is.True, "Room capture needs an active graphics device.");
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                image.Apply();
                string folder = Path.Combine(Directory.GetCurrentDirectory(), "Docs/QA/VisualLogs");
                Directory.CreateDirectory(folder);
                File.WriteAllBytes(Path.Combine(folder, "MenuOdradek" + odradek + ".png"), image.EncodeToPNG());
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
