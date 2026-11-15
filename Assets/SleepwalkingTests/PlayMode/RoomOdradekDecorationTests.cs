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
            Assert.That(decorations.Count, Is.EqualTo(38));
            var names = new HashSet<string>();
            var ids = new HashSet<string>();
            foreach (object decoration in decorations)
            {
                names.Add((string)decoration.GetType().GetField("Name").GetValue(decoration));
                string id = (string)decoration.GetType().GetField("Id").GetValue(decoration);
                Assert.That(ids.Add(id), Is.True, "Duplicate decoration id: " + id);
            }
            foreach (string required in new[] { "Window", "Nightstand", "Calendar", "Poster",
                "Big Clock Arrow", "Small Clock Arrow", "Slippers", "Bookshelf 1", "Bookshelf 2", "Eye",
                "Calendar 2", "Calendar 3", "Bed Look 08", "Crack 4 Stage 1", "Crack 5 Stage 3" })
                Assert.That(names, Does.Contain(required));
            Assert.That(ids, Does.Not.Contain("eyeball"));
            Assert.That(ids, Does.Not.Contain("crack.1"));
            Transform eye = room.transform.Find("Eye");
            Transform pupil = eye?.Find("Eyeball");
            Assert.That(pupil, Is.Not.Null, "Eye and pupil must share one Odradek object.");
            MonoBehaviour tracker = FindComponent(eye.gameObject, "Sleepwalking.Visual.RoomEyeTracker");
            Assert.That(tracker, Is.Not.Null);
            MeshRenderer shelf = room.transform.Find("Books")?.GetComponent<MeshRenderer>();
            Assert.That(shelf, Is.Not.Null);

            FieldInfo odradek = game.GetType().GetField("odradek", BindingFlags.Instance | BindingFlags.NonPublic);
            Camera camera = (Camera)game.GetType().GetField("GameCamera").GetValue(game);
            Assert.That(odradek, Is.Not.Null);
            Assert.That(camera, Is.Not.Null);

            foreach (int value in new[] { 1, 50, 54, 55, 75, 100 })
            {
                odradek.SetValue(game, value);
                yield return null;
                Assert.That((int)environment.GetType().GetProperty("CurrentOdradek").GetValue(environment), Is.EqualTo(value));
                foreach (object decoration in decorations)
                {
                    Type type = decoration.GetType();
                    int minimum = (int)type.GetField("MinOdradek").GetValue(decoration);
                    int maximum = (int)type.GetField("MaxOdradek").GetValue(decoration);
                    GameObject instance = (GameObject)type.GetField("Instance").GetValue(decoration);
                    string name = (string)type.GetField("Name").GetValue(decoration);
                    string group = (string)type.GetField("Group").GetValue(decoration);
                    Assert.That(instance, Is.Not.Null, name);
                    if (Convert.ToInt32(type.GetField("Mode").GetValue(decoration)) != 0) continue;
                    bool active = value >= minimum && value <= maximum;
                    Assert.That(instance.activeSelf, Is.EqualTo(active), name + " at " + value);
                }
                AssertVariant(decorations, "poster", value, 1);
                AssertVariant(decorations, "calendar", value, value == 1 ? 0 : 1);
                AssertVariant(decorations, "bed", value, 1);
                AssertVariant(decorations, "shelf", value, 1);
                string shelfId = value < 55 ? "shelf.1" : "shelf.2";
                foreach (object decoration in decorations)
                {
                    Type type = decoration.GetType();
                    if ((string)type.GetField("Id").GetValue(decoration) != shelfId) continue;
                    var properties = new MaterialPropertyBlock();
                    shelf.GetPropertyBlock(properties);
                    Assert.That(properties.GetTexture("_BaseMap"),
                        Is.SameAs(type.GetField("Texture").GetValue(decoration)), shelfId);
                    break;
                }
                AssertVariant(decorations, "crack.4", value, value == 100 ? 1 : 0);
                AssertVariant(decorations, "crack.5", value, value == 100 ? 1 : 0);

                int hotspots = 0;
                foreach (MonoBehaviour component in room.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (component == null || component.GetType().FullName != "Sleepwalking.Visual.RoomHotspot") continue;
                    Assert.That(component.gameObject.activeInHierarchy, Is.True, component.gameObject.name);
                    hotspots++;
                }
                Assert.That(hotspots, Is.EqualTo(4));
                if (value == 50)
                {
                    float progress = (float)environment.GetType().GetField("clockProgress",
                        BindingFlags.Instance | BindingFlags.NonPublic).GetValue(environment);
                    Assert.That(progress, Is.GreaterThan(0f).And.LessThan(49f / 99f),
                        "Clock hands should move gradually toward the new Odradek value.");
                }
                environment.GetType().GetMethod("UpdateClock", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(environment, new object[] { 4f });
                CaptureRoom(camera, value);
            }

            environment.GetType().GetMethod("UpdateClock", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(environment, new object[] { 4f });
            Transform hands = (Transform)environment.GetType().GetField("ClockHand").GetValue(environment);
            Assert.That(Quaternion.Angle(hands.Find("Big Hand Pivot").localRotation, Quaternion.identity),
                Is.LessThan(.01f), "Minute hand should indicate 12:00 at Odradek 100.");
            Assert.That(Quaternion.Angle(hands.Find("Small Hand Pivot").localRotation, Quaternion.identity),
                Is.LessThan(.01f), "Hour hand should indicate 12:00 at Odradek 100.");

            foreach (var growth in new[] { (80, .3f, "Crack 4"), (85, .6f, "Crack 4"),
                (90, 1f, "Crack 4"), (94, 1f, "Crack 4"), (95, .3f, "Crack 5"), (97, .6f, "Crack 5"),
                (99, 1f, "Crack 5") })
            {
                odradek.SetValue(game, growth.Item1);
                yield return null;
                Transform crack = room.transform.Find(growth.Item3);
                Assert.That(crack.gameObject.activeSelf, Is.True);
                Assert.That(crack.localScale.x, Is.EqualTo(1.92f * growth.Item2).Within(.001f));
            }
            odradek.SetValue(game, 100);
            yield return null;
            Transform crack4 = room.transform.Find("Crack 4");
            Assert.That(crack4.gameObject.activeSelf, Is.False, "The eye replaces crack 4 in its fourth stage.");
            Assert.That(eye.gameObject.activeSelf, Is.True);
            Assert.That(eye.localPosition.x, Is.EqualTo(crack4.localPosition.x).Within(.001f));
            Assert.That(eye.localPosition.y, Is.EqualTo(crack4.localPosition.y).Within(.001f));
            Vector3 pupilStart = pupil.localPosition;
            MethodInfo look = tracker.GetType().GetMethod("LookAtWorld");
            for (int q = 0; q < 12; q++) look.Invoke(tracker, new object[] { (Vector2)(eye.position + Vector3.right * 10f) });
            Assert.That(pupil.localPosition.x, Is.GreaterThan(pupilStart.x + .01f));
            for (int q = 0; q < 24; q++) look.Invoke(tracker, new object[] { (Vector2)(eye.position + Vector3.left * 10f) });
            Assert.That(pupil.localPosition.x, Is.LessThan(pupilStart.x - .01f));

            MethodInfo applyJson = environment.GetType().GetMethod("TryApplyJson");
            object[] invalid = { "{\"version\":1,\"decorations\":[{\"id\":\"bed.base\",\"group\":\"bed\",\"min\":1,\"max\":12,\"asset\":\"crack.1\"}]}", null };
            Assert.That((bool)applyJson.Invoke(environment, invalid), Is.False);
            Assert.That((string)invalid[1], Does.Contain("asset"));

            object[] eyeThreshold = { "{\"version\":1,\"decorations\":[" +
                "{\"id\":\"crack.4.stage3\",\"group\":\"crack.4\",\"min\":90,\"max\":95,\"asset\":\"crack.4\"}," +
                "{\"id\":\"eye\",\"group\":\"crack.4\",\"min\":96,\"max\":100,\"asset\":\"eye\"}]}", null };
            Assert.That((bool)applyJson.Invoke(environment, eyeThreshold), Is.True, eyeThreshold[1]?.ToString());
            odradek.SetValue(game, 95);
            yield return null;
            Assert.That(crack4.gameObject.activeSelf, Is.True);
            Assert.That(eye.gameObject.activeSelf, Is.False);
            odradek.SetValue(game, 96);
            yield return null;
            Assert.That(crack4.gameObject.activeSelf, Is.False);
            Assert.That(eye.gameObject.activeSelf, Is.True);

            object[] update = { "{\"version\":1,\"decorations\":[{\"id\":\"crack.4.stage1\",\"group\":\"crack.4\",\"min\":81,\"max\":84,\"asset\":\"crack.4\"}]}", null };
            Assert.That((bool)applyJson.Invoke(environment, update), Is.True, update[1]?.ToString());
            odradek.SetValue(game, 80);
            yield return null;
            foreach (object decoration in decorations)
                if ((string)decoration.GetType().GetField("Id").GetValue(decoration) == "crack.4.stage1")
                    Assert.That(((GameObject)decoration.GetType().GetField("Instance").GetValue(decoration)).activeSelf,
                        Is.False, "JSON threshold must apply without rebuilding C#.");

            object[] calendar = { "{\"version\":1,\"decorations\":[" +
                "{\"id\":\"calendar.1\",\"group\":\"calendar\",\"min\":60,\"max\":74,\"asset\":\"calendar.1\"}," +
                "{\"id\":\"calendar.2\",\"group\":\"calendar\",\"min\":75,\"max\":89,\"asset\":\"calendar.2\"}," +
                "{\"id\":\"calendar.3\",\"group\":\"calendar\",\"min\":90,\"max\":100,\"asset\":\"calendar.3\"}]}", null };
            Assert.That((bool)applyJson.Invoke(environment, calendar), Is.True, calendar[1]?.ToString());
            odradek.SetValue(game, 59);
            yield return null;
            AssertVariant(decorations, "calendar", 59, 0);
            odradek.SetValue(game, 60);
            yield return null;
            AssertVariant(decorations, "calendar", 60, 1);
        }

        static void AssertVariant(IList decorations, string group, int value, int expectedCount)
        {
            int count = 0;
            foreach (object decoration in decorations)
            {
                Type type = decoration.GetType();
                if ((string)type.GetField("Group").GetValue(decoration) != group ||
                    Convert.ToInt32(type.GetField("Mode").GetValue(decoration)) != 1) continue;
                int minimum = (int)type.GetField("MinOdradek").GetValue(decoration);
                int maximum = (int)type.GetField("MaxOdradek").GetValue(decoration);
                if (value < minimum || value > maximum) continue;
                count++;
                GameObject target = (GameObject)type.GetField("Instance").GetValue(decoration);
                Texture expected = (Texture)type.GetField("Texture").GetValue(decoration);
                Assert.That(target.activeSelf, Is.True, group + " at " + value);
                var properties = new MaterialPropertyBlock();
                target.GetComponentInChildren<MeshRenderer>(true).GetPropertyBlock(properties);
                if (group != "bed")
                    Assert.That(properties.GetTexture("_MainTex"), Is.EqualTo(expected), group + " texture at " + value);
            }
            Assert.That(count, Is.EqualTo(expectedCount), group + " variants at " + value);
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
