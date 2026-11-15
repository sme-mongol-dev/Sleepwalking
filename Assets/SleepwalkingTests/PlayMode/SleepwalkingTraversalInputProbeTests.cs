using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Sleepwalking.Tests.PlayMode
{
    public sealed class SleepwalkingTraversalInputProbeTests
    {
        const float Unit = 32f;

        [UnityTest]
        public IEnumerator SimulatedCommandsDriveTheLiveL01Controller()
        {
            AsyncOperation loading = SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
            Assert.That(loading, Is.Not.Null);
            while (!loading.isDone)
                yield return null;
            yield return null;

            MonoBehaviour game = FindComponent(GameObject.Find("Sleepwalking Game"), "Sleepwalking.SleepwalkingGame");
            Assert.That(game, Is.Not.Null);
            Type gameType = game.GetType();
            MethodInfo startDream = gameType.GetMethod("StartDream", BindingFlags.Instance | BindingFlags.Public);
            PropertyInfo inDream = gameType.GetProperty("InDream", BindingFlags.Instance | BindingFlags.Public);
            FieldInfo transitioning = gameType.GetField("transitioning", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(startDream, Is.Not.Null);
            Assert.That(inDream, Is.Not.Null);
            Assert.That(transitioning, Is.Not.Null);
            startDream.Invoke(game, null);

            float deadline = Time.realtimeSinceStartup + 10f;
            while ((!((bool)inDream.GetValue(game)) || (bool)transitioning.GetValue(game)) &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That((bool)inDream.GetValue(game), Is.True);
            Assert.That((bool)transitioning.GetValue(game), Is.False);
            Assert.That(GameObject.Find("Dream L01"), Is.Not.Null);
            Camera camera = (Camera)gameType.GetField("GameCamera").GetValue(game);
            Assert.That(camera.targetTexture, Is.Null, "Dreams must render directly to the screen.");

            GameObject dreamer = GameObject.Find("Dreamer");
            Assert.That(dreamer, Is.Not.Null);
            MonoBehaviour player = FindComponent(dreamer, "Sleepwalking.SleepwalkingPlayer");
            Rigidbody2D body = dreamer.GetComponent<Rigidbody2D>();
            Collider2D shape = dreamer.GetComponent<Collider2D>();
            Assert.That(player, Is.Not.Null);
            Assert.That(body, Is.Not.Null);
            Assert.That(shape, Is.Not.Null);
            Type playerType = player.GetType();
            MethodInfo setInput = playerType.GetMethod("SetSimulatedInput", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo clearInput = playerType.GetMethod("ClearSimulatedInput", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo resetAt = playerType.GetMethod("ResetAt", new[] { typeof(Vector2) });
            FieldInfo dashRemaining = playerType.GetField("dashRemaining", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(setInput, Is.Not.Null);
            Assert.That(clearInput, Is.Not.Null);
            Assert.That(resetAt, Is.Not.Null);
            Assert.That(dashRemaining, Is.Not.Null);

            Vector2 start = body.position;
            float cameraY = camera.transform.position.y;
            float cameraSize = camera.orthographicSize;
            try
            {
                yield return ResetAndSettle(resetAt, player, start);
                float initialY = body.position.y;
                setInput.Invoke(player, new object[] { 0f, true, false });
                float peakY = initialY;
                for (int q = 0; q < 15; q++)
                {
                    yield return new WaitForFixedUpdate();
                    peakY = Mathf.Max(peakY, body.position.y);
                }
                Assert.That(peakY - initialY, Is.GreaterThan(.2f * Unit),
                    "Jump command did not lift the live body.");
                Assert.That(camera.transform.position.y, Is.EqualTo(cameraY).Within(.01f),
                    "Horizontal follow camera moved vertically during the jump.");

                yield return ResetAndSettle(resetAt, player, start);
                float initialX = body.position.x;
                setInput.Invoke(player, new object[] { 1f, false, false });
                for (int q = 0; q < 10; q++)
                    yield return new WaitForFixedUpdate();
                Assert.That(body.position.x - initialX, Is.GreaterThan(.15f * Unit),
                    "Horizontal command did not move the live body.");

                Collider2D middleSupport = GameObject.Find("RiseA")?.GetComponent<Collider2D>();
                Assert.That(middleSupport, Is.Not.Null);
                Vector3 cameraBeforeFollow = camera.transform.position;
                Vector2 middle = new Vector2(15.2f * Unit,
                    middleSupport.bounds.max.y + shape.bounds.extents.y + .015f * Unit);
                yield return ResetAndSettle(resetAt, player, middle);
                for (int q = 0; q < 30; q++) yield return null;
                Assert.That(camera.transform.position.x - cameraBeforeFollow.x,
                    Is.GreaterThan(.25f * Unit), "Dream camera did not follow the player along X.");
                Assert.That(camera.transform.position.y, Is.EqualTo(cameraY).Within(.01f),
                    "Dream camera must keep a fixed Y while following horizontally.");
                Assert.That(camera.orthographicSize, Is.EqualTo(cameraSize).Within(.01f),
                    "Dream camera changed its vertical scale while following.");

                yield return ResetAndSettle(resetAt, player, start);
                initialX = body.position.x;
                setInput.Invoke(player, new object[] { 1f, false, true });
                yield return null;
                Assert.That((float)dashRemaining.GetValue(player), Is.GreaterThan(0f),
                    "Dash command did not enter the live dash state.");
                for (int q = 0; q < 3; q++)
                    yield return new WaitForFixedUpdate();
                Assert.That(body.position.x - initialX, Is.GreaterThan(.15f * Unit),
                    "Dash command did not move the live body.");
            }
            finally
            {
                clearInput.Invoke(player, null);
            }
            LogAssert.NoUnexpectedReceived();
        }

        static IEnumerator ResetAndSettle(MethodInfo resetAt, MonoBehaviour player, Vector2 start)
        {
            resetAt.Invoke(player, new object[] { start });
            Physics2D.SyncTransforms();
            for (int q = 0; q < 3; q++)
                yield return new WaitForFixedUpdate();
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
