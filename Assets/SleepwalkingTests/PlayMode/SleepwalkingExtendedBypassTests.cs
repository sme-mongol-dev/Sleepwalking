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
    public sealed class SleepwalkingExtendedBypassTests
    {
        const float Unit = 32f;
        const float Margin = .15f * Unit;
        static float C(float cells) => cells * Unit;

        sealed class Attempt
        {
            public string Name;
            public int Level;
            public int Profile;
            public int Odradek = 1;
            public string StartSupport;
            public float StartX;
            public string TargetSupport;
            public float TargetMinX;
            public string Forbidden;
            public bool Jump;
            public float DashAtX;
        }

        [UnityTest]
        public IEnumerator SampledNewObstacleSkipsDoNotReachTargets()
        {
            Attempt[] attempts =
            {
                new Attempt { Name = "L03_SkipSecondSawRun", Level = 3, Profile = 1,
                    StartSupport = "ExitRun", StartX = 12.35f, TargetSupport = "UpperLanding",
                    TargetMinX = 21.425f, Forbidden = "SecondSawRun", Jump = true, DashAtX = 13f },
                new Attempt { Name = "L04_SecondLaserNoButton", Level = 4, Profile = 1,
                    StartSupport = "SecondLaserRun", StartX = 18f, TargetSupport = "SecondLaserRun",
                    TargetMinX = 22f, Forbidden = "L04_ButtonSecond" },
                new Attempt { Name = "L05_TimedA_NoB_to_TimedC", Level = 5, Profile = 2,
                    StartSupport = "L05_TimedA", StartX = 5.3f, TargetSupport = "L05_TimedC",
                    TargetMinX = 15.425f, Forbidden = "L05_TimedB", Jump = true, DashAtX = 7.2f },
                new Attempt { Name = "L05_TimedA_NoB_to_TimedC_O100", Level = 5, Profile = 2,
                    Odradek = 100, StartSupport = "L05_TimedA", StartX = 5.3f,
                    TargetSupport = "L05_TimedC", TargetMinX = 15.425f,
                    Forbidden = "L05_TimedB", Jump = true, DashAtX = 7.2f },
                new Attempt { Name = "L05_TimedB_NoC_to_ExitRun", Level = 5, Profile = 2,
                    StartSupport = "L05_TimedB", StartX = 8.3f, TargetSupport = "ExitRun",
                    TargetMinX = 19.425f, Forbidden = "L05_TimedC", Jump = true, DashAtX = 9f },
                new Attempt { Name = "L05_TimedB_NoC_to_ExitRun_O100", Level = 5, Profile = 2,
                    Odradek = 100, StartSupport = "L05_TimedB", StartX = 8.3f, TargetSupport = "ExitRun",
                    TargetMinX = 19.425f, Forbidden = "L05_TimedC", Jump = true, DashAtX = 9f },
                new Attempt { Name = "L05_SkipTimedD", Level = 5, Profile = 2,
                    StartSupport = "ExitRun", StartX = 22.3f, TargetSupport = "L05_TimedE",
                    TargetMinX = 27.375f, Forbidden = "L05_TimedD", Jump = true, DashAtX = 23f },
                new Attempt { Name = "L05_SkipTimedE", Level = 5, Profile = 2,
                    StartSupport = "L05_TimedD", StartX = 25.3f, TargetSupport = "FinalRun",
                    TargetMinX = 35.425f, Forbidden = "L05_TimedE", Jump = true, DashAtX = 26f },
                new Attempt { Name = "L05_SkipTimedE_O100", Level = 5, Profile = 2,
                    Odradek = 100, StartSupport = "L05_TimedD", StartX = 25.3f,
                    TargetSupport = "FinalRun", TargetMinX = 35.425f, Forbidden = "L05_TimedE",
                    Jump = true, DashAtX = 26f },
                new Attempt { Name = "L06_SkipVertical", Level = 6, Profile = 1,
                    StartSupport = "LiftLanding", StartX = 17.3f, TargetSupport = "HighExit",
                    TargetMinX = 21.425f, Forbidden = "L06_Vertical", Jump = true, DashAtX = 18f },
                new Attempt { Name = "L06_SkipFinalBridge", Level = 6, Profile = 1,
                    StartSupport = "HighExit", StartX = 22.35f, TargetSupport = "FinalRun",
                    TargetMinX = 38.425f, Forbidden = "L06_FinalBridge", Jump = true, DashAtX = 24f },
                new Attempt { Name = "L07_SecondLaserNoButton", Level = 7, Profile = 1,
                    StartSupport = "SecondLaserRun", StartX = 26f, TargetSupport = "SecondLaserRun",
                    TargetMinX = 30f, Forbidden = "L07_ButtonC" },
                new Attempt { Name = "L07_SecondPitNoDash", Level = 7, Profile = 1,
                    StartSupport = "SecondLaserRun", StartX = 30.3f, TargetSupport = "FinalRun",
                    TargetMinX = 37.425f, Jump = true }
            };
            List<string> trace = new List<string>
            {
                "case,step,time,x,y,vx,vy,horizontal,jump,dash,forbiddenTouch,targetTouch,stable,margin,dying,terminal"
            };
            List<string> bypasses = new List<string>();
            foreach (Attempt attempt in attempts)
            {
                bool bypass = false;
                yield return Drive(attempt, trace, result => bypass = result);
                if (bypass) bypasses.Add(attempt.Name);
            }
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string path = Path.Combine(root, "Docs", "QA", "extended-bypass.csv");
            File.WriteAllLines(path, trace);
            Assert.That(bypasses, Is.Empty,
                "Sampled new-obstacle bypass: " + string.Join(", ", bypasses) + "; see " + path);
            LogAssert.NoUnexpectedReceived();
        }

        IEnumerator Drive(Attempt attempt, List<string> trace, Action<bool> result)
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
            gameType.GetField("MovementProfile").SetValue(game, profiles.GetValue(attempt.Profile));
            gameType.GetField("odradek", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(game, attempt.Odradek);
            gameType.GetField("completedDreams", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(game, attempt.Level - 1);
            FieldInfo dying = gameType.GetField("dying", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo transitioning = gameType.GetField("transitioning", BindingFlags.Instance | BindingFlags.NonPublic);
            PropertyInfo inDream = gameType.GetProperty("InDream");
            gameType.GetMethod("StartDream").Invoke(game, null);
            float deadline = Time.realtimeSinceStartup + 10f;
            while ((!((bool)inDream.GetValue(game)) || (bool)transitioning.GetValue(game)) &&
                Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That((bool)inDream.GetValue(game), Is.True);

            GameObject dreamer = GameObject.Find("Dreamer");
            MonoBehaviour player = FindComponent(dreamer, "Sleepwalking.SleepwalkingPlayer");
            Rigidbody2D body = dreamer?.GetComponent<Rigidbody2D>();
            Collider2D shape = dreamer?.GetComponent<Collider2D>();
            Collider2D start = GameObject.Find(attempt.StartSupport)?.GetComponent<Collider2D>();
            Collider2D target = GameObject.Find(attempt.TargetSupport)?.GetComponent<Collider2D>();
            Collider2D forbidden = attempt.Forbidden == null ? null :
                GameObject.Find(attempt.Forbidden)?.GetComponent<Collider2D>();
            Assert.That(player, Is.Not.Null);
            Assert.That(body, Is.Not.Null);
            Assert.That(shape, Is.Not.Null);
            Assert.That(start, Is.Not.Null);
            Assert.That(target, Is.Not.Null);
            if (attempt.Forbidden != null) Assert.That(forbidden, Is.Not.Null);
            MethodInfo input = player.GetType().GetMethod("SetSimulatedInput");
            MethodInfo reset = player.GetType().GetMethod("ResetAt", new[] { typeof(Vector2) });
            FieldInfo dashRemaining = player.GetType().GetField("dashRemaining",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(input, Is.Not.Null);
            Assert.That(reset, Is.Not.Null);
            Assert.That(dashRemaining, Is.Not.Null);
            if (attempt.StartSupport == "L05_TimedA" ||
                attempt.StartSupport == "L05_TimedB" || attempt.StartSupport == "L05_TimedD")
            {
                MonoBehaviour mechanism = FindComponent(start.gameObject, "Sleepwalking.SleepwalkingMechanism");
                FieldInfo elapsed = mechanism.GetType().GetField("elapsed",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                for (int q = 0; q < 400; q++)
                {
                    float cycle = (float)mechanism.GetType().GetField("CycleSeconds").GetValue(mechanism);
                    float active = (float)mechanism.GetType().GetField("ActiveSeconds").GetValue(mechanism);
                    float reserve = active - Mathf.Repeat((float)elapsed.GetValue(mechanism), cycle);
                    if (start.enabled && reserve > 4.8f) break;
                    yield return new WaitForFixedUpdate();
                }
                Assert.That(start.enabled, Is.True, attempt.StartSupport + " not available for bypass sample");
            }
            Vector2 position = new Vector2(C(attempt.StartX),
                start.bounds.max.y + shape.bounds.extents.y + .015f * Unit);
            reset.Invoke(player, new object[] { position });
            Physics2D.SyncTransforms();
            for (int q = 0; q < 3; q++) yield return new WaitForFixedUpdate();
            Assert.That(Supported(shape, body, start, out float startMargin) && startMargin >= Margin,
                Is.True, attempt.Name + " did not start on safe support");

            bool touchedForbidden = false;
            bool dashRequested = false;
            bool dashActivated = false;
            bool jumpActivated = false;
            bool bypass = false;
            int stable = 0;
            for (int q = 0; q < 190; q++)
            {
                bool jump = attempt.Jump && q == 0;
                bool dash = attempt.DashAtX > 0f && !dashRequested && body.position.x >= C(attempt.DashAtX);
                dashRequested |= dash;
                input.Invoke(player, new object[] { 1f, jump, dash });
                yield return new WaitForFixedUpdate();
                if (body == null) break;
                if (jump) jumpActivated = body.linearVelocity.y > C(2f);
                if (dash) dashActivated = (float)dashRemaining.GetValue(player) > 0f;
                touchedForbidden |= forbidden != null && shape.IsTouching(forbidden);
                bool targetTouch = Supported(shape, body, target, out float margin) &&
                    body.position.x >= C(attempt.TargetMinX);
                stable = targetTouch && margin >= Margin ? stable + 1 : 0;
                bool dead = (bool)dying.GetValue(game);
                bypass = stable >= 3 && !touchedForbidden && !dead;
                string terminal = bypass ? "bypass" : dead ? "death" : q == 189 ? "timeout" : "";
                trace.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0},{1},{2:F3},{3:F3},{4:F3},{5:F3},{6:F3},{7:F2},{8},{9},{10},{11},{12},{13:F3},{14},{15}",
                    attempt.Name, q, Time.fixedTime, body.position.x, body.position.y,
                    body.linearVelocity.x, body.linearVelocity.y, 1f, jump, dash,
                    touchedForbidden, targetTouch, stable, margin, dead, terminal));
                if (bypass || dead || (bool)transitioning.GetValue(game)) break;
            }
            trace.Add("# " + attempt.Name + " jumpActivated=" + jumpActivated +
                " dashRequested=" + dashRequested + " dashActivated=" + dashActivated +
                " forbiddenTouch=" + touchedForbidden + " bypass=" + bypass);
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            File.WriteAllLines(Path.Combine(root, "Docs", "QA", "extended-bypass.csv"), trace);
            Assert.That(!attempt.Jump || jumpActivated, Is.True, attempt.Name + " jump did not activate");
            Assert.That(attempt.DashAtX <= 0f || dashActivated, Is.True,
                attempt.Name + " dash did not activate");
            result(bypass);
        }

        static bool Supported(Collider2D shape, Rigidbody2D body, Collider2D target, out float margin)
        {
            Bounds player = shape.bounds;
            Bounds surface = target.bounds;
            margin = Mathf.Min(player.min.x - surface.min.x, surface.max.x - player.max.x);
            return target.enabled && shape.IsTouching(target) &&
                Mathf.Abs(player.min.y - surface.max.y) < C(.12f) &&
                body.linearVelocity.y <= C(.15f);
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
