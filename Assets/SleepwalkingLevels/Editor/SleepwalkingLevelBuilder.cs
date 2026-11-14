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

            float resetWidth = index == 6 || index == 7 ? 23 : 13;
            GameObject fallReset = new GameObject("FallReset");
            fallReset.transform.SetParent(hazards, false);
            fallReset.transform.localPosition = new Vector3(resetWidth / 2, -3.2f, 0);
            fallReset.transform.localScale = new Vector3(resetWidth, 0.3f, Depth);
            fallReset.AddComponent<BoxCollider2D>().isTrigger = true;
            GameObject reset3D = new GameObject("Collision3D");
            reset3D.transform.SetParent(fallReset.transform, false);
            reset3D.AddComponent<BoxCollider>().isTrigger = true;
            fallReset.AddComponent<SleepwalkingLevelFeature>().Configure(LevelFeatureKind.FallReset, "FallReset");

            Transform start = Marker(markers, "PlayerStart", 1f, index == 6 ? -0.8f : 0.1f);
            float exitX = index == 7 ? 20.1f : index == 6 ? 17.7f : index == 4 ? 10.5f : 11.0f;
            float exitY = index == 5 ? 5.2f : index == 6 ? 5.05f : index == 7 ? 2.6f : 0.2f;
            Transform exit = Bed(markers, exitX, exitY, index == 4 ? -0.2f : 0);
            root.AddComponent<SleepwalkingLevelDefinition>().Configure(index, DreamStyle.Meat, start, exit);

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

        private static void Platform(Transform parent, string name, float left, float right, float top, float height = 0.7f, bool oneWay = false)
        {
            GameObject block = Cube(parent, name, (left + right) / 2, top - height / 2, right - left, height, floorMaterial);
            if (oneWay) MakeOneWay(block);
            block.AddComponent<SleepwalkingTileSurface>();
            VisualCube(readability, name + "_SafeEdge", new Vector3((left + right) / 2, top - 0.015f, -0.55f),
                new Vector3(right - left, 0.04f, 0.05f), safeEdgeMaterial);
            if (name == "WallGrabLedge")
                VisualCube(readability, "WallGrip_Cue", new Vector3(left + 0.05f, top - height / 2, -0.58f),
                    new Vector3(0.1f, height, 0.05f), safeEdgeMaterial);
        }

        private static void MakeOneWay(GameObject block)
        {
            // Верхняя опора сохраняется, а боковая грань не должна давать короткий путь к следующей ступени.
            block.GetComponent<BoxCollider2D>().usedByEffector = true;
            PlatformEffector2D effector = block.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 90f;
            effector.sideArc = 0f;
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

        private static void Spikes(Transform parent, string id, float left, float top, int count)
        {
            for (int q = 0; q < count; q++)
            {
                GameObject spike = Triangle(parent, id + "_" + (q + 1), left + q * 0.45f + 0.225f, top, 0.45f, 0.48f);
                spike.AddComponent<SleepwalkingLevelFeature>().Configure(LevelFeatureKind.Spikes, id);
            }
        }

        private static void Saw(Transform parent, string id, float x, float y)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = id;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = new Vector3(x, y, 0);
            obj.transform.localScale = new Vector3(0.75f, 0.75f, Depth);
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

        private static void Timed(Transform parent, string id, float x, float top, float phase, bool oneWay = false)
        {
            GameObject obj = Cube(parent, id, x, top - 0.15f, 1.3f, 0.3f, mechanismMaterial);
            if (oneWay) MakeOneWay(obj);
            obj.AddComponent<SleepwalkingTileSurface>();
            obj.AddComponent<SleepwalkingLevelFeature>().Configure(LevelFeatureKind.TimedPlatform, id, duration: 2.8f, cycle: 4, phase: phase);
            MechanismCues(obj.transform, true);
            obj.AddComponent<SleepwalkingLevelVisualState>();
        }

        private static void Moving(Transform parent, string id, float x, float top, Vector2 offset, float cycle, bool oneWay = false)
        {
            GameObject obj = Cube(parent, id, x, top - 0.15f, 1.6f, 0.3f, mechanismMaterial);
            if (oneWay) MakeOneWay(obj);
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
            Platform(g, "Start", 0, 3.8f, 0);
            Platform(g, "JumpLanding", 5.2f, 8.4f, 0);
            Platform(g, "ExitRun", 9.5f, 12, 0.2f);
        }

        private static void Layout2(Transform g, Transform h, Transform m)
        {
            Platform(g, "Start", 0, 3.5f, 0);
            Platform(g, "SpikeLanding", 4.7f, 8.2f, 0);
            Platform(g, "ExitRun", 9.3f, 12, 0.2f);
            Spikes(h, "L02_PitSpikes", 3.65f, -0.8f, 2);
            Spikes(h, "L02_FloorSpikes", 6.15f, 0, 2);
        }

        private static void Layout3(Transform g, Transform h, Transform m)
        {
            Platform(g, "Start", 0, 3.6f, 0);
            Platform(g, "SawRun", 4.8f, 8.4f, 0);
            Platform(g, "ExitRun", 9.5f, 12, 0.2f);
            Saw(h, "L03_Saw", 6.45f, 0.39f);
        }

        private static void Layout4(Transform g, Transform h, Transform m)
        {
            Platform(g, "LaserRun", 0, 11.6f, 0);
            Platform(g, "LaserCeiling", 5.5f, 6.5f, 3.2f, 0.3f);
            Button(m, "L04_ButtonLaser", "L04_Laser", 3.1f, 0, 7);
            Laser(h, "L04_Laser", 6, 0, 2.9f);
        }

        private static void Layout5(Transform g, Transform h, Transform m)
        {
            Platform(g, "Start", 0, 3.2f, 0);
            Timed(m, "L05_TimedA", 4.0f, 1.3f, 0, true);
            Timed(m, "L05_TimedB", 5.65f, 2.6f, 0.3f, true);
            Timed(m, "L05_TimedC", 7.3f, 3.9f, 0, true);
            Platform(g, "ExitRun", 8.5f, 12, 5.2f, oneWay: true);
            Spikes(h, "L05_PitSpikes", 3.25f, -1.2f, 11);
        }

        private static void Layout6(Transform g, Transform h, Transform m)
        {
            Platform(g, "Start", 0, 3, -0.9f);
            MoverWaitCue();
            Moving(m, "L06_Horizontal", 4.4f, 0.4f, new Vector2(8.8f, 0), 8, true);
            Platform(g, "LiftLanding", 14.5f, 15.9f, 2.35f, oneWay: true);
            Moving(m, "L06_Vertical", 16.15f, 2.5f, new Vector2(0, 1.5f), 4, true);
            Platform(g, "HighExit", 17, 18.5f, 5.05f, oneWay: true);
        }

        private static void Layout7(Transform g, Transform h, Transform m)
        {
            Platform(g, "Start", 0, 3.1f, 0);
            Platform(g, "WallGrabLedge", 3.7f, 5.3f, 2.45f, 2.45f);
            Platform(g, "LaserRun", 5.3f, 9.7f, 2.45f);
            Platform(g, "DashLanding", 18.5f, 21.1f, 2.6f);
            Platform(g, "LaserCeiling", 6.9f, 7.9f, 5.4f, 0.3f);
            Button(m, "L07_ButtonA", "L07_Laser", 4.7f, 2.45f, 8);
            Laser(h, "L07_Laser", 7.4f, 2.45f, 5.1f);
            Button(m, "L07_ButtonB", "L07_Saw", 9.0f, 2.45f, 8);
            Saw(h, "L07_Saw", 19.0f, 3.0f);
            Spikes(h, "L07_PitSpikes", 9.9f, -0.8f, 20);
        }
    }
}
