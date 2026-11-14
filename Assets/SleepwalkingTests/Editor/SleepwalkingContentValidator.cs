using System;
using System.Collections.Generic;
using Sleepwalking.Levels;
using UnityEditor;
using UnityEngine;

namespace Sleepwalking.Tests.Editor
{
    public static class SleepwalkingContentValidator
    {
        private const string PrefabRoot = "Assets/SleepwalkingLevels/Prefabs";

        [MenuItem("Sleepwalking/QA/Validate Seven Level Prefabs")]
        public static void ValidateFromMenu()
        {
            List<string> errors = ValidateSevenLevelPrefabs();
            foreach (string error in errors)
                Debug.LogError("Sleepwalking QA: " + error);

            if (errors.Count == 0)
                Debug.Log("Sleepwalking QA: seven level prefab contracts passed. Gameplay and presentation remain untested.");
        }

        // Вызывается через Unity -batchmode -executeMethod; ошибка должна завершить проверку неуспешно.
        public static void ValidateFromCommandLine()
        {
            List<string> errors = ValidateSevenLevelPrefabs();
            foreach (string error in errors)
                Debug.LogError("Sleepwalking QA: " + error);

            if (errors.Count > 0)
                throw new InvalidOperationException("Sleepwalking QA: " + errors.Count + " level prefab contract errors.");

            Debug.Log("Sleepwalking QA: seven level prefab contracts passed. Gameplay and presentation remain untested.");
        }

