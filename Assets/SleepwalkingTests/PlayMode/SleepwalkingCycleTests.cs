using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Sleepwalking.Tests.PlayMode
{
    public sealed class SleepwalkingCycleTests
    {
        [UnityTest]
        public IEnumerator EightDreamsReuseSevenAuthoredLevels()
        {
            AsyncOperation loading = SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
            Assert.That(loading, Is.Not.Null, "The integrated Sleepwalking scene must load.");
            while (!loading.isDone)
                yield return null;
            yield return null;

            GameObject gameObject = GameObject.Find("Sleepwalking Game");
            Assert.That(gameObject, Is.Not.Null, "The scene must contain Sleepwalking Game.");

            MonoBehaviour game = null;
            foreach (MonoBehaviour component in gameObject.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().FullName == "Sleepwalking.SleepwalkingGame")
                    game = component;
            Assert.That(game, Is.Not.Null, "The gameplay component must be present.");

            Type type = game.GetType();
            MethodInfo startDream = type.GetMethod("StartDream", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo completeDream = type.GetMethod("CompleteDream", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo killPlayer = type.GetMethod("KillPlayer", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo restartDream = type.GetMethod("RestartDream", BindingFlags.Instance | BindingFlags.Public);
            PropertyInfo inDream = type.GetProperty("InDream", BindingFlags.Instance | BindingFlags.Public);
            PropertyInfo completedDreams = type.GetProperty("CompletedDreams", BindingFlags.Instance | BindingFlags.Public);
            FieldInfo transitioning = type.GetField("transitioning", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo levelPrefabs = type.GetField("LevelPrefabs", BindingFlags.Instance | BindingFlags.Public);
            FieldInfo roomField = type.GetField("room", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo paintField = type.GetField("paint", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo dying = type.GetField("dying", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo uiField = type.GetField("UI", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(startDream, Is.Not.Null);
            Assert.That(completeDream, Is.Not.Null);
            Assert.That(killPlayer, Is.Not.Null);
            Assert.That(restartDream, Is.Not.Null);
            Assert.That(inDream, Is.Not.Null);
            Assert.That(completedDreams, Is.Not.Null);
            Assert.That(transitioning, Is.Not.Null);
            Assert.That(levelPrefabs, Is.Not.Null);
            Assert.That(roomField, Is.Not.Null);
            Assert.That(paintField, Is.Not.Null);
            Assert.That(dying, Is.Not.Null);
            Assert.That(uiField, Is.Not.Null);

            MonoBehaviour ui = uiField.GetValue(game) as MonoBehaviour;
            Assert.That(ui, Is.Not.Null, "The integrated scene must connect gameplay to UI.");
            Type uiType = ui.GetType();
            MethodInfo activateRoomTarget = uiType.GetMethod("ActivateRoomTarget", new[] { typeof(Component) });
            MethodInfo openPause = uiType.GetMethod("OpenPause", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo closeOverlay = uiType.GetMethod("CloseOverlay", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(activateRoomTarget, Is.Not.Null);
            Assert.That(openPause, Is.Not.Null);
            Assert.That(closeOverlay, Is.Not.Null);

            GameObject[] prefabs = (GameObject[])levelPrefabs.GetValue(game);
            Assert.That(prefabs, Has.Length.EqualTo(7));
            foreach (GameObject prefab in prefabs)
                Assert.That(prefab, Is.Not.Null, "All seven authored levels must be assigned.");

            Assert.That((bool)inDream.GetValue(game), Is.False);
            Assert.That((int)completedDreams.GetValue(game), Is.Zero);
            GameObject room = (GameObject)roomField.GetValue(game);
            Assert.That(room, Is.Not.Null);
            Assert.That(room.activeSelf, Is.True);

            Component dreamHotspot = null;
            foreach (MonoBehaviour component in room.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null || component.GetType().FullName != "Sleepwalking.Visual.RoomHotspot" ||
                    !component.gameObject.name.Contains("Bed")) continue;
                object action = component.GetType().GetField("Action")?.GetValue(component);
                if (action?.ToString() == "Dream") dreamHotspot = component;
            }
            Assert.That(dreamHotspot, Is.Not.Null, "The waking room must have a Dream hotspot on the bed.");

            // Восьмой вход проверяет возврат к первой геометрии после полного цикла.
            for (int q = 0; q < 8; q++)
            {
                if (q == 0) activateRoomTarget.Invoke(ui, new object[] { dreamHotspot });
                else startDream.Invoke(game, null);
                float deadline = Time.realtimeSinceStartup + 10f;
                while ((!((bool)inDream.GetValue(game)) || (bool)transitioning.GetValue(game)) && Time.realtimeSinceStartup < deadline)
                    yield return null;

                Assert.That((bool)inDream.GetValue(game), Is.True, "Dream " + (q + 1) + " did not start.");
                Assert.That((bool)transitioning.GetValue(game), Is.False, "Dream " + (q + 1) + " transition did not finish.");
                Assert.That((int)completedDreams.GetValue(game), Is.EqualTo(q));
                Assert.That(room.activeSelf, Is.False, "The waking room must be hidden during a dream.");
                Assert.That(GameObject.Find("Dream L" + (q % 7 + 1).ToString("00")), Is.Not.Null,
                    "Dream " + (q + 1) + " must instantiate its authored level, not fallback geometry.");

                if (q == 0)
                {
                    // Пауза проверяется после перехода, когда UI уже находится в режиме сна.
                    float runningTimeScale = Time.timeScale;
                    Assert.That(runningTimeScale, Is.GreaterThan(0f));
                    openPause.Invoke(ui, null);
                    yield return null;
                    Assert.That(Time.timeScale, Is.Zero, "OpenPause must stop game time.");
                    closeOverlay.Invoke(ui, null);
                    Assert.That(Time.timeScale, Is.EqualTo(runningTimeScale), "CloseOverlay must restore game time.");

                    // Смерть и ручной повтор должны оставить краску в том же сне.
                    GameObject firstLevel = GameObject.Find("Dream L01");
                    GameObject paint = (GameObject)paintField.GetValue(game);
                    GameObject dreamer = GameObject.Find("Dreamer");
                    Assert.That(paint, Is.Not.Null);
                    Assert.That(dreamer, Is.Not.Null);
                    Assert.That(paint.transform.childCount, Is.Zero);

                    Vector3 position = dreamer.transform.position;
                    killPlayer.Invoke(game, new object[] { new Vector2(position.x, position.y) });
                    Assert.That((bool)dying.GetValue(game), Is.True, "KillPlayer must begin the death coroutine.");
                    deadline = Time.realtimeSinceStartup + 5f;
                    while ((bool)dying.GetValue(game) && Time.realtimeSinceStartup < deadline)
                        yield return null;

                    Assert.That((bool)dying.GetValue(game), Is.False, "The death and retry coroutine did not finish.");
                    Assert.That((bool)inDream.GetValue(game), Is.True);
                    Assert.That((int)completedDreams.GetValue(game), Is.Zero);
                    Assert.That(GameObject.Find("Dream L01"), Is.SameAs(firstLevel));
                    Assert.That(paint.transform.childCount, Is.GreaterThan(0), "Death must leave a paint mark.");

                    int marks = paint.transform.childCount;
                    restartDream.Invoke(game, null);
                    yield return null;
                    Assert.That(GameObject.Find("Dream L01"), Is.SameAs(firstLevel));
                    Assert.That(paintField.GetValue(game), Is.SameAs(paint));
                    Assert.That(paint.transform.childCount, Is.EqualTo(marks), "Restart must preserve paint in the current dream.");
                    Assert.That((int)completedDreams.GetValue(game), Is.Zero);
                }

                completeDream.Invoke(game, null);
                deadline = Time.realtimeSinceStartup + 10f;
                while (((bool)inDream.GetValue(game) || (bool)transitioning.GetValue(game)) && Time.realtimeSinceStartup < deadline)
                    yield return null;

                Assert.That((bool)inDream.GetValue(game), Is.False, "Dream " + (q + 1) + " did not return to the room.");
                Assert.That((bool)transitioning.GetValue(game), Is.False, "Wake " + (q + 1) + " transition did not finish.");
                Assert.That((int)completedDreams.GetValue(game), Is.EqualTo(q + 1));
                Assert.That(room.activeSelf, Is.True, "The waking room must return after dream " + (q + 1) + ".");
                Assert.That(GameObject.Find("Dream L" + (q % 7 + 1).ToString("00")), Is.Null,
                    "The completed dream level must be removed.");
                if (q == 0)
                {
                    Assert.That(paintField.GetValue(game), Is.Null, "Leaving the dream must clear paint state.");
                    Assert.That(GameObject.Find("Paint marks (current dream)"), Is.Null,
                        "Leaving the dream must destroy its paint marks.");
                }
            }

            LogAssert.NoUnexpectedReceived();
        }
    }
}
