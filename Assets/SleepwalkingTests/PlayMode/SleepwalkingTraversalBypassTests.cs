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
    public sealed class SleepwalkingTraversalBypassTests
    {
        sealed class Case
        {
            public string Name;
            public int Level;
            public int ProfileIndex;
            public float StartX;
            public string StartSupportName;
            public float LaunchX;
            public string TargetName;
            public string[] Forbidden;
            public float[] BrakeX;
            public float DashX;
            public int DashAgainAfterSteps;
        }

        bool bypassFound;
        bool targetContactFound;
        bool exitBeforeForbidden;
        float landingX;
        float landingMargin;

        [UnityTest]
        public IEnumerator SampledPlainJumpBypassAuditL05AndL06()
        {
            Case[] cases =
            {
                new Case
                {
                    Name = "L05_Aggressive_Start_to_ExitRun", Level = 5, ProfileIndex = 2,
                    StartX = 1.8f, LaunchX = 2.7f, TargetName = "ExitRun",
                    Forbidden = new[] { "L05_TimedA", "L05_TimedB", "L05_TimedC" },
                    BrakeX = new[] { 100f }
                },
                new Case
                {
                    Name = "L06_Responsive_Start_to_LiftLanding", Level = 6, ProfileIndex = 1,
                    StartX = 1.8f, LaunchX = 2.5f, TargetName = "LiftLanding",
                    Forbidden = new[] { "L06_Horizontal" },
                    BrakeX = new[] { 100f, 13.0f, 13.6f, 14.0f }
                }
            };

            List<string> defects = new List<string>();
            foreach (Case item in cases)
            {
                yield return DriveCase(item);
                if (bypassFound)
                    defects.Add(item.Name + " landed at X=" + landingX.ToString("F3") +
                        " with edge margin=" + landingMargin.ToString("F3"));
                else if (targetContactFound)
                    defects.Add(item.Name + " touched " + item.TargetName + " without stable landing");
            }
            Assert.That(defects, Is.Empty,
                "Plain jump bypasses a required mechanism in a sampled route; see traversal-bypass-*.csv. " +
                string.Join("; ", defects));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SampledOneChargeDashBypassAuditL05AndL06()
        {
            Case[] cases =
            {
                new Case
                {
                    Name = "L05_Aggressive_Dash_SkipA_to_TimedB", Level = 5, ProfileIndex = 2,
                    StartX = 1.8f, LaunchX = 2.7f, TargetName = "L05_TimedB",
                    Forbidden = new[] { "L05_TimedA" }, BrakeX = new[] { 100f },
                    DashX = 3.8f, DashAgainAfterSteps = 21
                },
                new Case
                {
                    Name = "L06_Responsive_Dash_SkipHorizontal_to_LiftLanding", Level = 6,
                    ProfileIndex = 1, StartX = 1.8f, LaunchX = 2.75f,
                    TargetName = "LiftLanding", Forbidden = new[] { "L06_Horizontal" },
                    BrakeX = new[] { 100f }, DashX = 5f, DashAgainAfterSteps = 21
                },
                new Case
                {
                    Name = "L06_Responsive_Dash_SkipVertical_to_HighExit", Level = 6,
                    ProfileIndex = 1, StartX = 15f, StartSupportName = "LiftLanding",
                    LaunchX = 15.4f, TargetName = "HighExit",
                    Forbidden = new[] { "L06_Vertical" }, BrakeX = new[] { 100f },
                    DashX = 16f, DashAgainAfterSteps = 21
                }
            };
            List<string> defects = new List<string>();
            foreach (Case item in cases)
            {
                yield return DriveCase(item);
                if (bypassFound)
                    defects.Add(item.Name + " landed at X=" + landingX.ToString("F3"));
                else if (targetContactFound)
                    defects.Add(item.Name + " touched " + item.TargetName + " before required support");
                else if (exitBeforeForbidden)
                    defects.Add(item.Name + " exited before required support");
            }
            Assert.That(defects, Is.Empty,
                "Sampled dash bypass reached a target before its required support; see traversal-bypass-*.csv. " +
                string.Join("; ", defects));
            LogAssert.NoUnexpectedReceived();
        }

        IEnumerator DriveCase(Case item)
        {
            bypassFound = false;
            targetContactFound = false;
            exitBeforeForbidden = false;
            landingX = float.NaN;
            landingMargin = float.NaN;
            AsyncOperation loading = SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
            Assert.That(loading, Is.Not.Null);
            while (!loading.isDone)
                yield return null;
            yield return null;

            MonoBehaviour game = FindComponent(GameObject.Find("Sleepwalking Game"), "Sleepwalking.SleepwalkingGame");
            Assert.That(game, Is.Not.Null);
            Type gameType = game.GetType();
            Array profiles = (Array)gameType.GetField("MovementProfiles").GetValue(game);
            Assert.That(profiles.Length, Is.GreaterThan(item.ProfileIndex));
            ScriptableObject profile = (ScriptableObject)profiles.GetValue(item.ProfileIndex);
            Assert.That(profile, Is.Not.Null);
            gameType.GetField("MovementProfile").SetValue(game, profile);
            gameType.GetField("completedDreams", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(game, item.Level - 1);
            MethodInfo startDream = gameType.GetMethod("StartDream");
            PropertyInfo inDream = gameType.GetProperty("InDream");
            FieldInfo transitioning = gameType.GetField("transitioning", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo dying = gameType.GetField("dying", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(startDream, Is.Not.Null);
            Assert.That(inDream, Is.Not.Null);
            Assert.That(transitioning, Is.Not.Null);
            Assert.That(dying, Is.Not.Null);
            startDream.Invoke(game, null);
            float deadline = Time.realtimeSinceStartup + 10f;
            while ((!((bool)inDream.GetValue(game)) || (bool)transitioning.GetValue(game)) &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That((bool)inDream.GetValue(game), Is.True);
            Assert.That((bool)transitioning.GetValue(game), Is.False);
            Assert.That(GameObject.Find("Dream L" + item.Level.ToString("00")), Is.Not.Null);

            GameObject dreamer = GameObject.Find("Dreamer");
            Assert.That(dreamer, Is.Not.Null);
            MonoBehaviour player = FindComponent(dreamer, "Sleepwalking.SleepwalkingPlayer");
            Rigidbody2D body = dreamer.GetComponent<Rigidbody2D>();
            Collider2D playerShape = dreamer.GetComponent<Collider2D>();
            Collider2D startShape = GameObject.Find(item.StartSupportName ?? "Start")?.GetComponent<Collider2D>();
            Collider2D target = GameObject.Find(item.TargetName)?.GetComponent<Collider2D>();
            Assert.That(player, Is.Not.Null);
            Assert.That(body, Is.Not.Null);
            Assert.That(playerShape, Is.Not.Null);
            Assert.That(startShape, Is.Not.Null);
            Assert.That(target, Is.Not.Null);
            Collider2D[] forbidden = new Collider2D[item.Forbidden.Length];
            for (int q = 0; q < forbidden.Length; q++)
            {
                forbidden[q] = GameObject.Find(item.Forbidden[q])?.GetComponent<Collider2D>();
                Assert.That(forbidden[q], Is.Not.Null, "Missing forbidden support " + item.Forbidden[q]);
            }
            Type playerType = player.GetType();
            FieldInfo dashRemaining = playerType.GetField("dashRemaining",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo dashCharges = playerType.GetField("dashCharges",
                BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo reset = playerType.GetMethod("ResetAt", new[] { typeof(Vector2) });
            MethodInfo input = playerType.GetMethod("SetSimulatedInput");
            MethodInfo clear = playerType.GetMethod("ClearSimulatedInput");
            Assert.That(reset, Is.Not.Null);
            Assert.That(input, Is.Not.Null);
            Assert.That(clear, Is.Not.Null);
            Assert.That(dashRemaining, Is.Not.Null);
            Assert.That(dashCharges, Is.Not.Null);

            List<string> trace = new List<string> { "profile,brakeX,step,time,x,y,vx,vy,horizontal,jump,dash,targetContact,forbiddenContact,stable,edgeMargin,terminal" };
            foreach (float brakeX in item.BrakeX)
            {
                clear.Invoke(player, null);
                float startY = startShape.bounds.max.y + playerShape.bounds.extents.y + .06f;
                reset.Invoke(player, new object[] { new Vector2(item.StartX, startY) });
                Physics2D.SyncTransforms();
                for (int q = 0; q < 5; q++)
                    yield return new WaitForFixedUpdate();
                Assert.That(playerShape.IsTouching(startShape), Is.True,
                    "QA start placement did not establish support on Start.");

                bool jumped = false;
                bool dashIssued = false;
                bool firstDashActivated = false;
                bool secondDashIssued = false;
                int firstDashStep = -1;
                bool touchedForbidden = false;
                int stable = 0;
                int sampledSteps = 0;
                for (int q = 0; q < 150; q++)
                {
                    bool jumpNow = !jumped && body.position.x >= item.LaunchX;
                    if (jumpNow) jumped = true;
                    bool dashNow = false;
                    if (item.DashX > 0f && !dashIssued && body.position.x >= item.DashX)
                    {
                        dashNow = dashIssued = true;
                        firstDashStep = q;
                    }
                    else if (item.DashAgainAfterSteps > 0 && dashIssued && !secondDashIssued &&
                             q - firstDashStep >= item.DashAgainAfterSteps)
                    {
                        dashNow = secondDashIssued = true;
                    }
                    float horizontal = body.position.x < brakeX ? 1f : 0f;
                    input.Invoke(player, new object[] { horizontal, jumpNow, dashNow });
                    yield return new WaitForFixedUpdate();
                    if (dashIssued && q == firstDashStep)
                        firstDashActivated = (float)dashRemaining.GetValue(player) > 0f &&
                            (int)dashCharges.GetValue(player) == 0;

                    foreach (Collider2D shape in forbidden)
                        touchedForbidden |= playerShape.IsTouching(shape);
                    Bounds playerBounds = playerShape.bounds;
                    Bounds targetBounds = target.bounds;
                    bool contact = playerShape.IsTouching(target);
                    targetContactFound |= contact && !touchedForbidden;
                    bool supported = contact && Mathf.Abs(playerBounds.min.y - targetBounds.max.y) < .12f &&
                        body.linearVelocity.y <= .15f;
                    stable = supported ? stable + 1 : 0;
                    float margin = Mathf.Min(playerBounds.min.x - targetBounds.min.x,
                        targetBounds.max.x - playerBounds.max.x);
                    bool died = (bool)dying.GetValue(game);
                    bool stableBypass = stable >= 3 && margin >= .15f && !touchedForbidden && !died;
                    bool exited = (bool)transitioning.GetValue(game);
                    exitBeforeForbidden |= exited && !touchedForbidden;
                    string terminal = stableBypass ? "bypass" : died ? "death" :
                        exited ? "exit" : q == 149 ? "timeout" : "";
                    trace.Add(string.Format(CultureInfo.InvariantCulture,
                        "{0},{1:F2},{2},{3:F3},{4:F3},{5:F3},{6:F3},{7:F3},{8:F1},{9},{10},{11},{12},{13},{14:F3},{15}",
                        profile.name, brakeX, q, Time.fixedTime, body.position.x, body.position.y,
                        body.linearVelocity.x, body.linearVelocity.y, horizontal, jumpNow, dashNow,
                        contact, touchedForbidden, stable, margin, terminal));
                    sampledSteps++;
                    if (stableBypass)
                    {
                        bypassFound = true;
                        landingX = body.position.x;
                        landingMargin = margin;
                        break;
                    }
                    if (died || exited)
                        break;
                }
                Assert.That(jumped, Is.True, item.Name + " did not execute its plain jump");
                if (item.DashX > 0f)
                {
                    Assert.That(dashIssued, Is.True, item.Name + " did not reach its first dash point");
                    Assert.That(firstDashActivated, Is.True, item.Name + " did not activate its first dash");
                    trace.Add("# secondDashIssued=" + secondDashIssued);
                }
                Assert.That(sampledSteps, Is.GreaterThanOrEqualTo(20),
                    item.Name + " ended before a meaningful airborne attempt");
                clear.Invoke(player, null);
                if ((bool)dying.GetValue(game))
                {
                    deadline = Time.realtimeSinceStartup + 5f;
                    while ((bool)dying.GetValue(game) && Time.realtimeSinceStartup < deadline)
                        yield return null;
                    Assert.That((bool)dying.GetValue(game), Is.False,
                        item.Name + " did not finish its death/respawn transition");
                }
                if (bypassFound) break;
            }
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            File.WriteAllLines(Path.Combine(projectRoot, "Docs", "QA", "traversal-bypass-" + item.Name + ".csv"), trace);
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
