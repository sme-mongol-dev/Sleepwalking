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
    [TestFixture(1)]
    [TestFixture(100)]
    public sealed class SleepwalkingRemainingRouteTests
    {
        readonly int odradek;

        public SleepwalkingRemainingRouteTests(int odradek)
        {
            this.odradek = odradek;
        }

        const float Unit = 32f;
        const float Margin = .15f * Unit;
        static float C(float cells) => cells * Unit;

        sealed class Route
        {
            public int Level;
            public int ProfileIndex = 1;
            public string Profile;
            public MonoBehaviour Game;
            public MonoBehaviour Player;
            public Rigidbody2D Body;
            public Collider2D Shape;
            public MethodInfo Input;
            public MethodInfo Clear;
            public FieldInfo Dying;
            public FieldInfo Transitioning;
            public PropertyInfo InDream;
            public string LastSupport = "PlayerStart";
            public string Failure;
            public List<string> Trace = new List<string>
            {
                "level,profile,stage,step,time,x,y,vx,vy,horizontal,jump,dash,target,contact,stable,margin,hazardContact,dying,transitioning"
            };
            public List<string> Events = new List<string>();
        }

        [UnityTest]
        public IEnumerator L01BothGapsReachExitRun()
        {
            Route route = new Route { Level = 1 };
            yield return Start(route);
            if (route.Failure == null) yield return RunTo(route, "Start", C(2.6f), 120);
            if (route.Failure == null) yield return JumpTo(route, "JumpLanding", C(6.2f), C(5.425f), C(8.575f), 110);
            if (route.Failure == null) yield return RunTo(route, "JumpLanding", C(8.45f), 120);
            if (route.Failure == null) yield return JumpTo(route, "ExitRun", C(10.8f), C(10.425f), C(12.575f), 110);
            Finish(route);
        }

        [UnityTest]
        public IEnumerator L02PitAndFloorSpikesReachExitRun()
        {
            Route route = new Route { Level = 2 };
            yield return Start(route);
            if (route.Failure == null) yield return RunTo(route, "Start", C(2.6f), 120);
            if (route.Failure == null) yield return JumpTo(route, "SpikeLanding", C(5.6f), C(5.425f), C(6.2f), 120,
                "L02_PitSpikes");
            if (route.Failure == null) yield return JumpTo(route, "SpikeLanding", C(8.1f), C(7.825f), C(8.575f), 120,
                "L02_FloorSpikes", C(.48f));
            if (route.Failure == null) yield return RunTo(route, "SpikeLanding", C(8.45f), 80);
            if (route.Failure == null) yield return JumpTo(route, "ExitRun", C(10.8f), C(10.425f), C(12.575f), 120);
            Finish(route);
        }

        [UnityTest]
        public IEnumerator L03SawHopReachesExitRun()
        {
            Route route = new Route { Level = 3 };
            yield return Start(route);
            if (route.Failure == null) yield return RunTo(route, "Start", C(2.6f), 120);
            if (route.Failure == null) yield return JumpTo(route, "SawRun", C(5.6f), C(5.425f), C(6.3f), 120);
            if (route.Failure == null) yield return JumpTo(route, "SawRun", C(8.1f), C(7.675f), C(8.575f), 120,
                "L03_Saw", C(.765f));
            if (route.Failure == null) yield return RunTo(route, "SawRun", C(8.45f), 80);
            if (route.Failure == null) yield return JumpTo(route, "ExitRun", C(10.8f), C(10.425f), C(12.575f), 120);
            Finish(route);
        }

        [UnityTest]
        public IEnumerator L04RealButtonLaserReachesLaserRun()
        {
            Route route = new Route { Level = 4 };
            yield return Start(route);
            if (route.Failure == null) yield return CrossLaser(route);
            Finish(route);
        }

        [UnityTest]
        public IEnumerator L07WallButtonsDashSawReachDashLanding()
        {
            Route route = new Route { Level = 7 };
            yield return Start(route);
            if (route.Failure == null) yield return CrossL07(route);
            Finish(route);
        }

        [UnityTest]
        public IEnumerator L07ControlledProfileReachesDashLanding()
        {
            Route route = new Route { Level = 7, ProfileIndex = 0 };
            yield return Start(route);
            if (route.Failure == null) yield return CrossL07(route);
            Finish(route);
        }

        IEnumerator CrossL07(Route route)
        {
            Collider2D ledge = GameObject.Find("WallGrabLedge")?.GetComponent<Collider2D>();
            Collider2D landing = GameObject.Find("DashLanding")?.GetComponent<Collider2D>();
            Collider2D buttonA = GameObject.Find("L07_ButtonA")?.GetComponent<Collider2D>();
            Collider2D buttonB = GameObject.Find("L07_ButtonB")?.GetComponent<Collider2D>();
            Collider2D beam = GameObject.Find("L07_Laser_Beam")?.GetComponent<Collider2D>();
            Collider2D laserBase = GameObject.Find("L07_Laser_Base")?.GetComponent<Collider2D>();
            Collider2D saw = GameObject.Find("L07_Saw")?.GetComponent<Collider2D>();
            Assert.That(ledge, Is.Not.Null);
            Assert.That(landing, Is.Not.Null);
            Assert.That(buttonA, Is.Not.Null);
            Assert.That(buttonB, Is.Not.Null);
            Assert.That(beam, Is.Not.Null);
            Assert.That(laserBase, Is.Not.Null);
            Assert.That(saw, Is.Not.Null);
            MonoBehaviour laserMechanism = FindComponent(beam.gameObject, "Sleepwalking.SleepwalkingMechanism");
            MonoBehaviour sawMechanism = FindComponent(saw.gameObject, "Sleepwalking.SleepwalkingMechanism");
            FieldInfo laserUntil = laserMechanism?.GetType().GetField("disabledUntil",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo sawUntil = sawMechanism?.GetType().GetField("disabledUntil",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo wallProbe = route.Player.GetType().GetField("wall",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo grounded = route.Player.GetType().GetField("grounded",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo dashRemaining = route.Player.GetType().GetField("dashRemaining",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo dashCharges = route.Player.GetType().GetField("dashCharges",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(laserUntil, Is.Not.Null);
            Assert.That(sawUntil, Is.Not.Null);
            Assert.That(wallProbe, Is.Not.Null);
            Assert.That(grounded, Is.Not.Null);
            Assert.That(dashRemaining, Is.Not.Null);
            Assert.That(dashCharges, Is.Not.Null);

            bool firstJump = false;
            bool wallJump = false;
            bool ledgeDash = false;
            bool laserBaseJump = false;
            bool laserBaseDash = false;
            bool laserRecovered = false;
            bool laserStaged = false;
            bool pitJump = false;
            bool dash = false;
            bool wallContact = false;
            bool wallJumpActivated = false;
            bool dashActivated = false;
            bool buttonAContact = false;
            bool buttonBContact = false;
            bool laserDisabled = false;
            bool sawDisabled = false;
            float laserReserve = float.NaN;
            float sawReserve = float.NaN;
            int stage = 0;
            int stable = 0;
            for (int q = 0; q < 460; q++)
            {
                Vector2 position = route.Body.position;
                float beforeY = route.Body.linearVelocity.y;
                bool jumpNow = false;
                bool dashNow = false;
                float horizontal = 1f;
                if (stage == 0)
                {
                    if (!firstJump && position.x >= C(2.7f))
                    {
                        firstJump = jumpNow = true;
                        route.Events.Add("firstJump X=" + position.x.ToString("F3"));
                    }
                    if (firstJump && !wallJump && position.x >= C(3.65f) &&
                        position.y < C(4f) && route.Body.linearVelocity.y <= C(.1f))
                    {
                        if ((int)wallProbe.GetValue(route.Player) == 0 ||
                            (bool)grounded.GetValue(route.Player))
                        {
                            route.Failure = "L07 wall jump requested without wall contact";
                            yield break;
                        }
                        wallJump = jumpNow = true;
                        route.Events.Add("wallJump X=" + position.x.ToString("F3"));
                    }
                    horizontal = position.x < C(4.6f) ? 1f : position.x > C(5.2f) ? -1f : 0f;
                    if (wallJump && !ledgeDash && route.Body.linearVelocity.x >= C(.6f) &&
                        route.Shape.bounds.min.y >= ledge.bounds.max.y + C(.1f))
                    {
                        ledgeDash = dashNow = true;
                        route.Events.Add("ledgeDash X=" + position.x.ToString("F3"));
                    }
                }
                else
                {
                    if (!laserBaseJump && position.x >= C(7.35f))
                    {
                        if (!laserDisabled)
                        {
                            route.Failure = "ButtonA did not disable the beam before laser base";
                            yield break;
                        }
                        laserBaseJump = jumpNow = true;
                        route.Events.Add("laserBaseJump X=" + position.x.ToString("F3"));
                    }
                    if (laserBaseJump && !laserBaseDash && position.x >= C(7.75f))
                    {
                        laserBaseDash = dashNow = true;
                        route.Events.Add("laserBaseDash X=" + position.x.ToString("F3"));
                    }
                    if (laserBaseDash && !laserRecovered)
                    {
                        horizontal = -1f;
                        if ((bool)grounded.GetValue(route.Player) &&
                            (float)dashRemaining.GetValue(route.Player) <= 0f)
                        {
                            laserRecovered = true;
                            route.Events.Add("laserRecovered X=" + position.x.ToString("F3"));
                        }
                    }
                    if (laserRecovered && !laserStaged && position.x <= C(9.5f))
                    {
                        laserStaged = true;
                        route.Events.Add("laserStaged X=" + position.x.ToString("F3"));
                    }
                    if (laserRecovered && !pitJump)
                        horizontal = laserStaged ? 1f : -1f;
                    if (laserStaged && !pitJump && position.x >= C(10.55f) &&
                        route.Body.linearVelocity.x >= C(4f) &&
                        (bool)grounded.GetValue(route.Player))
                    {
                        pitJump = jumpNow = true;
                        route.Events.Add("pitJump X=" + position.x.ToString("F3"));
                    }
                    if (pitJump && !dash && position.x >= C(13.5f))
                    {
                        dash = dashNow = true;
                        route.Events.Add("dash X=" + position.x.ToString("F3"));
                    }
                    if (position.x >= C(19f))
                        horizontal = position.x < C(20.1f) ? 1f : position.x > C(20.7f) ? -1f : 0f;
                }
                route.Input.Invoke(route.Player, new object[] { horizontal, jumpNow, dashNow });
                yield return new WaitForFixedUpdate();
                if (route.Body == null) { route.Failure = "Player destroyed on L07"; yield break; }
                position = route.Body.position;
                // Update may consume the simulated press after this fixed step.
                if (stage == 0 && wallJump)
                    wallJumpActivated |= route.Body.linearVelocity.y > beforeY + C(2f) &&
                        route.Body.linearVelocity.y > C(5f);
                if (dashNow && stage == 1)
                    dashActivated = (float)dashRemaining.GetValue(route.Player) > 0f &&
                        (int)dashCharges.GetValue(route.Player) == 0 &&
                        route.Body.linearVelocity.x > C(10f);
                if (stage == 0 && (int)wallProbe.GetValue(route.Player) != 0 &&
                    !(bool)grounded.GetValue(route.Player)) wallContact = true;
                buttonAContact |= route.Shape.IsTouching(buttonA);
                buttonBContact |= route.Shape.IsTouching(buttonB);
                laserDisabled |= !beam.enabled && laserBase.enabled;
                sawDisabled |= !saw.enabled;
                if (float.IsNaN(laserReserve) && position.x >= C(9.325f))
                    laserReserve = (float)laserUntil.GetValue(laserMechanism) - Time.time;
                Collider2D target = stage == 0 ? ledge : landing;
                bool contact = Supported(route, target, out float margin);
                bool safe = stage == 0 ? position.x >= C(4.425f) && position.x <= C(5.575f) :
                    position.x >= C(17.425f) && position.x <= C(20.575f);
                stable = contact && safe && margin >= Margin ? stable + 1 : 0;
                bool beamContact = beam.enabled && route.Shape.IsTouching(beam);
                bool baseContact = laserBase.enabled && route.Shape.IsTouching(laserBase);
                bool sawContact = saw.enabled && route.Shape.IsTouching(saw);
                bool hazardContact = beamContact || baseContact || sawContact;
                Record(route, stage == 0 ? "WallLedge" : "LaserDashSaw", q, horizontal,
                    jumpNow, dashNow, target, contact, stable, margin, hazardContact);
                if (hazardContact || Dying(route))
                {
                    route.Events.Add("hazard X=" + position.x.ToString("F3") +
                        " beamTouch=" + beamContact + " baseTouch=" + baseContact +
                        " sawTouch=" + sawContact + " beamOverlap=" +
                        beam.bounds.Intersects(route.Shape.bounds) + " baseOverlap=" +
                        laserBase.bounds.Intersects(route.Shape.bounds) + " buttonA=" +
                        buttonAContact + " beamDisabled=" + laserDisabled);
                    route.Failure = "L07 hazard/death after " + route.LastSupport;
                    yield break;
                }
                if (stage == 0 && stable >= 3)
                {
                    if (!firstJump || !wallJump || !wallContact || !wallJumpActivated)
                    {
                        route.Failure = "WallGrabLedge reached without wall jump/contact";
                        yield break;
                    }
                    stage = 1;
                    stable = 0;
                    route.LastSupport = "WallGrabLedge X=" + position.x.ToString("F3");
                    route.Events.Add("ledgeContact X=" + position.x.ToString("F3") +
                        " margin=" + margin.ToString("F3"));
                }
                else if (stage == 1 && stable >= 3)
                {
                    sawReserve = (float)sawUntil.GetValue(sawMechanism) - Time.time;
                    route.LastSupport = "DashLanding X=" + position.x.ToString("F3");
                    route.Events.Add("dashLanding X=" + position.x.ToString("F3") +
                        " margin=" + margin.ToString("F3") +
                        " sawReserve=" + sawReserve.ToString("F3"));
                    if (!pitJump || !dash || !dashActivated || !buttonAContact || !buttonBContact ||
                        !laserDisabled || !sawDisabled || beam.enabled || !laserBase.enabled ||
                        saw.enabled || laserReserve < 1f || sawReserve < 1f)
                        route.Failure = "L07 landing lacked jump/dash/button contact or 1 s reserve; " +
                            "A=" + buttonAContact + " B=" + buttonBContact +
                            " laser=" + laserReserve.ToString("F3") +
                            " saw=" + sawReserve.ToString("F3");
                    route.Clear.Invoke(route.Player, null);
                    yield break;
                }
                if ((bool)route.Transitioning.GetValue(route.Game))
                {
                    route.Failure = "L07 exit transition before DashLanding";
                    yield break;
                }
                if (position.y < C(-3f) || position.x > C(23f))
                {
                    route.Failure = "L07 left route bounds after " + route.LastSupport;
                    yield break;
                }
            }
            route.Failure = "L07 route timed out after " + route.LastSupport;
            route.Clear.Invoke(route.Player, null);
        }

        IEnumerator Start(Route route)
        {
            AsyncOperation loading = SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
            Assert.That(loading, Is.Not.Null);
            while (!loading.isDone) yield return null;
            yield return null;
            route.Game = FindComponent(GameObject.Find("Sleepwalking Game"), "Sleepwalking.SleepwalkingGame");
            Assert.That(route.Game, Is.Not.Null);
            Type gameType = route.Game.GetType();
            gameType.GetField("odradek", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(route.Game, odradek);
            Array profiles = (Array)gameType.GetField("MovementProfiles").GetValue(route.Game);
            ScriptableObject profile = (ScriptableObject)profiles.GetValue(route.ProfileIndex);
            Assert.That(profile, Is.Not.Null);
            route.Profile = profile.name;
            gameType.GetField("MovementProfile").SetValue(route.Game, profile);
            gameType.GetField("completedDreams", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(route.Game, route.Level - 1);
            gameType.GetField("tutorialStage", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(route.Game, 2);
            route.Dying = gameType.GetField("dying", BindingFlags.Instance | BindingFlags.NonPublic);
            route.Transitioning = gameType.GetField("transitioning", BindingFlags.Instance | BindingFlags.NonPublic);
            route.InDream = gameType.GetProperty("InDream");
            gameType.GetMethod("StartDream").Invoke(route.Game, null);
            float deadline = Time.realtimeSinceStartup + 10f;
            while ((!((bool)route.InDream.GetValue(route.Game)) ||
                    (bool)route.Transitioning.GetValue(route.Game)) && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That((bool)route.InDream.GetValue(route.Game), Is.True);
            Assert.That((bool)route.Transitioning.GetValue(route.Game), Is.False);
            Assert.That(GameObject.Find("Dream L" + route.Level.ToString("00")), Is.Not.Null);
            GameObject dreamer = GameObject.Find("Dreamer");
            route.Player = FindComponent(dreamer, "Sleepwalking.SleepwalkingPlayer");
            route.Body = dreamer?.GetComponent<Rigidbody2D>();
            route.Shape = dreamer?.GetComponent<Collider2D>();
            Assert.That(route.Player, Is.Not.Null);
            Assert.That(route.Body, Is.Not.Null);
            Assert.That(route.Shape, Is.Not.Null);
            route.Input = route.Player.GetType().GetMethod("SetSimulatedInput");
            route.Clear = route.Player.GetType().GetMethod("ClearSimulatedInput");
            Assert.That(route.Input, Is.Not.Null);
            Assert.That(route.Clear, Is.Not.Null);
            Collider2D start = GameObject.Find(route.Level == 4 ? "LaserRun" : "Start")?.GetComponent<Collider2D>();
            Assert.That(start, Is.Not.Null);
            for (int q = 0; q < 5; q++) yield return new WaitForFixedUpdate();
            if (!route.Shape.IsTouching(start))
                route.Failure = "PlayerStart did not establish contact with " + start.name;
        }

        IEnumerator RunTo(Route route, string supportName, float targetX, int limit)
        {
            Collider2D support = GameObject.Find(supportName)?.GetComponent<Collider2D>();
            Assert.That(support, Is.Not.Null);
            for (int q = 0; q < limit; q++)
            {
                route.Input.Invoke(route.Player, new object[] { 1f, false, false });
                yield return new WaitForFixedUpdate();
                if (route.Body == null) { route.Failure = "Player destroyed during run"; yield break; }
                bool contact = Supported(route, support, out float margin);
                Record(route, "RunTo_" + targetX.ToString("F2", CultureInfo.InvariantCulture), q,
                    1f, false, false, support, contact, contact ? 1 : 0, margin, false);
                if (Dying(route)) { route.Failure = "Died while running from " + route.LastSupport; yield break; }
                if (route.Body.position.x >= targetX)
                {
                    if (!contact)
                        route.Failure = "Launch X=" + targetX.ToString("F2") +
                            " was reached without support " + supportName;
                    yield break;
                }
            }
            route.Failure = "Run did not reach X=" + targetX.ToString("F2") +
                " after " + route.LastSupport;
        }

        IEnumerator JumpTo(Route route, string targetName, float aimX, float safeMin, float safeMax,
            int limit, string hazardName = null, float clearanceTop = float.NaN)
        {
            Collider2D target = GameObject.Find(targetName)?.GetComponent<Collider2D>();
            List<Collider2D> hazards = new List<Collider2D>();
            if (hazardName != null)
            {
                Collider2D exact = GameObject.Find(hazardName)?.GetComponent<Collider2D>();
                if (exact != null) hazards.Add(exact);
                for (int q = 1; q <= 20; q++)
                {
                    Collider2D part = GameObject.Find(hazardName + "_" + q)?.GetComponent<Collider2D>();
                    if (part == null) break;
                    hazards.Add(part);
                }
            }
            Assert.That(target, Is.Not.Null);
            if (hazardName != null) Assert.That(hazards, Is.Not.Empty,
                "Missing hazard " + hazardName);
            int stable = 0;
            bool jumped = false;
            for (int q = 0; q < limit; q++)
            {
                float error = (aimX - route.Body.position.x) / Unit;
                float horizontal = Mathf.Clamp(error * 2.4f -
                    route.Body.linearVelocity.x / Unit * .55f, -1f, 1f);
                bool jump = !jumped;
                jumped = true;
                route.Input.Invoke(route.Player, new object[] { horizontal, jump, false });
                yield return new WaitForFixedUpdate();
                if (route.Body == null) { route.Failure = "Player destroyed before " + targetName; yield break; }
                bool contact = Supported(route, target, out float margin);
                bool safe = route.Body.position.x >= safeMin && route.Body.position.x <= safeMax;
                stable = contact && safe && margin >= Margin ? stable + 1 : 0;
                bool hazardContact = false;
                foreach (Collider2D hazard in hazards)
                {
                    hazardContact |= route.Shape.IsTouching(hazard);
                    if (!float.IsNaN(clearanceTop) &&
                        route.Shape.bounds.max.x > hazard.bounds.min.x &&
                        route.Shape.bounds.min.x < hazard.bounds.max.x &&
                        route.Shape.bounds.min.y <= clearanceTop + C(.02f))
                        hazardContact = true;
                }
                Record(route, "JumpTo_" + targetName + "_" + safeMin.ToString("F2", CultureInfo.InvariantCulture),
                    q, horizontal, jump, false, target, contact, stable, margin, hazardContact);
                if (hazardContact || Dying(route))
                {
                    route.Failure = "Hazard/death before safe " + targetName + " from " + route.LastSupport;
                    yield break;
                }
                if (stable >= 3)
                {
                    route.LastSupport = targetName + " X=" + route.Body.position.x.ToString("F3");
                    route.Clear.Invoke(route.Player, null);
                    yield break;
                }
                if ((bool)route.Transitioning.GetValue(route.Game))
                {
                    route.Failure = "Exit transition before stable " + targetName;
                    yield break;
                }
            }
            route.Failure = "No stable " + targetName + " landing in safe X " + safeMin.ToString("F2") +
                "–" + safeMax.ToString("F2") + " after " + route.LastSupport;
            route.Clear.Invoke(route.Player, null);
        }

        IEnumerator CrossLaser(Route route)
        {
            Collider2D floor = GameObject.Find("LaserRun")?.GetComponent<Collider2D>();
            Collider2D button = GameObject.Find("L04_ButtonLaser")?.GetComponent<Collider2D>();
            Collider2D beam = GameObject.Find("L04_Laser_Beam")?.GetComponent<Collider2D>();
            Collider2D laserBase = GameObject.Find("L04_Laser_Base")?.GetComponent<Collider2D>();
            Assert.That(floor, Is.Not.Null);
            Assert.That(button, Is.Not.Null);
            Assert.That(beam, Is.Not.Null);
            Assert.That(laserBase, Is.Not.Null);
            MonoBehaviour mechanism = FindComponent(beam.gameObject, "Sleepwalking.SleepwalkingMechanism");
            FieldInfo disabledUntil = mechanism?.GetType().GetField("disabledUntil",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(disabledUntil, Is.Not.Null);
            bool buttonTouched = false;
            bool disabledSeen = false;
            bool laserBaseJump = false;
            float reserve = float.NaN;
            for (int q = 0; q < 180; q++)
            {
                bool jump = !laserBaseJump && route.Body.position.x >= C(6.4f);
                if (jump)
                {
                    if (!disabledSeen)
                    {
                        route.Failure = "Button did not disable the beam before laser base";
                        yield break;
                    }
                    laserBaseJump = true;
                }
                route.Input.Invoke(route.Player, new object[] { 1f, jump, false });
                yield return new WaitForFixedUpdate();
                if (route.Body == null) { route.Failure = "Player destroyed at laser"; yield break; }
                buttonTouched |= route.Shape.IsTouching(button);
                disabledSeen |= !beam.enabled && laserBase.enabled;
                bool hazardContact = (beam.enabled && route.Shape.IsTouching(beam)) ||
                    (laserBase.enabled && route.Shape.IsTouching(laserBase));
                bool contact = Supported(route, floor, out float margin);
                Record(route, "ButtonLaser", q, 1f, jump, false, floor, contact,
                    contact ? 1 : 0, margin, hazardContact);
                if (hazardContact || Dying(route)) { route.Failure = "Died/collided at live laser"; yield break; }
                if (route.Body.position.x >= C(8.325f))
                {
                    reserve = (float)disabledUntil.GetValue(mechanism) - Time.time;
                    if (!buttonTouched || !disabledSeen || beam.enabled || !laserBase.enabled ||
                        reserve < 1f)
                        route.Failure = "Laser crossing lacked button contact, disabled colliders, or 1 s reserve; " +
                            "button=" + buttonTouched + ", disabled=" + disabledSeen +
                            ", reserve=" + reserve.ToString("F3");
                    else route.LastSupport = "LaserRun beyond laser X=" + route.Body.position.x.ToString("F3");
                    route.Clear.Invoke(route.Player, null);
                    yield break;
                }
            }
            route.Failure = "Laser crossing did not reach cell X=8.325";
            route.Clear.Invoke(route.Player, null);
        }

        IEnumerator WalkToBed(Route route, int limit)
        {
            Collider2D bed = GameObject.Find("BedExit")?.GetComponent<Collider2D>();
            Assert.That(bed, Is.Not.Null);
            bool touched = false;
            for (int q = 0; q < limit; q++)
            {
                float horizontal = route.Body.position.x < bed.bounds.center.x ? 1f : 0f;
                route.Input.Invoke(route.Player, new object[] { horizontal, false, false });
                yield return new WaitForFixedUpdate();
                if (route.Body == null) { route.Failure = "Player destroyed before BedExit"; yield break; }
                touched |= route.Shape.IsTouching(bed);
                Record(route, "BedExit", q, horizontal, false, false, bed, touched, touched ? 1 : 0,
                    float.NaN, false);
                if (Dying(route)) { route.Failure = "Died before BedExit after " + route.LastSupport; yield break; }
                if ((bool)route.Transitioning.GetValue(route.Game) || !(bool)route.InDream.GetValue(route.Game))
                    break;
            }
            route.Clear.Invoke(route.Player, null);
            float deadline = Time.realtimeSinceStartup + 3f;
            while ((bool)route.InDream.GetValue(route.Game) && Time.realtimeSinceStartup < deadline)
                yield return null;
            if (!touched || (bool)route.InDream.GetValue(route.Game))
                route.Failure = "BedExit not completed after " + route.LastSupport + "; touched=" + touched;
        }

        static bool Supported(Route route, Collider2D target, out float margin)
        {
            Bounds player = route.Shape.bounds;
            Bounds surface = target.bounds;
            margin = Mathf.Min(player.min.x - surface.min.x, surface.max.x - player.max.x);
            return target.enabled && route.Shape.IsTouching(target) &&
                Mathf.Abs(player.min.y - surface.max.y) < C(.12f) &&
                route.Body.linearVelocity.y <= C(.15f);
        }

        static void Record(Route route, string stage, int step, float horizontal, bool jump, bool dash,
            Collider2D target, bool contact, int stable, float margin, bool hazardContact)
        {
            route.Trace.Add(string.Format(CultureInfo.InvariantCulture,
                "{0},{1},{2},{3},{4:F3},{5:F3},{6:F3},{7:F3},{8:F3},{9:F2},{10},{11},{12},{13},{14},{15:F3},{16},{17},{18}",
                route.Level, route.Profile, stage, step, Time.fixedTime, route.Body.position.x,
                route.Body.position.y, route.Body.linearVelocity.x, route.Body.linearVelocity.y,
                horizontal, jump, dash, target?.name ?? "", contact, stable, margin,
                hazardContact, Dying(route), (bool)route.Transitioning.GetValue(route.Game)));
        }

        static bool Dying(Route route) => (bool)route.Dying.GetValue(route.Game);

        void Finish(Route route)
        {
            route.Clear?.Invoke(route.Player, null);
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string folder = Path.Combine(root, "Builds", "QA");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "traversal-remaining-L" +
                route.Level.ToString("00") + "-odradek-" + odradek +
                (route.ProfileIndex == 0 ? "-controlled" : "") + ".csv");
            File.WriteAllLines(path, route.Trace);
            File.WriteAllLines(Path.ChangeExtension(path, ".events.txt"), route.Events);
            if (odradek == 1)
            {
                string legacyPath = Path.Combine(root, "Docs", "QA", "traversal-remaining-L" +
                    route.Level.ToString("00") +
                    (route.ProfileIndex == 0 ? "-controlled" : "") + ".csv");
                File.WriteAllLines(legacyPath, route.Trace);
                File.WriteAllLines(Path.ChangeExtension(legacyPath, ".events.txt"), route.Events);
            }
            Assert.That(route.Failure, Is.Null, route.Failure + "; last support " + route.LastSupport +
                "; see " + path);
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
