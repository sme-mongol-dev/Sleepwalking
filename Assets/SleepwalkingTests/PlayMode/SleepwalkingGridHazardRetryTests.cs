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
    public sealed class SleepwalkingGridHazardRetryTests
    {
        const float Unit = 32f;

        [UnityTest]
        public IEnumerator EveryLethalColliderHasVisibleArt()
        {
            for (int levelIndex = 1; levelIndex <= 7; levelIndex++)
            {
                AsyncOperation loading = SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
                Assert.That(loading, Is.Not.Null);
                while (!loading.isDone) yield return null;
                yield return null;

                MonoBehaviour game = FindComponent(GameObject.Find("Sleepwalking Game"),
                    "Sleepwalking.SleepwalkingGame");
                Assert.That(game, Is.Not.Null);
                Type gameType = game.GetType();
                gameType.GetField("completedDreams", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(game, levelIndex - 1);
                gameType.GetField("tutorialStage", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(game, 2);
                gameType.GetMethod("StartDream").Invoke(game, null);
                PropertyInfo inDream = gameType.GetProperty("InDream");
                FieldInfo transitioning = gameType.GetField("transitioning",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                float deadline = Time.realtimeSinceStartup + 10f;
                while ((!((bool)inDream.GetValue(game)) ||
                    (bool)transitioning.GetValue(game)) && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.That((bool)inDream.GetValue(game), Is.True);

                GameObject level = GameObject.Find("Dream L" + levelIndex.ToString("00"));
                Assert.That(level, Is.Not.Null);
                int lethalCount = 0;
                foreach (MonoBehaviour component in level.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (component == null || component.GetType().FullName !=
                        "Sleepwalking.Levels.SleepwalkingLevelFeature") continue;
                    string kind = component.GetType().GetProperty("Kind").GetValue(component).ToString();
                    Assert.That(kind, Is.Not.EqualTo("FallReset"), component.name);
                    if (kind != "Spikes" && kind != "Saw" && kind != "LaserBase" &&
                        kind != "LaserBeam") continue;

                    lethalCount++;
                    Collider2D collider = component.GetComponent<Collider2D>();
                    SpriteRenderer art = component.transform.Find("Dream Hazard Art")?
                        .GetComponent<SpriteRenderer>();
                    Assert.That(collider, Is.Not.Null, component.name + " collider");
                    Assert.That(art, Is.Not.Null, component.name + " art");
                    Assert.That(art.sprite, Is.Not.Null, component.name + " sprite");
                    Assert.That(art.enabled && art.gameObject.activeInHierarchy && art.color.a > 0f,
                        Is.True, component.name + " invisible lethal art");
                }
                Assert.That(lethalCount, Is.GreaterThan(0), "L" + levelIndex + " hazards");
            }
            LogAssert.NoUnexpectedReceived();
        }

        sealed class Case
        {
            public int Level;
            public string Hazard;
            public string Support;
            public string SafeSupport;
            public int ExpectedCount;
        }

        [UnityTest]
        public IEnumerator PhysicalTrapsRetryTheSameDream()
        {
            Case[] cases =
            {
                new Case { Level = 1, Hazard = "L01_GapA_1", Support = "Cave_L01_GapAFoundation", SafeSupport = "Start", ExpectedCount = 2 },
                new Case { Level = 2, Hazard = "L02_FloorSpikes_1", Support = "SpikeLanding" },
                new Case { Level = 2, Hazard = "L02_PitSpikes_1", Support = "Cave_PitSpikeFoundation", SafeSupport = "Start", ExpectedCount = 2 },
                new Case { Level = 2, Hazard = "L02_SecondPitSpikes_1", Support = "Cave_SecondPitFoundation", SafeSupport = "ExitRun", ExpectedCount = 2 },
                new Case { Level = 2, Hazard = "L02_BridgeSpikes_1", Support = "SpikeBridge" },
                new Case { Level = 2, Hazard = "L02_GapB_1", Support = "Cave_L02_GapBFoundation", SafeSupport = "SpikeBridge", ExpectedCount = 4 },
                new Case { Level = 3, Hazard = "L03_Saw", Support = "SawRun" },
                new Case { Level = 3, Hazard = "L03_SecondSaw", Support = "SecondSawRun" },
                new Case { Level = 3, Hazard = "L03_GapD_1", Support = "Cave_L03_GapDFoundation", SafeSupport = "SecondSawRun", ExpectedCount = 4 },
                new Case { Level = 4, Hazard = "L04_Laser_Base", Support = "LaserRun" },
                new Case { Level = 4, Hazard = "L04_SecondLaser_Base", Support = "SecondLaserRun" },
                new Case { Level = 4, Hazard = "L04_GapA_1", Support = "Cave_L04_GapAFoundation", SafeSupport = "LaserRun", ExpectedCount = 2 },
                new Case { Level = 5, Hazard = "L05_PitSpikes_1", Support = "Cave_PitSpikeFoundation", SafeSupport = "Start", ExpectedCount = 32 },
                new Case { Level = 5, Hazard = "L05_SecondPitSpikes_1", Support = "Cave_SecondPitFoundation", SafeSupport = "ExitRun", ExpectedCount = 24 },
                new Case { Level = 6, Hazard = "L06_FloorSpikes_1", Support = "Cave_L06_FloorSpikesFoundation", SafeSupport = "Start", ExpectedCount = 68 },
                new Case { Level = 7, Hazard = "L07_Saw", Support = "DashLanding" },
                new Case { Level = 7, Hazard = "L07_GapB_1", Support = "Cave_L07_GapBFoundation", SafeSupport = "DashLanding", ExpectedCount = 2 },
                new Case { Level = 7, Hazard = "L07_PitSpikes_1", Support = "Cave_PitSpikeFoundation", SafeSupport = "Cave_RunFoundation", ExpectedCount = 12 },
                new Case { Level = 7, Hazard = "L07_SecondLaser_Base", Support = "SecondLaserRun" },
                new Case { Level = 7, Hazard = "L07_SecondPitSpikes_1", Support = "Cave_SecondPitFoundation", SafeSupport = "SecondLaserRun", ExpectedCount = 12 }
            };
            List<string> trace = new List<string>
            {
                "level,hazard,step,time,x,y,hazardTouch,dying"
            };
            foreach (Case item in cases)
            {
                AsyncOperation loading = SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
                Assert.That(loading, Is.Not.Null);
                while (!loading.isDone) yield return null;
                yield return null;
                MonoBehaviour game = FindComponent(GameObject.Find("Sleepwalking Game"),
                    "Sleepwalking.SleepwalkingGame");
                Assert.That(game, Is.Not.Null);
                Type gameType = game.GetType();
                FieldInfo count = gameType.GetField("completedDreams",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo dying = gameType.GetField("dying",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo transitioning = gameType.GetField("transitioning",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                PropertyInfo inDream = gameType.GetProperty("InDream");
                Assert.That(count, Is.Not.Null);
                Assert.That(dying, Is.Not.Null);
                Assert.That(transitioning, Is.Not.Null);
                Assert.That(inDream, Is.Not.Null);
                count.SetValue(game, item.Level - 1);
                gameType.GetField("tutorialStage", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(game, 2);
                gameType.GetMethod("StartDream").Invoke(game, null);
                float deadline = Time.realtimeSinceStartup + 10f;
                while ((!((bool)inDream.GetValue(game)) ||
                    (bool)transitioning.GetValue(game)) && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.That((bool)inDream.GetValue(game), Is.True);
                Assert.That((bool)transitioning.GetValue(game), Is.False);
                GameObject level = GameObject.Find("Dream L" + item.Level.ToString("00"));
                GameObject dreamer = GameObject.Find("Dreamer");
                Collider2D hazard = GameObject.Find(item.Hazard)?.GetComponent<Collider2D>();
                Collider2D support = GameObject.Find(item.Support)?.GetComponent<Collider2D>();
                Collider2D shape = dreamer?.GetComponent<Collider2D>();
                Rigidbody2D body = dreamer?.GetComponent<Rigidbody2D>();
                MonoBehaviour player = FindComponent(dreamer, "Sleepwalking.SleepwalkingPlayer");
                Assert.That(level, Is.Not.Null);
                Assert.That(hazard, Is.Not.Null);
                Assert.That(support, Is.Not.Null);
                Assert.That(shape, Is.Not.Null);
                Assert.That(body, Is.Not.Null);
                Assert.That(player, Is.Not.Null);
                Assert.That(hazard.enabled, Is.True, item.Hazard);
                MethodInfo reset = player.GetType().GetMethod("ResetAt", new[] { typeof(Vector2) });
                Assert.That(reset, Is.Not.Null);
                if (item.SafeSupport != null)
                {
                    Collider2D safeSupport = GameObject.Find(item.SafeSupport)?.GetComponent<Collider2D>();
                    Assert.That(safeSupport, Is.Not.Null, item.SafeSupport);
                    Assert.That(hazard.isTrigger, Is.True, item.Hazard);
                    Assert.That(hazard.bounds.min.y, Is.EqualTo(support.bounds.max.y).Within(.02f * Unit),
                        item.Hazard + " base must meet the pit support");
                    string prefix = item.Hazard.Substring(0, item.Hazard.LastIndexOf('_') + 1);
                    List<Collider2D> row = new List<Collider2D>();
                    foreach (Collider2D triangle in hazard.transform.parent.GetComponentsInChildren<Collider2D>())
                        if (triangle.name.StartsWith(prefix, StringComparison.Ordinal)) row.Add(triangle);
                    row.Sort((left, right) => left.bounds.min.x.CompareTo(right.bounds.min.x));
                    Assert.That(row.Count, Is.EqualTo(item.ExpectedCount), prefix + " count");
                    Assert.That(row[0].bounds.min.x, Is.EqualTo(support.bounds.min.x).Within(.002f * Unit),
                        prefix + " starts after the foundation edge");
                    Assert.That(row[row.Count - 1].bounds.max.x,
                        Is.EqualTo(support.bounds.max.x).Within(.002f * Unit),
                        prefix + " ends before the foundation edge");
                    for (int q = 0; q < row.Count; q++)
                    {
                        Assert.That(row[q].isTrigger, Is.True, row[q].name);
                        Assert.That(row[q].bounds.min.y,
                            Is.EqualTo(support.bounds.max.y).Within(.002f * Unit), row[q].name);
                        if (q > 0) Assert.That(row[q].bounds.min.x,
                            Is.EqualTo(row[q - 1].bounds.max.x).Within(.002f * Unit),
                            row[q].name + " leaves a gap at the pit floor");
                    }
                    SpriteRenderer art = hazard.transform.Find("Dream Hazard Art")?.GetComponent<SpriteRenderer>();
                    Assert.That(art, Is.Not.Null, item.Hazard + " visible sprite");
                    Assert.That(art.enabled, Is.True, item.Hazard + " visible sprite");

                    Vector2 beside = new Vector2(hazard.bounds.min.x - shape.bounds.extents.x - .05f * Unit,
                        safeSupport.bounds.max.y + shape.bounds.extents.y + .01f * Unit);
                    reset.Invoke(player, new object[] { beside });
                    Physics2D.SyncTransforms();
                    for (int q = 0; q < 5; q++) yield return new WaitForFixedUpdate();
                    Assert.That(shape.IsTouching(hazard), Is.False,
                        item.Hazard + " reaches the adjacent safe support");
                    Assert.That((bool)dying.GetValue(game), Is.False,
                        item.Hazard + " killed beside the drawn row");
                    Assert.That((bool)inDream.GetValue(game), Is.True);
                }
                if (item.Hazard.EndsWith("Saw", StringComparison.Ordinal))
                {
                    Bounds blade = hazard.bounds;
                    Bounds floor = support.bounds;
                    Assert.That(blade.center.y, Is.EqualTo(floor.max.y).Within(.02f * Unit),
                        item.Hazard + " axis must meet the solid surface");
                    Assert.That(blade.min.x, Is.GreaterThanOrEqualTo(floor.min.x),
                        item.Hazard + " hidden left half must stay inside the solid");
                    Assert.That(blade.max.x, Is.LessThanOrEqualTo(floor.max.x),
                        item.Hazard + " hidden right half must stay inside the solid");
                    Assert.That(blade.min.y, Is.GreaterThan(floor.min.y),
                        item.Hazard + " hidden lower half must stay inside the solid");
                    SpriteRenderer art = hazard.transform.Find("Dream Hazard Art")?.GetComponent<SpriteRenderer>();
                    Assert.That(art, Is.Not.Null, item.Hazard + " visible sprite");
                    Assert.That(art.enabled, Is.True, item.Hazard + " visible sprite");
                    Assert.That(art.sortingOrder, Is.LessThan(0),
                        item.Hazard + " lower half must render behind tiles");
                    Assert.That(art.bounds.max.y, Is.GreaterThan(floor.max.y + .2f * Unit),
                        item.Hazard + " visible upper half");

                    Vector2 beside = new Vector2(blade.min.x - shape.bounds.extents.x - .05f * Unit,
                        floor.max.y + shape.bounds.extents.y + .01f * Unit);
                    reset.Invoke(player, new object[] { beside });
                    Physics2D.SyncTransforms();
                    for (int q = 0; q < 5; q++) yield return new WaitForFixedUpdate();
                    Assert.That(shape.IsTouching(hazard), Is.False,
                        item.Hazard + " has lethal contact outside its visible blade");
                    Assert.That((bool)dying.GetValue(game), Is.False,
                        item.Hazard + " killed beside the visible blade");
                    Assert.That((bool)transitioning.GetValue(game), Is.False,
                        item.Hazard + " safe probe entered BedExit");
                    Assert.That((bool)inDream.GetValue(game), Is.True,
                        item.Hazard + " safe probe left the dream");
                }
                Vector2 contact = new Vector2(hazard.bounds.center.x,
                    support.bounds.max.y + shape.bounds.extents.y + .01f * Unit);
                reset.Invoke(player, new object[] { contact });
                Physics2D.SyncTransforms();
                Assert.That(hazard.Distance(shape).isOverlapped, Is.True,
                    item.Hazard + " probe does not overlap the named hazard");

                bool hit = false;
                for (int q = 0; q < 25; q++)
                {
                    yield return new WaitForFixedUpdate();
                    bool dead = (bool)dying.GetValue(game);
                    trace.Add(string.Format(CultureInfo.InvariantCulture,
                        "{0},{1},{2},{3:F3},{4:F3},{5:F3},{6},{7}",
                        item.Level, item.Hazard, q, Time.fixedTime,
                        body != null ? body.position.x : float.NaN,
                        body != null ? body.position.y : float.NaN,
                        shape != null && shape.IsTouching(hazard), dead));
                    if (!dead) continue;
                    hit = true;
                    break;
                }
                string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string path = Path.Combine(root, "Docs", "QA", "grid-hazard-retry.csv");
                File.WriteAllLines(path, trace);
                Assert.That(hit, Is.True,
                    item.Hazard + " did not trigger physical death; see " + path);
                deadline = Time.realtimeSinceStartup + 5f;
                while ((bool)dying.GetValue(game) && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.That((bool)dying.GetValue(game), Is.False,
                    item.Hazard + " did not finish retry");
                Assert.That((bool)inDream.GetValue(game), Is.True);
                Assert.That((int)count.GetValue(game), Is.EqualTo(item.Level - 1));
                Assert.That(GameObject.Find("Dream L" + item.Level.ToString("00")),
                    Is.SameAs(level), "Retry loaded a different level");
                Rigidbody2D retryBody = GameObject.Find("Dreamer")?.GetComponent<Rigidbody2D>();
                Assert.That(retryBody, Is.Not.Null, item.Hazard + " missing respawn body");
                Assert.That(retryBody.position.x, Is.EqualTo(Unit).Within(.2f * Unit),
                    item.Hazard + " did not return to PlayerStart");
            }
            LogAssert.NoUnexpectedReceived();
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
