using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Sleepwalking.Tests.PlayMode
{
    public sealed class MovementTrajectoryTests
    {
        sealed class Route
        {
            public string Name;
            public int Level;
            public float StartX;
            public float StartTop;
            public float LaunchX;
            public float TargetLeft;
            public float TargetRight;
            public float TargetTop;
            public string TargetName;
            public float SafeMin;
            public float SafeMax;
            public float DashX = -1f;
            public float DashAgainDelay = -1f;
            public bool WallJump;
            public bool RunOnly;
        }

        readonly Route[] routes =
        {
            new Route { Name = "L01 gap A", Level = 1, StartX = 2.1f, StartTop = 0, LaunchX = 3.25f, TargetLeft = 5.2f, TargetRight = 8.4f, TargetTop = 0, TargetName = "JumpLanding", SafeMin = 5.625f, SafeMax = 7.975f },
            new Route { Name = "L01 gap B", Level = 1, StartX = 6.4f, StartTop = 0, LaunchX = 7.9f, TargetLeft = 9.5f, TargetRight = 12f, TargetTop = .2f, TargetName = "ExitRun", SafeMin = 9.925f, SafeMax = 11.575f },
            new Route { Name = "L02 spike gap", Level = 2, StartX = 2.0f, StartTop = 0, LaunchX = 3.0f, TargetLeft = 4.7f, TargetRight = 8.2f, TargetTop = 0, TargetName = "SpikeLanding", SafeMin = 5.125f, SafeMax = 5.725f },
            new Route { Name = "L02 spike hop", Level = 2, StartX = 5.4f, StartTop = 0, LaunchX = 5.45f, TargetLeft = 4.7f, TargetRight = 8.2f, TargetTop = 0, TargetName = "SpikeLanding", SafeMin = 7.475f, SafeMax = 7.775f },
            new Route { Name = "L02 exit gap", Level = 2, StartX = 7.65f, StartTop = 0, LaunchX = 7.9f, TargetLeft = 9.3f, TargetRight = 12f, TargetTop = .2f, TargetName = "ExitRun", SafeMin = 9.725f, SafeMax = 11.575f },
            new Route { Name = "L03 saw gap", Level = 3, StartX = 2.0f, StartTop = 0, LaunchX = 3.1f, TargetLeft = 4.8f, TargetRight = 8.4f, TargetTop = 0, TargetName = "SawRun", SafeMin = 5.225f, SafeMax = 5.65f },
            new Route { Name = "L03 saw hop", Level = 3, StartX = 5.4f, StartTop = 0, LaunchX = 5.45f, TargetLeft = 4.8f, TargetRight = 8.4f, TargetTop = 0, TargetName = "SawRun", SafeMin = 7.25f, SafeMax = 7.975f },
            new Route { Name = "L03 exit gap", Level = 3, StartX = 7.65f, StartTop = 0, LaunchX = 8.05f, TargetLeft = 9.5f, TargetRight = 12f, TargetTop = .2f, TargetName = "ExitRun", SafeMin = 9.925f, SafeMax = 11.575f },
            new Route { Name = "L04 button laser", Level = 4, StartX = 1f, StartTop = 0, LaunchX = 0, TargetLeft = 9.7f, TargetRight = 10.6f, TargetTop = 0, TargetName = "LaserRun", RunOnly = true },
            new Route { Name = "L05 timed A", Level = 5, StartX = 1.8f, StartTop = 0, LaunchX = 2.1f, TargetLeft = 3.35f, TargetRight = 4.65f, TargetTop = 1.3f, TargetName = "L05_TimedA", SafeMin = 3.725f, SafeMax = 4.275f },
            new Route { Name = "L05 timed B", Level = 5, StartX = 3.9f, StartTop = 1.3f, LaunchX = 3.95f, TargetLeft = 5f, TargetRight = 6.3f, TargetTop = 2.6f, TargetName = "L05_TimedB", SafeMin = 5.375f, SafeMax = 5.925f },
            new Route { Name = "L05 timed C", Level = 5, StartX = 5.5f, StartTop = 2.6f, LaunchX = 5.55f, TargetLeft = 6.65f, TargetRight = 7.95f, TargetTop = 3.9f, TargetName = "L05_TimedC", SafeMin = 7.025f, SafeMax = 7.575f },
            new Route { Name = "L05 exit", Level = 5, StartX = 7.1f, StartTop = 3.9f, LaunchX = 7.15f, TargetLeft = 8.5f, TargetRight = 12f, TargetTop = 5.2f, TargetName = "ExitRun", SafeMin = 8.925f, SafeMax = 11.575f },
            new Route { Name = "L05 plain bypass", Level = 5, StartX = 1.8f, StartTop = 0, LaunchX = 2.7f, TargetLeft = 8.5f, TargetRight = 12f, TargetTop = 5.2f, TargetName = "ExitRun", SafeMin = 8.925f, SafeMax = 11.575f },
            new Route { Name = "L05 dash bypass B", Level = 5, StartX = 1.8f, StartTop = 0, LaunchX = 2.7f, TargetLeft = 5f, TargetRight = 6.3f, TargetTop = 2.6f, TargetName = "L05_TimedB", SafeMin = 5.375f, SafeMax = 5.925f, DashX = 3.8f },
            new Route { Name = "L05 dash bypass exit", Level = 5, StartX = 1.8f, StartTop = 0, LaunchX = 2.7f, TargetLeft = 8.5f, TargetRight = 12f, TargetTop = 5.2f, TargetName = "ExitRun", SafeMin = 8.925f, SafeMax = 11.575f, DashX = 4.0f, DashAgainDelay = .42f },
            new Route { Name = "L06 moving A", Level = 6, StartX = 1.8f, StartTop = -.9f, LaunchX = 2.75f, TargetLeft = 3.6f, TargetRight = 5.2f, TargetTop = .4f, TargetName = "L06_Horizontal", SafeMin = 3.975f, SafeMax = 4.825f },
            new Route { Name = "L06 plain bypass", Level = 6, StartX = 1.8f, StartTop = -.9f, LaunchX = 2.75f, TargetLeft = 14.5f, TargetRight = 15.9f, TargetTop = 2.35f, TargetName = "LiftLanding", SafeMin = 14.925f, SafeMax = 15.475f },
            new Route { Name = "L06 dash bypass", Level = 6, StartX = 1.8f, StartTop = -.9f, LaunchX = 2.75f, TargetLeft = 14.5f, TargetRight = 15.9f, TargetTop = 2.35f, TargetName = "LiftLanding", SafeMin = 14.925f, SafeMax = 15.475f, DashX = 5.0f, DashAgainDelay = .42f },
            new Route { Name = "L06 lift bypass", Level = 6, StartX = 14.9f, StartTop = 2.35f, LaunchX = 15.4f, TargetLeft = 17f, TargetRight = 18.5f, TargetTop = 5.05f, TargetName = "HighExit", SafeMin = 17.425f, SafeMax = 18.075f },
            new Route { Name = "L06 lift dash bypass", Level = 6, StartX = 14.9f, StartTop = 2.35f, LaunchX = 15.4f, TargetLeft = 17f, TargetRight = 18.5f, TargetTop = 5.05f, TargetName = "HighExit", SafeMin = 17.425f, SafeMax = 18.075f, DashX = 16f },
            new Route { Name = "L07 wall", Level = 7, StartX = 1.6f, StartTop = 0, LaunchX = 2.8f, TargetLeft = 3.7f, TargetRight = 5.3f, TargetTop = 2.45f, TargetName = "WallGrabLedge", SafeMin = 4.125f, SafeMax = 4.875f, WallJump = true },
            new Route { Name = "L07 wall plain", Level = 7, StartX = 1.6f, StartTop = 0, LaunchX = 2.8f, TargetLeft = 3.7f, TargetRight = 5.3f, TargetTop = 2.45f, TargetName = "WallGrabLedge", SafeMin = 4.125f, SafeMax = 4.875f },
            new Route { Name = "L07 pit plain", Level = 7, StartX = 8.45f, StartTop = 2.45f, LaunchX = 9.25f, TargetLeft = 18.5f, TargetRight = 21.1f, TargetTop = 2.6f, TargetName = "DashLanding", SafeMin = 18.925f, SafeMax = 20.675f },
            new Route { Name = "L07 pit dash", Level = 7, StartX = 8.45f, StartTop = 2.45f, LaunchX = 9.25f, TargetLeft = 18.5f, TargetRight = 21.1f, TargetTop = 2.6f, TargetName = "DashLanding", SafeMin = 18.925f, SafeMax = 20.675f, DashX = 12.5f }
        };

        MonoBehaviour game;
        Type gameType;
        FieldInfo completedDreams;
        FieldInfo transitioning;
        FieldInfo dying;
        PropertyInfo inDream;
        MethodInfo startDream;
        MethodInfo returnToRoom;
        readonly List<string> measurements = new List<string>();

        [UnityTest]
        public IEnumerator MeasureThreeProfilesOnAuthoredRoutes()
        {
            yield return LoadGame();
            Array profiles = (Array)gameType.GetField("MovementProfiles").GetValue(game);
            Assert.That(profiles.Length, Is.EqualTo(3));
            for (int q = 0; q < profiles.Length; q++)
            {
                ScriptableObject profile = (ScriptableObject)profiles.GetValue(q);
                Assert.That(profile, Is.Not.Null);
                foreach (Route route in routes)
                {
                    yield return OpenLevel(route.Level, profile);
                    yield return Drive(route, profile.name);
                    returnToRoom.Invoke(game, null);
                    yield return null;
                }
                yield return OpenLevel(7, profile);
                yield return DriveFullL07(profile.name);
                returnToRoom.Invoke(game, null);
                yield return null;
                yield return OpenLevel(6, profile);
                yield return DriveFullL06(profile.name);
                returnToRoom.Invoke(game, null);
                yield return null;
            }
            foreach (string measurement in measurements) Debug.Log("SW_MOVE " + measurement);
            Assert.That(measurements.Count, Is.EqualTo(profiles.Length * (routes.Length + 2)));
        }

        [UnityTest]
        public IEnumerator MeasureRevisedL05L06()
        {
            yield return LoadGame();
            Array profiles = (Array)gameType.GetField("MovementProfiles").GetValue(game);
            foreach (ScriptableObject profile in profiles)
            {
                foreach (Route route in routes)
                {
                    if (route.Level != 5 && route.Level != 6) continue;
                    yield return OpenLevel(route.Level, profile);
                    yield return Drive(route, profile.name);
                    returnToRoom.Invoke(game, null);
                    yield return null;
                }
                yield return OpenLevel(6, profile);
                yield return DriveFullL06(profile.name);
                returnToRoom.Invoke(game, null);
                yield return null;
            }
            foreach (string measurement in measurements) Debug.Log("SW_MOVE " + measurement);
            Assert.That(measurements.Count, Is.EqualTo(profiles.Length * 13));
        }

        [UnityTest]
        public IEnumerator MeasureContinuousL05L06()
        {
            yield return LoadGame();
            Array profiles = (Array)gameType.GetField("MovementProfiles").GetValue(game);
            for (int q = 0; q < 2; q++)
            {
                ScriptableObject profile = (ScriptableObject)profiles.GetValue(q);
                yield return OpenLevel(5, profile);
                yield return DriveFullL05(profile.name);
                returnToRoom.Invoke(game, null);
                yield return null;
                yield return OpenLevel(6, profile);
                yield return DriveFullL06(profile.name);
                returnToRoom.Invoke(game, null);
                yield return null;
            }
            foreach (string measurement in measurements) Debug.Log("SW_MOVE " + measurement);
            Assert.That(measurements.Count, Is.EqualTo(4));
        }

        [UnityTest]
        public IEnumerator MeasureCoreAndBypassAfterControllerFix()
        {
            yield return LoadGame();
            Array profiles = (Array)gameType.GetField("MovementProfiles").GetValue(game);
            foreach (ScriptableObject profile in profiles)
            {
                foreach (Route route in routes)
                {
                    bool core = route.Level == 1 || route.Level == 7 || route.Name.Contains("bypass");
                    if (!core) continue;
                    yield return OpenLevel(route.Level, profile);
                    yield return Drive(route, profile.name);
                    returnToRoom.Invoke(game, null);
                    yield return null;
                }
                yield return OpenLevel(7, profile);
                yield return DriveFullL07(profile.name);
                returnToRoom.Invoke(game, null);
                yield return null;
            }
            foreach (string measurement in measurements) Debug.Log("SW_MOVE " + measurement);
            Assert.That(measurements.Count, Is.GreaterThan(0));
        }

        IEnumerator LoadGame()
        {
            AsyncOperation loading = SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
            Assert.That(loading, Is.Not.Null);
            while (!loading.isDone) yield return null;
            yield return null;
            GameObject root = GameObject.Find("Sleepwalking Game");
            Assert.That(root, Is.Not.Null);
            foreach (MonoBehaviour component in root.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().FullName == "Sleepwalking.SleepwalkingGame") game = component;
            Assert.That(game, Is.Not.Null);
            gameType = game.GetType();
            completedDreams = gameType.GetField("completedDreams", BindingFlags.Instance | BindingFlags.NonPublic);
            transitioning = gameType.GetField("transitioning", BindingFlags.Instance | BindingFlags.NonPublic);
            dying = gameType.GetField("dying", BindingFlags.Instance | BindingFlags.NonPublic);
            inDream = gameType.GetProperty("InDream");
            startDream = gameType.GetMethod("StartDream");
            returnToRoom = gameType.GetMethod("ReturnToRoom");
        }

        IEnumerator OpenLevel(int level, ScriptableObject profile)
        {
            gameType.GetField("MovementProfile").SetValue(game, profile);
            completedDreams.SetValue(game, level - 1);
            startDream.Invoke(game, null);
            float deadline = Time.realtimeSinceStartup + 10f;
            while ((!((bool)inDream.GetValue(game)) || (bool)transitioning.GetValue(game)) && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That((bool)inDream.GetValue(game), Is.True, "Could not open L" + level);
        }

        IEnumerator Drive(Route route, string profileName)
        {
            GameObject dreamer = GameObject.Find("Dreamer");
            Assert.That(dreamer, Is.Not.Null);
            MonoBehaviour player = null;
            foreach (MonoBehaviour component in dreamer.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().FullName == "Sleepwalking.SleepwalkingPlayer") player = component;
            Assert.That(player, Is.Not.Null);
            Type playerType = player.GetType();
            MethodInfo reset = playerType.GetMethod("ResetAt", new[] { typeof(Vector2) });
            MethodInfo input = playerType.GetMethod("SetSimulatedInput");
            MethodInfo clear = playerType.GetMethod("ClearSimulatedInput");
            Rigidbody2D body = dreamer.GetComponent<Rigidbody2D>();
            Collider2D playerShape = dreamer.GetComponent<Collider2D>();
            GameObject targetObject = GameObject.Find(route.TargetName);
            Assert.That(targetObject, Is.Not.Null, "Missing target " + route.TargetName);
            Collider2D target = targetObject.GetComponent<Collider2D>();
            Assert.That(target, Is.Not.Null);
            if (route.Level == 7 && route.Name.Contains("pit"))
            {
                foreach (MonoBehaviour mechanism in GameObject.Find("Dream L07").GetComponentsInChildren<MonoBehaviour>())
                {
                    if (mechanism == null || mechanism.GetType().Name != "SleepwalkingMechanism") continue;
                    string id = mechanism.GetType().GetField("FeatureId").GetValue(mechanism).ToString();
                    if (id.StartsWith("L07_Laser") || id == "L07_Saw")
                        mechanism.GetType().GetMethod("DisableFor").Invoke(mechanism, new object[] { 8f });
                }
            }
            reset.Invoke(player, new object[] { new Vector2(route.StartX, route.StartTop + .56f) });
            Physics2D.SyncTransforms();
            float maxFeet = route.StartTop;
            float maxX = route.StartX;
            float landingX = float.NaN;
            bool jumped = route.RunOnly;
            bool wallJumped = false;
            bool dashed = false;
            bool dashedAgain = false;
            float dashAt = float.NaN;
            bool dead = false;
            int stableFrames = 0;
            float elapsed = 0f;
            string events = "";
            float deadline = Time.realtimeSinceStartup + 4f;
            while (Time.realtimeSinceStartup < deadline)
            {
                Vector2 position = body.position;
                bool jumpNow = !jumped && position.x >= route.LaunchX;
                if (jumpNow)
                {
                    jumped = true;
                    events += " jump@" + position.x.ToString("F2");
                }
                if (route.WallJump && jumped && !wallJumped && position.x >= 3.35f && position.y < 3.1f && body.linearVelocity.y <= .1f)
                {
                    jumpNow = true;
                    wallJumped = true;
                    events += " wall@" + position.x.ToString("F2");
                }
                bool dashNow = route.DashX >= 0f && !dashed && position.x >= route.DashX;
                if (dashNow)
                {
                    dashed = true;
                    dashAt = elapsed;
                    events += " dash@" + position.x.ToString("F2");
                }
                bool dashAgainNow = route.DashAgainDelay > 0f && dashed && !dashedAgain &&
                    elapsed - dashAt >= route.DashAgainDelay;
                if (dashAgainNow)
                {
                    dashedAgain = true;
                    events += " secondDashAttempt@" + position.x.ToString("F2");
                }
                float desiredX = route.RunOnly ? 10.55f : route.Name == "L06 moving A" ? target.bounds.center.x :
                    route.SafeMin > 0f ? (route.SafeMin + route.SafeMax) / 2f :
                    Mathf.Min(route.TargetRight - .5f, route.TargetLeft + 1f);
                bool bypass = route.Name.Contains("bypass") || route.Name.Contains("pit");
                float horizontal = bypass ? 1f : position.x < desiredX - .8f ? 1f :
                    position.x > desiredX + .1f ? -1f : 0f;
                if (route.Name == "L06 moving A") horizontal = position.x < target.bounds.center.x ? 1f : 0f;
                input.Invoke(player, new object[] { horizontal, jumpNow, dashNow || dashAgainNow });
                yield return null;
                elapsed += Time.deltaTime;
                if (body == null || player == null || target == null) break;
                position = body.position;
                float feet = position.y - .5f;
                maxFeet = Mathf.Max(maxFeet, feet);
                maxX = Mathf.Max(maxX, position.x);
                if ((bool)dying.GetValue(game)) { dead = true; break; }
                bool safeX = route.Name == "L06 moving A" ?
                    position.x >= target.bounds.min.x + .375f && position.x <= target.bounds.max.x - .375f :
                    route.SafeMin <= 0f || position.x >= route.SafeMin && position.x <= route.SafeMax;
                bool contact = playerShape.IsTouching(target) && Mathf.Abs(feet - target.bounds.max.y) < .13f &&
                    body.linearVelocity.y <= .1f;
                bool routeReached = !route.RunOnly || position.x >= 10.1f;
                if (jumped && contact && safeX && routeReached)
                {
                    stableFrames++;
                    if (stableFrames >= 3)
                    {
                        landingX = position.x;
                        events += " land@" + landingX.ToString("F2");
                        break;
                    }
                }
                else stableFrames = 0;
                if (position.y < Mathf.Min(route.StartTop, route.TargetTop) - 2f ||
                    position.x > Mathf.Max(route.TargetRight, target.bounds.max.x) + 1f) break;
            }
            if (player != null) clear.Invoke(player, null);
            float safeMin = route.Name == "L06 moving A" ? target.bounds.min.x + .375f : route.SafeMin;
            float safeMax = route.Name == "L06 moving A" ? target.bounds.max.x - .375f : route.SafeMax;
            float margin = float.IsNaN(landingX) ? float.NaN : Mathf.Min(landingX - safeMin, safeMax - landingX);
            measurements.Add(profileName + " | " + route.Name + " | success=" + (!float.IsNaN(landingX)) +
                " | maxFeet=" + maxFeet.ToString("F2") + " | maxX=" + maxX.ToString("F2") +
                " | landingX=" + landingX.ToString("F2") + " | margin=" + margin.ToString("F2") +
                " | died=" + dead + " | elapsed=" + elapsed.ToString("F2") +
                " | wallJump=" + wallJumped + " | dash=" + dashed +
                " | secondDashAttempt=" + dashedAgain + " | events=" + events);
        }

        IEnumerator DriveFullL07(string profileName)
        {
            GameObject dreamer = GameObject.Find("Dreamer");
            Assert.That(dreamer, Is.Not.Null);
            MonoBehaviour player = null;
            foreach (MonoBehaviour component in dreamer.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().FullName == "Sleepwalking.SleepwalkingPlayer") player = component;
            Assert.That(player, Is.Not.Null);
            Type type = player.GetType();
            MethodInfo input = type.GetMethod("SetSimulatedInput");
            MethodInfo clear = type.GetMethod("ClearSimulatedInput");
            Rigidbody2D body = dreamer.GetComponent<Rigidbody2D>();
            Collider2D shape = dreamer.GetComponent<Collider2D>();
            Collider2D ledge = GameObject.Find("WallGrabLedge").GetComponent<Collider2D>();
            Collider2D landing = GameObject.Find("DashLanding").GetComponent<Collider2D>();
            Collider2D laser = GameObject.Find("L07_Laser_Beam").GetComponent<Collider2D>();
            Collider2D saw = GameObject.Find("L07_Saw").GetComponent<Collider2D>();
            MonoBehaviour laserMechanism = laser.GetComponent<MonoBehaviour>();
            MonoBehaviour sawMechanism = saw.GetComponent<MonoBehaviour>();
            foreach (MonoBehaviour component in laser.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().Name == "SleepwalkingMechanism") laserMechanism = component;
            foreach (MonoBehaviour component in saw.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().Name == "SleepwalkingMechanism") sawMechanism = component;
            FieldInfo laserUntil = laserMechanism.GetType().GetField("disabledUntil", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo sawUntil = sawMechanism.GetType().GetField("disabledUntil", BindingFlags.Instance | BindingFlags.NonPublic);
            bool jumped = false;
            bool wallJumped = false;
            bool pitJumped = false;
            bool dashed = false;
            bool dead = false;
            bool laserDisabled = false;
            bool sawDisabled = false;
            float laserReserve = float.NaN;
            float sawReserve = float.NaN;
            bool bedReached = false;
            int ledgeFrames = 0;
            int landingFrames = 0;
            int stage = 0;
            float maxX = body.position.x;
            float landingX = float.NaN;
            float elapsed = 0f;
            string events = "";
            float deadline = Time.realtimeSinceStartup + 6f;
            while (Time.realtimeSinceStartup < deadline)
            {
                Vector2 position = body.position;
                bool jumpNow = false;
                bool dashNow = false;
                float horizontal = 1f;
                if (stage == 0)
                {
                    if (!jumped && position.x >= 2.8f)
                    {
                        jumped = jumpNow = true;
                        events += " jump@" + position.x.ToString("F2");
                    }
                    if (jumped && !wallJumped && position.x >= 3.35f && position.y < 3.1f && body.linearVelocity.y <= .1f)
                    {
                        wallJumped = jumpNow = true;
                        events += " wall@" + position.x.ToString("F2");
                    }
                    horizontal = position.x < 4.1f ? 1f : position.x > 4.7f ? -1f : 0f;
                }
                else if (stage == 1)
                {
                    if (!pitJumped && position.x >= 9.25f)
                    {
                        pitJumped = jumpNow = true;
                        events += " pitJump@" + position.x.ToString("F2");
                    }
                    if (pitJumped && !dashed && position.x >= 12.5f)
                    {
                        dashed = dashNow = true;
                        events += " dash@" + position.x.ToString("F2");
                    }
                    if (position.x >= 18.5f) horizontal = position.x < 19.25f ? 1f : position.x > 19.7f ? -1f : 0f;
                }
                input.Invoke(player, new object[] { horizontal, jumpNow, dashNow });
                yield return null;
                elapsed += Time.deltaTime;
                if (body == null || player == null) break;
                position = body.position;
                maxX = Mathf.Max(maxX, position.x);
                if (!laser.enabled) laserDisabled = true;
                if (!saw.enabled) sawDisabled = true;
                if (float.IsNaN(laserReserve) && position.x >= 8.225f)
                    laserReserve = (float)laserUntil.GetValue(laserMechanism) - Time.time;
                if ((bool)dying.GetValue(game)) { dead = true; break; }
                if (stage == 0)
                {
                    if (shape.IsTouching(ledge) && position.x >= 4.125f && position.x <= 4.875f &&
                        Mathf.Abs(position.y - .5f - ledge.bounds.max.y) < .13f) ledgeFrames++;
                    else ledgeFrames = 0;
                    if (ledgeFrames >= 3)
                    {
                        stage = 1;
                        events += " ledge@" + position.x.ToString("F2");
                    }
                }
                else
                {
                    if (shape.IsTouching(landing) && position.x >= 18.925f && position.x <= 20.675f &&
                        Mathf.Abs(position.y - .5f - landing.bounds.max.y) < .13f) landingFrames++;
                    else landingFrames = 0;
                    if (landingFrames >= 3)
                    {
                        landingX = position.x;
                        sawReserve = (float)sawUntil.GetValue(sawMechanism) - Time.time;
                        events += " land@" + position.x.ToString("F2");
                        break;
                    }
                }
                if (position.y < -2f || position.x > 21.5f || !(bool)inDream.GetValue(game)) break;
            }
            bedReached = (bool)transitioning.GetValue(game);
            if (!float.IsNaN(landingX) && !bedReached)
            {
                float bedDeadline = Time.realtimeSinceStartup + 1.5f;
                while (Time.realtimeSinceStartup < bedDeadline && !(bool)transitioning.GetValue(game) &&
                    !(bool)dying.GetValue(game))
                {
                    input.Invoke(player, new object[] { 1f, false, false });
                    yield return null;
                }
                bedReached = (bool)transitioning.GetValue(game) || !(bool)inDream.GetValue(game);
            }
            if (player != null) clear.Invoke(player, null);
            measurements.Add(profileName + " | L07 full route | success=" + (!float.IsNaN(landingX) &&
                laserDisabled && sawDisabled && laserReserve >= 1f && sawReserve >= 1f &&
                bedReached && !dead) + " | maxX=" + maxX.ToString("F2") +
                " | landingX=" + landingX.ToString("F2") + " | died=" + dead +
                " | elapsed=" + elapsed.ToString("F2") + " | buttonA=" + laserDisabled +
                " | buttonB=" + sawDisabled + " | laserReserve=" + laserReserve.ToString("F2") +
                " | sawReserve=" + sawReserve.ToString("F2") + " | bed=" + bedReached +
                " | events=" + events);
        }

        IEnumerator DriveFullL06(string profileName)
        {
            GameObject dreamer = GameObject.Find("Dreamer");
            Assert.That(dreamer, Is.Not.Null);
            MonoBehaviour player = null;
            foreach (MonoBehaviour component in dreamer.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().FullName == "Sleepwalking.SleepwalkingPlayer") player = component;
            Assert.That(player, Is.Not.Null);
            Type type = player.GetType();
            MethodInfo reset = type.GetMethod("ResetAt", new[] { typeof(Vector2) });
            MethodInfo input = type.GetMethod("SetSimulatedInput");
            MethodInfo clear = type.GetMethod("ClearSimulatedInput");
            Rigidbody2D body = dreamer.GetComponent<Rigidbody2D>();
            Collider2D shape = dreamer.GetComponent<Collider2D>();
            Collider2D horizontalMover = GameObject.Find("L06_Horizontal").GetComponent<Collider2D>();
            Collider2D landing = GameObject.Find("LiftLanding").GetComponent<Collider2D>();
            Collider2D verticalMover = GameObject.Find("L06_Vertical").GetComponent<Collider2D>();
            Collider2D highExit = GameObject.Find("HighExit").GetComponent<Collider2D>();
            reset.Invoke(player, new object[] { new Vector2(1.8f, -.34f) });
            Physics2D.SyncTransforms();
            int stage = 0;
            int stable = 0;
            bool jumped = false;
            bool dead = false;
            float maxX = body.position.x;
            float exitX = float.NaN;
            float maxFeet = -.9f;
            float moverGap = float.PositiveInfinity;
            int moverTouchFrames = 0;
            float elapsed = 0f;
            string events = "";
            float deadline = Time.realtimeSinceStartup + 12f;
            while (Time.realtimeSinceStartup < deadline)
            {
                Vector2 position = body.position;
                bool jumpNow = false;
                float horizontal = 0f;
                if (stage == 0)
                {
                    if (!jumped && position.x >= 2.75f)
                    {
                        jumped = jumpNow = true;
                        events += " startJump@" + position.x.ToString("F2");
                    }
                    horizontal = position.x < horizontalMover.bounds.center.x ? 1f : 0f;
                }
                else if (stage == 1)
                    horizontal = position.x < horizontalMover.bounds.center.x + .4f ? 1f : 0f;
                else if (stage == 2)
                    horizontal = position.x < 14.6f ? 1f : position.x > 15.2f ? -1f : 0f;
                else if (stage == 3)
                    horizontal = 0f;
                else if (stage == 4)
                    horizontal = Mathf.Clamp((verticalMover.bounds.center.x - position.x) * 2.4f -
                        body.linearVelocity.x * .55f, -1f, 1f);
                else if (stage == 5)
                    horizontal = position.x < 16.35f ? 1f : 0f;
                else if (stage == 6)
                    horizontal = position.x < 17.5f ? 1f : position.x > 18.0f ? -1f : 0f;

                if (stage == 1 && horizontalMover.bounds.center.x >= 12.6f)
                {
                    stage = 2;
                    jumpNow = true;
                    horizontal = 1f;
                    events += " dismount@" + position.x.ToString("F2");
                }
                if (stage == 3 && verticalMover.bounds.max.y <= 2.8f)
                {
                    stage = 4;
                    jumpNow = true;
                    horizontal = 1f;
                    events += " board@" + position.x.ToString("F2");
                }
                if (stage == 5 && verticalMover.bounds.max.y >= 3.7f)
                {
                    stage = 6;
                    jumpNow = true;
                    horizontal = 1f;
                    events += " exitJump@" + position.x.ToString("F2");
                }
                input.Invoke(player, new object[] { horizontal, jumpNow, false });
                yield return null;
                elapsed += Time.deltaTime;
                if (body == null || player == null) break;
                position = body.position;
                maxX = Mathf.Max(maxX, position.x);
                maxFeet = Mathf.Max(maxFeet, position.y - .5f);
                if (stage == 4 && position.x >= verticalMover.bounds.min.x &&
                    position.x <= verticalMover.bounds.max.x)
                {
                    moverGap = Mathf.Min(moverGap, Mathf.Abs(position.y - .5f - verticalMover.bounds.max.y));
                    if (shape.IsTouching(verticalMover)) moverTouchFrames++;
                }
                if ((bool)dying.GetValue(game)) { dead = true; break; }
                Collider2D target = stage == 0 ? horizontalMover : stage == 2 ? landing :
                    stage == 4 ? verticalMover : stage == 6 ? highExit : null;
                bool safeX = stage == 0 || stage == 4 ? target != null &&
                    position.x >= target.bounds.min.x + .375f && position.x <= target.bounds.max.x - .375f :
                    stage == 2 ? position.x >= 14.925f && position.x <= 15.475f :
                    stage == 6 && position.x >= 17.425f && position.x <= 18.075f;
                if (target != null && shape.IsTouching(target) && safeX &&
                    Mathf.Abs(position.y - .5f - target.bounds.max.y) < .13f &&
                    (stage == 4 || body.linearVelocity.y <= .1f))
                    stable++;
                else stable = 0;
                if (stable >= 3)
                {
                    if (stage == 0) { stage = 1; events += " mover@" + position.x.ToString("F2"); }
                    else if (stage == 2) { stage = 3; events += " landing@" + position.x.ToString("F2"); }
                    else if (stage == 4) { stage = 5; events += " lift@" + position.x.ToString("F2"); }
                    else if (stage == 6) { exitX = position.x; events += " exit@" + position.x.ToString("F2"); break; }
                    stable = 0;
                }
                if (position.y < -2f || position.x > 19.5f || !(bool)inDream.GetValue(game)) break;
            }
            bool bedReached = (bool)transitioning.GetValue(game);
            if (!float.IsNaN(exitX) && !bedReached && player != null)
            {
                float bedDeadline = Time.realtimeSinceStartup + 1.5f;
                while (Time.realtimeSinceStartup < bedDeadline && !(bool)transitioning.GetValue(game) &&
                    !(bool)dying.GetValue(game))
                {
                    input.Invoke(player, new object[] { 1f, false, false });
                    yield return null;
                }
                bedReached = (bool)transitioning.GetValue(game) || !(bool)inDream.GetValue(game);
            }
            if (player != null) clear.Invoke(player, null);
            measurements.Add(profileName + " | L06 full route | success=" + (!float.IsNaN(exitX) && bedReached && !dead) +
                " | stage=" + stage + " | maxX=" + maxX.ToString("F2") +
                " | maxFeet=" + maxFeet.ToString("F2") + " | moverGap=" + moverGap.ToString("F2") +
                " | moverTouchFrames=" + moverTouchFrames +
                " | exitX=" + exitX.ToString("F2") + " | bed=" + bedReached + " | died=" + dead +
                " | elapsed=" + elapsed.ToString("F2") + " | events=" + events);
        }

        IEnumerator DriveFullL05(string profileName)
        {
            GameObject dreamer = GameObject.Find("Dreamer");
            Assert.That(dreamer, Is.Not.Null);
            MonoBehaviour player = null;
            foreach (MonoBehaviour component in dreamer.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().FullName == "Sleepwalking.SleepwalkingPlayer") player = component;
            Assert.That(player, Is.Not.Null);
            Type type = player.GetType();
            MethodInfo reset = type.GetMethod("ResetAt", new[] { typeof(Vector2) });
            MethodInfo input = type.GetMethod("SetSimulatedInput");
            MethodInfo clear = type.GetMethod("ClearSimulatedInput");
            Rigidbody2D body = dreamer.GetComponent<Rigidbody2D>();
            Collider2D shape = dreamer.GetComponent<Collider2D>();
            string[] names = { "L05_TimedA", "L05_TimedB", "L05_TimedC", "ExitRun" };
            float[] safeMin = { 3.725f, 5.375f, 7.025f, 8.925f };
            float[] safeMax = { 4.275f, 5.925f, 7.575f, 11.575f };
            float[] desired = { 4f, 5.65f, 7.3f, 9.4f };
            Collider2D[] targets = new Collider2D[4];
            for (int q = 0; q < targets.Length; q++)
            {
                targets[q] = GameObject.Find(names[q]).GetComponent<Collider2D>();
                Assert.That(targets[q], Is.Not.Null);
            }
            foreach (MonoBehaviour mechanism in GameObject.Find("Dream L05").GetComponentsInChildren<MonoBehaviour>())
                if (mechanism != null && mechanism.GetType().Name == "SleepwalkingMechanism" &&
                    mechanism.GetType().GetField("Kind").GetValue(mechanism).ToString() == "TimedPlatform")
                    mechanism.GetType().GetMethod("ResetMechanism").Invoke(mechanism, null);
            reset.Invoke(player, new object[] { new Vector2(1.8f, .56f) });
            Physics2D.SyncTransforms();
            int stage = 0;
            int stable = 0;
            bool stageJumped = false;
            bool dead = false;
            float cReserve = float.NaN;
            float exitX = float.NaN;
            float elapsed = 0f;
            string events = "";
            float deadline = Time.realtimeSinceStartup + 5f;
            while (Time.realtimeSinceStartup < deadline && stage < 4)
            {
                Vector2 position = body.position;
                bool jumpNow = !stageJumped && (stage > 0 || position.x >= 2.1f);
                if (jumpNow)
                {
                    stageJumped = true;
                    events += " jump" + stage + "@" + position.x.ToString("F2");
                }
                float horizontal = position.x < desired[stage] - .8f ? 1f :
                    position.x > desired[stage] + .1f ? -1f : 0f;
                input.Invoke(player, new object[] { horizontal, jumpNow, false });
                yield return null;
                elapsed += Time.deltaTime;
                if (body == null || player == null) break;
                position = body.position;
                if ((bool)dying.GetValue(game)) { dead = true; break; }
                Collider2D target = targets[stage];
                float feet = position.y - .5f;
                bool landed = stageJumped && target.enabled && shape.IsTouching(target) &&
                    position.x >= safeMin[stage] && position.x <= safeMax[stage] &&
                    Mathf.Abs(feet - target.bounds.max.y) < .13f && body.linearVelocity.y <= .1f;
                stable = landed ? stable + 1 : 0;
                if (stable >= 3)
                {
                    events += " land" + stage + "@" + position.x.ToString("F2");
                    if (stage == 2)
                    {
                        MonoBehaviour mechanism = null;
                        foreach (MonoBehaviour component in target.GetComponents<MonoBehaviour>())
                            if (component != null && component.GetType().Name == "SleepwalkingMechanism") mechanism = component;
                        Type mechanismType = mechanism.GetType();
                        float cycleElapsed = (float)mechanismType.GetField("elapsed", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(mechanism);
                        float active = (float)mechanismType.GetField("ActiveSeconds").GetValue(mechanism);
                        float cycle = (float)mechanismType.GetField("CycleSeconds").GetValue(mechanism);
                        cReserve = active - Mathf.Repeat(cycleElapsed, cycle);
                    }
                    if (stage == 3) exitX = position.x;
                    stage++;
                    stageJumped = false;
                    stable = 0;
                }
                if (position.y < -2f || position.x > 12.5f || !(bool)inDream.GetValue(game)) break;
            }
            bool bedReached = (bool)transitioning.GetValue(game);
            if (!float.IsNaN(exitX) && !bedReached && player != null)
            {
                float bedDeadline = Time.realtimeSinceStartup + 1.5f;
                while (Time.realtimeSinceStartup < bedDeadline && !(bool)transitioning.GetValue(game) &&
                    !(bool)dying.GetValue(game))
                {
                    input.Invoke(player, new object[] { 1f, false, false });
                    yield return null;
                }
                bedReached = (bool)transitioning.GetValue(game) || !(bool)inDream.GetValue(game);
            }
            if (player != null) clear.Invoke(player, null);
            measurements.Add(profileName + " | L05 full route | success=" + (stage == 4 && cReserve >= .15f &&
                bedReached && !dead) + " | stage=" + stage + " | exitX=" + exitX.ToString("F2") +
                " | cReserve=" + cReserve.ToString("F2") + " | bed=" + bedReached +
                " | died=" + dead + " | elapsed=" + elapsed.ToString("F2") + " | events=" + events);
        }
    }
}
