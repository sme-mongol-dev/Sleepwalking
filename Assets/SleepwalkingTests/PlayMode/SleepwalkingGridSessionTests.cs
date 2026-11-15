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
    public sealed class SleepwalkingGridSessionTests
    {
        const float Unit = 32f;
        const float Margin = .15f * Unit;

        [UnityTest]
        public IEnumerator SevenDreamsReachBedsInOnePhysicalSession()
        {
            AsyncOperation loading = SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
            Assert.That(loading, Is.Not.Null);
            while (!loading.isDone) yield return null;
            yield return null;
            MonoBehaviour game = FindComponent(GameObject.Find("Sleepwalking Game"),
                "Sleepwalking.SleepwalkingGame");
            Assert.That(game, Is.Not.Null);
            Type gameType = game.GetType();
            Array profiles = (Array)gameType.GetField("MovementProfiles").GetValue(game);
            gameType.GetField("MovementProfile").SetValue(game, profiles.GetValue(1));
            gameType.GetField("odradek", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(game, 1);
            FieldInfo count = gameType.GetField("completedDreams",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo dying = gameType.GetField("dying",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo transitioning = gameType.GetField("transitioning",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo roomField = gameType.GetField("room",
                BindingFlags.Instance | BindingFlags.NonPublic);
            PropertyInfo inDream = gameType.GetProperty("InDream");
            MethodInfo startDream = gameType.GetMethod("StartDream");
            Assert.That(count, Is.Not.Null);
            Assert.That(dying, Is.Not.Null);
            Assert.That(transitioning, Is.Not.Null);
            Assert.That(roomField, Is.Not.Null);
            Assert.That(inDream, Is.Not.Null);
            Assert.That(startDream, Is.Not.Null);
            count.SetValue(game, 0);
            gameType.GetField("tutorialStage", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(game, 2);
            GameObject room = roomField.GetValue(game) as GameObject;
            Assert.That(room, Is.Not.Null);

            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            List<string> trace = new List<string>
            {
                "level,step,time,x,y,horizontal,jump,dash,bedTouch,dying,transitioning"
            };
            for (int level = 1; level <= 7; level++)
            {
                Assert.That(room.activeSelf, Is.True, "Room before L" + level);
                Assert.That((int)count.GetValue(game), Is.EqualTo(level - 1));
                startDream.Invoke(game, null);
                float deadline = Time.realtimeSinceStartup + 10f;
                while ((!((bool)inDream.GetValue(game)) ||
                    (bool)transitioning.GetValue(game)) && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.That((bool)inDream.GetValue(game), Is.True, "L" + level + " did not start");
                Assert.That((bool)transitioning.GetValue(game), Is.False);
                Assert.That(room.activeSelf, Is.False);
                Assert.That(GameObject.Find("Dream L" + level.ToString("00")), Is.Not.Null);
                GameObject dreamer = GameObject.Find("Dreamer");
                MonoBehaviour player = FindComponent(dreamer, "Sleepwalking.SleepwalkingPlayer");
                Rigidbody2D body = dreamer?.GetComponent<Rigidbody2D>();
                Collider2D shape = dreamer?.GetComponent<Collider2D>();
                Collider2D bed = GameObject.Find("BedExit")?.GetComponent<Collider2D>();
                Assert.That(player, Is.Not.Null);
                Assert.That(body, Is.Not.Null);
                Assert.That(shape, Is.Not.Null);
                Assert.That(bed, Is.Not.Null);
                MethodInfo input = player.GetType().GetMethod("SetSimulatedInput");
                MethodInfo clear = player.GetType().GetMethod("ClearSimulatedInput");
                FieldInfo dashRemaining = player.GetType().GetField("dashRemaining",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(input, Is.Not.Null);
                Assert.That(clear, Is.Not.Null);
                Assert.That(dashRemaining, Is.Not.Null);
                for (int q = 0; q < 5; q++) yield return new WaitForFixedUpdate();

                string sourceName = "extended-L" + level.ToString("00") + "-responsive.csv";
                string source = Path.Combine(root, "Docs", "QA", sourceName);
                Assert.That(File.Exists(source), Is.True,
                    "Run the grid route test before the full session: " + source);
                string[] lines = File.ReadAllLines(source);
                Assert.That(lines.Length, Is.GreaterThan(2), sourceName);
                string[] first = lines[1].Split(',');
                Assert.That(float.Parse(first[5], CultureInfo.InvariantCulture),
                    Is.GreaterThan(20f), "Old non-grid input trace: " + sourceName);
                string[] requiredNames = level == 1 ?
                    new[] { "JumpLanding", "ExitRun", "RiseA", "GapShelf", "FinalRun" } :
                    level == 2 ? new[] { "SpikeLanding", "ExitRun", "SpikeBridge", "HighRecovery", "FinalRun" } :
                    level == 3 ? new[] { "SawRun", "ExitRun", "SecondSawRun", "UpperLanding", "FinalRun" } :
                    level == 4 ? new[] { "LaserRun", "SecondLaserRun" } :
                    level == 5 ? new[] { "L05_TimedA", "L05_TimedB", "L05_TimedC", "ExitRun",
                        "L05_TimedD", "L05_TimedE", "FinalRun" } :
                    level == 6 ? new[] { "L06_Horizontal", "LiftLanding", "L06_Vertical", "HighExit",
                        "L06_FinalBridge", "FinalRun" } :
                    new[] { "WallGrabLedge", "LaserRun", "DashLanding", "SecondLaserRun", "FinalRun" };
                Collider2D[] required = new Collider2D[requiredNames.Length];
                int[] stable = new int[required.Length];
                bool[] visited = new bool[required.Length];
                for (int q = 0; q < required.Length; q++)
                {
                    required[q] = GameObject.Find(requiredNames[q])?.GetComponent<Collider2D>();
                    Assert.That(required[q], Is.Not.Null, requiredNames[q]);
                }
                Collider2D buttonA = GameObject.Find(level == 4 ? "L04_ButtonLaser" :
                    level == 7 ? "L07_ButtonA" : "")?.GetComponent<Collider2D>();
                Collider2D buttonB = level == 7 ?
                    GameObject.Find("L07_ButtonB")?.GetComponent<Collider2D>() : null;
                Collider2D buttonSecond = GameObject.Find(level == 4 ? "L04_ButtonSecond" :
                    level == 7 ? "L07_ButtonC" : "")?.GetComponent<Collider2D>();
                bool touchedA = false;
                bool touchedB = false;
                bool touchedSecond = false;
                int activeDashes = 0;
                bool bedTouched = false;
                bool dead = false;
                int step = 0;
                foreach (string line in lines)
                {
                    if (line.Length == 0 || line[0] == '#') continue;
                    string[] row = line.Split(',');
                    if (row.Length < 12 || row[0] == "level") continue;
                    float horizontal = float.Parse(row[9], CultureInfo.InvariantCulture);
                    bool jump = bool.Parse(row[10]);
                    bool dash = bool.Parse(row[11]);
                    input.Invoke(player, new object[] { horizontal, jump, dash });
                    yield return new WaitForFixedUpdate();
                    if (body == null) { dead = true; break; }
                    Record(trace, level, step++, body, horizontal, jump, dash, shape,
                        bed, game, dying, transitioning);
                    UpdateRequired(shape, body, required, stable, visited);
                    touchedA |= buttonA != null && shape.IsTouching(buttonA);
                    touchedB |= buttonB != null && shape.IsTouching(buttonB);
                    touchedSecond |= buttonSecond != null && shape.IsTouching(buttonSecond);
                    if (dash && (float)dashRemaining.GetValue(player) > 0f) activeDashes++;
                    bedTouched |= shape.IsTouching(bed);
                    dead |= (bool)dying.GetValue(game);
                    if (dead || (bool)transitioning.GetValue(game) ||
                        !(bool)inDream.GetValue(game)) break;
                }
                for (int q = 0; q < 120 && !dead &&
                    (bool)inDream.GetValue(game) && !(bool)transitioning.GetValue(game); q++)
                {
                    float horizontal = body.position.x < bed.bounds.center.x ? 1f : 0f;
                    input.Invoke(player, new object[] { horizontal, false, false });
                    yield return new WaitForFixedUpdate();
                    if (body == null) { dead = true; break; }
                    Record(trace, level, step++, body, horizontal, false, false, shape,
                        bed, game, dying, transitioning);
                    UpdateRequired(shape, body, required, stable, visited);
                    touchedA |= buttonA != null && shape.IsTouching(buttonA);
                    touchedB |= buttonB != null && shape.IsTouching(buttonB);
                    touchedSecond |= buttonSecond != null && shape.IsTouching(buttonSecond);
                    bedTouched |= shape.IsTouching(bed);
                    dead |= (bool)dying.GetValue(game);
                }
                clear.Invoke(player, null);
                string tracePath = Path.Combine(root, "Docs", "QA", "grid-full-session.csv");
                File.WriteAllLines(tracePath, trace);
                Assert.That(dead, Is.False, "Death in L" + level + "; see " + tracePath);
                Assert.That(bedTouched, Is.True, "No physical BedExit contact in L" + level);
                for (int q = 0; q < visited.Length; q++)
                    Assert.That(visited[q], Is.True,
                        "No stable " + requiredNames[q] + " in L" + level);
                if (level == 4 || level == 7)
                {
                    Assert.That(touchedA, Is.True, "Button A was not touched in L" + level);
                    Assert.That(touchedSecond, Is.True, "Second laser button was not touched in L" + level);
                }
                if (level == 7)
                {
                    Assert.That(touchedB, Is.True, "Button B was not touched in L07");
                    Assert.That(activeDashes, Is.GreaterThanOrEqualTo(2),
                        "Both L07 dash commands did not activate");
                }
                deadline = Time.realtimeSinceStartup + 5f;
                while (((bool)inDream.GetValue(game) ||
                    (bool)transitioning.GetValue(game)) && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.That((bool)inDream.GetValue(game), Is.False,
                    "L" + level + " did not finish through BedExit");
                Assert.That((int)count.GetValue(game), Is.EqualTo(level));
                Assert.That(room.activeSelf, Is.True);
                Assert.That(GameObject.Find("Dream L" + level.ToString("00")), Is.Null);
            }
            LogAssert.NoUnexpectedReceived();
        }

        static void UpdateRequired(Collider2D shape, Rigidbody2D body,
            Collider2D[] targets, int[] stable, bool[] visited)
        {
            for (int q = 0; q < targets.Length; q++)
            {
                Collider2D target = targets[q];
                Bounds player = shape.bounds;
                Bounds surface = target.bounds;
                float margin = Mathf.Min(player.min.x - surface.min.x,
                    surface.max.x - player.max.x);
                bool supported = target.enabled && shape.IsTouching(target) &&
                    Mathf.Abs(player.min.y - surface.max.y) < .12f * Unit &&
                    body.linearVelocity.y <= .15f * Unit;
                stable[q] = supported ? stable[q] + 1 : 0;
                visited[q] |= stable[q] >= 3 && margin >= Margin;
            }
        }

        static void Record(List<string> trace, int level, int step,
            Rigidbody2D body, float horizontal, bool jump, bool dash,
            Collider2D shape, Collider2D bed, MonoBehaviour game,
            FieldInfo dying, FieldInfo transitioning)
        {
            trace.Add(string.Format(CultureInfo.InvariantCulture,
                "{0},{1},{2:F3},{3:F3},{4:F3},{5:F2},{6},{7},{8},{9},{10}",
                level, step, Time.fixedTime, body.position.x, body.position.y,
                horizontal, jump, dash, shape.IsTouching(bed),
                (bool)dying.GetValue(game), (bool)transitioning.GetValue(game)));
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
