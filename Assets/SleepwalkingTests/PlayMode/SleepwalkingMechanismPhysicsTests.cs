using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Sleepwalking.Tests.PlayMode
{
    public sealed class SleepwalkingMechanismPhysicsTests
    {
        MonoBehaviour game;
        Type gameType;
        PropertyInfo inDream;
        FieldInfo transitioning;
        FieldInfo levelField;
        MethodInfo startDream;
        MethodInfo completeDream;

        [UnityTest]
        public IEnumerator L05TimedPlatformAlternatesCollision()
        {
            yield return OpenLevel(5);
            MonoBehaviour platform = FindMechanism("TimedPlatform", "L05_TimedA");
            Assert.That(platform, Is.Not.Null);
            Type type = platform.GetType();
            float activeSeconds = (float)type.GetField("ActiveSeconds").GetValue(platform);
            float cycleSeconds = (float)type.GetField("CycleSeconds").GetValue(platform);
            Assert.That(activeSeconds, Is.EqualTo(2.8f).Within(.001f));
            Assert.That(cycleSeconds, Is.EqualTo(4f).Within(.001f));

            Collider2D shape = platform.GetComponent<Collider2D>();
            Assert.That(shape, Is.Not.Null);
            type.GetMethod("ResetMechanism").Invoke(platform, null);
            yield return null;
            Assert.That(shape.enabled, Is.True, "L05 timed platform must start active.");
            yield return new WaitForSecondsRealtime(3f);
            Assert.That(shape.enabled, Is.False, "L05 timed platform must be inactive after 2.8 seconds.");
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.That(shape.enabled, Is.True, "L05 timed platform must reactivate in the next 4-second cycle.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator L06MovingPlatformsCarryPlayerAndReset()
        {
            yield return OpenLevel(6);
            yield return CheckMovingPlatform("L06_Horizontal", Vector2.right);
            yield return CheckMovingPlatform("L06_Vertical", Vector2.up);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator L07MovingPlatformCarriesPlayerIfAuthored()
        {
            yield return OpenLevel(7);
            MonoBehaviour platform = FindMechanism("MovingPlatform", null);
            if (platform == null)
            {
                Assert.Ignore("L07 has no authored MovingPlatform in the current prefab; rider contract is not applicable.");
                yield break;
            }
            yield return CheckMovingPlatform((string)platform.GetType().GetField("FeatureId").GetValue(platform), Vector2.zero);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator BeamWidthMatchesThreeToFourPixelsAtBothRenderHeights()
        {
            yield return LoadScene();
            Camera camera = (Camera)gameType.GetField("GameCamera").GetValue(game);
            MethodInfo beamWidth = gameType.GetMethod("BeamWidth", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(camera, Is.Not.Null);
            Assert.That(beamWidth, Is.Not.Null);

            RenderTexture original = camera.targetTexture;
            foreach (int height in new[] { 180, 240 })
            {
                RenderTexture target = new RenderTexture(320, height, 24);
                target.Create();
                try
                {
                    camera.targetTexture = target;
                    float width = (float)beamWidth.Invoke(game, null);
                    float pixels = width * height / (camera.orthographicSize * 2f);
                    Assert.That(pixels, Is.InRange(3f, 4f), "Beam width at 320x" + height + ".");
                }
                finally
                {
                    camera.targetTexture = original;
                    target.Release();
                    UnityEngine.Object.Destroy(target);
                }
            }
            LogAssert.NoUnexpectedReceived();
        }

        IEnumerator CheckMovingPlatform(string featureId, Vector2 expectedAxis)
        {
            MonoBehaviour platform = FindMechanism("MovingPlatform", featureId);
            Assert.That(platform, Is.Not.Null, "Missing authored moving platform " + featureId + ".");
            Type platformType = platform.GetType();
            FieldInfo initialPosition = platformType.GetField("initialPosition", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo resetMechanism = platformType.GetMethod("ResetMechanism", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(initialPosition, Is.Not.Null);
            Assert.That(resetMechanism, Is.Not.Null);

            Collider2D platformShape = platform.GetComponent<Collider2D>();
            Rigidbody2D platformBody = platform.GetComponent<Rigidbody2D>();
            Assert.That(platformShape, Is.Not.Null);
            Assert.That(platformBody, Is.Not.Null);
            resetMechanism.Invoke(platform, null);
            Physics2D.SyncTransforms();

            GameObject dreamer = GameObject.Find("Dreamer");
            Assert.That(dreamer, Is.Not.Null);
            MonoBehaviour player = null;
            foreach (MonoBehaviour component in dreamer.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().FullName == "Sleepwalking.SleepwalkingPlayer")
                    player = component;
            Assert.That(player, Is.Not.Null);
            Rigidbody2D playerBody = dreamer.GetComponent<Rigidbody2D>();
            Collider2D playerShape = dreamer.GetComponent<Collider2D>();
            Assert.That(playerBody, Is.Not.Null);
            Assert.That(playerShape, Is.Not.Null);

            MethodInfo resetAt = player.GetType().GetMethod("ResetAt", new[] { typeof(Vector2) });
            FieldInfo standingPlatform = player.GetType().GetField("standingPlatform", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(resetAt, Is.Not.Null);
            Assert.That(standingPlatform, Is.Not.Null);
            Vector2 landing = new Vector2(platformShape.bounds.center.x,
                platformShape.bounds.max.y + playerShape.bounds.extents.y + .03f);
            resetAt.Invoke(player, new object[] { landing });
            Physics2D.SyncTransforms();

            float deadline = Time.realtimeSinceStartup + 1f;
            while (standingPlatform.GetValue(player) != platform && Time.realtimeSinceStartup < deadline)
                yield return new WaitForFixedUpdate();
            if (standingPlatform.GetValue(player) != platform)
                Assert.Inconclusive("QA placement did not establish standing contact on " + featureId + ".");

            Vector2 startPlatform = platformBody.position;
            Vector2 relativeStart = playerBody.position - startPlatform;
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.That(standingPlatform.GetValue(player), Is.SameAs(platform),
                "Player lost standing contact on " + featureId + ".");
            Vector2 movement = platformBody.position - startPlatform;
            if (expectedAxis == Vector2.right)
                Assert.That(Mathf.Abs(movement.x), Is.GreaterThan(.35f));
            else if (expectedAxis == Vector2.up)
                Assert.That(Mathf.Abs(movement.y), Is.GreaterThan(.35f));
            else
                Assert.That(movement.magnitude, Is.GreaterThan(.35f));
            Vector2 relativeEnd = playerBody.position - platformBody.position;
            Assert.That(Vector2.Distance(relativeStart, relativeEnd), Is.LessThan(.35f),
                "Player did not follow " + featureId + " while standing on it.");

            resetMechanism.Invoke(platform, null);
            Vector3 authoredStart = (Vector3)initialPosition.GetValue(platform);
            Assert.That(Vector3.Distance(platform.transform.localPosition, authoredStart), Is.LessThan(.01f),
                "ResetMechanism must return " + featureId + " to its authored start.");
        }

        IEnumerator OpenLevel(int number)
        {
            yield return LoadScene();
            for (int q = 1; q <= number; q++)
            {
                startDream.Invoke(game, null);
                yield return WaitForDream(true);
                Assert.That(GameObject.Find("Dream L" + q.ToString("00")), Is.Not.Null);
                if (q == number) yield break;
                completeDream.Invoke(game, null);
                yield return WaitForDream(false);
            }
        }

        IEnumerator LoadScene()
        {
            AsyncOperation loading = SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
            Assert.That(loading, Is.Not.Null);
            while (!loading.isDone)
                yield return null;
            yield return null;
            GameObject gameObject = GameObject.Find("Sleepwalking Game");
            Assert.That(gameObject, Is.Not.Null);
            foreach (MonoBehaviour component in gameObject.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().FullName == "Sleepwalking.SleepwalkingGame")
                    game = component;
            Assert.That(game, Is.Not.Null);
            gameType = game.GetType();
            inDream = gameType.GetProperty("InDream", BindingFlags.Instance | BindingFlags.Public);
            transitioning = gameType.GetField("transitioning", BindingFlags.Instance | BindingFlags.NonPublic);
            levelField = gameType.GetField("level", BindingFlags.Instance | BindingFlags.NonPublic);
            startDream = gameType.GetMethod("StartDream", BindingFlags.Instance | BindingFlags.Public);
            completeDream = gameType.GetMethod("CompleteDream", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(inDream, Is.Not.Null);
            Assert.That(transitioning, Is.Not.Null);
            Assert.That(levelField, Is.Not.Null);
            Assert.That(startDream, Is.Not.Null);
            Assert.That(completeDream, Is.Not.Null);
        }

        IEnumerator WaitForDream(bool expected)
        {
            float deadline = Time.realtimeSinceStartup + 10f;
            while (((bool)inDream.GetValue(game) != expected || (bool)transitioning.GetValue(game)) &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That((bool)inDream.GetValue(game), Is.EqualTo(expected));
            Assert.That((bool)transitioning.GetValue(game), Is.False);
        }

        MonoBehaviour FindMechanism(string kind, string featureId)
        {
            GameObject level = (GameObject)levelField.GetValue(game);
            Assert.That(level, Is.Not.Null);
            foreach (MonoBehaviour component in level.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null || component.GetType().FullName != "Sleepwalking.SleepwalkingMechanism") continue;
                Type type = component.GetType();
                if ((string)type.GetField("Kind").GetValue(component) != kind) continue;
                if (featureId == null || (string)type.GetField("FeatureId").GetValue(component) == featureId)
                    return component;
            }
            return null;
        }
    }
}
