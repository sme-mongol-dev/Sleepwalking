using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Sleepwalking.Tests.PlayMode
{
    public sealed class SleepwalkingCaveShellTests
    {
        const float Unit = 32f;
        static float C(float cells) => cells * Unit;

        [UnityTest]
        public IEnumerator ShellWallGrantsWallJump()
        {
            AsyncOperation loading = SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
            Assert.That(loading, Is.Not.Null);
            while (!loading.isDone) yield return null;
            yield return null;

            GameObject gameObject = GameObject.Find("Sleepwalking Game");
            MonoBehaviour game = null;
            foreach (MonoBehaviour component in gameObject.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().FullName == "Sleepwalking.SleepwalkingGame")
                    game = component;
            Assert.That(game, Is.Not.Null);
            Type type = game.GetType();
            type.GetField("tutorialStage", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(game, 2);
            type.GetMethod("StartDream", BindingFlags.Instance | BindingFlags.Public).Invoke(game, null);
            PropertyInfo inDream = type.GetProperty("InDream", BindingFlags.Instance | BindingFlags.Public);
            FieldInfo transitioning = type.GetField("transitioning", BindingFlags.Instance | BindingFlags.NonPublic);
            float deadline = Time.realtimeSinceStartup + 10f;
            while ((!((bool)inDream.GetValue(game)) || (bool)transitioning.GetValue(game)) &&
                Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That((bool)inDream.GetValue(game), Is.True);

            GameObject dreamer = GameObject.Find("Dreamer");
            Assert.That(dreamer, Is.Not.Null);
            Rigidbody2D body = dreamer?.GetComponent<Rigidbody2D>();
            Collider2D shape = dreamer?.GetComponent<Collider2D>();
            Collider2D wall = GameObject.Find("Cave_LeftWall")?.GetComponent<Collider2D>();
            MonoBehaviour player = null;
            foreach (MonoBehaviour component in dreamer.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().FullName == "Sleepwalking.SleepwalkingPlayer")
                    player = component;
            Assert.That(body, Is.Not.Null);
            Assert.That(shape, Is.Not.Null);
            Assert.That(wall, Is.Not.Null);
            Assert.That(player, Is.Not.Null);
            FieldInfo wallDirection = player.GetType().GetField("wall", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(wallDirection, Is.Not.Null);

            body.position = new Vector2(C(.28f), C(1.2f));
            body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(shape.bounds.min.x - wall.bounds.max.x, Is.InRange(0f, C(.07f)));
            Assert.That((int)wallDirection.GetValue(player), Is.EqualTo(-1),
                "Every solid wall, including the cave boundary, must support a wall jump.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator VisiblePitSpikesTriggerRetryBeforeBedrock()
        {
            AsyncOperation loading = SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
            Assert.That(loading, Is.Not.Null);
            while (!loading.isDone) yield return null;
            yield return null;

            GameObject gameObject = GameObject.Find("Sleepwalking Game");
            Assert.That(gameObject, Is.Not.Null);
            MonoBehaviour game = null;
            foreach (MonoBehaviour component in gameObject.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().FullName == "Sleepwalking.SleepwalkingGame")
                    game = component;
            Assert.That(game, Is.Not.Null);

            Type type = game.GetType();
            type.GetField("tutorialStage", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(game, 2);
            type.GetMethod("StartDream", BindingFlags.Instance | BindingFlags.Public).Invoke(game, null);
            PropertyInfo inDream = type.GetProperty("InDream", BindingFlags.Instance | BindingFlags.Public);
            FieldInfo transitioning = type.GetField("transitioning", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo dying = type.GetField("dying", BindingFlags.Instance | BindingFlags.NonPublic);
            float deadline = Time.realtimeSinceStartup + 10f;
            while ((!((bool)inDream.GetValue(game)) || (bool)transitioning.GetValue(game)) &&
                Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That((bool)inDream.GetValue(game), Is.True);

            GameObject dreamer = GameObject.Find("Dreamer");
            Rigidbody2D body = dreamer?.GetComponent<Rigidbody2D>();
            Collider2D shape = dreamer?.GetComponent<Collider2D>();
            Collider2D spikeA = GameObject.Find("L01_GapA_1")?.GetComponent<Collider2D>();
            Collider2D spikeB = GameObject.Find("L01_GapA_2")?.GetComponent<Collider2D>();
            Collider2D bedrock = GameObject.Find("Cave_Bedrock")?.GetComponent<Collider2D>();
            Assert.That(body, Is.Not.Null);
            Assert.That(shape, Is.Not.Null);
            Assert.That(spikeA, Is.Not.Null);
            Assert.That(spikeB, Is.Not.Null);
            Assert.That(bedrock, Is.Not.Null);
            Assert.That(GameObject.Find("FallReset"), Is.Null);
            Assert.That(spikeA.bounds.max.y, Is.GreaterThan(bedrock.bounds.max.y));

            // Первый разрыв L01 закрыт видимыми шипами над твёрдым основанием.
            body.position = new Vector2(C(4.5f), C(1.2f));
            body.linearVelocity = Vector2.zero;
            bool hitSpikes = false;
            for (int q = 0; q < 240; q++)
            {
                yield return new WaitForFixedUpdate();
                hitSpikes |= shape.IsTouching(spikeA) || shape.IsTouching(spikeB);
                if (!(bool)dying.GetValue(game)) continue;
                Assert.That(hitSpikes, Is.True, "Retry began without physical contact with visible pit spikes.");
                Assert.That(body.position.y - C(.5f), Is.GreaterThan(bedrock.bounds.max.y),
                    "Visible spikes must trigger retry above the bedrock.");
                break;
            }
            Assert.That(hitSpikes, Is.True, "The player missed visible pit spikes.");
            Assert.That((bool)dying.GetValue(game), Is.True, "Visible pit spikes did not trigger retry.");
            deadline = Time.realtimeSinceStartup + 5f;
            while ((bool)dying.GetValue(game) && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That((bool)dying.GetValue(game), Is.False);
            Assert.That(body.position.x, Is.EqualTo(C(1f)).Within(C(.2f)));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
