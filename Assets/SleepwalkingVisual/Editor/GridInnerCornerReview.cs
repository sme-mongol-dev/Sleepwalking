using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Sleepwalking.Visual.Editor
{
    public static class GridInnerCornerReview
    {
        const string Output = "Docs/QA/VisualLogs/InnerCorners";

        [MenuItem("Sleepwalking/Visual/Capture Inner Corners")]
        public static void Capture()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string output = Path.Combine(root, Output);
            Directory.CreateDirectory(output);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            MethodInfo buildMesh = typeof(DreamTheme).GetMethod("GridMesh", BindingFlags.Static | BindingFlags.NonPublic);
            if (buildMesh == null) throw new InvalidOperationException("DreamTheme.GridMesh is missing.");
            GameObject geometry = new GameObject("Inner corner review", typeof(MeshFilter), typeof(MeshRenderer));
            MeshFilter filter = geometry.GetComponent<MeshFilter>();
            MeshRenderer renderer = geometry.GetComponent<MeshRenderer>();
            GameObject cameraObject = new GameObject("Inner corner camera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 54f;
            camera.aspect = 1f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(22, 19, 24, 255);
            camera.transform.position = new Vector3(48f, 48f, -100f);

            string[] themes = { "Meat", "PostApocalypse", "DeepWater", "Heaven" };
            string[] corners = { "NW", "NE", "SW", "SE" };
            Vector2Int[] missing = { new Vector2Int(0, 2), new Vector2Int(2, 2),
                new Vector2Int(0, 0), new Vector2Int(2, 0) };
            string[] pairs = { "SW-SE", "NW-SW", "NW-NE", "NE-SE" };
            Vector2Int[][] missingPairs =
            {
                new[] { new Vector2Int(0, 0), new Vector2Int(2, 0) },
                new[] { new Vector2Int(0, 2), new Vector2Int(0, 0) },
                new[] { new Vector2Int(0, 2), new Vector2Int(2, 2) },
                new[] { new Vector2Int(2, 2), new Vector2Int(2, 0) }
            };
            StringBuilder report = new StringBuilder();
            for (int themeIndex = 0; themeIndex < themes.Length; themeIndex++)
            {
                string prefabPath = "Assets/SleepwalkingVisual/Prefabs/Themes/" + themes[themeIndex] + ".prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                DreamTheme theme = prefab != null ? prefab.GetComponent<DreamTheme>() : null;
                if (theme == null || theme.Tiles == null)
                    throw new InvalidOperationException("Theme tiles are missing: " + prefabPath);
                renderer.sharedMaterial = theme.Tiles;
                HashSet<string> orientations = new HashSet<string>();

                for (int q = 0; q < corners.Length; q++)
                {
                    HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
                    for (int y = 0; y < 3; y++)
                    for (int x = 0; x < 3; x++)
                    {
                        Vector2Int cell = new Vector2Int(x, y);
                        if (cell != missing[q]) cells.Add(cell);
                    }
                    Mesh mesh = buildMesh.Invoke(null, new object[] { geometry.transform, cells }) as Mesh;
                    if (mesh == null) throw new InvalidOperationException("GridMesh did not return a mesh.");
                    filter.sharedMesh = mesh;
                    string uv = CenterTileUv(mesh, 17);
                    orientations.Add(uv);
                    report.Append(themes[themeIndex]).Append(' ').Append(corners[q]).Append(' ')
                        .Append(uv).AppendLine();
                    SaveFrame(camera, Path.Combine(output,
                        "tile17-" + themes[themeIndex] + "-missing-" + corners[q] + ".png"));
                    filter.sharedMesh = null;
                    UnityEngine.Object.DestroyImmediate(mesh);
                }
                if (orientations.Count != 4)
                    throw new InvalidOperationException("Inner corner rotations are not unique: " + themes[themeIndex]);

                orientations.Clear();
                for (int q = 0; q < pairs.Length; q++)
                {
                    HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
                    for (int y = 0; y < 3; y++)
                    for (int x = 0; x < 3; x++)
                    {
                        Vector2Int cell = new Vector2Int(x, y);
                        if (cell != missingPairs[q][0] && cell != missingPairs[q][1]) cells.Add(cell);
                    }
                    Mesh mesh = buildMesh.Invoke(null, new object[] { geometry.transform, cells }) as Mesh;
                    if (mesh == null) throw new InvalidOperationException("GridMesh did not return a mesh.");
                    filter.sharedMesh = mesh;
                    string uv = CenterTileUv(mesh, 18);
                    orientations.Add(uv);
                    report.Append(themes[themeIndex]).Append(' ').Append(pairs[q]).Append(' ')
                        .Append(uv).AppendLine();
                    SaveFrame(camera, Path.Combine(output,
                        "tile18-" + themes[themeIndex] + "-missing-" + pairs[q] + ".png"));
                    filter.sharedMesh = null;
                    UnityEngine.Object.DestroyImmediate(mesh);
                }
                if (orientations.Count != 4)
                    throw new InvalidOperationException("Double inner corner rotations are not unique: " + themes[themeIndex]);
            }
            File.WriteAllText(Path.Combine(output, "uv-review.txt"), report.ToString());
            WriteContactSheet(output, themes, corners, 17);
            WriteContactSheet(output, themes, pairs, 18);
            Debug.Log("Captured 32 inner corner cases with four distinct UV rotations per tile and theme: " + output);
        }

        static string CenterTileUv(Mesh mesh, int tile)
        {
            Vector3[] vertices = mesh.vertices;
            Vector2[] uv = mesh.uv;
            for (int q = 0; q < vertices.Length; q += 4)
            {
                if (Mathf.Abs(vertices[q].x - 32f) > .01f ||
                    Mathf.Abs(vertices[q].y - 32f) > .01f) continue;
                StringBuilder signature = new StringBuilder();
                for (int e = 0; e < 4; e++)
                {
                    Vector2 point = uv[q + e];
                    float u0 = (tile == 17 ? 32f : 64f) / 96f;
                    float u1 = (tile == 17 ? 64f : 96f) / 96f;
                    if (point.x < u0 - .001f || point.x > u1 + .001f ||
                        point.y < -.001f || point.y > 32f / 192f + .001f)
                        throw new InvalidOperationException("Center cell does not use tile " + tile + ": " + point);
                    signature.Append(Mathf.RoundToInt(point.x * 9600f)).Append(',')
                        .Append(Mathf.RoundToInt(point.y * 19200f)).Append(';');
                }
                return signature.ToString();
            }
            throw new InvalidOperationException("Center cell is missing from the inner corner mesh.");
        }

        static void SaveFrame(Camera camera, string path)
        {
            RenderTexture target = new RenderTexture(512, 512, 24);
            target.Create();
            RenderTexture previous = RenderTexture.active;
            Texture2D image = new Texture2D(512, 512, TextureFormat.RGBA32, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
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

        static void WriteContactSheet(string output, string[] themes, string[] corners, int tile)
        {
            StringBuilder page = new StringBuilder();
            page.Append("<!doctype html><html lang=\"ru\"><meta charset=\"utf-8\">");
            page.Append("<title>Sleepwalking tile ").Append(tile).Append(" inner corners</title>");
            page.Append("<style>body{margin:24px;background:#17151c;color:#eee;font:16px sans-serif}");
            page.Append("main{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:16px}");
            page.Append("figure{margin:0;background:#292631;padding:8px}img{display:block;width:100%;image-rendering:pixelated}");
            page.Append("figcaption{padding:8px 0 2px}a{color:inherit;text-decoration:none}</style>");
            page.Append("<h1>Тайл ").Append(tile).Append(": 4 темы × 4 положения</h1><main>");
            foreach (string theme in themes)
            foreach (string corner in corners)
            {
                string name = "tile" + tile + "-" + theme + "-missing-" + corner + ".png";
                page.Append("<figure><a href=\"").Append(name).Append("\"><img src=\"")
                    .Append(name).Append("\" alt=\"").Append(name).Append("\"></a><figcaption>")
                    .Append(theme).Append(" · ").Append(corner).Append(" пустой")
                    .Append("</figcaption></figure>");
            }
            page.Append("</main></html>");
            File.WriteAllText(Path.Combine(output, tile == 17 ? "contact-sheet.html" :
                "contact-sheet-tile18.html"), page.ToString());
        }
    }
}
