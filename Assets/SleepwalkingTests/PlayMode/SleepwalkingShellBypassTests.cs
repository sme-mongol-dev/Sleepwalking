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
    public sealed class SleepwalkingShellBypassTests
    {
        const float Unit = 32f;
        const float Margin = .15f * Unit;
        static float C(float cells) => cells * Unit;

        sealed class Case
        {
            public string Name;
            public int Level;
            public int Profile;
            public string Target;
            public string Forbidden;
            public float AimX;
            public float SafeMin;
            public float SafeMax;
            public float DashX;
            public bool DirectNoWallJump;
        }

        [UnityTest]
        public IEnumerator SampledShellWallRoutesPreserveRequiredMechanisms()
        {
            Case[] cases =
            {
                new Case { Name = "L04_Laser_NoButton", Level = 4, Profile = 1,
                    Target = "LaserRun", Forbidden = "L04_ButtonLaser", AimX = C(9f),
                    SafeMin = C(8.325f), SafeMax = C(10f), DashX = C(4.5f) },
                new Case { Name = "L05_TimedB_NoA", Level = 5, Profile = 2,
                    Target = "L05_TimedB", Forbidden = "L05_TimedA", AimX = C(8f),
                    SafeMin = C(7.375f), SafeMax = C(8.625f), DashX = C(4f) },
                new Case { Name = "L05_TimedC_NoAB", Level = 5, Profile = 2,
                    Target = "L05_TimedC", Forbidden = "L05_TimedA", AimX = C(11f),
                    SafeMin = C(10.375f), SafeMax = C(11.625f), DashX = C(4f) },
                new Case { Name = "L06_Lift_NoHorizontal", Level = 6, Profile = 1,
                    Target = "LiftLanding", Forbidden = "L06_Horizontal", AimX = C(17f),
                    SafeMin = C(16.425f), SafeMax = C(17.575f), DashX = C(4.5f) },
                new Case { Name = "L07_Ledge_NoAuthoredWall", Level = 7, Profile = 1,
                    Target = "WallGrabLedge", AimX = C(5f),
                    SafeMin = C(4.425f), SafeMax = C(5.575f) },
                new Case { Name = "L07_Ledge_NoAuthoredWall_Dash", Level = 7, Profile = 1,
                    Target = "WallGrabLedge", AimX = C(5f),
                    SafeMin = C(4.425f), SafeMax = C(5.575f), DashX = C(2.5f) }
            };
            List<string> bypasses = new List<string>();
            foreach (Case item in cases)
            {
                bool bypass = false;
                yield return Drive(item, result => bypass = result);
                if (item.Level == 7)
                    Assert.That(bypass, Is.True, item.Name + " wall-jump route should reach the ledge");
                else if (bypass) bypasses.Add(item.Name);
            }
            Assert.That(bypasses, Is.Empty,
                "CaveShell wall launch bypassed an intended mechanism: " +
                string.Join(", ", bypasses) + "; see shell-bypass-*.csv");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator L07DirectJumpAndDashCannotLandWithoutWallJump()
        {
            for (int profile = 0; profile < 3; profile++)
            {
                for (int dash = 0; dash < 2; dash++)
                {
                    Case item = new Case
                    {
                        Name = "L07_Direct_NoWall_" + profile + "_" + dash,
                        Level = 7,
                        Profile = profile,
                        Target = "WallGrabLedge",
                        AimX = C(5f),
                        SafeMin = C(4.425f),
                        SafeMax = C(5.575f),
                        DashX = dash == 0 ? 0f : C(3.3f),
                        DirectNoWallJump = true
                    };
                    bool reached = false;
                    yield return Drive(item, result => reached = result);
                    Assert.That(reached, Is.False,
                        item.Name + " landed on ledge without wall-jump; see shell-bypass-*.csv");
                }
            }
            LogAssert.NoUnexpectedReceived();
        }

        IEnumerator Drive(Case item, Action<bool> result)
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
            gameType.GetField("MovementProfile").SetValue(game, profiles.GetValue(item.Profile));
            gameType.GetField("completedDreams", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(game, item.Level - 1);
            FieldInfo dying = gameType.GetField("dying", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo transitioning = gameType.GetField("transitioning", BindingFlags.Instance | BindingFlags.NonPublic);
            PropertyInfo inDream = gameType.GetProperty("InDream");
            gameType.GetMethod("StartDream").Invoke(game, null);
            float deadline = Time.realtimeSinceStartup + 10f;
            while ((!((bool)inDream.GetValue(game)) || (bool)transitioning.GetValue(game)) &&
                Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That((bool)inDream.GetValue(game), Is.True);
            Assert.That((bool)transitioning.GetValue(game), Is.False);

            GameObject dreamer = GameObject.Find("Dreamer");
            MonoBehaviour player = FindComponent(dreamer, "Sleepwalking.SleepwalkingPlayer");
            Rigidbody2D body = dreamer?.GetComponent<Rigidbody2D>();
            Collider2D shape = dreamer?.GetComponent<Collider2D>();
            Collider2D shellWall = GameObject.Find("Cave_LeftWall")?.GetComponent<Collider2D>();
            Collider2D target = GameObject.Find(item.Target)?.GetComponent<Collider2D>();
            Collider2D forbidden = item.Forbidden == null ? null :
                GameObject.Find(item.Forbidden)?.GetComponent<Collider2D>();
            Assert.That(player, Is.Not.Null);
            Assert.That(body, Is.Not.Null);
            Assert.That(shape, Is.Not.Null);
            Assert.That(shellWall, Is.Not.Null);
            Assert.That(target, Is.Not.Null);
            if (item.Forbidden != null) Assert.That(forbidden, Is.Not.Null);
            Type playerType = player.GetType();
            MethodInfo input = playerType.GetMethod("SetSimulatedInput");
            MethodInfo clear = playerType.GetMethod("ClearSimulatedInput");
            FieldInfo wall = playerType.GetField("wall", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo grounded = playerType.GetField("grounded", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo dashRemaining = playerType.GetField("dashRemaining", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(input, Is.Not.Null);
            Assert.That(clear, Is.Not.Null);
            Assert.That(wall, Is.Not.Null);
            Assert.That(grounded, Is.Not.Null);
            Assert.That(dashRemaining, Is.Not.Null);

            List<string> trace = new List<string>
            {
                "step,time,x,y,vx,vy,horizontal,jump,dash,shellTouch,authoredSideTouch,forbiddenTouch,targetTouch,stable,margin,dying,terminal"
            };
            bool firstJump = false;
            bool wallJump = false;
            bool wallJumpActivated = false;
            bool dash = false;
            bool dashActivated = false;
            bool touchedForbidden = false;
            bool touchedAuthoredSide = false;
            bool bypass = false;
            float maxX = body.position.x;
            float maxY = body.position.y;
            int stable = 0;
            for (int q = 0; q < 190; q++)
            {
                Vector2 position = body.position;
                float beforeY = body.linearVelocity.y;
                bool jumpNow = false;
                bool dashNow = false;
                float horizontal;
                if (item.DirectNoWallJump)
                {
                    horizontal = position.x < item.AimX - C(.25f) ? 1f : -1f;
                    if (!firstJump && position.x >= C(2.6f))
                        firstJump = jumpNow = true;
                    if (item.DashX > 0f && firstJump && !dash && position.x >= item.DashX)
                        dash = dashNow = true;
                }
                else if (!wallJump)
                {
                    horizontal = -1f;
                    if (!firstJump && position.x <= C(.9f))
                        firstJump = jumpNow = true;
                    else if (firstJump && (int)wall.GetValue(player) == -1 &&
                        !(bool)grounded.GetValue(player) && beforeY <= .1f)
                        wallJump = jumpNow = true;
                }
                else
                {
                    horizontal = position.x < item.AimX - C(.5f) ? 1f :
                        position.x > item.AimX + C(.1f) ? -1f : 0f;
                    if (item.DashX > 0f && !dash && position.x >= item.DashX)
                        dash = dashNow = true;
                }
                input.Invoke(player, new object[] { horizontal, jumpNow, dashNow });
                yield return new WaitForFixedUpdate();
                if (body == null) break;
                position = body.position;
                maxX = Mathf.Max(maxX, position.x);
                maxY = Mathf.Max(maxY, position.y);
                if (wallJump && jumpNow)
                    wallJumpActivated = body.linearVelocity.y > beforeY + C(2f) &&
                        body.linearVelocity.y > C(5f);
                if (dashNow)
                    dashActivated = (float)dashRemaining.GetValue(player) > 0f;
                if (forbidden != null) touchedForbidden |= shape.IsTouching(forbidden);
                bool targetTouch = shape.IsTouching(target);
                if (item.Level == 7 && targetTouch &&
                    shape.bounds.min.y < target.bounds.max.y - C(.2f))
                    touchedAuthoredSide = true;
                float margin = Mathf.Min(shape.bounds.min.x - target.bounds.min.x,
                    target.bounds.max.x - shape.bounds.max.x);
                bool supported = target.enabled && targetTouch &&
                    Mathf.Abs(shape.bounds.min.y - target.bounds.max.y) < C(.12f) &&
                    body.linearVelocity.y <= C(.15f) &&
                    position.x >= item.SafeMin && position.x <= item.SafeMax && margin >= Margin;
                stable = supported ? stable + 1 : 0;
                bool died = (bool)dying.GetValue(game);
                bool exit = (bool)transitioning.GetValue(game) || !(bool)inDream.GetValue(game);
                bypass = (item.DirectNoWallJump ? firstJump : wallJumpActivated) &&
                    (!dash || dashActivated) &&
                    !touchedForbidden && !touchedAuthoredSide && !died &&
                    (stable >= 3 || item.Level == 4 && exit);
                string terminal = bypass ? "bypass" : died ? "death" :
                    exit ? "exit" : q == 189 ? "timeout" : "";
                trace.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0},{1:F3},{2:F3},{3:F3},{4:F3},{5:F3},{6:F2},{7},{8},{9},{10},{11},{12},{13},{14:F3},{15},{16}",
                    q, Time.fixedTime, position.x, position.y, body.linearVelocity.x,
                    body.linearVelocity.y, horizontal, jumpNow, dashNow,
                    shape.IsTouching(shellWall), touchedAuthoredSide, touchedForbidden,
                    targetTouch, stable, margin, died, terminal));
                if (bypass || died || exit) break;
            }
            clear.Invoke(player, null);
            string path = Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..")),
                "Docs", "QA", "shell-bypass-" + item.Name + ".csv");
            trace.Add("# wallJumpActivated=" + wallJumpActivated + " dashActivated=" + dashActivated +
                " maxX=" + maxX.ToString("F3", CultureInfo.InvariantCulture) +
                " maxY=" + maxY.ToString("F3", CultureInfo.InvariantCulture));
            File.WriteAllLines(path, trace);
            if (!item.DirectNoWallJump)
                Assert.That(wallJumpActivated, Is.True,
                    item.Name + " never activated the CaveShell wall jump; see " + path);
            else
                Assert.That(firstJump, Is.True,
                    item.Name + " never jumped from Start; see " + path);
            if (item.DashX > 0f) Assert.That(dashActivated, Is.True,
                item.Name + " never activated dash; see " + path);
            result(bypass);
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
