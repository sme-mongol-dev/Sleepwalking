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
    [TestFixture(0, 1)]
    [TestFixture(0, 100)]
    [TestFixture(1, 1)]
    [TestFixture(1, 100)]
    public sealed class SleepwalkingEighthLevelTests
    {
        readonly int profileIndex;
        readonly int odradek;
        const float Unit = 32f;
        const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

        public SleepwalkingEighthLevelTests(int profileIndex, int odradek)
        {
            this.profileIndex = profileIndex;
            this.odradek = odradek;
        }

        [UnityTest]
        public IEnumerator SwitchesShuttleAndExitAreReachableWithoutDeaths()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
            yield return null;
            Component game = GameObject.Find("Sleepwalking Game").GetComponent("SleepwalkingGame");
            Type gameType = game.GetType();
            var profiles = (Array)gameType.GetField("MovementProfiles").GetValue(game);
            gameType.GetField("MovementProfile").SetValue(game, profiles.GetValue(profileIndex));
            gameType.GetField("completedDreams", Private).SetValue(game, 7);
            gameType.GetField("tutorialStage", Private).SetValue(game, 2);
            gameType.GetField("odradek", Private).SetValue(game, odradek);
            gameType.GetMethod("StartDream").Invoke(game, null);
            float deadline = Time.realtimeSinceStartup + 10f;
            while ((bool)gameType.GetField("transitioning", Private).GetValue(game) && Time.realtimeSinceStartup < deadline)
                yield return null;

            GameObject level = GameObject.Find("Dream L08");
            Assert.That(level, Is.Not.Null);
            foreach (Component surface in level.GetComponentsInChildren<Component>())
                if (surface != null && surface.GetType().Name == "SleepwalkingTileSurface")
                {
                    Collider2D solid = surface.GetComponent<Collider2D>();
                    Assert.That(solid, Is.Not.Null, surface.name);
                    Assert.That(solid.enabled && !solid.isTrigger, Is.True, surface.name);
                }
            Capture(Camera.main, "L08-overview-profile-" + profileIndex + "-odradek-" + odradek);

            Component player = GameObject.Find("Dreamer").GetComponent("SleepwalkingPlayer");
            Type playerType = player.GetType();
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            Collider2D shape = player.GetComponent<Collider2D>();
            MethodInfo input = playerType.GetMethod("SetSimulatedInput");
            MethodInfo consumeInput = playerType.GetMethod("Update", Private);
            FieldInfo wall = playerType.GetField("wall", Private);
            FieldInfo dying = gameType.GetField("dying", Private);
            Collider2D ledge = GameObject.Find("SawSwitchLedge").GetComponent<Collider2D>();
            Collider2D shuttle = GameObject.Find("L08_Shuttle").GetComponent<Collider2D>();
            Collider2D switchLedge = GameObject.Find("LaserSwitchLedge").GetComponent<Collider2D>();
            Collider2D final = GameObject.Find("FinalRun").GetComponent<Collider2D>();
            Collider2D sawButton = GameObject.Find("L08_SawButton").GetComponent<Collider2D>();
            Collider2D laserButton = GameObject.Find("L08_LaserButton").GetComponent<Collider2D>();
            Collider2D saw = GameObject.Find("L08_Saws_Exit").GetComponent<Collider2D>();
            Collider2D entranceSaw = GameObject.Find("L08_Saws_Entrance").GetComponent<Collider2D>();
            Collider2D beam = GameObject.Find("L08_Laser_Beam").GetComponent<Collider2D>();
            Collider2D emitter = GameObject.Find("L08_Laser_Base").GetComponent<Collider2D>();
            bool sawPressed = false, laserPressed = false, carried = false;
            bool jumped = false, wallJumped = false, dashUsed = false;
            int stage = 0, stable = 0;
            List<string> trace = new List<string> { "step,stage,x,feet,vx,vy,wall,input,jump,dash,sawOff,beamOff" };
            try
            {
                for (int step = 0; step < 2400; step++)
                {
                    if (body == null || !(bool)gameType.GetProperty("InDream").GetValue(game)) break;
                    float x = body.position.x / Unit;
                    float feet = shape.bounds.min.y / Unit;
                    float vx = body.linearVelocity.x / Unit;
                    float vy = body.linearVelocity.y / Unit;
                    float horizontal = 0f;
                    bool jump = false, dash = false;
                    sawPressed |= shape.IsTouching(sawButton);
                    laserPressed |= shape.IsTouching(laserButton);

                    if (stage == 0)
                    {
                        horizontal = Steer(4.5f, x, vx);
                        if (!jumped && x >= 2.2f) jumped = jump = true;
                        if (jumped && !wallJumped && (int)wall.GetValue(player) != 0 && vy <= 0f &&
                            !(bool)playerType.GetProperty("IsGrounded").GetValue(player))
                            wallJumped = jump = true;
                        if (wallJumped && !dashUsed && vx > 0f && feet > 3.15f)
                            dashUsed = dash = true;
                        stable = Supported(shape, body, ledge) ? stable + 1 : 0;
                        if (stable >= 3 && sawPressed)
                        {
                            Assert.That(saw.enabled || entranceSaw.enabled, Is.False, "Plus must disable both saws.");
                            Assert.That(beam.enabled, Is.True, "Plus must leave the laser active.");
                            stage = 1; jumped = dashUsed = false; stable = 0;
                        }
                    }
                    else if (stage == 1)
                    {
                        horizontal = jumped && x >= 6.95f ? Steer(7.35f, x, vx) : 1f;
                        if (!jumped && x >= 4.7f) jumped = jump = true;
                        if (jumped && x > 6.8f && Supported(shape, body, ledge))
                        { stage = 2; jumped = false; }
                    }
                    else if (stage == 2)
                    {
                        horizontal = Steer(7.35f, x, vx);
                        float shuttleX = shuttle.transform.position.x / Unit;
                        if (shuttleX < 9.7f)
                        { stage = 3; jump = true; horizontal = 1f; }
                    }
                    else if (stage == 3)
                    {
                        horizontal = Steer(shuttle.transform.position.x / Unit, x, vx);
                        stable = Supported(shape, body, shuttle) ? stable + 1 : 0;
                        if (stable >= 3) { stage = 4; stable = 0; }
                    }
                    else if (stage == 4)
                    {
                        horizontal = Steer(shuttle.transform.position.x / Unit, x, vx);
                        carried |= Supported(shape, body, shuttle) && x > 12f;
                        if (carried && shuttle.transform.position.x / Unit > 14.5f)
                        { stage = 5; jump = true; horizontal = 1f; }
                    }
                    else if (stage == 5)
                    {
                        horizontal = Steer(18f, x, vx);
                        stable = Supported(shape, body, switchLedge) ? stable + 1 : 0;
                        if (stable >= 3 && laserPressed && !beam.enabled)
                        {
                            Assert.That(beam.enabled, Is.False, "Minus must disable the laser beam.");
                            Assert.That(emitter.enabled, Is.False, "The emitter must remain intangible.");
                            stage = 6; stable = 0;
                        }
                    }
                    else if (stage == 6)
                    {
                        horizontal = 1f;
                        if (x >= 20.35f) { stage = 7; jump = true; }
                    }
                    else if (stage == 7)
                    {
                        horizontal = Steer(23.4f, x, vx);
                        if (!dashUsed && feet > 7.01f) dashUsed = dash = true;
                        stable = Supported(shape, body, final) ? stable + 1 : 0;
                        if (stable >= 3) { stage = 8; Capture(Camera.main, "L08-exit-profile-" + profileIndex + "-odradek-" + odradek); }
                    }
                    else horizontal = 1f;

                    trace.Add(string.Format(CultureInfo.InvariantCulture,
                        "{0},{1},{2:F3},{3:F3},{4:F3},{5:F3},{6},{7:F2},{8},{9},{10},{11}",
                        step, stage, x, feet, vx, vy, wall.GetValue(player), horizontal, jump, dash, !saw.enabled, !beam.enabled));
                    input.Invoke(player, new object[] { horizontal, jump, dash });
                    // Batch mode can run several physics ticks before Update.
                    // Deliver a press before the next tick, at its recorded position.
                    if (jump || dash) consumeInput.Invoke(player, null);
                    yield return new WaitForFixedUpdate();
                    Assert.That((bool)dying.GetValue(game), Is.False, "Death in stage " + stage + " at " + x + "," + feet);
                }
                deadline = Time.realtimeSinceStartup + 5f;
                while ((bool)gameType.GetField("transitioning", Private).GetValue(game) && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.That(stage, Is.EqualTo(8), "Incomplete route; see L08 trace.");
                Assert.That(carried && sawPressed && laserPressed && dashUsed, Is.True);
                Assert.That((int)gameType.GetProperty("CompletedDreams").GetValue(game), Is.EqualTo(8));
                Assert.That((int)gameType.GetProperty("DreamDeaths").GetValue(game), Is.Zero);
                gameType.GetMethod("StartDream").Invoke(game, null);
                deadline = Time.realtimeSinceStartup + 5f;
                while ((bool)gameType.GetField("transitioning", Private).GetValue(game) && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.That(GameObject.Find("Dream L01"), Is.Not.Null, "L08 must wrap to a new cycle.");
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                Directory.CreateDirectory("Builds/QA");
                File.WriteAllLines("Builds/QA/L08-route-profile-" + profileIndex + "-odradek-" + odradek + ".csv", trace);
            }
        }

        static float Steer(float target, float x, float velocity) => Mathf.Clamp((target - x) * 3f - velocity * .55f, -1f, 1f);

        static bool Supported(Collider2D player, Rigidbody2D body, Collider2D platform) =>
            player.IsTouching(platform) && Mathf.Abs(player.bounds.min.y - platform.bounds.max.y) < .15f * Unit &&
            Mathf.Abs(body.linearVelocity.y) < .3f * Unit;

        static void Capture(Camera camera, string name)
        {
            RenderTexture previous = camera.targetTexture;
            RenderTexture active = RenderTexture.active;
            RenderTexture target = RenderTexture.GetTemporary(1280, 720, 24);
            Texture2D shot = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                shot.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                shot.Apply();
                Directory.CreateDirectory("Builds/QA");
                File.WriteAllBytes("Builds/QA/" + name + ".png", shot.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previous;
                RenderTexture.active = active;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.Destroy(shot);
            }
        }
    }
}
