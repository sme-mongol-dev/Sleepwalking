using System.Collections;
using System;
using System.Globalization;
using System.Reflection;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
#endif

namespace Sleepwalking.Visual
{
    public sealed class AutoDreamPreview : MonoBehaviour
    {
        IEnumerator Start()
        {
            // Только отдельная QA-сцена запускает первый сон без ручного ввода.
            yield return null;
            Sleepwalking.SleepwalkingGame game = FindFirstObjectByType<Sleepwalking.SleepwalkingGame>();
            if (game != null)
            {
                int level = ReadInt("-previewLevel=", 1);
                if (level >= 1 && level <= 7)
                    typeof(Sleepwalking.SleepwalkingGame).GetField("completedDreams", BindingFlags.Instance | BindingFlags.NonPublic)
                        ?.SetValue(game, level - 1);
                game.StartDream();
            }
            string view = ReadText("-previewView=");
            if (!string.IsNullOrEmpty(view))
                yield return PlaceDreamerForView(view);
            if (Array.Exists(Environment.GetCommandLineArgs(), value => value == "-previewMotion"))
                yield return CheckPreviewMotion();
        }

        IEnumerator PlaceDreamerForView(string value)
        {
            yield return new WaitForSecondsRealtime(1.5f);
            string[] parts = value.Split(',');
            if (parts.Length != 2 ||
                !float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) ||
                !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
            {
                Debug.LogError("Preview view: expected X,Y");
                yield break;
            }
            Sleepwalking.SleepwalkingPlayer actor = FindFirstObjectByType<Sleepwalking.SleepwalkingPlayer>();
            if (actor == null) { Debug.LogError("Preview view: Dreamer missing"); yield break; }
            // Фиксируем персонажа только в QA-сцене, чтобы показать верхний край камеры.
            actor.ControlsEnabled = false;
            actor.GetComponent<Rigidbody2D>().simulated = false;
            actor.transform.position = new Vector3(x, y, actor.transform.position.z);
            Debug.Log("Preview view Dreamer: " + x.ToString("F2") + "," + y.ToString("F2"));
        }

        static int ReadInt(string prefix, int fallback) => int.TryParse(ReadText(prefix), out int value) ? value : fallback;

        static string ReadText(string prefix)
        {
            foreach (string value in Environment.GetCommandLineArgs())
                if (value.StartsWith(prefix, StringComparison.Ordinal)) return value.Substring(prefix.Length);
            return "";
        }

        IEnumerator CheckPreviewMotion()
        {
            // Ввод существует только в отдельной QA-сцене, не в основной игре.
            yield return new WaitForSecondsRealtime(2f);
            Sleepwalking.SleepwalkingPlayer actor = FindFirstObjectByType<Sleepwalking.SleepwalkingPlayer>();
            if (actor == null) { Debug.LogError("Preview motion: Dreamer missing"); yield break; }
            float before = actor.transform.position.x;
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) { Debug.LogError("Preview motion: Keyboard missing"); yield break; }
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
            yield return new WaitForSecondsRealtime(.14f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
#else
            Debug.LogError("Preview motion: Input System is unavailable");
            yield break;
#endif
            yield return new WaitForSecondsRealtime(.25f);
            Debug.Log("Preview motion Dreamer X: " + before.ToString("F3") + " -> " + actor.transform.position.x.ToString("F3"));
        }
    }
}
