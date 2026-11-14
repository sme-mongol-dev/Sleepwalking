using System;
using Sleepwalking.Levels;
using UnityEngine;

namespace Sleepwalking.Visual
{
    public sealed class TileShowcasePreview : MonoBehaviour
    {
        public GameObject[] Levels;
        public GameObject[] Themes;
        public Camera PreviewCamera;

        void Start()
        {
            int levelIndex = Mathf.Clamp(ReadInt("-tileLevel=", 1), 1, Levels.Length);
            int styleIndex = Mathf.Clamp(ReadInt("-tileStyle=", 0), 0, Themes.Length - 1);
            GameObject level = Instantiate(Levels[levelIndex - 1]);
            GameObject theme = Instantiate(Themes[styleIndex], level.transform);
            Transform geometry = level.transform.Find("Geometry");
            DreamTheme appearance = theme.GetComponent<DreamTheme>();
            if (geometry != null && appearance != null) appearance.ApplyTo(geometry);
            if (ReadInt("-disableTimed=", 0) == 1)
            {
                foreach (SleepwalkingLevelFeature feature in level.GetComponentsInChildren<SleepwalkingLevelFeature>(true))
                    if (feature.Kind == LevelFeatureKind.TimedPlatform && feature.GetComponent<Collider2D>() != null)
                        feature.GetComponent<Collider2D>().enabled = false;
            }
            float x = ReadFloat("-tileX=", levelIndex == 1 ? 3f : levelIndex == 5 ? 5.7f : levelIndex == 6 ? 6.8f : 20.5f);
            PreviewCamera.transform.position = new Vector3(x, 1.8f, -20f);
        }

        static int ReadInt(string prefix, int fallback)
        {
            foreach (string value in Environment.GetCommandLineArgs())
                if (value.StartsWith(prefix, StringComparison.Ordinal) && int.TryParse(value.Substring(prefix.Length), out int result))
                    return result;
            return fallback;
        }

        static float ReadFloat(string prefix, float fallback)
        {
            foreach (string value in Environment.GetCommandLineArgs())
                if (value.StartsWith(prefix, StringComparison.Ordinal) &&
                    float.TryParse(value.Substring(prefix.Length), System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out float result))
                    return result;
            return fallback;
        }
    }
}
