using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Sleepwalking.Tests.PlayMode
{
    public sealed class SleepwalkingLaserIntegrationTests
    {
        const float Unit = 32f;

        sealed class LaserCase
        {
            public int Level;
            public string Id;
            public string Button;
        }

        [UnityTest]
        public IEnumerator SecondLasersKillAndButtonsDisableThenRestoreMatchingArtAndColliders()
        {
            LaserCase[] cases =
            {
                new LaserCase { Level = 4, Id = "L04_SecondLaser", Button = "L04_ButtonSecond" },
                new LaserCase { Level = 7, Id = "L07_SecondLaser", Button = "L07_ButtonC" }
            };
            List<string> trace = new List<string>
            {
                "level,laser,phase,time,beamCollider,baseCollider,beamArt,baseArt,dying,beamPixels"
            };

            foreach (LaserCase item in cases)
            {
                AsyncOperation loading = SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
                Assert.That(loading, Is.Not.Null);
                while (!loading.isDone) yield return null;
                yield return null;

                MonoBehaviour game = FindComponent(GameObject.Find("Sleepwalking Game"),
                    "Sleepwalking.SleepwalkingGame");
                Assert.That(game, Is.Not.Null);
                Type gameType = game.GetType();
                FieldInfo completed = gameType.GetField("completedDreams",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo dying = gameType.GetField("dying", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo transitioning = gameType.GetField("transitioning",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                PropertyInfo inDream = gameType.GetProperty("InDream");
                Assert.That(completed, Is.Not.Null);
                Assert.That(dying, Is.Not.Null);
                completed.SetValue(game, item.Level - 1);
                gameType.GetMethod("StartDream").Invoke(game, null);
                float deadline = Time.realtimeSinceStartup + 10f;
                while ((!((bool)inDream.GetValue(game)) || (bool)transitioning.GetValue(game)) &&
                    Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That((bool)inDream.GetValue(game), Is.True);
                Assert.That((bool)transitioning.GetValue(game), Is.False);

                GameObject dreamer = GameObject.Find("Dreamer");
                MonoBehaviour player = FindComponent(dreamer, "Sleepwalking.SleepwalkingPlayer");
                Collider2D shape = dreamer?.GetComponent<Collider2D>();
                Rigidbody2D body = dreamer?.GetComponent<Rigidbody2D>();
                Collider2D beam = GameObject.Find(item.Id + "_Beam")?.GetComponent<Collider2D>();
                Collider2D laserBase = GameObject.Find(item.Id + "_Base")?.GetComponent<Collider2D>();
                Collider2D button = GameObject.Find(item.Button)?.GetComponent<Collider2D>();
                Transform start = GameObject.Find("PlayerStart")?.transform;
                Assert.That(player, Is.Not.Null);
                Assert.That(shape, Is.Not.Null);
                Assert.That(body, Is.Not.Null);
                Assert.That(beam, Is.Not.Null);
                Assert.That(laserBase, Is.Not.Null);
                Assert.That(button, Is.Not.Null);
                Assert.That(start, Is.Not.Null);
                SpriteRenderer beamArt = beam.transform.Find("Dream Hazard Art")?.GetComponent<SpriteRenderer>();
                SpriteRenderer baseArt = laserBase.transform.Find("Dream Hazard Art")?.GetComponent<SpriteRenderer>();
                Assert.That(beamArt, Is.Not.Null);
                Assert.That(baseArt, Is.Not.Null);
                MonoBehaviour buttonMechanism = FindComponent(button.gameObject,
                    "Sleepwalking.SleepwalkingMechanism");
                MonoBehaviour beamMechanism = FindComponent(beam.gameObject,
                    "Sleepwalking.SleepwalkingMechanism");
                MonoBehaviour baseMechanism = FindComponent(laserBase.gameObject,
                    "Sleepwalking.SleepwalkingMechanism");
                Assert.That(buttonMechanism, Is.Not.Null);
                Assert.That(beamMechanism, Is.Not.Null);
                Assert.That(baseMechanism, Is.Not.Null);
                string linked = (string)buttonMechanism.GetType().GetField("LinkedFeatureId")
                    .GetValue(buttonMechanism);
                Assert.That(linked, Is.EqualTo(item.Id));
                Assert.That((string)beamMechanism.GetType().GetField("FeatureId").GetValue(beamMechanism),
                    Is.EqualTo(item.Id));
                Assert.That((string)baseMechanism.GetType().GetField("FeatureId").GetValue(baseMechanism),
                    Is.EqualTo(item.Id));

                Camera camera = (Camera)gameType.GetField("GameCamera").GetValue(game);
                int pixelHeight = camera.targetTexture != null ? camera.targetTexture.height :
                    Mathf.Max(1, camera.pixelHeight);
                float beamPixels = beam.bounds.size.x * pixelHeight / (camera.orthographicSize * 2f);
                Assert.That(beamPixels, Is.InRange(3f, 4f), item.Id);
                Assert.That(beam.enabled && !laserBase.enabled && beamArt.enabled && baseArt.enabled,
                    Is.True, item.Id + " starts active");
                Record(trace, item, "active", beam, laserBase, beamArt, baseArt, dying, beamPixels);

                MethodInfo reset = player.GetType().GetMethod("ResetAt", new[] { typeof(Vector2) });
                Assert.That(reset, Is.Not.Null);
                yield return HitBeam(player, reset, shape, beam, game, dying, item);
                Record(trace, item, "beamHit", beam, laserBase, beamArt, baseArt, dying, beamPixels);
                deadline = Time.realtimeSinceStartup + 5f;
                while ((bool)dying.GetValue(game) && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That((bool)dying.GetValue(game), Is.False, item.Id + " retry did not finish");
                Assert.That((bool)inDream.GetValue(game), Is.True);
                Assert.That((int)completed.GetValue(game), Is.EqualTo(item.Level - 1));
                Assert.That(beam.enabled && !laserBase.enabled, Is.True, item.Id + " retry reset");

                Vector2 buttonPosition = new Vector2(button.bounds.center.x,
                    button.bounds.min.y + shape.bounds.extents.y + .01f * Unit);
                reset.Invoke(player, new object[] { buttonPosition });
                Physics2D.SyncTransforms();
                Assert.That(button.Distance(shape).isOverlapped, Is.True,
                    item.Button + " physical probe misses button");
                bool disabled = false;
                for (int q = 0; q < 30; q++)
                {
                    yield return new WaitForFixedUpdate();
                    if (!beam.enabled && !laserBase.enabled) { disabled = true; break; }
                }
                Assert.That(disabled, Is.True, item.Button + " did not disable the beam");
                yield return null;
                Assert.That(!beamArt.enabled && baseArt.enabled, Is.True,
                    item.Id + " beam disappeared with its emitter");
                Record(trace, item, "buttonOff", beam, laserBase, beamArt, baseArt, dying, beamPixels);

                reset.Invoke(player, new object[] { (Vector2)beam.bounds.center });
                Physics2D.SyncTransforms();
                for (int q = 0; q < 3; q++) yield return new WaitForFixedUpdate();
                Assert.That((bool)dying.GetValue(game), Is.False,
                    item.Id + " killed player while button disabled it");
                Record(trace, item, "safeBeamContact", beam, laserBase, beamArt, baseArt, dying, beamPixels);

                reset.Invoke(player, new object[] { (Vector2)start.position + Vector2.up * (.5f * Unit) });
                Physics2D.SyncTransforms();
                deadline = Time.realtimeSinceStartup + 10f;
                while (!beam.enabled && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.That(beam.enabled && !laserBase.enabled, Is.True,
                    item.Id + " did not restore after button timer");
                yield return null;
                Assert.That(beamArt.enabled && baseArt.enabled, Is.True,
                    item.Id + " art did not restore after button timer");
                Record(trace, item, "restored", beam, laserBase, beamArt, baseArt, dying, beamPixels);
                yield return HitBeam(player, reset, shape, beam, game, dying, item);
                Record(trace, item, "restoredBeamHit", beam, laserBase, beamArt, baseArt, dying, beamPixels);
            }

            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            File.WriteAllLines(Path.Combine(root, "Docs", "QA", "laser-integration.csv"), trace);
            LogAssert.NoUnexpectedReceived();
        }

        static IEnumerator HitBeam(MonoBehaviour player, MethodInfo reset, Collider2D shape,
            Collider2D beam, MonoBehaviour game, FieldInfo dying, LaserCase item)
        {
            reset.Invoke(player, new object[] { (Vector2)beam.bounds.center });
            Physics2D.SyncTransforms();
            Assert.That(beam.Distance(shape).isOverlapped, Is.True,
                item.Id + " physical probe misses beam");
            bool killed = false;
            for (int q = 0; q < 25; q++)
            {
                yield return new WaitForFixedUpdate();
                if (!(bool)dying.GetValue(game)) continue;
                killed = true;
                break;
            }
            Assert.That(killed, Is.True, item.Id + " active beam did not kill on contact");
        }

        static void Record(List<string> trace, LaserCase item, string phase, Collider2D beam,
            Collider2D laserBase, SpriteRenderer beamArt, SpriteRenderer baseArt, FieldInfo dying,
            float beamPixels)
        {
            MonoBehaviour game = FindComponent(GameObject.Find("Sleepwalking Game"),
                "Sleepwalking.SleepwalkingGame");
            trace.Add(string.Format(CultureInfo.InvariantCulture,
                "{0},{1},{2},{3:F3},{4},{5},{6},{7},{8},{9:F3}", item.Level, item.Id, phase,
                Time.time, beam.enabled, laserBase.enabled, beamArt.enabled, baseArt.enabled,
                (bool)dying.GetValue(game), beamPixels));
        }

        static MonoBehaviour FindComponent(GameObject owner, string typeName)
        {
            if (owner == null) return null;
            foreach (MonoBehaviour component in owner.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().FullName == typeName)
                    return component;
            return null;
        }
    }
}
