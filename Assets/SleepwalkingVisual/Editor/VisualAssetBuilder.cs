using System.IO;
using Sleepwalking.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sleepwalking.Visual.Editor
{
    public static class VisualAssetBuilder
    {
        const string Root = "Assets/SleepwalkingVisual";
        const string IllustratedScene = Root + "/Preview/IllustratedMenuPreview.unity";
        const string IllustratedRoomScene = Root + "/Preview/RoomPreviewIllustrated.unity";
        const string L07FarScene = Root + "/Preview/L07FarMeatPreview.unity";
        const string L01MeatPilotScene = Root + "/Preview/L01MeatTexturePilot.unity";
        const string L01TilesPreviewScene = Root + "/Preview/L01TilesPlayerPreview.unity";
        const string DreamLevelPreviewScene = Root + "/Preview/DreamLevelPlayerPreview.unity";
        const string TileShowcaseScene = Root + "/Preview/TileShowcase.unity";
        const string TilesetRoot = Root + "/Textures/Tilesets";
        static Material background, panel, frame, shadow, text, secondary, accent, hover, pressed, blue, heaven;

        [MenuItem("Sleepwalking/Build Visual Assets")]
        public static void Build()
        {
            Directory.CreateDirectory(Root + "/Materials");
            Directory.CreateDirectory(Root + "/Prefabs/Themes");
            Directory.CreateDirectory(Root + "/Preview");
            ImportDreamTilesets();
            BuildTemporaryDreamerSprite();
            LoadMaterials();
            PreserveFlatThemes();
            BuildThemes();
            BuildRoom();
            BuildIllustratedVariant();
            BuildIllustratedRoomPreview();
            AssetDatabase.SaveAssets();
            Debug.Log("Visual assets ready: room, illustrated variant, preview scene, four dream themes.");
        }

        [MenuItem("Sleepwalking/Build Dream Themes")]
        public static void BuildDreamThemesOnly()
        {
            Directory.CreateDirectory(Root + "/Materials");
            Directory.CreateDirectory(Root + "/Prefabs/Themes");
            ImportDreamTilesets();
            BuildTemporaryDreamerSprite();
            LoadMaterials();
            PreserveFlatThemes();
            BuildThemes();
            AssetDatabase.SaveAssets();
            Debug.Log("Four dream themes ready.");
        }

        [MenuItem("Sleepwalking/Build Room Composition")]
        public static void BuildRoomCompositionOnly()
        {
            Directory.CreateDirectory(Root + "/Materials");
            Directory.CreateDirectory(Root + "/Prefabs");
            Directory.CreateDirectory(Root + "/Preview");
            LoadMaterials();
            BuildRoom();
            BuildIllustratedVariant();
            BuildIllustratedRoomPreview();
            AssetDatabase.SaveAssets();
            Debug.Log("Room composition and illustrated variant ready.");
        }

        [MenuItem("Sleepwalking/Import Dream Tilesets")]
        public static void ImportDreamTilesets()
        {
            string[] names = { "Meat", "PostApoc", "DeepWater", "Heaven" };
            foreach (string name in names)
            {
                string path = TilesetRoot + "/TL_" + name + "_Dream.png";
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new System.InvalidOperationException("Dream tileset missing: " + path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit = 32;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.alphaIsTransparency = true;
                importer.maxTextureSize = 256;
                SpriteMetaData[] tiles = new SpriteMetaData[18];
                for (int q = 0; q < 6; q++)
                for (int e = 0; e < 3; e++)
                {
                    int index = q * 3 + e;
                    tiles[index] = new SpriteMetaData
                    {
                        name = name + "_r" + q + "c" + e,
                        rect = new Rect(e * 32, (5 - q) * 32, 32, 32),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(.5f, .5f)
                    };
                }
                importer.spritesheet = tiles;
                importer.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Four original dream tilesets imported as 32-pixel tiles.");
        }

        static void BuildTemporaryDreamerSprite()
        {
            // Замещаемый силуэт нужен лишь до выбора окончательной внешности героя.
            const string path = Root + "/Textures/Processed/Dreamer_TempSilhouette.png";
            Directory.CreateDirectory(Root + "/Textures/Processed");
            Texture2D image = new Texture2D(16, 24, TextureFormat.RGBA32, false);
            Color transparent = new Color(0, 0, 0, 0);
            for (int q = 0; q < image.width; q++)
            for (int e = 0; e < image.height; e++) image.SetPixel(q, e, transparent);
            Color outline = ColorFromHex("161318");
            Color hood = ColorFromHex("229EDE");
            Color coat = ColorFromHex("222036");
            Color face = ColorFromHex("F8D4B8");
            DrawPixelRect(image, 4, 1, 7, 3, outline);
            DrawPixelRect(image, 9, 1, 12, 3, outline);
            DrawPixelOval(image, 8, 8, 6, 7, outline);
            DrawPixelOval(image, 8, 8, 5, 6, coat);
            DrawPixelRect(image, 4, 7, 5, 11, hood);
            DrawPixelRect(image, 11, 7, 12, 11, hood);
            DrawPixelOval(image, 8, 17, 6, 6, outline);
            DrawPixelOval(image, 8, 17, 5, 5, hood);
            DrawPixelOval(image, 9, 16, 3, 3, outline);
            DrawPixelOval(image, 9, 16, 2, 2, face);
            image.SetPixel(10, 16, outline);
            image.SetPixel(3, 21, hood);
            image.SetPixel(4, 22, outline);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            Object.DestroyImmediate(image);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new System.InvalidOperationException("Temporary dreamer sprite import failed.");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 24;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        static void DrawPixelRect(Texture2D image, int left, int bottom, int right, int top, Color color)
        {
            for (int q = left; q <= right; q++)
            for (int e = bottom; e <= top; e++) image.SetPixel(q, e, color);
        }

        static void DrawPixelOval(Texture2D image, int cx, int cy, int rx, int ry, Color color)
        {
            for (int q = cx - rx; q <= cx + rx; q++)
            for (int e = cy - ry; e <= cy + ry; e++)
                if ((q - cx) * (q - cx) * ry * ry + (e - cy) * (e - cy) * rx * rx <= rx * rx * ry * ry)
                    image.SetPixel(q, e, color);
        }

        [MenuItem("Sleepwalking/Build Illustrated Menu Preview Player")]
        public static void BuildIllustratedPreviewPlayer()
        {
            // Копия общей сцены нужна только для сравнения вариантов; исходную сцену и build settings не меняем.
            const string sharedScene = "Assets/Scenes/Sleepwalking.unity";
            Scene scene = EditorSceneManager.OpenScene(sharedScene, OpenSceneMode.Single);
            Sleepwalking.SleepwalkingGame game = Object.FindFirstObjectByType<Sleepwalking.SleepwalkingGame>();
            GameObject variant = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/SleepwalkingRoom_IllustratedHero.prefab");
            if (game == null || variant == null) throw new System.InvalidOperationException("Integrated scene or illustrated room prefab is missing.");
            game.RoomPrefab = variant;
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, IllustratedScene, true))
                throw new System.InvalidOperationException("Could not save illustrated preview scene.");
            Directory.CreateDirectory("Builds/VisualPreview");
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { IllustratedScene },
                locationPathName = "Builds/VisualPreview/SleepwalkingIllustrated.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log("Illustrated preview build: " + report.summary.result + " " + report.summary.totalErrors + " errors");
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.InvalidOperationException("Illustrated preview Player failed.");
        }

        [MenuItem("Sleepwalking/Build L01 Tiles Player Preview")]
        public static void BuildL01TilesPreviewPlayer()
        {
            // Копия общей сцены запускает настоящий первый сон, не меняя игровой Player.
            const string sharedScene = "Assets/Scenes/Sleepwalking.unity";
            Scene scene = EditorSceneManager.OpenScene(sharedScene, OpenSceneMode.Single);
            if (Object.FindFirstObjectByType<Sleepwalking.SleepwalkingGame>() == null)
                throw new System.InvalidOperationException("Integrated scene is missing SleepwalkingGame.");
            new GameObject("Auto Dream Preview").AddComponent<AutoDreamPreview>();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, L01TilesPreviewScene, true))
                throw new System.InvalidOperationException("Could not save L01 tiles preview scene.");
            Directory.CreateDirectory("Builds/VisualPreview");
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { L01TilesPreviewScene },
                locationPathName = "Builds/VisualPreview/L01TilesPlayerPreview.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log("L01 tiles Player preview build: " + report.summary.result + " " + report.summary.totalErrors + " errors");
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.InvalidOperationException("L01 tiles Player preview failed.");
        }

        [MenuItem("Sleepwalking/Build Dream Level Preview Player")]
        public static void BuildDreamLevelPreviewPlayer()
        {
            // Изолированный Player использует общую сцену и принимает номер уровня через argv.
            const string sharedScene = "Assets/Scenes/Sleepwalking.unity";
            Scene scene = EditorSceneManager.OpenScene(sharedScene, OpenSceneMode.Single);
            if (Object.FindFirstObjectByType<Sleepwalking.SleepwalkingGame>() == null)
                throw new System.InvalidOperationException("Integrated scene is missing SleepwalkingGame.");
            new GameObject("Auto Dream Preview").AddComponent<AutoDreamPreview>();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, DreamLevelPreviewScene, true))
                throw new System.InvalidOperationException("Could not save dream level preview scene.");
            Directory.CreateDirectory("Builds/VisualPreview");
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { DreamLevelPreviewScene },
                locationPathName = "Builds/VisualPreview/DreamLevelPreview.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log("Dream level preview build: " + report.summary.result + " " + report.summary.totalErrors + " errors");
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.InvalidOperationException("Dream level preview failed.");
        }

        [MenuItem("Sleepwalking/Build Tile Showcase Player")]
        public static void BuildTileShowcasePlayer()
        {
            // Один отдельный Player принимает номер уровня и темы через аргументы запуска.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("Tile Showcase", typeof(TileShowcasePreview));
            TileShowcasePreview showcase = root.GetComponent<TileShowcasePreview>();
            showcase.Levels = new GameObject[7];
            showcase.Themes = new GameObject[4];
            for (int q = 0; q < 7; q++)
                showcase.Levels[q] = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/SleepwalkingLevels/Prefabs/L" + (q + 1).ToString("00") + "_DreamGeometry.prefab");
            string[] names = { "Meat", "PostApocalypse", "DeepWater", "Heaven" };
            for (int q = 0; q < names.Length; q++)
                showcase.Themes[q] = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Themes/" + names[q] + ".prefab");
            foreach (GameObject prefab in showcase.Levels) if (prefab == null) throw new System.InvalidOperationException("Showcase level missing.");
            foreach (GameObject prefab in showcase.Themes) if (prefab == null) throw new System.InvalidOperationException("Showcase theme missing.");
            GameObject cameraObject = new GameObject("Tile Showcase Pixel Camera", typeof(Camera), typeof(PixelRoomView));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = ColorFromHex("161318");
            showcase.PreviewCamera = camera;
            if (!EditorSceneManager.SaveScene(scene, TileShowcaseScene)) throw new System.InvalidOperationException("Could not save tile showcase scene.");
            Directory.CreateDirectory("Builds/VisualPreview");
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { TileShowcaseScene },
                locationPathName = "Builds/VisualPreview/TileShowcase.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log("Tile showcase build: " + report.summary.result + " " + report.summary.totalErrors + " errors");
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.InvalidOperationException("Tile showcase Player failed.");
        }

        [MenuItem("Sleepwalking/Build L07 Far Theme Preview Player")]
        public static void BuildL07FarThemePreviewPlayer()
        {
            // Проверяем дальний край маршрута как отдельную сцену без правок игрового уровня.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject levelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SleepwalkingLevels/Prefabs/L07_DreamGeometry.prefab");
            GameObject themePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Themes/Meat.prefab");
            if (levelPrefab == null || themePrefab == null) throw new System.InvalidOperationException("L07 or Meat theme prefab is missing.");
            GameObject level = PrefabUtility.InstantiatePrefab(levelPrefab, scene) as GameObject;
            GameObject theme = PrefabUtility.InstantiatePrefab(themePrefab, scene) as GameObject;
            if (level == null || theme == null) throw new System.InvalidOperationException("Could not instantiate L07 far preview.");
            DreamTheme style = theme.GetComponent<DreamTheme>();
            Transform geometry = level.transform.Find("Geometry");
            if (style == null || geometry == null) throw new System.InvalidOperationException("L07 geometry or dream theme component is missing.");
            style.ApplyTo(geometry);
            GameObject cameraObject = new GameObject("L07 Far Pixel Camera", typeof(Camera), typeof(PixelRoomView));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(21.1f, 1.5f, -20f);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = ColorFromHex("161318");
            if (!EditorSceneManager.SaveScene(scene, L07FarScene))
                throw new System.InvalidOperationException("Could not save L07 far preview scene.");
            Directory.CreateDirectory("Builds/VisualPreview");
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { L07FarScene },
                locationPathName = "Builds/VisualPreview/L07FarMeat.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log("L07 far theme preview build: " + report.summary.result + " " + report.summary.totalErrors + " errors");
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.InvalidOperationException("L07 far theme preview Player failed.");
        }

        [MenuItem("Sleepwalking/Build Meat Texture Pilot")]
        public static void BuildMeatTexturePilot()
        {
            // Отдельный prefab оставляет плоскую Meat-тему доступной для сравнения.
            const string texturePath = Root + "/Textures/Processed/MeatDream_UIPalette.png";
            GameObject flat = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Themes/Meat.prefab");
            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (flat == null || importer == null) throw new System.InvalidOperationException("Meat pilot input is missing.");
            importer.textureType = TextureImporterType.Default;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            Shader shader = Shader.Find("Sleepwalking/PixelCutout");
            if (atlas == null || shader == null) throw new System.InvalidOperationException("Meat pilot texture or shader failed to import.");
            Directory.CreateDirectory(Root + "/Materials");
            Material leftRib = AtlasMaterial("MeatDream_RibLeft", shader, atlas, new Vector2(1f / 3f, .5f), Vector2.zero);
            Material centerRib = AtlasMaterial("MeatDream_RibCenter", shader, atlas, new Vector2(1f / 3f, .5f), new Vector2(1f / 3f, 0));
            GameObject root = PrefabUtility.InstantiatePrefab(flat) as GameObject;
            if (root == null) throw new System.InvalidOperationException("Could not instantiate flat Meat theme.");
            root.name = "Meat Dream Texture Pilot";
            Transform tissue = Group(root.transform, "Tissue Cutouts - editable accents", Vector3.zero).transform;
            // Два вырезанных фрагмента ставим по краям стартового кадра, оставляя маршрут пустым.
            TexturedQuad(tissue, "Left Sinew", new Vector3(-2.7f, 2.4f, 3.25f), new Vector3(.85f, 2.55f, 1f), leftRib);
            TexturedQuad(tissue, "Right Sinew", new Vector3(8.7f, 2f, 3.25f), new Vector3(.85f, 2.55f, 1f), centerRib);
            PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/Themes/MeatTexturePilot.prefab");
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log("Meat texture pilot ready: two editable cutouts, no new colliders.");
        }

        [MenuItem("Sleepwalking/Build Meat Texture Pilot Preview")]
        public static void BuildMeatTexturePilotPreview()
        {
            BuildMeatTexturePilot();
            BuildL01MeatPilotPreviewPlayer();
        }

        [MenuItem("Sleepwalking/Build L01 Meat Texture Pilot Preview Player")]
        public static void BuildL01MeatPilotPreviewPlayer()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject levelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SleepwalkingLevels/Prefabs/L01_DreamGeometry.prefab");
            GameObject themePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Themes/MeatTexturePilot.prefab");
            if (levelPrefab == null || themePrefab == null) throw new System.InvalidOperationException("L01 or pilot theme prefab is missing.");
            GameObject level = PrefabUtility.InstantiatePrefab(levelPrefab, scene) as GameObject;
            GameObject theme = PrefabUtility.InstantiatePrefab(themePrefab, scene) as GameObject;
            if (level == null || theme == null) throw new System.InvalidOperationException("Could not instantiate pilot preview.");
            DreamTheme style = theme.GetComponent<DreamTheme>();
            Transform geometry = level.transform.Find("Geometry");
            if (style == null || geometry == null) throw new System.InvalidOperationException("L01 geometry or pilot theme component is missing.");
            style.ApplyTo(geometry);
            GameObject cameraObject = new GameObject("L01 Pilot Pixel Camera", typeof(Camera), typeof(PixelRoomView));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(3f, 1.8f, -20f);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = ColorFromHex("161318");
            if (!EditorSceneManager.SaveScene(scene, L01MeatPilotScene))
                throw new System.InvalidOperationException("Could not save L01 pilot preview scene.");
            Directory.CreateDirectory("Builds/VisualPreview");
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { L01MeatPilotScene },
                locationPathName = "Builds/VisualPreview/L01MeatTexturePilot.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log("L01 Meat pilot preview build: " + report.summary.result + " " + report.summary.totalErrors + " errors");
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.InvalidOperationException("L01 Meat pilot preview Player failed.");
        }

        static Material AtlasMaterial(string name, Shader shader, Texture2D atlas, Vector2 scale, Vector2 offset)
        {
            string path = Root + "/Materials/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.mainTexture = atlas;
            material.mainTextureScale = scale;
            material.mainTextureOffset = offset;
            EditorUtility.SetDirty(material);
            return material;
        }

        static void TexturedQuad(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            quad.transform.SetParent(parent, false);
            quad.transform.localPosition = position;
            quad.transform.localScale = scale;
            quad.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(quad.GetComponent<Collider>());
        }

        static Material Material(string name, string hex)
        {
            string path = Root + "/Materials/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            ColorUtility.TryParseHtmlString("#" + hex, out Color color);
            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        static void LoadMaterials()
        {
            background = Material("Background_161318", "161318");
            panel = Material("Panel_222036", "222036");
            frame = Material("Frame_8C5050", "8C5050");
            shadow = Material("Shadow_3C2030", "3C2030");
            text = Material("Text_F8D4B8", "F8D4B8");
            secondary = Material("Secondary_B4B8D4", "B4B8D4");
            accent = Material("Accent_B4202A", "B4202A");
            hover = Material("Hover_E04020", "E04020");
            pressed = Material("Pressed_6E1830", "6E1830");
            blue = Material("GlitchBlue_229EDE", "229EDE");
            heaven = Material("Heaven_90DCC0", "90DCC0");
        }

        static GameObject Group(Transform parent, string name, Vector3 position)
        {
            GameObject node = new GameObject(name);
            node.transform.SetParent(parent, false);
            node.transform.localPosition = position;
            return node;
        }

        static GameObject Shape(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            GameObject node = GameObject.CreatePrimitive(type);
            node.name = name;
            node.transform.SetParent(parent, false);
            node.transform.localPosition = position;
            node.transform.localScale = scale;
            node.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(node.GetComponent<Collider>());
            return node;
        }

        static GameObject Box(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            return Shape(parent, name, PrimitiveType.Cube, position, scale, material);
        }

        static GameObject Oval(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            return Shape(parent, name, PrimitiveType.Sphere, position, scale, material);
        }

        static void Hotspot(GameObject node, RoomAction action, string label, Vector3 center, Vector3 size)
        {
            BoxCollider collider = node.AddComponent<BoxCollider>();
            collider.center = center;
            collider.size = size;
            RoomHotspot hotspot = node.AddComponent<RoomHotspot>();
            hotspot.Action = action;
            hotspot.Label = label;
        }

        static void BuildRoom()
        {
            GameObject root = new GameObject("Sleepwalking Room");
            RoomEnvironment environment = root.AddComponent<RoomEnvironment>();

            Transform shell = Group(root.transform, "Room Shell - editable pieces", Vector3.zero).transform;
            Box(shell, "Floor", new Vector3(0, -.98f, .65f), new Vector3(15.2f, .24f, 7f), panel);
            for (int q = 0; q < 13; q++)
                Box(shell, "Floorboard " + (q + 1), new Vector3(-7.2f + q * 1.2f, -.845f, .65f), new Vector3(.025f, .02f, 6.9f), shadow);
            Box(shell, "Back Wall", new Vector3(0, 2.15f, 3.43f), new Vector3(15.2f, 6.2f, .2f), panel);
            Box(shell, "Low Wall Shadow", new Vector3(0, .22f, 3.29f), new Vector3(15f, 1.3f, .025f), shadow);
            for (int q = 0; q < 7; q++)
                Box(shell, "Wall Panel Seam " + q, new Vector3(-6.2f + q * 2.05f, .2f, 3.24f), new Vector3(.035f, 1.2f, .035f), frame);
            Box(shell, "Picture Rail", new Vector3(0, 4.9f, 3.21f), new Vector3(15.1f, .08f, .19f), shadow);
            Box(shell, "Upper Left Vignette", new Vector3(-6.72f, 4.06f, 3.16f), new Vector3(1.42f, 2.12f, .02f), background);
            Box(shell, "Upper Right Vignette", new Vector3(7.2f, 4.08f, 3.16f), new Vector3(.54f, 2.04f, .02f), background);
            Box(shell, "Left Wall", new Vector3(-7.55f, 1.55f, .55f), new Vector3(.2f, 5f, 5.8f), shadow);
            Box(shell, "Right Wall", new Vector3(7.55f, 1.55f, .55f), new Vector3(.2f, 5f, 5.8f), shadow);
            Box(shell, "Ceiling Beam", new Vector3(0, 5.12f, 3.23f), new Vector3(15.2f, .24f, .3f), frame);
            Box(shell, "Wall Trim", new Vector3(0, -.52f, 3.29f), new Vector3(15f, .14f, .15f), frame);
            int windowStart = shell.childCount;
            Box(shell, "Window Outer Shadow", new Vector3(4.55f, 3f, 3.2f), new Vector3(3.08f, 3.22f, .13f), shadow);
            Box(shell, "Window Niche", new Vector3(4.5f, 3.05f, 3.06f), new Vector3(2.45f, 2.75f, .24f), frame);
            Box(shell, "Window Glass", new Vector3(4.5f, 3.05f, 2.82f), new Vector3(2.12f, 2.43f, .08f), background);
            Oval(shell, "Moon Halo", new Vector3(4.2f, 3.72f, 2.72f), new Vector3(.78f, .78f, .07f), panel);
            Oval(shell, "Moon", new Vector3(4.2f, 3.72f, 2.63f), new Vector3(.52f, .52f, .08f), text);
            Box(shell, "Distant Rooftop Left", new Vector3(3.87f, 2.16f, 2.61f), new Vector3(.86f, .6f, .06f), panel);
            Box(shell, "Distant Rooftop Right", new Vector3(5.05f, 2.02f, 2.61f), new Vector3(.98f, .42f, .06f), panel);
            for (int q = 0; q < 8; q++)
                Box(shell, "Night Star " + q, new Vector3(3.62f + (q * 7 % 17) * .12f, 2.62f + (q * 5 % 13) * .13f, 2.53f), new Vector3(.035f, .035f, .02f), secondary);
            Box(shell, "Window Cross Vertical", new Vector3(4.5f, 3.05f, 2.42f), new Vector3(.12f, 2.55f, .19f), frame);
            Box(shell, "Window Sill", new Vector3(4.5f, 1.64f, 2.42f), new Vector3(2.9f, .15f, .46f), shadow);
            Box(shell, "Window Sill Top", new Vector3(4.5f, 1.74f, 2.39f), new Vector3(2.9f, .07f, .52f), frame);
            GameObject star = Box(shell, "Impossible Star - appears after sleep", new Vector3(5.25f, 3.65f, 2.49f), new Vector3(.15f, .15f, .04f), text);
            environment.ExtraStar = star;
            star.SetActive(false);
            GameObject curtain = Box(shell, "Curtain Left", new Vector3(3.22f, 3.05f, 2.3f), new Vector3(.34f, 2.7f, .2f), shadow);
            environment.Curtain = curtain.transform;
            Box(shell, "Curtain Left Fold", new Vector3(3.34f, 3.05f, 2.17f), new Vector3(.12f, 2.54f, .1f), panel);
            Box(shell, "Curtain Left Edge", new Vector3(3.04f, 3.05f, 2.16f), new Vector3(.05f, 2.64f, .07f), secondary);
            Box(shell, "Curtain Right", new Vector3(5.77f, 3.05f, 2.3f), new Vector3(.34f, 2.7f, .2f), shadow);
            Box(shell, "Curtain Right Fold", new Vector3(5.67f, 3.05f, 2.17f), new Vector3(.12f, 2.54f, .1f), panel);
            Box(shell, "Curtain Right Edge", new Vector3(5.96f, 3.05f, 2.16f), new Vector3(.05f, 2.64f, .07f), secondary);
            Box(shell, "Curtain Rod", new Vector3(4.5f, 4.51f, 2.12f), new Vector3(2.9f, .09f, .12f), frame);
            Box(shell, "Curtain Tie Left", new Vector3(3.21f, 2.93f, 2.06f), new Vector3(.4f, .07f, .12f), frame);
            Box(shell, "Curtain Tie Right", new Vector3(5.78f, 2.93f, 2.06f), new Vector3(.4f, .07f, .12f), frame);
            // Освобождаем правую часть стены под дверь, не меняя устройство окна и его анимацию.
            int windowCount = shell.childCount - windowStart;
            Transform window = Group(shell, "Moon Window", new Vector3(-.9f, 0, 0)).transform;
            for (int q = 0; q < windowCount; q++) shell.GetChild(windowStart).SetParent(window, false);

            GameObject bed = Group(root.transform, "Bed - DREAM hotspot", new Vector3(-.45f, -.25f, -.15f));
            Box(bed.transform, "Bed Cast Shadow", new Vector3(.15f, -.68f, -.38f), new Vector3(10.8f, .04f, 2.9f), background);
            Box(bed.transform, "Bed Frame", new Vector3(0, -.1f, 0), new Vector3(10.2f, .43f, 2.25f), shadow);
            Box(bed.transform, "Wooden Front Rail", new Vector3(0, -.52f, -1.19f), new Vector3(10.1f, .67f, .2f), frame);
            Box(bed.transform, "Front Rail Lower Shadow", new Vector3(0, -.76f, -1.31f), new Vector3(10f, .11f, .04f), shadow);
            Box(bed.transform, "Front Rail Top Glint", new Vector3(0, -.18f, -1.32f), new Vector3(10f, .06f, .04f), secondary);
            Box(bed.transform, "Mattress Side", new Vector3(0, .13f, -1.08f), new Vector3(9.78f, .36f, .19f), secondary);
            Box(bed.transform, "Mattress Top", new Vector3(0, .31f, -.04f), new Vector3(9.8f, .1f, 2.05f), secondary);
            Oval(bed.transform, "Pillow Cast Shadow", new Vector3(-3.72f, .39f, -.18f), new Vector3(1.8f, .27f, 1.48f), frame);
            Oval(bed.transform, "Pillow Puffed Body", new Vector3(-3.72f, .53f, -.2f), new Vector3(1.7f, .28f, 1.42f), secondary);
            Box(bed.transform, "Pillow Seam", new Vector3(-4.42f, .47f, -.85f), new Vector3(.13f, .05f, .13f), secondary);
            Oval(bed.transform, "Quilt Silhouette", new Vector3(1.35f, .51f, -.12f), new Vector3(6.63f, .64f, 2.18f), frame);
            Oval(bed.transform, "Quilt Knee Hump", new Vector3(1.5f, .75f, .06f), new Vector3(2.5f, .72f, 1.73f), panel);
            Oval(bed.transform, "Quilt Foot Hump", new Vector3(3.47f, .55f, -.02f), new Vector3(2.15f, .5f, 1.92f), frame);
            Box(bed.transform, "Moonlit Quilt Fold Left", new Vector3(-.68f, .71f, -1.12f), new Vector3(.62f, .07f, .06f), secondary);
            Box(bed.transform, "Moonlit Quilt Fold Middle", new Vector3(1.53f, .86f, -1.01f), new Vector3(.44f, .065f, .06f), secondary);
            Box(bed.transform, "Moonlit Quilt Fold Right", new Vector3(3.64f, .69f, -1.1f), new Vector3(.7f, .055f, .06f), secondary);
            Box(bed.transform, "Quilt Front Drape", new Vector3(1.35f, .24f, -1.18f), new Vector3(6.25f, .23f, .14f), panel);
            Box(bed.transform, "Quilt Front Hem", new Vector3(1.35f, .1f, -1.27f), new Vector3(6.25f, .045f, .06f), shadow);
            Box(bed.transform, "Quilt Turned Edge", new Vector3(-1.9f, .62f, -.14f), new Vector3(.2f, .16f, 2.05f), secondary);
            Box(bed.transform, "Quilt Fold One", new Vector3(.52f, .83f, -.92f), new Vector3(1.12f, .08f, .12f), frame);
            Box(bed.transform, "Quilt Fold Two", new Vector3(2.42f, .88f, -.88f), new Vector3(.78f, .06f, .13f), secondary);
            Box(bed.transform, "Headboard", new Vector3(-5.22f, .43f, 0), new Vector3(.23f, 1.55f, 2.45f), shadow);
            Box(bed.transform, "Headboard Inset", new Vector3(-5.34f, .53f, -.06f), new Vector3(.05f, 1.08f, 1.98f), frame);
            Box(bed.transform, "Footboard", new Vector3(5.22f, .12f, 0), new Vector3(.23f, 1.1f, 2.45f), shadow);
            Box(bed.transform, "Footboard Inset", new Vector3(5.1f, .18f, -.06f), new Vector3(.05f, .69f, 1.93f), frame);
            for (int q = -1; q <= 1; q += 2)
            {
                Box(bed.transform, "Leg " + q + " front", new Vector3(q * 4.9f, -.51f, -1f), new Vector3(.2f, .6f, .2f), shadow);
                Box(bed.transform, "Leg " + q + " back", new Vector3(q * 4.9f, -.51f, 1f), new Vector3(.2f, .6f, .2f), shadow);
            }
            for (int q = -1; q <= 1; q += 2)
            {
                Box(bed.transform, "Tall Front Bedpost " + q, new Vector3(q * 5.14f, .18f, -1.2f), new Vector3(.36f, 1.43f, .35f), shadow);
                Oval(bed.transform, "Bedpost Finial " + q, new Vector3(q * 5.14f, 1f, -1.2f), new Vector3(.42f, .42f, .42f), frame);
                Box(bed.transform, "Bedpost Highlight " + q, new Vector3(q * 5.14f - .15f, .2f, -1.39f), new Vector3(.06f, 1.12f, .04f), frame);
            }
            Hotspot(bed, RoomAction.Dream, "New Game", new Vector3(0, .23f, 0), new Vector3(10.7f, 1.52f, 2.7f));

            GameObject hero = Group(root.transform, "Sleeping Hero - DREAM hotspot", new Vector3(-3.65f, .68f, -.95f));
            Oval(hero.transform, "Neck Shadow", new Vector3(.42f, -.06f, .08f), new Vector3(.52f, .38f, .54f), frame);
            Oval(hero.transform, "Head", new Vector3(-.08f, .21f, -.1f), new Vector3(1.12f, 1.02f, .86f), text);
            Oval(hero.transform, "Ear", new Vector3(.34f, .2f, -.19f), new Vector3(.24f, .3f, .26f), frame);
            Oval(hero.transform, "Cheek", new Vector3(.12f, .04f, -.53f), new Vector3(.34f, .18f, .03f), frame);
            Box(hero.transform, "Hair Back", new Vector3(-.34f, .54f, -.2f), new Vector3(.78f, .34f, .79f), shadow);
            Oval(hero.transform, "Night Cap Crown", new Vector3(-.14f, .76f, -.04f), new Vector3(1.2f, .37f, .85f), panel);
            Box(hero.transform, "Night Cap Band", new Vector3(-.12f, .58f, -.47f), new Vector3(.95f, .14f, .16f), secondary);
            Oval(hero.transform, "Night Cap Tail", new Vector3(-.72f, .72f, .12f), new Vector3(.43f, .22f, .25f), panel);
            Oval(hero.transform, "Night Cap Pom", new Vector3(-.96f, .68f, .1f), new Vector3(.2f, .2f, .2f), secondary);
            Box(hero.transform, "Closed Eye", new Vector3(.13f, .13f, -.546f), new Vector3(.3f, .05f, .03f), background);
            Box(hero.transform, "Eyebrow", new Vector3(.12f, .31f, -.52f), new Vector3(.22f, .035f, .03f), shadow);
            Oval(hero.transform, "Nose", new Vector3(.4f, -.03f, -.49f), new Vector3(.13f, .12f, .1f), text);
            Box(hero.transform, "Sleeping Mouth", new Vector3(.24f, -.2f, -.52f), new Vector3(.17f, .035f, .02f), pressed);
            Oval(hero.transform, "Pajama Torso", new Vector3(1.05f, -.06f, .07f), new Vector3(1.74f, .72f, .88f), blue);
            Box(hero.transform, "Pajama Collar", new Vector3(.42f, .02f, -.43f), new Vector3(.38f, .12f, .1f), secondary);
            Oval(hero.transform, "Bent Sleeve", new Vector3(.78f, -.28f, -.39f), new Vector3(1.12f, .32f, .43f), secondary);
            Box(hero.transform, "Sleeve Cuff", new Vector3(1.28f, -.3f, -.56f), new Vector3(.12f, .3f, .1f), secondary);
            Oval(hero.transform, "Resting Hand", new Vector3(1.48f, -.35f, -.51f), new Vector3(.49f, .18f, .32f), text);
            Hotspot(hero, RoomAction.Dream, "New Game", new Vector3(.55f, .15f, -.08f), new Vector3(2.55f, 1.25f, 1.17f));

            GameObject clock = Group(root.transform, "Clock - SETTINGS hotspot", new Vector3(-1.2f, 3.48f, 3f));
            Oval(clock.transform, "Clock Cast Shadow", new Vector3(.08f, -.12f, .12f), new Vector3(1.18f, 1.18f, .12f), shadow);
            Shape(clock.transform, "Clock Body", PrimitiveType.Cylinder, Vector3.zero, new Vector3(.87f, .08f, .87f), frame).transform.localRotation = Quaternion.Euler(90, 0, 0);
            Shape(clock.transform, "Clock Face", PrimitiveType.Cylinder, new Vector3(0, 0, -.12f), new Vector3(.7f, .04f, .7f), secondary).transform.localRotation = Quaternion.Euler(90, 0, 0);
            for (int q = 0; q < 12; q++)
            {
                float a = q * Mathf.PI / 6f;
                Box(clock.transform, "Hour Mark " + q, new Vector3(Mathf.Sin(a) * .28f, Mathf.Cos(a) * .28f, -.17f), new Vector3(.04f, .07f, .02f), shadow);
            }
            GameObject hand = Group(clock.transform, "Moving Hand", new Vector3(0, 0, -.21f));
            Box(hand.transform, "Hand", new Vector3(0, .12f, 0), new Vector3(.035f, .25f, .025f), accent);
            environment.ClockHand = hand.transform;
            Hotspot(clock, RoomAction.Settings, "Settings", Vector3.zero, new Vector3(1.1f, 1.1f, .45f));

            GameObject shelf = Group(root.transform, "Bookshelf - CREDITS hotspot", new Vector3(-5.55f, 3.6f, 2.68f));
            Box(shelf.transform, "Left Upright", new Vector3(-.62f, 0, 0), new Vector3(.15f, 1.15f, .65f), frame);
            Box(shelf.transform, "Right Upright", new Vector3(.62f, 0, 0), new Vector3(.15f, 1.15f, .65f), frame);
            Box(shelf.transform, "Shelf Board", new Vector3(0, -.53f, 0), new Vector3(1.65f, .15f, .75f), frame);
            Box(shelf.transform, "Shelf Rail", new Vector3(0, .08f, -.3f), new Vector3(1.38f, .06f, .08f), shadow);
            Material[] spines = { accent, text, secondary, pressed, blue };
            for (int q = 0; q < 5; q++)
                Box(shelf.transform, "Book " + q, new Vector3(-.48f + q * .2f, -.12f, -.08f), new Vector3(.14f, .65f + (q % 3) * .07f, .42f), spines[q]);
            GameObject loose = Box(shelf.transform, "Loose Book", new Vector3(.28f, .42f, -.1f), new Vector3(.48f, .11f, .42f), accent);
            environment.LooseBook = loose.transform;
            Box(shelf.transform, "Plant Pot", new Vector3(.48f, -.06f, -.12f), new Vector3(.28f, .34f, .28f), pressed);
            for (int q = 0; q < 3; q++)
                Box(shelf.transform, "Plant Leaf " + q, new Vector3(.38f + q * .1f, .25f + (q % 2) * .12f, -.1f), new Vector3(.08f, .34f, .08f), heaven);
            Hotspot(shelf, RoomAction.Credits, "Credits", Vector3.zero, new Vector3(1.8f, 1.3f, .85f));

            GameObject calendar = Group(root.transform, "Calendar", new Vector3(1.05f, 3.23f, 3.07f));
            Box(calendar.transform, "Calendar Cast Shadow", new Vector3(.08f, -.1f, .08f), new Vector3(1.61f, 2.08f, .07f), shadow);
            Box(calendar.transform, "Calendar Sheet", Vector3.zero, new Vector3(1.55f, 2.02f, .08f), panel);
            Box(calendar.transform, "Calendar Binding", new Vector3(0, 1.04f, -.05f), new Vector3(1.62f, .1f, .12f), frame);
            for (int q = 1; q < 7; q++)
                Box(calendar.transform, "Calendar Column " + q, new Vector3(-.78f + q * .22f, -.08f, -.06f), new Vector3(.035f, 1.77f, .02f), secondary);
            for (int q = 1; q < 7; q++)
                Box(calendar.transform, "Calendar Row " + q, new Vector3(0, -.94f + q * .25f, -.13f), new Vector3(1.43f, .07f, .025f), secondary);
            Box(calendar.transform, "Circled Day", new Vector3(.25f, -.28f, -.1f), new Vector3(.24f, .05f, .025f), accent);
            for (int q = 0; q < 5; q++)
                Box(calendar.transform, "Calendar Day " + q, new Vector3(-.48f + q * .23f, .63f, -.09f), new Vector3(.07f, .07f, .025f), secondary);

            GameObject nightstand = Group(root.transform, "Nightstand", new Vector3(2.45f, .1f, 2.5f));
            Box(nightstand.transform, "Nightstand Cast Shadow", new Vector3(.14f, -.24f, .32f), new Vector3(1.95f, 2.13f, .18f), background);
            Box(nightstand.transform, "Cabinet", Vector3.zero, new Vector3(1.55f, 1.65f, .82f), shadow);
            Box(nightstand.transform, "Top", new Vector3(0, .88f, -.03f), new Vector3(1.7f, .12f, .95f), frame);
            Box(nightstand.transform, "Cabinet Side", new Vector3(.81f, 0, -.03f), new Vector3(.12f, 1.64f, .84f), panel);
            for (int q = 0; q < 2; q++)
            {
                Box(nightstand.transform, "Drawer " + q, new Vector3(0, .33f - q * .75f, -.44f), new Vector3(1.34f, .61f, .06f), panel);
                Shape(nightstand.transform, "Drawer Handle " + q, PrimitiveType.Sphere, new Vector3(0, .32f - q * .75f, -.5f), new Vector3(.12f, .09f, .06f), frame);
            }
            Box(nightstand.transform, "Lamp Stem", new Vector3(.22f, 1.12f, -.08f), new Vector3(.08f, .4f, .08f), frame);
            Oval(nightstand.transform, "Lamp Shade", new Vector3(.22f, 1.43f, -.08f), new Vector3(.62f, .34f, .5f), pressed);
            Box(nightstand.transform, "Lamp Warm Opening", new Vector3(.22f, 1.31f, -.35f), new Vector3(.35f, .045f, .03f), hover);
            Box(nightstand.transform, "Lamp Pool On Top", new Vector3(.22f, .96f, -.31f), new Vector3(.66f, .025f, .38f), accent);

            GameObject door = Group(root.transform, "Door - QUIT hotspot", new Vector3(6.4f, 1.55f, 3.15f));
            Box(door.transform, "Door Cast Shadow", new Vector3(-.14f, -.04f, .21f), new Vector3(2.45f, 4.52f, .07f), background);
            Box(door.transform, "Door Frame", Vector3.zero, new Vector3(2.1f, 4.46f, .24f), frame);
            Box(door.transform, "Door Leaf", new Vector3(0, 0, -.15f), new Vector3(1.94f, 4.17f, .13f), panel);
            Box(door.transform, "Door Upper Panel", new Vector3(0, .76f, -.23f), new Vector3(1.44f, 1.54f, .07f), shadow);
            Box(door.transform, "Door Lower Panel", new Vector3(0, -.92f, -.23f), new Vector3(1.44f, 1.3f, .07f), shadow);
            Box(door.transform, "Door Light Leak", new Vector3(.82f, -1.94f, -.28f), new Vector3(.07f, .24f, .03f), hover);
            Box(door.transform, "Door Top Lintel", new Vector3(0, 2.25f, -.29f), new Vector3(2.4f, .14f, .14f), shadow);
            Box(door.transform, "Door Panel Edge", new Vector3(-.94f, 0, -.29f), new Vector3(.05f, 4.05f, .04f), secondary);
            Box(door.transform, "Door Latch Edge", new Vector3(.93f, 0, -.3f), new Vector3(.05f, 4.05f, .045f), frame);
            Box(door.transform, "Door Key Plate", new Vector3(-.68f, -.1f, -.42f), new Vector3(.3f, .5f, .04f), frame);
            Oval(door.transform, "Doorknob Highlight", new Vector3(-.68f, -.1f, -.49f), new Vector3(.15f, .15f, .05f), text);
            Shape(door.transform, "Doorknob", PrimitiveType.Sphere, new Vector3(-.68f, -.1f, -.36f), new Vector3(.16f, .16f, .14f), text);
            Hotspot(door, RoomAction.Quit, "Quit", Vector3.zero, new Vector3(2.3f, 4.5f, .48f));

            GameObject picture = Group(root.transform, "Crooked Picture", new Vector3(-3.3f, 3.35f, 3.16f));
            Box(picture.transform, "Picture Frame", Vector3.zero, new Vector3(1.15f, .8f, .12f), frame);
            Box(picture.transform, "Picture Face", new Vector3(0, 0, -.08f), new Vector3(.91f, .55f, .03f), panel);
            Box(picture.transform, "Picture Moon", new Vector3(.18f, .08f, -.11f), new Vector3(.18f, .18f, .02f), secondary);
            environment.Picture = picture.transform;
            Box(root.transform, "Rug", new Vector3(.4f, -.82f, -1.35f), new Vector3(4.5f, .03f, 1.45f), pressed);
            Box(root.transform, "Rug Border", new Vector3(.4f, -.79f, -1.35f), new Vector3(4.14f, .02f, 1.13f), shadow);

            GameObject cameraNode = Group(root.transform, "Pixel Room Camera", new Vector3(0, 3.4f, -9.5f));
            cameraNode.tag = "MainCamera";
            cameraNode.transform.LookAt(new Vector3(0, 1.5f, 1.1f));
            Camera camera = cameraNode.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 4.15f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = ColorFromHex("161318");
            camera.cullingMask = ~0;
            PixelRoomView pixels = cameraNode.AddComponent<PixelRoomView>();
            pixels.MinimumHalfWorldWidth = 7.8f;
            pixels.RoomRoot = environment;

            string path = Root + "/Prefabs/SleepwalkingRoom.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            if (prefab == null) throw new System.InvalidOperationException("Room prefab was not saved: " + path);
            Object.DestroyImmediate(root);

            Scene previousScene = SceneManager.GetActiveScene();
            bool batch = Application.isBatchMode;
            Scene scene;
            try
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                    batch ? NewSceneMode.Single : NewSceneMode.Additive);
            }
            catch (System.InvalidOperationException)
            {
                Debug.LogWarning("Room preview skipped: save the open untitled scene, then rebuild visual assets.");
                return;
            }
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance == null || scene.rootCount == 0)
                throw new System.InvalidOperationException("Room preview has no prefab instance.");
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, Root + "/Preview/RoomPreview.unity"))
                throw new System.InvalidOperationException("Room preview scene could not be saved.");
            if (!batch)
            {
                if (previousScene.IsValid()) SceneManager.SetActiveScene(previousScene);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        static Color ColorFromHex(string value)
        {
            ColorUtility.TryParseHtmlString("#" + value, out Color color);
            return color;
        }

        static void BuildIllustratedVariant()
        {
            const string texturePath = Root + "/Textures/Processed/SleepingHero_UIPalette.png";
            const string materialPath = Root + "/Materials/IllustratedHeroCutout.mat";
            const string variantPath = Root + "/Prefabs/SleepwalkingRoom_IllustratedHero.prefab";
            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer == null) throw new System.InvalidOperationException("Missing Tessera hero texture: " + texturePath);
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 256;
            importer.SaveAndReimport();
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            Shader shader = Shader.Find("Sleepwalking/PixelCutout");
            if (texture == null || shader == null) throw new System.InvalidOperationException("Illustrated hero texture or cutout shader was not imported.");
            Material cutout = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (cutout == null)
            {
                cutout = new Material(shader);
                AssetDatabase.CreateAsset(cutout, materialPath);
            }
            cutout.shader = shader;
            cutout.SetTexture("_MainTex", texture);
            EditorUtility.SetDirty(cutout);

            GameObject baseRoom = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/SleepwalkingRoom.prefab");
            GameObject instance = PrefabUtility.InstantiatePrefab(baseRoom) as GameObject;
            if (instance == null) throw new System.InvalidOperationException("Could not instantiate room for illustrated variant.");
            instance.name = "Sleepwalking Room - Illustrated Hero";
            Transform hero = instance.transform.Find("Sleeping Hero - DREAM hotspot");
            if (hero == null) throw new System.InvalidOperationException("Room hero hotspot was not found.");
            foreach (Renderer renderer in hero.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
            GameObject cutoutObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            cutoutObject.name = "Sleeping Hero Illustration - editable 3D quad";
            cutoutObject.transform.SetParent(hero, false);
            cutoutObject.transform.localPosition = new Vector3(.55f, .12f, -.25f);
            cutoutObject.transform.localRotation = Quaternion.identity;
            cutoutObject.transform.localScale = new Vector3(2.9f, 1.75f, 1f);
            Object.DestroyImmediate(cutoutObject.GetComponent<Collider>());
            cutoutObject.GetComponent<Renderer>().sharedMaterial = cutout;
            BoxCollider hotspot = hero.GetComponent<BoxCollider>();
            hotspot.center = new Vector3(.72f, .37f, -.72f);
            hotspot.size = new Vector3(3.55f, 2.25f, 1.1f);
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, variantPath);
            Object.DestroyImmediate(instance);
            if (saved == null) throw new System.InvalidOperationException("Illustrated room variant was not saved.");
        }

        static void BuildIllustratedRoomPreview()
        {
            GameObject variant = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/SleepwalkingRoom_IllustratedHero.prefab");
            if (variant == null) throw new System.InvalidOperationException("Illustrated room prefab is missing.");
            Scene previousScene = SceneManager.GetActiveScene();
            bool batch = Application.isBatchMode;
            Scene scene;
            try
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                    batch ? NewSceneMode.Single : NewSceneMode.Additive);
            }
            catch (System.InvalidOperationException)
            {
                Debug.LogWarning("Illustrated preview skipped: save the open untitled scene, then rebuild visual assets.");
                return;
            }
            GameObject instance = PrefabUtility.InstantiatePrefab(variant, scene) as GameObject;
            if (instance == null || scene.rootCount != 1)
                throw new System.InvalidOperationException("Illustrated preview must contain one room prefab root.");
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, IllustratedRoomScene))
                throw new System.InvalidOperationException("Could not save illustrated room preview scene.");
            if (!batch)
            {
                if (previousScene.IsValid()) SceneManager.SetActiveScene(previousScene);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        static void BuildThemes()
        {
            BuildTheme(DreamThemeId.Meat, accent, hover, pressed);
            BuildTheme(DreamThemeId.PostApocalypse, frame, secondary, shadow);
            BuildTheme(DreamThemeId.DeepWater, blue, secondary, panel);
            BuildTheme(DreamThemeId.Heaven, text, heaven, secondary);
        }

        static void PreserveFlatThemes()
        {
            string flatRoot = Root + "/Prefabs/Themes/Flat";
            Directory.CreateDirectory(flatRoot);
            string[] names = { "Meat", "PostApocalypse", "DeepWater", "Heaven" };
            foreach (string name in names)
            {
                string backup = flatRoot + "/" + name + "_Flat.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(backup) != null) continue;
                AssetDatabase.CopyAsset(Root + "/Prefabs/Themes/" + name + ".prefab", backup);
            }
        }

        static void BuildGeneratedBackdrop(Transform parent, DreamThemeId id)
        {
            string imageName = id == DreamThemeId.PostApocalypse ? "PostApoc" : id.ToString();
            string texturePath = Root + "/Textures/Generated/" + imageName + "Backdrop_320.png";
            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer == null) throw new System.InvalidOperationException("Generated backdrop missing: " + texturePath);
            importer.textureType = TextureImporterType.Default;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            Shader shader = Shader.Find("Sleepwalking/PixelBackdrop");
            if (texture == null || shader == null) throw new System.InvalidOperationException("Pixel backdrop import failed: " + texturePath);
            string materialPath = Root + "/Materials/Backdrop_" + id + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.shader = shader;
            material.SetTexture("_MainTex", texture);
            material.SetColor("_Tint", id == DreamThemeId.Heaven ? new Color(.58f, .62f, .72f, 1) : Color.white);
            EditorUtility.SetDirty(material);
            TexturedQuad(parent, "Distant Background Left", new Vector3(-1f, 1.8f, 3.84f), new Vector3(22f, 14f, 1), material);
            TexturedQuad(parent, "Distant Background Right", new Vector3(21f, 1.8f, 3.84f), new Vector3(22f, 14f, 1), material);
        }

        static void BuildTheme(DreamThemeId id, Material main, Material highlight, Material dark)
        {
            GameObject root = new GameObject(id + " Dream Theme");
            DreamTheme theme = root.AddComponent<DreamTheme>();
            theme.Id = id;
            theme.Main = main;
            theme.Accent = highlight;
            theme.Shadow = dark;
            string sheetName = id == DreamThemeId.PostApocalypse ? "PostApoc" : id.ToString();
            string sheetPath = TilesetRoot + "/TL_" + sheetName + "_Dream.png";
            Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(sheetPath);
            Shader tileShader = Shader.Find("Sleepwalking/PixelCutout");
            if (atlas == null || tileShader == null) throw new System.InvalidOperationException("Dream tileset import failed: " + sheetPath);
            theme.Tiles = AtlasMaterial("DreamTiles_" + id, tileShader, atlas, Vector2.one, Vector2.zero);
            Sprite dreamerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Textures/Processed/Dreamer_TempSilhouette.png");
            if (dreamerSprite == null) throw new System.InvalidOperationException("Temporary dreamer sprite is missing.");
            root.AddComponent<DreamPlayerAppearance>().TemporarySprite = dreamerSprite;
            // L07 заканчивается на X=21.1; фон закрывает края камеры у выхода по обеим осям.
            Box(root.transform, "Backdrop", new Vector3(10f, 1.8f, 4), new Vector3(44f, 14f, .2f), dark);
            BuildGeneratedBackdrop(root.transform, id);
            PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/Themes/" + id + ".prefab");
            Object.DestroyImmediate(root);
        }
    }
}
