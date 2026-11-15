using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Sleepwalking.Editor
{
    public static class TutorialSignCapture
    {
        const string LevelRoot = "Assets/SleepwalkingLevels/Prefabs";

        [MenuItem("Sleepwalking/UI/Capture Tutorial Signs")]
        public static void Capture()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "../Docs/QA"));
            CaptureLevel(1, 18f);
            CaptureLevel(2, 20f);
            Debug.Log("Captured tutorial signs at 16:9 and 4:3.");
        }

        public static void CaptureFromCommandLine() { Capture(); }

        public static void RebuildAndCaptureFromCommandLine()
        {
            TutorialSignBuilder.BuildPrefabs();
            Capture();
        }

        static void CaptureLevel(int number, float endInBlocks)
        {
            string levelPath = LevelRoot + "/Tutorial" + number.ToString("00") + "_DreamGeometry.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(levelPath);
            if (prefab == null) throw new FileNotFoundException("Missing tutorial level.", levelPath);
            GameObject level = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                SleepwalkingTutorialSign[] signs = level.GetComponentsInChildren<SleepwalkingTutorialSign>(true);
                if (signs.Length != (number == 1 ? 4 : 1))
                    throw new InvalidOperationException("Unexpected tutorial sign count in " + levelPath);
                foreach (SleepwalkingTutorialSign sign in signs)
                {
                    if ((sign.Kind == TutorialSignKind.Dash) != (number == 2))
                        throw new InvalidOperationException("Shift sign is in the wrong tutorial: " + levelPath);
                    if (sign.GetComponentInChildren<Collider>() != null || sign.GetComponentInChildren<Collider2D>() != null)
                        throw new InvalidOperationException("Tutorial sign has a collider: " + sign.name);
                }

                GameObject cameraObject = new GameObject("Tutorial sign QA camera", typeof(Camera));
                try
                {
                    Camera camera = cameraObject.GetComponent<Camera>();
                    camera.orthographic = true;
                    camera.clearFlags = CameraClearFlags.SolidColor;
                    camera.backgroundColor = SleepwalkingUI.Background;
                    camera.nearClipPlane = .1f;
                    camera.farClipPlane = 100f;
                    camera.allowHDR = false;
                    camera.allowMSAA = false;
                    camera.transform.position = new Vector3(endInBlocks * 16f, 3.5f * 32f, -25f);
                    camera.transform.rotation = Quaternion.identity;
                    CaptureAspect(camera, number, endInBlocks, 320, 180, "16x9");
                    CaptureAspect(camera, number, endInBlocks, 320, 240, "4x3");
                }
                finally { UnityEngine.Object.DestroyImmediate(cameraObject); }
            }
            finally { UnityEngine.Object.DestroyImmediate(level); }
        }

        static void CaptureAspect(Camera camera, int number, float endInBlocks, int width, int height, string aspect)
        {
            float ratio = (float)width / height;
            camera.orthographicSize = Mathf.Max(6f * 32f, (endInBlocks + 1f) * 32f / (2f * ratio));
            RenderTexture target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            target.filterMode = FilterMode.Point;
            target.Create();
            camera.targetTexture = target;
            RenderTexture previous = RenderTexture.active;
            Texture2D small = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Texture2D full = new Texture2D(width * 4, height * 4, TextureFormat.RGBA32, false);
            try
            {
                camera.Render();
                RenderTexture.active = target;
                small.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                small.Apply();
                Color32[] source = small.GetPixels32();
                Color32[] enlarged = new Color32[width * height * 16];
                for (int y = 0; y < height * 4; y++)
                    for (int x = 0; x < width * 4; x++)
                        enlarged[y * width * 4 + x] = source[(y / 4) * width + x / 4];
                full.SetPixels32(enlarged);
                full.Apply();
                string output = Path.Combine(Application.dataPath, "../Docs/QA/tutorial" + number.ToString("00") + "-signs-" + aspect + ".png");
                File.WriteAllBytes(output, full.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(small);
                UnityEngine.Object.DestroyImmediate(full);
            }
        }
    }
}
