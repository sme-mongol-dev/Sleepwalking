using System;
using System.Collections.Generic;
using Sleepwalking.Audio;
using Sleepwalking.Visual;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Sleepwalking
{
    public enum UICommand { NewGame, Resume, Restart, Settings, Credits, MainMenu, Quit, Back, Confirm, Cancel }
    public enum RoomAction { Dream, Settings, Credits, Quit }

    // Коллайдер предмета остаётся частью редактируемого префаба комнаты.
    public sealed class SleepwalkingRoomTarget : MonoBehaviour
    {
        public RoomAction Action;
        public SleepwalkingUI UI;
        public string Label => Action == RoomAction.Dream ? "NEW GAME" : Action == RoomAction.Quit ? "QUIT GAME" : Action.ToString().ToUpperInvariant();
        public void Activate()
        {
            SleepwalkingUI owner = UI != null ? UI : FindFirstObjectByType<SleepwalkingUI>();
            if (owner != null) owner.ActivateRoomTarget(this);
        }
    }

    public sealed class UIAction : MonoBehaviour
    {
        public UICommand Command;
        public SleepwalkingUI Owner;

        void Awake()
        {
            GetComponent<Button>().onClick.AddListener(() => Owner.Handle(Command));
        }
    }

    public sealed class UIStyledButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        public SleepwalkingUI Owner;
        public Image Border;
        public Image Fill;
        public Text Label;
        public bool Primary;

        bool hover;
        bool pressed;
        bool focused;

        void OnEnable() { Refresh(); }
        void Update() { Refresh(); }
        public void OnPointerEnter(PointerEventData eventData) { if (!hover) Owner?.PlayHover(); hover = true; Refresh(); }
        public void OnPointerExit(PointerEventData eventData) { hover = false; pressed = false; Refresh(); }
        public void OnPointerDown(PointerEventData eventData) { pressed = true; Refresh(); }
        public void OnPointerUp(PointerEventData eventData) { pressed = false; Refresh(); }
        public void OnSelect(BaseEventData eventData) { if (!focused && !hover) Owner?.PlayHover(); focused = true; Refresh(); }
        public void OnDeselect(BaseEventData eventData) { focused = false; Refresh(); }

        void Refresh()
        {
            if (Border == null || Fill == null || Label == null) return;
            Button button = GetComponent<Button>();
            bool enabled = button == null || button.interactable;
            Color edge = SleepwalkingUI.FrameColor;
            Color fill = Primary ? SleepwalkingUI.Accent : SleepwalkingUI.Background;
            Color text = SleepwalkingUI.MainText;
            if (enabled && pressed)
            {
                edge = fill = SleepwalkingUI.Pressed;
            }
            else if (enabled && (hover || focused))
            {
                edge = fill = SleepwalkingUI.Hover;
                text = SleepwalkingUI.Background;
            }
            Border.color = edge;
            Fill.color = fill;
            Label.color = text;
            CanvasGroup group = GetComponent<CanvasGroup>();
            if (group != null) group.alpha = enabled ? 1f : .4f;
            Label.rectTransform.anchoredPosition = new Vector2(0, enabled && pressed ? -4 : 0);
        }
    }

    public sealed class SleepwalkingUI : MonoBehaviour
    {
        public static readonly Color Background = Hex("161318");
        public static readonly Color Panel = Hex("222036");
        public static readonly Color FrameColor = Hex("8C5050");
        public static readonly Color Shadow = Hex("3C2030");
        public static readonly Color MainText = Hex("F8D4B8");
        public static readonly Color SecondaryText = Hex("B4B8D4");
        public static readonly Color Accent = Hex("B4202A");
        public static readonly Color Hover = Hex("E04020");
        public static readonly Color Pressed = Hex("6E1830");
        public static readonly Color GlitchBlue = Hex("229EDE");

        GameObject menu;
        GameObject pause;
        GameObject settings;
        GameObject credits;
        GameObject confirm;
        GameObject dim;
        Text roomLabel;
        Text roomHint;
        RectTransform roomLabelFrame;
        RectTransform logoFrame;
        Image roomLabelBorder;
        Text confirmTitle;
        Text confirmMessage;
        UICommand pending;
        GameObject previous;
        GameObject settingsPrevious;
        bool inRoom = true;
        bool dreamStarted;
        bool paused;
        float previousTimeScale = 1f;
        Component hoveredTarget;
        Component focusedTarget;
        Vector2 lastPointerPosition;
        Camera fittedRoomCamera;
        float baseRoomSize;
        float fittedAspect = -1f;
        public Camera roomCamera;
        public event Action DreamRequested;
        public event Action RestartRequested;
        public event Action MainMenuRequested;
        public bool BlocksGameplayInput => inRoom || paused || settings != null && settings.activeSelf || credits != null && credits.activeSelf || confirm != null && confirm.activeSelf;
        Font titleFont;
        Font bodyFont;
        Font buttonFont;

        void Awake()
        {
            if (PlayerPrefs.HasKey("Sleepwalking.Width") || PlayerPrefs.HasKey("Sleepwalking.Fullscreen"))
                Screen.SetResolution(PlayerPrefs.GetInt("Sleepwalking.Width", Screen.width), PlayerPrefs.GetInt("Sleepwalking.Height", Screen.height), PlayerPrefs.GetInt("Sleepwalking.Fullscreen", Screen.fullScreen ? 1 : 0) != 0);
            Transform savedCanvas = transform.Find("Sleepwalking Canvas");
            if (savedCanvas == null || savedCanvas.Find("Room Label") == null) Build();
            else
            {
                ReadScene(savedCanvas);
                BindSettings();
                LoadFonts();
                ApplyFonts(savedCanvas);
            }
            AudioListener.volume = PlayerPrefs.GetFloat("Sleepwalking.Master", 80f) / 100f;
            UpdateAudioVolumes();
            EnterRoom();
        }

        void ReadScene(Transform canvas)
        {
            menu = canvas.Find("Main Menu").gameObject;
            logoFrame = (RectTransform)menu.transform.Find("Sleepwalking Title");
            pause = canvas.Find("Pause").gameObject;
            settings = canvas.Find("Settings").gameObject;
            credits = canvas.Find("Credits").gameObject;
            dim = canvas.Find("Dim Overlay").gameObject;
            confirm = canvas.Find("Confirmation").gameObject;
            roomLabelFrame = (RectTransform)canvas.Find("Room Label");
            roomLabel = roomLabelFrame.Find("Action Label").GetComponent<Text>();
            roomLabelBorder = roomLabelFrame.GetComponent<Image>();
            roomHint = menu.transform.Find("Room Hint").GetComponent<Text>();
            confirmTitle = confirm.transform.Find("Confirm Panel/Quit? Title/Title Text").GetComponent<Text>();
            confirmMessage = confirm.transform.Find("Confirm Panel/Confirm Message").GetComponent<Text>();
            foreach (UIStyledButton button in canvas.GetComponentsInChildren<UIStyledButton>(true)) button.Owner = this;
        }

        void BindSettings()
        {
            Transform panel = settings.transform.Find("Settings Panel");
            string[] keys = { "Master", "Music", "SFX" };
            foreach (string key in keys)
            {
                Slider slider = panel.Find(key + " Slider").GetComponent<Slider>();
                slider.SetValueWithoutNotify(PlayerPrefs.GetFloat("Sleepwalking." + key, 80f));
                slider.onValueChanged.AddListener(value =>
                {
                    PlayerPrefs.SetFloat("Sleepwalking." + key, value);
                    if (key == "Master") AudioListener.volume = value / 100f;
                    UpdateAudioVolumes();
                });
            }
            Toggle fullscreen = panel.Find("Fullscreen Toggle").GetComponent<Toggle>();
            fullscreen.SetIsOnWithoutNotify(Screen.fullScreen);
            fullscreen.onValueChanged.AddListener(value => { Screen.fullScreen = value; PlayerPrefs.SetInt("Sleepwalking.Fullscreen", value ? 1 : 0); });
            Transform dropdown = panel.Find("Resolution Dropdown");
            Transform options = dropdown.Find("Options");
            Text label = dropdown.Find("Selection").GetComponent<Text>();
            label.text = Screen.width + "×" + Screen.height + "  ▾";
            dropdown.GetComponent<Button>().onClick.AddListener(() => options.gameObject.SetActive(!options.gameObject.activeSelf));
            foreach (Transform option in options)
            {
                string value = option.name;
                option.GetComponent<Button>().onClick.AddListener(() =>
                {
                    label.text = value + "  ▾";
                    ApplyResolution(value);
                    options.gameObject.SetActive(false);
                });
            }
        }

        void OnDestroy()
        {
            if (paused) Time.timeScale = previousTimeScale;
        }

        void Update()
        {
            bool escape = false;
#if ENABLE_INPUT_SYSTEM
            escape = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            escape = Input.GetKeyDown(KeyCode.Escape);
#endif
            if (escape)
            {
                if (confirm.activeSelf || settings.activeSelf || credits.activeSelf || pause.activeSelf) CloseOverlay();
                else if (!inRoom) OpenPause();
            }
            UpdateRoomTargets();
        }

        void LateUpdate()
        {
            Rect canvas = ((RectTransform)menu.transform.parent).rect;
            Fit(menu, canvas, 1360, 900);
            FitLogo(canvas);
            Fit(pause, canvas, 800, 990);
            Fit(settings, canvas, 1120, 810);
            Fit(credits, canvas, 800, 540);
            Fit(confirm, canvas, 800, 470);
            if (inRoom) FitRoomCamera();
        }

        void FitRoomCamera()
        {
            Camera camera = roomCamera != null ? roomCamera : Camera.main;
            if (camera == null || !camera.orthographic) return;
            if (camera != fittedRoomCamera)
            {
                fittedRoomCamera = camera;
                baseRoomSize = camera.orthographicSize;
                fittedAspect = -1f;
            }
            float aspect = Mathf.Max(.1f, camera.aspect);
            if (Mathf.Abs(aspect - fittedAspect) < .002f) return;

            // Сохраняем видимость всех предметов меню на узких экранах.
            float left = 0f;
            float right = 0f;
            Vector3 axis = camera.transform.right;
            foreach (Collider collider in FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if (FindRoomTarget(collider) == null) continue;
                Bounds bounds = collider.bounds;
                float center = Vector3.Dot(axis, bounds.center - camera.transform.position);
                float radius = Mathf.Abs(axis.x) * bounds.extents.x + Mathf.Abs(axis.y) * bounds.extents.y + Mathf.Abs(axis.z) * bounds.extents.z;
                left = Mathf.Min(left, center - radius);
                right = Mathf.Max(right, center + radius);
            }
            float halfWidth = Mathf.Max(-left, right) + .25f;
            camera.orthographicSize = Mathf.Max(baseRoomSize, halfWidth / aspect);
            fittedAspect = aspect;
        }

        static void Fit(GameObject screen, Rect canvas, float width, float height)
        {
            float scale = Mathf.Min(1f, canvas.width / width, canvas.height / height);
            screen.transform.localScale = Vector3.one * scale;
        }

        void FitLogo(Rect canvas)
        {
            if (logoFrame == null) return;
            float wide = Mathf.InverseLerp(4f / 3f, 16f / 9f, canvas.width / canvas.height);
            logoFrame.anchoredPosition = new Vector2(-100f, Mathf.Lerp(465f, 422f, wide));
            logoFrame.localScale = Vector3.one * .81f;
        }

        public void Build()
        {
            // Повторная сборка очищает только UI, созданный этим компонентом.
            Transform old = transform.Find("Sleepwalking Canvas");
            if (old != null) DestroyImmediate(old.gameObject);

            LoadFonts();

            GameObject canvasObject = new GameObject("Sleepwalking Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("UI Event System", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
                eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                eventSystem.AddComponent<StandaloneInputModule>();
#endif
            }

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            menu = ScreenRoot(canvasRect, "Main Menu");
            pause = ScreenRoot(canvasRect, "Pause");
            settings = ScreenRoot(canvasRect, "Settings");
            credits = ScreenRoot(canvasRect, "Credits");
            dim = FullRect(canvasRect, "Dim Overlay", new Color(10f / 255, 6f / 255, 8f / 255, .70f)).gameObject;
            dim.transform.SetSiblingIndex(1);
            confirm = ScreenRoot(canvasRect, "Confirmation");
            roomLabelFrame = Rect("Room Label", canvasRect, Vector2.zero, new Vector2(360, 76));
            Image labelBorder = roomLabelFrame.gameObject.AddComponent<Image>();
            labelBorder.color = Hover;
            labelBorder.raycastTarget = false;
            roomLabelBorder = labelBorder;
            RectTransform labelShadow = Rect("Shadow", roomLabelFrame, new Vector2(8, -8), new Vector2(360, 76));
            Image shadowImage = labelShadow.gameObject.AddComponent<Image>();
            shadowImage.color = Shadow;
            shadowImage.raycastTarget = false;
            RectTransform labelFill = Rect("Fill", roomLabelFrame, Vector2.zero, new Vector2(352, 68));
            Image labelBackground = labelFill.gameObject.AddComponent<Image>();
            labelBackground.color = Background;
            labelBackground.raycastTarget = false;
            roomLabel = MakeText(roomLabelFrame, "Action Label", "", buttonFont, 28, MainText, Vector2.zero, new Vector2(340, 68));
            roomLabel.fontStyle = FontStyle.Bold;
            roomLabelFrame.gameObject.SetActive(false);

            BuildMenu(menu.transform);
            BuildPause(pause.transform);
            BuildSettings(settings.transform);
            BuildCredits(credits.transform);
            BuildConfirmation(confirm.transform);
            Show(menu);
        }

        void LoadFonts()
        {
            titleFont = Resources.Load<Font>("SleepwalkingFonts/CormorantGaramond-Italic") ?? Font.CreateDynamicFontFromOSFont(new[] { "Cormorant Garamond", "Georgia" }, 64);
            bodyFont = Resources.Load<Font>("SleepwalkingFonts/SpaceMono-Regular") ?? Font.CreateDynamicFontFromOSFont(new[] { "Space Mono", "Consolas" }, 26);
            buttonFont = Resources.Load<Font>("SleepwalkingFonts/SpaceMono-Bold") ?? Font.CreateDynamicFontFromOSFont(new[] { "Space Mono", "Consolas" }, 28);
        }

        void ApplyFonts(Transform canvas)
        {
            foreach (Text text in canvas.GetComponentsInChildren<Text>(true))
            {
                bool title = text.transform.parent.name.EndsWith(" Title", StringComparison.Ordinal);
                bool button = text.name == "Action Label" || text.name == "Label" && text.transform.parent.name.EndsWith(" Button", StringComparison.Ordinal);
                text.font = title ? titleFont : button ? buttonFont : bodyFont;
                if (title) text.fontStyle = titleFont.name.Contains("Italic") ? FontStyle.Normal : FontStyle.Italic;
                else if (button) text.fontStyle = buttonFont.name.Contains("Bold") ? FontStyle.Normal : FontStyle.Bold;
            }
        }

        GameObject ScreenRoot(RectTransform parent, string name)
        {
            RectTransform root = Rect(name, parent, Vector2.zero, Vector2.zero);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
            return root.gameObject;
        }

        void BuildMenu(Transform root)
        {
            RectTransform parent = (RectTransform)root;
            Text logo = Title(parent, "Sleepwalking", new Vector2(-100, 422), new Vector2(1300, 180), 144, true);
            logoFrame = (RectTransform)logo.transform.parent;
            Text subtitle = MakeText(parent, "Subtitle", "Full Circle of your dream", bodyFont, 26, SecondaryText, new Vector2(-100, 50), new Vector2(900, 52));
            subtitle.fontStyle = FontStyle.Normal;
            roomHint = MakeText(parent, "Room Hint", "HOVER AN OBJECT  /  TAB TO FOCUS", bodyFont, 26, SecondaryText, new Vector2(0, -380), new Vector2(900, 52));
            Text version = MakeText(parent, "Version", "v0.1", bodyFont, 26, SecondaryText, Vector2.zero, new Vector2(250, 50));
            version.alignment = TextAnchor.MiddleLeft;
            version.rectTransform.anchorMin = version.rectTransform.anchorMax = new Vector2(0, 0);
            version.rectTransform.pivot = new Vector2(0, 0);
            version.rectTransform.anchoredPosition = new Vector2(40, 40);
        }

        void BuildPause(Transform root)
        {
            RectTransform panel = MakePanel((RectTransform)root, "Pause Panel", new Vector2(720, 950), FrameColor);
            Title(panel, "PAUSED", new Vector2(0, 360), new Vector2(580, 100), 64);
            ButtonAt(panel, "RESUME", UICommand.Resume, new Vector2(0, 234), true);
            ButtonAt(panel, "RESTART", UICommand.Restart, new Vector2(24, 114));
            ButtonAt(panel, "SETTINGS", UICommand.Settings, new Vector2(0, -6));
            ButtonAt(panel, "MAIN MENU", UICommand.MainMenu, new Vector2(24, -126));
            ButtonAt(panel, "QUIT GAME", UICommand.Quit, new Vector2(0, -246));
            MakeText(panel, "Pause Hint", "ESC — resume", bodyFont, 26, SecondaryText, new Vector2(0, -388), new Vector2(540, 50));
        }

        void BuildSettings(Transform root)
        {
            RectTransform panel = MakePanel((RectTransform)root, "Settings Panel", new Vector2(1040, 770), FrameColor);
            Title(panel, "SETTINGS", new Vector2(0, 296), new Vector2(840, 100), 64);
            string[] names = { "Master Volume", "Music Volume", "SFX Volume" };
            string[] keys = { "Master", "Music", "SFX" };
            for (int q = 0; q < 3; q++)
            {
                float y = 175 - 82 * q;
                Text label = MakeText(panel, names[q], names[q], bodyFont, 26, MainText, new Vector2(-285, y), new Vector2(380, 50));
                label.alignment = TextAnchor.MiddleLeft;
                MakeSlider(panel, keys[q], new Vector2(210, y));
            }
            Text fullLabel = MakeText(panel, "Fullscreen Label", "Fullscreen", bodyFont, 26, MainText, new Vector2(-285, -82), new Vector2(380, 50));
            fullLabel.alignment = TextAnchor.MiddleLeft;
            MakeToggle(panel, new Vector2(419, -82));
            Text resolutionLabel = MakeText(panel, "Resolution Label", "Resolution", bodyFont, 26, MainText, new Vector2(-285, -165), new Vector2(380, 50));
            resolutionLabel.alignment = TextAnchor.MiddleLeft;
            MakeDropdown(panel, new Vector2(317, -165));
            ButtonAt(panel, "BACK", UICommand.Back, new Vector2(0, -285), true);
        }

        void BuildCredits(Transform root)
        {
            RectTransform panel = MakePanel((RectTransform)root, "Credits Panel", new Vector2(720, 500), FrameColor);
            Title(panel, "CREDITS", new Vector2(0, 145), new Vector2(600, 100), 64);
            MakeText(panel, "Credits Body", "SLEEPWALKING GAME JAM PROJECT\n\nAUDIO SOURCES\ntechiew, bart, rubberduck\nSketchMan3, Cleyton Kauffman", bodyFont, 24, SecondaryText, new Vector2(0, 3), new Vector2(620, 230));
            ButtonAt(panel, "BACK", UICommand.Back, new Vector2(0, -170), true);
        }

        void BuildConfirmation(Transform root)
        {
            RectTransform panel = MakePanel((RectTransform)root, "Confirm Panel", new Vector2(720, 430), FrameColor);
            confirmTitle = Title(panel, "Quit?", new Vector2(0, 126), new Vector2(610, 80), 48);
            confirmMessage = MakeText(panel, "Confirm Message", "Are you sure?", bodyFont, 26, SecondaryText, new Vector2(0, 18), new Vector2(600, 110));
            ButtonAt(panel, "CANCEL", UICommand.Cancel, new Vector2(-150, -122), false, new Vector2(260, 96));
            ButtonAt(panel, "CONFIRM", UICommand.Confirm, new Vector2(150, -122), true, new Vector2(260, 96));
        }

        RectTransform MakePanel(RectTransform parent, string name, Vector2 size, Color edge)
        {
            RectTransform shadow = Rect(name + " Shadow", parent, new Vector2(12, -12), size);
            shadow.gameObject.AddComponent<Image>().color = Shadow;
            RectTransform glow = Rect(name + " Glow", parent, Vector2.zero, size + new Vector2(44, 44));
            glow.gameObject.AddComponent<Image>().color = new Color(Accent.r, Accent.g, Accent.b, .12f);
            glow.SetSiblingIndex(shadow.GetSiblingIndex());
            RectTransform panel = Rect(name, parent, Vector2.zero, size);
            panel.gameObject.AddComponent<Image>().color = edge;
            RectTransform inside = Rect("Interior", panel, Vector2.zero, size - new Vector2(8, 8));
            inside.gameObject.AddComponent<Image>().color = Panel;
            return panel;
        }

        Text Title(RectTransform parent, string words, Vector2 position, Vector2 size, int fontSize, bool tilt = false)
        {
            RectTransform group = Rect(words + " Title", parent, position, size);
            if (tilt) group.localRotation = Quaternion.Euler(0, 0, -1.5f);
            Text blue = MakeText(group, "Blue Glitch", words, titleFont, fontSize, GlitchBlue, new Vector2(-4, 0), size);
            blue.fontStyle = titleFont.name.Contains("Italic") ? FontStyle.Normal : FontStyle.Italic;
            Text red = MakeText(group, "Red Glitch", words, titleFont, fontSize, Accent, new Vector2(tilt ? 6 : 4, 0), size);
            red.fontStyle = blue.fontStyle;
            if (tilt)
            {
                Text glow = MakeText(group, "Red Glow", words, titleFont, fontSize, new Color(Hover.r, Hover.g, Hover.b, .55f), new Vector2(3, -2), size);
                glow.fontStyle = blue.fontStyle;
            }
            Text main = MakeText(group, "Title Text", words, titleFont, fontSize, MainText, Vector2.zero, size);
            main.fontStyle = blue.fontStyle;
            return main;
        }

        Button ButtonAt(RectTransform parent, string words, UICommand command, Vector2 position, bool primary = false, Vector2? customSize = null)
        {
            Vector2 size = customSize ?? new Vector2(560, 96);
            RectTransform frame = Rect(words + " Button", parent, position, size);
            Image border = frame.gameObject.AddComponent<Image>();
            border.color = FrameColor;
            RectTransform fillRect = Rect("Fill", frame, Vector2.zero, size - new Vector2(8, 8));
            Image fill = fillRect.gameObject.AddComponent<Image>();
            fill.color = primary ? Accent : Background;
            Text label = MakeText(frame, "Label", Track(words), buttonFont, 28, MainText, Vector2.zero, size);
            label.fontStyle = buttonFont.name.Contains("Bold") ? FontStyle.Normal : FontStyle.Bold;
            Button button = frame.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            frame.gameObject.AddComponent<CanvasGroup>();
            UIStyledButton style = frame.gameObject.AddComponent<UIStyledButton>();
            style.Border = border;
            style.Fill = fill;
            style.Label = label;
            style.Primary = primary;
            style.Owner = this;
            UIAction action = frame.gameObject.AddComponent<UIAction>();
            action.Command = command;
            action.Owner = this;
            return button;
        }

        void MakeSlider(RectTransform parent, string key, Vector2 position)
        {
            RectTransform track = Rect(key + " Slider", parent, position, new Vector2(440, 44));
            Slider slider = track.gameObject.AddComponent<Slider>();
            slider.minValue = 0;
            slider.maxValue = 100;
            RectTransform bar = Rect("Track", track, Vector2.zero, new Vector2(440, 8));
            bar.gameObject.AddComponent<Image>().color = SecondaryText;
            RectTransform fillArea = Rect("Fill Area", track, Vector2.zero, new Vector2(440, 8));
            fillArea.anchorMin = new Vector2(0, .5f);
            fillArea.anchorMax = new Vector2(1, .5f);
            RectTransform fill = Rect("Fill", fillArea, Vector2.zero, Vector2.zero);
            fill.anchorMin = new Vector2(0, 0);
            fill.anchorMax = new Vector2(1, 1);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            fill.gameObject.AddComponent<Image>().color = Hover;
            RectTransform handleArea = Rect("Handle Area", track, Vector2.zero, new Vector2(440, 44));
            RectTransform handle = Rect("Handle", handleArea, Vector2.zero, new Vector2(28, 28));
            handle.gameObject.AddComponent<Image>().color = Color.white;
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.transition = Selectable.Transition.ColorTint;
            slider.colors = FocusColors(Hover);
            slider.value = PlayerPrefs.GetFloat("Sleepwalking." + key, 80f);
            slider.onValueChanged.AddListener(value =>
            {
                PlayerPrefs.SetFloat("Sleepwalking." + key, value);
                if (key == "Master") AudioListener.volume = value / 100f;
                UpdateAudioVolumes();
            });
        }

        void MakeToggle(RectTransform parent, Vector2 position)
        {
            RectTransform box = Rect("Fullscreen Toggle", parent, position, new Vector2(44, 44));
            box.gameObject.AddComponent<Image>().color = Color.white;
            RectTransform interior = Rect("Interior", box, Vector2.zero, new Vector2(36, 36));
            interior.gameObject.AddComponent<Image>().color = Background;
            RectTransform check = Rect("Check", box, Vector2.zero, new Vector2(28, 28));
            check.gameObject.AddComponent<Image>().color = Hover;
            Toggle toggle = box.gameObject.AddComponent<Toggle>();
            toggle.graphic = check.GetComponent<Image>();
            toggle.targetGraphic = box.GetComponent<Image>();
            toggle.transition = Selectable.Transition.ColorTint;
            toggle.colors = FocusColors(FrameColor);
            toggle.isOn = Screen.fullScreen;
            toggle.onValueChanged.AddListener(value => { Screen.fullScreen = value; PlayerPrefs.SetInt("Sleepwalking.Fullscreen", value ? 1 : 0); });
        }

        void MakeDropdown(RectTransform parent, Vector2 position)
        {
            RectTransform box = Rect("Resolution Dropdown", parent, position, new Vector2(240, 76));
            box.gameObject.AddComponent<Image>().color = FrameColor;
            RectTransform interior = Rect("Interior", box, Vector2.zero, new Vector2(232, 68));
            interior.gameObject.AddComponent<Image>().color = Background;
            Text label = MakeText(box, "Selection", "1920×1080  ▾", bodyFont, 23, MainText, Vector2.zero, new Vector2(220, 70));
            Button button = box.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            UIStyledButton dropdownStyle = box.gameObject.AddComponent<UIStyledButton>();
            dropdownStyle.Border = box.GetComponent<Image>();
            dropdownStyle.Fill = interior.GetComponent<Image>();
            dropdownStyle.Label = label;
            dropdownStyle.Owner = this;
            RectTransform options = Rect("Options", box, new Vector2(0, 158), new Vector2(240, 228));
            options.gameObject.AddComponent<Image>().color = FrameColor;
            string[] values = { "1920×1080", "1600×900", "1280×720" };
            for (int q = 0; q < values.Length; q++)
            {
                string value = values[q];
                RectTransform option = Rect(value, options, new Vector2(0, 76 - 76 * q), new Vector2(232, 68));
                Image optionImage = option.gameObject.AddComponent<Image>();
                optionImage.color = FrameColor;
                RectTransform optionFill = Rect("Fill", option, Vector2.zero, new Vector2(224, 60));
                Image optionBackground = optionFill.gameObject.AddComponent<Image>();
                optionBackground.color = Background;
                Text optionLabel = MakeText(option, "Label", value, bodyFont, 23, MainText, Vector2.zero, new Vector2(220, 68));
                Button choice = option.gameObject.AddComponent<Button>();
                choice.transition = Selectable.Transition.None;
                UIStyledButton optionStyle = option.gameObject.AddComponent<UIStyledButton>();
                optionStyle.Border = optionImage;
                optionStyle.Fill = optionBackground;
                optionStyle.Label = optionLabel;
                optionStyle.Owner = this;
                choice.onClick.AddListener(() =>
                {
                    label.text = value + "  ▾";
                    ApplyResolution(value);
                    options.gameObject.SetActive(false);
                });
            }
            button.onClick.AddListener(() => options.gameObject.SetActive(!options.gameObject.activeSelf));
            options.gameObject.SetActive(false);
        }

        static string Track(string value) { return string.Join("\u2009", value.ToCharArray()); }

        void ApplyResolution(string value)
        {
            string[] parts = value.Split('×');
            int width = int.Parse(parts[0]);
            int height = int.Parse(parts[1]);
            PlayerPrefs.SetInt("Sleepwalking.Width", width);
            PlayerPrefs.SetInt("Sleepwalking.Height", height);
            Screen.SetResolution(width, height, Screen.fullScreen);
        }

        static ColorBlock FocusColors(Color normal)
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = normal;
            colors.highlightedColor = Hover;
            colors.selectedColor = Hover;
            colors.pressedColor = Pressed;
            return colors;
        }

        static RectTransform Rect(string name, RectTransform parent, Vector2 position, Vector2 size)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        static Image FullRect(RectTransform parent, string name, Color color)
        {
            RectTransform rect = Rect(name, parent, Vector2.zero, Vector2.zero);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static Text MakeText(RectTransform parent, string name, string value, Font font, int size, Color color, Vector2 position, Vector2 bounds)
        {
            RectTransform rect = Rect(name, parent, position, bounds);
            Text text = rect.gameObject.AddComponent<Text>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString("#" + value, out Color color);
            return color;
        }

        void Show(GameObject screen)
        {
            menu.SetActive(screen == menu);
            pause.SetActive(screen == pause);
            settings.SetActive(screen == settings);
            credits.SetActive(screen == credits);
            confirm.SetActive(screen == confirm);
            dim.SetActive(screen != null && screen != menu);
            roomLabelFrame.gameObject.SetActive(false);
            if (EventSystem.current == null) return;
            EventSystem.current.SetSelectedGameObject(null);
            if (screen == null || screen == menu) return;
            UIStyledButton[] buttons = screen.GetComponentsInChildren<UIStyledButton>(false);
            if (buttons.Length > 0) EventSystem.current.SetSelectedGameObject(buttons[0].gameObject);
        }

        public void EnterRoom()
        {
            RestoreTime();
            inRoom = true;
            fittedAspect = -1f;
            FitRoomCamera();
            UpdateAudioVolumes();
            Show(menu);
        }

        public void EnterDream()
        {
            RestoreTime();
            inRoom = false;
            dreamStarted = true;
            UpdateAudioVolumes();
            Show(null);
        }

        public void SetDreamsCompleted(int count) { dreamStarted = count > 0; }

        public void OpenPause()
        {
            if (inRoom || paused) return;
            previousTimeScale = Time.timeScale;
            paused = true;
            Time.timeScale = 0f;
            Show(pause);
        }

        void RestoreTime()
        {
            if (!paused) return;
            Time.timeScale = previousTimeScale;
            paused = false;
        }

        public void CloseOverlay()
        {
            if (confirm.activeSelf) { Show(previous); return; }
            if (settings.activeSelf)
            {
                settings.transform.Find("Settings Panel/Resolution Dropdown/Options").gameObject.SetActive(false);
                Show(settingsPrevious);
                return;
            }
            if (credits.activeSelf) { Show(inRoom ? menu : pause); return; }
            if (pause.activeSelf) { RestoreTime(); Show(null); }
        }

        void Ask(UICommand command)
        {
            pending = command;
            previous = pause.activeSelf ? pause : settings.activeSelf ? settings : menu;
            if (command == UICommand.Restart) { confirmTitle.text = "Restart?"; confirmMessage.text = "Restart the current dream?"; }
            if (command == UICommand.MainMenu) { confirmTitle.text = "Main Menu?"; confirmMessage.text = "Return to the room?\nCurrent progress will be lost."; }
            if (command == UICommand.Quit) { confirmTitle.text = "Quit?"; confirmMessage.text = "Are you sure you want to quit?"; }
            Show(confirm);
        }

        public void Handle(UICommand command)
        {
            if (command == UICommand.Back || command == UICommand.Cancel) PlayCue(SoundCue.MenuBack);
            else PlayCue(SoundCue.MenuConfirm);
            switch (command)
            {
                case UICommand.NewGame:
                    DreamRequested?.Invoke();
                    break;
                case UICommand.Resume: CloseOverlay(); break;
                case UICommand.Restart: case UICommand.MainMenu: case UICommand.Quit: Ask(command); break;
                case UICommand.Settings:
                    settingsPrevious = pause.activeSelf ? pause : menu;
                    Show(settings);
                    break;
                case UICommand.Credits: Show(credits); break;
                case UICommand.Back: CloseOverlay(); break;
                case UICommand.Cancel: Show(previous); break;
                case UICommand.Confirm:
                    if (pending == UICommand.Restart)
                    {
                        RestoreTime();
                        Show(null);
                        RestartRequested?.Invoke();
                    }
                    else if (pending == UICommand.MainMenu)
                    {
                        EnterRoom();
                        MainMenuRequested?.Invoke();
                    }
                    else if (pending == UICommand.Quit)
                    {
                        RestoreTime();
                        Application.Quit();
#if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
#endif
                    }
                    break;
            }
        }

        public void ActivateRoomTarget(SleepwalkingRoomTarget target) => ActivateRoomTarget((Component)target);

        public void PlayHover() => PlayCue(SoundCue.MenuHover);

        void PlayCue(SoundCue cue)
        {
            SleepwalkingAudioDirector.Instance?.Play(cue);
        }

        void UpdateAudioVolumes()
        {
            SleepwalkingAudioDirector.Instance?.SetVolumes(MusicVolume, SfxVolume * .32f, SfxVolume);
        }

        public void ActivateRoomTarget(Component target)
        {
            if (!inRoom || !menu.activeSelf || !TryGetRoomAction(target, out RoomAction action, out _)) return;
            switch (action)
            {
                case RoomAction.Dream: Handle(UICommand.NewGame); break;
                case RoomAction.Settings: Handle(UICommand.Settings); break;
                case RoomAction.Credits: Handle(UICommand.Credits); break;
                case RoomAction.Quit: Handle(UICommand.Quit); break;
            }
        }

        // Визуальный модуль подключается по имени типа, чтобы UI-сцена собиралась отдельно от него.
        static bool TryGetRoomAction(Component target, out RoomAction action, out string label)
        {
            action = RoomAction.Dream;
            label = null;
            if (target == null) return false;
            if (target is SleepwalkingRoomTarget local)
            {
                action = local.Action;
                label = local.Label;
                return true;
            }
            Type type = target.GetType();
            if (type.FullName != "Sleepwalking.Visual.RoomHotspot") return false;
            object value = type.GetField("Action")?.GetValue(target) ?? type.GetProperty("Action")?.GetValue(target);
            if (value == null || !Enum.TryParse(value.ToString(), out action)) return false;
            label = (type.GetField("Label")?.GetValue(target) ?? type.GetProperty("Label")?.GetValue(target)) as string;
            return true;
        }

        static Component FindRoomTarget(Collider collider)
        {
            if (collider == null) return null;
            MonoBehaviour[] components = collider.GetComponentsInParent<MonoBehaviour>();
            foreach (MonoBehaviour component in components)
                if (TryGetRoomAction(component, out _, out _)) return component;
            return null;
        }

        void UpdateRoomTargets()
        {
            if (!inRoom || !menu.activeSelf)
            {
                hoveredTarget = null;
                roomLabelFrame.gameObject.SetActive(false);
                return;
            }
            Camera camera = roomCamera != null ? roomCamera : Camera.main;
            if (camera == null)
            {
                roomLabelFrame.gameObject.SetActive(false);
                return;
            }
            Vector2 pointer = Vector2.zero;
            bool click = false;
            bool next = false;
            bool activate = false;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                pointer = Mouse.current.position.ReadValue();
                click = Mouse.current.leftButton.wasPressedThisFrame;
            }
            if (Keyboard.current != null)
            {
                next = Keyboard.current.tabKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame;
                activate = Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame;
            }
#else
            pointer = Input.mousePosition;
            click = Input.GetMouseButtonDown(0);
            next = Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.RightArrow);
            activate = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space);
