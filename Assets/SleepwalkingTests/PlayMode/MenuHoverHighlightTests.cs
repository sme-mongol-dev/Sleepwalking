using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Sleepwalking.Tests.PlayMode
{
    public sealed class MenuHoverHighlightTests
    {
        [UnityTest]
        public IEnumerator FourRoomTargetsHighlightWhiteAndClearOnLeave()
        {
            AsyncOperation loading = SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
            Assert.That(loading, Is.Not.Null);
            while (!loading.isDone) yield return null;
            yield return null;

            GameObject room = GameObject.Find("Waking room");
            Assert.That(room, Is.Not.Null);
            MonoBehaviour ui = null;
            foreach (MonoBehaviour component in FindAll<MonoBehaviour>())
                if (component != null && component.GetType().FullName == "Sleepwalking.SleepwalkingUI")
                    ui = component;
            Assert.That(ui, Is.Not.Null);

            Camera camera = (Camera)ui.GetType().GetField("roomCamera").GetValue(ui);
            Assert.That(camera, Is.Not.Null);
            MethodInfo process = ui.GetType().GetMethod("ProcessRoomInput", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo selected = ui.GetType().GetField("highlightedTarget", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(process, Is.Not.Null);
            Assert.That(selected, Is.Not.Null);

            var hotspots = new Dictionary<string, MonoBehaviour>();
            foreach (MonoBehaviour component in room.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null || component.GetType().FullName != "Sleepwalking.Visual.RoomHotspot") continue;
                string action = component.GetType().GetField("Action").GetValue(component).ToString();
                Assert.That(hotspots.ContainsKey(action), Is.False, "Duplicate hotspot action " + action);
                hotspots.Add(action, component);
            }
            Assert.That(hotspots.Keys, Is.EquivalentTo(new[] { "Dream", "Settings", "Credits", "Quit" }));
            Renderer wallpaper = null;
            foreach (Renderer layer in room.GetComponentsInChildren<Renderer>(true))
                if (layer.name == "Wallpaper") wallpaper = layer;
            Assert.That(wallpaper, Is.Not.Null);
            var backgroundBefore = new MaterialPropertyBlock();
            wallpaper.GetPropertyBlock(backgroundBefore);

            MonoBehaviour previous = null;
            var invisible = new List<string>();
            foreach (string action in new[] { "Dream", "Settings", "Credits", "Quit" })
            {
                MonoBehaviour hotspot = hotspots[action];
                Collider collider = hotspot.GetComponent<Collider>();
                MeshRenderer artwork = (MeshRenderer)hotspot.GetType().GetField("Artwork").GetValue(hotspot);
                Assert.That(collider, Is.Not.Null, action);
                Assert.That(artwork, Is.Not.Null, action);
                Assert.That(artwork.sharedMaterial.HasProperty("_Highlight") ||
                    artwork.sharedMaterial.HasProperty("_EmissionColor"), Is.True, action);
                process.Invoke(ui, new object[] { camera, new Vector2(-100, -100), false, false, false });
                Color32[] before = CaptureRoom(camera, action == "Dream" ? "MenuHoverDreamBefore.png" : null);

                bool found = false;
                Bounds bounds = collider.bounds;
                for (int y = 0; y <= 10 && !found; y++)
                for (int x = 0; x <= 10 && !found; x++)
                {
                    Vector3 world = new Vector3(
                        Mathf.Lerp(bounds.min.x, bounds.max.x, (x + .5f) / 11f),
                        Mathf.Lerp(bounds.min.y, bounds.max.y, (y + .5f) / 11f),
                        bounds.center.z);
                    Vector3 screen = camera.WorldToScreenPoint(world);
                    if (screen.z <= 0) continue;
                    Vector2 pointer = new Vector2(
                        screen.x * Screen.width / Mathf.Max(1, camera.pixelWidth),
                        screen.y * Screen.height / Mathf.Max(1, camera.pixelHeight));
                    process.Invoke(ui, new object[] { camera, pointer, false, false, false });
                    found = (UnityEngine.Object)selected.GetValue(ui) == hotspot;
                }
                Assert.That(found, Is.True, action + " never received a real room raycast.");
                AssertHighlight(artwork, .14f, action + " hover");
                Color32[] after = CaptureRoom(camera, action == "Dream" ? "MenuHoverDreamAfter.png" : null);
                int brightened = 0;
                for (int q = 0; q < before.Length; q++)
                    if (after[q].r + after[q].g + after[q].b >
                        before[q].r + before[q].g + before[q].b + 8)
                        brightened++;
                if (brightened == 0)
                    invisible.Add(action + " (" + artwork.sharedMaterial.name + " / " +
                        artwork.sharedMaterial.shader.name + ")");
                if (previous != null)
                {
                    MeshRenderer oldArtwork = (MeshRenderer)previous.GetType().GetField("Artwork").GetValue(previous);
                    AssertHighlight(oldArtwork, 0f, previous.name + " leave");
                }
                previous = hotspot;
            }

            process.Invoke(ui, new object[] { camera, new Vector2(-100, -100), false, false, false });
            Assert.That(selected.GetValue(ui), Is.Null, "Leaving the room targets must clear hover.");
            MeshRenderer lastArtwork = (MeshRenderer)previous.GetType().GetField("Artwork").GetValue(previous);
            AssertHighlight(lastArtwork, 0f, "pointer leave");
            var backgroundAfter = new MaterialPropertyBlock();
            wallpaper.GetPropertyBlock(backgroundAfter);
            Assert.That(backgroundAfter.GetFloat("_Highlight"),
                Is.EqualTo(backgroundBefore.GetFloat("_Highlight")), "The room background must not glow.");
            Assert.That(backgroundAfter.GetColor("_EmissionColor"),
                Is.EqualTo(backgroundBefore.GetColor("_EmissionColor")), "The room background emission must stay unchanged.");

            process.Invoke(ui, new object[] { camera, new Vector2(-100, -100), false, true, false });
            Assert.That(selected.GetValue(ui), Is.Not.Null, "Keyboard focus must still highlight a target.");
            ui.enabled = false;
            Assert.That(selected.GetValue(ui), Is.Null, "Disabling the UI must clear the highlight.");
            Assert.That(invisible, Is.Empty, "These actions did not visibly brighten: " +
                string.Join(", ", invisible));
        }

        static T[] FindAll<T>() where T : UnityEngine.Object =>
            UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None);

        static Color32[] CaptureRoom(Camera camera, string fileName)
        {
            var target = new RenderTexture(640, 360, 24);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture old = RenderTexture.active;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            try
            {
                Assert.That(target.Create(), Is.True);
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                image.Apply();
                if (fileName != null)
                    File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "Docs/QA/VisualLogs", fileName),
                        image.EncodeToPNG());
                return image.GetPixels32();
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = old;
                UnityEngine.Object.Destroy(image);
                target.Release();
                UnityEngine.Object.Destroy(target);
            }
        }

        static void AssertHighlight(MeshRenderer renderer, float offset, string label)
        {
            var properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            if (renderer.sharedMaterial.HasProperty("_Highlight"))
            {
                Assert.That(properties.GetFloat("_Highlight"), Is.EqualTo(offset).Within(.005f), label);
                return;
            }
            Color actual = properties.GetColor("_EmissionColor");
            Color baseline = renderer.sharedMaterial.GetColor("_EmissionColor");
            Assert.That(actual.r, Is.EqualTo(baseline.r + offset).Within(.005f), label + " red");
            Assert.That(actual.g, Is.EqualTo(baseline.g + offset).Within(.005f), label + " green");
            Assert.That(actual.b, Is.EqualTo(baseline.b + offset).Within(.005f), label + " blue");
        }
    }
}
