using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Sleepwalking.Tests.PlayMode
{
    public sealed class LaserEmitterTests
    {
        [UnityTest]
        public IEnumerator ButtonDisablesOnlyBeamWhileEmitterRemainsLethal()
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
            const BindingFlags privateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
            gameType.GetField("completedDreams", privateInstance).SetValue(game, 3);
            gameType.GetMethod("StartDream").Invoke(game, null);
            float deadline = Time.realtimeSinceStartup + 10f;
            while ((!(bool)gameType.GetProperty("InDream").GetValue(game) ||
                (bool)gameType.GetField("transitioning", privateInstance).GetValue(game)) &&
                Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That((bool)gameType.GetProperty("InDream").GetValue(game), Is.True);

            GameObject emitter = GameObject.Find("L04_Laser_Base");
            GameObject beam = GameObject.Find("L04_Laser_Beam");
            GameObject button = GameObject.Find("L04_ButtonLaser");
            GameObject dreamer = GameObject.Find("Dreamer");
            Assert.That(emitter, Is.Not.Null);
            Assert.That(beam, Is.Not.Null);
            Assert.That(button, Is.Not.Null);
            Assert.That(dreamer, Is.Not.Null);
            Collider2D emitterShape = emitter.GetComponent<Collider2D>();
            Collider2D beamShape = beam.GetComponent<Collider2D>();
            Renderer emitterVisual = emitter.GetComponent<Renderer>();
            Renderer beamVisual = beam.GetComponent<Renderer>();
            Assert.That(emitterShape.enabled && emitterVisual.enabled, Is.True);
            Assert.That(beamShape.enabled && beamVisual.enabled, Is.True);
            Vector2 beamCenter = beamShape.bounds.center;

            Type mechanismType = Type.GetType("Sleepwalking.SleepwalkingMechanism, Assembly-CSharp");
            Assert.That(mechanismType, Is.Not.Null);
            Component buttonMechanism = button.GetComponent(mechanismType);
            Assert.That(buttonMechanism, Is.Not.Null);
            mechanismType.GetField("ActiveSeconds").SetValue(buttonMechanism, .3f);
            gameType.GetMethod("PressButton").Invoke(game, new object[] { buttonMechanism });
            yield return null;
            Assert.That(emitterShape.enabled && emitterVisual.enabled, Is.True,
                "The linked laser button hid its emitter.");
            Assert.That(beamShape.enabled || beamVisual.enabled, Is.False,
                "The beam must disappear and stop colliding while disabled.");

            MonoBehaviour player = null;
            foreach (MonoBehaviour component in dreamer.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().Name == "SleepwalkingPlayer") player = component;
            Assert.That(player, Is.Not.Null);
            MethodInfo reset = player.GetType().GetMethod("ResetAt");
            Rigidbody2D body = dreamer.GetComponent<Rigidbody2D>();
            int deaths = (int)gameType.GetProperty("DreamDeaths").GetValue(game);
            reset.Invoke(player, new object[] { beamCenter });
            body.gravityScale = 0f;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That((int)gameType.GetProperty("DreamDeaths").GetValue(game), Is.EqualTo(deaths),
                "The disabled beam remained lethal.");

            Transform start = GameObject.Find("PlayerStart").transform;
            reset.Invoke(player, new object[] { (Vector2)(start.position + Vector3.up * 16f) });
            deadline = Time.realtimeSinceStartup + 2f;
            while (!beamShape.enabled && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(beamShape.enabled && beamVisual.enabled, Is.True,
                "The beam did not return after the button expired.");

            gameType.GetMethod("PressButton").Invoke(game, new object[] { buttonMechanism });
            yield return null;
            Assert.That(beamShape.enabled, Is.False);
            Assert.That(emitterShape.enabled && emitterVisual.enabled, Is.True);
            reset.Invoke(player, new object[] { (Vector2)emitterShape.bounds.center });
            body.gravityScale = 0f;
            Physics2D.SyncTransforms();
            deadline = Time.realtimeSinceStartup + 2f;
            while ((int)gameType.GetProperty("DreamDeaths").GetValue(game) == deaths &&
                Time.realtimeSinceStartup < deadline) yield return new WaitForFixedUpdate();
            Assert.That((int)gameType.GetProperty("DreamDeaths").GetValue(game), Is.GreaterThan(deaths),
                "The emitter was no longer lethal while its beam was disabled.");
        }
    }
}
