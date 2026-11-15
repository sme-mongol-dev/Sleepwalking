using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Sleepwalking.Tests.PlayMode
{
    public sealed class SleepwalkingCycleTests
    {
        [UnityTest]
        public IEnumerator CreditsShowsDeveloperLinks()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
            yield return null;

            Transform panel = GameObject.Find("Sleepwalking UI").transform
                .Find("Sleepwalking Canvas/Credits/Credits Panel");
            Assert.That(panel, Is.Not.Null);
            Assert.That(panel.Find("Credits Body"), Is.Null);
            Assert.That(panel.Find("Developers Label").GetComponent<Text>().text, Is.EqualTo("Developers:"));

            string[] names = { "Timbuhtuk", "Mongol-dev", "Raccoon-Reed", "StuarTTdesa" };
            foreach (string name in names)
            {
                Transform link = panel.Find(name + " Link");
                Assert.That(link, Is.Not.Null, name);
                Assert.That(link.GetComponent<Text>().text, Is.EqualTo(name));
                Assert.That(link.GetComponent<Text>().raycastTarget, Is.True);
                Assert.That(link.GetComponent<Button>().interactable, Is.True);
            }
        }

        [UnityTest]
        public IEnumerator TexturedRoomHighlightsAndOpensEachMenuAction()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
            yield return null;

            GameObject room = GameObject.Find("Waking room");
            GameObject ui = GameObject.Find("Sleepwalking UI");
            Assert.That(room, Is.Not.Null);
            Assert.That(ui, Is.Not.Null);
            Camera camera = Camera.main;
            Assert.That(camera, Is.Not.Null);
            Assert.That(camera.targetTexture, Is.Null, "The menu must render directly to the screen.");
            Light light = room.transform.Find("Window Light").GetComponent<Light>();
            Assert.That(light, Is.Not.Null);
            Assert.That(light.lightmapBakeType, Is.EqualTo(LightmapBakeType.Realtime));
            Assert.That(light.shadows, Is.EqualTo(LightShadows.Soft));
            Transform window = room.transform.Find("Window");
            Assert.That(light.transform.position.y, Is.GreaterThan(window.position.y));
            foreach (Renderer renderer in room.GetComponentsInChildren<Renderer>())
                Assert.That(renderer.shadowCastingMode,
                    Is.EqualTo(renderer.transform == window ? UnityEngine.Rendering.ShadowCastingMode.Off :
                        UnityEngine.Rendering.ShadowCastingMode.TwoSided), renderer.name);
            Color32[] shadowed = CaptureRoomLighting(camera, 1280, 720);
            light.shadows = LightShadows.None;
            yield return null;
            Color32[] unshadowed = CaptureRoomLighting(camera, 1280, 720, "-NoShadows");
            light.shadows = LightShadows.Soft;
            yield return null;
            int shadowPixels = 0;
            for (int i = 0; i < shadowed.Length; i++)
                if (Mathf.Abs(shadowed[i].r - unshadowed[i].r) + Mathf.Abs(shadowed[i].g - unshadowed[i].g) +
                    Mathf.Abs(shadowed[i].b - unshadowed[i].b) > 6) shadowPixels++;
            Assert.That(shadowPixels, Is.GreaterThan(100), "Realtime shadows must visibly change the rendered room.");
            CaptureRoomLighting(camera, 960, 720);
            Transform canvas = ui.transform.Find("Sleepwalking Canvas");
            Component controller = ui.GetComponent("SleepwalkingUI");
            var closeOverlay = controller.GetType().GetMethod("CloseOverlay");
            var processRoomInput = controller.GetType().GetMethod("ProcessRoomInput", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(closeOverlay, Is.Not.Null);
            Assert.That(processRoomInput, Is.Not.Null);
            string[] props = { "Clock", "Books", "Door", "Bed" };
            string[] screens = { "Settings", "Credits", "Confirmation" };
            var properties = new MaterialPropertyBlock();
            int highlight = Shader.PropertyToID("_Highlight");
            Physics.SyncTransforms();
            for (int i = 0; i < props.Length; i++)
            {
                Transform prop = room.transform.Find(props[i]);
                Assert.That(prop, Is.Not.Null);
                var renderer = prop.GetComponent<Renderer>();
                var texture = (Texture2D)renderer.sharedMaterial.mainTexture;
                Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Point));
                Assert.That(texture.mipmapCount, Is.EqualTo(1));

                Vector2 opaque = RoomTexturePoint(camera, prop, texture, true);
                Vector2 transparent = RoomTexturePoint(camera, prop, texture, false);
                processRoomInput.Invoke(controller, new object[] { camera, transparent, false, false, false });
                renderer.GetPropertyBlock(properties);
                Assert.That(properties.GetFloat(highlight), Is.Zero, prop.name + " transparent pixel");

                processRoomInput.Invoke(controller, new object[] { camera, opaque, false, false, false });
                renderer.GetPropertyBlock(properties);
                Assert.That(properties.GetFloat(highlight), Is.GreaterThan(0f), prop.name + " hover");

                processRoomInput.Invoke(controller, new object[] { camera, opaque, true, false, false });
                if (i < screens.Length)
                {
                    Assert.That(canvas.Find(screens[i]).gameObject.activeSelf, Is.True, prop.name + " click");
                    renderer.GetPropertyBlock(properties);
                    Assert.That(properties.GetFloat(highlight), Is.Zero, prop.name + " overlay");
                    closeOverlay.Invoke(controller, null);
                }
            }

            float deadline = Time.realtimeSinceStartup + 5f;
            while (GameObject.Find("Dream L01") == null && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(GameObject.Find("Dream L01"), Is.Not.Null, "Bed click must start the first dream.");
        }

        static Vector2 RoomTexturePoint(Camera camera, Transform prop, Texture2D texture, bool opaque)
        {
            BoxCollider collider = prop.GetComponent<BoxCollider>();
            float bestDistance = float.PositiveInfinity;
            Vector2 closest = default;
            for (int y = 0; y < texture.height; y++)
            for (int x = 0; x < texture.width; x++)
            {
                Vector2 point = new Vector2((x + .5f) / texture.width - .5f, (y + .5f) / texture.height - .5f);
                Vector2 offset = point - (Vector2)collider.center;
                if (Mathf.Abs(offset.x) >= collider.size.x * .5f || Mathf.Abs(offset.y) >= collider.size.y * .5f)
                    continue;
                if ((texture.GetPixel(x, y).a >= .5f) != opaque || offset.sqrMagnitude >= bestDistance)
                    continue;
                closest = point;
                bestDistance = offset.sqrMagnitude;
            }
            Assert.That(bestDistance, Is.LessThan(float.PositiveInfinity), prop.name);
            Vector3 viewport = camera.WorldToViewportPoint(prop.TransformPoint(closest));
            return new Vector2(viewport.x * Screen.width, viewport.y * Screen.height);
        }

        static Color32[] CaptureRoomLighting(Camera camera, int width, int height, string suffix = "")
        {
            RenderTexture target = RenderTexture.GetTemporary(width, height, 24);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            float previousSize = camera.orthographicSize;
            var image = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                camera.targetTexture = target;
                camera.orthographicSize = Mathf.Max(4.15f, 7.9f * height / width);
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                string folder = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../Builds/QA"));
                System.IO.Directory.CreateDirectory(folder);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder, "MenuLighting-" + width + "x" + height + suffix + ".png"),
                    image.EncodeToPNG());
                return image.GetPixels32();
            }
            finally
            {
                camera.targetTexture = previousTarget;
                camera.orthographicSize = previousSize;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.Destroy(image);
            }
        }

        [UnityTest]
        public IEnumerator VolumeFillsStayAlignedWithTracksWhenHovered()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
            yield return null;

            GameObject ui = GameObject.Find("Sleepwalking UI");
            Assert.That(ui, Is.Not.Null);
            Transform settings = ui.transform.Find("Sleepwalking Canvas/Settings");
            Assert.That(settings, Is.Not.Null);
            settings.gameObject.SetActive(true);
            Transform panel = settings.Find("Settings Panel");
            Assert.That(panel, Is.Not.Null);
            var pointer = new PointerEventData(EventSystem.current);
            var corners = new Vector3[4];

            foreach (string channel in new[] { "Master", "Music", "SFX" })
            {
                Slider slider = panel.Find(channel + " Slider").GetComponent<Slider>();
                var track = (RectTransform)slider.transform.Find("Track");
                slider.OnPointerEnter(pointer);
                foreach (float fraction in new[] { 1f, .8f, .5f, .25f, 0f })
                {
                    slider.SetValueWithoutNotify(Mathf.Lerp(slider.minValue, slider.maxValue, fraction));
                    Canvas.ForceUpdateCanvases();
                    slider.fillRect.GetWorldCorners(corners);
                    Assert.That(track.InverseTransformPoint(corners[0]).x,
                        Is.EqualTo(track.rect.xMin).Within(.01f), channel + " fill must start at the left edge.");
                    Assert.That(track.InverseTransformPoint(corners[3]).x,
                        Is.EqualTo(Mathf.Lerp(track.rect.xMin, track.rect.xMax, fraction)).Within(.01f),
                        channel + " fill must end at the current volume.");
                }
                slider.OnPointerExit(pointer);
            }
        }

        [UnityTest]
        public IEnumerator FullCycleIncludesSevenLevelsAndWrapsToFirst()
        {
            AsyncOperation loading = SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
            Assert.That(loading, Is.Not.Null, "The integrated Sleepwalking scene must load.");
            while (!loading.isDone)
                yield return null;
            yield return null;

            GameObject gameObject = GameObject.Find("Sleepwalking Game");
            Assert.That(gameObject, Is.Not.Null, "The scene must contain Sleepwalking Game.");

            MonoBehaviour game = null;
            foreach (MonoBehaviour component in gameObject.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().FullName == "Sleepwalking.SleepwalkingGame")
                    game = component;
            Assert.That(game, Is.Not.Null, "The gameplay component must be present.");

            Type type = game.GetType();
            MethodInfo startDream = type.GetMethod("StartDream", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo completeDream = type.GetMethod("CompleteDream", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo killPlayer = type.GetMethod("KillPlayer", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo restartDream = type.GetMethod("RestartDream", BindingFlags.Instance | BindingFlags.Public);
            PropertyInfo inDream = type.GetProperty("InDream", BindingFlags.Instance | BindingFlags.Public);
            PropertyInfo completedDreams = type.GetProperty("CompletedDreams", BindingFlags.Instance | BindingFlags.Public);
            FieldInfo transitioning = type.GetField("transitioning", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo levelPrefabs = type.GetField("LevelPrefabs", BindingFlags.Instance | BindingFlags.Public);
            FieldInfo roomField = type.GetField("room", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo paintField = type.GetField("paint", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo dying = type.GetField("dying", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo uiField = type.GetField("UI", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(startDream, Is.Not.Null);
            Assert.That(completeDream, Is.Not.Null);
            Assert.That(killPlayer, Is.Not.Null);
            Assert.That(restartDream, Is.Not.Null);
            Assert.That(inDream, Is.Not.Null);
            Assert.That(completedDreams, Is.Not.Null);
            Assert.That(transitioning, Is.Not.Null);
            Assert.That(levelPrefabs, Is.Not.Null);
            Assert.That(roomField, Is.Not.Null);
            Assert.That(paintField, Is.Not.Null);
            Assert.That(dying, Is.Not.Null);
            Assert.That(uiField, Is.Not.Null);

            MonoBehaviour ui = uiField.GetValue(game) as MonoBehaviour;
            Assert.That(ui, Is.Not.Null, "The integrated scene must connect gameplay to UI.");
            Type uiType = ui.GetType();
            MethodInfo activateRoomTarget = uiType.GetMethod("ActivateRoomTarget", new[] { typeof(Component) });
            MethodInfo openPause = uiType.GetMethod("OpenPause", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo closeOverlay = uiType.GetMethod("CloseOverlay", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(activateRoomTarget, Is.Not.Null);
            Assert.That(openPause, Is.Not.Null);
            Assert.That(closeOverlay, Is.Not.Null);

            GameObject[] prefabs = (GameObject[])levelPrefabs.GetValue(game);
            type.GetField("tutorialStage", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(game, 2);
            Assert.That(prefabs, Has.Length.EqualTo(7));
            foreach (GameObject prefab in prefabs)
                Assert.That(prefab, Is.Not.Null, "All authored levels must be assigned.");

            Assert.That((bool)inDream.GetValue(game), Is.False);
            Assert.That((int)completedDreams.GetValue(game), Is.Zero);
            GameObject room = (GameObject)roomField.GetValue(game);
            Assert.That(room, Is.Not.Null);
            Assert.That(room.activeSelf, Is.True);
            MonoBehaviour environment = Array.Find(room.GetComponents<MonoBehaviour>(),
                component => component.GetType().Name == "RoomEnvironment");
            Assert.That(environment, Is.Not.Null);
            var poster = (Renderer)environment.GetType().GetField("Poster").GetValue(environment);
            var posters = (Texture2D[])environment.GetType().GetField("Posters").GetValue(environment);
            Assert.That(posters, Has.Length.EqualTo(12));
            Assert.That(posters, Is.Unique);
            Assert.That(poster, Is.Not.Null);
            var posterProperties = new MaterialPropertyBlock();
            poster.GetPropertyBlock(posterProperties);
            Texture initialPoster = posterProperties.GetTexture("_MainTex");
            Assert.That(initialPoster, Is.Not.Null);
            Assert.That(posters, Does.Contain(initialPoster));
            Assert.That(posterProperties.GetTexture("_BaseMap"), Is.SameAs(initialPoster));

            Component dreamHotspot = null;
            foreach (MonoBehaviour component in room.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null || component.GetType().FullName != "Sleepwalking.Visual.RoomHotspot" ||
                    !component.gameObject.name.Contains("Bed")) continue;
                object action = component.GetType().GetField("Action")?.GetValue(component);
                if (action?.ToString() == "Dream") dreamHotspot = component;
            }
            Assert.That(dreamHotspot, Is.Not.Null, "The waking room must have a Dream hotspot on the bed.");

            // Дополнительный вход проверяет возврат к первой геометрии после полного цикла.
            for (int q = 0; q <= prefabs.Length; q++)
            {
                if (q == 0) activateRoomTarget.Invoke(ui, new object[] { dreamHotspot });
                else startDream.Invoke(game, null);
                float deadline = Time.realtimeSinceStartup + 10f;
                while ((!((bool)inDream.GetValue(game)) || (bool)transitioning.GetValue(game)) && Time.realtimeSinceStartup < deadline)
                    yield return null;

                Assert.That((bool)inDream.GetValue(game), Is.True, "Dream " + (q + 1) + " did not start.");
                Assert.That((bool)transitioning.GetValue(game), Is.False, "Dream " + (q + 1) + " transition did not finish.");
                Assert.That((int)completedDreams.GetValue(game), Is.EqualTo(q));
                Assert.That(room.activeSelf, Is.False, "The waking room must be hidden during a dream.");
                GameObject level = GameObject.Find("Dream L" + (q % prefabs.Length + 1).ToString("00"));
                Assert.That(level, Is.Not.Null,
                    "Dream " + (q + 1) + " must instantiate its authored level, not fallback geometry.");
                AssertTilesMatchColliders(level);

                if (q == 0)
                {
                    // Пауза проверяется после перехода, когда UI уже находится в режиме сна.
                    float runningTimeScale = Time.timeScale;
                    Assert.That(runningTimeScale, Is.GreaterThan(0f));
                    openPause.Invoke(ui, null);
                    yield return null;
                    Assert.That(Time.timeScale, Is.Zero, "OpenPause must stop game time.");
                    closeOverlay.Invoke(ui, null);
                    Assert.That(Time.timeScale, Is.EqualTo(runningTimeScale), "CloseOverlay must restore game time.");

                    // Смерть и ручной повтор должны оставить краску в том же сне.
                    GameObject firstLevel = GameObject.Find("Dream L01");
                    GameObject paint = (GameObject)paintField.GetValue(game);
                    GameObject dreamer = GameObject.Find("Dreamer");
                    Assert.That(paint, Is.Not.Null);
                    Assert.That(dreamer, Is.Not.Null);
                    Assert.That(paint.transform.childCount, Is.Zero);

                    Vector3 position = dreamer.transform.position;
                    killPlayer.Invoke(game, new object[] { new Vector2(position.x, position.y) });
                    Assert.That((bool)dying.GetValue(game), Is.True, "KillPlayer must begin the death coroutine.");
                    deadline = Time.realtimeSinceStartup + 5f;
                    while ((bool)dying.GetValue(game) && Time.realtimeSinceStartup < deadline)
                        yield return null;

                    Assert.That((bool)dying.GetValue(game), Is.False, "The death and retry coroutine did not finish.");
                    Assert.That((bool)inDream.GetValue(game), Is.True);
                    Assert.That((int)completedDreams.GetValue(game), Is.Zero);
                    Assert.That(GameObject.Find("Dream L01"), Is.SameAs(firstLevel));
                    Assert.That(paint.transform.childCount, Is.GreaterThan(0), "Death must leave a paint mark.");

                    int marks = paint.transform.childCount;
                    restartDream.Invoke(game, null);
                    yield return null;
                    Assert.That(GameObject.Find("Dream L01"), Is.SameAs(firstLevel));
                    Assert.That(paintField.GetValue(game), Is.SameAs(paint));
                    Assert.That(paint.transform.childCount, Is.EqualTo(marks), "Restart must preserve paint in the current dream.");
                    Assert.That((int)completedDreams.GetValue(game), Is.Zero);
                }

                completeDream.Invoke(game, null);
                deadline = Time.realtimeSinceStartup + 10f;
                while (((bool)inDream.GetValue(game) || (bool)transitioning.GetValue(game)) && Time.realtimeSinceStartup < deadline)
                    yield return null;

                Assert.That((bool)inDream.GetValue(game), Is.False, "Dream " + (q + 1) + " did not return to the room.");
                Assert.That((bool)transitioning.GetValue(game), Is.False, "Wake " + (q + 1) + " transition did not finish.");
                Assert.That((int)completedDreams.GetValue(game), Is.EqualTo(q + 1));
                Assert.That(room.activeSelf, Is.True, "The waking room must return after dream " + (q + 1) + ".");
                AssertPosterMatchesOdradek(environment, poster);
                Assert.That(GameObject.Find("Dream L" + (q % prefabs.Length + 1).ToString("00")), Is.Null,
                    "The completed dream level must be removed.");
                if (q == 0)
                {
                    Assert.That(paintField.GetValue(game), Is.Null, "Leaving the dream must clear paint state.");
                    Assert.That(GameObject.Find("Paint marks (current dream)"), Is.Null,
                        "Leaving the dream must destroy its paint marks.");
                }
            }

            LogAssert.NoUnexpectedReceived();
        }

        static void AssertPosterMatchesOdradek(MonoBehaviour environment, Renderer poster)
        {
            int value = (int)environment.GetType().GetProperty("CurrentOdradek").GetValue(environment);
            IList decorations = (IList)environment.GetType().GetField("Decorations").GetValue(environment);
            Texture expected = null;
            int selected = 0;
            foreach (object row in decorations)
            {
                Type type = row.GetType();
                if ((string)type.GetField("Group").GetValue(row) != "poster") continue;
                int minimum = (int)type.GetField("MinOdradek").GetValue(row);
                int maximum = (int)type.GetField("MaxOdradek").GetValue(row);
                if (value < minimum || value > maximum) continue;
                expected = (Texture)type.GetField("Texture").GetValue(row);
                selected++;
            }
            Assert.That(selected, Is.EqualTo(1), "One poster must be selected at Odradek " + value);
            var properties = new MaterialPropertyBlock();
            poster.GetPropertyBlock(properties);
            Assert.That(properties.GetTexture("_MainTex"), Is.SameAs(expected));
            Assert.That(properties.GetTexture("_BaseMap"), Is.SameAs(expected));
        }

        static void AssertTilesMatchColliders(GameObject level)
        {
            Transform matrix = level.transform.Find("Dream Tile Matrix");
            Assert.That(matrix, Is.Not.Null, level.name + " static tile matrix");
            MeshFilter staticMesh = matrix.GetComponent<MeshFilter>();
            MeshRenderer staticArt = matrix.GetComponent<MeshRenderer>();
            Assert.That(staticMesh?.sharedMesh, Is.Not.Null, level.name + " static tile mesh");
            Assert.That(staticMesh.sharedMesh.vertexCount, Is.GreaterThan(0), level.name + " static tiles");
            Assert.That(staticArt, Is.Not.Null, level.name + " static tile art");
            foreach (Transform child in level.GetComponentsInChildren<Transform>(true))
            {
                if (child.name != "Dream Tiles") continue;
                Collider2D collider = child.parent.GetComponent<Collider2D>();
                string platform = level.name + "/" + child.parent.name;
                Assert.That(collider, Is.Not.Null, platform);
                Assert.That(collider.isTrigger, Is.False, platform);

                MeshRenderer[] tiles = child.GetComponentsInChildren<MeshRenderer>(true);
                Assert.That(tiles, Is.Not.Empty, platform);
                Bounds visual = tiles[0].bounds;
                foreach (MeshRenderer tile in tiles)
                    visual.Encapsulate(tile.bounds);

                // Timed platforms can be inactive, so compare with their authored shape.
                Vector2 localMin = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                Vector2 localMax = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
                if (collider is BoxCollider2D box)
                {
                    localMin = box.offset - box.size * .5f;
                    localMax = box.offset + box.size * .5f;
                }
                else if (collider is PolygonCollider2D polygon)
                {
                    for (int q = 0; q < polygon.pathCount; q++)
                        foreach (Vector2 point in polygon.GetPath(q))
                        {
                            localMin = Vector2.Min(localMin, point + polygon.offset);
                            localMax = Vector2.Max(localMax, point + polygon.offset);
                        }
                }
                else Assert.Fail(platform + " has unsupported solid collider");
                Vector2 center = child.parent.TransformPoint((localMin + localMax) * .5f);
                Vector2 size = Vector2.Scale(localMax - localMin, child.parent.lossyScale);
                Assert.That(visual.center.x, Is.EqualTo(center.x).Within(.001f), platform);
                Assert.That(visual.center.y, Is.EqualTo(center.y).Within(.001f), platform);
                Assert.That(visual.size.x, Is.EqualTo(size.x).Within(.001f), platform);
                Assert.That(visual.size.y, Is.EqualTo(size.y).Within(.001f), platform);
            }
        }
    }
}
