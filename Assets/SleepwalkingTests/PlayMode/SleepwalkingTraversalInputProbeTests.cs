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

            GameObject dreamer = GameObject.Find("Dreamer");
            Assert.That(dreamer, Is.Not.Null);
            MonoBehaviour player = FindComponent(dreamer, "Sleepwalking.SleepwalkingPlayer");
            Rigidbody2D body = dreamer.GetComponent<Rigidbody2D>();
            Assert.That(player, Is.Not.Null);
            Assert.That(body, Is.Not.Null);
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
                Assert.That(peakY - initialY, Is.GreaterThan(.2f), "Jump command did not lift the live body.");

                yield return ResetAndSettle(resetAt, player, start);
                float initialX = body.position.x;
                setInput.Invoke(player, new object[] { 1f, false, false });
                for (int q = 0; q < 10; q++)
                    yield return new WaitForFixedUpdate();
                Assert.That(body.position.x - initialX, Is.GreaterThan(.15f),
                    "Horizontal command did not move the live body.");

                yield return ResetAndSettle(resetAt, player, start);
                initialX = body.position.x;
                setInput.Invoke(player, new object[] { 1f, false, true });
                yield return null;
                Assert.That((float)dashRemaining.GetValue(player), Is.GreaterThan(0f),
                    "Dash command did not enter the live dash state.");
                for (int q = 0; q < 3; q++)
                    yield return new WaitForFixedUpdate();
                Assert.That(body.position.x - initialX, Is.GreaterThan(.15f),
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