#endif
            if ((pointer - lastPointerPosition).sqrMagnitude > 4f) focusedTarget = null;
            lastPointerPosition = pointer;
            if (next)
            {
                Component oldFocus = focusedTarget;
                List<Component> targets = new List<Component>();
                foreach (Collider collider in FindObjectsByType<Collider>(FindObjectsSortMode.None))
                {
                    Component target = FindRoomTarget(collider);
                    if (target != null && !targets.Contains(target)) targets.Add(target);
                }
                targets.Sort((a, b) =>
                {
                    TryGetRoomAction(a, out RoomAction first, out _);
                    TryGetRoomAction(b, out RoomAction second, out _);
                    int result = first.CompareTo(second);
                    return result == 0 ? string.CompareOrdinal(a.name, b.name) : result;
                });
                int current = targets.IndexOf(focusedTarget);
                focusedTarget = targets.Count == 0 ? null : targets[(current + 1) % targets.Count];
                if (focusedTarget != null && focusedTarget != oldFocus) PlayHover();
            }
            // RenderTexture комнаты имеет собственный размер, отличный от окна игры.
            Vector2 cameraPoint = new Vector2(pointer.x * camera.pixelWidth / Mathf.Max(1, Screen.width), pointer.y * camera.pixelHeight / Mathf.Max(1, Screen.height));
            Ray ray = camera.ScreenPointToRay(cameraPoint);
            Component previousHover = hoveredTarget;
            hoveredTarget = Physics.Raycast(ray, out RaycastHit hit, 1000f) ? FindRoomTarget(hit.collider) : null;
            if (hoveredTarget != null && hoveredTarget != previousHover) PlayHover();
            Component selected = focusedTarget != null ? focusedTarget : hoveredTarget;
            roomLabelFrame.gameObject.SetActive(selected != null);
            if (TryGetRoomAction(selected, out RoomAction action, out string label))
            {
                roomLabel.text = action == RoomAction.Dream && dreamStarted ? "NEXT DREAM" : string.IsNullOrWhiteSpace(label) ? action.ToString().ToUpperInvariant() : label.ToUpperInvariant();
                PlaceRoomLabel(selected, focusedTarget != null, pointer, camera);
            }
            roomLabelBorder.color = focusedTarget != null ? GlitchBlue : Hover;
            roomHint.text = focusedTarget != null ? "ENTER TO CHOOSE  /  TAB FOR NEXT" : hoveredTarget != null ? "CLICK TO CHOOSE  /  TAB TO FOCUS" : "HOVER AN OBJECT  /  TAB TO FOCUS";
            roomHint.color = selected != null ? MainText : SecondaryText;
            if (click && hoveredTarget != null) ActivateRoomTarget(hoveredTarget);
            else if (activate && selected != null) ActivateRoomTarget(selected);
        }

        void PlaceRoomLabel(Component target, bool keyboardFocus, Vector2 pointer, Camera camera)
        {
            RectTransform canvasRect = (RectTransform)roomLabelFrame.parent;
            Vector2 screenPoint = pointer;
            if (keyboardFocus)
            {
                Collider collider = target.GetComponentInChildren<Collider>();
                if (collider != null)
                {
                    Vector3 cameraPoint = camera.WorldToScreenPoint(collider.bounds.center);
                    screenPoint = new Vector2(cameraPoint.x * Screen.width / Mathf.Max(1, camera.pixelWidth), cameraPoint.y * Screen.height / Mathf.Max(1, camera.pixelHeight));
                }
            }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out Vector2 local);
            if (keyboardFocus)
                local.y += local.y > 0 ? -110f : 110f;
            else
            {
                local.x += local.x > canvasRect.rect.width * .5f - 400f ? -205f : 205f;
                local.y += local.y < -canvasRect.rect.height * .5f + 160f ? 95f : -95f;
            }
            Vector2 half = roomLabelFrame.rect.size * .5f;
            Rect bounds = canvasRect.rect;
            float hintTop = canvasRect.InverseTransformPoint(roomHint.rectTransform.TransformPoint(new Vector3(0f, roomHint.rectTransform.rect.yMax, 0f))).y;
            float labelBottom = Mathf.Max(bounds.yMin + half.y + 16f, hintTop + half.y + 20f);
            roomLabelFrame.anchoredPosition = new Vector2(Mathf.Clamp(local.x, bounds.xMin + half.x + 16f, bounds.xMax - half.x - 16f), Mathf.Clamp(local.y, labelBottom, bounds.yMax - half.y - 16f));
        }

        public float MusicVolume => PlayerPrefs.GetFloat("Sleepwalking.Music", 80f) / 100f;
        public float SfxVolume => PlayerPrefs.GetFloat("Sleepwalking.SFX", 80f) / 100f;
    }

    // В изолированной UI-сцене заменяет обработчик игрового цикла для проверки навигации.
    public sealed class SleepwalkingUIShowcase : MonoBehaviour
    {
        SleepwalkingUI ui;
        SleepwalkingAudioDirector audioDirector;

        void OnEnable()
        {
            ui = GetComponent<SleepwalkingUI>();
            if (ui == null) return;
            if (ui.roomCamera != null && ui.roomCamera.GetComponent<PixelRoomView>() == null)
                ui.roomCamera.gameObject.AddComponent<PixelRoomView>();
            audioDirector = SleepwalkingAudioDirector.CreateOrGet();
            if (audioDirector != null)
            {
                audioDirector.SetVolumes(ui.MusicVolume, ui.SfxVolume * .32f, ui.SfxVolume);
                audioDirector.EnterRoom();
            }
            ui.DreamRequested += EnterDream;
            ui.RestartRequested += EnterDream;
            ui.MainMenuRequested += EnterRoom;
        }

        void EnterDream()
        {
            ui.EnterDream();
            if (audioDirector != null) audioDirector.EnterDream(Sleepwalking.Audio.DreamTheme.Meat);
        }

        void EnterRoom()
        {
            ui.EnterRoom();
            if (audioDirector != null) audioDirector.EnterRoom();
        }

        void OnDisable()
        {
            if (ui == null) return;
            ui.DreamRequested -= EnterDream;
            ui.RestartRequested -= EnterDream;
            ui.MainMenuRequested -= EnterRoom;
        }
    }
}
