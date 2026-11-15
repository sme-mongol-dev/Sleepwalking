using System.IO;
using Sleepwalking.Levels;
using UnityEditor;
using UnityEngine;

namespace Sleepwalking.Levels.Editor
{
    public static class SleepwalkingLevelBuilder
    {
        private const string Root = "Assets/SleepwalkingLevels";
        private const string Prefabs = Root + "/Prefabs";
        private const string Materials = Root + "/Materials";
        private const float Depth = 0.8f;
        private const float CellSize = 32f;

        private static Material floorMaterial;
        private static Material hazardMaterial;
        private static Material mechanismMaterial;
        private static Material exitMaterial;
        private static Material safeFaceMaterial;
        private static Material safeEdgeMaterial;
        private static Material buttonMaterial;
        private static Mesh triangleMesh;
        private static Transform readability;

        [MenuItem("Sleepwalking/Levels/Build Seven Prefabs")]
        public static void BuildSevenPrefabs()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "SleepwalkingLevels/Prefabs"));
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "SleepwalkingLevels/Materials"));
            AssetDatabase.Refresh();

            floorMaterial = Material("Floor", new Color32(34, 32, 54, 255));
            hazardMaterial = Material("Hazard", new Color32(224, 64, 32, 255));
            mechanismMaterial = Material("Mechanism", new Color32(140, 80, 80, 255));
            exitMaterial = Material("Exit", new Color32(248, 212, 184, 255));
            safeFaceMaterial = Material("SafeFace", new Color32(22, 19, 24, 255));
            safeEdgeMaterial = Material("SafeEdge", new Color32(180, 184, 212, 255));
            buttonMaterial = Material("Button", new Color32(34, 158, 222, 255));
            triangleMesh = TriangleMesh();

            for (int q = 1; q <= 7; q++)
                Build(q);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Sleepwalking: seven editable level prefabs built.");
        }

        public static void BuildFromCommandLine()
        {
            BuildSevenPrefabs();
        }

        private static Material Material(string name, Color color)
        {
            string path = Materials + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null)
                    shader = Shader.Find("Unlit/Color");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Mesh TriangleMesh()
        {
            string path = Root + "/TrianglePrism.asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null)
                return mesh;

            mesh = new Mesh { name = "TrianglePrism" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0, 0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f), new Vector3(0, 0.5f, 0.5f)
            };
            mesh.triangles = new[] { 0, 2, 1, 3, 4, 5, 0, 1, 4, 0, 4, 3, 1, 2, 5, 1, 5, 4, 2, 0, 3, 2, 3, 5 };
            mesh.RecalculateNormals();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static void Build(int index)
        {
            GameObject root = new GameObject("L" + index.ToString("00") + "_DreamGeometry");
            root.transform.localScale = new Vector3(CellSize, CellSize, 1f);
            Transform geometry = Group(root.transform, "Geometry");
            Transform hazards = Group(root.transform, "Hazards");
            Transform mechanisms = Group(root.transform, "Mechanisms");
            Transform markers = Group(root.transform, "Markers");
            readability = Group(root.transform, "Readability");

            switch (index)
            {
                case 1: Layout1(geometry, hazards, mechanisms); break;
                case 2: Layout2(geometry, hazards, mechanisms); break;
                case 3: Layout3(geometry, hazards, mechanisms); break;
                case 4: Layout4(geometry, hazards, mechanisms); break;
                case 5: Layout5(geometry, hazards, mechanisms); break;
                case 6: Layout6(geometry, hazards, mechanisms); break;
                case 7: Layout7(geometry, hazards, mechanisms); break;
            }
            CaveShell(geometry, hazards, index);

            Transform start = Marker(markers, "PlayerStart", 1f, index == 6 ? -0.9f : 0.1f);
            float exitX = index == 1 ? 25.5f : index == 2 ? 30.5f : index == 3 ? 27.5f :
                index == 4 ? 23.5f : index == 5 ? 38.5f : index == 6 ? 40f : 42f;
            float exitY = index == 1 || index == 2 || index == 3 ? 2f :
                index == 4 ? 1f : index == 5 ? 2f : index == 6 ? 5f : 5f;
            Transform exit = Bed(markers, exitX, exitY, 0);
            root.AddComponent<SleepwalkingLevelDefinition>().Configure(index, DreamStyle.Meat, start, exit);
            SleepwalkingTileCollisionBuilder.Shape(root);

            string path = Prefabs + "/L" + index.ToString("00") + "_DreamGeometry.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static Transform Group(Transform parent, string name)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            return obj.transform;
        }

        private static float LevelEnd(int index)
        {
            return index == 1 ? 27f : index == 2 ? 32f : index == 3 ? 29f :
                index == 4 ? 25f : index == 5 ? 41f : index == 6 ? 42f : 44f;
        }

        private static GameObject Cube(Transform parent, string name, float x, float y, float width, float height, Material material, bool trigger = false)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = new Vector3(x, y, 0);
            obj.transform.localScale = new Vector3(width, height, Depth);
            obj.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.DestroyImmediate(obj.GetComponent<BoxCollider>());
            obj.AddComponent<BoxCollider2D>().isTrigger = trigger;
            GameObject collision3D = new GameObject("Collision3D");
            collision3D.transform.SetParent(obj.transform, false);
            collision3D.AddComponent<BoxCollider>().isTrigger = trigger;
            return obj;
        }

        private static GameObject VisualCube(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            obj.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.DestroyImmediate(obj.GetComponent<BoxCollider>());
            return obj;
        }

        private static void Platform(Transform parent, string name, float left, float right, float top, float height = 1f)
        {
            GameObject block = Cube(parent, name, (left + right) / 2, top - height / 2, right - left, height, floorMaterial);
            block.AddComponent<SleepwalkingTileSurface>();
        }

        private static void CaveBlock(Transform parent, string name, float left, float right, float bottom, float top)
        {
            GameObject block = Cube(parent, name, (left + right) / 2, (bottom + top) / 2,
                right - left, top - bottom, floorMaterial);
            block.AddComponent<SleepwalkingTileSurface>().IsShell = true;
        }

        private static void VisiblePit(Transform cave, Transform hazards, string id,
            float left, float right, float top)
        {
            CaveBlock(cave, "Cave_" + id + "Foundation", left, right, -4f, top);
            Spikes(hazards, id, left, right, top, Mathf.RoundToInt((right - left) * 2f));
        }

        private static void CaveShell(Transform geometry, Transform hazards, int index)
        {
            // Вся область вне игрового коридора заполнена твёрдыми тайлами.
            // Шипованные карманы лежат над дном пещеры, а потолок оставляет место для прыжка и рывка.
            const float floor = -4f;
            const float roof = 28f;
            Transform cave = Group(geometry, "CaveShell");
            float end = LevelEnd(index);
            float[] edges;
            float[] heights;
            switch (index)
            {
                case 5:
                    edges = new[] { 0f, 3f, 6f, 9f, 13f, 18f, 23f, 26f, 29f, end };
                    heights = new[] { 5f, 7f, 9f, 7f, 7f, 5f, 7f, 9f, 8f };
                    break;
                case 6:
                    edges = new[] { 0f, 3f, 16f, 20f, 23f, end };
                    heights = new[] { 3f, 5f, 6f, 8f, 9f };
                    break;
                case 7:
                    edges = new[] { 0f, 3f, 22f, 32f, end };
                    heights = new[] { 5f, 8f, 10f, 10f };
                    break;
                default:
                    edges = index == 4 ? new[] { 0f, end } : new[] { 0f, 13f, end };
                    heights = index == 4 ? new[] { 5f } : new[] { 5f, 7f };
                    break;
            }

            CaveBlock(cave, "Cave_Bedrock", -8f, end + 8f, -20f, floor);
            CaveBlock(cave, "Cave_LeftWall", -8f, 0f, floor, heights[0]);
            CaveBlock(cave, "Cave_RightWall", end, end + 8f, floor, heights[heights.Length - 1]);
            CaveBlock(cave, "Cave_LeftRoof", -8f, 0f, heights[0], roof);
            for (int q = 0; q < heights.Length; q++)
                CaveBlock(cave, "Cave_Roof_" + q, edges[q], edges[q + 1], heights[q], roof);
            CaveBlock(cave, "Cave_RightRoof", end, end + 8f, heights[heights.Length - 1], roof);

            switch (index)
            {
                case 1:
                    CaveBlock(cave, "Cave_StartFoundation", 0f, 4f, floor, -1f);
                    CaveBlock(cave, "Cave_LandingFoundation", 5f, 9f, floor, -1f);
                    CaveBlock(cave, "Cave_ExitFoundation", 10f, 13f, floor, 0f);
                    CaveBlock(cave, "Cave_RiseFoundation", 14f, 17f, floor, 1f);
                    CaveBlock(cave, "Cave_ShelfFoundation", 19f, 21f, floor, 0f);
                    CaveBlock(cave, "Cave_FinalFoundation", 23f, 27f, floor, 1f);
                    VisiblePit(cave, hazards, "L01_GapA", 4f, 5f, -1f);
                    VisiblePit(cave, hazards, "L01_GapB", 9f, 10f, -1f);
                    VisiblePit(cave, hazards, "L01_GapC", 13f, 14f, -1f);
                    VisiblePit(cave, hazards, "L01_GapD", 17f, 19f, -1f);
                    VisiblePit(cave, hazards, "L01_GapE", 21f, 23f, -1f);
                    break;
                case 2:
                    CaveBlock(cave, "Cave_StartFoundation", 0f, 4f, floor, -1f);
                    CaveBlock(cave, "Cave_PitSpikeFoundation", 4f, 5f, floor, -1f);
                    CaveBlock(cave, "Cave_LandingFoundation", 5f, 9f, floor, -1f);
                    CaveBlock(cave, "Cave_ExitFoundation", 10f, 13f, floor, 0f);
                    CaveBlock(cave, "Cave_SecondPitFoundation", 13f, 14f, floor, 0f);
                    CaveBlock(cave, "Cave_BridgeFoundation", 14f, 21f, floor, 0f);
                    CaveBlock(cave, "Cave_RecoveryFoundation", 23f, 26f, floor, 1f);
                    CaveBlock(cave, "Cave_FinalFoundation", 28f, 32f, floor, 1f);
                    VisiblePit(cave, hazards, "L02_GapA", 9f, 10f, -1f);
                    VisiblePit(cave, hazards, "L02_GapB", 21f, 23f, -1f);
                    VisiblePit(cave, hazards, "L02_GapC", 26f, 28f, -1f);
                    break;
                case 3:
                    CaveBlock(cave, "Cave_StartFoundation", 0f, 4f, floor, -1f);
                    CaveBlock(cave, "Cave_LandingFoundation", 5f, 9f, floor, -1f);
                    CaveBlock(cave, "Cave_ExitFoundation", 10f, 13f, floor, 0f);
                    CaveBlock(cave, "Cave_SecondSawFoundation", 14f, 19f, floor, 0f);
                    CaveBlock(cave, "Cave_UpperFoundation", 21f, 24f, floor, 1f);
                    CaveBlock(cave, "Cave_FinalFoundation", 26f, 29f, floor, 1f);
                    VisiblePit(cave, hazards, "L03_GapA", 4f, 5f, -1f);
                    VisiblePit(cave, hazards, "L03_GapB", 9f, 10f, -1f);
                    VisiblePit(cave, hazards, "L03_GapC", 13f, 14f, -1f);
                    VisiblePit(cave, hazards, "L03_GapD", 19f, 21f, -1f);
                    VisiblePit(cave, hazards, "L03_GapE", 24f, 26f, -1f);
                    break;
                case 4:
                    CaveBlock(cave, "Cave_FloorFoundation", 0f, 13f, floor, -1f);
                    CaveBlock(cave, "Cave_SecondFloorFoundation", 14f, 25f, floor, 0f);
                    VisiblePit(cave, hazards, "L04_GapA", 13f, 14f, -1f);
                    break;
                case 5:
                    CaveBlock(cave, "Cave_StartFoundation", 0f, 3f, floor, -1f);
                    CaveBlock(cave, "Cave_PitSpikeFoundation", 3f, 19f, floor, -1f);
                    CaveBlock(cave, "Cave_ExitFoundation", 19f, 23f, floor, -1f);
                    CaveBlock(cave, "Cave_SecondPitFoundation", 23f, 35f, floor, -1f);
                    CaveBlock(cave, "Cave_FinalFoundation", 35f, 41f, floor, 1f);
                    break;
                case 6:
                    CaveBlock(cave, "Cave_StartFoundation", 0f, 3f, floor, -2f);
                    CaveBlock(cave, "Cave_FinalFoundation", 37f, 42f, floor, 4f);
                    VisiblePit(cave, hazards, "L06_FloorSpikes", 3f, 37f, -2f);
                    break;
                case 7:
                    CaveBlock(cave, "Cave_StartFoundation", 0f, 3f, floor, -1f);
                    CaveBlock(cave, "Cave_WallFoundation", 4f, 6f, floor, 0f);
                    CaveBlock(cave, "Cave_RunFoundation", 6f, 11f, floor, 2f);
                    CaveBlock(cave, "Cave_PitSpikeFoundation", 11f, 17f, floor, -1f);
                    CaveBlock(cave, "Cave_ExitFoundation", 17f, 21f, floor, 2f);
                    CaveBlock(cave, "Cave_SecondLaserFoundation", 22f, 31f, floor, 4f);
                    CaveBlock(cave, "Cave_SecondPitFoundation", 31f, 37f, floor, -1f);
                    CaveBlock(cave, "Cave_FinalFoundation", 37f, 44f, floor, 4f);
                    VisiblePit(cave, hazards, "L07_GapA", 3f, 4f, -1f);
                    VisiblePit(cave, hazards, "L07_GapB", 21f, 22f, -1f);
                    break;
            }
        }

        private static GameObject Triangle(Transform parent, string name, float x, float bottom, float width, float height)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = new Vector3(x, bottom + height / 2, 0);
            obj.transform.localScale = new Vector3(width, height, Depth);
            obj.AddComponent<MeshFilter>().sharedMesh = triangleMesh;
            obj.AddComponent<MeshRenderer>().sharedMaterial = hazardMaterial;
            PolygonCollider2D collider2D = obj.AddComponent<PolygonCollider2D>();
            collider2D.points = new[] { new Vector2(-0.5f, -0.5f), new Vector2(0.5f, -0.5f), new Vector2(0, 0.5f) };
            collider2D.isTrigger = true;
            GameObject collision3D = new GameObject("Collision3D");
            collision3D.transform.SetParent(obj.transform, false);
            MeshCollider collider3D = collision3D.AddComponent<MeshCollider>();
            collider3D.sharedMesh = triangleMesh;
            collider3D.convex = true;
            collider3D.isTrigger = true;
            obj.AddComponent<SleepwalkingLevelVisualState>();
            return obj;
        }

        private static void Spikes(Transform parent, string id, float left, float right, float top, int count)
        {
            float width = (right - left) / count;
            for (int q = 0; q < count; q++)
            {
                GameObject spike = Triangle(parent, id + "_" + (q + 1), left + (q + 0.5f) * width, top, width, 0.27f);
                spike.AddComponent<SleepwalkingLevelFeature>().Configure(LevelFeatureKind.Spikes, id);
            }
        }

        private static void Saw(Transform parent, string id, float x, float y, float diameter = 0.75f)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = id;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = new Vector3(x, y, 0);
            obj.transform.localScale = new Vector3(diameter, diameter, Depth);
            obj.GetComponent<MeshRenderer>().sharedMaterial = hazardMaterial;
            Object.DestroyImmediate(obj.GetComponent<SphereCollider>());
            obj.AddComponent<CircleCollider2D>().isTrigger = true;
            GameObject collision3D = new GameObject("Collision3D");
            collision3D.transform.SetParent(obj.transform, false);
            collision3D.AddComponent<SphereCollider>().isTrigger = true;
            obj.AddComponent<SleepwalkingLevelFeature>().Configure(LevelFeatureKind.Saw, id);
            VisualCube(obj.transform, "Saw_Hub", new Vector3(0, 0, -0.7f), new Vector3(0.25f, 0.25f, 0.06f), safeFaceMaterial);
            obj.AddComponent<SleepwalkingLevelVisualState>();
        }

        private static void Laser(Transform parent, string id, float x, float baseBottom, float beamTop)
        {
            GameObject baseObj = Triangle(parent, id + "_Base", x, baseBottom, 0.8f, 0.65f);
            baseObj.AddComponent<SleepwalkingLevelFeature>().Configure(LevelFeatureKind.LaserBase, id);
            float beamBottom = baseBottom + 0.65f;
            GameObject beam = Cube(parent, id + "_Beam", x, (beamBottom + beamTop) / 2, 0.2f,
                beamTop - beamBottom, hazardMaterial, true);
            beam.AddComponent<SleepwalkingLevelFeature>().Configure(LevelFeatureKind.LaserBeam, id, beamPixels: 3.5f);
            beam.AddComponent<SleepwalkingLevelVisualState>();
        }

        private static void Timed(Transform parent, string id, float x, float top, float phase)
        {
            GameObject obj = Cube(parent, id, x, top - 0.5f, 2f, 1f, mechanismMaterial);
            obj.AddComponent<SleepwalkingTileSurface>();
            obj.AddComponent<SleepwalkingLevelFeature>().Configure(LevelFeatureKind.TimedPlatform, id, duration: 5.5f, cycle: 7, phase: phase);
            MechanismCues(obj.transform, true);
            obj.AddComponent<SleepwalkingLevelVisualState>();
        }

        private static void Moving(Transform parent, string id, float x, float top, Vector2 offset, float cycle)
        {
            GameObject obj = Cube(parent, id, x, top - 0.5f, 2f, 1f, mechanismMaterial);
            obj.AddComponent<SleepwalkingTileSurface>();
            obj.AddComponent<SleepwalkingLevelFeature>().Configure(LevelFeatureKind.MovingPlatform, id, movement: offset, cycle: cycle);
            MechanismCues(obj.transform, false);
        }

        private static void MechanismCues(Transform parent, bool timed)
        {
            if (timed)
            {
                for (int q = -1; q <= 1; q++)
                    VisualCube(parent, "Timed_Pulse_" + q, new Vector3(q * 0.3f, 0.32f, -0.7f),
                        new Vector3(0.18f, 0.38f, 0.06f), buttonMaterial);
                return;
            }

            VisualCube(parent, "Moving_BlueEdge", new Vector3(0, 0.32f, -0.7f),
                new Vector3(1, 0.38f, 0.06f), buttonMaterial);
            VisualCube(parent, "Moving_DirectionLeft", new Vector3(-0.32f, -0.08f, -0.72f),
                new Vector3(0.12f, 0.4f, 0.06f), safeEdgeMaterial);
            VisualCube(parent, "Moving_DirectionRight", new Vector3(0.32f, -0.08f, -0.72f),
                new Vector3(0.12f, 0.4f, 0.06f), safeEdgeMaterial);
        }

        private static void MoverWaitCue()
        {
            // Неподвижная подсказка остаётся у старта, когда платформа уходит за край камеры.
            for (int q = 0; q < 3; q++)
                VisualCube(readability, "L06_MoverWait_Dot_" + q,
                    new Vector3(2.04f + q * 0.21f, -0.55f, -0.8f),
                    new Vector3(0.13f, 0.13f, 0.06f), buttonMaterial);
            VisualCube(readability, "L06_MoverWait_ChevronTopOuter", new Vector3(2.60f, -0.31f, -0.8f),
                new Vector3(0.14f, 0.14f, 0.06f), safeEdgeMaterial);
            VisualCube(readability, "L06_MoverWait_ChevronTopInner", new Vector3(2.75f, -0.43f, -0.8f),
                new Vector3(0.14f, 0.14f, 0.06f), safeEdgeMaterial);
            VisualCube(readability, "L06_MoverWait_ChevronTip", new Vector3(2.90f, -0.55f, -0.8f),
                new Vector3(0.14f, 0.14f, 0.06f), safeEdgeMaterial);
            VisualCube(readability, "L06_MoverWait_ChevronBottomInner", new Vector3(2.75f, -0.67f, -0.8f),
                new Vector3(0.14f, 0.14f, 0.06f), safeEdgeMaterial);
            VisualCube(readability, "L06_MoverWait_ChevronBottomOuter", new Vector3(2.60f, -0.79f, -0.8f),
                new Vector3(0.14f, 0.14f, 0.06f), safeEdgeMaterial);
        }

        private static void Button(Transform parent, string id, string target, float x, float top, float seconds)
        {
            GameObject obj = Cube(parent, id, x, top + 0.12f, 0.55f, 0.24f, buttonMaterial, true);
            obj.AddComponent<SleepwalkingLevelFeature>().Configure(LevelFeatureKind.Button, id, target, seconds);
            VisualCube(obj.transform, "Button_Light", new Vector3(0, 0, -0.7f),
                new Vector3(0.32f, 0.48f, 0.06f), safeEdgeMaterial);
        }

        private static Transform Marker(Transform parent, string name, float x, float y)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = new Vector3(x, y, 0);
            return obj.transform;
        }

        private static Transform Bed(Transform parent, float x, float floorTop, float visualOffset)
        {
            GameObject bed = new GameObject("BedExit");
            bed.transform.SetParent(parent, false);
            bed.transform.localPosition = new Vector3(x, floorTop, 0);
            GameObject collision3D = new GameObject("Collision3D");
            collision3D.transform.SetParent(bed.transform, false);
            BoxCollider box = collision3D.AddComponent<BoxCollider>();
            box.size = new Vector3(1.4f, 1.5f, Depth);
            box.center = new Vector3(0, 0.75f, 0);
            box.isTrigger = true;
            BoxCollider2D box2D = bed.AddComponent<BoxCollider2D>();
            box2D.size = new Vector2(1.4f, 1.5f);
            box2D.offset = new Vector2(0, 0.75f);
            box2D.isTrigger = true;
            bed.AddComponent<SleepwalkingLevelFeature>().Configure(LevelFeatureKind.Exit, "BedExit");

            VisualCube(bed.transform, "Bed_Silhouette", new Vector3(0, 0.5f + visualOffset, 0.2f),
                new Vector3(1.5f, 0.9f, 0.06f), safeFaceMaterial);
            GameObject frame = Cube(bed.transform, "Frame", 0, 0.2f, 1.25f, 0.35f, exitMaterial, true);
            GameObject pillow = Cube(bed.transform, "Pillow", -0.42f, 0.46f, 0.28f, 0.22f, safeEdgeMaterial, true);
            VisualCube(bed.transform, "Bed_Headboard", new Vector3(-0.58f, 0.56f + visualOffset, -0.56f),
                new Vector3(0.16f, 0.96f, 0.07f), exitMaterial);
            VisualCube(bed.transform, "Bed_Footboard", new Vector3(0.59f, 0.39f + visualOffset, -0.56f),
                new Vector3(0.12f, 0.62f, 0.07f), exitMaterial);
            VisualCube(bed.transform, "Bed_Blanket", new Vector3(0.17f, 0.39f + visualOffset, -0.58f),
                new Vector3(0.68f, 0.23f, 0.07f), safeEdgeMaterial);
            if (visualOffset != 0)
            {
                // Сдвигаем только вид кровати: прежние триггеры L04 остаются на месте.
                frame.GetComponent<MeshRenderer>().enabled = false;
                pillow.GetComponent<MeshRenderer>().enabled = false;
                VisualCube(bed.transform, "Frame_SeatedVisual", new Vector3(0, 0.2f + visualOffset, -0.54f),
                    new Vector3(1.25f, 0.35f, 0.06f), exitMaterial);
                VisualCube(bed.transform, "Pillow_SeatedVisual", new Vector3(-0.42f, 0.46f + visualOffset, -0.55f),
                    new Vector3(0.28f, 0.22f, 0.06f), safeEdgeMaterial);
            }
            return bed.transform;
        }

        private static void Layout1(Transform g, Transform h, Transform m)
        {
            Platform(g, "Start", 0, 4, 0);
            Platform(g, "JumpLanding", 5, 9, 0);
            Platform(g, "ExitRun", 10, 13, 1);
            Platform(g, "RiseA", 14, 17, 2);
            Platform(g, "GapShelf", 19, 21, 1);
            Platform(g, "FinalRun", 23, 27, 2);
        }

        private static void Layout2(Transform g, Transform h, Transform m)
        {
            Platform(g, "Start", 0, 4, 0);
            Platform(g, "SpikeLanding", 5, 9, 0);
            Platform(g, "ExitRun", 10, 13, 1);
            Spikes(h, "L02_PitSpikes", 4f, 5f, -1f, 2);
            Spikes(h, "L02_FloorSpikes", 6.5f, 7.4f, 0f, 2);
            Platform(g, "SpikeBridge", 14, 21, 1);
            Platform(g, "HighRecovery", 23, 26, 2);
            Platform(g, "FinalRun", 28, 32, 2);
            Spikes(h, "L02_SecondPitSpikes", 13f, 14f, 0f, 2);
            Spikes(h, "L02_BridgeSpikes", 18f, 19f, 1f, 2);
        }

        private static void Layout3(Transform g, Transform h, Transform m)
        {
            Platform(g, "Start", 0, 4, 0);
            Platform(g, "SawRun", 5, 9, 0);
            Platform(g, "ExitRun", 10, 13, 1);
            Saw(h, "L03_Saw", 7f, 0f);
            Platform(g, "SecondSawRun", 14, 19, 1);
            Platform(g, "UpperLanding", 21, 24, 2);
            Platform(g, "FinalRun", 26, 29, 2);
            Saw(h, "L03_SecondSaw", 16.5f, 1f);
        }

        private static void Layout4(Transform g, Transform h, Transform m)
        {
            Platform(g, "LaserRun", 0, 13, 0);
            Platform(g, "LaserCeiling", 7, 8, 4);
            Button(m, "L04_ButtonLaser", "L04_Laser", 3, 0, 7);
            Laser(h, "L04_Laser", 7.5f, 0, 3f);
            Platform(g, "SecondLaserRun", 14, 25, 1);
            Platform(g, "SecondLaserCeiling", 20, 22, 5, 1);
            Button(m, "L04_ButtonSecond", "L04_SecondLaser", 15.5f, 1f, 7);
            Laser(h, "L04_SecondLaser", 21f, 1f, 4f);
        }

        private static void Layout5(Transform g, Transform h, Transform m)
        {
            Platform(g, "Start", 0, 3, 0);
            Timed(m, "L05_TimedA", 5, 2, 0);
            Timed(m, "L05_TimedB", 8, 4, 0.3f);
            Timed(m, "L05_TimedC", 16, 2, 0);
            Platform(g, "ExitRun", 19, 23, 0);
            Spikes(h, "L05_PitSpikes", 3f, 19f, -1f, 32);
            Timed(m, "L05_TimedD", 25, 2, 0.3f);
            Timed(m, "L05_TimedE", 28, 4, 0f);
            Platform(g, "FinalRun", 35, 41, 2);
            Spikes(h, "L05_SecondPitSpikes", 23f, 35f, -1f, 24);
        }

        private static void Layout6(Transform g, Transform h, Transform m)
        {
            Platform(g, "Start", 0, 3, -1);
            MoverWaitCue();
            Moving(m, "L06_Horizontal", 5, 1, new Vector2(9, 0), 8);
            Platform(g, "LiftLanding", 16, 18, 0);
            Moving(m, "L06_Vertical", 19, 0, new Vector2(0, 2), 4);
            Platform(g, "HighExit", 21, 23, 4);
            Moving(m, "L06_FinalBridge", 25, 3, new Vector2(10, 0), 6);
            Platform(g, "FinalRun", 37, 42, 5);
        }

        private static void Layout7(Transform g, Transform h, Transform m)
        {
            Platform(g, "Start", 0, 3, 0);
            Platform(g, "WallGrabLedge", 4, 6, 3, 3);
            Platform(g, "LaserRun", 6, 11, 3);
            Platform(g, "DashLanding", 17, 21, 3);
            Platform(g, "SawRoof", 18, 20, 6, 1);
            Platform(g, "LaserCeiling", 8, 9, 6);
            Button(m, "L07_ButtonA", "L07_Laser", 5, 3, 8);
            Laser(h, "L07_Laser", 8.5f, 3, 5f);
            Button(m, "L07_ButtonB", "L07_Saw", 10, 3, 8);
            Saw(h, "L07_Saw", 19f, 3f, 2f);
            Spikes(h, "L07_PitSpikes", 11f, 17f, -1f, 12);
            Platform(g, "SecondLaserRun", 22, 31, 5);
            Platform(g, "SecondLaserCeiling", 27, 29, 10, 1);
            Button(m, "L07_ButtonC", "L07_SecondLaser", 23.5f, 5f, 8);
            Laser(h, "L07_SecondLaser", 28f, 5f, 9f);
            Platform(g, "FinalRun", 37, 44, 5);
            Spikes(h, "L07_SecondPitSpikes", 31f, 37f, -1f, 12);
        }
    }
}
