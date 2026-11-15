using UnityEngine;

namespace Sleepwalking.Visual
{
    [DisallowMultipleComponent]
    public sealed class RoomBedAwakeAnimation : MonoBehaviour
    {
        public MeshRenderer Artwork;
        public Texture2D[] Frames;
        public float IntroFrameSeconds = .22f;
        public float LoopFrameSeconds = .32f;
        public int LoopStartFrame = 2;
        public int LoopEndFrame = 6;
        public int CurrentFrameIndex { get; private set; }

        static readonly int MainTex = Shader.PropertyToID("_MainTex");
        static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
        static readonly int EmissionMap = Shader.PropertyToID("_EmissionMap");

        MaterialPropertyBlock properties;
        float elapsed;
        bool intro;

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
            intro = true;
            CurrentFrameIndex = Frames != null && Frames.Length > 0 ? Frames.Length - 1 : 0;
            ApplyFrame();
        }

        public void Advance(float seconds)
        {
            if (Frames == null || Frames.Length == 0 || Artwork == null) return;
            int loopStart = Mathf.Clamp(LoopStartFrame, 0, Frames.Length - 1);
            int loopEnd = Mathf.Clamp(LoopEndFrame, loopStart, Frames.Length - 1);
            elapsed += Mathf.Max(0f, seconds);
            float frameSeconds = intro ? IntroFrameSeconds : LoopFrameSeconds;
            frameSeconds = Mathf.Max(.01f, frameSeconds);
            while (elapsed >= frameSeconds)
            {
                elapsed -= frameSeconds;
                if (intro && CurrentFrameIndex > 0)
                    CurrentFrameIndex--;
                else
                {
                    intro = false;
                    CurrentFrameIndex = CurrentFrameIndex < loopStart || CurrentFrameIndex >= loopEnd ?
                        loopStart : CurrentFrameIndex + 1;
                }
                frameSeconds = Mathf.Max(.01f, intro ? IntroFrameSeconds : LoopFrameSeconds);
            }
            ApplyFrame();
        }

        void ApplyFrame()
        {
            if (Artwork == null || Frames == null || Frames.Length == 0) return;
            Texture2D frame = Frames[Mathf.Clamp(CurrentFrameIndex, 0, Frames.Length - 1)];
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
