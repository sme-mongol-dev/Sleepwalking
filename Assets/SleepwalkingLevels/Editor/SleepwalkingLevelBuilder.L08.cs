using Sleepwalking.Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Sleepwalking.Levels.Editor
{
    public static partial class SleepwalkingLevelBuilder
    {
        public const string EighthPrefabPath = "Assets/SleepwalkingLevels/Prefabs/L08_DreamGeometry.prefab";

        public static void BuildAndConnectEighthLevel()
        {
            BuildEighthPrefab();
            ConnectEighthLevel();
        }

        [MenuItem("Sleepwalking/Gameplay/Build Windows With L08")]
        public static void BuildWindowsWithEighthLevel()
        {
            const string scene = "Assets/Scenes/Sleepwalking.unity";
            string output = System.IO.Path.GetFullPath("Builds/Windows-L08/Sleepwalking.exe");
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(output));
            AssetDatabase.SaveAssets();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { scene },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            Debug.Log("L08_WINDOWS_BUILD " + report.summary.result + " errors=" + report.summary.totalErrors +
                " warnings=" + report.summary.totalWarnings + " path=" + output);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new UnityException("L08 Windows build failed.");
        }

        [MenuItem("Sleepwalking/Levels/Build L08 From Layout")]
        public static void BuildEighthPrefab()
        {
            PrepareMaterials();
            GameObject root = new GameObject("L08_DreamGeometry");
            try
            {
                root.transform.localScale = new Vector3(CellSize, CellSize, 1f);
                Transform geometry = Group(root.transform, "Geometry");
                Transform hazards = Group(root.transform, "Hazards");
                Transform mechanisms = Group(root.transform, "Mechanisms");
                Transform markers = Group(root.transform, "Markers");
                readability = Group(root.transform, "Readability");

                // The sketch reads left to right: start, saw switch, shuttle,
                // lower recovery pads, raised laser switch, and the exit wall.
                Platform(geometry, "Start", 0f, 3f, 0f, 4f);
                Platform(geometry, "SawSwitchLedge", 3f, 8f, 3f, 2f);
                Platform(geometry, "LowerRecoveryA", 14f, 16f, 1f);
                Platform(geometry, "LowerRecoveryB", 18f, 20f, 0f);
                Platform(geometry, "LaserSwitchLedge", 16f, 20f, 5f, 2f);
                Platform(geometry, "FinalRun", 22f, 26f, 7f, 9f);

                Moving(mechanisms, "L08_Shuttle", 9f, 3f, new Vector2(6f, 0f), 8f);
                Button(mechanisms, "L08_SawButton", "L08_Saws", 4.5f, 3f, 32f);
                ButtonGlyph(mechanisms.Find("L08_SawButton"), true);
                Button(mechanisms, "L08_LaserButton", "L08_Laser", 18f, 5f, 18f);
                ButtonGlyph(mechanisms.Find("L08_LaserButton"), false);
                Laser(hazards, "L08_Laser", 6.3f, 3f, 4.5f);
                Saw(hazards, "L08_Saws_Entrance", .6f, 8.9f, .8f);
                Saw(hazards, "L08_Saws_Exit", 21.2f, 7.3f, 1.1f);

                Transform cave = Group(geometry, "CaveShell");
                CaveBlock(cave, "Cave_Bedrock", -3f, 29f, -10f, -4f);
                CaveBlock(cave, "Cave_LeftWall", -3f, 0f, -4f, 12f);
                CaveBlock(cave, "Cave_RightWall", 26f, 29f, -4f, 12f);
                CaveBlock(cave, "Cave_Roof_0", 0f, 26f, 11f, 15f);
                CaveBlock(cave, "Cave_SawOverhang", 3f, 8f, 6f, 11f);
                CaveBlock(cave, "Cave_SwitchOverhang", 17f, 20f, 7f, 11f);
                CaveBlock(cave, "Cave_PitFoundation", 3f, 22f, -4f, -3f);
                Platform(geometry, "UpperLeftShelf", 0f, 1f, 8f);
                Spikes(hazards, "L08_PitSpikes", 3f, 22f, -3f, 38);

                Transform start = Marker(markers, "PlayerStart", 1.7f, .1f);
                Transform exit = Bed(markers, 24.2f, 7f, 0f);
                root.AddComponent<SleepwalkingLevelDefinition>().Configure(8, DreamStyle.Meat, start, exit);
                SleepwalkingTileCollisionBuilder.Shape(root);
                PrefabUtility.SaveAsPrefabAsset(root, EighthPrefabPath);
            }
            finally { Object.DestroyImmediate(root); }
            AssetDatabase.SaveAssets();
            Debug.Log("Built L08 from the supplied layout.");
        }

        static void ButtonGlyph(Transform button, bool plus)
        {
            Transform light = button.Find("Button_Light");
            if (light != null) Object.DestroyImmediate(light.gameObject);
            Vector3 position = new Vector3(button.localPosition.x, button.localPosition.y + .85f, -1.5f);
            VisualCube(readability, plus ? "SawSwitch_MinusStroke" : "LaserSwitch_Minus", position,
                new Vector3(.45f, .08f, .06f), safeEdgeMaterial);
            if (plus)
                VisualCube(readability, "SawSwitch_PlusStroke", position,
                    new Vector3(.08f, .45f, .06f), safeEdgeMaterial);
        }

        // Update only the level list; keep the authored scene and room lighting.
        [MenuItem("Sleepwalking/Levels/Connect L08 To Scene")]
        public static void ConnectEighthLevel()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Sleepwalking.unity");
            SleepwalkingGame game = Object.FindFirstObjectByType<SleepwalkingGame>();
            if (game == null) throw new UnityException("Integration scene has no SleepwalkingGame.");
            var prefabs = new GameObject[SleepwalkingGame.AuthoredDreamCount];
            for (int i = 0; i < prefabs.Length; i++)
                prefabs[i] = i < game.LevelPrefabs.Length ? game.LevelPrefabs[i] : null;
            prefabs[7] = AssetDatabase.LoadAssetAtPath<GameObject>(EighthPrefabPath);
            if (prefabs[7] == null) throw new UnityException("Build L08 before connecting it.");
            game.LevelPrefabs = prefabs;
            EditorUtility.SetDirty(game);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
