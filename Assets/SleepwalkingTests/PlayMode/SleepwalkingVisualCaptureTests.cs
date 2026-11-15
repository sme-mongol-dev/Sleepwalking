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
    public sealed class SleepwalkingVisualCaptureTests
    {
        [UnityTest]
        public IEnumerator CaptureLongLevelBackdrop()
        {
            AsyncOperation loading = SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
            Assert.That(loading, Is.Not.Null);
            while (!loading.isDone) yield return null;
            yield return null;

            MonoBehaviour game = null;
            foreach (MonoBehaviour component in GameObject.Find("Sleepwalking Game").GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().FullName == "Sleepwalking.SleepwalkingGame")
                    game = component;
            Assert.That(game, Is.Not.Null);
            Type type = game.GetType();
            type.GetField("completedDreams", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(game, 4);
            type.GetMethod("StartDream").Invoke(game, null);
            PropertyInfo inDream = type.GetProperty("InDream", BindingFlags.Instance | BindingFlags.Public);
            FieldInfo transitioning = type.GetField("transitioning", BindingFlags.Instance | BindingFlags.NonPublic);
            float deadline = Time.realtimeSinceStartup + 10f;
            while ((!((bool)inDream.GetValue(game)) || (bool)transitioning.GetValue(game)) &&
                Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That((bool)inDream.GetValue(game), Is.True);
            GameObject level = GameObject.Find("Dream L05");
            GameObject dreamer = GameObject.Find("Dreamer");
            Assert.That(level, Is.Not.Null);
            Assert.That(dreamer, Is.Not.Null);
            dreamer.GetComponent<Rigidbody2D>().simulated = false;
            Camera camera = (Camera)type.GetField("GameCamera").GetValue(game);
            FieldInfo style = type.GetField("style", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo applyStyle = type.GetMethod("ApplyStyle", BindingFlags.Instance | BindingFlags.NonPublic);
            Transform start = level.transform.Find("Markers/PlayerStart");
            Transform exit = GameObject.Find("BedExit")?.transform;
            Assert.That(start, Is.Not.Null);
            Assert.That(exit, Is.Not.Null);
            string[] names = { "Meat", "PostApocalypse", "DeepWater", "Heaven" };

            for (int q = 0; q < names.Length; q++)
            {
                Transform previous = level.transform.Find("Dream style");
                if (previous != null)
                {
                    UnityEngine.Object.Destroy(previous.gameObject);
                    yield return null;
                }
                style.SetValue(game, q);
                applyStyle.Invoke(game, null);
                for (int ratio = 0; ratio < 2; ratio++)
                {
                    RenderTexture original = camera.targetTexture;
                    RenderTexture target = new RenderTexture(ratio == 0 ? 1280 : 1024,
                        ratio == 0 ? 720 : 768, 24);
                    target.Create();
                    camera.targetTexture = target;
                    float lastX = float.NegativeInfinity;
                    for (int edge = 0; edge < 3; edge++)
                    {
                        float x = edge == 0 ? start.position.x :
                            edge == 1 ? (start.position.x + exit.position.x) / 2f : exit.position.x;
                        dreamer.transform.position = new Vector3(x, start.position.y + 16f, 0f);
                        yield return new WaitForSecondsRealtime(1.1f);
                        yield return null;
                        Transform theme = level.transform.Find("Dream style");
                        Transform background = theme.Find("Distant Background Left");
                        Assert.That(background, Is.Not.Null);
                        Renderer art = background.GetComponent<Renderer>();
                        Assert.That(art.sharedMaterial.GetColor("_Tint").r, Is.LessThan(1f));
                        Assert.That(background.position.x, Is.EqualTo(camera.transform.position.x).Within(.01f));
                        Assert.That(background.position.y, Is.EqualTo(camera.transform.position.y).Within(.01f));
                        if (edge > 0) Assert.That(camera.transform.position.x, Is.GreaterThan(lastX + 32f));
                        lastX = camera.transform.position.x;
                        Texture image = art.sharedMaterial.mainTexture;
                        Assert.That(art.bounds.size.x / art.bounds.size.y,
                            Is.EqualTo((float)image.width / image.height).Within(.01f));
                        float halfWidth = camera.orthographicSize * camera.aspect;
                        Assert.That(art.bounds.min.x, Is.LessThanOrEqualTo(camera.transform.position.x - halfWidth));
                        Assert.That(art.bounds.max.x, Is.GreaterThanOrEqualTo(camera.transform.position.x + halfWidth));
                        Assert.That(art.bounds.min.y, Is.LessThanOrEqualTo(camera.transform.position.y - camera.orthographicSize));
                        Assert.That(art.bounds.max.y, Is.GreaterThanOrEqualTo(camera.transform.position.y + camera.orthographicSize));
                        CaptureFinalFrame(camera, q, "backdrop-L05-" + names[q] + "-" +
                            (edge == 0 ? "start" : edge == 1 ? "middle" : "end") +
                            (ratio == 0 ? "-16x9" : "-4x3"), false, true);
                    }
                    camera.targetTexture = original;
                    target.Release();
                    UnityEngine.Object.Destroy(target);
                }
            }
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator CaptureFollowingDreamArt()
        {
            AsyncOperation loading = SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
            Assert.That(loading, Is.Not.Null);
            while (!loading.isDone) yield return null;
            yield return null;

            MonoBehaviour game = null;
            foreach (MonoBehaviour component in GameObject.Find("Sleepwalking Game").GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().FullName == "Sleepwalking.SleepwalkingGame")
                    game = component;
            Assert.That(game, Is.Not.Null);
            Type type = game.GetType();
            FieldInfo completed = type.GetField("completedDreams", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo style = type.GetField("style", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo applyStyle = type.GetMethod("ApplyStyle", BindingFlags.Instance | BindingFlags.NonPublic);
            PropertyInfo inDream = type.GetProperty("InDream", BindingFlags.Instance | BindingFlags.Public);
            FieldInfo transitioning = type.GetField("transitioning", BindingFlags.Instance | BindingFlags.NonPublic);
            Camera camera = (Camera)type.GetField("GameCamera").GetValue(game);
            string[] names = { "Meat", "PostApocalypse", "DeepWater", "Heaven" };

            foreach (int levelIndex in new[] { 4, 7 })
            {
                completed.SetValue(game, levelIndex - 1);
                type.GetMethod("StartDream").Invoke(game, null);
                float deadline = Time.realtimeSinceStartup + 10f;
                while ((!((bool)inDream.GetValue(game)) || (bool)transitioning.GetValue(game)) &&
                    Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That((bool)inDream.GetValue(game), Is.True);
                GameObject level = GameObject.Find("Dream L" + levelIndex.ToString("00"));
                GameObject dreamer = GameObject.Find("Dreamer");
                Assert.That(level, Is.Not.Null);
                Assert.That(dreamer, Is.Not.Null);
                dreamer.GetComponent<Rigidbody2D>().simulated = false;

                for (int q = 0; q < names.Length; q++)
                {
                    Transform previous = level.transform.Find("Dream style");
                    if (previous != null)
                    {
                        UnityEngine.Object.Destroy(previous.gameObject);
                        yield return null;
                    }
                    style.SetValue(game, q);
                    applyStyle.Invoke(game, null);
                    for (int ratio = 0; ratio < 2; ratio++)
                    {
                        RenderTexture original = camera.targetTexture;
                        RenderTexture target = new RenderTexture(ratio == 0 ? 1280 : 1024,
                            ratio == 0 ? 720 : 768, 24);
                        target.Create();
                        camera.targetTexture = target;
                        for (int section = 0; section < 2; section++)
                        {
                            string prefix = levelIndex == 4 ? "L04_" : "L07_";
                            string buttonName = prefix + (levelIndex == 4 ?
                                section == 0 ? "ButtonLaser" : "ButtonSecond" :
                                section == 0 ? "ButtonA" : "ButtonC");
                            string laserName = prefix + (section == 0 ? "Laser" : "SecondLaser") + "_Beam";
                            GameObject button = GameObject.Find(buttonName);
                            GameObject beam = GameObject.Find(laserName);
                            Assert.That(button, Is.Not.Null, buttonName);
                            Assert.That(beam, Is.Not.Null, laserName);
                            SpriteRenderer buttonArt = button.transform.Find("Dream Button Art")?.GetComponent<SpriteRenderer>();
                            SpriteRenderer beamArt = beam.transform.Find("Dream Hazard Art")?.GetComponent<SpriteRenderer>();
                            Assert.That(buttonArt, Is.Not.Null);
                            Assert.That(beamArt, Is.Not.Null);
                            Assert.That(buttonArt.sprite.name, Is.EqualTo("Button_Up"));
                            Assert.That(buttonArt.bounds.size.x / 32f, Is.InRange(.86f, .94f));
                            Assert.That(beamArt.enabled, Is.True);
                            dreamer.transform.position = new Vector3((button.transform.position.x +
                                beam.transform.position.x) / 2f, button.transform.position.y + 16f, 0f);
                            yield return new WaitForSecondsRealtime(.9f);
                            yield return null;
                            Transform theme = level.transform.Find("Dream style");
                            Assert.That(theme, Is.Not.Null);
                            Transform background = theme.Find("Distant Background Left");
                            Assert.That(background, Is.Not.Null);
                            Renderer backgroundArt = background.GetComponent<Renderer>();
                            float halfWidth = camera.orthographicSize * camera.aspect;
                            Assert.That(backgroundArt.bounds.min.x, Is.LessThanOrEqualTo(camera.transform.position.x - halfWidth));
                            Assert.That(backgroundArt.bounds.max.x, Is.GreaterThanOrEqualTo(camera.transform.position.x + halfWidth));
                            Assert.That(backgroundArt.bounds.min.y, Is.LessThanOrEqualTo(camera.transform.position.y - camera.orthographicSize));
                            Assert.That(backgroundArt.bounds.max.y, Is.GreaterThanOrEqualTo(camera.transform.position.y + camera.orthographicSize));
                            if (levelIndex == 7)
                            {
                                Renderer oldCue = level.transform.Find("Readability/WallGrip_Cue")?.GetComponent<Renderer>();
                                if (oldCue != null) Assert.That(oldCue.forceRenderingOff, Is.True);
                            }
                            CaptureFinalFrame(camera, q, "laser-follow-" + levelIndex.ToString("00") +
                                "-" + names[q] + "-" + (section == 0 ? "first" : "second") +
                                (ratio == 0 ? "-16x9" : "-4x3"), false, true);
                        }
                        if (levelIndex == 7)
                        {
                            Transform exit = GameObject.Find("BedExit")?.transform;
                            Assert.That(exit, Is.Not.Null);
                            dreamer.transform.position = exit.position + Vector3.up * 16f;
                            yield return new WaitForSecondsRealtime(.9f);
                            yield return null;
                            Transform theme = level.transform.Find("Dream style");
                            Renderer backgroundArt = theme.Find("Distant Background Left").GetComponent<Renderer>();
                            float halfWidth = camera.orthographicSize * camera.aspect;
                            Assert.That(backgroundArt.bounds.min.x, Is.LessThanOrEqualTo(camera.transform.position.x - halfWidth));
                            Assert.That(backgroundArt.bounds.max.x, Is.GreaterThanOrEqualTo(camera.transform.position.x + halfWidth));
                            CaptureFinalFrame(camera, q, "backdrop-end-07-" + names[q] +
                                (ratio == 0 ? "-16x9" : "-4x3"), false, true);
                        }
                        camera.targetTexture = original;
                        target.Release();
                        UnityEngine.Object.Destroy(target);
                    }
                }

                if (levelIndex == 7) break;
                type.GetMethod("CompleteDream").Invoke(game, null);
                deadline = Time.realtimeSinceStartup + 10f;
                while (((bool)inDream.GetValue(game) || (bool)transitioning.GetValue(game)) &&
                    Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That((bool)inDream.GetValue(game), Is.False);
            }
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ButtonAtlasShowsUpAndDownStates()
        {
            AsyncOperation loading = SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
            Assert.That(loading, Is.Not.Null);
            while (!loading.isDone) yield return null;
            yield return null;

            MonoBehaviour game = null;
            foreach (MonoBehaviour component in GameObject.Find("Sleepwalking Game").GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().FullName == "Sleepwalking.SleepwalkingGame")
                    game = component;
            Assert.That(game, Is.Not.Null);
            Type type = game.GetType();
            GameObject[] themes = (GameObject[])type.GetField("ThemePrefabs").GetValue(game);
            Assert.That(themes.Length, Is.EqualTo(4));
            foreach (GameObject prefab in themes)
            {
                MonoBehaviour theme = null;
                foreach (MonoBehaviour component in prefab.GetComponents<MonoBehaviour>())
                    if (component != null && component.GetType().Name == "DreamTheme") theme = component;
                Assert.That(theme, Is.Not.Null);
                Sprite up = (Sprite)theme.GetType().GetField("ButtonUpArt").GetValue(theme);
                Sprite down = (Sprite)theme.GetType().GetField("ButtonDownArt").GetValue(theme);
                Assert.That(up, Is.Not.Null);
                Assert.That(down, Is.Not.Null);
                Assert.That(up.name, Is.EqualTo("Button_Up"));
                Assert.That(down.name, Is.EqualTo("Button_Down"));
                Assert.That(up.texture.name, Is.EqualTo("TL_Button"));
                Assert.That(down.texture.name, Is.EqualTo("TL_Button"));
            }

            type.GetField("completedDreams", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(game, 3);
            type.GetMethod("StartDream", BindingFlags.Instance | BindingFlags.Public).Invoke(game, null);
            PropertyInfo inDream = type.GetProperty("InDream", BindingFlags.Instance | BindingFlags.Public);
            FieldInfo transitioning = type.GetField("transitioning", BindingFlags.Instance | BindingFlags.NonPublic);
            float deadline = Time.realtimeSinceStartup + 10f;
            while ((!((bool)inDream.GetValue(game)) || (bool)transitioning.GetValue(game)) &&
                Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That((bool)inDream.GetValue(game), Is.True);

            GameObject button = GameObject.Find("L04_ButtonLaser");
            Assert.That(button, Is.Not.Null);
            Collider2D shape = button.GetComponent<Collider2D>();
            Bounds originalBounds = shape.bounds;
            SpriteRenderer art = button.transform.Find("Dream Button Art")?.GetComponent<SpriteRenderer>();
            SpriteRenderer outline = button.transform.Find("Dream Button Outline")?.GetComponent<SpriteRenderer>();
            Assert.That(art, Is.Not.Null);
            Assert.That(outline, Is.Not.Null);
            Assert.That(outline.enabled, Is.True);
            Color raisedOutlineColor = outline.color;
            Assert.That(art.sprite.name, Is.EqualTo("Button_Up"));
            Assert.That(art.enabled, Is.True);
            Assert.That(button.GetComponent<MeshRenderer>().enabled, Is.False);
            Assert.That(Mathf.Abs(art.bounds.min.y - shape.bounds.min.y), Is.LessThan(.01f));
            CaptureFinalFrame((Camera)type.GetField("GameCamera").GetValue(game), 0, "button-art-L04-up", false);

            MonoBehaviour mechanism = null;
            foreach (MonoBehaviour component in button.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().Name == "SleepwalkingMechanism") mechanism = component;
            Assert.That(mechanism, Is.Not.Null);
            type.GetMethod("PressButton", BindingFlags.Instance | BindingFlags.Public).Invoke(game, new object[] { mechanism });
            yield return null;
            yield return null;
            Assert.That(art.sprite.name, Is.EqualTo("Button_Down"));
            Assert.That(outline.color, Is.Not.EqualTo(raisedOutlineColor));
            CaptureFinalFrame((Camera)type.GetField("GameCamera").GetValue(game), 0, "button-art-L04-down", false);
            yield return new WaitForSecondsRealtime(.22f);
            yield return null;
            yield return null;
            Assert.That(art.sprite.name, Is.EqualTo("Button_Up"));
            Assert.That(Vector3.Distance(shape.bounds.center, originalBounds.center), Is.LessThan(.001f));
            Assert.That(Vector3.Distance(shape.bounds.size, originalBounds.size), Is.LessThan(.001f));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator DreamUsesDirectScreenRendering()
        {
            AsyncOperation loading = SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
            Assert.That(loading, Is.Not.Null);
            while (!loading.isDone) yield return null;
            yield return null;

            MonoBehaviour game = null;
            foreach (MonoBehaviour component in GameObject.Find("Sleepwalking Game").GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().FullName == "Sleepwalking.SleepwalkingGame")
                    game = component;
            Assert.That(game, Is.Not.Null);
            Type type = game.GetType();
            Camera camera = (Camera)type.GetField("GameCamera", BindingFlags.Instance | BindingFlags.Public).GetValue(game);
            Canvas pixelOutput = camera.transform.Find("Pixel Room Output")?.GetComponent<Canvas>();
            Assert.That(pixelOutput, Is.Not.Null);
            Assert.That(camera.targetTexture, Is.Not.Null, "The waking room keeps its pixel output.");
            Assert.That(pixelOutput.enabled, Is.True);

            type.GetMethod("StartDream", BindingFlags.Instance | BindingFlags.Public).Invoke(game, null);
            PropertyInfo inDream = type.GetProperty("InDream", BindingFlags.Instance | BindingFlags.Public);
            FieldInfo transitioning = type.GetField("transitioning", BindingFlags.Instance | BindingFlags.NonPublic);
            float deadline = Time.realtimeSinceStartup + 10f;
            while ((!((bool)inDream.GetValue(game)) || (bool)transitioning.GetValue(game)) &&
                Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That((bool)inDream.GetValue(game), Is.True);
            Assert.That(camera.targetTexture, Is.Null);
            Assert.That(pixelOutput.enabled, Is.False);
            Assert.That(camera.pixelWidth, Is.EqualTo(Screen.width));
            Assert.That(camera.pixelHeight, Is.EqualTo(Screen.height));

            type.GetMethod("ReturnToRoom", BindingFlags.Instance | BindingFlags.Public).Invoke(game, null);
            yield return null;
            Assert.That((bool)inDream.GetValue(game), Is.False);
            Assert.That(camera.targetTexture, Is.Not.Null);
            Assert.That(pixelOutput.enabled, Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator CaptureCaveShell()
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

            Type type = game.GetType();
            MethodInfo startDream = type.GetMethod("StartDream", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo completeDream = type.GetMethod("CompleteDream", BindingFlags.Instance | BindingFlags.Public);
            PropertyInfo inDream = type.GetProperty("InDream", BindingFlags.Instance | BindingFlags.Public);
            FieldInfo transitioning = type.GetField("transitioning", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo style = type.GetField("style", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo cameraField = type.GetField("GameCamera", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(startDream, Is.Not.Null);
            Assert.That(completeDream, Is.Not.Null);
            Assert.That(inDream, Is.Not.Null);
            Assert.That(transitioning, Is.Not.Null);
            Assert.That(style, Is.Not.Null);
            Assert.That(cameraField, Is.Not.Null);
            Camera gameCamera = (Camera)cameraField.GetValue(game);
            Assert.That(gameCamera, Is.Not.Null);
            type.GetField("tutorialStage", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(game, 2);

            for (int q = 1; q <= 7; q++)
            {
                startDream.Invoke(game, null);
                float deadline = Time.realtimeSinceStartup + 10f;
                while ((!((bool)inDream.GetValue(game)) || (bool)transitioning.GetValue(game)) &&
                    Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That((bool)inDream.GetValue(game), Is.True, "Dream " + q + " did not start.");
                yield return new WaitForSecondsRealtime(.8f);
                yield return null;
                yield return null;
                AssertTileCollisionMatchesArt(GameObject.Find("Dream L" + q.ToString("00")));
                Vector3 startCamera = gameCamera.transform.position;
                GameObject exit = GameObject.Find("BedExit");
                Assert.That(exit, Is.Not.Null, "Level " + q + " needs a visible end point.");
                Vector3 startView = gameCamera.WorldToViewportPoint(GameObject.Find("Dreamer").transform.position);
                Assert.That(startView.x, Is.InRange(0f, 1f));
                Assert.That(startView.y, Is.InRange(0f, 1f));
                CaptureFinalFrame(gameCamera, (int)style.GetValue(game),
                    "grid-follow-L" + q.ToString("00") + "-start", false);

                RenderTexture originalTarget = gameCamera.targetTexture;
                RenderTexture wideTarget = new RenderTexture(320, 180, 24);
                wideTarget.Create();
                gameCamera.targetTexture = wideTarget;
                yield return null;
                Assert.That(Mathf.Abs(gameCamera.transform.position.y - startCamera.y), Is.LessThan(.001f));
                startView = gameCamera.WorldToViewportPoint(GameObject.Find("Dreamer").transform.position);
                Assert.That(startView.x, Is.InRange(0f, 1f), "16:9 start X L" + q);
                Assert.That(startView.y, Is.InRange(0f, 1f), "16:9 start Y L" + q);
                gameCamera.targetTexture = originalTarget;
                UnityEngine.Object.Destroy(wideTarget);
                yield return null;

                if (q == 5 || q == 6 || q == 7)
                {
                    // Камера держит весь уровень в одном неподвижном кадре.
                    GameObject dreamer = GameObject.Find("Dreamer");
                    Assert.That(dreamer, Is.Not.Null);
                    Rigidbody2D body = dreamer.GetComponent<Rigidbody2D>();
                    Assert.That(body, Is.Not.Null);
                    body.simulated = false;
                    float x = (q == 5 ? 14.5f : q == 6 ? 22f : 20.5f) * 32f;
                    float y = (q == 5 ? 8.5f : q == 6 ? 7.5f : 3.5f) * 32f;
                    dreamer.transform.position = new Vector3(x, y, 0);
                    yield return new WaitForSecondsRealtime(1.2f);
                    Assert.That(gameCamera.transform.position.x, Is.EqualTo(startCamera.x).Within(.001f));
                    Assert.That(Mathf.Abs(gameCamera.transform.position.y - startCamera.y), Is.LessThan(.001f));
                    Vector3 farView = gameCamera.WorldToViewportPoint(dreamer.transform.position);
                    Assert.That(farView.x, Is.InRange(0f, 1f));
                    CaptureFinalFrame(gameCamera, (int)style.GetValue(game),
                        "grid-follow-L" + q.ToString("00") + "-far", false, true);
                }

                if (q == 7) break;
                completeDream.Invoke(game, null);
                deadline = Time.realtimeSinceStartup + 10f;
                while (((bool)inDream.GetValue(game) || (bool)transitioning.GetValue(game)) &&
                    Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That((bool)inDream.GetValue(game), Is.False);
            }
            LogAssert.NoUnexpectedReceived();
        }

        static void AssertTileCollisionMatchesArt(GameObject level)
        {
            Assert.That(level, Is.Not.Null);
            const float pitch = 32f;
            Transform geometry = level.transform.Find("Geometry");
            Transform matrix = level.transform.Find("Dream Tile Matrix");
            Assert.That(geometry, Is.Not.Null, level.name + " Geometry");
            Assert.That(matrix, Is.Not.Null, level.name + " static Dream Tile Matrix");
            MeshFilter staticMesh = matrix.GetComponent<MeshFilter>();
            MeshRenderer staticRenderer = matrix.GetComponent<MeshRenderer>();
            Assert.That(staticMesh, Is.Not.Null);
            Assert.That(staticMesh.sharedMesh, Is.Not.Null);
            Assert.That(staticRenderer, Is.Not.Null);
            Assert.That(staticRenderer.enabled, Is.True);

            HashSet<Vector2Int> expected = new HashSet<Vector2Int>();
            int staticSolids = 0;
            int dynamicSolids = 0;
            foreach (MonoBehaviour surface in level.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (surface == null || surface.GetType().FullName != "Sleepwalking.Levels.SleepwalkingTileSurface")
                    continue;
                Collider2D shape = surface.GetType().GetProperty("Solid")?.GetValue(surface) as Collider2D;
                Assert.That(shape, Is.Not.Null, surface.name);
                Assert.That(shape.isTrigger, Is.False, surface.name);
                if (surface.transform.IsChildOf(geometry))
                {
                    AddColliderCells(shape, expected, pitch);
                    staticSolids++;
                    continue;
                }

                Transform tiles = surface.transform.Find("Dream Tiles");
                Assert.That(tiles, Is.Not.Null, surface.name + " dynamic tiles");
                MeshFilter filter = tiles.GetComponent<MeshFilter>();
                MeshRenderer renderer = tiles.GetComponent<MeshRenderer>();
                Assert.That(filter, Is.Not.Null, surface.name);
                Assert.That(filter.sharedMesh, Is.Not.Null, surface.name);
                Assert.That(renderer, Is.Not.Null, surface.name);
                Assert.That(filter.sharedMesh.vertexCount, Is.EqualTo(8),
                    surface.name + " must cover its two authored cells");
                Assert.That(renderer.enabled, Is.True, surface.name + " must remain visible without collision");
                if (!shape.enabled)
                {
                    var properties = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(properties);
                    Assert.That(properties.GetFloat("_Opacity"), Is.EqualTo(.42f).Within(.01f), surface.name);
                }
                Bounds art = renderer.bounds;
                Bounds solid = shape.bounds;
                Assert.That(Mathf.Abs(art.min.x - solid.min.x), Is.LessThan(2f), surface.name + " left");
                Assert.That(Mathf.Abs(art.max.x - solid.max.x), Is.LessThan(2f), surface.name + " right");
                Assert.That(Mathf.Abs(art.min.y - solid.min.y), Is.LessThan(2f), surface.name + " bottom");
                Assert.That(Mathf.Abs(art.max.y - solid.max.y), Is.LessThan(2f), surface.name + " top");
                dynamicSolids++;
            }
            Assert.That(staticSolids, Is.GreaterThan(0));
            if (level.name == "Dream L05" || level.name == "Dream L06")
                Assert.That(dynamicSolids, Is.GreaterThan(0), level.name);

            Mesh mesh = staticMesh.sharedMesh;
            Assert.That(mesh.vertexCount, Is.EqualTo(expected.Count * 4), level.name);
            Assert.That(mesh.triangles.Length, Is.EqualTo(expected.Count * 6), level.name);
            HashSet<Vector2Int> actual = new HashSet<Vector2Int>();
            Vector3[] vertices = mesh.vertices;
            for (int q = 0; q < vertices.Length; q += 4)
            {
                float left = float.PositiveInfinity;
                float right = float.NegativeInfinity;
                float bottom = float.PositiveInfinity;
                float top = float.NegativeInfinity;
                for (int e = 0; e < 4; e++)
                {
                    Vector3 world = matrix.TransformPoint(vertices[q + e]);
                    left = Mathf.Min(left, world.x);
                    right = Mathf.Max(right, world.x);
                    bottom = Mathf.Min(bottom, world.y);
                    top = Mathf.Max(top, world.y);
                }
                Assert.That(right - left, Is.EqualTo(pitch).Within(.02f), level.name + " tile width");
                Assert.That(top - bottom, Is.EqualTo(pitch).Within(.02f), level.name + " tile height");
                Vector2Int cell = new Vector2Int(Mathf.RoundToInt(left / pitch),
                    Mathf.RoundToInt(bottom / pitch));
                Assert.That(actual.Add(cell), Is.True, level.name + " duplicate tile " + cell);
            }
            foreach (Vector2Int cell in expected)
                Assert.That(actual.Contains(cell), Is.True, level.name + " missing solid tile " + cell);
            foreach (Vector2Int cell in actual)
                Assert.That(expected.Contains(cell), Is.True, level.name + " art without solid " + cell);
        }

        static void AddColliderCells(Collider2D shape, HashSet<Vector2Int> cells, float pitch)
        {
            Bounds bounds = shape.bounds;
            int x0 = Mathf.RoundToInt(bounds.min.x / pitch);
            int x1 = Mathf.RoundToInt(bounds.max.x / pitch);
            int y0 = Mathf.RoundToInt(bounds.min.y / pitch);
            int y1 = Mathf.RoundToInt(bounds.max.y / pitch);
            Assert.That(Mathf.Abs(bounds.min.x / pitch - x0), Is.LessThan(.001f), shape.name + " left grid");
            Assert.That(Mathf.Abs(bounds.max.x / pitch - x1), Is.LessThan(.001f), shape.name + " right grid");
            Assert.That(Mathf.Abs(bounds.min.y / pitch - y0), Is.LessThan(.001f), shape.name + " bottom grid");
            Assert.That(Mathf.Abs(bounds.max.y / pitch - y1), Is.LessThan(.001f), shape.name + " top grid");
            for (int y = y0; y < y1; y++)
            for (int x = x0; x < x1; x++)
                cells.Add(new Vector2Int(x, y));
        }

        [UnityTest]
        public IEnumerator CaptureFinalL01AndL07Far()
        {
            AsyncOperation loading = SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
            Assert.That(loading, Is.Not.Null);
            while (!loading.isDone)
                yield return null;
            yield return null;

            GameObject gameObject = GameObject.Find("Sleepwalking Game");
            Assert.That(gameObject, Is.Not.Null);
            MonoBehaviour game = null;
            foreach (MonoBehaviour component in gameObject.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().FullName == "Sleepwalking.SleepwalkingGame")
                    game = component;
            Assert.That(game, Is.Not.Null);

            Type type = game.GetType();
            MethodInfo startDream = type.GetMethod("StartDream", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo completeDream = type.GetMethod("CompleteDream", BindingFlags.Instance | BindingFlags.Public);
            PropertyInfo inDream = type.GetProperty("InDream", BindingFlags.Instance | BindingFlags.Public);
            FieldInfo transitioning = type.GetField("transitioning", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo style = type.GetField("style", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo cameraField = type.GetField("GameCamera", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(startDream, Is.Not.Null);
            Assert.That(completeDream, Is.Not.Null);
            Assert.That(inDream, Is.Not.Null);
            Assert.That(transitioning, Is.Not.Null);
            Assert.That(style, Is.Not.Null);
            Assert.That(cameraField, Is.Not.Null);
            Camera gameCamera = (Camera)cameraField.GetValue(game);
            Assert.That(gameCamera, Is.Not.Null);

            for (int q = 1; q <= 7; q++)
            {
                startDream.Invoke(game, null);
                float deadline = Time.realtimeSinceStartup + 10f;
                while ((!((bool)inDream.GetValue(game)) || (bool)transitioning.GetValue(game)) && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.That((bool)inDream.GetValue(game), Is.True, "Dream " + q + " did not start.");
                Assert.That((bool)transitioning.GetValue(game), Is.False);
                Assert.That(GameObject.Find("Dream L" + q.ToString("00")), Is.Not.Null);

                if (q == 1 || q == 7)
                {
                    yield return new WaitForSecondsRealtime(1.2f);
                    yield return null;
                    yield return null;
                    if (q == 7)
                    {
                        // Фиксированный дальний ракурс проверяет покрытие фона за концом маршрута.
                        gameCamera.transform.position = new Vector3(20.5f * 32f, 4f * 32f, -20f);
                    }
                    string name = q == 1 ? "dream-L01-final-main" : "dream-L07-far-final-main";
                    CaptureFinalFrame(gameCamera, (int)style.GetValue(game), name, q == 7);
                }

                if (q == 7) break;
                completeDream.Invoke(game, null);
                deadline = Time.realtimeSinceStartup + 10f;
                while (((bool)inDream.GetValue(game) || (bool)transitioning.GetValue(game)) && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.That((bool)inDream.GetValue(game), Is.False);
                Assert.That((bool)transitioning.GetValue(game), Is.False);
            }
            LogAssert.NoUnexpectedReceived();
        }

        static void CaptureFinalFrame(Camera gameCamera, int style, string name, bool forcedCamera,
            bool positionedActor = false)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string path = Path.Combine(projectRoot, "Docs", "QA", name + ".png");
            CaptureDreamFrame(gameCamera, path, " Style=" + style +
                " Direct=" + (gameCamera.targetTexture == null) +
                " ForcedCamera=" + forcedCamera + " PositionedActor=" + positionedActor);
        }

        static void CaptureDreamFrame(Camera camera, string path, string metadata = "")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            GameObject dreamer = GameObject.Find("Dreamer");
            Assert.That(dreamer, Is.Not.Null);
            Vector3 playerViewport = camera.WorldToViewportPoint(dreamer.transform.position);
            var target = RenderTexture.GetTemporary(Mathf.Max(1, camera.pixelWidth),
                Mathf.Max(1, camera.pixelHeight), 24);
            RenderTexture original = camera.targetTexture;
            RenderTexture active = RenderTexture.active;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
                File.WriteAllText(Path.ChangeExtension(path, ".txt"),
                    "Camera=" + camera.transform.position + " Size=" + camera.orthographicSize +
                    " Player=" + dreamer.transform.position + " PlayerViewport=" + playerViewport +
                    " Target=" + target.width + "x" + target.height + metadata + " TimeScale=" + Time.timeScale);
            }
            finally
            {
                camera.targetTexture = original;
                RenderTexture.active = active;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.Destroy(image);
            }
        }

        [UnityTest]
        public IEnumerator CaptureSuppliedBackdrops()
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

            Type type = game.GetType();
            type.GetMethod("StartDream", BindingFlags.Instance | BindingFlags.Public).Invoke(game, null);
            PropertyInfo inDream = type.GetProperty("InDream", BindingFlags.Instance | BindingFlags.Public);
            FieldInfo transitioning = type.GetField("transitioning", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo style = type.GetField("style", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo applyStyle = type.GetMethod("ApplyStyle", BindingFlags.Instance | BindingFlags.NonPublic);
            Camera gameCamera = (Camera)type.GetField("GameCamera", BindingFlags.Instance | BindingFlags.Public).GetValue(game);
            float deadline = Time.realtimeSinceStartup + 10f;
            while ((!((bool)inDream.GetValue(game)) || (bool)transitioning.GetValue(game)) &&
                Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That((bool)inDream.GetValue(game), Is.True);
            GameObject level = GameObject.Find("Dream L01");
            Assert.That(level, Is.Not.Null);

            string[] names = { "BloodBgnew", "WarBG" };
            string[] tags = { "meat", "war" };
            for (int q = 0; q < names.Length; q++)
            {
                Transform oldTheme = level.transform.Find("Dream style");
                if (oldTheme != null)
                {
                    UnityEngine.Object.Destroy(oldTheme.gameObject);
                    yield return null;
                }
                style.SetValue(game, q);
                applyStyle.Invoke(game, null);
                yield return null;
                yield return null;
                Transform theme = level.transform.Find("Dream style");
                Assert.That(theme, Is.Not.Null);
                Transform background = theme.Find("Distant Background Left");
                Assert.That(background, Is.Not.Null);
                Texture texture = background.GetComponent<MeshRenderer>().sharedMaterial.mainTexture;
                Assert.That(texture, Is.Not.Null);
                Assert.That(texture.name, Is.EqualTo(names[q]));
                Assert.That(theme.Find("Distant Background Right").gameObject.activeSelf, Is.False);
                Assert.That(background.localScale.x / background.localScale.y,
                    Is.EqualTo((float)texture.width / texture.height).Within(.01f));
                CaptureFinalFrame(gameCamera, q, "supplied-backdrop-" + tags[q], false);
            }
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator CaptureEnvironmentTraps()
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
            Type type = game.GetType();
            type.GetField("completedDreams", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(game, 6);
            type.GetMethod("StartDream", BindingFlags.Instance | BindingFlags.Public).Invoke(game, null);
            PropertyInfo inDream = type.GetProperty("InDream", BindingFlags.Instance | BindingFlags.Public);
            FieldInfo transitioning = type.GetField("transitioning", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo style = type.GetField("style", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo applyStyle = type.GetMethod("ApplyStyle", BindingFlags.Instance | BindingFlags.NonPublic);
            Camera gameCamera = (Camera)type.GetField("GameCamera", BindingFlags.Instance | BindingFlags.Public).GetValue(game);
            float deadline = Time.realtimeSinceStartup + 10f;
            while ((!((bool)inDream.GetValue(game)) || (bool)transitioning.GetValue(game)) &&
                Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That((bool)inDream.GetValue(game), Is.True);
            GameObject level = GameObject.Find("Dream L07");
            Assert.That(level, Is.Not.Null);

            string[] targets = { "L07_PitSpikes_1", "L07_Laser_Base", "L07_Laser_Beam", "L07_Saw" };
            string[] parts = { "Spikes", "LaserBase", "LaserBeam", "Saw" };
            string[] themes = { "Meat", "PostApoc", "DarkWater", "Heaven" };
            Collider2D[] shapes = new Collider2D[targets.Length];
            Bounds[] originalBounds = new Bounds[targets.Length];
            for (int e = 0; e < targets.Length; e++)
            {
                shapes[e] = GameObject.Find(targets[e])?.GetComponent<Collider2D>();
                Assert.That(shapes[e], Is.Not.Null, targets[e]);
                originalBounds[e] = shapes[e].bounds;
            }

            for (int q = 0; q < themes.Length; q++)
            {
                Transform oldTheme = level.transform.Find("Dream style");
                if (oldTheme != null)
                {
                    UnityEngine.Object.Destroy(oldTheme.gameObject);
                    yield return null;
                }
                style.SetValue(game, q);
                applyStyle.Invoke(game, null);
                yield return null;
                Transform theme = level.transform.Find("Dream style");
                Assert.That(theme, Is.Not.Null);
                foreach (MonoBehaviour component in theme.GetComponents<MonoBehaviour>())
                    if (component != null && component.GetType().Name == "DreamTheme")
                        component.GetType().GetMethod("UpdateHazardArt", BindingFlags.Instance | BindingFlags.NonPublic)
                            .Invoke(component, null);
                for (int e = 0; e < shapes.Length; e++)
                {
                    Transform art = shapes[e].transform.Find("Dream Hazard Art");
                    Assert.That(art, Is.Not.Null, targets[e] + " " + themes[q]);
                    SpriteRenderer sprite = art.GetComponent<SpriteRenderer>();
                    Assert.That(sprite.sprite.name, Is.EqualTo(themes[q] + "_" + parts[e]));
                    string atlas = parts[e] == "LaserBase" ? "T_Lasers" :
                        "TL_" + themes[q] + "_environment";
                    Assert.That(sprite.sprite.texture.name, Is.EqualTo(atlas));
                    Assert.That(Vector3.Distance(shapes[e].bounds.center, originalBounds[e].center), Is.LessThan(.001f));
                    Assert.That(Vector3.Distance(shapes[e].bounds.size, originalBounds[e].size), Is.LessThan(.001f));
                    Assert.That(shapes[e].isTrigger, Is.True);
                    Assert.That(shapes[e].GetComponent<MeshRenderer>().enabled, Is.False);
                    if (parts[e] == "LaserBeam")
                    {
                        int pixelHeight = gameCamera.targetTexture != null ? gameCamera.targetTexture.height :
                            Mathf.Max(1, gameCamera.pixelHeight);
                        float widthPixels = shapes[e].bounds.size.x * pixelHeight /
                            (gameCamera.orthographicSize * 2f);
                        Assert.That(widthPixels, Is.InRange(3f, 4f));
                    }
                }
                CaptureFinalFrame(gameCamera, q, "environment-traps-" + themes[q].ToLowerInvariant(), false);
            }
            MonoBehaviour sawMechanism = null;
            foreach (MonoBehaviour component in shapes[3].GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().Name == "SleepwalkingMechanism")
                    sawMechanism = component;
            Assert.That(sawMechanism, Is.Not.Null);
            sawMechanism.GetType().GetMethod("DisableFor", BindingFlags.Instance | BindingFlags.Public)
                .Invoke(sawMechanism, new object[] { 1f });
            yield return null;
            Transform lastTheme = level.transform.Find("Dream style");
            foreach (MonoBehaviour component in lastTheme.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().Name == "DreamTheme")
                    component.GetType().GetMethod("UpdateHazardArt", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(component, null);
            Assert.That(shapes[3].enabled, Is.False);
            Assert.That(shapes[3].transform.Find("Dream Hazard Art").GetComponent<SpriteRenderer>().enabled, Is.False);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator CaptureFirstCycleAssignedStyles()
        {
            AsyncOperation loading = SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
            Assert.That(loading, Is.Not.Null);
            while (!loading.isDone)
                yield return null;
            yield return null;

            GameObject gameObject = GameObject.Find("Sleepwalking Game");
            Assert.That(gameObject, Is.Not.Null);
            MonoBehaviour game = null;
            foreach (MonoBehaviour component in gameObject.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().FullName == "Sleepwalking.SleepwalkingGame")
                    game = component;
            Assert.That(game, Is.Not.Null);

            Type type = game.GetType();
            MethodInfo startDream = type.GetMethod("StartDream", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo completeDream = type.GetMethod("CompleteDream", BindingFlags.Instance | BindingFlags.Public);
            PropertyInfo inDream = type.GetProperty("InDream", BindingFlags.Instance | BindingFlags.Public);
            FieldInfo transitioning = type.GetField("transitioning", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo style = type.GetField("style", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo cameraField = type.GetField("GameCamera", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(startDream, Is.Not.Null);
            Assert.That(completeDream, Is.Not.Null);
            Assert.That(inDream, Is.Not.Null);
            Assert.That(transitioning, Is.Not.Null);
            Assert.That(style, Is.Not.Null);
            Assert.That(cameraField, Is.Not.Null);

            Camera gameCamera = (Camera)cameraField.GetValue(game);
            Assert.That(gameCamera, Is.Not.Null);
            string[] names = { "Meat", "PostApocalypse", "DeepWater", "Heaven" };
            bool[] captured = new bool[names.Length];
            int captureCount = 0;

            // Сохраняем только реально назначенные стили первого цикла, не подменяя конфигурацию уровня.
            for (int q = 0; q < 7; q++)
            {
                startDream.Invoke(game, null);
                float deadline = Time.realtimeSinceStartup + 10f;
                while ((!((bool)inDream.GetValue(game)) || (bool)transitioning.GetValue(game)) && Time.realtimeSinceStartup < deadline)
                    yield return null;

                Assert.That((bool)inDream.GetValue(game), Is.True, "Dream " + (q + 1) + " did not start.");
                Assert.That((bool)transitioning.GetValue(game), Is.False, "Dream " + (q + 1) + " transition did not finish.");
                Assert.That(GameObject.Find("Dream L" + (q + 1).ToString("00")), Is.Not.Null);

                int selectedStyle = (int)style.GetValue(game);
                Assert.That(selectedStyle, Is.InRange(0, names.Length - 1));
                if (!captured[selectedStyle])
                {
                    // Даём камере и физике стабилизироваться после окончания fade.
                    yield return new WaitForSecondsRealtime(1.2f);
                    yield return null;
                    yield return null;
                    string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                    string tag = Environment.GetEnvironmentVariable("SLEEPWALKING_QA_CAPTURE_TAG") ?? "capture";
                    string path = Path.Combine(projectRoot, "Builds", "QA",
                        "dream-L" + (q + 1).ToString("00") + "-" + names[selectedStyle] + "-" + tag + ".png");
                    CaptureDreamFrame(gameCamera, path);
                    captured[selectedStyle] = true;
                    captureCount++;
                }

                completeDream.Invoke(game, null);
                deadline = Time.realtimeSinceStartup + 10f;
                while (((bool)inDream.GetValue(game) || (bool)transitioning.GetValue(game)) && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.That((bool)inDream.GetValue(game), Is.False);
                Assert.That((bool)transitioning.GetValue(game), Is.False);
            }

            Assert.That(captureCount, Is.GreaterThan(0));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
