using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Sleepwalking.Tests.PlayMode
{
    public sealed class MovementContractTests
    {
        [UnityTest]
        public IEnumerator AuthoredProfilesKeepFiveCellRunAndTwoPointSevenCellDash()
        {
            AsyncOperation loading = SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
            while (!loading.isDone) yield return null;
            yield return null;

            GameObject root = GameObject.Find("Sleepwalking Game");
            Assert.That(root, Is.Not.Null);
            MonoBehaviour game = null;
            foreach (MonoBehaviour component in root.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().Name == "SleepwalkingGame") game = component;
            Assert.That(game, Is.Not.Null);
            Type gameType = game.GetType();
            FieldInfo odradek = gameType.GetField("odradek", BindingFlags.Instance | BindingFlags.NonPublic);
            Array profiles = (Array)gameType.GetField("MovementProfiles").GetValue(game);
            Assert.That(profiles, Has.Length.EqualTo(3));

            const float unit = 32f;
            GameObject ground = new GameObject("Profile movement ground");
            ground.transform.position = new Vector3(0f, -.5f * unit, 0f);
            ground.AddComponent<BoxCollider2D>().size = new Vector2(100f * unit, unit);
            GameObject actor = new GameObject("Profile movement player");
            actor.transform.position = new Vector3(0f, .5f * unit, 0f);
            actor.transform.localScale = new Vector3(.55f * unit, unit, 1f);
            Rigidbody2D body = actor.AddComponent<Rigidbody2D>();
            actor.AddComponent<BoxCollider2D>();
            Type playerType = Type.GetType("Sleepwalking.SleepwalkingPlayer, Assembly-CSharp");
            Assert.That(playerType, Is.Not.Null);
            MonoBehaviour player = (MonoBehaviour)actor.AddComponent(playerType);
            playerType.GetField("WorldUnitsPerCell").SetValue(player, unit);
            MethodInfo applyProfile = playerType.GetMethod("ApplyProfile");
            MethodInfo reset = playerType.GetMethod("ResetAt");
            MethodInfo setInput = playerType.GetMethod("SetSimulatedInput");
            MethodInfo update = playerType.GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo dashRemaining = playerType.GetField("dashRemaining",
                BindingFlags.Instance | BindingFlags.NonPublic);

            int[] difficulties = { 1, 100 };
            foreach (int difficulty in difficulties)
            {
                odradek.SetValue(game, difficulty);
                float multiplier = (float)gameType.GetProperty("OdradekRunMultiplier").GetValue(game);
                for (int q = 0; q < profiles.Length; q++)
                {
                    ScriptableObject profile = (ScriptableObject)profiles.GetValue(q);
                    Assert.That(profile, Is.Not.Null);
                    Assert.That((float)profile.GetType().GetField("RunSpeed").GetValue(profile),
                        Is.EqualTo(5f).Within(.001f));
                    Assert.That((float)profile.GetType().GetField("DashDistance").GetValue(profile),
                        Is.EqualTo(2.7f).Within(.001f));
                    playerType.GetField("Profile").SetValue(player, profile);
                    applyProfile.Invoke(player, null);

                    reset.Invoke(player, new object[] { new Vector2(0f, .5f * unit) });
                    setInput.Invoke(player, new object[] { 1f, false, false });
                    for (int step = 0; step < 20; step++) yield return new WaitForFixedUpdate();
                    float run = body.linearVelocity.x / unit;
                    Assert.That(run, Is.EqualTo(5f * multiplier).Within(.02f),
                        "Profile " + q + " run speed at Odradek " + difficulty);

                    float groundAcceleration = (float)playerType.GetField("GroundAcceleration").GetValue(player);
                    float airAcceleration = (float)playerType.GetProperty("AirAcceleration").GetValue(player);
                    Assert.That(airAcceleration / groundAcceleration, Is.EqualTo(.27f).Within(.0001f));
                    reset.Invoke(player, new object[] { new Vector2(0f, 10f * unit) });
                    body.gravityScale = 0f;
                    body.linearVelocity = new Vector2(2.3f * unit, 0f);
                    setInput.Invoke(player, new object[] { -1f, false, false });
                    yield return new WaitForFixedUpdate();
                    float airSpeed = body.linearVelocity.x / unit;
                    Assert.That(airSpeed, Is.EqualTo(2.3f - airAcceleration * Time.fixedDeltaTime / unit)
                        .Within(.02f), "Profile " + q + " air acceleration at Odradek " + difficulty);
                    Assert.That(airSpeed, Is.GreaterThan(0f), "Air control reversed velocity instantly.");

                    reset.Invoke(player, new object[] { new Vector2(0f, 10f * unit) });
                    body.gravityScale = 0f;
                    setInput.Invoke(player, new object[] { 1f, false, true });
                    update.Invoke(player, null);
                    setInput.Invoke(player, new object[] { -1f, false, false });
                    float dashStart = body.position.x;
                    int steps = 0;
                    while ((float)dashRemaining.GetValue(player) > 0f && steps < 20)
                    {
                        yield return new WaitForFixedUpdate();
                        Assert.That(body.linearVelocity.x, Is.GreaterThan(0f),
                            "Profile " + q + " dash reversed under opposite A/D input.");
                        steps++;
                    }
                    Assert.That(steps, Is.InRange(1, 20));
                    float dash = (body.position.x - dashStart) / unit;
                    Assert.That(dash, Is.EqualTo(2.7f).Within(.04f),
                        "Profile " + q + " dash distance at Odradek " + difficulty);
                    Debug.Log("SW_MOVEMENT profile=" + q + " odradek=" + difficulty +
                        " runCellsPerSecond=" + run.ToString("F3") +
                        " airRatio=" + (airAcceleration / groundAcceleration).ToString("F3") +
                        " airAfterOneStep=" + airSpeed.ToString("F3") +
                        " dashCells=" + dash.ToString("F3"));
                }
            }

            UnityEngine.Object.Destroy(actor);
            UnityEngine.Object.Destroy(ground);
        }

        [UnityTest]
        public IEnumerator RunAirDashAndWallJumpRespectCellContract()
        {
            const float unit = 32f;
            GameObject ground = new GameObject("Movement contract ground");
            ground.transform.position = new Vector3(0f, -.5f * unit, 0f);
            ground.AddComponent<BoxCollider2D>().size = new Vector2(100f * unit, unit);

            GameObject actor = new GameObject("Movement contract player");
            actor.transform.position = new Vector3(0f, .5f * unit, 0f);
            actor.transform.localScale = new Vector3(.55f * unit, unit, 1f);
            Rigidbody2D body = actor.AddComponent<Rigidbody2D>();
            actor.AddComponent<BoxCollider2D>();
            Type playerType = Type.GetType("Sleepwalking.SleepwalkingPlayer, Assembly-CSharp");
            Type profileType = Type.GetType("Sleepwalking.SleepwalkingMovementProfile, Assembly-CSharp");
            Assert.That(playerType, Is.Not.Null);
            Assert.That(profileType, Is.Not.Null);
            MonoBehaviour player = (MonoBehaviour)actor.AddComponent(playerType);
            ScriptableObject profile = ScriptableObject.CreateInstance(profileType);
            profileType.GetField("RunSpeed").SetValue(profile, 9.4f);
            profileType.GetField("DashDistance").SetValue(profile, 4f);
            profileType.GetField("DashSeconds").SetValue(profile, .17f);
            playerType.GetField("Profile").SetValue(player, profile);
            playerType.GetField("WorldUnitsPerCell").SetValue(player, unit);
            playerType.GetMethod("ApplyProfile").Invoke(player, null);
            Assert.That((float)playerType.GetField("RunSpeed").GetValue(player) / unit,
                Is.EqualTo(5f).Within(.001f));
            Assert.That((float)playerType.GetField("DashDistance").GetValue(player) / unit,
                Is.EqualTo(2.7f).Within(.001f));
            MethodInfo setInput = playerType.GetMethod("SetSimulatedInput");

            setInput.Invoke(player, new object[] { 1f, false, false });
            for (int q = 0; q < 20; q++) yield return new WaitForFixedUpdate();
            float groundSpeed = body.linearVelocity.x / unit;
            float effectiveRunSpeed = (float)playerType.GetProperty("EffectiveRunSpeed").GetValue(player) / unit;
            Debug.Log("SW_MOVEMENT groundSpeed=" + groundSpeed.ToString("F3") +
                " grounded=" + playerType.GetProperty("IsGrounded").GetValue(player));
            Assert.That(effectiveRunSpeed, Is.LessThanOrEqualTo(5f));
            Assert.That(groundSpeed, Is.EqualTo(effectiveRunSpeed).Within(.02f),
                "Running must reach the capped speed after the Odradek multiplier.");

            body.position = new Vector2(0f, 10f * unit);
            body.linearVelocity = new Vector2(2.3f * unit, 0f);
            body.gravityScale = 0f;
            setInput.Invoke(player, new object[] { -1f, false, false });
            float groundAcceleration = (float)playerType.GetField("GroundAcceleration").GetValue(player);
            float airAcceleration = (float)playerType.GetProperty("AirAcceleration").GetValue(player);
            Assert.That(airAcceleration / groundAcceleration, Is.EqualTo(.27f).Within(.0001f));
            for (int q = 0; q < 6; q++) yield return new WaitForFixedUpdate();
            float airSpeedBeforeDash = body.linearVelocity.x / unit;
            Assert.That(airSpeedBeforeDash,
                Is.EqualTo(2.3f - 6f * airAcceleration * Time.fixedDeltaTime / unit).Within(.02f),
                "A/D must change airborne velocity with 27% of ground acceleration.");
            Assert.That(airSpeedBeforeDash, Is.GreaterThan(0f),
                "Airborne velocity reversed before the bounded acceleration could decelerate it.");

            setInput.Invoke(player, new object[] { -1f, false, true });
            MethodInfo update = playerType.GetMethod("Update",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo dashRemaining = playerType.GetField("dashRemaining",
                BindingFlags.Instance | BindingFlags.NonPublic);
            update.Invoke(player, null);
            float dashStartX = body.position.x;
            int steps = 0;
            while ((float)dashRemaining.GetValue(player) > 0f && steps < 20)
            {
                yield return new WaitForFixedUpdate();
                Assert.That(body.linearVelocity.x, Is.GreaterThan(0f),
                    "Opposite A/D input reversed an active dash.");
                steps++;
            }
            Assert.That(steps, Is.InRange(1, 20));
            float dashCells = (body.position.x - dashStartX) / unit;
            Debug.Log("SW_MOVEMENT runCellsPerSecond=" + groundSpeed.ToString("F3") +
                " dashCells=" + dashCells.ToString("F3") + " dashSteps=" + steps);
            Assert.That(dashCells, Is.EqualTo(2.7f).Within(.04f),
                "Full dash must travel 2.7 cells even when duration spans partial physics steps.");
            yield return new WaitForFixedUpdate();
            Assert.That(body.linearVelocity.x / unit,
                Is.EqualTo(airSpeedBeforeDash - airAcceleration * Time.fixedDeltaTime / unit).Within(.02f),
                "After dash, air control must continue from the pre-dash momentum.");

            GameObject wall = new GameObject("Movement contract wall");
            wall.transform.position = new Vector3(.795f * unit, 10f * unit, 0f);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(unit, 6f * unit);
            playerType.GetMethod("ResetAt").Invoke(player, new object[] { new Vector2(0f, 10f * unit) });
            body.gravityScale = 0f;
            setInput.Invoke(player, new object[] { 1f, true, false });
            update.Invoke(player, null);
            yield return new WaitForFixedUpdate();
            Assert.That(body.linearVelocity.x, Is.LessThan(-2f * unit),
                "Wall jump must apply its own impulse away from the wall.");

            wall.transform.position = new Vector3(.795f * unit, .5f * unit, 0f);
            playerType.GetMethod("ResetAt").Invoke(player, new object[] { new Vector2(0f, .5f * unit) });
            setInput.Invoke(player, new object[] { 1f, false, false });
            yield return new WaitForFixedUpdate();
            setInput.Invoke(player, new object[] { 1f, false, true });
            update.Invoke(player, null);
            steps = 0;
            while ((float)dashRemaining.GetValue(player) > 0f && steps < 20)
            {
                yield return new WaitForFixedUpdate();
                steps++;
            }
            float wallBoundary = wall.GetComponent<Collider2D>().bounds.min.x;
            float playerRight = actor.GetComponent<Collider2D>().bounds.max.x;
            Assert.That(playerRight, Is.LessThanOrEqualTo(wallBoundary + .05f * unit),
                "Dash passed through the wall after its distance was capped.");

            playerType.GetField("Profile").SetValue(player, null);
            playerType.GetMethod("ApplyProfile").Invoke(player, null);
            Assert.That((float)playerType.GetField("RunSpeed").GetValue(player) / unit,
                Is.EqualTo(5f).Within(.001f), "Fallback run speed");
            Assert.That((float)playerType.GetField("DashDistance").GetValue(player) / unit,
                Is.EqualTo(2.7f).Within(.001f), "Fallback dash distance");

            UnityEngine.Object.Destroy(wall);
            UnityEngine.Object.Destroy(actor);
            UnityEngine.Object.Destroy(ground);
            UnityEngine.Object.Destroy(profile);
        }
    }
}
