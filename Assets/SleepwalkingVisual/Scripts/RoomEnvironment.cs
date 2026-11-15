using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sleepwalking.Visual
{
    [Serializable]
    public sealed class RoomOdradekDecoration
    {
        public string Name;
        [Range(1, 100)] public int MinOdradek = 1;
        public GameObject RoomObject;
        public GameObject Prefab;
        public Transform Parent;
        public Vector3 LocalPosition;
        public Vector3 LocalEulerAngles;
        public Vector3 LocalScale = Vector3.one;
        public int SortingOrder;

        [NonSerialized] public GameObject Instance;
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

        void Awake()
        {
            if (Poster != null && Posters != null && Posters.Length > 0)
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
                    decoration.MinOdradek = Mathf.Clamp(decoration.MinOdradek, 1, 100);
        }

        void PrepareDecorations()
        {
            if (Decorations == null) return;
            foreach (RoomOdradekDecoration decoration in Decorations)
            {
                if (decoration == null) continue;
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
                GameObject instance = decoration?.Instance;
                if (instance == null) continue;
                bool interactive = instance.GetComponentInChildren<RoomHotspot>(true) != null;
                instance.SetActive(interactive || CurrentOdradek >= Mathf.Clamp(decoration.MinOdradek, 1, 100));
            }
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
