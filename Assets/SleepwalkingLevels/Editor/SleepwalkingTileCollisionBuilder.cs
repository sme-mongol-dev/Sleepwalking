using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Sleepwalking.Levels.Editor
{
    public static class SleepwalkingTileCollisionBuilder
    {
        const int Cell = 32;
        const int Step = 2;
        const int Samples = Cell / Step;

        struct Surface
        {
            public SleepwalkingTileSurface Tile;
            public BoxCollider2D Box;
            public RectInt Cells;
            public bool IsStatic;
        }

        [MenuItem("Sleepwalking/Levels/Shape Solid Tile Colliders")]
        public static void ShapeSavedPrefabs()
        {
            string[] paths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/SleepwalkingLevels/Prefabs" });
            int changed = 0;
            foreach (string guid in paths)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith("_DreamGeometry.prefab", StringComparison.Ordinal)) continue;
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (Shape(root) > 0)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        changed++;
                    }
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            Debug.Log("Shaped solid tile colliders in " + changed + " level prefabs.");
        }

        public static int Shape(GameObject root)
        {
            Transform geometry = root.transform.Find("Geometry");
            if (geometry == null) throw new InvalidOperationException("Level has no Geometry group: " + root.name);
            List<Surface> surfaces = new List<Surface>();
            HashSet<Vector2Int> staticCells = new HashSet<Vector2Int>();
            foreach (SleepwalkingTileSurface tile in root.GetComponentsInChildren<SleepwalkingTileSurface>(true))
            {
                BoxCollider2D box = tile.GetComponent<BoxCollider2D>();
                if (box == null) continue;
                RectInt cells = GridRect(box, tile.name);
                bool isStatic = tile.transform.IsChildOf(geometry);
                surfaces.Add(new Surface { Tile = tile, Box = box, Cells = cells, IsStatic = isStatic });
                if (isStatic)
                    for (int y = cells.yMin; y < cells.yMax; y++)
                    for (int x = cells.xMin; x < cells.xMax; x++)
                        staticCells.Add(new Vector2Int(x, y));
            }

            foreach (Surface surface in surfaces)
            {
                HashSet<Vector2Int> neighbors = surface.IsStatic ? staticCells : OwnCells(surface.Cells);
                List<Vector2Int[]> outlines = Outlines(surface.Cells, neighbors,
                    surface.IsStatic ? 2 : 0);
                PolygonCollider2D polygon = surface.Tile.gameObject.AddComponent<PolygonCollider2D>();
                polygon.enabled = surface.Box.enabled;
                polygon.isTrigger = surface.Box.isTrigger;
                polygon.sharedMaterial = surface.Box.sharedMaterial;
                polygon.usedByEffector = surface.Box.usedByEffector;
                polygon.pathCount = outlines.Count;
                for (int q = 0; q < outlines.Count; q++)
                {
                    Vector2[] points = new Vector2[outlines[q].Length];
                    for (int e = 0; e < points.Length; e++)
                    {
                        Vector2Int point = outlines[q][e];
                        Vector3 world = new Vector3(surface.Cells.xMin * Cell + point.x * Step,
                            surface.Cells.yMin * Cell + point.y * Step, surface.Tile.transform.position.z);
                        points[e] = surface.Tile.transform.InverseTransformPoint(world);
                    }
                    polygon.SetPath(q, points);
                }
                UnityEngine.Object.DestroyImmediate(surface.Box);
            }
            return surfaces.Count;
        }

        static RectInt GridRect(BoxCollider2D box, string name)
        {
            Vector2 half = box.size * .5f;
            Vector3 a = box.transform.TransformPoint(box.offset - half);
            Vector3 b = box.transform.TransformPoint(box.offset + half);
            float x0 = Mathf.Min(a.x, b.x) / Cell;
            float x1 = Mathf.Max(a.x, b.x) / Cell;
            float y0 = Mathf.Min(a.y, b.y) / Cell;
            float y1 = Mathf.Max(a.y, b.y) / Cell;
            int left = Mathf.RoundToInt(x0);
            int right = Mathf.RoundToInt(x1);
            int bottom = Mathf.RoundToInt(y0);
            int top = Mathf.RoundToInt(y1);
            if (Mathf.Abs(x0 - left) > .001f || Mathf.Abs(x1 - right) > .001f ||
                Mathf.Abs(y0 - bottom) > .001f || Mathf.Abs(y1 - top) > .001f)
                throw new InvalidOperationException("Tile collider is off the 32-unit grid: " + name);
            return new RectInt(left, bottom, right - left, top - bottom);
        }

        static HashSet<Vector2Int> OwnCells(RectInt rect)
        {
            HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
            for (int y = rect.yMin; y < rect.yMax; y++)
            for (int x = rect.xMin; x < rect.xMax; x++)
                cells.Add(new Vector2Int(x, y));
            return cells;
        }

        static List<Vector2Int[]> Outlines(RectInt rect, HashSet<Vector2Int> neighbors,
            int cornerCut)
        {
            int width = rect.width * Samples;
            int height = rect.height * Samples;
            bool[] filled = new bool[width * height];
            for (int cy = rect.yMin; cy < rect.yMax; cy++)
            for (int cx = rect.xMin; cx < rect.xMax; cx++)
            {
                Vector2Int cell = new Vector2Int(cx, cy);
                bool up = neighbors.Contains(cell + Vector2Int.up);
                bool right = neighbors.Contains(cell + Vector2Int.right);
                bool down = neighbors.Contains(cell + Vector2Int.down);
                bool left = neighbors.Contains(cell + Vector2Int.left);
                bool ne = neighbors.Contains(cell + new Vector2Int(1, 1));
                bool se = neighbors.Contains(cell + new Vector2Int(1, -1));
                bool sw = neighbors.Contains(cell + new Vector2Int(-1, -1));
                bool nw = neighbors.Contains(cell + new Vector2Int(-1, 1));
                int diagonals = (ne ? 1 : 0) + (se ? 1 : 0) + (sw ? 1 : 0) + (nw ? 1 : 0);
                for (int y = 0; y < Samples; y++)
                for (int x = 0; x < Samples; x++)
                {
                    // Ровные верх и бок нужны для точных прыжков и wall jump.
                    // Срезаем только прозрачные пиксели открытого угла, по 2-пиксельной сетке.
                    bool trim = false;
                    if (!up && !left && x + Samples - 1 - y < cornerCut) trim = true;
                    if (!up && !right && Samples - 1 - x + Samples - 1 - y < cornerCut) trim = true;
                    if (!down && !left && x + y < cornerCut) trim = true;
                    if (!down && !right && Samples - 1 - x + y < cornerCut) trim = true;
                    if (up && right && down && left && diagonals == 3)
                    {
                        if (!ne && Samples - 1 - x + Samples - 1 - y < 1) trim = true;
                        if (!se && Samples - 1 - x + y < 1) trim = true;
                        if (!sw && x + y < 1) trim = true;
                        if (!nw && x + Samples - 1 - y < 1) trim = true;
                    }
                    int px = (cx - rect.xMin) * Samples + x;
                    int py = (cy - rect.yMin) * Samples + y;
                    filled[py * width + px] = !trim;
                }
            }

            Dictionary<int, int> next = new Dictionary<int, int>();
            int stride = width + 1;
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                if (!filled[y * width + x]) continue;
                if (y == 0 || !filled[(y - 1) * width + x]) Add(next, y * stride + x, y * stride + x + 1);
                if (x == width - 1 || !filled[y * width + x + 1]) Add(next, y * stride + x + 1, (y + 1) * stride + x + 1);
                if (y == height - 1 || !filled[(y + 1) * width + x]) Add(next, (y + 1) * stride + x + 1, (y + 1) * stride + x);
                if (x == 0 || !filled[y * width + x - 1]) Add(next, (y + 1) * stride + x, y * stride + x);
            }

            List<Vector2Int[]> outlines = new List<Vector2Int[]>();
            while (next.Count > 0)
            {
                int first = 0;
                foreach (int key in next.Keys) { first = key; break; }
                int at = first;
                List<Vector2Int> points = new List<Vector2Int>();
                do
                {
                    points.Add(new Vector2Int(at % stride, at / stride));
                    if (!next.TryGetValue(at, out int end))
                        throw new InvalidOperationException("Open tile collider contour");
                    next.Remove(at);
                    at = end;
                } while (at != first);
                for (int q = points.Count - 1; q >= 0; q--)
                {
                    Vector2Int a = points[(q + points.Count - 1) % points.Count];
                    Vector2Int b = points[q];
                    Vector2Int c = points[(q + 1) % points.Count];
                    if ((b.x - a.x) * (c.y - b.y) == (b.y - a.y) * (c.x - b.x)) points.RemoveAt(q);
                }
                if (points.Count < 3) throw new InvalidOperationException("Degenerate tile collider contour");
                outlines.Add(points.ToArray());
            }
            return outlines;
        }

        static void Add(Dictionary<int, int> next, int a, int b)
        {
            if (next.ContainsKey(a)) throw new InvalidOperationException("Ambiguous tile collider contour");
            next.Add(a, b);
        }
    }
}
