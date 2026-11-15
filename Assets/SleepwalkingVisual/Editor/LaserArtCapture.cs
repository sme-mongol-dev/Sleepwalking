using System;
using System.IO;
using Sleepwalking.Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sleepwalking.Visual.Editor
{
    public static class LaserArtCapture
    {
        const int Width = 1280;
        const int Height = 720;

        [MenuItem("Sleepwalking/Visual/Capture Four Lasers")]
        public static void Capture()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string output = Path.Combine(root, "Docs", "QA", "VisualLogs", "Lasers");
            Directory.CreateDirectory(output);
            string[] themes = { "Meat", "DeepWater", "Heaven", "PostApocalypse" };
            foreach (string name in themes)
            {
                CaptureTheme(4, "L04_Laser", "LaserCeiling", name, output, "");
                CaptureTheme(4, "L04_SecondLaser", "SecondLaserCeiling", name, output, "-second");
                CaptureTheme(7, "L07_Laser", "LaserCeiling", name, output, "");
                CaptureTheme(7, "L07_SecondLaser", "SecondLaserCeiling", name, output, "-second");
            }
            Debug.Log("Captured 16 laser cases across four themes and four level sections: " + output);
        }

        static void CaptureTheme(int levelIndex, string featureId, string ceilingName,
            string name, string output, string suffix)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject levelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/SleepwalkingLevels/Prefabs/L" + levelIndex.ToString("00") + "_DreamGeometry.prefab");
            GameObject themePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/SleepwalkingVisual/Prefabs/Themes/" + name + ".prefab");
            if (levelPrefab == null || themePrefab == null)
                throw new InvalidOperationException("Laser capture input missing: " + name);

            GameObject level = PrefabUtility.InstantiatePrefab(levelPrefab, scene) as GameObject;
            GameObject themeObject = PrefabUtility.InstantiatePrefab(themePrefab, scene) as GameObject;
            DreamTheme theme = themeObject != null ? themeObject.GetComponent<DreamTheme>() : null;
            if (level == null || theme == null)
                throw new InvalidOperationException("Could not instantiate laser capture: " + name);

            Transform geometry = level.transform.Find("Geometry");
            if (geometry == null) throw new InvalidOperationException("Laser geometry missing: L" + levelIndex);
            theme.ApplyTo(geometry);

            SleepwalkingLevelFeature laserBase = null;
            SleepwalkingLevelFeature laserBeam = null;
            foreach (SleepwalkingLevelFeature feature in level.GetComponentsInChildren<SleepwalkingLevelFeature>(true))
            {
                if (feature.FeatureId != featureId) continue;
                if (feature.Kind == LevelFeatureKind.LaserBase) laserBase = feature;
                if (feature.Kind == LevelFeatureKind.LaserBeam) laserBeam = feature;
            }
            if (laserBase == null || laserBeam == null)
                throw new InvalidOperationException("Laser features missing: " + featureId);
            SpriteRenderer baseArt = laserBase.transform.Find("Dream Hazard Art")?.GetComponent<SpriteRenderer>();
            SpriteRenderer beamArt = laserBeam.transform.Find("Dream Hazard Art")?.GetComponent<SpriteRenderer>();
            if (baseArt == null || beamArt == null)
                throw new InvalidOperationException("Laser art missing: " + featureId + " " + name);
            string basePath = AssetDatabase.GetAssetPath(baseArt.sprite);
            string beamPath = AssetDatabase.GetAssetPath(beamArt.sprite);
            if (!basePath.EndsWith("/T_Lasers.png", StringComparison.Ordinal) ||
                !beamPath.EndsWith("_environment.png", StringComparison.Ordinal) ||
                !beamArt.sprite.name.EndsWith("_LaserBeam", StringComparison.Ordinal))
                throw new InvalidOperationException("Wrong laser sprites: " + name + " " + basePath + " " + beamPath);

            BoxCollider2D beamShape = laserBeam.GetComponent<BoxCollider2D>();
            PolygonCollider2D baseShape = laserBase.GetComponent<PolygonCollider2D>();
            if (beamShape == null || baseShape == null || baseShape.enabled ||
                Mathf.Abs(beamShape.bounds.min.y - baseShape.transform.TransformPoint(baseShape.points[2]).y) > .01f)
                throw new InvalidOperationException("Laser base and beam do not meet: " + featureId + " " + name);
            Transform ceiling = level.transform.Find("Geometry/" + ceilingName);
            Collider2D ceilingShape = ceiling != null ? ceiling.GetComponent<Collider2D>() : null;
            if (ceilingShape == null || Mathf.Abs(beamShape.bounds.max.y - ceilingShape.bounds.min.y) > .01f)
                throw new InvalidOperationException("Laser beam does not meet its ceiling: " + featureId + " " + name);

            GameObject cameraObject = new GameObject("Laser art camera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(80f,
                (ceilingShape.bounds.max.y - baseShape.transform.TransformPoint(baseShape.points[0]).y) * .5f + 24f);
            camera.aspect = (float)Width / Height;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(22, 19, 24, 255);
            camera.transform.position = new Vector3(laserBase.transform.position.x,
                (baseShape.transform.TransformPoint(baseShape.points[0]).y + ceilingShape.bounds.max.y) * .5f, -100f);

            float width = laserBeam.BeamPixelWidth * camera.orthographicSize * 2f / Height;
            Vector3 scale = laserBeam.transform.localScale;
            scale.x = width / Mathf.Abs(laserBeam.transform.parent.lossyScale.x);
            laserBeam.transform.localScale = scale;
            Physics2D.SyncTransforms();
            float projectedPixels = beamShape.bounds.size.x * Height / (camera.orthographicSize * 2f);
            if (projectedPixels < 3f || projectedPixels > 4f)
                throw new InvalidOperationException("Laser hitbox outside 3-4 pixels: " + projectedPixels);

            theme.FrameBackdrop(camera);
            string path = Path.Combine(output, "L" + levelIndex.ToString("00") + "-" + name + suffix + "-laser.png");
            SaveFrame(camera, path);
            Debug.Log("Captured " + featureId + " " + name + " laser, hitbox " + projectedPixels.ToString("F2") +
                " pixels, base " + baseArt.sprite.name + ", beam " + beamArt.sprite.name);
        }

        static void SaveFrame(Camera camera, string path)
        {
            RenderTexture target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            target.filterMode = FilterMode.Point;
            target.Create();
            RenderTexture previous = RenderTexture.active;
            Texture2D image = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(image);
            }
        }
    }
}
