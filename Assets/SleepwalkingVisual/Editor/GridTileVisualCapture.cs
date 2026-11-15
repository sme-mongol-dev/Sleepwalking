using System;
using System.IO;
using System.Reflection;
using System.Text;
using Sleepwalking.Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sleepwalking.Visual.Editor
{
    public static class GridTileVisualCapture
    {
        const int Width = 960;
        const int Height = 540;

        [MenuItem("Sleepwalking/Visual/Capture Grid Themes")]
        public static void CaptureGridThemes()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string output = Path.Combine(root, "Docs", "QA", "VisualLogs");
            Directory.CreateDirectory(output);
            string[] themes = { "Meat", "PostApocalypse", "DeepWater", "Heaven" };
            foreach (string name in themes)
                Capture(4, name, 6.5f, -1f, output);
            Capture(5, "Meat", 13f, 6f, output);
            Capture(6, "Meat", 19f, 4f, output);
            Capture(7, "Meat", 19f, 3f, output);
            CaptureTimedState(output);
        }

        [MenuItem("Sleepwalking/Visual/Capture Dynamic Grid")]
        public static void CaptureDynamicGrid()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string output = Path.Combine(root, "Docs", "QA", "VisualLogs");
            Directory.CreateDirectory(output);
            Capture(5, "Meat", 13f, 6f, output, "-After");
            Capture(6, "Meat", 19f, 4f, output, "-After");
            CaptureTimedState(output);
        }

        [MenuItem("Sleepwalking/Visual/Capture Grid Matrix")]
        public static void CaptureGridMatrix()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string output = Path.Combine(root, "Docs", "QA", "VisualLogs", "GridMatrix");
            Directory.CreateDirectory(output);
            string[] themes = { "Meat", "PostApocalypse", "DeepWater", "Heaven" };
            for (int index = 1; index <= 7; index++)
            foreach (string name in themes)
                CaptureMatrixTheme(index, name, output);
            WriteContactSheet(output, themes, "16x9");
            WriteContactSheet(output, themes, "4x3");
            Debug.Log("Captured 56 grid matrix frames: " + output);
        }

        [MenuItem("Sleepwalking/Visual/Capture L05 Cue Review")]
        public static void CaptureL05CueReview()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string matrix = Path.Combine(root, "Docs", "QA", "VisualLogs", "GridMatrix");
            string output = Path.Combine(root, "Docs", "QA", "VisualLogs");
            Directory.CreateDirectory(matrix);
            CaptureMatrixTheme(5, "Heaven", matrix);
            CaptureMatrixTheme(5, "DeepWater", matrix);
            CaptureMatrixTheme(6, "Heaven", matrix);
            CaptureMatrixTheme(6, "DeepWater", matrix);
            CaptureTimedState(output);
            Debug.Log("Captured L05/L06 Heaven/DeepWater cue review and timed state.");
        }

        static void WriteContactSheet(string output, string[] themes, string aspect)
        {
            StringBuilder page = new StringBuilder();
            page.Append("<!doctype html><html lang=\"ru\"><meta charset=\"utf-8\">");
            page.Append("<title>Sleepwalking grid matrix ").Append(aspect).Append("</title>");
            page.Append("<style>body{margin:24px;background:#17151c;color:#eee;font:16px sans-serif}");
            page.Append("h1{font-size:24px}main{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:16px}");
            page.Append("figure{margin:0;background:#292631;padding:8px}img{display:block;width:100%;height:auto;image-rendering:pixelated}");
            page.Append("figcaption{padding:8px 0 2px}a{color:inherit;text-decoration:none}</style>");
            page.Append("<h1>Sleepwalking: 7 уровней × 4 темы, ").Append(aspect).Append("</h1><main>");
            for (int index = 1; index <= 7; index++)
            foreach (string theme in themes)
            {
                string name = "L" + index.ToString("00") + "-" + theme + "-" + aspect + ".png";
                page.Append("<figure><a href=\"").Append(name).Append("\"><img loading=\"lazy\" src=\"")
                    .Append(name).Append("\" alt=\"").Append(name).Append("\"></a><figcaption>")
                    .Append("L").Append(index.ToString("00")).Append(" · ").Append(theme)
                    .Append("</figcaption></figure>");
            }
            page.Append("</main></html>");
            File.WriteAllText(Path.Combine(output, "contact-sheet-" + aspect + ".html"), page.ToString());
        }

        static void CaptureMatrixTheme(int index, string name, string output)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject levelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/SleepwalkingLevels/Prefabs/L" + index.ToString("00") + "_DreamGeometry.prefab");
            GameObject themePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/SleepwalkingVisual/Prefabs/Themes/" + name + ".prefab");
            if (levelPrefab == null || themePrefab == null)
                throw new InvalidOperationException("Grid matrix input is missing: L" + index + " " + name);

            GameObject level = PrefabUtility.InstantiatePrefab(levelPrefab, scene) as GameObject;
            GameObject theme = PrefabUtility.InstantiatePrefab(themePrefab, scene) as GameObject;
            if (level == null || theme == null)
                throw new InvalidOperationException("Could not instantiate grid matrix scene.");
            DreamTheme style = theme.GetComponent<DreamTheme>();
            Transform geometry = level.transform.Find("Geometry");
            SleepwalkingLevelDefinition definition = level.GetComponent<SleepwalkingLevelDefinition>();
            if (style == null || geometry == null || definition == null ||
                definition.PlayerStart == null || definition.BedExit == null)
                throw new InvalidOperationException("Grid matrix components are missing: L" + index);
            style.ApplyTo(geometry);
            Transform tiles = level.transform.Find("Dream Tile Matrix");
            MeshFilter matrix = tiles != null ? tiles.GetComponent<MeshFilter>() : null;
            if (matrix == null || matrix.sharedMesh == null || matrix.sharedMesh.vertexCount == 0)
                throw new InvalidOperationException("Unified tile matrix was not built: L" + index + " " + name);

            Vector2 minimum = definition.PlayerStart.position;
            Vector2 maximum = minimum;
            Include(ref minimum, ref maximum, definition.BedExit.position);
            foreach (SleepwalkingTileSurface surface in level.GetComponentsInChildren<SleepwalkingTileSurface>(true))
            {
                if (surface.IsShell || surface.Solid == null) continue;
                Include(ref minimum, ref maximum, surface.Solid);
                SleepwalkingLevelFeature feature = surface.GetComponent<SleepwalkingLevelFeature>();
                if (feature == null || feature.Kind != LevelFeatureKind.MovingPlatform) continue;
                Vector3 movement = surface.transform.parent.TransformVector(feature.MoveOffset);
                Bounds bounds = surface.Solid.bounds;
                Include(ref minimum, ref maximum, bounds.min + movement);
                Include(ref minimum, ref maximum, bounds.max + movement);
            }
            foreach (SleepwalkingLevelFeature feature in level.GetComponentsInChildren<SleepwalkingLevelFeature>(true))
            {
                Collider2D shape = feature.GetComponent<Collider2D>();
                if (shape != null && feature.GetComponent<SleepwalkingTileSurface>() == null)
                    Include(ref minimum, ref maximum, shape.bounds);
            }

            GameObject cameraObject = new GameObject("Grid matrix camera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(22, 19, 24, 255);
            Vector2 center = (minimum + maximum) * .5f;
            camera.transform.position = new Vector3(center.x, center.y, -100f);
            CaptureMatrixAspect(camera, style, definition, minimum, maximum, 1280, 720,
                Path.Combine(output, "L" + index.ToString("00") + "-" + name + "-16x9.png"));
            CaptureMatrixAspect(camera, style, definition, minimum, maximum, 1024, 768,
                Path.Combine(output, "L" + index.ToString("00") + "-" + name + "-4x3.png"));
            Debug.Log("Captured grid matrix L" + index.ToString("00") + " " + name +
                ": " + matrix.sharedMesh.vertexCount / 4 + " static cells");
        }

        static void CaptureMatrixAspect(Camera camera, DreamTheme style, SleepwalkingLevelDefinition definition,
            Vector2 minimum, Vector2 maximum, int width, int height, string path)
        {
            camera.aspect = (float)width / height;
            Vector2 half = (maximum - minimum) * .5f + Vector2.one * 40f;
            camera.orthographicSize = Mathf.Max(half.y, half.x / camera.aspect);
            style.FrameBackdrop(camera);
            CheckInFrame(camera, definition.PlayerStart.position, "PlayerStart", path);
            CheckInFrame(camera, definition.BedExit.position, "BedExit", path);
            SaveFrame(camera, path, width, height);
        }

        static void CheckInFrame(Camera camera, Vector3 position, string marker, string path)
        {
            Vector3 viewport = camera.WorldToViewportPoint(position);
            if (viewport.x < .03f || viewport.x > .97f || viewport.y < .03f || viewport.y > .97f)
                throw new InvalidOperationException(marker + " is outside the matrix frame: " + path);
        }

        static void Include(ref Vector2 minimum, ref Vector2 maximum, Collider2D shape)
        {
            Include(ref minimum, ref maximum, shape.bounds);
        }

        static void Include(ref Vector2 minimum, ref Vector2 maximum, Bounds bounds)
        {
            Include(ref minimum, ref maximum, bounds.min);
            Include(ref minimum, ref maximum, bounds.max);
        }

        static void Include(ref Vector2 minimum, ref Vector2 maximum, Vector3 point)
        {
            minimum = Vector2.Min(minimum, point);
            maximum = Vector2.Max(maximum, point);
        }

        static void Capture(int index, string name, float x, float y, string output, string suffix = "")
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject levelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/SleepwalkingLevels/Prefabs/L" + index.ToString("00") + "_DreamGeometry.prefab");
            GameObject themePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/SleepwalkingVisual/Prefabs/Themes/" + name + ".prefab");
            if (levelPrefab == null || themePrefab == null)
                throw new InvalidOperationException("Grid capture input is missing: " + name);

            GameObject level = PrefabUtility.InstantiatePrefab(levelPrefab, scene) as GameObject;
            GameObject theme = PrefabUtility.InstantiatePrefab(themePrefab, scene) as GameObject;
            if (level == null || theme == null) throw new InvalidOperationException("Could not instantiate grid capture.");
            DreamTheme style = theme.GetComponent<DreamTheme>();
            Transform geometry = level.transform.Find("Geometry");
            if (style == null || geometry == null) throw new InvalidOperationException("Grid capture components are missing.");
            style.ApplyTo(geometry);
            Transform tiles = level.transform.Find("Dream Tile Matrix");
            if (tiles == null || tiles.GetComponent<MeshFilter>().sharedMesh.vertexCount == 0)
                throw new InvalidOperationException("Unified tile matrix was not built: " + name);

            GameObject cameraObject = new GameObject("Grid capture camera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 96f;
            camera.aspect = (float)Width / Height;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(22, 19, 24, 255);
            camera.transform.position = new Vector3(x * 32f, y * 32f, -100f);
            style.FrameBackdrop(camera);

            string path = Path.Combine(output, "GridL" + index.ToString("00") + "-" + name + suffix + ".png");
            SaveFrame(camera, path);
            Debug.Log("Captured unified grid: " + path + " (" + tiles.GetComponent<MeshFilter>().sharedMesh.vertexCount / 4 + " cells)");
        }

        static void CaptureTimedState(string output)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject levelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/SleepwalkingLevels/Prefabs/L05_DreamGeometry.prefab");
            GameObject themePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/SleepwalkingVisual/Prefabs/Themes/Meat.prefab");
            GameObject level = PrefabUtility.InstantiatePrefab(levelPrefab, scene) as GameObject;
            GameObject theme = PrefabUtility.InstantiatePrefab(themePrefab, scene) as GameObject;
            if (level == null || theme == null) throw new InvalidOperationException("Timed capture input is missing.");
            DreamTheme style = theme.GetComponent<DreamTheme>();
            style.ApplyTo(level.transform.Find("Geometry"));

            SleepwalkingLevelFeature timed = null;
            foreach (SleepwalkingLevelFeature feature in level.GetComponentsInChildren<SleepwalkingLevelFeature>(true))
                if (feature.FeatureId == "L05_TimedA") timed = feature;
            if (timed == null) throw new InvalidOperationException("L05_TimedA is missing.");
            Collider2D shape = timed.GetComponent<Collider2D>();
            MeshRenderer backing = timed.GetComponent<MeshRenderer>();
            MeshRenderer tiles = timed.transform.Find("Dream Tiles")?.GetComponent<MeshRenderer>();
            SleepwalkingLevelVisualState visualState = timed.GetComponent<SleepwalkingLevelVisualState>();
            if (shape == null || backing == null || tiles == null || visualState == null)
                throw new InvalidOperationException("Timed platform visual components are missing.");
            MethodInfo awake = typeof(SleepwalkingLevelVisualState).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo update = typeof(SleepwalkingLevelVisualState).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo themeUpdate = typeof(DreamTheme).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            if (awake == null || update == null || themeUpdate == null)
                throw new InvalidOperationException("Timed platform visual state methods are missing.");

            GameObject cameraObject = new GameObject("Timed capture camera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 64f;
            camera.aspect = (float)Width / Height;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(22, 19, 24, 255);
            camera.transform.position = new Vector3(5f * 32f, 2f * 32f, -100f);
            style.FrameBackdrop(camera);

            awake.Invoke(visualState, null);
            if (backing.enabled || !tiles.enabled) throw new InvalidOperationException("Timed platform initial art state is wrong.");
            CheckTimedCuesHidden(timed);
            SaveFrame(camera, Path.Combine(output, "GridL05-TimedA-on.png"));
            shape.enabled = false;
            update.Invoke(visualState, null);
            themeUpdate.Invoke(style, null);
            if (backing.enabled || tiles.enabled) throw new InvalidOperationException("Timed platform disabled art state is wrong.");
            CheckTimedCuesHidden(timed);
            SaveFrame(camera, Path.Combine(output, "GridL05-TimedA-off.png"));
            shape.enabled = true;
            update.Invoke(visualState, null);
            themeUpdate.Invoke(style, null);
            if (backing.enabled || !tiles.enabled) throw new InvalidOperationException("Timed platform restored art state is wrong.");
            CheckTimedCuesHidden(timed);
            SaveFrame(camera, Path.Combine(output, "GridL05-TimedA-restored.png"));
            Debug.Log("Captured L05 timed platform on/off/restored with backing hidden.");
        }

        static void CheckTimedCuesHidden(SleepwalkingLevelFeature timed)
        {
            foreach (Renderer renderer in timed.GetComponentsInChildren<Renderer>(true))
                if (renderer.name.StartsWith("Timed_Pulse_") && renderer.enabled)
                    throw new InvalidOperationException("Old timed cue is visible: " + renderer.name);
        }

        static void SaveFrame(Camera camera, string path)
        {
            SaveFrame(camera, path, Width, Height);
        }

        static void SaveFrame(Camera camera, string path, int width, int height)
        {
            RenderTexture target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            target.filterMode = FilterMode.Point;
            target.Create();
            RenderTexture previous = RenderTexture.active;
            Texture2D image = null;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image = new Texture2D(width, height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                if (image != null) UnityEngine.Object.DestroyImmediate(image);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }
}
