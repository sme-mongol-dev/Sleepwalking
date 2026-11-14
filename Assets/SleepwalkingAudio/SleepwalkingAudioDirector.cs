using System.Collections.Generic;
using UnityEngine;

namespace Sleepwalking.Audio
{
    public sealed class SleepwalkingAudioDirector : MonoBehaviour
    {
        enum AudioState { Stopped, Room, Dream }

        public static SleepwalkingAudioDirector Instance { get; private set; }

        [SerializeField] SleepwalkingAudioCatalog catalog;
        [SerializeField, Range(0f, 1f)] float musicVolume = 0.32f;
        [SerializeField, Range(0f, 1f)] float ambienceVolume = 0.22f;
        [SerializeField, Range(0f, 1f)] float sfxVolume = 0.7f;
        [SerializeField] bool persistBetweenScenes = true;

        readonly Dictionary<SoundCue, int> lastVariant = new Dictionary<SoundCue, int>();
        readonly List<AudioSource> effects = new List<AudioSource>();
        readonly List<float> effectGains = new List<float>();
        AudioSource music;
        AudioSource ambience;
        int nextEffect;
        AudioState state;
        DreamTheme currentTheme;

        public SleepwalkingAudioCatalog Catalog => catalog;
        public bool IsInRoom => state == AudioState.Room;
        public bool IsMenuMusicPlaying => music != null && music.isPlaying;
        public AudioClip CurrentMusic => music != null ? music.clip : null;
        public AudioClip CurrentAmbience => ambience != null ? ambience.clip : null;
        public DreamTheme CurrentTheme => currentTheme;

        public static SleepwalkingAudioDirector CreateOrGet()
        {
            if (Instance != null) return Instance;
            GameObject prefab = Resources.Load<GameObject>("SleepwalkingAudioDirector");
            if (prefab == null || prefab.GetComponent<SleepwalkingAudioDirector>() == null)
            {
                Debug.LogError("Sleepwalking audio prefab is missing from Resources.");
                return null;
            }
            return Instantiate(prefab).GetComponent<SleepwalkingAudioDirector>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (persistBetweenScenes) DontDestroyOnLoad(gameObject);
            music = MakeSource("Menu Music", true);
            ambience = MakeSource("Dream Ambience", true);
            for (int q = 0; q < 8; q++)
            {
                effects.Add(MakeSource("Effect " + (q + 1), false));
                effectGains.Add(0f);
            }
        }

        AudioSource MakeSource(string sourceName, bool loop)
        {
            GameObject child = new GameObject(sourceName);
            child.transform.SetParent(transform, false);
            AudioSource source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            return source;
        }

        // Комната — единственное место, где разрешена музыкальная дорожка.
        public void EnterRoom()
        {
            state = AudioState.Room;
            ambience.Stop();
            ambience.clip = null;
            music.Stop();
            music.clip = catalog != null ? catalog.MenuMusic : null;
            music.volume = musicVolume;
            if (music.clip != null) music.Play();
        }

        public void EnterDream(DreamTheme theme)
        {
            state = AudioState.Dream;
            currentTheme = theme;
            music.Stop();
            music.clip = null;
            ambience.Stop();
            ambience.clip = catalog != null ? catalog.GetAmbience(theme) : null;
            ambience.volume = ambienceVolume;
            if (ambience.clip != null) ambience.Play();
        }

        public void SetDreamTheme(DreamTheme theme)
        {
            currentTheme = theme;
            if (state == AudioState.Dream) EnterDream(theme);
        }

        public void StopAll()
        {
            state = AudioState.Stopped;
            music.Stop();
            music.clip = null;
            ambience.Stop();
            ambience.clip = null;
            foreach (AudioSource source in effects) source.Stop();
        }

        public int GetVariantCount(SoundCue cue)
        {
            SoundCueEntry entry = catalog != null ? catalog.GetCue(cue) : null;
            return entry != null && entry.Variants != null ? entry.Variants.Length : 0;
        }

        public bool Play(SoundCue cue)
        {
            int count = GetVariantCount(cue);
            if (count == 0) return false;
            int index = Random.Range(0, count);
            if (count > 1 && lastVariant.TryGetValue(cue, out int previous) && index == previous)
                index = (index + 1 + Random.Range(0, count - 1)) % count;
            return Play(cue, index);
        }

        public bool Play(SoundCue cue, int variant)
        {
            SoundCueEntry entry = catalog != null ? catalog.GetCue(cue) : null;
            if (entry == null || entry.Variants == null || variant < 0 || variant >= entry.Variants.Length) return false;
            AudioClip clip = entry.Variants[variant];
            if (clip == null) return false;

            int sourceIndex = nextEffect;
            AudioSource source = effects[sourceIndex];
            nextEffect = (nextEffect + 1) % effects.Count;
            source.Stop();
            source.clip = clip;
            effectGains[sourceIndex] = Mathf.Clamp01(entry.Gain);
            source.volume = effectGains[sourceIndex] * sfxVolume;
            source.Play();
            lastVariant[cue] = variant;
            return true;
        }

        public void SetVolumes(float musicValue, float ambienceValue, float sfxValue)
        {
            musicVolume = Mathf.Clamp01(musicValue);
            ambienceVolume = Mathf.Clamp01(ambienceValue);
            sfxVolume = Mathf.Clamp01(sfxValue);
            if (music != null) music.volume = musicVolume;
            if (ambience != null) ambience.volume = ambienceVolume;
            for (int q = 0; q < effects.Count; q++)
                effects[q].volume = effectGains[q] * sfxVolume;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
