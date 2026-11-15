using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Sleepwalking.Tests.PlayMode
{
    public sealed class TutorialProgressionTests
    {
        [UnityTest]
        public IEnumerator TutorialsWakeBetweenStagesThenMainDreamsRepeatWithoutThem()
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
            Array tutorials = (Array)gameType.GetField("TutorialPrefabs").GetValue(game);
            Assert.That(tutorials, Has.Length.EqualTo(2));
            Assert.That(tutorials.GetValue(0), Is.Not.Null);
            Assert.That(tutorials.GetValue(1), Is.Not.Null);
            Array mainLevels = (Array)gameType.GetField("LevelPrefabs").GetValue(game);
            int[] firstStyles = { 3, 2, 1, 0, 3, 2, 1, 0, 3, 2 };
            Assert.That(mainLevels, Has.Length.EqualTo(8));
            for (int q = 0; q < firstStyles.Length; q++)
            {
                GameObject prefab = (GameObject)(q < 2 ? tutorials.GetValue(q) : mainLevels.GetValue(q - 2));
                AssertDefinitionStyle(prefab, firstStyles[q]);
            }
            MethodInfo start = gameType.GetMethod("StartDream");
            MethodInfo complete = gameType.GetMethod("CompleteDream");

            start.Invoke(game, null);
            yield return WaitForState(game, true);
            Assert.That(GameObject.Find("Tutorial 1"), Is.Not.Null);
            Assert.That((int)gameType.GetProperty("TutorialStage").GetValue(game), Is.Zero);
            Assert.That((int)gameType.GetProperty("CompletedDreams").GetValue(game), Is.Zero);
            AssertDefinitionIndex(GameObject.Find("Tutorial 1"), -2);
            AssertStyle(game, 3);
            MonoBehaviour player = FindPlayer();
            Assert.That((bool)player.GetType().GetField("DashEnabled").GetValue(player), Is.False);
            player.GetType().GetMethod("SetSimulatedInput").Invoke(player, new object[] { 1f, false, true });
            player.GetType().GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(player, null);
            float dashRemaining = (float)player.GetType().GetField("dashRemaining",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player);
            Assert.That(dashRemaining, Is.Zero, "Shift started a dash in Tutorial1.");

            complete.Invoke(game, null);
            yield return WaitForState(game, false);
            Assert.That(GameObject.Find("Waking room"), Is.Not.Null);
            Assert.That((int)gameType.GetProperty("TutorialStage").GetValue(game), Is.EqualTo(1));
            Assert.That((int)gameType.GetProperty("CompletedDreams").GetValue(game), Is.Zero);
            Assert.That((int)gameType.GetField("roomChange", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(game), Is.EqualTo(1));

            start.Invoke(game, null);
            yield return WaitForState(game, true);
            Assert.That(GameObject.Find("Tutorial 2"), Is.Not.Null);
            AssertDefinitionIndex(GameObject.Find("Tutorial 2"), -1);
            AssertStyle(game, 2);
            player = FindPlayer();
            Assert.That((bool)player.GetType().GetField("DashEnabled").GetValue(player), Is.True);
            player.GetType().GetMethod("SetSimulatedInput").Invoke(player, new object[] { 1f, false, true });
            player.GetType().GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(player, null);
            dashRemaining = (float)player.GetType().GetField("dashRemaining",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player);
            Assert.That(dashRemaining, Is.GreaterThan(0f), "Shift did not start a dash in Tutorial2.");

            complete.Invoke(game, null);
            yield return WaitForState(game, false);
            Assert.That((int)gameType.GetProperty("TutorialStage").GetValue(game), Is.EqualTo(2));
            Assert.That((int)gameType.GetProperty("CompletedDreams").GetValue(game), Is.Zero);
            Assert.That((int)gameType.GetField("roomChange", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(game), Is.EqualTo(2));

            start.Invoke(game, null);
            yield return WaitForState(game, true);
            Assert.That(GameObject.Find("Dream L01"), Is.Not.Null);
            AssertStyle(game, 1);
            complete.Invoke(game, null);
            yield return WaitForState(game, false);
            Assert.That((int)gameType.GetProperty("CompletedDreams").GetValue(game), Is.EqualTo(1));

            gameType.GetField("completedDreams", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(game, 6);
            start.Invoke(game, null);
            yield return WaitForState(game, true);
            Assert.That(GameObject.Find("Dream L07"), Is.Not.Null);
            AssertStyle(game, 3);
            complete.Invoke(game, null);
            yield return WaitForState(game, false);
            Assert.That((int)gameType.GetProperty("CompletedDreams").GetValue(game), Is.EqualTo(7));
            start.Invoke(game, null);
            yield return WaitForState(game, true);
            Assert.That(GameObject.Find("Dream L08"), Is.Not.Null);
            AssertStyle(game, 2);
            complete.Invoke(game, null);
            yield return WaitForState(game, false);
            Assert.That((int)gameType.GetProperty("CompletedDreams").GetValue(game), Is.EqualTo(8));
            start.Invoke(game, null);
            yield return WaitForState(game, true);
            Assert.That(GameObject.Find("Dream L01"), Is.Not.Null);
            Assert.That(GameObject.Find("Tutorial 1"), Is.Null);
            Assert.That((int)gameType.GetField("style", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game),
                Is.InRange(0, 3), "Styles after the first cycle must still use a valid random theme.");

            gameType.GetMethod("ReturnToRoom").Invoke(game, null);
            yield return null;
            Assert.That((int)gameType.GetProperty("TutorialStage").GetValue(game), Is.Zero);
            Assert.That((int)gameType.GetProperty("CompletedDreams").GetValue(game), Is.Zero);
            start.Invoke(game, null);
            yield return WaitForState(game, true);
            Assert.That(GameObject.Find("Tutorial 1"), Is.Not.Null);
            AssertStyle(game, 3);
        }

        static IEnumerator WaitForState(MonoBehaviour game, bool dream)
        {
            Type type = game.GetType();
            float deadline = Time.realtimeSinceStartup + 10f;
            while (((bool)type.GetProperty("InDream").GetValue(game) != dream ||
                (bool)type.GetField("transitioning", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game)) &&
                Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That((bool)type.GetProperty("InDream").GetValue(game), Is.EqualTo(dream));
            Assert.That((bool)type.GetField("transitioning", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game),
                Is.False);
        }

        static MonoBehaviour FindPlayer()
        {
            GameObject actor = GameObject.Find("Dreamer");
            Assert.That(actor, Is.Not.Null);
            foreach (MonoBehaviour component in actor.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().Name == "SleepwalkingPlayer") return component;
            Assert.Fail("Dreamer has no SleepwalkingPlayer.");
            return null;
        }

        static void AssertDefinitionIndex(GameObject level, int expected)
        {
            foreach (MonoBehaviour component in level.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().Name == "SleepwalkingLevelDefinition")
                {
                    int index = (int)component.GetType().GetProperty("LevelIndex").GetValue(component);
                    Assert.That(index, Is.EqualTo(expected));
                    return;
                }
            Assert.Fail(level.name + " has no SleepwalkingLevelDefinition.");
        }

        static void AssertDefinitionStyle(GameObject level, int expected)
        {
            Assert.That(level, Is.Not.Null);
            foreach (MonoBehaviour component in level.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().Name == "SleepwalkingLevelDefinition")
                {
                    int style = Convert.ToInt32(component.GetType().GetProperty("FirstCycleStyle").GetValue(component));
                    Assert.That(style, Is.EqualTo(expected), level.name + " first-cycle theme");
                    return;
                }
            Assert.Fail(level.name + " has no SleepwalkingLevelDefinition.");
        }

        static void AssertStyle(MonoBehaviour game, int expected)
        {
            Type type = game.GetType();
            int style = (int)type.GetField("style", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game);
            Assert.That(style, Is.EqualTo(expected), "Wrong gameplay theme index.");
            GameObject theme = GameObject.Find("Dream style");
            Assert.That(theme, Is.Not.Null);
            foreach (MonoBehaviour component in theme.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().Name == "DreamTheme")
                {
                    int id = Convert.ToInt32(component.GetType().GetField("Id").GetValue(component));
                    Assert.That(id, Is.EqualTo(expected), "The visible theme differs from gameplay selection.");
                    return;
                }
            Assert.Fail("Dream style has no DreamTheme component.");
        }
    }
}
