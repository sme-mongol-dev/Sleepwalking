using System.IO;
using Sleepwalking.Levels;
using UnityEditor;
using UnityEngine;

namespace Sleepwalking.Levels.Editor
{
    public static class SleepwalkingTutorialBuilder
    {
        private const string Prefabs = "Assets/SleepwalkingLevels/Prefabs";
        private const string HintPrefabs = "Assets/SleepwalkingUI/Prefabs/Tutorial";
        private const float CellSize = 32f;
        private const float Depth = 0.8f;

        private static Material floorMaterial;
        private static Material hazardMaterial;
        private static Mesh triangleMesh;

        [MenuItem("Sleepwalking/Levels/Build Tutorial Prefabs")]
        public static void BuildTutorialPrefabs()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "SleepwalkingLevels/Prefabs"));
            AssetDatabase.Refresh();

            floorMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/SleepwalkingLevels/Materials/Floor.mat");
            hazardMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/SleepwalkingLevels/Materials/Hazard.mat");
            triangleMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/SleepwalkingLevels/TrianglePrism.asset");
            if (floorMaterial == null || hazardMaterial == null || triangleMesh == null)
                throw new FileNotFoundException("Build the seven level materials and triangle mesh before the tutorials.");

            Build(1);
            Build(2);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Sleepwalking: two editable tutorial prefabs built.");
        }

        public static void BuildFromCommandLine() => BuildTutorialPrefabs();

        private static void Build(int tutorial)
        {
            string name = "Tutorial" + tutorial.ToString("00") + "_DreamGeometry";
            string path = Prefabs + "/" + name + ".prefab";
            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
            GameObject root = exists ? PrefabUtility.LoadPrefabContents(path) : new GameObject(name);
            try
            {
                root.name = name;
                root.transform.localScale = new Vector3(CellSize, CellSize, 1f);
                Transform geometry = ReplaceGroup(root.transform, "Geometry");
                ReplaceGroup(root.transform, "Hazards");
                ReplaceGroup(root.transform, "Mechanisms");
                Transform markers = ReplaceGroup(root.transform, "Markers");
                Transform readability = GetOrCreate(root.transform, "Readability");
                Transform hints = GetOrCreate(readability, "TutorialHints");

                if (tutorial == 1) BuildMovementLesson(geometry, hints);
                else BuildDashLesson(geometry, root.transform.Find("Hazards"), hints);

                Transform start = Marker(markers, "PlayerStart", 1f, 0.1f);
                Transform bed = CloneBed(markers, tutorial == 1 ? 16.5f : 18.5f, tutorial == 1 ? 3f : 0f);
                SleepwalkingLevelDefinition definition = root.GetComponent<SleepwalkingLevelDefinition>();
                if (definition == null) definition = root.AddComponent<SleepwalkingLevelDefinition>();
                definition.Configure(tutorial == 1 ? -2 : -1,
                    tutorial == 1 ? DreamStyle.Heaven : DreamStyle.DeepWater, start, bed);

                SleepwalkingTileCollisionBuilder.Shape(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                if (exists) PrefabUtility.UnloadPrefabContents(root);
                else Object.DestroyImmediate(root);
            }
        }

        private static void BuildMovementLesson(Transform geometry, Transform hints)
        {
            Solid(geometry, "StartFloor", 0, 11, -1, 0);
            Solid(geometry, "GripWall", 7, 8, 2, 8);
            Solid(geometry, "ExitLedge", 11, 18, -1, 3);
            CaveShell(geometry, 18, 8);

            Hint(hints, "Run", 2.5f, 2.8f);
            Hint(hints, "Jump", 5.5f, 2.8f);
            Hint(hints, "Grab", 9.5f, 5.5f);
            Hint(hints, "WallJump", 13f, 5.5f);
        }

        private static void BuildDashLesson(Transform geometry, Transform hazards, Transform hints)
        {
            Solid(geometry, "StartFloor", 0, 7, -1, 0);
            Solid(geometry, "FinalRun", 13, 20, -1, 0);
            CaveShell(geometry, 20, 7);
            for (int q = 0; q < 12; q++)
                Spike(hazards, "Tutorial02_PitSpikes", q, 7f + (q + 0.5f) * 0.5f, -1f);

            Hint(hints, "Dash", 4.7f, 3.2f);
        }

        private static void CaveShell(Transform geometry, float end, float roofBottom)
        {
            Transform cave = Group(geometry, "CaveShell");
            Shell(cave, "Cave_Bedrock", -8, end + 8, -20, -4);
            Shell(cave, "Cave_LeftWall", -8, 0, -4, roofBottom);
            Shell(cave, "Cave_RightWall", end, end + 8, -4, roofBottom);
            Shell(cave, "Cave_LeftRoof", -8, 0, roofBottom, 28);
            Shell(cave, "Cave_Roof_0", 0, end, roofBottom, 28);
            Shell(cave, "Cave_RightRoof", end, end + 8, roofBottom, 28);
            Shell(cave, "Cave_Foundation", 0, end, -4, -1);
        }

        private static void Shell(Transform parent, string name, float left, float right, float bottom, float top)
        {
            Solid(parent, name, left, right, bottom, top, true);
        }

        private static void Solid(Transform parent, string name, float left, float right, float bottom, float top,
            bool shell = false)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = new Vector3((left + right) * 0.5f, (bottom + top) * 0.5f, 0f);
            block.transform.localScale = new Vector3(right - left, top - bottom, Depth);
            block.GetComponent<MeshRenderer>().sharedMaterial = floorMaterial;
            Object.DestroyImmediate(block.GetComponent<BoxCollider>());
            block.AddComponent<BoxCollider2D>();
            GameObject collision3D = new GameObject("Collision3D");
            collision3D.transform.SetParent(block.transform, false);
            collision3D.AddComponent<BoxCollider>();
            block.AddComponent<SleepwalkingTileSurface>().IsShell = shell;
        }

        private static void Spike(Transform parent, string id, int index, float x, float bottom)
        {
            GameObject spike = new GameObject(id + "_" + (index + 1));
            spike.transform.SetParent(parent, false);
            spike.transform.localPosition = new Vector3(x, bottom + 0.135f, 0f);
            spike.transform.localScale = new Vector3(0.5f, 0.27f, Depth);
            spike.AddComponent<MeshFilter>().sharedMesh = triangleMesh;
            spike.AddComponent<MeshRenderer>().sharedMaterial = hazardMaterial;
            PolygonCollider2D collider2D = spike.AddComponent<PolygonCollider2D>();
            collider2D.points = new[] { new Vector2(-0.5f, -0.5f), new Vector2(0.5f, -0.5f), new Vector2(0f, 0.5f) };
            collider2D.isTrigger = true;
            GameObject collision3D = new GameObject("Collision3D");
            collision3D.transform.SetParent(spike.transform, false);
            MeshCollider collider3D = collision3D.AddComponent<MeshCollider>();
            collider3D.sharedMesh = triangleMesh;
            collider3D.convex = true;
            collider3D.isTrigger = true;
            spike.AddComponent<SleepwalkingLevelFeature>().Configure(LevelFeatureKind.Spikes, id);
            spike.AddComponent<SleepwalkingLevelVisualState>();
        }

        private static Transform CloneBed(Transform parent, float x, float top)
        {
            const string source = Prefabs + "/L01_DreamGeometry.prefab";
            GameObject template = PrefabUtility.LoadPrefabContents(source);
            try
            {
                Transform original = template.transform.Find("Markers/BedExit");
                if (original == null) throw new MissingReferenceException("L01 BedExit is missing.");
                GameObject bed = Object.Instantiate(original.gameObject, parent);
                bed.name = "BedExit";
                bed.transform.localPosition = new Vector3(x, top, 0f);
                bed.transform.localRotation = Quaternion.identity;
                bed.transform.localScale = Vector3.one;
                return bed.transform;
            }
            finally { PrefabUtility.UnloadPrefabContents(template); }
        }

        private static void Hint(Transform parent, string name, float x, float y)
        {
            Transform marker = GetOrCreate(parent, name);
            marker.localPosition = new Vector3(x, y, -1f);
            marker.localRotation = Quaternion.identity;
            marker.localScale = Vector3.one;
            string path = HintPrefabs + "/Tutorial_" + name + ".prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new FileNotFoundException("Missing tutorial hint prefab: " + path);
            if (marker.Find(prefab.name) != null) return;
            GameObject sign = (GameObject)PrefabUtility.InstantiatePrefab(prefab, marker);
            sign.transform.localPosition = Vector3.zero;
            sign.transform.localRotation = Quaternion.identity;
            sign.transform.localScale = Vector3.one;
        }

        private static Transform Marker(Transform parent, string name, float x, float y)
        {
            Transform marker = Group(parent, name);
            marker.localPosition = new Vector3(x, y, 0f);
            return marker;
        }

        private static Transform ReplaceGroup(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);
            return Group(parent, name);
        }

        private static Transform GetOrCreate(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            return existing != null ? existing : Group(parent, name);
        }

        private static Transform Group(Transform parent, string name)
        {
            GameObject group = new GameObject(name);
            group.transform.SetParent(parent, false);
            return group.transform;
        }
    }
}