        public static List<string> ValidateSevenLevelPrefabs()
        {
            List<string> errors = new List<string>();
            HashSet<int> indices = new HashSet<int>();
            LevelFeatureKind[][] requiredForLevel =
            {
                new[] { LevelFeatureKind.Exit },
                new[] { LevelFeatureKind.Spikes },
                new[] { LevelFeatureKind.Saw },
                new[] { LevelFeatureKind.LaserBase, LevelFeatureKind.LaserBeam, LevelFeatureKind.Button },
                new[] { LevelFeatureKind.TimedPlatform },
                new[] { LevelFeatureKind.MovingPlatform },
                new[] { LevelFeatureKind.Spikes, LevelFeatureKind.Saw, LevelFeatureKind.LaserBase,
                    LevelFeatureKind.LaserBeam, LevelFeatureKind.Button }
            };

            string[] prefabIds = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot });
            int geometryCount = 0;
            foreach (string id in prefabIds)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(id);
                if (assetPath.EndsWith("_DreamGeometry.prefab", StringComparison.Ordinal)) geometryCount++;
            }
            if (geometryCount != 7)
                errors.Add("Found " + geometryCount + " dream geometry prefabs; expected exactly 7");

            for (int q = 1; q <= 7; q++)
            {
                string path = PrefabRoot + "/L" + q.ToString("00") + "_DreamGeometry.prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    errors.Add(path + ": prefab missing");
                    continue;
                }

                SleepwalkingLevelDefinition definition = prefab.GetComponent<SleepwalkingLevelDefinition>();
                if (definition == null)
                {
                    errors.Add(path + ": SleepwalkingLevelDefinition missing");
                    continue;
                }

                if (definition.LevelIndex != q || !indices.Add(definition.LevelIndex))
                    errors.Add(path + ": level index is not the unique expected index " + q);
                if (definition.PlayerStart == null || !definition.PlayerStart.IsChildOf(prefab.transform))
                    errors.Add(path + ": PlayerStart is missing or outside prefab");
                if (definition.BedExit == null || !definition.BedExit.IsChildOf(prefab.transform))
                    errors.Add(path + ": BedExit is missing or outside prefab");

                Transform geometry = prefab.transform.Find("Geometry");
                if (geometry == null || geometry.childCount == 0)
                    errors.Add(path + ": platform geometry is missing");
                else
                    foreach (Transform platform in geometry)
                        ValidatePlatform(path, platform, errors);

                SleepwalkingLevelFeature[] features = prefab.GetComponentsInChildren<SleepwalkingLevelFeature>(true);
                bool hasExit = false;
                foreach (SleepwalkingLevelFeature feature in features)
                {
                    Collider2D collider = feature.GetComponent<Collider2D>();
                    if (feature.Kind == LevelFeatureKind.Exit)
                    {
                        hasExit = true;
                        if (collider == null || !collider.isTrigger)
                            errors.Add(path + ": exit has no 2D trigger");
                    }
                    if (IsHazard(feature.Kind) && (collider == null || !collider.isTrigger))
                        errors.Add(path + ": hazard " + feature.name + " has no 2D trigger");
                    if (feature.Kind == LevelFeatureKind.MovingPlatform || feature.Kind == LevelFeatureKind.TimedPlatform)
                        ValidatePlatform(path, feature.transform, errors);
                    if (feature.Kind == LevelFeatureKind.Button)
                        ValidateButton(path, feature, features, collider, errors);
                    if (feature.Kind == LevelFeatureKind.LaserBase || feature.Kind == LevelFeatureKind.LaserBeam)
                        ValidateLaserPair(path, feature, features, errors);
                }

                if (!hasExit) errors.Add(path + ": Exit feature missing");
                foreach (LevelFeatureKind required in requiredForLevel[q - 1])
                {
                    bool found = false;
                    foreach (SleepwalkingLevelFeature feature in features)
                        if (feature.Kind == required) { found = true; break; }
                    if (!found) errors.Add(path + ": required feature " + required + " missing");
                }
            }

            if (indices.Count != 7)
                errors.Add("Only " + indices.Count + " unique level indices found; expected 7");
            return errors;
        }

        private static void ValidatePlatform(string path, Transform platform, List<string> errors)
        {
            SleepwalkingTileSurface surface = platform.GetComponent<SleepwalkingTileSurface>();
            BoxCollider2D shape = surface != null ? surface.Solid : null;
            if (shape == null || !shape.enabled || shape.isTrigger || shape.size.x <= 0 || shape.size.y <= 0)
                errors.Add(path + ": platform " + platform.name + " needs an enabled solid BoxCollider2D and a tile surface");
        }

        private static void ValidateButton(string path, SleepwalkingLevelFeature button,
            SleepwalkingLevelFeature[] features, Collider2D collider, List<string> errors)
        {
            if (collider == null || !collider.isTrigger)
                errors.Add(path + ": button " + button.name + " has no 2D trigger");
            if (button.ActiveSeconds <= 0f)
                errors.Add(path + ": button " + button.name + " has no positive duration");
            if (string.IsNullOrEmpty(button.LinkedFeatureId))
            {
                errors.Add(path + ": button " + button.name + " has no target ID");
                return;
            }

            bool matchedBase = false;
            bool matchedBeam = false;
            bool matchedOtherHazard = false;
            foreach (SleepwalkingLevelFeature feature in features)
            {
                if (!IsHazard(feature.Kind)) continue;
                if (feature.FeatureId == button.LinkedFeatureId ||
                    feature.FeatureId.StartsWith(button.LinkedFeatureId + "_", StringComparison.Ordinal))
                {
                    if (feature.Kind == LevelFeatureKind.LaserBase) matchedBase = true;
                    if (feature.Kind == LevelFeatureKind.LaserBeam) matchedBeam = true;
                    if (feature.Kind != LevelFeatureKind.LaserBase && feature.Kind != LevelFeatureKind.LaserBeam)
                        matchedOtherHazard = true;
                }
            }
            if (matchedBase && matchedBeam) return;
            if (matchedBase || matchedBeam)
            {
                errors.Add(path + ": button " + button.name + " does not target both laser base and beam");
                return;
            }
            if (matchedOtherHazard) return;
            errors.Add(path + ": button " + button.name + " targets no hazard: " + button.LinkedFeatureId);
        }

        private static void ValidateLaserPair(string path, SleepwalkingLevelFeature laser,
            SleepwalkingLevelFeature[] features, List<string> errors)
        {
            LevelFeatureKind partner = laser.Kind == LevelFeatureKind.LaserBase ?
                LevelFeatureKind.LaserBeam : LevelFeatureKind.LaserBase;
            foreach (SleepwalkingLevelFeature feature in features)
                if (feature.Kind == partner && feature.FeatureId == laser.FeatureId)
                    return;

            errors.Add(path + ": laser " + laser.name + " has no " + partner + " with ID " + laser.FeatureId);
        }

        private static bool IsHazard(LevelFeatureKind kind)
        {
            return kind == LevelFeatureKind.Spikes || kind == LevelFeatureKind.Saw ||
                kind == LevelFeatureKind.LaserBase || kind == LevelFeatureKind.LaserBeam;
        }
    }
}
