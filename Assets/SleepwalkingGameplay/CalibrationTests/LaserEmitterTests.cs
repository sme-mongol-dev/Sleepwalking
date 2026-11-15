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
        public IEnumerator OnlyBeamKillsWhileEmitterRemainsVisibleAndIntangible()
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
            SpriteRenderer emitterVisual = emitter.transform.Find("Dream Hazard Art").GetComponent<SpriteRenderer>();
            SpriteRenderer beamVisual = beam.transform.Find("Dream Hazard Art").GetComponent<SpriteRenderer>();
            Assert.That(emitterShape.enabled, Is.False);
            Assert.That(emitterVisual.enabled, Is.True);
            foreach (Collider shape in emitter.GetComponentsInChildren<Collider>(true))
                Assert.That(shape.enabled, Is.False, "The emitter has an active 3D collider.");
            Assert.That(beamShape.enabled && beamVisual.enabled, Is.True);
            Vector2 beamCenter = beamShape.bounds.center;

            Type mechanismType = Type.GetType("Sleepwalking.SleepwalkingMechanism, Assembly-CSharp");
            Assert.That(mechanismType, Is.Not.Null);
            Component buttonMechanism = button.GetComponent(mechanismType);
            Assert.That(buttonMechanism, Is.Not.Null);
            mechanismType.GetField("ActiveSeconds").SetValue(buttonMechanism, .3f);
            gameType.GetMethod("PressButton").Invoke(game, new object[] { buttonMechanism });
            yield return null;
            yield return null;
            Assert.That(emitterShape.enabled, Is.False);
            Assert.That(emitterVisual.enabled, Is.True, "The linked laser button hid its emitter.");
            Assert.That(beamShape.enabled, Is.False, "The beam collider remained active.");
            Assert.That(beamVisual.enabled, Is.False, "The beam art remained visible.");

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

            reset.Invoke(player, new object[] { (Vector2)emitter.transform.position });
            body.gravityScale = 0f;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            Assert.That((int)gameType.GetProperty("DreamDeaths").GetValue(game), Is.EqualTo(deaths),
                "The emitter killed the player while its beam was disabled.");

            Transform start = GameObject.Find("PlayerStart").transform;
            reset.Invoke(player, new object[] { (Vector2)(start.position + Vector3.up * 16f) });
            deadline = Time.realtimeSinceStartup + 2f;
            while (!beamShape.enabled && Time.realtimeSinceStartup < deadline) yield return null;
            yield return null;
            Assert.That(beamShape.enabled && beamVisual.enabled, Is.True,
                "The beam did not return after the button expired.");
            Assert.That(emitterShape.enabled, Is.False);
            reset.Invoke(player, new object[] { beamCenter });
            Physics2D.SyncTransforms();
            deadline = Time.realtimeSinceStartup + 2f;
            while ((int)gameType.GetProperty("DreamDeaths").GetValue(game) == deaths &&
                Time.realtimeSinceStartup < deadline) yield return new WaitForFixedUpdate();
            Assert.That((int)gameType.GetProperty("DreamDeaths").GetValue(game), Is.GreaterThan(deaths),
                "The active beam did not kill the player.");
        }
    }
}
