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
    public sealed class SleepwalkingTraversalSequenceTests
    {
        readonly int odradek;

        public SleepwalkingTraversalSequenceTests(int odradek)
        {
            this.odradek = odradek;
        }

        const float Unit = 32f;
        const float Margin = .15f * Unit;
        static float C(float cells) => cells * Unit;

        sealed class Route
        {
            public int Level;
            public int ProfileIndex;
            public MonoBehaviour Game;
            public MonoBehaviour Player;
            public Rigidbody2D Body;
            public Collider2D Shape;
            public MethodInfo SetInput;
            public MethodInfo ClearInput;
            public FieldInfo Dying;
            public FieldInfo Transitioning;
            public List<string> Trace = new List<string>
            {
                "level,profile,phase,step,time,x,y,vx,vy,horizontal,jump,dash,targetX,targetTop,targetEnabled,contact,stable,margin,cyclePhase,activeRemaining"
            };
            public string ProfileName;
            public bool Reached;
            public string Failure;
            public float VerticalCarry;
            public float VerticalRelativeDrift;
        }

        [UnityTest]
        public IEnumerator L05ControlledTimedStaircaseReachesExitInSequence()
        {
            yield return RunL05(0);
        }

        [UnityTest]
        public IEnumerator L05ResponsiveTimedStaircaseReachesExitInSequence()
        {
            yield return RunL05(1);
        }

        IEnumerator RunL05(int profileIndex)
        {
            Route route = new Route { Level = 5, ProfileIndex = profileIndex };
            yield return StartRoute(route);
            if (route.Failure == null)
                yield return WaitForFreshTimedCycle(route);
            if (route.Failure == null)
                yield return RunToLaunch(route, C(2.3f));
            if (route.Failure == null)
                yield return JumpTo(route, "L05_TimedA", 110, true);
            if (route.Failure == null)
                yield return StageTimedLaunch(route, "L05_TimedA", C(5.1f), true);
            if (route.Failure == null)
                yield return JumpTo(route, "L05_TimedB", 110, true);
            if (route.Failure == null)
                yield return StageTimedLaunch(route, "L05_TimedB", C(8.48f), true);
            if (route.Failure == null)
                yield return JumpTo(route, "L05_TimedC", 110, true, 0f, 17);
            if (route.Failure == null)
                yield return StageTimedLaunch(route, "L05_TimedC", C(16.4f), true);
            if (route.Failure == null)
                yield return JumpTo(route, "ExitRun", 110, true);
            route.ClearInput?.Invoke(route.Player, null);
            Finish(route);
        }

        [UnityTest]
        public IEnumerator L06ControlledBothMoversCarryPlayerToHighExit()
        {
            yield return RunL06(0);
        }

        [UnityTest]
        public IEnumerator L06ResponsiveBothMoversCarryPlayerToHighExit()
        {
            yield return RunL06(1);
        }

        IEnumerator RunL06(int profileIndex)
        {
            Route route = new Route { Level = 6, ProfileIndex = profileIndex };
            yield return StartRoute(route);
            if (route.Failure == null)
                yield return WaitForHorizontalNearStart(route);
            if (route.Failure == null)
                yield return RunToLaunch(route, C(2.5f));
            if (route.Failure == null)
                yield return JumpTo(route, "L06_Horizontal", 150, false, C(.25f));
            if (route.Failure == null)
                yield return Ride(route, "L06_Horizontal", C(5.5f), true, 300, C(13.5f));
            if (route.Failure == null)
                yield return StageMoverDismount(route, "L06_Horizontal", C(.45f));
            if (route.Failure == null)
                yield return JumpTo(route, "LiftLanding", 140, true);
            if (route.Failure == null)
                yield return WaitForVerticalNearBottom(route);
            if (route.Failure == null)
                yield return RunToSupportLaunch(route, "LiftLanding", C(17.25f));
            if (route.Failure == null)
                yield return BrakeOnSupport(route, "LiftLanding");
            if (route.Failure == null)
                yield return JumpTo(route, "L06_Vertical", 140);
            if (route.Failure == null)
                yield return StageVerticalLaunch(route);
            if (route.Failure == null)
                yield return JumpTo(route, "HighExit", 140, false, C(.25f));
            route.ClearInput?.Invoke(route.Player, null);
            Finish(route);
        }

        IEnumerator StartRoute(Route route)
        {
            AsyncOperation loading = SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
            Assert.That(loading, Is.Not.Null);
            while (!loading.isDone)
                yield return null;
            yield return null;

            route.Game = FindComponent(GameObject.Find("Sleepwalking Game"), "Sleepwalking.SleepwalkingGame");
            Assert.That(route.Game, Is.Not.Null);
            Type gameType = route.Game.GetType();
            gameType.GetField("odradek", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(route.Game, odradek);
            Array profiles = (Array)gameType.GetField("MovementProfiles").GetValue(route.Game);
            Assert.That(profiles.Length, Is.GreaterThan(route.ProfileIndex));
            ScriptableObject profile = (ScriptableObject)profiles.GetValue(route.ProfileIndex);
            Assert.That(profile, Is.Not.Null);
            route.ProfileName = profile.name;
            gameType.GetField("MovementProfile").SetValue(route.Game, profile);
            gameType.GetField("completedDreams", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(route.Game, route.Level - 1);
            route.Dying = gameType.GetField("dying", BindingFlags.Instance | BindingFlags.NonPublic);
            route.Transitioning = gameType.GetField("transitioning", BindingFlags.Instance | BindingFlags.NonPublic);
            gameType.GetMethod("StartDream").Invoke(route.Game, null);
            PropertyInfo inDream = gameType.GetProperty("InDream");
            FieldInfo transitioning = gameType.GetField("transitioning", BindingFlags.Instance | BindingFlags.NonPublic);
            float deadline = Time.realtimeSinceStartup + 10f;
            while ((!((bool)inDream.GetValue(route.Game)) || (bool)transitioning.GetValue(route.Game)) &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That((bool)inDream.GetValue(route.Game), Is.True);
            Assert.That((bool)transitioning.GetValue(route.Game), Is.False);
            Assert.That(GameObject.Find("Dream L" + route.Level.ToString("00")), Is.Not.Null);

            GameObject dreamer = GameObject.Find("Dreamer");
            route.Player = FindComponent(dreamer, "Sleepwalking.SleepwalkingPlayer");
            route.Body = dreamer?.GetComponent<Rigidbody2D>();
            route.Shape = dreamer?.GetComponent<Collider2D>();
            Assert.That(route.Player, Is.Not.Null);
            Assert.That(route.Body, Is.Not.Null);
            Assert.That(route.Shape, Is.Not.Null);
            Type playerType = route.Player.GetType();
            route.SetInput = playerType.GetMethod("SetSimulatedInput");
            route.ClearInput = playerType.GetMethod("ClearSimulatedInput");
            Assert.That(route.SetInput, Is.Not.Null);
            Assert.That(route.ClearInput, Is.Not.Null);
            Collider2D start = GameObject.Find("Start")?.GetComponent<Collider2D>();
            Assert.That(start, Is.Not.Null);
            for (int q = 0; q < 5; q++)
                yield return new WaitForFixedUpdate();
            if (!route.Shape.IsTouching(start))
                route.Failure = "Authored PlayerStart did not establish contact with Start";
        }

        IEnumerator WalkToLaunch(Route route, float launchX)
        {
            Collider2D start = GameObject.Find("Start")?.GetComponent<Collider2D>();
            Assert.That(start, Is.Not.Null);
            for (int q = 0; q < 120; q++)
            {
                float error = (launchX - route.Body.position.x) / Unit;
                float horizontal = Mathf.Clamp(error * 2.4f -
                    route.Body.linearVelocity.x / Unit * .55f, -1f, 1f);
                route.SetInput.Invoke(route.Player, new object[] { horizontal, false, false });
                yield return new WaitForFixedUpdate();
                bool contact = Supported(route, start, out float margin);
                Record(route, "StartApproach", q, horizontal, false, false, start, contact,
                    contact ? 1 : 0, margin);
                if (contact && Mathf.Abs(error) <= .25f &&
                    Mathf.Abs(route.Body.linearVelocity.x) <= C(.8f))
                {
                    route.ClearInput.Invoke(route.Player, null);
                    yield break;
                }
                if (IsDying(route) || route.Body.position.x > start.bounds.max.x - C(.2f))
                    break;
            }
            route.Failure = "Could not reach a stable jump launch on Start from PlayerStart";
            route.ClearInput.Invoke(route.Player, null);
        }

        IEnumerator RunToLaunch(Route route, float launchX)
        {
            Collider2D start = GameObject.Find("Start")?.GetComponent<Collider2D>();
            Assert.That(start, Is.Not.Null);
            for (int q = 0; q < 120; q++)
            {
                route.SetInput.Invoke(route.Player, new object[] { 1f, false, false });
                yield return new WaitForFixedUpdate();
                bool contact = Supported(route, start, out float margin);
                Record(route, "StartRun", q, 1f, false, false, start, contact, contact ? 1 : 0, margin);
                if (route.Body.position.x >= launchX)
                {
                    if (!contact) route.Failure = "Running launch lost Start support";
                    yield break;
                }
                if (IsDying(route) || route.Body.position.x > start.bounds.max.x + C(.3f))
                    break;
            }
            route.Failure = "Could not build a running launch from PlayerStart";
            route.ClearInput.Invoke(route.Player, null);
        }

        IEnumerator RunToSupportLaunch(Route route, string supportName, float launchX)
        {
            Collider2D support = GameObject.Find(supportName)?.GetComponent<Collider2D>();
            Assert.That(support, Is.Not.Null);
            for (int q = 0; q < 80; q++)
            {
                route.SetInput.Invoke(route.Player, new object[] { 1f, false, false });
                yield return new WaitForFixedUpdate();
                bool contact = Supported(route, support, out float margin);
                Record(route, supportName + "_run", q, 1f, false, false, support,
                    contact, contact ? 1 : 0, margin);
                if (route.Body.position.x >= launchX && contact && margin >= Margin)
                {
                    route.ClearInput.Invoke(route.Player, null);
                    yield break;
                }
                if (IsDying(route) || !contact || margin < Margin) break;
            }
            route.Failure = "No supported running launch from " + supportName;
            route.ClearInput.Invoke(route.Player, null);
        }

        IEnumerator BrakeOnSupport(Route route, string supportName)
        {
            Collider2D support = GameObject.Find(supportName)?.GetComponent<Collider2D>();
            Assert.That(support, Is.Not.Null);
            route.SetInput.Invoke(route.Player, new object[] { 0f, false, false });
            yield return new WaitForFixedUpdate();
            bool contact = Supported(route, support, out float margin);
            Record(route, supportName + "_brake", 0, 0f, false, false, support,
                contact, contact ? 1 : 0, margin);
            if (!contact || margin < Margin)
                route.Failure = "Lost " + supportName + " while braking before vertical mover";
            route.ClearInput.Invoke(route.Player, null);
        }

        IEnumerator WaitForFreshTimedCycle(Route route)
        {
            Collider2D start = GameObject.Find("Start")?.GetComponent<Collider2D>();
            Collider2D first = GameObject.Find("L05_TimedA")?.GetComponent<Collider2D>();
            Assert.That(start, Is.Not.Null);
            Assert.That(first, Is.Not.Null);
            MonoBehaviour mechanism = FindComponent(first.gameObject,
                "Sleepwalking.SleepwalkingMechanism");
            Assert.That(mechanism, Is.Not.Null);
            float cycle = (float)mechanism.GetType().GetField("CycleSeconds").GetValue(mechanism);
            int maxSteps = Mathf.CeilToInt(cycle / Time.fixedDeltaTime) + 90;
            float previous = float.NaN;
            for (int q = 0; q < maxSteps; q++)
            {
                route.SetInput.Invoke(route.Player, new object[] { 0f, false, false });
                yield return new WaitForFixedUpdate();
                bool contact = Supported(route, start, out float margin);
                Record(route, "WaitTimedCycle", q, 0f, false, false, first,
                    route.Shape.IsTouching(first), 0, margin);
                float current = CyclePhase(first);
                if (contact && !float.IsNaN(previous) &&
                    previous > cycle - .2f && current < .2f)
                {
                    route.ClearInput.Invoke(route.Player, null);
                    yield break;
                }
                if (IsDying(route)) break;
                previous = current;
            }
            route.Failure = "No fresh L05 timed cycle while standing on Start";
            route.ClearInput.Invoke(route.Player, null);
        }

        IEnumerator WaitForHorizontalNearStart(Route route)
        {
            Collider2D start = GameObject.Find("Start")?.GetComponent<Collider2D>();
            Collider2D mover = GameObject.Find("L06_Horizontal")?.GetComponent<Collider2D>();
            Assert.That(start, Is.Not.Null);
            Assert.That(mover, Is.Not.Null);
            float previous = float.NaN;
            for (int q = 0; q < 420; q++)
            {
                route.SetInput.Invoke(route.Player, new object[] { 0f, false, false });
                yield return new WaitForFixedUpdate();
                bool contact = Supported(route, start, out float margin);
                Record(route, "WaitHorizontalStart", q, 0f, false, false, mover,
                    route.Shape.IsTouching(mover), 0, margin);
                float current = CyclePhase(mover);
                if (contact && !float.IsNaN(previous) && previous < 6.9f && current >= 6.9f)
                {
                    route.ClearInput.Invoke(route.Player, null);
                    yield break;
                }
                if (IsDying(route)) break;
                previous = current;
            }
            route.Failure = "Horizontal mover did not return toward Start while player was supported";
            route.ClearInput.Invoke(route.Player, null);
        }

        IEnumerator StageTimedLaunch(Route route, string platformName, float launchX,
            bool running)
        {
            Collider2D platform = GameObject.Find(platformName)?.GetComponent<Collider2D>();
            Assert.That(platform, Is.Not.Null);
            for (int q = 0; q < 45; q++)
            {
                bool contact = Supported(route, platform, out float margin);
                if (route.Body.position.x >= launchX - (running ? 0f : C(.12f)) &&
                    (running || Mathf.Abs(route.Body.linearVelocity.x) <= C(.8f)) &&
                    contact && margin >= Margin &&
                    ActiveRemaining(platform) >= .15f)
                {
                    route.ClearInput.Invoke(route.Player, null);
                    yield break;
                }
                float error = (launchX - route.Body.position.x) / Unit;
                float horizontal = running ? 1f : Mathf.Clamp(error * 2.4f -
                    route.Body.linearVelocity.x / Unit * .55f, -1f, 1f);
                route.SetInput.Invoke(route.Player, new object[] { horizontal, false, false });
                yield return new WaitForFixedUpdate();
                contact = Supported(route, platform, out margin);
                Record(route, platformName + "_launch", q, horizontal, false, false, platform,
                    contact, contact ? 1 : 0, margin);
                if (IsDying(route) || !platform.enabled ||
                    route.Body.position.x > platform.bounds.max.x - C(.35f))
                    break;
            }
            route.Failure = "No safe staged launch near the right side of " + platformName;
            route.ClearInput.Invoke(route.Player, null);
        }

        IEnumerator StageMoverDismount(Route route, string platformName, float targetOffset)
        {
            Collider2D platform = GameObject.Find(platformName)?.GetComponent<Collider2D>();
            Assert.That(platform, Is.Not.Null);
            for (int q = 0; q < 55; q++)
            {
                float center = platform.bounds.center.x;
                float offset = route.Body.position.x - center;
                bool contact = Supported(route, platform, out float margin);
                if (offset >= targetOffset && contact && margin >= Margin &&
                    platform.bounds.center.x >= C(13.5f))
                {
                    route.ClearInput.Invoke(route.Player, null);
                    yield break;
                }
                route.SetInput.Invoke(route.Player, new object[] { 1f, false, false });
                yield return new WaitForFixedUpdate();
                contact = Supported(route, platform, out margin);
                Record(route, platformName + "_dismount", q, 1f, false, false,
                    platform, contact, contact ? 1 : 0, margin);
                if (IsDying(route) || !contact) break;
            }
            route.Failure = "No supported right-side dismount from " + platformName;
            route.ClearInput.Invoke(route.Player, null);
        }

        IEnumerator WaitForVerticalNearBottom(Route route)
        {
            Collider2D landing = GameObject.Find("LiftLanding")?.GetComponent<Collider2D>();
            Collider2D mover = GameObject.Find("L06_Vertical")?.GetComponent<Collider2D>();
            Assert.That(landing, Is.Not.Null);
            Assert.That(mover, Is.Not.Null);
            float previous = float.NaN;
            for (int q = 0; q < 240; q++)
            {
                route.SetInput.Invoke(route.Player, new object[] { 0f, false, false });
                yield return new WaitForFixedUpdate();
                bool supported = Supported(route, landing, out float margin);
                bool moverContact = route.Shape.IsTouching(mover);
                Record(route, "WaitVerticalBottom", q, 0f, false, false, mover, moverContact, 0, margin);
                float current = CyclePhase(mover);
                if (supported && !float.IsNaN(previous) && previous > 3.8f && current < .2f)
                {
                    route.ClearInput.Invoke(route.Player, null);
                    yield break;
                }
                if (IsDying(route)) break;
                previous = current;
            }
            route.Failure = "Vertical mover did not return near LiftLanding while player was supported";
            route.ClearInput.Invoke(route.Player, null);
        }

        IEnumerator StageVerticalLaunch(Route route)
        {
            Collider2D mover = GameObject.Find("L06_Vertical")?.GetComponent<Collider2D>();
            Assert.That(mover, Is.Not.Null);
            for (int q = 0; q < 20; q++)
            {
                bool contact = Supported(route, mover, out float margin);
                if (route.Body.position.x >= C(19.1f) && contact && margin >= Margin)
                {
                    route.ClearInput.Invoke(route.Player, null);
                    yield break;
                }
                route.SetInput.Invoke(route.Player, new object[] { 1f, false, false });
                yield return new WaitForFixedUpdate();
                contact = Supported(route, mover, out margin);
                Record(route, "VerticalLaunch", q, 1f, false, false, mover,
                    contact, contact ? 1 : 0, margin);
                if (IsDying(route) || !contact) break;
            }
            route.Failure = "No supported right-side launch from L06_Vertical";
            route.ClearInput.Invoke(route.Player, null);
        }

        IEnumerator JumpTo(Route route, string targetName, int maxSteps, bool landNearLeftEdge = false,
            float aimOffset = 0f, int dashAtStep = -1)
        {
            Collider2D target = GameObject.Find(targetName)?.GetComponent<Collider2D>();
            if (target == null)
            {
                route.Failure = targetName + " collider missing";
                yield break;
            }
            int stable = 0;
            float verticalStartTop = float.NaN;
            float verticalStartOffset = float.NaN;
            for (int q = 0; q < maxSteps; q++)
            {
                float aimX = landNearLeftEdge ?
                    target.bounds.min.x + route.Shape.bounds.extents.x + C(.25f) :
                    target.bounds.center.x + aimOffset;
                float error = (aimX - route.Body.position.x) / Unit;
                float horizontal = Mathf.Clamp(error * 2.4f -
                    route.Body.linearVelocity.x / Unit * .55f, -1f, 1f);
                if (targetName == "L06_Vertical" && q == 0) horizontal = 0f;
                if (dashAtStep >= 0 && q >= dashAtStep && q < dashAtStep + 10)
                    horizontal = 1f;
                bool jump = q == 0;
                bool dash = q == dashAtStep;
                route.SetInput.Invoke(route.Player, new object[] { horizontal, jump, dash });
                yield return new WaitForFixedUpdate();
                if (route.Body == null ||
                    ((bool)route.Transitioning.GetValue(route.Game) && targetName != "HighExit"))
                {
                    route.Failure = "Exit transition began before stable landing on " + targetName;
                    yield break;
                }
                bool contact = Supported(route, target, out float margin);
                stable = contact ? stable + 1 : 0;
                if (targetName == "L06_Vertical")
                {
                    float top = target.bounds.max.y;
                    float offset = route.Shape.bounds.min.y - top;
                    if (Mathf.Abs(offset) <= C(.08f) &&
                        route.Body.linearVelocity.y >= C(-.2f))
                    {
                        if (float.IsNaN(verticalStartTop))
                        {
                            verticalStartTop = top;
                            verticalStartOffset = offset;
                        }
                        route.VerticalCarry = Mathf.Max(route.VerticalCarry, top - verticalStartTop);
                        route.VerticalRelativeDrift = Mathf.Max(route.VerticalRelativeDrift,
                            Mathf.Abs(offset - verticalStartOffset));
                    }
                }
                Record(route, targetName, q, horizontal, jump, dash, target, contact, stable, margin);
                if (stable >= 3 && margin >= Margin)
                {
                    if (targetName == "L06_Vertical" &&
                        (route.VerticalCarry < C(.95f) || route.VerticalRelativeDrift > C(.08f)))
                    {
                        route.Failure = "Vertical mover contact lacked one cell ascent with stable foot offset";
                        route.ClearInput.Invoke(route.Player, null);
                        yield break;
                    }
                    float remaining = ActiveRemaining(target);
                    if (!float.IsNaN(remaining) && remaining < .15f)
                    {
                        route.Failure = targetName + " landed with less than 0.15 s timed reserve";
                        route.ClearInput.Invoke(route.Player, null);
                        yield break;
                    }
                    route.Reached = true;
                    route.ClearInput.Invoke(route.Player, null);
                    yield break;
                }
                if (IsDying(route) || route.Body.position.y < C(-3f))
                    break;
            }
            route.Failure = "No stable landing on " + targetName + " within " + maxSteps + " fixed steps";
            route.ClearInput.Invoke(route.Player, null);
        }

        IEnumerator Ride(Route route, string targetName, float requiredMovement, bool horizontalRide,
            int maxSteps, float minimumExitCoordinate)
        {
            Collider2D target = GameObject.Find(targetName)?.GetComponent<Collider2D>();
            Assert.That(target, Is.Not.Null);
            Vector2 firstPlatform = (Vector2)target.bounds.center;
            Vector2 firstRelative = route.Body.position - firstPlatform;
            int stable = 0;
            bool lostContact = false;
            float maxRelativeDrift = 0f;
            for (int q = 0; q < maxSteps; q++)
            {
                route.SetInput.Invoke(route.Player, new object[] { 0f, false, false });
                yield return new WaitForFixedUpdate();
                if (route.Body == null || (bool)route.Transitioning.GetValue(route.Game))
                {
                    route.Failure = "Exit transition began during ride on " + targetName;
                    yield break;
                }
                bool contact = Supported(route, target, out float margin);
                stable = contact ? stable + 1 : 0;
                lostContact |= !contact;
                Record(route, targetName + "_ride", q, 0f, false, false, target, contact, stable, margin);
                float movement = horizontalRide ? target.bounds.center.x - firstPlatform.x :
                    target.bounds.center.y - firstPlatform.y;
                float relativeDrift = (route.Body.position - (Vector2)target.bounds.center - firstRelative).magnitude;
                maxRelativeDrift = Mathf.Max(maxRelativeDrift, relativeDrift);
                bool nearExit = float.IsNaN(minimumExitCoordinate) ||
                    (horizontalRide ? target.bounds.center.x : target.bounds.max.y) >= minimumExitCoordinate;
                if (movement >= requiredMovement && nearExit && stable >= 3 && margin >= Margin &&
                    !lostContact && maxRelativeDrift <= C(.65f))
                {
                    route.ClearInput.Invoke(route.Player, null);
                    yield break;
                }
                if (IsDying(route) || route.Body.position.y < C(-3f))
                    break;
            }
            route.Failure = "No continuous " + targetName + " ride of " + requiredMovement.ToString("F1") +
                " world units with relative drift <=0.65 cell; lost contact=" + lostContact +
                ", max drift=" + maxRelativeDrift.ToString("F3");
            route.ClearInput.Invoke(route.Player, null);
        }

        IEnumerator WalkToBed(Route route, int maxSteps)
        {
            Collider2D bed = GameObject.Find("BedExit")?.GetComponent<Collider2D>();
            Assert.That(bed, Is.Not.Null);
            for (int q = 0; q < maxSteps; q++)
            {
                float horizontal = route.Body.position.x < bed.bounds.center.x ? 1f : 0f;
                route.SetInput.Invoke(route.Player, new object[] { horizontal, false, false });
                yield return new WaitForFixedUpdate();
                bool contact = route.Shape.IsTouching(bed);
                Record(route, "BedExit", q, horizontal, false, false, bed, contact, contact ? 1 : 0, float.NaN);
                if (contact)
                {
                    route.ClearInput.Invoke(route.Player, null);
                    PropertyInfo inDream = route.Game.GetType().GetProperty("InDream");
                    float deadline = Time.realtimeSinceStartup + 3f;
                    while ((bool)inDream.GetValue(route.Game) && Time.realtimeSinceStartup < deadline)
                        yield return null;
                    if ((bool)inDream.GetValue(route.Game))
                        route.Failure = "BedExit touched but dream did not complete";
                    yield break;
                }
                if (IsDying(route) || route.Body.position.y < C(-3f))
                    break;
            }
            route.Failure = "BedExit trigger not reached";
            route.ClearInput.Invoke(route.Player, null);
        }

        static bool Supported(Route route, Collider2D target, out float margin)
        {
            Bounds player = route.Shape.bounds;
            Bounds platform = target.bounds;
            margin = Mathf.Min(player.min.x - platform.min.x, platform.max.x - player.max.x);
            return target.enabled && route.Shape.IsTouching(target) &&
                Mathf.Abs(player.min.y - platform.max.y) < C(.12f) &&
                route.Body.linearVelocity.y <= C(.15f);
        }

        static void Record(Route route, string phase, int step, float horizontal, bool jump, bool dash,
            Collider2D target, bool contact, int stable, float margin)
        {
            float cyclePhase = float.NaN;
            float activeRemaining = float.NaN;
            MonoBehaviour mechanism = FindComponent(target.gameObject, "Sleepwalking.SleepwalkingMechanism");
            if (mechanism != null)
            {
                Type type = mechanism.GetType();
                FieldInfo elapsed = type.GetField("elapsed", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo cycle = type.GetField("CycleSeconds");
                FieldInfo active = type.GetField("ActiveSeconds");
                if (elapsed != null && cycle != null && active != null)
                {
                    cyclePhase = Mathf.Repeat((float)elapsed.GetValue(mechanism), (float)cycle.GetValue(mechanism));
                    activeRemaining = (float)active.GetValue(mechanism) - cyclePhase;
                }
            }
            route.Trace.Add(string.Format(CultureInfo.InvariantCulture,
                "{0},{1},{2},{3},{4:F3},{5:F3},{6:F3},{7:F3},{8:F3},{9:F3},{10},{11},{12:F3},{13:F3},{14},{15},{16},{17:F3},{18:F3},{19:F3}",
                route.Level, route.ProfileName, phase, step, Time.fixedTime, route.Body.position.x,
                route.Body.position.y, route.Body.linearVelocity.x, route.Body.linearVelocity.y,
                horizontal, jump, dash, target.bounds.center.x, target.bounds.max.y, target.enabled,
                contact, stable, margin, cyclePhase, activeRemaining));
        }

        static float ActiveRemaining(Collider2D target)
        {
            MonoBehaviour mechanism = FindComponent(target.gameObject, "Sleepwalking.SleepwalkingMechanism");
            if (mechanism == null) return float.NaN;
            Type type = mechanism.GetType();
            if ((string)type.GetField("Kind").GetValue(mechanism) != "TimedPlatform")
                return float.NaN;
            float elapsed = (float)type.GetField("elapsed", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(mechanism);
            float cycle = (float)type.GetField("CycleSeconds").GetValue(mechanism);
            float active = (float)type.GetField("ActiveSeconds").GetValue(mechanism);
            return active - Mathf.Repeat(elapsed, cycle);
        }

        static float CyclePhase(Collider2D target)
        {
            MonoBehaviour mechanism = FindComponent(target.gameObject, "Sleepwalking.SleepwalkingMechanism");
            if (mechanism == null) return float.NaN;
            Type type = mechanism.GetType();
            float elapsed = (float)type.GetField("elapsed", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(mechanism);
            float cycle = (float)type.GetField("CycleSeconds").GetValue(mechanism);
            return Mathf.Repeat(elapsed, cycle);
        }

        static bool IsDying(Route route) => (bool)route.Dying.GetValue(route.Game);

        void Finish(Route route)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string folder = Path.Combine(projectRoot, "Builds", "QA");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "traversal-sequence-L" +
                route.Level.ToString("00") + "-odradek-" + odradek +
                (route.ProfileIndex == 0 ? "-controlled.csv" : "-responsive.csv"));
            File.WriteAllLines(path, route.Trace);
            if (odradek == 1)
            {
                string legacyPath = Path.Combine(projectRoot, "Docs", "QA", "traversal-sequence-L" +
                    route.Level.ToString("00") +
                    (route.ProfileIndex == 0 ? "-controlled.csv" : "-responsive.csv"));
                File.WriteAllLines(legacyPath, route.Trace);
            }
            Assert.That(route.Failure, Is.Null, route.Failure + "; see " + path);
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
