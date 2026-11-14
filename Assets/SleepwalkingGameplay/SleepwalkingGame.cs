using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace Sleepwalking
{
    public sealed partial class SleepwalkingGame : MonoBehaviour
    {
        public static SleepwalkingGame Instance { get; private set; }
        public GameObject[] LevelPrefabs = new GameObject[7];
        public GameObject[] ThemePrefabs = new GameObject[4];
        public SleepwalkingMovementProfile[] MovementProfiles = new SleepwalkingMovementProfile[3];
        public SleepwalkingMovementProfile MovementProfile;
        public GameObject RoomPrefab;
        public SleepwalkingUI UI;
        public Camera GameCamera;
        public int CompletedDreams => completedDreams;
        public bool InDream => inDream;

        readonly Color[] backgrounds = { Hex("582C30"), Hex("3C2030"), Hex("222036"), Hex("B4B8D4") };
        readonly Color[] solids = { Hex("8C5050"), Hex("6E1830"), Hex("229EDE"), Hex("90DCC0") };
        GameObject room;
        GameObject level;
        GameObject paint;
        SleepwalkingPlayer player;
        Transform start;
        Vector3 cameraVelocity;
        Vector3 dreamCameraPosition;
        Vector3 roomCameraPosition = new Vector3(0f, 1f, -20f);
        Quaternion roomCameraRotation = Quaternion.identity;
        bool roomOrthographic = true;
        float roomOrthographicSize = 5f;
        int completedDreams;
        int style;
        int roomChange;
        bool inDream;
        bool transitioning;
        bool dying;
        bool uiEventsBound;
        float fade;
        string transitionText;
        float lastMusicVolume = -1f;
        float lastSfxVolume = -1f;

        void Awake()
        {
            Instance = this;
            BuildRoom();
            if (GameCamera == null) GameCamera = Camera.main;
            if (GameCamera == null)
            {
                GameObject cameraObject = new GameObject("Game Camera");
                GameCamera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }
            roomOrthographic = GameCamera.orthographic;
            roomOrthographicSize = GameCamera.orthographicSize;
            GameCamera.backgroundColor = Hex("161318");
            if (GameCamera.GetComponent<AudioListener>() == null) GameCamera.gameObject.AddComponent<AudioListener>();
            if (GameCamera.GetComponent<Sleepwalking.Visual.PixelRoomView>() == null)
                GameCamera.gameObject.AddComponent<Sleepwalking.Visual.PixelRoomView>();
            if (UI == null) UI = FindFirstObjectByType<SleepwalkingUI>();
        }

        void Start()
        {
            if (UI != null) UI.roomCamera = GameCamera;
            EnterRoom();
            BindUiEvents();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Time.timeScale = 1f;
        }

        void Update()
        {
            UpdateAudioVolumes();
            if (inDream) UpdateBeamWidths();
            if (inDream && player != null && player.transform.position.y < -8f) KillPlayer(player.transform.position);
#if ENABLE_INPUT_SYSTEM
            UnityEngine.InputSystem.Keyboard k = UnityEngine.InputSystem.Keyboard.current;
            if (!inDream && !transitioning && RoomMenuOpen() && k != null && k.eKey.wasPressedThisFrame) StartDream();
#else
            if (!inDream && !transitioning && RoomMenuOpen() && Input.GetKeyDown(KeyCode.E)) StartDream();
#endif
            bool click = false;
#if ENABLE_INPUT_SYSTEM
            click = UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame;
#else
            click = UnityEngine.Input.GetMouseButtonDown(0);
#endif
            if (!inDream && !transitioning && RoomMenuOpen() && click && (UI == null || RoomPrefab == null))
            {
                Vector3 pointer = Vector3.zero;
#if ENABLE_INPUT_SYSTEM
                pointer = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
#else
                pointer = UnityEngine.Input.mousePosition;
#endif
                Vector3 cameraPoint = new Vector3(
                    pointer.x * GameCamera.pixelWidth / Mathf.Max(1, Screen.width),
                    pointer.y * GameCamera.pixelHeight / Mathf.Max(1, Screen.height), 0f);
                Ray ray = GameCamera.ScreenPointToRay(cameraPoint);
                if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.name.Contains("Bed")) StartDream();
            }
        }

        void LateUpdate()
        {
            if (transitioning) return;
            Vector3 target = inDream && player != null
                ? new Vector3(Mathf.Max(3f, player.transform.position.x + 1.5f), 1.8f, -20f)
                : roomCameraPosition;
            if (inDream)
            {
                // Keep the smoothing state continuous while rendering at whole-pixel positions.
                dreamCameraPosition = Vector3.SmoothDamp(dreamCameraPosition, target, ref cameraVelocity, .35f);
                GameCamera.transform.position = SnapDreamCamera(dreamCameraPosition);
            }
            else
                GameCamera.transform.position = Vector3.SmoothDamp(GameCamera.transform.position, target, ref cameraVelocity, .5f);
            GameCamera.transform.rotation = Quaternion.Slerp(GameCamera.transform.rotation, inDream ? Quaternion.identity : roomCameraRotation, Time.deltaTime * 6f);
        }

        Vector3 SnapDreamCamera(Vector3 position)
        {
            float unitsPerPixel = 2f * GameCamera.orthographicSize / Mathf.Max(1, GameCamera.pixelHeight);
            position.x = Mathf.Round(position.x / unitsPerPixel) * unitsPerPixel;
            position.y = Mathf.Round(position.y / unitsPerPixel) * unitsPerPixel;
            return position;
        }

        bool RoomMenuOpen()
        {
            if (UI == null) return true;
            Transform menu = UI.transform.Find("Sleepwalking Canvas/Main Menu");
            return menu == null || menu.gameObject.activeInHierarchy;
        }

        void OnGUI()
        {
            if (inDream)
            {
                GUI.Label(new Rect(16, 12, 650, 32), "DREAM " + (completedDreams % 7 + 1) + "/7   CYCLE " + (completedDreams / 7 + 1));
                GUI.Label(new Rect(16, 38, 650, 32), "A/D move   SPACE jump   SHIFT dash   ESC pause");
            }
            else if (UI == null || !uiEventsBound)
                GUI.Label(new Rect(16, 12, 650, 32), "Click the bed or press E to enter the next dream");
            if (transitioning)
            {
                Color previous = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, fade);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = previous;
                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.fontSize = 34;
                style.alignment = TextAnchor.MiddleCenter;
                style.normal.textColor = Hex("F8D4B8");
                GUI.Label(new Rect(0, Screen.height * .42f, Screen.width, 70), transitionText, style);
            }
        }

        void BindUiEvents()
        {
            if (UI == null) return;
            uiEventsBound = BindEvent("DreamRequested", nameof(StartDream)) |
                BindEvent("RestartRequested", nameof(RestartDream)) |
                BindEvent("MainMenuRequested", nameof(ReturnToRoom));
        }

        bool BindEvent(string eventName, string methodName)
        {
            EventInfo eventInfo = UI.GetType().GetEvent(eventName);
            if (eventInfo == null) return false;
            MethodInfo method = GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            Delegate handler = Delegate.CreateDelegate(eventInfo.EventHandlerType, this, method, false);
            if (handler == null) return false;
            eventInfo.AddEventHandler(UI, handler);
            return true;
        }

        void CallUi(string method)
        {
            if (UI == null) return;
            UI.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public)?.Invoke(UI, null);
        }

        public void StartDream()
        {
            if (inDream || transitioning) return;
            StartCoroutine(DreamTransition());
        }

        IEnumerator DreamTransition()
        {
            transitioning = true;
            transitionText = "FALLING ASLEEP";
            Vector3 cameraStart = GameCamera.transform.position;
            float sizeStart = GameCamera.orthographicSize;
            for (float elapsed = 0f; elapsed < .55f; elapsed += Time.unscaledDeltaTime)
            {
                float t = Mathf.Clamp01(elapsed / .55f);
                fade = t;
                GameCamera.transform.position = Vector3.Lerp(cameraStart, cameraStart + new Vector3(-.5f, -1f, 0f), t);
                GameCamera.orthographicSize = Mathf.Lerp(sizeStart, sizeStart * .8f, t);
                yield return null;
            }
            if (room != null) room.SetActive(false);
            GameCamera.orthographic = true;
            GameCamera.orthographicSize = 5f;
            CreateLevel();
            inDream = true;
            dreamCameraPosition = new Vector3(Mathf.Max(3f, player.transform.position.x + 1.5f), 1.8f, -20f);
            cameraVelocity = Vector3.zero;
            GameCamera.transform.position = SnapDreamCamera(dreamCameraPosition);
            GameCamera.transform.rotation = Quaternion.identity;
            Sleepwalking.Audio.SleepwalkingAudioDirector audio = Sleepwalking.Audio.SleepwalkingAudioDirector.CreateOrGet();
            if (audio != null) audio.EnterDream((Sleepwalking.Audio.DreamTheme)style);
            CallUi("EnterDream");
            for (float elapsed = 0f; elapsed < .3f; elapsed += Time.unscaledDeltaTime)
            {
                fade = 1f - elapsed / .3f;
                yield return null;
            }
            fade = 0f;
            transitioning = false;
        }

        public void CompleteDream()
        {
            if (!inDream || transitioning || dying) return;
            StartCoroutine(WakeTransition());
        }

        IEnumerator WakeTransition()
        {
            transitioning = true;
            transitionText = "WAKING UP";
            if (player != null) player.ControlsEnabled = false;
            PlayCue("Sleep");
            for (float elapsed = 0f; elapsed < .45f; elapsed += Time.unscaledDeltaTime)
            {
                fade = elapsed / .45f;
                yield return null;
            }
            inDream = false;
            completedDreams++;
            DestroyLevel();
            roomChange++;
            ApplyRoomChange();
            room.SetActive(true);
            GameCamera.transform.position = roomCameraPosition;
            GameCamera.transform.rotation = roomCameraRotation;
            GameCamera.orthographic = roomOrthographic;
            GameCamera.orthographicSize = roomOrthographicSize;
            Sleepwalking.Audio.SleepwalkingAudioDirector audio = Sleepwalking.Audio.SleepwalkingAudioDirector.CreateOrGet();
            if (audio != null) audio.EnterRoom();
            CallUi("EnterRoom");
            for (float elapsed = 0f; elapsed < .3f; elapsed += Time.unscaledDeltaTime)
            {
                fade = 1f - elapsed / .3f;
                yield return null;
            }
            fade = 0f;
            transitioning = false;
        }

        public void RestartDream()
        {
            if (!inDream || transitioning) return;
            RestartAttempt();
        }

        public void ReturnToRoom()
        {
            StopAllCoroutines();
            transitioning = dying = inDream = false;
            fade = 0f;
            completedDreams = roomChange = 0;
            PlayerPrefs.SetInt("Sleepwalking.DreamStarted", 0);
            DestroyLevel();
            room.SetActive(true);
            ApplyRoomChange();
            GameCamera.orthographic = roomOrthographic;
            GameCamera.orthographicSize = roomOrthographicSize;
            CallUi("EnterRoom");
        }

        public void KillPlayer(Vector2 contact)
        {
            if (!inDream || transitioning || dying || player == null) return;
            StartCoroutine(ImpactAndRetry(contact));
        }

        IEnumerator ImpactAndRetry(Vector2 contact)
        {
            dying = true;
            player.SetImpact();
            PaintAt(contact);
            PlayCue("Impact");
            for (float elapsed = 0f; elapsed < .28f; elapsed += Time.unscaledDeltaTime)
            {
                float fraction = elapsed / .28f;
                player.transform.localScale = new Vector3(.55f * (1f + fraction * .7f), 1f - fraction * .65f, 1f);
                player.transform.rotation = Quaternion.Euler(0, 0, fraction * 45f);
                yield return null;
            }
            player.transform.localScale = new Vector3(.55f, 1f, 1f);
            player.transform.rotation = Quaternion.identity;
            RestartAttempt();
            yield return new WaitForSecondsRealtime(.25f);
            dying = false;
        }

        void RestartAttempt()
        {
            if (level == null || player == null || start == null) return;
            foreach (SleepwalkingMechanism mechanism in level.GetComponentsInChildren<SleepwalkingMechanism>()) mechanism.ResetMechanism();
            player.ResetAt(start.position + Vector3.up * .5f);
        }

        public void PressButton(SleepwalkingMechanism button)
        {
            if (level == null || string.IsNullOrEmpty(button.LinkedFeatureId)) return;
            foreach (SleepwalkingMechanism mechanism in level.GetComponentsInChildren<SleepwalkingMechanism>())
                if (mechanism.FeatureId == button.LinkedFeatureId || mechanism.FeatureId.StartsWith(button.LinkedFeatureId + "_"))
                    mechanism.DisableFor(button.ActiveSeconds);
            PlayCue("Button");
        }

        void CreateLevel()
        {
            int index = completedDreams % 7;
            if (LevelPrefabs != null && index < LevelPrefabs.Length && LevelPrefabs[index] != null)
            {
                level = Instantiate(LevelPrefabs[index]);
                level.name = "Dream L" + (index + 1).ToString("00");
                ConnectAuthoredLevel();
            }
            else BuildFallbackLevel(index);
            style = completedDreams < 7 ? FirstStyle(level) : UnityEngine.Random.Range(0, backgrounds.Length);
            ApplyStyle();
            paint = new GameObject("Paint marks (current dream)");
            paint.transform.SetParent(level.transform);
            CreatePlayer(start.position + Vector3.up * .5f);
        }

        void ConnectAuthoredLevel()
        {
            foreach (Transform child in level.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "PlayerStart") start = child;
                MonoBehaviour[] scripts = child.GetComponents<MonoBehaviour>();
                foreach (MonoBehaviour script in scripts)
                {
                    if (script == null || script.GetType().Name != "SleepwalkingLevelFeature") continue;
                    SleepwalkingMechanism mechanism = child.gameObject.AddComponent<SleepwalkingMechanism>();
                    Type type = script.GetType();
                    mechanism.FeatureId = ReadField(type, script, "FeatureId")?.ToString() ?? child.name;
                    mechanism.Kind = ReadField(type, script, "Kind")?.ToString() ?? "";
                    mechanism.LinkedFeatureId = ReadField(type, script, "LinkedFeatureId")?.ToString();
                    mechanism.ActiveSeconds = ConvertFloat(ReadField(type, script, "ActiveSeconds"), 3f);
                    mechanism.CycleSeconds = ConvertFloat(ReadField(type, script, "CycleSeconds"), 2f);
                    mechanism.PhaseSeconds = ConvertFloat(ReadField(type, script, "PhaseSeconds"), 0f);
                    object offset = ReadField(type, script, "MoveOffset");
                    if (offset is Vector2 vector) mechanism.MoveOffset = vector;
                    if (mechanism.Kind == "LaserBeam")
                    {
                        Vector3 beamScale = child.localScale;
                        beamScale.x = BeamWidth();
                        child.localScale = beamScale;
                    }
                    if (mechanism.Kind == "Exit" && child.GetComponent<Collider2D>() == null)
                        child.gameObject.AddComponent<BoxCollider2D>().isTrigger = true;
                    mechanism.ResetMechanism();
                }
            }
            if (start == null)
            {
                GameObject marker = new GameObject("PlayerStart");
                marker.transform.SetParent(level.transform);
                marker.transform.position = new Vector3(-3f, 0f);
                start = marker.transform;
            }
        }

        static object ReadField(Type type, object instance, string name)
        {
            return type.GetField(name, BindingFlags.Instance | BindingFlags.Public)?.GetValue(instance) ??
                type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public)?.GetValue(instance);
        }

        static float ConvertFloat(object value, float fallback) => value is float number ? number : fallback;

        int FirstStyle(GameObject prefabInstance)
        {
            foreach (MonoBehaviour script in prefabInstance.GetComponents<MonoBehaviour>())
            {
                if (script == null || script.GetType().Name != "SleepwalkingLevelDefinition") continue;
                object value = ReadField(script.GetType(), script, "FirstCycleStyle");
                if (value is int number) return Mathf.Abs(number) % backgrounds.Length;
                if (value is Enum selected) return Convert.ToInt32(selected) % backgrounds.Length;
            }
            return completedDreams % backgrounds.Length;
        }

        void ApplyStyle()
        {
            GameCamera.backgroundColor = backgrounds[style];
            if (ThemePrefabs != null && style < ThemePrefabs.Length && ThemePrefabs[style] != null)
            {
                GameObject theme = Instantiate(ThemePrefabs[style], level.transform);
                theme.name = "Dream style";
                Sleepwalking.Visual.DreamTheme visual = theme.GetComponent<Sleepwalking.Visual.DreamTheme>();
                Transform geometry = level.transform.Find("Geometry");
                if (visual != null && geometry != null) visual.ApplyTo(geometry);
            }
            foreach (SpriteRenderer renderer in level.GetComponentsInChildren<SpriteRenderer>())
            {
                SleepwalkingMechanism mechanism = renderer.GetComponent<SleepwalkingMechanism>();
                if (mechanism == null || mechanism.Kind == "Solid" || mechanism.Kind == "MovingPlatform" ||
                    mechanism.Kind == "TimedPlatform")
                    renderer.color = solids[style];
            }
        }

        void DestroyLevel()
        {
            if (level != null) Destroy(level);
            if (player != null) Destroy(player.gameObject);
            level = null;
            player = null;
            paint = null;
            start = null;
        }

        void CreatePlayer(Vector3 position)
        {
            GameObject actor = new GameObject("Dreamer", typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(Rigidbody2D), typeof(SleepwalkingPlayer));
            actor.transform.position = position;
            actor.GetComponent<SpriteRenderer>().sprite = SquareSprite();
            actor.GetComponent<SpriteRenderer>().color = Hex("F8D4B8");
            actor.GetComponent<SpriteRenderer>().sortingOrder = 20;
            actor.GetComponent<BoxCollider2D>().size = Vector2.one;
            actor.transform.localScale = new Vector3(.55f, 1f, 1f);
            player = actor.GetComponent<SleepwalkingPlayer>();
            player.Profile = MovementProfile;
            player.ApplyProfile();
        }

        public void PaintAt(Vector2 position)
        {
            if (paint == null) return;
            GameObject mark = new GameObject("Paint splat", typeof(SpriteRenderer));
            mark.transform.SetParent(paint.transform);
            mark.transform.position = new Vector3(position.x, position.y, -.2f);
            mark.transform.localScale = new Vector3(.42f, .28f, 1f);
            SpriteRenderer renderer = mark.GetComponent<SpriteRenderer>();
            renderer.sprite = SquareSprite();
            renderer.color = Hex("B4202A");
            renderer.sortingOrder = 15;
        }

        public void PlayCue(string cue)
        {
            Sleepwalking.Audio.SleepwalkingAudioDirector audio = Sleepwalking.Audio.SleepwalkingAudioDirector.CreateOrGet();
            if (audio == null) return;
            if (!Enum.TryParse(cue == "Impact" ? "TrapHit" : cue == "Sleep" ? "Collision" : cue,
                out Sleepwalking.Audio.SoundCue selected)) return;
            audio.Play(selected);
        }

        void UpdateAudioVolumes()
        {
            if (UI == null || (Mathf.Approximately(UI.MusicVolume, lastMusicVolume) &&
                Mathf.Approximately(UI.SfxVolume, lastSfxVolume))) return;
            Sleepwalking.Audio.SleepwalkingAudioDirector audio = Sleepwalking.Audio.SleepwalkingAudioDirector.CreateOrGet();
            if (audio == null) return;
            lastMusicVolume = UI.MusicVolume;
            lastSfxVolume = UI.SfxVolume;
            audio.SetVolumes(lastMusicVolume, lastSfxVolume * .32f, lastSfxVolume);
        }

        float BeamWidth()
        {
            Sleepwalking.Visual.PixelRoomView view = GameCamera.GetComponent<Sleepwalking.Visual.PixelRoomView>();
            float aspect = (float)Mathf.Max(1, Screen.width) / Mathf.Max(1, Screen.height);
            int pixelHeight = GameCamera.targetTexture != null ? GameCamera.targetTexture.height :
                view != null ? Mathf.Max(64, view.BaseHeight, Mathf.CeilToInt(view.BaseWidth / aspect)) : 180;
            return 3.5f * GameCamera.orthographicSize * 2f / Mathf.Max(64, pixelHeight);
        }

        void UpdateBeamWidths()
        {
            if (level == null) return;
            float width = BeamWidth();
            foreach (SleepwalkingMechanism mechanism in level.GetComponentsInChildren<SleepwalkingMechanism>())
            {
                if (mechanism.Kind != "LaserBeam" || Mathf.Abs(mechanism.transform.localScale.x - width) < .001f) continue;
                Vector3 scale = mechanism.transform.localScale;
                scale.x = width;
                mechanism.transform.localScale = scale;
            }
        }

        static Sprite square;
        static Sprite SquareSprite()
        {
            if (square != null) return square;
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            square = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1f);
            return square;
        }

        static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString("#" + value, out Color result);
            return result;
        }
    }
}
