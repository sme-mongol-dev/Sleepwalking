using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace Sleepwalking
{
    public sealed partial class SleepwalkingGame : MonoBehaviour
    {
        public static SleepwalkingGame Instance { get; private set; }
        public GameObject[] TutorialPrefabs = new GameObject[2];
        public const int AuthoredDreamCount = 8;
        public GameObject[] LevelPrefabs = new GameObject[AuthoredDreamCount];
        public GameObject[] ThemePrefabs = new GameObject[4];
        public SleepwalkingMovementProfile[] MovementProfiles = new SleepwalkingMovementProfile[3];
        public SleepwalkingMovementProfile MovementProfile;
        public GameObject RoomPrefab;
        public SleepwalkingUI UI;
        public Camera GameCamera;
        public int CompletedDreams => completedDreams;
        public int DreamCount => LevelPrefabs != null && LevelPrefabs.Length > 0 ? LevelPrefabs.Length : AuthoredDreamCount;
        public int TutorialStage => tutorialStage;
        public bool InDream => inDream;
        public bool HasEnteredDream { get; private set; }

        readonly Color[] backgrounds = { Hex("582C30"), Hex("3C2030"), Hex("222036"), Hex("B4B8D4") };
        readonly Color[] solids = { Hex("8C5050"), Hex("6E1830"), Hex("229EDE"), Hex("90DCC0") };
        GameObject room;
        GameObject level;
        GameObject paint;
        SleepwalkingPlayer player;
        Sleepwalking.Visual.DreamTheme activeTheme;
        Transform start;
        Vector3 cameraVelocity;
        Vector3 dreamCameraPosition;
        float dreamCameraMinX;
        float dreamCameraMaxX;
        float dreamCameraY;
        float dreamCameraSize;
        float dreamCameraVerticalSize;
        float dreamCameraVelocityX;
        bool retryReturning;
        Vector3 roomCameraPosition = new Vector3(0f, 1f, -20f);
        Quaternion roomCameraRotation = Quaternion.identity;
        bool roomOrthographic = true;
        float roomOrthographicSize = 5f;
        int completedDreams;
        int tutorialStage;
        bool activeTutorial;
        int style;
        int roomChange;
        bool inDream;
        float LevelUnit => level == null ? 1f : Mathf.Abs(level.transform.lossyScale.x);
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
            ResetOdradek();
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
            if (GameCamera.TryGetComponent(out Sleepwalking.Visual.PixelRoomView pixelView))
                pixelView.enabled = false;
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
            if (inDream && !transitioning) dreamElapsedSeconds += Time.deltaTime;
            if (inDream) UpdateBeamWidths();
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
            if (inDream)
            {
                if (!retryReturning && player != null)
                {
                    float targetX = ClampDreamCameraX(player.transform.position.x);
                    dreamCameraPosition.x = Mathf.SmoothDamp(dreamCameraPosition.x, targetX,
                        ref dreamCameraVelocityX, .28f);
                    dreamCameraPosition.x = ClampDreamCameraX(dreamCameraPosition.x);
                    GameCamera.transform.position = dreamCameraPosition;
                }
                GameCamera.transform.rotation = Quaternion.identity;
                activeTheme?.FrameBackdrop(GameCamera);
                return;
            }
            GameCamera.transform.position = Vector3.SmoothDamp(GameCamera.transform.position,
                roomCameraPosition, ref cameraVelocity, .5f);
            GameCamera.transform.rotation = Quaternion.Slerp(GameCamera.transform.rotation, roomCameraRotation, Time.deltaTime * 6f);
        }

        float DreamCameraAspect()
        {
            RenderTexture target = GameCamera.targetTexture;
            return target != null ? (float)target.width / Mathf.Max(1, target.height) :
                (float)Mathf.Max(1, Screen.width) / Mathf.Max(1, Screen.height);
        }

        float ClampDreamCameraX(float position)
        {
            float halfWidth = dreamCameraSize * DreamCameraAspect();
            float min = dreamCameraMinX + halfWidth;
            float max = dreamCameraMaxX - halfWidth;
            return min >= max ? (dreamCameraMinX + dreamCameraMaxX) * .5f :
                Mathf.Clamp(position, min, max);
        }

        void FrameDreamCamera()
        {
            Transform geometry = level.transform.Find("Geometry");
            Transform shell = geometry != null ? geometry.Find("CaveShell") : null;
            Collider2D left = shell != null ? shell.Find("Cave_LeftWall")?.GetComponent<Collider2D>() : null;
            Collider2D right = shell != null ? shell.Find("Cave_RightWall")?.GetComponent<Collider2D>() : null;
            if (left == null || right == null)
            {
                dreamCameraMinX = 0f;
                dreamCameraMaxX = 14f * LevelUnit;
                dreamCameraY = 2f * LevelUnit;
                dreamCameraVerticalSize = 3f * LevelUnit;
                ResetDreamCamera();
                return;
            }

            float unit = LevelUnit;
            float minX = left.bounds.max.x - .6f * unit;
            float maxX = right.bounds.min.x + .6f * unit;
            float maxY = start.position.y + 2f * unit;
            foreach (Collider2D roof in shell.GetComponentsInChildren<Collider2D>())
                if (roof.name.StartsWith("Cave_Roof_")) maxY = Mathf.Max(maxY, roof.bounds.min.y);

            float minY = start.position.y - 1.5f * unit;
            if (geometry != null)
                foreach (Collider2D block in geometry.GetComponentsInChildren<Collider2D>())
                    if (!block.isTrigger && !block.transform.IsChildOf(shell))
                        minY = Mathf.Min(minY, block.bounds.min.y);

            minY -= .6f * unit;
            maxY += .5f * unit;
            dreamCameraMinX = minX;
            dreamCameraMaxX = maxX;
            dreamCameraY = (minY + maxY) * .5f;
            dreamCameraVerticalSize = Mathf.Max(6.5f * unit, (maxY - minY) * .5f);
            ResetDreamCamera();
        }

        void ResetDreamCamera()
        {
            dreamCameraSize = dreamCameraVerticalSize;
            GameCamera.orthographicSize = dreamCameraSize;
            dreamCameraVelocityX = 0f;
            float playerX = player != null ? player.transform.position.x :
                (dreamCameraMinX + dreamCameraMaxX) * .5f;
            dreamCameraPosition = new Vector3(ClampDreamCameraX(playerX),
                dreamCameraY, -20f);
            GameCamera.transform.position = dreamCameraPosition;
        }

        bool RoomMenuOpen()
        {
            if (UI == null) return true;
            Transform menu = UI.transform.Find("Sleepwalking Canvas/Main Menu");
            return menu == null || menu.gameObject.activeInHierarchy;
        }

        void OnGUI()
        {
            string odradekLabel = "ODRADEK " + Odradek + "/100";
            if (!inDream && LastOdradekGain > 0) odradekLabel += " (+" + LastOdradekGain + ")";
            GUI.Label(new Rect(Screen.width - 220, 12, 204, 32), odradekLabel);
            if (inDream)
            {
                string dreamLabel = activeTutorial ? "TUTORIAL " + (tutorialStage + 1) + "/2" :
                    "DREAM " + (completedDreams % DreamCount + 1) + "/" + DreamCount +
                    "   CYCLE " + (completedDreams / DreamCount + 1);
                GUI.Label(new Rect(16, 12, 650, 32), dreamLabel);
                GUI.Label(new Rect(16, 38, 650, 32), activeTutorial && tutorialStage == 0 ?
                    "A/D move   SPACE jump / wall jump   ESC pause" :
                    "A/D move   SPACE jump   SHIFT dash   ESC pause");
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
            HasEnteredDream = true;
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
            GameCamera.GetComponent<Sleepwalking.Visual.PixelRoomView>()?.SetDirectRendering(true);
            GameCamera.orthographicSize = 5f;
            BeginDreamPerformance();
            CreateLevel();
            inDream = true;
            FrameDreamCamera();
            cameraVelocity = Vector3.zero;
            GameCamera.transform.rotation = Quaternion.identity;
            activeTheme?.FrameBackdrop(GameCamera);
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
            if (activeTutorial) tutorialStage = Mathf.Min(2, tutorialStage + 1);
            else
            {
                RewardDreamPerformance();
                completedDreams++;
            }
            DestroyLevel();
            roomChange++;
            ApplyRoomChange();
            GameCamera.GetComponent<Sleepwalking.Visual.PixelRoomView>()?.SetDirectRendering(false);
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
            if (!inDream || transitioning || dying) return;
            RestartAttempt();
        }

        public void ReturnToRoom()
        {
            StopAllCoroutines();
            transitioning = dying = inDream = false;
            retryReturning = false;
            fade = 0f;
            completedDreams = tutorialStage = roomChange = 0;
            activeTutorial = false;
            ResetOdradek();
            PlayerPrefs.SetInt("Sleepwalking.DreamStarted", 0);
            DestroyLevel();
            GameCamera.GetComponent<Sleepwalking.Visual.PixelRoomView>()?.SetDirectRendering(false);
            room.SetActive(true);
            ApplyRoomChange();
            GameCamera.orthographic = roomOrthographic;
            GameCamera.orthographicSize = roomOrthographicSize;
            GameCamera.transform.position = roomCameraPosition;
            GameCamera.transform.rotation = roomCameraRotation;
            CallUi("EnterRoom");
        }

        public void KillPlayer(Vector2 contact)
        {
            if (!inDream || transitioning || dying || player == null) return;
            dreamDeaths++;
            Collider2D surface = FindImpactSurface(contact, out Vector2 point, out Vector2 normal);
            StartCoroutine(ImpactAndRetry(point, normal, surface));
        }

        Collider2D FindImpactSurface(Vector2 contact, out Vector2 point, out Vector2 normal)
        {
            point = contact;
            normal = Vector2.up;
            Collider2D playerShape = player.GetComponent<Collider2D>();
            if (playerShape == null || level == null) return null;

            Collider2D selected = null;
            float best = float.PositiveInfinity;
            foreach (Collider2D surface in level.GetComponentsInChildren<Collider2D>())
            {
                if (!surface.enabled) continue;
                SleepwalkingMechanism mechanism = surface.GetComponent<SleepwalkingMechanism>();
                if (surface.isTrigger && (mechanism == null || !mechanism.IsHazard())) continue;
                ColliderDistance2D distance = surface.Distance(playerShape);
                if (!distance.isValid || distance.distance > .12f * LevelUnit) continue;
                float score = Vector2.Distance(distance.pointA, contact);
                if (mechanism != null && mechanism.IsHazard()) score -= 2f * LevelUnit;
                if (score >= best) continue;
                best = score;
                selected = surface;
                point = distance.pointA;
                Vector2 fromSurface = surface.OverlapPoint(player.transform.position) ?
                    point - (Vector2)surface.bounds.center : (Vector2)player.transform.position - point;
                normal = fromSurface.normalized;
                if (normal.sqrMagnitude < .01f) normal = -distance.normal;
            }
            return selected;
        }

        IEnumerator ImpactAndRetry(Vector2 point, Vector2 normal, Collider2D surface)
        {
            dying = true;
            player.SetImpact();
            if (surface != null) PaintAt(point, normal, surface);
            PlayCue("Impact");
            yield return new WaitForSecondsRealtime(.72f);

            Vector3 from = player.transform.position;
            Vector3 destination = start.position + Vector3.up * (.5f * LevelUnit);
            Vector3 cameraFrom = GameCamera.transform.position;
            Vector3 cameraTo = new Vector3(ClampDreamCameraX(destination.x), dreamCameraY, -20f);
            float returnSeconds = Mathf.Clamp(Vector3.Distance(from, destination) /
                (18f * LevelUnit), .6f, 1.4f);
            player.BeginReturn();
            retryReturning = true;
            for (float elapsed = 0f; elapsed < returnSeconds; elapsed += Time.unscaledDeltaTime)
            {
                float fraction = Mathf.SmoothStep(0f, 1f, elapsed / returnSeconds);
                player.transform.position = Vector3.Lerp(from, destination, fraction);
                dreamCameraPosition = Vector3.Lerp(cameraFrom, cameraTo, fraction);
                GameCamera.transform.position = dreamCameraPosition;
                yield return null;
            }

            player.transform.position = destination;
            dreamCameraPosition = cameraTo;
            GameCamera.transform.position = cameraTo;
            dreamCameraVelocityX = 0f;
            Sleepwalking.Visual.DreamPlayerAppearance appearance =
                activeTheme != null ? activeTheme.GetComponent<Sleepwalking.Visual.DreamPlayerAppearance>() : null;
            appearance?.BeginDeathReverse();
            RestartAttempt(false);
            player.ControlsEnabled = false;
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            if (body != null) body.simulated = false;
            while (appearance != null && !appearance.DeathReverseComplete)
                yield return null;
            if (body != null) body.simulated = true;
            player.ControlsEnabled = true;
            retryReturning = false;
            dying = false;
        }

        void RestartAttempt(bool resetCamera = true)
        {
            if (level == null || player == null || start == null) return;
            foreach (SleepwalkingMechanism mechanism in level.GetComponentsInChildren<SleepwalkingMechanism>()) mechanism.ResetMechanism();
            player.ResetAt(start.position + Vector3.up * (.5f * LevelUnit));
            if (resetCamera) ResetDreamCamera();
        }

        public void PressButton(SleepwalkingMechanism button)
        {
            if (level == null || string.IsNullOrEmpty(button.LinkedFeatureId)) return;
            button.MarkPressed();
            foreach (SleepwalkingMechanism mechanism in level.GetComponentsInChildren<SleepwalkingMechanism>())
                if (mechanism.Kind != "LaserBase" &&
                    (mechanism.FeatureId == button.LinkedFeatureId ||
                    mechanism.FeatureId.StartsWith(button.LinkedFeatureId + "_")))
                    mechanism.DisableFor(button.ActiveSeconds);
            PlayCue("Button");
        }

        void CreateLevel()
        {
            activeTutorial = completedDreams == 0 && tutorialStage < 2 &&
                TutorialPrefabs != null && tutorialStage < TutorialPrefabs.Length &&
                TutorialPrefabs[tutorialStage] != null;
            int index = completedDreams % DreamCount;
            if (activeTutorial)
            {
                level = Instantiate(TutorialPrefabs[tutorialStage]);
                level.name = "Tutorial " + (tutorialStage + 1);
                ConnectAuthoredLevel();
            }
            else if (LevelPrefabs != null && index < LevelPrefabs.Length && LevelPrefabs[index] != null)
            {
                level = Instantiate(LevelPrefabs[index]);
                level.name = "Dream L" + (index + 1).ToString("00");
                ConnectAuthoredLevel();
            }
            else BuildFallbackLevel(index);
            style = completedDreams < DreamCount ? FirstStyle(level) : UnityEngine.Random.Range(0, backgrounds.Length);
            ApplyStyle();
            paint = new GameObject("Paint marks (current dream)");
            paint.transform.SetParent(level.transform);
            CreatePlayer(start.position + Vector3.up * (.5f * LevelUnit));
            player.DashEnabled = !activeTutorial || tutorialStage > 0;
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
                        beamScale.x = BeamLocalWidth(child);
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
            activeTheme = null;
            if (ThemePrefabs != null && style < ThemePrefabs.Length && ThemePrefabs[style] != null)
            {
                GameObject theme = Instantiate(ThemePrefabs[style], level.transform);
                theme.name = "Dream style";
                Sleepwalking.Visual.DreamTheme visual = theme.GetComponent<Sleepwalking.Visual.DreamTheme>();
                activeTheme = visual;
                Transform geometry = level.transform.Find("Geometry");
                if (visual != null && geometry != null) visual.ApplyTo(geometry);
                if (inDream) visual?.FrameBackdrop(GameCamera);
            }
            Transform tutorialHints = level.transform.Find("Readability/TutorialHints");
            foreach (SpriteRenderer renderer in level.GetComponentsInChildren<SpriteRenderer>())
            {
                if (tutorialHints != null && renderer.transform.IsChildOf(tutorialHints)) continue;
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
            activeTheme = null;
        }

        void CreatePlayer(Vector3 position)
        {
            GameObject actor = new GameObject("Dreamer", typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(Rigidbody2D), typeof(SleepwalkingPlayer));
            actor.transform.position = position;
            actor.GetComponent<SpriteRenderer>().sprite = SquareSprite();
            actor.GetComponent<SpriteRenderer>().color = Hex("F8D4B8");
            actor.GetComponent<SpriteRenderer>().sortingOrder = 20;
            actor.GetComponent<BoxCollider2D>().size = Vector2.one;
            actor.transform.localScale = new Vector3(.55f * LevelUnit, LevelUnit, 1f);
            player = actor.GetComponent<SleepwalkingPlayer>();
            player.WorldUnitsPerCell = LevelUnit;
            player.Profile = MovementProfile;
            player.ApplyProfile();
        }

        public void PaintAt(Vector2 position, Vector2 normal, Collider2D surface)
        {
            if (paint == null || surface == null || !surface.enabled ||
                !surface.transform.IsChildOf(level.transform)) return;
            normal = normal.sqrMagnitude < .01f ? Vector2.up : normal.normalized;
            Vector2 edge = position;
            float thickness = Mathf.Min(.2f * LevelUnit,
                Mathf.Min(surface.bounds.size.x, surface.bounds.size.y) * .8f);
            if (thickness < .015f * LevelUnit) return;
            float width = Mathf.Min(.44f * LevelUnit,
                Mathf.Max(surface.bounds.size.x, surface.bounds.size.y) * .8f);
            if (surface is CircleCollider2D || surface is PolygonCollider2D)
            {
                width = Mathf.Min(width, .24f * LevelUnit);
                thickness = Mathf.Min(thickness, .12f * LevelUnit);
            }
            Vector2 center = surface is CircleCollider2D ?
                Vector2.Lerp(edge, surface.bounds.center, .75f) :
                edge - normal * (thickness * .42f);
            if (!(surface is PolygonCollider2D))
                center += new Vector2(-normal.y, normal.x) *
                    (Mathf.Sin((paint.transform.childCount + 1) * 2.4f) * .08f * LevelUnit);
            GameObject mark = new GameObject("Paint splat", typeof(SpriteRenderer), typeof(SleepwalkingSurfaceMark));
            mark.transform.SetParent(paint.transform, false);
            mark.transform.position = new Vector3(center.x, center.y, surface.transform.position.z - 1.1f);
            mark.transform.rotation = Quaternion.FromToRotation(Vector3.up, -normal);
            mark.transform.localScale = new Vector3(width, thickness, 1f);
            SpriteRenderer renderer = mark.GetComponent<SpriteRenderer>();
            renderer.sprite = ClippedPaintSprite(paint.transform.childCount % 3, mark.transform, surface);
            renderer.color = style == 0 ? Hex("F8D4B8") :
                style == 3 ? Hex("B4202A") : Hex("E04020");
            renderer.sortingOrder = 16;
            mark.GetComponent<SleepwalkingSurfaceMark>().Attach(surface.transform, renderer.sprite);
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
            // Сохраняем прежнюю толщину луча в мире после отключения пикселизации камеры.
            float aspect = DreamCameraAspect();
            int referenceHeight = Mathf.Max(180, Mathf.CeilToInt(320 / aspect));
            return 3.5f * GameCamera.orthographicSize * 2f / referenceHeight;
        }

        float BeamLocalWidth(Transform beam)
        {
            float parentScale = beam.parent == null ? 1f : Mathf.Abs(beam.parent.lossyScale.x);
            return BeamWidth() / Mathf.Max(.001f, parentScale);
        }

        void UpdateBeamWidths()
        {
            if (level == null) return;
            foreach (SleepwalkingMechanism mechanism in level.GetComponentsInChildren<SleepwalkingMechanism>())
            {
                float width = BeamLocalWidth(mechanism.transform);
                if (mechanism.Kind != "LaserBeam" || Mathf.Abs(mechanism.transform.localScale.x - width) < .001f) continue;
                Vector3 scale = mechanism.transform.localScale;
                scale.x = width;
                mechanism.transform.localScale = scale;
            }
        }

        static Sprite square;
        static readonly Sprite[] paintSprites = new Sprite[3];
        static Sprite ClippedPaintSprite(int variant, Transform mark, Collider2D surface)
        {
            Sprite source = PaintSprite(variant);
            const int pixels = 24;
            Texture2D texture = new Texture2D(pixels, pixels, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < pixels; y++)
            for (int x = 0; x < pixels; x++)
            {
                Color color = source.texture.GetPixel(x, y);
                Vector3 local = new Vector3((x + .5f) / pixels - .5f,
                    (y + .5f) / pixels - .5f, 0f);
                if (color.a > 0f && !surface.OverlapPoint(mark.TransformPoint(local)))
                    color = Color.clear;
                texture.SetPixel(x, y, color);
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, pixels, pixels),
                new Vector2(.5f, .5f), pixels);
        }

        static Sprite PaintSprite(int variant)
        {
            if (paintSprites[variant] != null) return paintSprites[variant];
            const int pixels = 24;
            Texture2D texture = new Texture2D(pixels, pixels, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < pixels; y++)
            for (int x = 0; x < pixels; x++)
            {
                float u = (x + .5f - pixels * .5f) / (pixels * .5f);
                float v = (y + .5f - pixels * .5f) / (pixels * .5f);
                float ripple = .10f * Mathf.Sin(x * 2.1f + variant * 3f) +
                    .08f * Mathf.Sin(y * 1.7f + variant * 5f);
                bool body = u * u / (.75f + ripple) + v * v / (.48f + ripple) < 1f;
                bool lobe = (u + .48f) * (u + .48f) + (v - .16f) * (v - .16f) < .12f;
                bool drop = (u - .59f) * (u - .59f) + (v + .51f) * (v + .51f) < .025f;
                texture.SetPixel(x, y, body || lobe || drop ? Color.white : Color.clear);
            }
            texture.Apply();
            paintSprites[variant] = Sprite.Create(texture, new Rect(0, 0, pixels, pixels),
                new Vector2(.5f, .5f), pixels);
            return paintSprites[variant];
        }

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
