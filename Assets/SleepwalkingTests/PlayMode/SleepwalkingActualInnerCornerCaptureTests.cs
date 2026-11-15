using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Sleepwalking.Tests.PlayMode
{
    public sealed class SleepwalkingActualInnerCornerCaptureTests
    {
        [UnityTest]
        public IEnumerator CaptureOneActualInnerCornerPerLevelAndTheme()
        {
            return CaptureRange(0, 7, 0, 4);
        }

        [UnityTest]
        public IEnumerator CaptureL02MeatPit()
        {
            return CaptureRange(1, 2, 0, 1);
        }

        static IEnumerator CaptureRange(int firstLevel, int lastLevel, int firstTheme, int lastTheme)
        {
            AsyncOperation loading = SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
            Assert.That(loading, Is.Not.Null);
            while (!loading.isDone) yield return null;
            yield return null;

            GameObject gameObject = GameObject.Find("Sleepwalking Game");
            Assert.That(gameObject, Is.Not.Null);
            MonoBehaviour game = null;
            foreach (MonoBehaviour component in gameObject.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().FullName == "Sleepwalking.SleepwalkingGame")
                    game = component;
            Assert.That(game, Is.Not.Null);
            Type gameType = game.GetType();
            GameObject[] levels = (GameObject[])gameType.GetField("LevelPrefabs").GetValue(game);
            GameObject[] themes = (GameObject[])gameType.GetField("ThemePrefabs").GetValue(game);
            Assert.That(levels.Length, Is.EqualTo(7));
            Assert.That(themes.Length, Is.EqualTo(4));

            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects()) root.SetActive(false);
            string output = Path.GetFullPath(Path.Combine(Application.dataPath,
                "..", "Docs", "QA", "VisualLogs", "InnerCorners", "ActualLevels"));
            Directory.CreateDirectory(output);
            Vector2Int[] targets =
            {
                new Vector2Int(13, 0), new Vector2Int(3, -2), new Vector2Int(-1, 5),
                new Vector2Int(13, 5), new Vector2Int(16, -5), new Vector2Int(2, -5),
                new Vector2Int(2, 8)
            };
            string[] corners = { "NW", "NE", "SE", "SW", "NW", "NE", "SE" };
            int[] expected = { 8, 8, 8, 4, 10, 8, 9 };

            GameObject cameraObject = new GameObject("Actual inner corner camera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 54f;
            camera.aspect = 1f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(22, 19, 24, 255);
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 200f;

            for (int q = firstLevel; q < lastLevel; q++)
            {
                Assert.That(levels[q], Is.Not.Null);
                for (int e = firstTheme; e < lastTheme; e++)
                {
                    Assert.That(themes[e], Is.Not.Null);
                    GameObject level = UnityEngine.Object.Instantiate(levels[q]);
                    GameObject themeObject = UnityEngine.Object.Instantiate(themes[e], level.transform);
                    MonoBehaviour theme = null;
                    foreach (MonoBehaviour component in themeObject.GetComponents<MonoBehaviour>())
                        if (component != null && component.GetType().FullName == "Sleepwalking.Visual.DreamTheme")
                            theme = component;
                    Assert.That(theme, Is.Not.Null);
                    Transform geometry = level.transform.Find("Geometry");
                    Assert.That(geometry, Is.Not.Null);
                    MethodInfo apply = theme.GetType().GetMethod("ApplyTo", BindingFlags.Public | BindingFlags.Instance);
                    Assert.That(apply, Is.Not.Null);
                    apply.Invoke(theme, new object[] { geometry });

                    Transform matrix = level.transform.Find("Dream Tile Matrix");
                    Assert.That(matrix, Is.Not.Null);
                    Mesh mesh = matrix.GetComponent<MeshFilter>().sharedMesh;
                    Assert.That(mesh, Is.Not.Null);
                    int total = CountTile17(mesh);
                    Assert.That(total, Is.EqualTo(expected[q]), "L" + (q + 1) + " tile 17 count");
                    AssertTile17At(matrix, mesh, targets[q]);

                    Vector2Int target = targets[q];
                    camera.transform.position = new Vector3((target.x + .5f) * 32f,
                        (target.y + .5f) * 32f, -100f);
                    string file = "L" + (q + 1).ToString("00") + "-" + themes[e].name.Replace("(Clone)", "") +
                        "-missing-" + corners[q] + ".png";
                    SaveFrame(camera, Path.Combine(output, file));
                    if (q == 1 && e == 0)
                    {
                        camera.transform.position = new Vector3(4.5f * 32f, -.7f * 32f, -100f);
                        camera.orthographicSize = 72f;
                        SaveFrame(camera, Path.Combine(output, "L02-Meat-pit-wide.png"));
                        camera.orthographicSize = 54f;
                    }
                    UnityEngine.Object.DestroyImmediate(level);
                }
            }
            UnityEngine.Object.DestroyImmediate(cameraObject);
            LogAssert.NoUnexpectedReceived();
        }

        static int CountTile17(Mesh mesh)
        {
            Vector2[] uv = mesh.uv;
            int total = 0;
            for (int q = 0; q < uv.Length; q += 4)
                if (IsTile17(uv[q]) && IsTile17(uv[q + 1]) &&
                    IsTile17(uv[q + 2]) && IsTile17(uv[q + 3])) total++;
            return total;
        }

        static void AssertTile17At(Transform matrix, Mesh mesh, Vector2Int cell)
        {
            Vector3[] vertices = mesh.vertices;
            Vector2[] uv = mesh.uv;
            for (int q = 0; q < vertices.Length; q += 4)
            {
                Vector3 world = matrix.TransformPoint(vertices[q]);
                if (Mathf.Abs(world.x - cell.x * 32f) > .01f ||
                    Mathf.Abs(world.y - cell.y * 32f) > .01f) continue;
                for (int e = 0; e < 4; e++)
                    Assert.That(IsTile17(uv[q + e]), Is.True,
                        "Cell " + cell + " does not use tile 17");
                return;
            }
            Assert.Fail("Cell " + cell + " is missing from the level matrix.");
        }

        static bool IsTile17(Vector2 point)
        {
            return point.x > 32f / 96f && point.x < 64f / 96f &&
                point.y > 0f && point.y < 32f / 192f;
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
    }
}
