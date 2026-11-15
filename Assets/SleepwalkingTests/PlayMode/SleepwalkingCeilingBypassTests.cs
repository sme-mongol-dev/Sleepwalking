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
    public sealed class SleepwalkingCeilingBypassTests
    {
        const float Unit = 32f;
        const float Margin = .15f * Unit;
        static float C(float cells) => cells * Unit;

        sealed class Case
        {
            public string Name;
            public int Level;
            public string Support;
            public string Roof;
            public string Button;
            public string Hazard;
            public string Target;
            public float StartX;
            public float LaunchX;
            public float SafeMinX;
            public float SafeMaxX;
        }

        sealed class Result
        {
            public bool WallJump;
            public bool Reversed;
            public bool Dash;
            public bool Button;
            public bool Bypass;
        }

        [UnityTest]
        public IEnumerator AirTurnCannotBypassFirstLaserOrSawButtons()
        {
            Case[] cases =
            {
                new Case { Name = "L04_FirstLaser", Level = 4, Support = "LaserRun",
                    Roof = "LaserCeiling", Button = "L04_ButtonLaser", Hazard = "L04_Laser_Beam",
                    Target = "LaserRun", StartX = 4.5f, LaunchX = 4.7f,
                    SafeMinX = 8.425f, SafeMaxX = 12.575f },
                new Case { Name = "L07_FirstLaser", Level = 7, Support = "LaserRun",
                    Roof = "LaserCeiling", Button = "L07_ButtonA", Hazard = "L07_Laser_Beam",
                    Target = "LaserRun", StartX = 6.1f, LaunchX = 6.75f,
                    SafeMinX = 9.425f, SafeMaxX = 10.575f },
                new Case { Name = "L07_Saw", Level = 7, Support = "DashLanding",
                    Roof = "SawRoof", Button = "L07_ButtonB", Hazard = "L07_Saw",
                    Target = "SecondLaserRun", StartX = 17.425f, LaunchX = 17.425f,
                    SafeMinX = 22.425f, SafeMaxX = 23.425f }
            };
            List<string> trace = new List<string>
            {
                "case,step,time,x,y,vx,vy,horizontal,jump,dash,roofTouch,roofStable,targetTouch,targetStable,buttonTouch,hazardEnabled,hazardTouch,dying"
            };
            List<string> bypasses = new List<string>();
            List<string> incomplete = new List<string>();
            foreach (Case item in cases)
            {
                Result result = null;
                yield return Drive(item, trace, value => result = value);
                if (result.Bypass) bypasses.Add(item.Name);
                if (!result.WallJump || !result.Reversed || !result.Dash)
                    incomplete.Add(item.Name + " wallJump=" + result.WallJump +
                        " reversed=" + result.Reversed + " dash=" + result.Dash);
                trace.Add("# " + item.Name + " wallJump=" + result.WallJump +
                    " reversed=" + result.Reversed + " dash=" + result.Dash +
                    " button=" + result.Button + " bypass=" + result.Bypass);
            }
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string path = Path.Combine(root, "Builds", "QA", "air-control-ceiling-bypass.csv");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllLines(path, trace);
            Assert.That(bypasses, Is.Empty,
                "Active hazard bypass without button: " + string.Join(", ", bypasses) + "; see " + path);
            Assert.That(incomplete, Is.Empty,
                "Ceiling probes did not exercise wall-jump, reversal and dash: " +
                string.Join("; ", incomplete) + "; see " + path);
            LogAssert.NoUnexpectedReceived();
        }

        IEnumerator Drive(Case item, List<string> trace, Action<Result> finish)
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
            gameType.GetField("completedDreams", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(game, item.Level - 1);
            FieldInfo dying = gameType.GetField("dying", BindingFlags.Instance | BindingFlags.NonPublic);
            PropertyInfo inDream = gameType.GetProperty("InDream");
            FieldInfo transitioning = gameType.GetField("transitioning", BindingFlags.Instance | BindingFlags.NonPublic);
            gameType.GetMethod("StartDream").Invoke(game, null);
            float deadline = Time.realtimeSinceStartup + 10f;
            while ((!((bool)inDream.GetValue(game)) || (bool)transitioning.GetValue(game)) &&
                Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That((bool)inDream.GetValue(game), Is.True);

            GameObject dreamer = GameObject.Find("Dreamer");
            MonoBehaviour player = FindComponent(dreamer, "Sleepwalking.SleepwalkingPlayer");
            Rigidbody2D body = dreamer?.GetComponent<Rigidbody2D>();
            Collider2D shape = dreamer?.GetComponent<Collider2D>();
            Collider2D support = GameObject.Find(item.Support)?.GetComponent<Collider2D>();
            Collider2D roof = GameObject.Find(item.Roof)?.GetComponent<Collider2D>();
            Collider2D target = GameObject.Find(item.Target)?.GetComponent<Collider2D>();
            Collider2D button = GameObject.Find(item.Button)?.GetComponent<Collider2D>();
            Collider2D hazard = GameObject.Find(item.Hazard)?.GetComponent<Collider2D>();
            Assert.That(player, Is.Not.Null);
            Assert.That(body, Is.Not.Null);
            Assert.That(shape, Is.Not.Null);
            Assert.That(support, Is.Not.Null);
            Assert.That(roof, Is.Not.Null);
            Assert.That(target, Is.Not.Null);
            Assert.That(button, Is.Not.Null);
            Assert.That(hazard, Is.Not.Null);
            Type playerType = player.GetType();
            MethodInfo input = playerType.GetMethod("SetSimulatedInput");
            MethodInfo clear = playerType.GetMethod("ClearSimulatedInput");
            MethodInfo reset = playerType.GetMethod("ResetAt", new[] { typeof(Vector2) });
            FieldInfo wall = playerType.GetField("wall", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo grounded = playerType.GetField("grounded", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo dashRemaining = playerType.GetField("dashRemaining", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(input, Is.Not.Null);
            Assert.That(clear, Is.Not.Null);
            Assert.That(reset, Is.Not.Null);
            Assert.That(wall, Is.Not.Null);
            Assert.That(grounded, Is.Not.Null);
            Assert.That(dashRemaining, Is.Not.Null);

            Vector2 start = new Vector2(C(item.StartX),
                support.bounds.max.y + shape.bounds.extents.y + C(.015f));
            reset.Invoke(player, new object[] { start });
            Physics2D.SyncTransforms();
            for (int q = 0; q < 3; q++) yield return new WaitForFixedUpdate();
            Assert.That(Supported(shape, body, support, out float margin), Is.True,
                item.Name + " did not start on support");
            Assert.That(shape.IsTouching(button), Is.False, item.Name + " touched button at start");

            Result result = new Result();
            bool firstJump = false;
            bool wallJump = false;
            bool dashRequested = false;
            int stable = 0;
            int targetStable = 0;
            for (int q = 0; q < 145; q++)
            {
                float beforeY = body.linearVelocity.y;
                bool jump = false;
                bool dash = false;
                if (!firstJump && body.position.x >= C(item.LaunchX))
                    firstJump = jump = true;
                else if (firstJump && !wallJump && (int)wall.GetValue(player) != 0 &&
                    !(bool)grounded.GetValue(player) && shape.IsTouching(roof))
                    wallJump = jump = true;
                if (wallJump && !dashRequested && body.linearVelocity.x >= C(.6f) &&
                    shape.bounds.min.y >= roof.bounds.max.y - C(.2f))
                    dashRequested = dash = true;

                input.Invoke(player, new object[] { 1f, jump, dash });
                yield return new WaitForFixedUpdate();
                if (body == null) break;
                result.Button |= shape.IsTouching(button);
                if (wallJump && jump)
                    result.WallJump |= body.linearVelocity.y > beforeY + C(2f) &&
                        body.linearVelocity.x < 0f;
                if (result.WallJump && body.linearVelocity.x > 0f) result.Reversed = true;
                if (dash) result.Dash |= (float)dashRemaining.GetValue(player) > 0f;
                bool roofTouch = Supported(shape, body, roof, out margin);
                stable = roofTouch && margin >= Margin ? stable + 1 : 0;
                bool targetTouch = Supported(shape, body, target, out margin);
                targetStable = targetTouch && margin >= Margin &&
                    body.position.x >= C(item.SafeMinX) && body.position.x <= C(item.SafeMaxX)
                    ? targetStable + 1 : 0;
                bool dead = (bool)dying.GetValue(game);
                result.Bypass |= (stable >= 3 || targetStable >= 3) &&
                    !result.Button && hazard.enabled && !dead;
                trace.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0},{1},{2:F3},{3:F3},{4:F3},{5:F3},{6:F3},1,{7},{8},{9},{10},{11},{12},{13},{14},{15},{16}",
                    item.Name, q, Time.fixedTime, body.position.x, body.position.y,
                    body.linearVelocity.x, body.linearVelocity.y, jump, dash, roofTouch, stable,
                    targetTouch, targetStable, result.Button, hazard.enabled,
                    hazard.enabled && shape.IsTouching(hazard), dead));
                if (result.Bypass || dead || (bool)transitioning.GetValue(game)) break;
            }
            clear.Invoke(player, null);
            finish(result);
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
