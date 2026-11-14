using System;
using UnityEngine;

namespace Sleepwalking.Audio
{
    public enum DreamTheme { Meat, PostApocalypse, DeepWater, Heaven }

    public enum SoundCue
    {
        Jump,
        Dash,
        WallGrab,
        Landing,
        TrapHit,
        Collision,
        Button,
        MenuHover,
        MenuConfirm,
        MenuBack
    }

    [Serializable]
    public sealed class SoundCueEntry
    {
        public SoundCue Cue;
        public AudioClip[] Variants;
        [Range(0f, 1f)] public float Gain = 0.5f;
    }

    [CreateAssetMenu(fileName = "SleepwalkingAudioCatalog", menuName = "Sleepwalking/Audio Catalog")]
    public sealed class SleepwalkingAudioCatalog : ScriptableObject
    {
        public AudioClip MenuMusic;
        public AudioClip[] DreamAmbience = new AudioClip[4];
        public SoundCueEntry[] Cues;

        public AudioClip GetAmbience(DreamTheme theme)
        {
            int index = (int)theme;
            return DreamAmbience != null && index >= 0 && index < DreamAmbience.Length ? DreamAmbience[index] : null;
        }

        public SoundCueEntry GetCue(SoundCue cue)
        {
            if (Cues == null) return null;
            foreach (SoundCueEntry entry in Cues)
                if (entry != null && entry.Cue == cue) return entry;
            return null;
        }
    }
}
