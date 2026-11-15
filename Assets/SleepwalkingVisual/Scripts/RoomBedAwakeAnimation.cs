using UnityEngine;

namespace Sleepwalking.Visual
{
    [DisallowMultipleComponent]
    public sealed class RoomBedAwakeAnimation : MonoBehaviour
    {
        public MeshRenderer Artwork;
        public Texture2D[] Frames;
        public Texture2D[] SleepingFrames;
        public float AwakeFrameSeconds = .22f;
        public float SleepingFrameSeconds = .1f;
        public int CurrentFrameIndex { get; private set; }
        public bool IsSleeping { get; private set; }

        static readonly int MainTex = Shader.PropertyToID("_MainTex");
        static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
        static readonly int EmissionMap = Shader.PropertyToID("_EmissionMap");

        MaterialPropertyBlock properties;
        float elapsed;
        int direction;

        void OnEnable()
        {
            RestartAnimation();
        }

        void LateUpdate()
        {
            Advance(Time.unscaledDeltaTime);
        }

        public void RestartAnimation()
        {
            elapsed = 0f;
            IsSleeping = SleepwalkingGame.Instance == null || !SleepwalkingGame.Instance.HasEnteredDream;
            CurrentFrameIndex = 0;
            direction = 1;
            ApplyFrame();
        }

        public void Advance(float seconds)
        {
            Texture2D[] sequence = IsSleeping ? SleepingFrames : Frames;
            if (sequence == null || sequence.Length == 0 || Artwork == null) return;
            elapsed += Mathf.Max(0f, seconds);
            float frameSeconds = Mathf.Max(.01f, IsSleeping ? SleepingFrameSeconds : AwakeFrameSeconds);
            while (elapsed >= frameSeconds)
            {
                elapsed -= frameSeconds;
                if (IsSleeping)
                    CurrentFrameIndex = (CurrentFrameIndex + 1) % sequence.Length;
                else
                {
                    // Финальный кадр закрывает глаза; цикл доходит только до предпоследнего.
                    int lastOpenFrame = Mathf.Max(0, sequence.Length - 2);
                    if (CurrentFrameIndex >= lastOpenFrame) direction = -1;
                    else if (CurrentFrameIndex <= 0) direction = 1;
                    CurrentFrameIndex = Mathf.Clamp(CurrentFrameIndex + direction, 0, lastOpenFrame);
                }
            }
            ApplyFrame();
        }

        void ApplyFrame()
        {
            Texture2D[] sequence = IsSleeping ? SleepingFrames : Frames;
            if (Artwork == null || sequence == null || sequence.Length == 0) return;
            Texture2D frame = sequence[Mathf.Clamp(CurrentFrameIndex, 0, sequence.Length - 1)];
            if (frame == null) return;
            properties ??= new MaterialPropertyBlock();
            Artwork.GetPropertyBlock(properties);
            properties.SetTexture(MainTex, frame);
            properties.SetTexture(BaseMap, frame);
            properties.SetTexture(EmissionMap, frame);
            Artwork.SetPropertyBlock(properties);
        }
    }
}
