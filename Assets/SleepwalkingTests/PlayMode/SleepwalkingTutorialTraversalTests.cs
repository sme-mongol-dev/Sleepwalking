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
    public sealed class SleepwalkingTutorialTraversalTests
    {
        const float Unit = 32f;
        const float Margin = .15f * Unit;
        static float C(float cells) => cells * Unit;

        sealed class Route
        {
            public string Name;
            public MonoBehaviour Game;
            public MonoBehaviour Player;
            public Rigidbody2D Body;
            public Collider2D Shape;
            public Collider2D Bed;
            public MethodInfo Input;
            public MethodInfo Clear;
            public FieldInfo Dying;
            public FieldInfo Wall;
            public FieldInfo Grounded;
            public FieldInfo DashRemaining;
            public PropertyInfo InDream;
            public readonly List<string> Trace = new List<string>
            {
                "route,phase,step,time,x,y,vx,vy,horizontal,jump,dash,wall,dying,bedTouch"
            };
        }

        [UnityTest]
        public IEnumerator TutorialOneNeedsWallJumpAndBlocksDash()
        {
            Route route = null;
            yield return Start(0, "T01", value => route = value);
            Collider2D wall = GameObject.Find("GripWall")?.GetComponent<Collider2D>();
            Collider2D ledge = GameObject.Find("ExitLedge")?.GetComponent<Collider2D>();
            Assert.That(wall, Is.Not.Null);
            Assert.That(ledge, Is.Not.Null);
            Assert.That((bool)route.Player.GetType().GetField("DashEnabled").GetValue(route.Player), Is.False);

            yield return Step(route, "BlockedDash", 0, 1f, false, true);
            Assert.That((float)route.DashRemaining.GetValue(route.Player), Is.Zero);
            for (int q = 0; q < 150 && route.Body.position.x < C(9.6f); q++)
                yield return Step(route, "RunUnderWall", q, 1f, false, false);
            Assert.That(route.Body.position.x, Is.InRange(C(9.6f), C(10.6f)));
            for (int q = 0; q < 55; q++)
            {
                if (route.Body.position.x <= C(9.45f) && route.Body.linearVelocity.x <= -C(4f)) break;
                yield return Step(route, "ReverseOnFloor", q, -1f, false, false);
            }
            Assert.That(route.Body.position.x, Is.LessThanOrEqualTo(C(9.45f)));
            Assert.That(route.Body.linearVelocity.x, Is.LessThanOrEqualTo(-C(4f)));

            bool wallContact = false;
            yield return Step(route, "JumpTowardWall", 0, -1f, true, false);
            for (int q = 0; q < 80; q++)
            {
                wallContact = (int)route.Wall.GetValue(route.Player) != 0 &&
                    !(bool)route.Grounded.GetValue(route.Player) && route.Shape.IsTouching(wall);
                if (wallContact) break;
                yield return Step(route, "GrabWall", q, -1f, false, false);
                if (Dead(route)) break;
            }
            Assert.That(wallContact, Is.True, "Tutorial1 did not physically grab GripWall");
            float beforeY = route.Body.linearVelocity.y;
            yield return Step(route, "WallJump", 0, 1f, true, false);
            bool wallJump = route.Body.linearVelocity.y > beforeY + C(2f) &&
                route.Body.linearVelocity.x > 0f;
            Assert.That(wallJump, Is.True, "Tutorial1 wall jump did not activate");

            int stable = 0;
            for (int q = 0; q < 150; q++)
            {
                yield return Step(route, "ToExitLedge", q, 1f, false, false);
                bool contact = Supported(route, ledge, out float margin);
                stable = contact && margin >= Margin ? stable + 1 : 0;
                if (stable >= 3 || Dead(route)) break;
            }
            Assert.That(stable, Is.GreaterThanOrEqualTo(3), "Tutorial1 missed ExitLedge");
            bool bedTouch = false;
            for (int q = 0; q < 140; q++)
            {
                yield return Step(route, "ToBed", q, 1f, false, false);
                bedTouch |= route.Shape != null && route.Shape.IsTouching(route.Bed);
                if (bedTouch || Dead(route)) break;
            }
            route.Clear.Invoke(route.Player, null);
            Save(route);
            Assert.That(bedTouch, Is.True, "Tutorial1 did not reach BedExit");
            yield return WaitForRoom(route);
            Assert.That((int)route.Game.GetType().GetProperty("TutorialStage").GetValue(route.Game),
                Is.EqualTo(1));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TutorialTwoNeedsDashAndVisibleSpikesRetry()
        {
            foreach (string mode in new[] { "Plain", "WallJump", "Dash" })
            {
                Route route = null;
                yield return Start(1, "T02_" + mode, value => route = value);
                Collider2D landing = GameObject.Find("FinalRun")?.GetComponent<Collider2D>();
                Assert.That(landing, Is.Not.Null);
                Collider2D[] spikes = GameObject.Find("Tutorial 2")
                    .GetComponentsInChildren<Collider2D>();
                bool dashRequested = false;
                bool dashActivated = false;
                bool wallJump = false;
                bool spikeTouch = false;
                bool bedTouch = false;
                bool died = false;
                int stable = 0;
                for (int q = 0; q < 120 && route.Body.position.x < C(6.35f); q++)
                    yield return Step(route, "RunToPit", q, 1f, false, false);
                Assert.That(route.Body.position.x, Is.InRange(C(6.35f), C(6.65f)));
                Assert.That(route.Body.linearVelocity.x, Is.GreaterThanOrEqualTo(C(4.5f)));

                bool jumped = false;
                for (int q = 0; q < 180; q++)
                {
                    float x = route.Body.position.x;
                    bool jump = !jumped;
                    float horizontal = 1f;
                    if (mode == "WallJump" && jumped && !wallJump && x >= C(7.3f))
                        horizontal = -1f;
                    if (mode == "WallJump" && jumped && !wallJump &&
                        (int)route.Wall.GetValue(route.Player) != 0 &&
                        !(bool)route.Grounded.GetValue(route.Player))
                    {
                        jump = true;
                        horizontal = 1f;
                        wallJump = true;
                    }
                    bool dash = mode == "Dash" && jumped && !dashRequested && x >= C(9f);
                    jumped = true;
                    dashRequested |= dash;
                    yield return Step(route, "Pit_" + mode, q, horizontal, jump, dash);
                    if (dash) dashActivated |= (float)route.DashRemaining.GetValue(route.Player) > 0f;
                    foreach (Collider2D spike in spikes)
                        if (spike != null && spike.name.StartsWith("Tutorial02_PitSpikes") &&
                            (route.Shape.IsTouching(spike) ||
                             (Dead(route) && route.Shape.bounds.Intersects(spike.bounds))))
                            spikeTouch = true;
                    bool contact = Supported(route, landing, out float margin);
                    stable = contact && margin >= Margin ? stable + 1 : 0;
                    bedTouch |= route.Shape.IsTouching(route.Bed);
                    died |= Dead(route);
                    if (stable >= 3 || bedTouch || died) break;
                }
                Save(route);
                if (mode == "Dash")
                {
                    Assert.That(dashActivated, Is.True, "Tutorial2 dash input did not activate");
                    Assert.That(stable, Is.GreaterThanOrEqualTo(3), "Tutorial2 dash missed FinalRun");
                    for (int q = 0; q < 140 && !bedTouch; q++)
                    {
                        yield return Step(route, "ToBed", q, 1f, false, false);
                        bedTouch |= route.Shape.IsTouching(route.Bed);
                        if (Dead(route)) break;
                    }
                    Save(route);
                    Assert.That(bedTouch, Is.True, "Tutorial2 dash did not reach BedExit");
                    yield return WaitForRoom(route);
                    Assert.That((int)route.Game.GetType().GetProperty("TutorialStage").GetValue(route.Game),
                        Is.EqualTo(2));
                }
                else
                {
                    if (mode == "WallJump")
                        Assert.That(wallJump, Is.True, "Wall-jump skip probe did not touch a wall");
                    Assert.That(stable, Is.Zero, mode + " crossed the dash pit without dash");
                    Assert.That(bedTouch, Is.False, mode + " reached the bed without dash");
                    Assert.That(spikeTouch && died, Is.True,
                        mode + " missed the visible spike contact and retry");
                    yield return WaitForRetry(route);
                }
                route.Clear?.Invoke(route.Player, null);
            }
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator CaptureThemedTutorialFrames()
        {
            for (int stage = 0; stage < 2; stage++)
            {
                Route route = null;
                yield return Start(stage, "T0" + (stage + 1), value => route = value);
                Transform level = GameObject.Find("Tutorial " + (stage + 1)).transform;
                Transform theme = level.Find("Dream style");
                Transform tiles = level.Find("Dream Tile Matrix");
                Transform background = theme?.Find("Distant Background Left");
                SpriteRenderer bedArt = route.Bed.transform.Find("Dream Bed Art")?.GetComponent<SpriteRenderer>();
                Assert.That(theme, Is.Not.Null, "Tutorial theme was not instantiated");
                Assert.That(tiles, Is.Not.Null, "Tutorial tiles were not generated");
                Assert.That(tiles.GetComponentInChildren<Renderer>().enabled, Is.True);
                Assert.That(background, Is.Not.Null, "Tutorial backdrop is missing");
                Assert.That(background.GetComponent<Renderer>().enabled, Is.True);
                Assert.That(bedArt, Is.Not.Null, "Tutorial bed art is missing");
                Assert.That(bedArt.enabled && bedArt.sprite != null, Is.True);

                Camera camera = (Camera)route.Game.GetType().GetField("GameCamera").GetValue(route.Game);
                Assert.That(camera, Is.Not.Null);
                foreach (Vector2Int resolution in new[] { new Vector2Int(1280, 720),
                    new Vector2Int(1024, 768) })
                {
                    RenderTexture original = camera.targetTexture;
                    RenderTexture active = RenderTexture.active;
                    RenderTexture target = new RenderTexture(resolution.x, resolution.y, 24);
                    Texture2D image = new Texture2D(resolution.x, resolution.y, TextureFormat.RGBA32, false);
                    try
                    {
                        target.Create();
                        camera.targetTexture = target;
                        route.Game.GetType().GetMethod("ResetDreamCamera",
                            BindingFlags.Instance | BindingFlags.NonPublic).Invoke(route.Game, null);
                        foreach (MonoBehaviour component in theme.GetComponents<MonoBehaviour>())
                            if (component != null && component.GetType().FullName == "Sleepwalking.Visual.DreamTheme")
                                component.GetType().GetMethod("FrameBackdrop").Invoke(component, new object[] { camera });
                        yield return null;
                        camera.Render();
                        RenderTexture.active = target;
                        image.ReadPixels(new Rect(0, 0, resolution.x, resolution.y), 0, 0);
                        image.Apply();
                        string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                        string label = resolution.x == 1280 ? "16x9" : "4x3";
                        string path = Path.Combine(root, "Docs", "QA", "tutorial-runtime-T0" +
                            (stage + 1) + "-" + label + ".png");
                        File.WriteAllBytes(path, image.EncodeToPNG());
                        File.WriteAllText(Path.ChangeExtension(path, ".txt"),
                            "Theme=" + theme.name + " Tiles=" + tiles.GetComponentsInChildren<Renderer>().Length +
                            " Bed=" + bedArt.sprite.name + " Camera=" + camera.transform.position +
                            " Size=" + camera.orthographicSize + " Resolution=" + resolution);
                    }
                    finally
                    {
                        camera.targetTexture = original;
                        RenderTexture.active = active;
                        target.Release();
                        UnityEngine.Object.Destroy(target);
                        UnityEngine.Object.Destroy(image);
                    }
                }
                route.Clear.Invoke(route.Player, null);
            }
            LogAssert.NoUnexpectedReceived();
        }

        static IEnumerator Start(int stage, string name, Action<Route> finish)
        {
            AsyncOperation loading = SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
            while (!loading.isDone) yield return null;
            yield return null;
            MonoBehaviour game = FindComponent(GameObject.Find("Sleepwalking Game"),
                "Sleepwalking.SleepwalkingGame");
            Assert.That(game, Is.Not.Null);
            Type gameType = game.GetType();
            gameType.GetField("tutorialStage", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(game, stage);
            gameType.GetField("odradek", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(game, 1);
            Array profiles = (Array)gameType.GetField("MovementProfiles").GetValue(game);
            gameType.GetField("MovementProfile").SetValue(game, profiles.GetValue(1));
            gameType.GetMethod("StartDream").Invoke(game, null);
            PropertyInfo inDream = gameType.GetProperty("InDream");
            FieldInfo transitioning = gameType.GetField("transitioning",
                BindingFlags.Instance | BindingFlags.NonPublic);
            float deadline = Time.realtimeSinceStartup + 10f;
            while ((!((bool)inDream.GetValue(game)) || (bool)transitioning.GetValue(game)) &&
                Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That((bool)inDream.GetValue(game), Is.True);
            Assert.That(GameObject.Find("Tutorial " + (stage + 1)), Is.Not.Null);
            GameObject dreamer = GameObject.Find("Dreamer");
            MonoBehaviour player = FindComponent(dreamer, "Sleepwalking.SleepwalkingPlayer");
            Assert.That(player, Is.Not.Null);
            Route route = new Route
            {
                Name = name,
                Game = game,
                Player = player,
                Body = dreamer.GetComponent<Rigidbody2D>(),
                Shape = dreamer.GetComponent<Collider2D>(),
                Bed = GameObject.Find("BedExit")?.GetComponent<Collider2D>(),
                Input = player.GetType().GetMethod("SetSimulatedInput"),
                Clear = player.GetType().GetMethod("ClearSimulatedInput"),
                Dying = gameType.GetField("dying", BindingFlags.Instance | BindingFlags.NonPublic),
                Wall = player.GetType().GetField("wall", BindingFlags.Instance | BindingFlags.NonPublic),
                Grounded = player.GetType().GetField("grounded", BindingFlags.Instance | BindingFlags.NonPublic),
                DashRemaining = player.GetType().GetField("dashRemaining", BindingFlags.Instance | BindingFlags.NonPublic),
                InDream = inDream
            };
            Assert.That(route.Body, Is.Not.Null);
            Assert.That(route.Shape, Is.Not.Null);
            Assert.That(route.Bed, Is.Not.Null);
            Assert.That(route.Input, Is.Not.Null);
            Assert.That(route.Clear, Is.Not.Null);
            Assert.That(route.Dying, Is.Not.Null);
            Assert.That(route.Wall, Is.Not.Null);
            Assert.That(route.Grounded, Is.Not.Null);
            Assert.That(route.DashRemaining, Is.Not.Null);
            for (int q = 0; q < 3; q++) yield return new WaitForFixedUpdate();
            finish(route);
        }

        static IEnumerator Step(Route route, string phase, int step, float horizontal, bool jump, bool dash)
        {
            route.Input.Invoke(route.Player, new object[] { horizontal, jump, dash });
            yield return new WaitForFixedUpdate();
            if (route.Body != null)
                route.Trace.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0},{1},{2},{3:F3},{4:F3},{5:F3},{6:F3},{7:F3},{8:F3},{9},{10},{11},{12},{13}",
                    route.Name, phase, step, Time.fixedTime, route.Body.position.x,
                    route.Body.position.y, route.Body.linearVelocity.x, route.Body.linearVelocity.y,
                    horizontal, jump, dash, route.Wall.GetValue(route.Player),
                    Dead(route), route.Shape.IsTouching(route.Bed)));
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

        static bool Dead(Route route) => (bool)route.Dying.GetValue(route.Game);

        static IEnumerator WaitForRetry(Route route)
        {
            float deadline = Time.realtimeSinceStartup + 5f;
            while (Dead(route) && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(Dead(route), Is.False, "Tutorial2 retry did not finish");
            Assert.That((bool)route.InDream.GetValue(route.Game), Is.True);
            Assert.That(GameObject.Find("Tutorial 2"), Is.Not.Null);
            Assert.That(route.Body.position.x, Is.InRange(C(.5f), C(2f)));
        }

        static IEnumerator WaitForRoom(Route route)
        {
            float deadline = Time.realtimeSinceStartup + 5f;
            while ((bool)route.InDream.GetValue(route.Game) && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That((bool)route.InDream.GetValue(route.Game), Is.False);
            Assert.That(GameObject.Find("Waking room"), Is.Not.Null);
        }

        static void Save(Route route)
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string path = Path.Combine(root, "Docs", "QA", "tutorial-traversal-" + route.Name + ".csv");
            File.WriteAllLines(path, route.Trace);
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
