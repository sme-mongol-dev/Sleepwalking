using System;
using UnityEditor;
using UnityEngine;

namespace Sleepwalking.Audio.Editor
{
    public static class SleepwalkingAudioBuilder
    {
        const string Root = "Assets/SleepwalkingAudio";
        const string CatalogPath = Root + "/SleepwalkingAudioCatalog.asset";
        const string PrefabPath = Root + "/Resources/SleepwalkingAudioDirector.prefab";

        [MenuItem("Sleepwalking/Audio/Rebuild catalog and prefab")]
        public static void Rebuild()
        {
            AssetDatabase.Refresh();
            SleepwalkingAudioCatalog catalog = AssetDatabase.LoadAssetAtPath<SleepwalkingAudioCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<SleepwalkingAudioCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.MenuMusic = Clip("Music/menu_horror.ogg");
            catalog.DreamAmbience = new[]
            {
                Clip("Ambience/meat_horror.ogg"),
                Clip("Ambience/postapoc_generator.wav"),
                Clip("Ambience/sfx100v2_loop_water_01.ogg"),
                Clip("Ambience/heaven_wind.ogg")
            };
            catalog.Cues = new[]
            {
                Entry(SoundCue.Jump, 0.38f, "Actions/sfx100v2_air_01.ogg", "Actions/sfx100v2_air_02.ogg"),
                Entry(SoundCue.Dash, 0.48f, "Actions/sfx100v2_air_03.ogg", "Actions/sfx100v2_air_02.ogg"),
                Entry(SoundCue.WallGrab, 0.45f, "Actions/sfx100v2_stones_01.ogg", "Actions/sfx100v2_stones_02.ogg"),
                Entry(SoundCue.Landing, 0.43f, "Actions/sfx100v2_footstep_01.ogg", "Actions/sfx100v2_footstep_02.ogg", "Actions/sfx100v2_footstep_wet_01.ogg", "Actions/sfx100v2_footstep_wet_02.ogg"),
                Entry(SoundCue.TrapHit, 0.58f, "Actions/sfx100v2_hit_01.ogg", "Actions/sfx100v2_hit_02.ogg", "Actions/sfx100v2_hit_03.ogg"),
                Entry(SoundCue.Collision, 0.45f, "Actions/sfx100v2_metal_hit_01.ogg", "Actions/sfx100v2_metal_hit_02.ogg", "Actions/sfx100v2_wood_hit_01.ogg", "Actions/sfx100v2_wood_hit_02.ogg"),
                Entry(SoundCue.Button, 0.45f, "Menu/sfx100v2_switch_01.ogg", "Menu/sfx100v2_switch_02.ogg"),
                Entry(SoundCue.MenuHover, 0.22f, "Menu/sfx100v2_items_01.ogg", "Menu/sfx100v2_items_02.ogg"),
                Entry(SoundCue.MenuConfirm, 0.38f, "Menu/sfx100v2_switch_01.ogg", "Menu/sfx100v2_switch_02.ogg"),
                Entry(SoundCue.MenuBack, 0.3f, "Menu/sfx100v2_items_02.ogg", "Menu/sfx100v2_items_01.ogg")
            };
            EditorUtility.SetDirty(catalog);

            if (!AssetDatabase.IsValidFolder(Root + "/Resources"))
                AssetDatabase.CreateFolder(Root, "Resources");
            GameObject root = new GameObject("Sleepwalking Audio Director");
            SleepwalkingAudioDirector director = root.AddComponent<SleepwalkingAudioDirector>();
            SerializedObject serialized = new SerializedObject(director);
            serialized.FindProperty("catalog").objectReferenceValue = catalog;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Validate();
        }

        [MenuItem("Sleepwalking/Audio/Validate catalog")]
        public static void Validate()
        {
            SleepwalkingAudioCatalog catalog = AssetDatabase.LoadAssetAtPath<SleepwalkingAudioCatalog>(CatalogPath);
            GameObject prefabObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            SleepwalkingAudioDirector prefab = prefabObject != null ? prefabObject.GetComponent<SleepwalkingAudioDirector>() : null;
            if (catalog == null || prefab == null || prefab.Catalog != catalog)
                throw new Exception("Audio catalog or prefab reference is missing.");
            if (Resources.Load<GameObject>("SleepwalkingAudioDirector") != prefabObject)
                throw new Exception("Audio prefab cannot be loaded through Resources.");
            if (catalog.MenuMusic == null || catalog.MenuMusic.length <= 0f)
                throw new Exception("Menu music is missing or empty.");
            foreach (DreamTheme theme in Enum.GetValues(typeof(DreamTheme)))
            {
                AudioClip clip = catalog.GetAmbience(theme);
                if (clip == null || clip.length <= 0f) throw new Exception("Missing ambience: " + theme);
            }
            foreach (SoundCue cue in Enum.GetValues(typeof(SoundCue)))
            {
                SoundCueEntry entry = catalog.GetCue(cue);
                if (entry == null || entry.Variants == null || entry.Variants.Length < 2)
                    throw new Exception("Fewer than two variants: " + cue);
                foreach (AudioClip clip in entry.Variants)
                    if (clip == null || clip.length <= 0f) throw new Exception("Invalid audio clip: " + cue);
            }

            Debug.Log("Sleepwalking audio: catalog, prefab, four ambiences, and cue variants validated.");
        }

        static AudioClip Clip(string relative)
        {
            string path = Root + "/Clips/" + relative;
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) throw new Exception("Missing audio clip: " + path);
            return clip;
        }

        static SoundCueEntry Entry(SoundCue cue, float gain, params string[] files)
        {
            SoundCueEntry entry = new SoundCueEntry { Cue = cue, Gain = gain, Variants = new AudioClip[files.Length] };
            for (int q = 0; q < files.Length; q++) entry.Variants[q] = Clip(files[q]);
            return entry;
        }
    }
}
