using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Sleepwalking.Visual
{
    public enum RoomDecorationMode { Visibility, Variant }

    [Serializable]
    public sealed class RoomOdradekDecoration
    {
        public string Name;
        public string Id;
        public string Group;
        [Range(1, 100)] public int MinOdradek = 1;
        [Range(1, 100)] public int MaxOdradek = 100;
        public string Asset;
        public Texture2D Texture;
        public RoomDecorationMode Mode;
        public GameObject RoomObject;
        public GameObject Prefab;
        public Transform Parent;
        public Vector3 LocalPosition;
        public Vector3 LocalEulerAngles;
        public Vector3 LocalScale = Vector3.one;
        public int SortingOrder;

        [NonSerialized] public GameObject Instance;
    }

    [Serializable]
    public sealed class RoomDecorJsonDocument
    {
        public int version;
        public RoomDecorJsonRow[] decorations;
    }

    [Serializable]
    public sealed class RoomDecorJsonRow
    {
        public string id;
        public string group;
        public int min;
        public int max;
        public string asset;
    }

    public sealed class RoomEnvironment : MonoBehaviour
    {
        public Transform ClockHand;
        public Transform Picture;
        public Transform LooseBook;
        public Transform Curtain;
        public GameObject ExtraStar;
        public MeshRenderer Poster;
        public Texture2D[] Posters;
        public List<RoomOdradekDecoration> Decorations = new List<RoomOdradekDecoration>();
        public int CurrentOdradek { get; private set; } = -1;

        Quaternion clockStart;
        Quaternion pictureStart;
        Quaternion bookStart;
        Vector3 curtainStart;
        bool captured;
        bool decorationsPrepared;
        readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        readonly Dictionary<string, string> assetGroups = new Dictionary<string, string>();

        void Awake()
        {
            bool posterVariants = false;
            foreach (RoomOdradekDecoration row in Decorations)
                if (row != null && row.Mode == RoomDecorationMode.Variant && row.Group == "poster")
                    posterVariants = true;
            if (!posterVariants && Poster != null && Posters != null && Posters.Length > 0)
            {
                var properties = new MaterialPropertyBlock();
                Poster.GetPropertyBlock(properties);
                Texture2D poster = Posters[UnityEngine.Random.Range(0, Posters.Length)];
                properties.SetTexture("_MainTex", poster);
                properties.SetTexture("_BaseMap", poster);
                properties.SetTexture("_EmissionMap", poster);
                Poster.SetPropertyBlock(properties);
            }
            PrepareDecorations();
            decorationsPrepared = true;
            ReloadDecorationJson();
            SetOdradek(SleepwalkingGame.Instance != null ? SleepwalkingGame.Instance.Odradek : 1);
            CaptureInitialState();
            SetWakeCount(0);
        }

        void OnEnable()
        {
            if (decorationsPrepared)
                SetOdradek(SleepwalkingGame.Instance != null ? SleepwalkingGame.Instance.Odradek : 1);
        }

        void Update()
        {
            int odradek = SleepwalkingGame.Instance != null ? SleepwalkingGame.Instance.Odradek : 1;
            if (odradek != CurrentOdradek) SetOdradek(odradek);
        }

        void OnValidate()
        {
            if (Decorations == null) return;
            foreach (RoomOdradekDecoration decoration in Decorations)
                if (decoration != null)
                {
                    decoration.MinOdradek = Mathf.Clamp(decoration.MinOdradek, 1, 100);
                    decoration.MaxOdradek = Mathf.Clamp(decoration.MaxOdradek, decoration.MinOdradek, 100);
                }
        }

        void PrepareDecorations()
        {
            if (Decorations == null) return;
            foreach (RoomOdradekDecoration decoration in Decorations)
            {
                if (decoration == null) continue;
                if (!string.IsNullOrEmpty(decoration.Asset) && decoration.Texture != null)
                {
                    textures[decoration.Asset] = decoration.Texture;
                    assetGroups[decoration.Asset] = decoration.Group;
                }
                GameObject instance = decoration.RoomObject;
                if (instance == null && decoration.Prefab != null)
                {
                    Transform parent = decoration.Parent != null ? decoration.Parent : transform;
                    instance = Instantiate(decoration.Prefab, parent);
                    instance.name = string.IsNullOrEmpty(decoration.Name) ? decoration.Prefab.name : decoration.Name;
                }
                if (instance == null) continue;

                decoration.Instance = instance;
                Transform item = instance.transform;
                item.localPosition = decoration.LocalPosition;
                item.localEulerAngles = decoration.LocalEulerAngles;
                item.localScale = decoration.LocalScale;
                foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                    renderer.sortingOrder = decoration.SortingOrder;
            }
        }

        public void SetOdradek(int value)
        {
            CurrentOdradek = Mathf.Clamp(value, 1, 100);
            if (Decorations == null) return;
            foreach (RoomOdradekDecoration decoration in Decorations)
            {
                if (decoration == null || decoration.Mode == RoomDecorationMode.Variant) continue;
                GameObject instance = decoration?.Instance;
                if (instance == null) continue;
                bool interactive = instance.GetComponentInChildren<RoomHotspot>(true) != null;
                bool visible = CurrentOdradek >= decoration.MinOdradek && CurrentOdradek <= decoration.MaxOdradek;
                instance.SetActive(interactive || visible);
                if (visible) ApplyTexture(instance, decoration.Asset);
            }

            var groups = new HashSet<string>();
            foreach (RoomOdradekDecoration row in Decorations)
                if (row != null && row.Mode == RoomDecorationMode.Variant)
                    groups.Add(row.Group);
            foreach (string group in groups)
            {
                GameObject target = null;
                RoomOdradekDecoration selected = null;
                foreach (RoomOdradekDecoration row in Decorations)
                {
                    if (row == null || row.Mode != RoomDecorationMode.Variant || row.Group != group) continue;
                    if (target == null) target = row.Instance;
                    if (CurrentOdradek >= row.MinOdradek && CurrentOdradek <= row.MaxOdradek)
                        selected = row;
                }
                if (target == null) continue;
                bool interactive = target.GetComponentInChildren<RoomHotspot>(true) != null;
                target.SetActive(interactive || selected != null);
                if (selected != null)
                {
                    Transform item = target.transform;
                    item.localPosition = selected.LocalPosition;
                    item.localEulerAngles = selected.LocalEulerAngles;
                    item.localScale = selected.LocalScale;
                    foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true))
                        renderer.sortingOrder = selected.SortingOrder;
                    ApplyTexture(target, selected.Asset);
                }
            }
        }

        void ApplyTexture(GameObject target, string asset)
        {
            if (string.IsNullOrEmpty(asset) || !textures.TryGetValue(asset, out Texture2D texture)) return;
            MeshRenderer renderer = target.GetComponentInChildren<MeshRenderer>(true);
            if (renderer == null) return;
            var properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            properties.SetTexture("_MainTex", texture);
            properties.SetTexture("_BaseMap", texture);
            properties.SetTexture("_EmissionMap", texture);
            renderer.SetPropertyBlock(properties);
        }

        public bool ReloadDecorationJson()
        {
            if (Decorations == null || Decorations.Count == 0) return false;
            string external = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "SleepwalkingMenuDecor.json"));
            string bundled = Path.Combine(Application.streamingAssetsPath, "SleepwalkingMenuDecor.json");
            string path = File.Exists(external) ? external : bundled;
            if (!File.Exists(path)) return false;
            try
            {
                if (TryApplyJson(File.ReadAllText(path), out string error)) return true;
                Debug.LogError("Menu decoration JSON rejected: " + error + " (" + path + ")");
            }
            catch (Exception exception)
            {
                Debug.LogError("Menu decoration JSON could not be read: " + exception.Message);
            }
            return false;
        }

        public bool TryApplyJson(string json, out string error)
        {
            error = null;
            RoomDecorJsonDocument document;
            try { document = JsonUtility.FromJson<RoomDecorJsonDocument>(json); }
            catch (Exception exception) { error = exception.Message; return false; }
            if (document == null || document.version != 1 || document.decorations == null)
            { error = "Expected version 1 and a decorations array."; return false; }

            var byId = new Dictionary<string, RoomOdradekDecoration>();
            foreach (RoomOdradekDecoration row in Decorations)
                if (row != null && !string.IsNullOrEmpty(row.Id))
                    byId[row.Id] = row;
            var overrides = new Dictionary<string, RoomDecorJsonRow>();
            foreach (RoomDecorJsonRow item in document.decorations)
            {
                if (item == null || string.IsNullOrEmpty(item.id) || !byId.TryGetValue(item.id, out RoomOdradekDecoration row))
                { error = "Unknown decoration id."; return false; }
                if (overrides.ContainsKey(item.id)) { error = "Duplicate id: " + item.id; return false; }
                if (item.group != row.Group) { error = "Group mismatch: " + item.id; return false; }
                if (item.min < 1 || item.max > 100 || item.min > item.max)
                { error = "Invalid range: " + item.id; return false; }
                if (string.IsNullOrEmpty(item.asset) || !textures.ContainsKey(item.asset) ||
                    assetGroups[item.asset] != row.Group)
                { error = "Unknown asset: " + item.id; return false; }
                overrides.Add(item.id, item);
            }

            foreach (string group in new[] { "poster", "calendar", "bed" })
            {
                for (int value = 1; value <= 100; value++)
                {
                    int count = 0;
                    foreach (RoomOdradekDecoration row in Decorations)
                    {
                        if (row == null || row.Mode != RoomDecorationMode.Variant || row.Group != group) continue;
                        RoomDecorJsonRow selected = overrides.TryGetValue(row.Id, out RoomDecorJsonRow entry) ? entry : null;
                        int minimum = selected != null ? selected.min : row.MinOdradek;
                        int maximum = selected != null ? selected.max : row.MaxOdradek;
                        if (value >= minimum && value <= maximum) count++;
                    }
                    bool required = group != "calendar";
                    if (count > 1 || (required && count != 1))
                    { error = "Overlapping or missing " + group + " variant at Odradek " + value; return false; }
                }
            }

            foreach (var item in overrides)
            {
                RoomOdradekDecoration row = byId[item.Key];
                row.MinOdradek = item.Value.min;
                row.MaxOdradek = item.Value.max;
                row.Asset = item.Value.asset;
            }
            SetOdradek(CurrentOdradek > 0 ? CurrentOdradek :
                SleepwalkingGame.Instance != null ? SleepwalkingGame.Instance.Odradek : 1);
            return true;
        }

        void CaptureInitialState()
        {
            if (captured) return;
            if (ClockHand != null) clockStart = ClockHand.localRotation;
            if (Picture != null) pictureStart = Picture.localRotation;
            if (LooseBook != null) bookStart = LooseBook.localRotation;
            if (Curtain != null) curtainStart = Curtain.localPosition;
            captured = true;
        }

        public void SetWakeCount(int completedDreams)
        {
            CaptureInitialState();
            int phase = Mathf.Max(0, completedDreams) % 4;
            if (ClockHand != null) ClockHand.localRotation = clockStart * Quaternion.Euler(0, 0, phase * 35f);
            if (Picture != null) Picture.localRotation = pictureStart * Quaternion.Euler(0, 0, phase == 0 ? 0 : -3f - phase * 2f);
            if (LooseBook != null) LooseBook.localRotation = bookStart * Quaternion.Euler(0, 0, phase == 0 ? 0 : phase * 8f);
            if (Curtain != null) Curtain.localPosition = curtainStart + Vector3.right * (phase * .09f);
            if (ExtraStar != null) ExtraStar.SetActive(phase >= 2);
            foreach (RoomHotspot hotspot in GetComponentsInChildren<RoomHotspot>(true))
                if (hotspot.Action == RoomAction.Dream)
                    hotspot.Label = completedDreams > 0 ? "Next Dream" : "New Game";
        }
    }
}
