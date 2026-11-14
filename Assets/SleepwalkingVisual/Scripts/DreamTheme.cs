using Sleepwalking.Levels;
using UnityEngine;

namespace Sleepwalking.Visual
{
    public enum DreamThemeId { Meat, PostApocalypse, DeepWater, Heaven }

    public sealed class DreamTheme : MonoBehaviour
    {
        public DreamThemeId Id;
        public Material Main;
        public Material Accent;
        public Material Shadow;
        public Material Tiles;

        static readonly Mesh[] tileMeshes = new Mesh[9];

        public void ApplyTo(Transform geometryRoot)
        {
            if (geometryRoot == null) return;
            foreach (Renderer renderer in geometryRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.GetComponentInParent<DreamTheme>() != null) continue;
                Material material = renderer.name.Contains("Accent") ? Accent :
                    renderer.name.Contains("Shadow") ? Shadow : Main;
                if (material == null) continue;
                Material[] slots = renderer.sharedMaterials;
                for (int q = 0; q < slots.Length; q++) slots[q] = material;
                renderer.sharedMaterials = slots;
            }

            if (Tiles == null) return;
            foreach (SleepwalkingTileSurface surface in geometryRoot.parent.GetComponentsInChildren<SleepwalkingTileSurface>(true))
                ApplyTiles(surface, surface.transform.IsChildOf(geometryRoot));
        }

        void ApplyTiles(SleepwalkingTileSurface surface, bool staticGeometry)
        {
            BoxCollider2D shape = surface.Solid;
            if (shape == null) return;
            Transform existing = surface.transform.Find("Dream Tiles");
            if (existing != null)
            {
                foreach (MeshRenderer renderer in existing.GetComponentsInChildren<MeshRenderer>(true))
                    renderer.sharedMaterial = Tiles;
                return;
            }

            Vector2 size = shape.size;
            Vector2 offset = shape.offset;
            float width = Mathf.Abs(surface.transform.lossyScale.x * size.x);
            float height = Mathf.Abs(surface.transform.lossyScale.y * size.y);
            int columns = width < 1.2f ? 1 : Mathf.Max(3, Mathf.CeilToInt(width / .75f));
            int rows = staticGeometry ? Mathf.Max(3, Mathf.CeilToInt(height / .7f)) : 1;
            GameObject layer = new GameObject("Dream Tiles");
            layer.transform.SetParent(surface.transform, false);
            for (int q = 0; q < columns; q++)
            for (int e = 0; e < rows; e++)
            {
                int column = columns == 1 ? 1 : q == 0 ? 0 : q == columns - 1 ? 2 : 1;
                int row = rows == 1 ? 0 : e == 0 ? 0 : e == rows - 1 ? 2 : 1;
                GameObject tile = new GameObject("Tile " + e + "-" + q, typeof(MeshFilter), typeof(MeshRenderer));
                tile.transform.SetParent(layer.transform, false);
                tile.transform.localPosition = new Vector3(
                    offset.x - size.x / 2 + (q + .5f) * size.x / columns,
                    offset.y + size.y / 2 - (e + .5f) * size.y / rows,
                    -.65f);
                tile.transform.localScale = new Vector3(size.x / columns, size.y / rows, 1);
                tile.GetComponent<MeshFilter>().sharedMesh = TileMesh(row, column);
                tile.GetComponent<MeshRenderer>().sharedMaterial = Tiles;
            }
        }

        static Mesh TileMesh(int row, int column)
        {
            int index = row * 3 + column;
            if (tileMeshes[index] != null) return tileMeshes[index];
            // Полпикселя внутри ячейки не даёт соседнему тайлу просочиться на шов.
            float u0 = (column * 32 + .5f) / 96f;
            float u1 = ((column + 1) * 32 - .5f) / 96f;
            float v0 = ((5 - row) * 32 + .5f) / 192f;
            float v1 = ((6 - row) * 32 - .5f) / 192f;
            Mesh mesh = new Mesh();
            mesh.name = "Dream tile " + row + "-" + column;
            mesh.vertices = new[]
            {
                new Vector3(-.5f, -.5f, 0), new Vector3(.5f, -.5f, 0),
                new Vector3(.5f, .5f, 0), new Vector3(-.5f, .5f, 0)
            };
            mesh.uv = new[]
            {
                new Vector2(u0, v0), new Vector2(u1, v0),
                new Vector2(u1, v1), new Vector2(u0, v1)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            tileMeshes[index] = mesh;
            return mesh;
        }
    }
}
