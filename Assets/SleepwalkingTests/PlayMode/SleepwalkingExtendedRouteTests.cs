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
    public sealed class SleepwalkingExtendedRouteTests
    {
        const float Unit = 32f;
        const float Margin = .15f * Unit;
        static float C(float cells) => cells * Unit;

        sealed class Route
        {
            public int Level;
            public int ProfileIndex;
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
            public string Failure;
            public string LastSupport = "PlayerStart";
            public readonly List<string> Trace = new List<string>
            {
                "level,profile,phase,step,time,x,y,vx,vy,horizontal,jump,dash,target,contact,stable,margin,dying,transitioning"
            };
        }

        [UnityTest] public IEnumerator L01Controlled() { yield return Run(1, 0); }
        [UnityTest] public IEnumerator L01Responsive() { yield return Run(1, 1); }
        [UnityTest] public IEnumerator L02Controlled() { yield return Run(2, 0); }
        [UnityTest] public IEnumerator L02Responsive() { yield return Run(2, 1); }
        [UnityTest] public IEnumerator L03Controlled() { yield return Run(3, 0); }
        [UnityTest] public IEnumerator L03Responsive() { yield return Run(3, 1); }
        [UnityTest] public IEnumerator L04Controlled() { yield return Run(4, 0); }
        [UnityTest] public IEnumerator L04Responsive() { yield return Run(4, 1); }
        [UnityTest] public IEnumerator L05Controlled() { yield return Run(5, 0); }
        [UnityTest] public IEnumerator L05Responsive() { yield return Run(5, 1); }
        [UnityTest] public IEnumerator L06Controlled() { yield return Run(6, 0); }
        [UnityTest] public IEnumerator L06Responsive() { yield return Run(6, 1); }
        [UnityTest] public IEnumerator L07Controlled() { yield return Run(7, 0); }
        [UnityTest] public IEnumerator L07Responsive() { yield return Run(7, 1); }

        IEnumerator Run(int level, int profileIndex)
        {
            Route route = new Route { Level = level, ProfileIndex = profileIndex };
            yield return Start(route);
            if (route.Failure == null)
            {
                if (route.ProfileIndex == 0 && level <= 3) yield return ControlledPrefix(route);
                else yield return ReplayPrefix(route);
            }
            if (route.Failure == null)
            {
                switch (level)
                {
                    case 1: yield return ContinueL01(route); break;
                    case 2: yield return ContinueL02(route); break;
                    case 3: yield return ContinueL03(route); break;
                    case 4: yield return ContinueL04(route); break;
                    case 5: yield return ContinueL05(route); break;
                    case 6: yield return ContinueL06(route); break;
                    case 7: yield return ContinueL07(route); break;
                }
            }
            if (route.Failure == null) yield return WalkToBed(route);
            route.Clear?.Invoke(route.Player, null);
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string path = Path.Combine(root, "Docs", "QA", "extended-L" + level.ToString("00") +
                (profileIndex == 0 ? "-controlled.csv" : "-responsive.csv"));
            File.WriteAllLines(path, route.Trace);
            Assert.That(route.Failure, Is.Null, route.Failure + "; see " + path);
            LogAssert.NoUnexpectedReceived();
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
            Array profiles = (Array)gameType.GetField("MovementProfiles").GetValue(route.Game);
            ScriptableObject profile = (ScriptableObject)profiles.GetValue(route.ProfileIndex);
            Assert.That(profile, Is.Not.Null);
            route.Profile = profile.name;
            gameType.GetField("MovementProfile").SetValue(route.Game, profile);
            gameType.GetField("odradek", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(route.Game, 1);
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
            for (int q = 0; q < 5; q++) yield return new WaitForFixedUpdate();
        }

        IEnumerator ReplayPrefix(Route route)
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string name = route.Level == 5 || route.Level == 6 ?
                "traversal-sequence-L" + route.Level.ToString("00") +
                    (route.ProfileIndex == 0 ? "-controlled.csv" : "-responsive.csv") :
                "traversal-remaining-L" + route.Level.ToString("00") + ".csv";
            if (route.Level == 7 && route.ProfileIndex == 0)
                name = "traversal-remaining-L07-controlled.csv";
            string path = Path.Combine(root, "Docs", "QA", name);
            Assert.That(File.Exists(path), Is.True, path);
            string[] requiredNames = route.Level == 1 ? new[] { "JumpLanding" } :
                route.Level == 2 ? new[] { "SpikeLanding" } :
                route.Level == 3 ? new[] { "SawRun" } :
                route.Level == 5 ? new[] { "L05_TimedA", "L05_TimedB", "L05_TimedC" } :
                route.Level == 6 ? new[] { "L06_Horizontal", "L06_Vertical" } :
                route.Level == 7 ? new[] { "WallGrabLedge", "DashLanding" } : Array.Empty<string>();
            Collider2D[] required = new Collider2D[requiredNames.Length];
            int[] stable = new int[required.Length];
            bool[] visited = new bool[required.Length];
            for (int q = 0; q < required.Length; q++)
            {
                required[q] = GameObject.Find(requiredNames[q])?.GetComponent<Collider2D>();
                Assert.That(required[q], Is.Not.Null, requiredNames[q]);
            }
            Collider2D buttonA = GameObject.Find(route.Level == 4 ? "L04_ButtonLaser" :
                route.Level == 7 ? "L07_ButtonA" : "")?.GetComponent<Collider2D>();
            Collider2D buttonB = route.Level == 7 ?
                GameObject.Find("L07_ButtonB")?.GetComponent<Collider2D>() : null;
            bool touchedA = false;
            bool touchedB = false;
            bool dashActivated = false;
            FieldInfo dashRemaining = route.Player.GetType().GetField("dashRemaining",
                BindingFlags.Instance | BindingFlags.NonPublic);
            int step = 0;
            foreach (string line in File.ReadAllLines(path))
            {
                if (line.Length == 0 || line[0] == '#') continue;
                string[] row = line.Split(',');
                if (row.Length < 12 || row[0] == "level") continue;
                if (row[2] == "BedExit") break;
                float horizontal = float.Parse(row[9], CultureInfo.InvariantCulture);
                bool jump = bool.Parse(row[10]);
                bool dash = bool.Parse(row[11]);
                route.Input.Invoke(route.Player, new object[] { horizontal, jump, dash });
                yield return new WaitForFixedUpdate();
                if (route.Body == null) { route.Failure = "Player destroyed in old route prefix"; yield break; }
                Record(route, "Prefix_" + row[2], step++, horizontal, jump, dash, null, false, 0, float.NaN);
                if (Dying(route) || Transitioning(route))
                {
                    route.Failure = "Old route prefix died or completed before new section at " + row[2];
                    yield break;
                }
                for (int q = 0; q < required.Length; q++)
                {
                    bool contact = Supported(route, required[q], out float margin);
                    stable[q] = contact ? stable[q] + 1 : 0;
                    visited[q] |= stable[q] >= 3 && margin >= Margin;
                }
                touchedA |= buttonA != null && route.Shape.IsTouching(buttonA);
                touchedB |= buttonB != null && route.Shape.IsTouching(buttonB);
                if (dash && dashRemaining != null)
                    dashActivated |= (float)dashRemaining.GetValue(route.Player) > 0f;
            }
            route.Clear.Invoke(route.Player, null);
            for (int q = 0; q < visited.Length; q++)
                if (!visited[q]) route.Failure = "Old prefix missed stable " + requiredNames[q];
            if ((route.Level == 4 || route.Level == 7) && !touchedA)
                route.Failure = "Old prefix missed first laser button";
            if (route.Level == 7 && (!touchedB || !dashActivated))
                route.Failure = "Old prefix missed ButtonB or dash";
            string supportName = route.Level == 4 ? "LaserRun" :
                route.Level == 6 ? "HighExit" : route.Level == 7 ? "DashLanding" : "ExitRun";
            Collider2D support = GameObject.Find(supportName)?.GetComponent<Collider2D>();
            Assert.That(support, Is.Not.Null, supportName);
            bool supported = Supported(route, support, out float edge);
            if (!supported || edge < Margin)
                route.Failure = "Old prefix did not finish safely on " + supportName;
            route.LastSupport = supportName;
        }

        IEnumerator ControlledPrefix(Route route)
        {
            yield return MoveTo(route, "Start", 3.2f);
            if (route.Level == 1)
            {
                if (route.Failure == null) yield return JumpTo(route, "JumpLanding", 5.8f, 5.425f, 8.575f);
                if (route.Failure == null) yield return MoveTo(route, "JumpLanding", 8.35f);
            }
            else if (route.Level == 2)
            {
                if (route.Failure == null) yield return JumpTo(route, "SpikeLanding", 5.8f, 5.425f, 6.2f,
                    "L02_PitSpikes");
                if (route.Failure == null) yield return MoveTo(route, "SpikeLanding", 5.8f);
                if (route.Failure == null) yield return JumpTo(route, "SpikeLanding", 8.1f, 7.725f, 8.575f,
                    "L02_FloorSpikes");
                if (route.Failure == null) yield return MoveTo(route, "SpikeLanding", 8.35f);
            }
            else
            {
                if (route.Failure == null) yield return JumpTo(route, "SawRun", 5.8f, 5.425f, 6.3f);
                if (route.Failure == null) yield return MoveTo(route, "SawRun", 5.8f);
                if (route.Failure == null) yield return JumpTo(route, "SawRun", 8.1f, 7.725f, 8.575f,
                    "L03_Saw");
                if (route.Failure == null) yield return MoveTo(route, "SawRun", 8.35f);
            }
            if (route.Failure == null) yield return JumpTo(route, "ExitRun", 10.8f, 10.425f, 12.575f);
        }

        IEnumerator ContinueL01(Route route)
        {
            yield return MoveTo(route, "ExitRun", 12.35f);
            if (route.Failure == null) yield return JumpTo(route, "RiseA", 14.8f, 14.425f, 16.575f);
            if (route.Failure == null) yield return MoveTo(route, "RiseA", 16.35f);
            if (route.Failure == null) yield return JumpTo(route, "GapShelf", 19.7f, 19.425f, 20.575f);
            if (route.Failure == null) yield return MoveTo(route, "GapShelf", 20.35f);
            if (route.Failure == null) yield return JumpTo(route, "FinalRun", 23.8f, 23.425f, 26.575f);
        }

        IEnumerator ContinueL02(Route route)
        {
            yield return MoveTo(route, "ExitRun", 12.35f);
            if (route.Failure == null) yield return JumpTo(route, "SpikeBridge", 14.8f, 14.425f, 16f,
                "L02_SecondPitSpikes");
            if (route.Failure == null) yield return MoveTo(route, "SpikeBridge", 17.3f);
            if (route.Failure == null) yield return JumpTo(route, "SpikeBridge", 19.8f, 19.425f, 20.575f,
                "L02_BridgeSpikes");
            if (route.Failure == null) yield return MoveTo(route, "SpikeBridge", 20.35f);
            if (route.Failure == null) yield return JumpTo(route, "HighRecovery", 23.8f, 23.425f, 25.575f);
            if (route.Failure == null) yield return MoveTo(route, "HighRecovery", 25.35f);
            if (route.Failure == null) yield return JumpTo(route, "FinalRun", 28.8f, 28.425f, 31.575f);
        }

        IEnumerator ContinueL03(Route route)
        {
            yield return MoveTo(route, "ExitRun", 12.35f);
            if (route.Failure == null) yield return JumpTo(route, "SecondSawRun", 14.8f, 14.425f, 15.5f);
            if (route.Failure == null) yield return MoveTo(route, "SecondSawRun", 15.35f);
            if (route.Failure == null) yield return JumpTo(route, "SecondSawRun", 17.8f, 17.425f, 18.575f,
                "L03_SecondSaw");
            if (route.Failure == null) yield return MoveTo(route, "SecondSawRun", 18.35f);
            if (route.Failure == null) yield return JumpTo(route, "UpperLanding", 21.8f, 21.425f, 23.575f);
            if (route.Failure == null) yield return MoveTo(route, "UpperLanding", 23.35f);
            if (route.Failure == null) yield return JumpTo(route, "FinalRun", 26.8f, 26.425f, 28.575f);
        }

        IEnumerator ContinueL04(Route route)
        {
            yield return MoveTo(route, "LaserRun", 12.35f);
            if (route.Failure == null) yield return JumpTo(route, "SecondLaserRun", 14.8f, 14.425f, 16f);
            if (route.Failure == null)
                yield return CrossLaser(route, "L04_ButtonSecond", "L04_SecondLaser", 22f);
        }

        IEnumerator ContinueL05(Route route)
        {
            yield return MoveTo(route, "ExitRun", 22.3f);
            if (route.Failure == null) yield return WaitForTimedCycle(route, "L05_TimedD");
            if (route.Failure == null) yield return JumpTo(route, "L05_TimedD", 25f, 24.375f, 25.625f);
            if (route.Failure == null) yield return MoveTo(route, "L05_TimedD", 25.3f);
            if (route.Failure == null) yield return JumpTo(route, "L05_TimedE", 28f, 27.375f, 28.625f);
            if (route.Failure == null) yield return RunToTimedEdge(route, "L05_TimedE", 28.4f);
            if (route.Failure == null) yield return DashTimedPit(route);
        }

        IEnumerator ContinueL06(Route route)
        {
            yield return MoveTo(route, "HighExit", 22.35f);
            if (route.Failure == null) yield return WaitForMover(route, "L06_FinalBridge", 25.5f);
            if (route.Failure == null) yield return JumpTo(route, "L06_FinalBridge", 26.8f, 26.375f, 28.625f);
            if (route.Failure == null) yield return RideMover(route, "L06_FinalBridge", 34.5f);
            if (route.Failure == null) yield return RunToMoverEdge(route, "L06_FinalBridge");
            if (route.Failure == null) yield return JumpTo(route, "FinalRun", 38.8f, 38.425f, 41.575f);
        }

        IEnumerator ContinueL07(Route route)
        {
            yield return MoveTo(route, "DashLanding", 20.35f);
            if (route.Failure == null) yield return JumpTo(route, "SecondLaserRun", 23.8f, 22.425f, 25f);
            if (route.Failure == null)
                yield return CrossLaser(route, "L07_ButtonC", "L07_SecondLaser", 29.5f);
            if (route.Failure == null) yield return MoveTo(route, "SecondLaserRun", 29.2f);
            if (route.Failure == null) yield return DashPit(route);
        }

        IEnumerator MoveTo(Route route, string supportName, float x)
        {
            Collider2D support = GameObject.Find(supportName)?.GetComponent<Collider2D>();
            Assert.That(support, Is.Not.Null, supportName);
            int stable = 0;
            for (int q = 0; q < 120; q++)
            {
                float error = (C(x) - route.Body.position.x) / Unit;
                float horizontal = Mathf.Clamp(error * 2.4f - route.Body.linearVelocity.x / Unit * .55f, -1f, 1f);
                route.Input.Invoke(route.Player, new object[] { horizontal, false, false });
                yield return new WaitForFixedUpdate();
                if (route.Body == null) { route.Failure = "Player destroyed on " + supportName; yield break; }
                bool contact = Supported(route, support, out float margin);
                stable = contact && margin >= Margin && Mathf.Abs(error) < .15f &&
                    Mathf.Abs(route.Body.linearVelocity.x) < C(.8f) ? stable + 1 : 0;
                Record(route, "Stage_" + supportName, q, horizontal, false, false, support, contact, stable, margin);
                if (Dying(route) || !support.enabled) { route.Failure = "Lost " + supportName; yield break; }
                if (stable >= 3)
                {
                    route.LastSupport = supportName;
                    route.Clear.Invoke(route.Player, null);
                    yield break;
                }
            }
            route.Failure = "Could not stage X=" + x + " on " + supportName;
        }

        IEnumerator RunToTimedEdge(Route route, string supportName, float x)
        {
            Collider2D support = GameObject.Find(supportName)?.GetComponent<Collider2D>();
            Assert.That(support, Is.Not.Null, supportName);
            for (int q = 0; q < 45; q++)
            {
                route.Input.Invoke(route.Player, new object[] { 1f, false, false });
                yield return new WaitForFixedUpdate();
                if (route.Body == null) { route.Failure = "Player destroyed on " + supportName; yield break; }
                bool contact = Supported(route, support, out float margin);
                Record(route, "RunToEdge_" + supportName, q, 1f, false, false,
                    support, contact, contact ? 1 : 0, margin);
                if (Dying(route) || !support.enabled || !contact || margin < Margin)
                {
                    route.Failure = "Lost safe " + supportName + " during running launch";
                    yield break;
                }
                if (route.Body.position.x >= C(x) && route.Body.linearVelocity.x >= C(5f) &&
                    ActiveRemaining(support) >= .15f)
                {
                    route.Clear.Invoke(route.Player, null);
                    yield break;
                }
            }
            route.Failure = "Could not reach timed running launch X=" + x + " on " + supportName;
        }

        IEnumerator JumpTo(Route route, string targetName, float aimCell, float minCell, float maxCell,
            string hazardPrefix = null)
        {
            Collider2D target = GameObject.Find(targetName)?.GetComponent<Collider2D>();
            Assert.That(target, Is.Not.Null, targetName);
            if (route.Body.linearVelocity.x < C(4.5f))
            {
                yield return PrepareRunningJump(route);
                if (route.Failure != null) yield break;
            }
            int stable = 0;
            bool boarding = false;
            for (int q = 0; q < 145; q++)
            {
                float aim = targetName == "L06_FinalBridge" ? target.bounds.center.x : C(aimCell);
                float error = (aim - route.Body.position.x) / Unit;
                float horizontal = Mathf.Clamp(error * 2.4f - route.Body.linearVelocity.x / Unit * .55f, -1f, 1f);
                if (targetName == "L06_FinalBridge" && boarding) horizontal = 1f;
                bool jump = q == 0;
                route.Input.Invoke(route.Player, new object[] { horizontal, jump, false });
                yield return new WaitForFixedUpdate();
                if (route.Body == null) { route.Failure = "Player destroyed before " + targetName; yield break; }
                bool contact = Supported(route, target, out float margin);
                if (targetName == "L06_FinalBridge" && contact) boarding = true;
                bool safe = route.Body.position.x >= C(minCell) && route.Body.position.x <= C(maxCell);
                if (targetName == "L06_FinalBridge")
                    safe = margin >= Margin;
                stable = contact && safe && margin >= Margin ? stable + 1 : 0;
                Record(route, "Jump_" + targetName, q, horizontal, jump, false, target, contact, stable, margin);
                if (Dying(route) || TouchesHazard(route, hazardPrefix))
                {
                    route.Failure = "Hazard/death before " + targetName + " after " + route.LastSupport;
                    yield break;
                }
                if (stable >= 3)
                {
                    route.LastSupport = targetName;
                    route.Clear.Invoke(route.Player, null);
                    yield break;
                }
                if (Transitioning(route)) { route.Failure = "Exit before " + targetName; yield break; }
            }
            route.Failure = "No stable " + targetName + " landing after " + route.LastSupport;
        }

        IEnumerator PrepareRunningJump(Route route)
        {
            Collider2D support = GameObject.Find(route.LastSupport)?.GetComponent<Collider2D>();
            Assert.That(support, Is.Not.Null, route.LastSupport);
            for (int q = 0; q < 20; q++)
            {
                route.Input.Invoke(route.Player, new object[] { 1f, false, false });
                yield return new WaitForFixedUpdate();
                if (route.Body == null) { route.Failure = "Player destroyed during running launch"; yield break; }
                bool contact = Supported(route, support, out float margin);
                Record(route, "Runup_" + route.LastSupport, q, 1f, false, false,
                    support, contact, contact ? 1 : 0, margin);
                if (Dying(route) || !contact)
                {
                    route.Failure = "Lost " + route.LastSupport + " during running launch";
                    yield break;
                }
                if (route.Body.linearVelocity.x >= C(4.5f)) yield break;
            }
            route.Failure = "Could not build running jump from " + route.LastSupport;
        }

        IEnumerator WaitForTimedCycle(Route route, string name)
        {
            Collider2D timed = GameObject.Find(name)?.GetComponent<Collider2D>();
            Collider2D support = GameObject.Find("ExitRun")?.GetComponent<Collider2D>();
            Assert.That(timed, Is.Not.Null);
            for (int q = 0; q < 400; q++)
            {
                route.Input.Invoke(route.Player, new object[] { 0f, false, false });
                yield return new WaitForFixedUpdate();
                bool contact = Supported(route, support, out float margin);
                Record(route, "WaitCycle", q, 0f, false, false, support, contact, contact ? 1 : 0, margin);
                if (!contact || Dying(route)) { route.Failure = "Lost safe ExitRun while waiting for D"; yield break; }
                if (timed.enabled && ActiveRemaining(timed) > 4.8f)
                {
                    route.Clear.Invoke(route.Player, null);
                    yield break;
                }
            }
            route.Failure = "TimedD did not start a fresh cycle";
        }

        IEnumerator WaitForMover(Route route, string name, float nearCell)
        {
            Collider2D mover = GameObject.Find(name)?.GetComponent<Collider2D>();
            Collider2D support = GameObject.Find("HighExit")?.GetComponent<Collider2D>();
            Assert.That(mover, Is.Not.Null);
            float previous = float.NaN;
            for (int q = 0; q < 400; q++)
            {
                route.Input.Invoke(route.Player, new object[] { 0f, false, false });
                yield return new WaitForFixedUpdate();
                bool contact = Supported(route, support, out float margin);
                Record(route, "WaitFinalBridge", q, 0f, false, false, mover, contact, contact ? 1 : 0, margin);
                if (!contact || Dying(route)) { route.Failure = "Lost HighExit before FinalBridge"; yield break; }
                float center = mover.bounds.center.x;
                if (center <= C(nearCell) && !float.IsNaN(previous) && center <= previous)
                {
                    route.Clear.Invoke(route.Player, null);
                    yield break;
                }
                previous = center;
            }
            route.Failure = "FinalBridge never returned to HighExit";
        }

        IEnumerator RideMover(Route route, string name, float farCell)
        {
            Collider2D mover = GameObject.Find(name)?.GetComponent<Collider2D>();
            Assert.That(mover, Is.Not.Null);
            float start = mover.bounds.center.x;
            float offset = route.Body.position.x - start;
            int stable = 0;
            for (int q = 0; q < 350; q++)
            {
                route.Input.Invoke(route.Player, new object[] { 0f, false, false });
                yield return new WaitForFixedUpdate();
                bool contact = Supported(route, mover, out float margin);
                float drift = Mathf.Abs(route.Body.position.x - mover.bounds.center.x - offset);
                stable = contact && margin >= Margin && drift <= C(.65f) ? stable + 1 : 0;
                Record(route, "RideFinalBridge", q, 0f, false, false, mover, contact, stable, margin);
                if (Dying(route) || !contact || drift > C(.65f))
                {
                    route.Failure = "Lost FinalBridge carry or drift exceeded .65 cell";
                    yield break;
                }
                if (mover.bounds.center.x >= C(farCell) && mover.bounds.center.x - start >= C(5.5f) && stable >= 3)
                {
                    route.Clear.Invoke(route.Player, null);
                    yield break;
                }
            }
            route.Failure = "FinalBridge did not carry player far enough";
        }

        IEnumerator RunToMoverEdge(Route route, string name)
        {
            Collider2D mover = GameObject.Find(name)?.GetComponent<Collider2D>();
            Assert.That(mover, Is.Not.Null);
            for (int q = 0; q < 35; q++)
            {
                route.Input.Invoke(route.Player, new object[] { 1f, false, false });
                yield return new WaitForFixedUpdate();
                bool contact = Supported(route, mover, out float margin);
                float offset = route.Body.position.x - mover.bounds.center.x;
                Record(route, "RunToFinalBridgeEdge", q, 1f, false, false, mover,
                    contact, contact ? 1 : 0, margin);
                if (Dying(route) || !contact || margin < Margin)
                {
                    route.Failure = "Lost safe FinalBridge while staging final jump";
                    yield break;
                }
                if (mover.bounds.center.x >= C(34.2f) && offset >= C(.4f) &&
                    route.Body.linearVelocity.x >= C(3f))
                {
                    route.Clear.Invoke(route.Player, null);
                    yield break;
                }
            }
            route.Failure = "Could not reach FinalBridge right edge before return";
        }

        IEnumerator CrossLaser(Route route, string buttonName, string laserName, float beyondCell)
        {
            Collider2D floor = GameObject.Find(route.Level == 4 ? "SecondLaserRun" :
                "SecondLaserRun")?.GetComponent<Collider2D>();
            Collider2D button = GameObject.Find(buttonName)?.GetComponent<Collider2D>();
            Collider2D beam = GameObject.Find(laserName + "_Beam")?.GetComponent<Collider2D>();
            Collider2D baseShape = GameObject.Find(laserName + "_Base")?.GetComponent<Collider2D>();
            Assert.That(floor, Is.Not.Null);
            Assert.That(button, Is.Not.Null);
            Assert.That(beam, Is.Not.Null);
            Assert.That(baseShape, Is.Not.Null);
            bool touched = false;
            bool disabled = false;
            bool jumped = false;
            for (int q = 0; q < 230; q++)
            {
                bool jump = !jumped && route.Body.position.x >= baseShape.bounds.center.x - C(1.1f);
                if (jump)
                {
                    if (!disabled)
                    {
                        route.Failure = "Button did not disable " + laserName + " before emitter";
                        yield break;
                    }
                    jumped = true;
                }
                float horizontal = route.Body.position.x < C(beyondCell) ? 1f : -1f;
                route.Input.Invoke(route.Player, new object[] { horizontal, jump, false });
                yield return new WaitForFixedUpdate();
                if (route.Body == null) { route.Failure = "Player destroyed at " + laserName; yield break; }
                touched |= route.Shape.IsTouching(button);
                disabled |= !beam.enabled && baseShape.enabled;
                bool contact = Supported(route, floor, out float margin);
                Record(route, "Cross_" + laserName, q, horizontal, jump, false, floor, contact,
                    contact ? 1 : 0, margin);
                if (Dying(route) || (beam.enabled && route.Shape.IsTouching(beam)) ||
                    (baseShape.enabled && route.Shape.IsTouching(baseShape)))
                {
                    route.Failure = "Hit live " + laserName;
                    yield break;
                }
                if (route.Body.position.x >= C(beyondCell))
                {
                    if (!touched || !disabled || !jumped || beam.enabled || !baseShape.enabled || !contact)
                        route.Failure = "Crossed " + laserName + " without button/disabled contact";
                    route.LastSupport = "SecondLaserRun";
                    route.Clear.Invoke(route.Player, null);
                    yield break;
                }
            }
            route.Failure = "Could not cross " + laserName;
        }

        IEnumerator DashTimedPit(Route route)
        {
            Collider2D landing = GameObject.Find("FinalRun")?.GetComponent<Collider2D>();
            Assert.That(landing, Is.Not.Null);
            FieldInfo dashRemaining = route.Player.GetType().GetField("dashRemaining",
                BindingFlags.Instance | BindingFlags.NonPublic);
            bool jumped = false;
            bool dashed = false;
            bool activated = false;
            int stable = 0;
            for (int q = 0; q < 150; q++)
            {
                float x = route.Body.position.x;
                bool jump = !jumped;
                bool dash = jumped && !dashed && x >= C(30.5f);
                jumped |= jump;
                dashed |= dash;
                float horizontal = x < C(37.2f) ? 1f : x > C(37.8f) ? -1f : 0f;
                route.Input.Invoke(route.Player, new object[] { horizontal, jump, dash });
                yield return new WaitForFixedUpdate();
                if (route.Body == null) { route.Failure = "Player destroyed over second L05 pit"; yield break; }
                if (dash && dashRemaining != null)
                    activated |= (float)dashRemaining.GetValue(route.Player) > 0f;
                bool contact = Supported(route, landing, out float margin);
                stable = contact && margin >= Margin && route.Body.position.x >= C(35.425f) &&
                    route.Body.position.x <= C(40.575f) ? stable + 1 : 0;
                Record(route, "SecondTimedPit", q, horizontal, jump, dash,
                    landing, contact, stable, margin);
                if (Dying(route) || TouchesHazard(route, "L05_SecondPitSpikes"))
                {
                    route.Failure = "Death on second L05 pit";
                    yield break;
                }
                if (stable >= 3)
                {
                    if (!jumped || !dashed || !activated)
                        route.Failure = "Second L05 pit landed without jump and active dash";
                    route.LastSupport = "FinalRun";
                    route.Clear.Invoke(route.Player, null);
                    yield break;
                }
            }
            route.Failure = "No stable FinalRun landing after second L05 pit";
        }

        IEnumerator DashPit(Route route)
        {
            Collider2D landing = GameObject.Find("FinalRun")?.GetComponent<Collider2D>();
            Assert.That(landing, Is.Not.Null);
            FieldInfo dashRemaining = route.Player.GetType().GetField("dashRemaining",
                BindingFlags.Instance | BindingFlags.NonPublic);
            bool jumped = false;
            bool dashed = false;
            bool activated = false;
            int stable = 0;
            for (int q = 0; q < 230; q++)
            {
                float x = route.Body.position.x;
                bool jump = !jumped && x >= C(30.25f);
                bool dash = jumped && !dashed && x >= C(33f);
                jumped |= jump;
                dashed |= dash;
                float horizontal = x < C(41.2f) ? 1f : x > C(41.8f) ? -1f : 0f;
                route.Input.Invoke(route.Player, new object[] { horizontal, jump, dash });
                yield return new WaitForFixedUpdate();
                if (route.Body == null) { route.Failure = "Player destroyed over second L07 pit"; yield break; }
                if (dash && dashRemaining != null)
                    activated |= (float)dashRemaining.GetValue(route.Player) > 0f;
                bool contact = Supported(route, landing, out float margin);
                stable = contact && margin >= Margin && route.Body.position.x >= C(37.425f) &&
                    route.Body.position.x <= C(43.575f) ? stable + 1 : 0;
                Record(route, "SecondDashPit", q, horizontal, jump, dash, landing, contact, stable, margin);
                if (Dying(route) || TouchesHazard(route, "L07_SecondPitSpikes"))
                {
                    route.Failure = "Death on second L07 pit";
                    yield break;
                }
                if (stable >= 3)
                {
                    if (!jumped || !dashed || !activated)
                        route.Failure = "Second L07 pit landed without jump and active dash";
                    route.LastSupport = "FinalRun";
                    route.Clear.Invoke(route.Player, null);
                    yield break;
                }
            }
            route.Failure = "No stable FinalRun landing after second L07 pit";
        }

        IEnumerator WalkToBed(Route route)
        {
            Collider2D bed = GameObject.Find("BedExit")?.GetComponent<Collider2D>();
            Assert.That(bed, Is.Not.Null);
            bool touched = false;
            for (int q = 0; q < 160; q++)
            {
                float horizontal = route.Body.position.x < bed.bounds.center.x ? 1f : 0f;
                route.Input.Invoke(route.Player, new object[] { horizontal, false, false });
                yield return new WaitForFixedUpdate();
                if (route.Body == null) { route.Failure = "Player destroyed before final bed"; yield break; }
                touched |= route.Shape.IsTouching(bed);
                Record(route, "FinalBed", q, horizontal, false, false, bed, touched, touched ? 1 : 0, float.NaN);
                if (Dying(route)) { route.Failure = "Died before final bed"; yield break; }
                if (Transitioning(route) || !(bool)route.InDream.GetValue(route.Game)) break;
            }
            route.Clear.Invoke(route.Player, null);
            float deadline = Time.realtimeSinceStartup + 3f;
            while ((bool)route.InDream.GetValue(route.Game) && Time.realtimeSinceStartup < deadline)
                yield return null;
            if (!touched || (bool)route.InDream.GetValue(route.Game))
                route.Failure = "Final BedExit was not completed after " + route.LastSupport;
        }

        static bool TouchesHazard(Route route, string prefix)
        {
            if (prefix == null) return false;
            Collider2D exact = GameObject.Find(prefix)?.GetComponent<Collider2D>();
            if (exact != null && exact.enabled && route.Shape.IsTouching(exact)) return true;
            for (int q = 1; q <= 30; q++)
            {
                Collider2D part = GameObject.Find(prefix + "_" + q)?.GetComponent<Collider2D>();
                if (part == null) break;
                if (part.enabled && route.Shape.IsTouching(part)) return true;
            }
            return false;
        }

        static float ActiveRemaining(Collider2D target)
        {
            MonoBehaviour mechanism = FindComponent(target.gameObject, "Sleepwalking.SleepwalkingMechanism");
            if (mechanism == null) return float.NaN;
            Type type = mechanism.GetType();
            FieldInfo elapsed = type.GetField("elapsed", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo cycle = type.GetField("CycleSeconds");
            FieldInfo active = type.GetField("ActiveSeconds");
            return (float)active.GetValue(mechanism) -
                Mathf.Repeat((float)elapsed.GetValue(mechanism), (float)cycle.GetValue(mechanism));
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

        static bool Dying(Route route) => (bool)route.Dying.GetValue(route.Game);
        static bool Transitioning(Route route) => (bool)route.Transitioning.GetValue(route.Game);

        static void Record(Route route, string phase, int step, float horizontal, bool jump, bool dash,
            Collider2D target, bool contact, int stable, float margin)
        {
            route.Trace.Add(string.Format(CultureInfo.InvariantCulture,
                "{0},{1},{2},{3},{4:F3},{5:F3},{6:F3},{7:F3},{8:F3},{9:F3},{10},{11},{12},{13},{14},{15:F3},{16},{17}",
                route.Level, route.Profile, phase, step, Time.fixedTime, route.Body.position.x,
                route.Body.position.y, route.Body.linearVelocity.x, route.Body.linearVelocity.y,
                horizontal, jump, dash, target != null ? target.name : "", contact, stable, margin,
                Dying(route), Transitioning(route)));
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
