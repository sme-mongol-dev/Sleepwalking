using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Sleepwalking.Tests.PlayMode
{
    public sealed class GridPhysicsTests
    {
        [UnityTest]
        public IEnumerator L05TimedPlatformsHaveSevenSecondCycle()
        {
            AsyncOperation loading = SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
            while (!loading.isDone) yield return null;
            yield return null;

            GameObject root = GameObject.Find("Sleepwalking Game");
            MonoBehaviour game = null;
            foreach (MonoBehaviour component in root.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().Name == "SleepwalkingGame") game = component;
            Assert.That(game, Is.Not.Null);
            Type gameType = game.GetType();
            gameType.GetField("completedDreams", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(game, 4);
            gameType.GetMethod("StartDream").Invoke(game, null);
            float deadline = Time.realtimeSinceStartup + 10f;
            while (!(bool)gameType.GetProperty("InDream").GetValue(game) && Time.realtimeSinceStartup < deadline)
                yield return null;
            while ((bool)gameType.GetField("transitioning", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game))
                yield return null;

            string[] names = { "L05_TimedA", "L05_TimedB", "L05_TimedC" };
            float[] phases = { 0f, .3f, 0f };
            for (int q = 0; q < names.Length; q++)
            {
                GameObject platform = GameObject.Find(names[q]);
                Assert.That(platform, Is.Not.Null);
                MonoBehaviour mechanism = null;
                foreach (MonoBehaviour component in platform.GetComponents<MonoBehaviour>())
                    if (component != null && component.GetType().Name == "SleepwalkingMechanism") mechanism = component;
                Assert.That(mechanism, Is.Not.Null);
                Type type = mechanism.GetType();
                Assert.That((float)type.GetField("ActiveSeconds").GetValue(mechanism), Is.EqualTo(5.5f).Within(.001f));
                Assert.That((float)type.GetField("CycleSeconds").GetValue(mechanism), Is.EqualTo(7f).Within(.001f));
                Assert.That((float)type.GetField("PhaseSeconds").GetValue(mechanism), Is.EqualTo(phases[q]).Within(.001f));
            }

            GameObject first = GameObject.Find(names[0]);
            MonoBehaviour firstMechanism = null;
            foreach (MonoBehaviour component in first.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().Name == "SleepwalkingMechanism") firstMechanism = component;
            firstMechanism.GetType().GetMethod("ResetMechanism").Invoke(firstMechanism, null);
            Collider2D shape = first.GetComponent<Collider2D>();
            yield return null;
            Assert.That(shape.enabled, Is.True);
            yield return new WaitForSecondsRealtime(5.6f);
            Assert.That(shape.enabled, Is.False, "L05 pad should be off during its 1.5-second gap.");
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.That(shape.enabled, Is.True, "L05 pad should reactivate in the next cycle.");
        }

        [UnityTest]
        public IEnumerator L05ControlledJumpClearsTwoCellRise()
        {
            AsyncOperation loading = SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
            while (!loading.isDone) yield return null;
            yield return null;

            GameObject root = GameObject.Find("Sleepwalking Game");
            MonoBehaviour game = null;
            foreach (MonoBehaviour component in root.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().Name == "SleepwalkingGame") game = component;
            Assert.That(game, Is.Not.Null);
            Type gameType = game.GetType();
            Array profiles = (Array)gameType.GetField("MovementProfiles").GetValue(game);
            gameType.GetField("MovementProfile").SetValue(game, profiles.GetValue(0));
            gameType.GetField("completedDreams", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(game, 4);
            gameType.GetMethod("StartDream").Invoke(game, null);
            float deadline = Time.realtimeSinceStartup + 10f;
            while (!(bool)gameType.GetProperty("InDream").GetValue(game) && Time.realtimeSinceStartup < deadline)
                yield return null;
            while ((bool)gameType.GetField("transitioning", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game))
                yield return null;

            GameObject dreamer = GameObject.Find("Dreamer");
            MonoBehaviour player = null;
            foreach (MonoBehaviour component in dreamer.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().Name == "SleepwalkingPlayer") player = component;
            Assert.That(player, Is.Not.Null);
            Assert.That((float)player.GetType().GetField("JumpSpeed").GetValue(player),
                Is.EqualTo(12.15f * 32f).Within(.1f));

            Collider2D shape = dreamer.GetComponent<Collider2D>();
            player.GetType().GetMethod("ResetAt").Invoke(player,
                new object[] { new Vector2(1.7f * 32f, .52f * 32f) });
            Physics2D.SyncTransforms();
            yield return new WaitForSeconds(.08f);
            MethodInfo input = player.GetType().GetMethod("SetSimulatedInput");
            float apex = 0f;
            for (int q = 0; q < 40; q++)
            {
                input.Invoke(player, new object[] { 0f, q == 0, false });
                yield return new WaitForFixedUpdate();
                apex = Mathf.Max(apex, shape.bounds.min.y);
            }
            player.GetType().GetMethod("ClearSimulatedInput").Invoke(player, null);
            Debug.Log("SW_GRID Controlled apexFeet=" + (apex / 32f).ToString("F3") + " cells");
            Assert.That(apex / 32f, Is.GreaterThan(2.15f));
        }

        [UnityTest]
        public IEnumerator L07WallJumpAndDashReachLanding()
        {
            AsyncOperation loading = SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
            while (!loading.isDone) yield return null;
            yield return null;

            GameObject root = GameObject.Find("Sleepwalking Game");
            MonoBehaviour game = null;
            foreach (MonoBehaviour component in root.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().Name == "SleepwalkingGame") game = component;
            Assert.That(game, Is.Not.Null);
            Type gameType = game.GetType();
            gameType.GetField("completedDreams", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(game, 6);
            gameType.GetMethod("StartDream").Invoke(game, null);
            float deadline = Time.realtimeSinceStartup + 10f;
            while (!(bool)gameType.GetProperty("InDream").GetValue(game) && Time.realtimeSinceStartup < deadline)
                yield return null;
            while ((bool)gameType.GetField("transitioning", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game))
                yield return null;

            GameObject dreamer = GameObject.Find("Dreamer");
            MonoBehaviour player = null;
            foreach (MonoBehaviour component in dreamer.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().Name == "SleepwalkingPlayer") player = component;
            Assert.That(player, Is.Not.Null);
            Rigidbody2D body = dreamer.GetComponent<Rigidbody2D>();
            Collider2D playerShape = dreamer.GetComponent<Collider2D>();
            MethodInfo reset = player.GetType().GetMethod("ResetAt");
            MethodInfo input = player.GetType().GetMethod("SetSimulatedInput");
            MethodInfo clear = player.GetType().GetMethod("ClearSimulatedInput");

            reset.Invoke(player, new object[] { new Vector2(3.7f * 32f, 1.5f * 32f) });
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            input.Invoke(player, new object[] { 1f, true, false });
            yield return new WaitForSeconds(.04f);
            Assert.That(body.linearVelocity.y, Is.GreaterThan(0f), "L07 solid wall did not allow a wall jump.");
            Assert.That(body.linearVelocity.x, Is.LessThan(0f), "Wall jump did not push away from the wall.");
            clear.Invoke(player, null);

            GameObject landing = GameObject.Find("DashLanding");
            Collider2D landingShape = landing.GetComponent<Collider2D>();
            reset.Invoke(player, new object[] { new Vector2(10f * 32f, 3.52f * 32f) });
            Physics2D.SyncTransforms();
            yield return new WaitForSeconds(.08f);
            bool buttonPressed = false;
            GameObject button = GameObject.Find("L07_ButtonB");
            MonoBehaviour buttonMechanism = null;
            foreach (MonoBehaviour component in button.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().Name == "SleepwalkingMechanism") buttonMechanism = component;
            if (buttonMechanism != null)
                buttonPressed = (bool)buttonMechanism.GetType().GetProperty("IsPressed").GetValue(buttonMechanism);
            Assert.That(buttonPressed, Is.True, "L07 button B was not pressed by physical contact.");

            for (int q = 0; q < 20 && body.position.x < 10.8f * 32f; q++)
            {
                input.Invoke(player, new object[] { 1f, false, false });
                yield return new WaitForFixedUpdate();
            }
            Assert.That(body.position.x, Is.GreaterThan(10.7f * 32f));
            bool dashed = false;
            int stableFrames = 0;
            float maxX = body.position.x;
            for (int q = 0; q < 90 && stableFrames < 3; q++)
            {
                float x = body.position.x / 32f;
                bool dash = !dashed && x >= 13f;
                if (dash) dashed = true;
                input.Invoke(player, new object[] { x < 19f ? 1f : 0f, q == 0, dash });
                yield return new WaitForFixedUpdate();
                maxX = Mathf.Max(maxX, body.position.x);
                bool landed = landingShape.enabled && playerShape.IsTouching(landingShape) &&
                    Mathf.Abs(playerShape.bounds.min.y - landingShape.bounds.max.y) < 3f &&
                    body.position.x > 19.425f * 32f && body.position.x < 21.575f * 32f;
                stableFrames = landed ? stableFrames + 1 : 0;
            }
            clear.Invoke(player, null);
            Debug.Log("SW_GRID L07 dash=" + dashed + " maxX=" + (maxX / 32f).ToString("F3") +
                " finalX=" + (body.position.x / 32f).ToString("F3") + " stable=" + stableFrames);
            Assert.That(dashed, Is.True);
            Assert.That(stableFrames, Is.EqualTo(3), "L07 jump and dash did not reach the safe landing.");

            for (int q = 0; q < 100 && (bool)gameType.GetProperty("InDream").GetValue(game); q++)
            {
                float horizontal = body.position.x < 20.8f * 32f ? 1f : 0f;
                input.Invoke(player, new object[] { horizontal, false, false });
                yield return new WaitForFixedUpdate();
            }
            Assert.That((bool)gameType.GetProperty("InDream").GetValue(game), Is.False,
                "L07 supported bed contact did not complete the dream.");
        }

        [UnityTest]
        public IEnumerator L05TwoCellStepHasPhysicalLanding()
        {
            AsyncOperation loading = SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
            while (!loading.isDone) yield return null;
            yield return null;

            GameObject root = GameObject.Find("Sleepwalking Game");
            MonoBehaviour game = null;
            foreach (MonoBehaviour component in root.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().Name == "SleepwalkingGame") game = component;
            Assert.That(game, Is.Not.Null);
            Type gameType = game.GetType();
            gameType.GetField("completedDreams", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(game, 4);
            gameType.GetMethod("StartDream").Invoke(game, null);
            float deadline = Time.realtimeSinceStartup + 10f;
            while (!(bool)gameType.GetProperty("InDream").GetValue(game) && Time.realtimeSinceStartup < deadline)
                yield return null;
            while ((bool)gameType.GetField("transitioning", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game))
                yield return null;

            GameObject dreamer = GameObject.Find("Dreamer");
            MonoBehaviour player = null;
            foreach (MonoBehaviour component in dreamer.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().Name == "SleepwalkingPlayer") player = component;
            Assert.That(player, Is.Not.Null);
            GameObject pad = GameObject.Find("L05_TimedA");
            Assert.That(pad, Is.Not.Null);
            Collider2D padShape = pad.GetComponent<Collider2D>();
            Collider2D playerShape = dreamer.GetComponent<Collider2D>();
            Rigidbody2D body = dreamer.GetComponent<Rigidbody2D>();
            Assert.That(padShape.bounds.min.x, Is.EqualTo(4f * 32f).Within(.1f));
            Assert.That(padShape.bounds.max.y, Is.EqualTo(2f * 32f).Within(.1f));

            MethodInfo reset = player.GetType().GetMethod("ResetAt");
            MethodInfo input = player.GetType().GetMethod("SetSimulatedInput");
            reset.Invoke(player, new object[] { new Vector2(1.7f * 32f, .52f * 32f) });
            Physics2D.SyncTransforms();
            yield return new WaitForSeconds(.08f);
            float apex = 0f;
            int stableFrames = 0;
            for (int q = 0; q < 80 && stableFrames < 3; q++)
            {
                float horizontal = body.position.x < 3.9f * 32f ? 1f : 0f;
                input.Invoke(player, new object[] { horizontal, q == 0, false });
                yield return new WaitForFixedUpdate();
                apex = Mathf.Max(apex, playerShape.bounds.min.y);
                bool landed = padShape.enabled && playerShape.IsTouching(padShape) &&
                    Mathf.Abs(playerShape.bounds.min.y - padShape.bounds.max.y) < 3f &&
                    body.position.x > 4.275f * 32f && body.position.x < 5.725f * 32f;
                stableFrames = landed ? stableFrames + 1 : 0;
            }
            player.GetType().GetMethod("ClearSimulatedInput").Invoke(player, null);
            Debug.Log("SW_GRID L05 A apexFeet=" + (apex / 32f).ToString("F3") +
                " landingX=" + (body.position.x / 32f).ToString("F3") + " stable=" + stableFrames);
            Assert.That(apex, Is.GreaterThan(2f * 32f));
            Assert.That(stableFrames, Is.EqualTo(3), "L05 first two-cell step did not land on TimedA.");
        }

        [UnityTest]
        public IEnumerator L04GridPlayerMovesAndCameraKeepsVerticalFrame()
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
            gameType.GetField("completedDreams", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(game, 3);
            gameType.GetMethod("StartDream").Invoke(game, null);
            float deadline = Time.realtimeSinceStartup + 10f;
            while (!(bool)gameType.GetProperty("InDream").GetValue(game) && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That((bool)gameType.GetProperty("InDream").GetValue(game), Is.True);
            while ((bool)gameType.GetField("transitioning", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game))
                yield return null;

            GameObject level = (GameObject)gameType.GetField("level", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game);
            Assert.That(level.transform.lossyScale.x, Is.EqualTo(32f).Within(.01f));
            GameObject dreamer = GameObject.Find("Dreamer");
            Assert.That(dreamer, Is.Not.Null);
            MonoBehaviour player = null;
            foreach (MonoBehaviour component in dreamer.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().Name == "SleepwalkingPlayer") player = component;
            Assert.That(player, Is.Not.Null);

            BoxCollider2D shape = dreamer.GetComponent<BoxCollider2D>();
            Rigidbody2D body = dreamer.GetComponent<Rigidbody2D>();
            Assert.That(shape.bounds.size.x, Is.EqualTo(17.6f).Within(.05f));
            Assert.That(shape.bounds.size.y, Is.EqualTo(32f).Within(.05f));
            float runSpeed = (float)player.GetType().GetField("RunSpeed").GetValue(player);
            float dashSpeed = (float)player.GetType().GetField("DashSpeed").GetValue(player);
            Assert.That(runSpeed, Is.EqualTo(272f).Within(.1f));
            Assert.That(Physics2D.maxTranslationSpeed, Is.GreaterThan(dashSpeed));

            Camera camera = (Camera)gameType.GetField("GameCamera").GetValue(game);
            Vector3 cameraStart = camera.transform.position;
            float sizeStart = camera.orthographicSize;
            yield return new WaitForSeconds(.2f);
            float startX = body.position.x;
            MethodInfo input = player.GetType().GetMethod("SetSimulatedInput");
            for (int q = 0; q < 12; q++)
            {
                input.Invoke(player, new object[] { 1f, false, false });
                yield return new WaitForFixedUpdate();
            }
            player.GetType().GetMethod("ClearSimulatedInput").Invoke(player, null);
            Assert.That(body.position.x - startX, Is.GreaterThan(16f));
            Assert.That(body.position.x - startX, Is.LessThan(80f));
            Assert.That(body.linearVelocity.x, Is.EqualTo(runSpeed).Within(10f));
            Assert.That(camera.transform.position.y, Is.EqualTo(cameraStart.y).Within(.01f));
            Assert.That(camera.transform.position.x, Is.GreaterThanOrEqualTo(cameraStart.x - .5f));
            Assert.That(camera.orthographicSize, Is.EqualTo(sizeStart).Within(.01f));

            foreach (MonoBehaviour component in level.GetComponentsInChildren<MonoBehaviour>())
            {
                if (component == null || component.GetType().Name != "SleepwalkingMechanism") continue;
                if ((string)component.GetType().GetField("Kind").GetValue(component) != "LaserBeam") continue;
                float pixelSize = camera.orthographicSize * 2f / camera.pixelHeight;
                BoxCollider2D beamShape = component.GetComponent<BoxCollider2D>();
                Assert.That(beamShape, Is.Not.Null);
                float pixels = Mathf.Abs(component.transform.lossyScale.x) * beamShape.size.x / pixelSize;
                Assert.That(pixels, Is.EqualTo(3.5f).Within(.5f));
                yield break;
            }
            Assert.Fail("L04 laser beam was not found.");
        }
    }
}
