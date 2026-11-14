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
    public sealed class SleepwalkingTraversalContactTests
    {
        [UnityTest]
        public IEnumerator L01FirstGapLandsOnTheAuthoredCollider()
        {
            AsyncOperation loading = SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
            Assert.That(loading, Is.Not.Null);
            while (!loading.isDone)
                yield return null;
            yield return null;

            MonoBehaviour game = FindComponent(GameObject.Find("Sleepwalking Game"), "Sleepwalking.SleepwalkingGame");
            Assert.That(game, Is.Not.Null);
            Type gameType = game.GetType();
            MethodInfo startDream = gameType.GetMethod("StartDream");
            PropertyInfo inDream = gameType.GetProperty("InDream");
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
            Collider2D playerShape = dreamer.GetComponent<Collider2D>();
            Collider2D startShape = GameObject.Find("Start")?.GetComponent<Collider2D>();
            Collider2D targetShape = GameObject.Find("JumpLanding")?.GetComponent<Collider2D>();
            Assert.That(player, Is.Not.Null);
            Assert.That(body, Is.Not.Null);
            Assert.That(playerShape, Is.Not.Null);
            Assert.That(startShape, Is.Not.Null);
            Assert.That(targetShape, Is.Not.Null);
            Type playerType = player.GetType();
            MethodInfo resetAt = playerType.GetMethod("ResetAt", new[] { typeof(Vector2) });
            MethodInfo setInput = playerType.GetMethod("SetSimulatedInput");
            MethodInfo clearInput = playerType.GetMethod("ClearSimulatedInput");
            Assert.That(resetAt, Is.Not.Null);
            Assert.That(setInput, Is.Not.Null);
            Assert.That(clearInput, Is.Not.Null);

            List<string> trace = new List<string> { "launchX,brakeX,step,time,x,y,vx,vy,horizontal,jump,dash,targetContact,stable,margin" };
            float[] launches = { 2.4f, 2.8f, 3.2f };
            float[] brakes = { 5.2f, 6.0f, 6.8f };
            bool found = false;
            float bestMargin = float.NegativeInfinity;
            int attempt = 0;
            for (int scenario = 0; scenario < launches.Length * brakes.Length; scenario++)
            {
                float launchX = launches[scenario / brakes.Length];
                float brakeX = brakes[scenario % brakes.Length];
                attempt++;
                clearInput.Invoke(player, null);
                resetAt.Invoke(player, new object[] { new Vector2(1.4f, .56f) });
                Physics2D.SyncTransforms();
                for (int q = 0; q < 5; q++)
                    yield return new WaitForFixedUpdate();
                Assert.That(playerShape.IsTouching(startShape), Is.True,
                    "QA start placement did not establish support on Start.");

                bool jumped = false;
                int stable = 0;
                deadline = Time.realtimeSinceStartup + 2.5f;
                for (int q = 0; q < 150 && Time.realtimeSinceStartup < deadline; q++)
                {
                    bool jumpNow = !jumped && body.position.x >= launchX;
                    if (jumpNow) jumped = true;
                    float direction = body.position.x < brakeX ? 1f : 0f;
                    setInput.Invoke(player, new object[] { direction, jumpNow, false });
                    yield return new WaitForFixedUpdate();

                    Bounds bodyBounds = playerShape.bounds;
                    Bounds targetBounds = targetShape.bounds;
                    bool contact = playerShape.IsTouching(targetShape);
                    float feet = bodyBounds.min.y;
                    bool landed = contact && Mathf.Abs(feet - targetBounds.max.y) < .12f &&
                        body.linearVelocity.y <= .15f;
                    stable = landed ? stable + 1 : 0;
                    float margin = Mathf.Min(bodyBounds.min.x - targetBounds.min.x,
                        targetBounds.max.x - bodyBounds.max.x);
                    trace.Add(string.Format(CultureInfo.InvariantCulture,
                        "{0:F2},{1:F2},{2},{3:F3},{4:F3},{5:F3},{6:F3},{7:F3},{8:F1},{9},{10},{11},{12},{13:F3}",
                        launchX, brakeX, q, Time.fixedTime, body.position.x, body.position.y,
                        body.linearVelocity.x, body.linearVelocity.y, direction, jumpNow, false,
                        contact, stable, margin));
                    if (stable >= 3 && margin >= .15f)
                    {
                        found = true;
                        bestMargin = Mathf.Max(bestMargin, margin);
                        break;
                    }
                    if (body.position.x > targetBounds.max.x + 1f || body.position.y < -2f)
                        break;
                }
                clearInput.Invoke(player, null);
                if (found) break;
            }

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string path = Path.Combine(projectRoot, "Docs", "QA", "traversal-l01-first-gap-contact.csv");
            File.WriteAllLines(path, trace);
            Assert.That(attempt, Is.GreaterThan(0));
            if (!found)
                Assert.Inconclusive("No stable L01 first-gap landing found in nine QA input schedules; see " + path);
            Assert.That(bestMargin, Is.GreaterThanOrEqualTo(.15f));
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
