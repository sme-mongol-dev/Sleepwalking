using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Sleepwalking.Tests.PlayMode
{
    public sealed class SleepwalkingOdradekTests
    {
        Component game;
        Type gameType;
        const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

        [TestCase(6f, 0, 6)]
        [TestCase(6f, 1, 5)]
        [TestCase(6f, 2, 4)]
        [TestCase(6f, 5, 3)]
        [TestCase(20f, 0, 3)]
        [TestCase(20f, 3, 0)]
        public void RewardDependsOnTimeAndDeaths(float seconds, int deaths, int expected)
        {
            Type type = Type.GetType("Sleepwalking.SleepwalkingGame, Assembly-CSharp");
            Assert.That(type, Is.Not.Null);
            MethodInfo score = type.GetMethod("CalculateOdradekGain", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(score.Invoke(null, new object[] { seconds, deaths, 10f }), Is.EqualTo(expected));
        }

        [UnityTest]
        public IEnumerator TracksActiveTimeDeathsAndAwardsEachLevelOnce()
        {
            yield return LoadGame();
            MethodInfo roll = gameType.GetMethod("ResetOdradek", PrivateInstance);
            for (int i = 0; i < 100; i++)
            {
                roll.Invoke(game, null);
                Assert.That(Read<int>("Odradek"), Is.InRange(20, 35));
            }

            Set("odradek", 20);
            yield return StartDream();
            Component ui = (Component)gameType.GetField("UI").GetValue(game);
            ui.GetType().GetMethod("OpenPause").Invoke(ui, null);
            yield return null;
            float pausedTime = Read<float>("DreamElapsedSeconds");
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(Read<float>("DreamElapsedSeconds"), Is.EqualTo(pausedTime).Within(.001f));
            ui.GetType().GetMethod("CloseOverlay").Invoke(ui, null);

            Vector2 point = GameObject.Find("Dreamer").transform.position;
            gameType.GetMethod("KillPlayer").Invoke(game, new object[] { point });
            gameType.GetMethod("KillPlayer").Invoke(game, new object[] { point });
            Assert.That(Read<int>("DreamDeaths"), Is.EqualTo(1), "Repeated contacts during one death count once.");
            float deadline = Time.realtimeSinceStartup + 5f;
            while ((bool)gameType.GetField("dying", PrivateInstance).GetValue(game) && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That((bool)gameType.GetField("dying", PrivateInstance).GetValue(game), Is.False);

            float elapsed = Read<float>("DreamElapsedSeconds");
            gameType.GetMethod("RestartDream").Invoke(game, null);
            yield return null;
            Assert.That(Read<int>("DreamDeaths"), Is.EqualTo(1));
            Assert.That(Read<float>("DreamElapsedSeconds"), Is.GreaterThanOrEqualTo(elapsed));

            Set("dreamElapsedSeconds", 6f);
            Set("dreamDeaths", 0);
            gameType.GetMethod("CompleteDream").Invoke(game, null);
            gameType.GetMethod("CompleteDream").Invoke(game, null);
            yield return WaitForTransition();
            Assert.That(Read<int>("Odradek"), Is.EqualTo(26));
            Assert.That(Read<int>("LastOdradekGain"), Is.EqualTo(6));
            Assert.That(Read<int>("CompletedDreams"), Is.EqualTo(1));

            yield return StartDream();
            Assert.That(Read<int>("DreamDeaths"), Is.Zero);
            Assert.That(Read<float>("DreamElapsedSeconds"), Is.LessThan(.5f));
            Assert.That(Read<int>("Odradek"), Is.EqualTo(26));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator MaximumOnlyMildlyReducesMovementAndTrapDisableTime()
        {
            yield return LoadGame();
            Set("odradek", 100);
            Set("completedDreams", 3);
            ScriptableObject profile = (ScriptableObject)gameType.GetField("MovementProfile").GetValue(game);
            float run = (float)profile.GetType().GetField("RunSpeed").GetValue(profile);
            float jump = (float)profile.GetType().GetField("JumpSpeed").GetValue(profile);
            yield return StartDream();

            Component player = GameObject.Find("Dreamer").GetComponent("SleepwalkingPlayer");
            Type playerType = player.GetType();
            float unit = (float)playerType.GetField("WorldUnitsPerCell").GetValue(player);
            playerType.GetMethod("ApplyProfile").Invoke(player, null);
            playerType.GetMethod("ApplyProfile").Invoke(player, null);
            Assert.That((float)playerType.GetProperty("EffectiveRunSpeed").GetValue(player),
                Is.EqualTo(run * unit * .95f).Within(.001f));
            Assert.That((float)playerType.GetProperty("EffectiveJumpSpeed").GetValue(player),
                Is.EqualTo(jump * unit * .99f).Within(.001f));
            Assert.That((float)profile.GetType().GetField("RunSpeed").GetValue(profile), Is.EqualTo(run));
            Assert.That((float)profile.GetType().GetField("JumpSpeed").GetValue(profile), Is.EqualTo(jump));

            Component button = GameObject.Find("L04_ButtonLaser").GetComponent("SleepwalkingMechanism");
            Component beam = GameObject.Find("L04_Laser_Beam").GetComponent("SleepwalkingMechanism");
            float duration = (float)button.GetType().GetField("ActiveSeconds").GetValue(button);
            gameType.GetMethod("PressButton").Invoke(game, new object[] { button });
            float disabledUntil = (float)beam.GetType().GetField("disabledUntil", PrivateInstance).GetValue(beam);
            Assert.That(disabledUntil - Time.time, Is.EqualTo(duration * .9f).Within(.001f));

            Set("odradek", 99);
            Set("dreamElapsedSeconds", 0f);
            Set("dreamDeaths", 0);
            gameType.GetMethod("CompleteDream").Invoke(game, null);
            yield return WaitForTransition();
            Assert.That(Read<int>("Odradek"), Is.EqualTo(100));
            Assert.That(Read<int>("LastOdradekGain"), Is.EqualTo(1));
            gameType.GetMethod("ReturnToRoom").Invoke(game, null);
            Assert.That(Read<int>("Odradek"), Is.InRange(20, 35));
            Assert.That(Read<int>("LastOdradekGain"), Is.Zero);
            LogAssert.NoUnexpectedReceived();
        }

        IEnumerator LoadGame()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
            yield return null;
            game = GameObject.Find("Sleepwalking Game").GetComponent("SleepwalkingGame");
            Assert.That(game, Is.Not.Null);
            gameType = game.GetType();
            Set("tutorialStage", 2);
            Assert.That(Read<int>("Odradek"), Is.InRange(20, 35));
        }

        IEnumerator StartDream()
        {
            gameType.GetMethod("StartDream").Invoke(game, null);
            yield return WaitForTransition();
            Assert.That(Read<bool>("InDream"), Is.True);
        }

        IEnumerator WaitForTransition()
        {
            FieldInfo transition = gameType.GetField("transitioning", PrivateInstance);
            float deadline = Time.realtimeSinceStartup + 10f;
            while ((bool)transition.GetValue(game) && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That((bool)transition.GetValue(game), Is.False);
        }

        T Read<T>(string name) => (T)gameType.GetProperty(name).GetValue(game);
        void Set(string name, object value) => gameType.GetField(name, PrivateInstance).SetValue(game, value);
    }
}
