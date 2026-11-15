using Sleepwalking.Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Sleepwalking.Levels.Editor
{
    public static partial class SleepwalkingLevelBuilder
    {
        public const string NinthPrefabPath = "Assets/SleepwalkingLevels/Prefabs/L09_DreamGeometry.prefab";

        [MenuItem("Sleepwalking/Levels/Build L09 From Layout")]
        public static void BuildNinthPrefab()
        {
            PrepareMaterials();
            GameObject root = new GameObject("L09_DreamGeometry");
            try
            {
                root.transform.localScale = new Vector3(CellSize, CellSize, 1f);
                Transform geometry = Group(root.transform, "Geometry");
                Transform hazards = Group(root.transform, "Hazards");
                Transform mechanisms = Group(root.transform, "Mechanisms");
                Transform markers = Group(root.transform, "Markers");
                readability = Group(root.transform, "Readability");

                // Схема: старт слева, висящая колонна, челнок под двумя пилами,
                // средний столб с полкой, широкая площадка, щель с двумя пилами и выход.
                Platform(geometry, "Start", 0f, 4f, 0f, 4f);
                Platform(geometry, "LeftColumn", 5f, 7f, 13f, 9f);
                Platform(geometry, "CeilingCap", 10f, 12f, 13f, 2f);
                Platform(geometry, "CenterPillar", 15f, 17f, 11f, 9f);
                Platform(geometry, "HighShelf", 17f, 20f, 7f, 1f);
                Platform(geometry, "WideFloor", 12f, 22f, 0f, 1f);
                Platform(geometry, "RightColumn", 23f, 26f, 13f, 8f);
                Platform(geometry, "EndRun", 25f, 30f, 0f, 4f);

                GameObject shuttle = Cube(mechanisms, "L09_Shuttle", 5.5f, -0.5f, 3f, 1f, mechanismMaterial);
                shuttle.AddComponent<SleepwalkingTileSurface>();
                shuttle.AddComponent<SleepwalkingLevelFeature>().Configure(
                    LevelFeatureKind.MovingPlatform, "L09_Shuttle", movement: new Vector2(8f, 0f), cycle: 7f);
                MechanismCues(shuttle.transform, false);

                Saw(hazards, "L09_Saw_LeftLow", 9f, 3.4f, .8f);
                Saw(hazards, "L09_Saw_LeftHigh", 9f, 6.6f, .8f);
                Saw(hazards, "L09_Saw_Middle", 18.5f, 3.2f, .8f);
                Saw(hazards, "L09_Saw_RightLow", 23.6f, 1.3f, .8f);
                Saw(hazards, "L09_Saw_RightHigh", 23.6f, 4.1f, .8f);
                Spikes(hazards, "L09_PitSpikes", 4f, 25f, -3f, 42);

                Transform cave = Group(geometry, "CaveShell");
                CaveBlock(cave, "Cave_Bedrock", -3f, 33f, -8f, -4f);
                CaveBlock(cave, "Cave_LeftWall", -3f, 0f, -4f, 14f);
                CaveBlock(cave, "Cave_RightWall", 30f, 33f, -4f, 14f);
                CaveBlock(cave, "Cave_Roof_0", 0f, 30f, 13f, 16f);
                CaveBlock(cave, "Cave_PitFoundation", 4f, 25f, -4f, -3f);

                Transform start = Marker(markers, "PlayerStart", 1.6f, .1f);
                Transform exit = Bed(markers, 27.2f, 0f, 0f);
                root.AddComponent<SleepwalkingLevelDefinition>().Configure(9, DreamStyle.Meat, start, exit);
                SleepwalkingTileCollisionBuilder.Shape(root);
                PrefabUtility.SaveAsPrefabAsset(root, NinthPrefabPath);
            }
            finally { Object.DestroyImmediate(root); }
            AssetDatabase.SaveAssets();
            Debug.Log("Built L09 from the supplied layout.");
        }

        [MenuItem("Sleepwalking/Levels/Connect L09 To Scene")]
        public static void ConnectNinthLevel()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Sleepwalking.unity");
            SleepwalkingGame game = Object.FindFirstObjectByType<SleepwalkingGame>();
            if (game == null) throw new UnityException("Integration scene has no SleepwalkingGame.");
            var prefabs = new GameObject[SleepwalkingGame.AuthoredDreamCount];
            for (int i = 0; i < prefabs.Length; i++)
                prefabs[i] = game.LevelPrefabs != null && i < game.LevelPrefabs.Length ? game.LevelPrefabs[i] : null;
            prefabs[8] = AssetDatabase.LoadAssetAtPath<GameObject>(NinthPrefabPath);
            if (prefabs[8] == null) throw new UnityException("Build L09 before connecting it.");
            game.LevelPrefabs = prefabs;
            EditorUtility.SetDirty(game);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Connected L09. Dream count is " + game.LevelPrefabs.Length + ".");
        }
    }
}
