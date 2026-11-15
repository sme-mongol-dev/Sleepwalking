using System.Collections.Generic;
using Sleepwalking.Levels;
using UnityEngine;

namespace Sleepwalking.Visual
{
    public enum DreamThemeId { Meat, PostApocalypse, DeepWater, Heaven }

    [DefaultExecutionOrder(500)]
    public sealed class DreamTheme : MonoBehaviour
    {
        public DreamThemeId Id;
        public Material Main;
        public Material Accent;
        public Material Shadow;
        public Material Tiles;
        public Sprite SpikesArt;
        public Sprite LaserBaseArt;
        public Sprite LaserBeamArt;
        public Sprite SawArt;
        public Sprite ButtonUpArt;
        public Sprite ButtonDownArt;
        public Sprite BedArt;

        readonly List<(Collider2D shape, Renderer[] oldRenderers, SpriteRenderer art)> hazardArt =
            new List<(Collider2D, Renderer[], SpriteRenderer)>();
        readonly List<(SleepwalkingMechanism mechanism, Renderer[] oldRenderers, SpriteRenderer art,
            SpriteRenderer outline)> buttonArt =
            new List<(SleepwalkingMechanism, Renderer[], SpriteRenderer, SpriteRenderer)>();
        readonly List<Renderer> tileBackings = new List<Renderer>();
        readonly List<Renderer> bedBackings = new List<Renderer>();
        readonly List<SpriteRenderer> bedSprites = new List<SpriteRenderer>();
        public void FrameBackdrop(Camera camera)
        {
            if (camera == null) return;
            Transform background = transform.Find("Distant Background Left");
            Transform duplicate = transform.Find("Distant Background Right");
            if (background == null) return;
            Renderer renderer = background.GetComponent<Renderer>();
            Texture texture = renderer != null ? renderer.sharedMaterial.mainTexture : null;
            if (texture == null) return;

            float viewHeight = camera.orthographicSize * 2f + .1f;
            float viewWidth = viewHeight * camera.aspect;
            float imageAspect = (float)texture.width / texture.height;
            float width = Mathf.Max(viewWidth, viewHeight * imageAspect);
            float height = width / imageAspect;
            background.position = new Vector3(camera.transform.position.x,
                camera.transform.position.y, background.position.z);
            Vector3 parentScale = background.parent != null ? background.parent.lossyScale : Vector3.one;
            background.localScale = new Vector3(width / Mathf.Max(.001f, Mathf.Abs(parentScale.x)),
                height / Mathf.Max(.001f, Mathf.Abs(parentScale.y)), 1f);
            if (duplicate != null) duplicate.gameObject.SetActive(false);
        }

        static readonly Mesh[] tileMeshes = new Mesh[9];
        static Sprite buttonOutlineSprite;
        static readonly Dictionary<(Vector2, Vector2, int, int), Mesh> shellMeshes =
            new Dictionary<(Vector2, Vector2, int, int), Mesh>();
        const float GridPitch = 32f;
        static readonly int[] gridTiles = { 16, 13, 14, 7, 12, 11, 1, 4, 15, 9, 10, 8, 3, 6, 2, 5 };

        public void ApplyTo(Transform geometryRoot)
        {
            if (geometryRoot == null) return;
            tileBackings.Clear();
            bedBackings.Clear();
            bedSprites.Clear();
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

            ApplyHazardArt(geometryRoot.parent);
            ApplyButtonArt(geometryRoot.parent);
            ApplyBedArt(geometryRoot);
            Transform oldWallCue = geometryRoot.parent.Find("Readability/WallGrip_Cue");
            if (oldWallCue != null)
                foreach (Renderer renderer in oldWallCue.GetComponentsInChildren<Renderer>(true))
                    renderer.forceRenderingOff = true;
            if (Tiles == null) return;
            if (Mathf.Abs(geometryRoot.parent.lossyScale.x - GridPitch) < .01f &&
                Mathf.Abs(geometryRoot.parent.lossyScale.y - GridPitch) < .01f)
            {
                ApplyGridTiles(geometryRoot);
                return;
            }
            foreach (SleepwalkingTileSurface surface in geometryRoot.parent.GetComponentsInChildren<SleepwalkingTileSurface>(true))
                ApplyTiles(surface, surface.transform.IsChildOf(geometryRoot));
        }

        void ApplyGridTiles(Transform geometryRoot)
        {
            Transform levelRoot = geometryRoot.parent;
            HashSet<Vector2Int> solid = new HashSet<Vector2Int>();
            List<Renderer> staticBackings = new List<Renderer>();
            foreach (SleepwalkingTileSurface surface in levelRoot.GetComponentsInChildren<SleepwalkingTileSurface>(true))
            {
                Collider2D shape = surface.Solid;
                if (shape == null) continue;
                HashSet<Vector2Int> cells = GridCells(shape);
                if (surface.transform.IsChildOf(geometryRoot))
                {
                    solid.UnionWith(cells);
                    Renderer backing = surface.GetComponent<Renderer>();
                    if (backing != null) staticBackings.Add(backing);
                    continue;
                }

                SleepwalkingLevelFeature feature = surface.GetComponent<SleepwalkingLevelFeature>();
                if (feature != null && (feature.Kind == LevelFeatureKind.TimedPlatform ||
                    feature.Kind == LevelFeatureKind.MovingPlatform))
                {
                    Renderer backing = surface.GetComponent<Renderer>();
                    if (backing != null)
                    {
                        backing.enabled = false;
                        backing.forceRenderingOff = true;
                        tileBackings.Add(backing);
                    }
                    foreach (Renderer cue in surface.GetComponentsInChildren<Renderer>(true))
                    {
                        if (cue == backing || cue.name == "Dream Tiles") continue;
                        if (!cue.name.StartsWith("Timed_Pulse_") &&
                            !cue.name.StartsWith("Moving_")) continue;
                        cue.enabled = false;
                        cue.forceRenderingOff = true;
                        tileBackings.Add(cue);
                    }
                }
                Transform existing = surface.transform.Find("Dream Tiles");
                if (existing != null)
                {
                    MeshRenderer renderer = existing.GetComponent<MeshRenderer>();
                    if (renderer != null) renderer.sharedMaterial = Tiles;
                    continue;
                }

                AddGridLayer(surface.transform, "Dream Tiles", cells, shape.enabled);
            }

            Transform staticLayer = levelRoot.Find("Dream Tile Matrix");
            if (staticLayer != null)
            {
                MeshRenderer renderer = staticLayer.GetComponent<MeshRenderer>();
                if (renderer != null) renderer.sharedMaterial = Tiles;
            }
            else
            {
                AddGridLayer(levelRoot, "Dream Tile Matrix", solid, true);
                staticLayer = levelRoot.Find("Dream Tile Matrix");
            }
            MeshFilter matrix = staticLayer != null ? staticLayer.GetComponent<MeshFilter>() : null;
            if (matrix == null || matrix.sharedMesh == null || matrix.sharedMesh.vertexCount == 0) return;
            foreach (Renderer backing in staticBackings)
            {
                backing.enabled = false;
                backing.forceRenderingOff = true;
                tileBackings.Add(backing);
            }
        }

        static HashSet<Vector2Int> GridCells(Collider2D shape)
        {
            HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
            Renderer backing = shape.GetComponent<Renderer>();
            Bounds bounds = backing != null ? backing.bounds : shape.bounds;
            float left = bounds.min.x / GridPitch;
            float right = bounds.max.x / GridPitch;
            float bottom = bounds.min.y / GridPitch;
            float top = bounds.max.y / GridPitch;
            int x0 = Mathf.RoundToInt(left);
            int x1 = Mathf.RoundToInt(right);
            int y0 = Mathf.RoundToInt(bottom);
            int y1 = Mathf.RoundToInt(top);

            // Матрица и коллайдеры должны иметь одну границу клетки.
            if (Mathf.Abs(left - x0) > .001f || Mathf.Abs(right - x1) > .001f ||
                Mathf.Abs(bottom - y0) > .001f || Mathf.Abs(top - y1) > .001f)
            {
                Debug.LogError("Collider is off the 32-unit tile grid: " + shape.name, shape);
                return cells;
            }

            for (int y = y0; y < y1; y++)
            for (int x = x0; x < x1; x++)
                cells.Add(new Vector2Int(x, y));
            return cells;
        }

        void AddGridLayer(Transform parent, string name, HashSet<Vector2Int> cells, bool visible)
        {
            if (cells.Count == 0) return;
            GameObject layer = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            layer.transform.SetParent(parent, false);
            layer.GetComponent<MeshFilter>().sharedMesh = GridMesh(parent, cells);
            MeshRenderer renderer = layer.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = Tiles;
            renderer.enabled = visible;
        }

        static Mesh GridMesh(Transform parent, HashSet<Vector2Int> cells)
        {
            List<Vector2Int> ordered = new List<Vector2Int>(cells);
            ordered.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
            Vector3[] vertices = new Vector3[ordered.Count * 4];
            Vector2[] uv = new Vector2[ordered.Count * 4];
            int[] triangles = new int[ordered.Count * 6];
            for (int q = 0; q < ordered.Count; q++)
            {
                Vector2Int cell = ordered[q];
                int neighbors = 0;
                if (cells.Contains(cell + Vector2Int.up)) neighbors |= 1;
                if (cells.Contains(cell + Vector2Int.right)) neighbors |= 2;
                if (cells.Contains(cell + Vector2Int.down)) neighbors |= 4;
                if (cells.Contains(cell + Vector2Int.left)) neighbors |= 8;
                int tile = gridTiles[neighbors] - 1;
                int rotation = 0;
                if (neighbors == 15)
                {
                    bool northEast = cells.Contains(cell + new Vector2Int(1, 1));
                    bool southEast = cells.Contains(cell + new Vector2Int(1, -1));
                    bool southWest = cells.Contains(cell + new Vector2Int(-1, -1));
                    bool northWest = cells.Contains(cell + new Vector2Int(-1, 1));
                    int diagonals = (northEast ? 1 : 0) + (southEast ? 1 : 0) +
                        (southWest ? 1 : 0) + (northWest ? 1 : 0);
                    if (diagonals == 3)
                    {
                        // Фактурный угол тайла 17 находится на юго-востоке и смотрит в пустую диагональ.
                        tile = 16;
                        rotation = !southEast ? 0 : !southWest ? 1 : !northWest ? 2 : 3;
                    }
                    else if (diagonals == 2)
                    {
                        // У тайла 18 открыты юго-западная и юго-восточная диагонали.
                        if (!southWest && !southEast) { tile = 17; rotation = 0; }
                        else if (!northWest && !southWest) { tile = 17; rotation = 1; }
                        else if (!northWest && !northEast) { tile = 17; rotation = 2; }
                        else if (!northEast && !southEast) { tile = 17; rotation = 3; }
                    }
                }
                int column = tile % 3;
                int row = tile / 3;
                int vertex = q * 4;
                int triangle = q * 6;
                float x0 = cell.x * GridPitch;
                float x1 = x0 + GridPitch;
                float y0 = cell.y * GridPitch;
                float y1 = y0 + GridPitch;
                vertices[vertex] = parent.InverseTransformPoint(new Vector3(x0, y0, -.65f));
                vertices[vertex + 1] = parent.InverseTransformPoint(new Vector3(x1, y0, -.65f));
                vertices[vertex + 2] = parent.InverseTransformPoint(new Vector3(x1, y1, -.65f));
                vertices[vertex + 3] = parent.InverseTransformPoint(new Vector3(x0, y1, -.65f));

                float u0 = (column * 32 + .5f) / 96f;
                float u1 = ((column + 1) * 32 - .5f) / 96f;
                float v0 = ((5 - row) * 32 + .5f) / 192f;
                float v1 = ((6 - row) * 32 - .5f) / 192f;
                Vector2[] corners =
                {
                    new Vector2(u0, v0), new Vector2(u1, v0),
                    new Vector2(u1, v1), new Vector2(u0, v1)
                };
                for (int e = 0; e < 4; e++)
                    uv[vertex + e] = corners[(e + rotation) & 3];
                triangles[triangle] = vertex;
                triangles[triangle + 1] = vertex + 2;
                triangles[triangle + 2] = vertex + 1;
                triangles[triangle + 3] = vertex;
                triangles[triangle + 4] = vertex + 3;
                triangles[triangle + 5] = vertex + 2;
            }

            Mesh mesh = new Mesh { name = "Dream tile matrix " + ordered.Count };
            mesh.indexFormat = ordered.Count * 4 > 65535 ?
                UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        void ApplyHazardArt(Transform levelRoot)
        {
            hazardArt.Clear();
            foreach (SleepwalkingLevelFeature feature in levelRoot.GetComponentsInChildren<SleepwalkingLevelFeature>(true))
            {
                Sprite sprite = feature.Kind == LevelFeatureKind.Spikes ? SpikesArt :
                    feature.Kind == LevelFeatureKind.LaserBase ? LaserBaseArt :
                    feature.Kind == LevelFeatureKind.LaserBeam ? LaserBeamArt :
                    feature.Kind == LevelFeatureKind.Saw ? SawArt : null;
                if (sprite == null) continue;

                Transform existing = feature.transform.Find("Dream Hazard Art");
                GameObject node = existing != null ? existing.gameObject : new GameObject("Dream Hazard Art", typeof(SpriteRenderer));
                if (existing == null) node.transform.SetParent(feature.transform, false);
                float spikeSeatPixels = Id == DreamThemeId.Meat ? 3f :
                    Id == DreamThemeId.PostApocalypse ? 2f :
                    Id == DreamThemeId.DeepWater ? 8f : 6f;
                float spikeArtHeight = Id == DreamThemeId.Meat ? .65f :
                    Id == DreamThemeId.PostApocalypse ? .84f :
                    Id == DreamThemeId.DeepWater ? .68f : .84f;
                float spikeHeight = feature.transform.localScale.y;
                float spikeArtY = feature.Kind == LevelFeatureKind.Spikes ?
                    (spikeArtHeight / 2f - spikeSeatPixels / 32f - spikeHeight / 2f) / spikeHeight : 0f;
                // У нового основания прозрачный верх; видимый кончик совмещён с лучом, низ посажен в тайл пола.
                float laserBaseEmptyPixels = Id == DreamThemeId.Meat ? 8f :
                    Id == DreamThemeId.Heaven ? 12f : 11f;
                float laserBaseSeat = Id == DreamThemeId.Meat ? 5f :
                    Id == DreamThemeId.PostApocalypse ? 1f : 3f;
                float laserBaseHeight = Mathf.Max(.001f, Mathf.Abs(feature.transform.lossyScale.y));
                float laserBaseScale = 32f / (32f - laserBaseEmptyPixels) *
                    (1f + laserBaseSeat / laserBaseHeight);
                float laserBeamPixels = Id == DreamThemeId.Meat ? 11f :
                    Id == DreamThemeId.PostApocalypse ? 14f : 10f;
                // Видимая кромка облачных потолков второго лазера выше физической грани блока.
                float laserBeamReach = Id == DreamThemeId.Heaven &&
                    feature.FeatureId == "L04_SecondLaser" ? 13f :
                    Id == DreamThemeId.Heaven && feature.FeatureId == "L07_SecondLaser" ? 15f :
                    Id == DreamThemeId.DeepWater || Id == DreamThemeId.Heaven ? 7f :
                    Id == DreamThemeId.Meat ? 3f : 2f;
                float laserBeamExtension = laserBeamReach /
                    Mathf.Max(.001f, Mathf.Abs(feature.transform.lossyScale.y));
                float artY = feature.Kind == LevelFeatureKind.LaserBase ?
                    .5f - laserBaseScale * (.5f - laserBaseEmptyPixels / 32f) :
                    feature.Kind == LevelFeatureKind.LaserBeam ? laserBeamExtension / 2f : spikeArtY;
                node.transform.localPosition = new Vector3(0f, artY,
                    feature.Kind == LevelFeatureKind.Saw ? -.5f : -.9f);
                node.transform.localRotation = feature.Kind == LevelFeatureKind.LaserBeam ?
                    Quaternion.Euler(0f, 0f, 90f) : Quaternion.identity;
                node.transform.localScale = feature.Kind == LevelFeatureKind.Spikes ?
                    new Vector3(1f, spikeArtHeight / spikeHeight, 1f) :
                    feature.Kind == LevelFeatureKind.LaserBase ?
                    new Vector3(1f, laserBaseScale, 1f) :
                    feature.Kind == LevelFeatureKind.LaserBeam ?
                    new Vector3(1f + laserBeamExtension, 32f / laserBeamPixels, 1f) : Vector3.one;
                SpriteRenderer art = node.GetComponent<SpriteRenderer>();
                art.sprite = sprite;
                art.color = Color.white;
                art.sortingOrder = feature.Kind == LevelFeatureKind.Saw ? -1 : 10;

                Renderer[] oldRenderers = feature.GetComponentsInChildren<Renderer>(true);
                // Состояние ловушки может снова включить renderer; старый примитив не должен проступить сквозь спрайт.
                foreach (Renderer renderer in oldRenderers)
                    if (renderer != null && renderer != art) renderer.forceRenderingOff = true;
                hazardArt.Add((feature.GetComponent<Collider2D>(), oldRenderers, art));
            }
            UpdateHazardArt();
        }

        void ApplyButtonArt(Transform levelRoot)
        {
            buttonArt.Clear();
            if (ButtonUpArt == null || ButtonDownArt == null) return;
            float cellScale = Mathf.Max(.001f, Mathf.Abs(levelRoot.lossyScale.x));
            foreach (SleepwalkingLevelFeature feature in levelRoot.GetComponentsInChildren<SleepwalkingLevelFeature>(true))
            {
                if (feature.Kind != LevelFeatureKind.Button) continue;
                Transform existing = feature.transform.Find("Dream Button Art");
                GameObject node = existing != null ? existing.gameObject : new GameObject("Dream Button Art", typeof(SpriteRenderer));
                if (existing == null) node.transform.SetParent(feature.transform, false);
                float widthScale = Mathf.Max(.001f, Mathf.Abs(feature.transform.lossyScale.x));
                float heightScale = Mathf.Max(.001f, Mathf.Abs(feature.transform.lossyScale.y));
                // В исходном кадре кнопка занимает лишь нижние 9 из 32 пикселей.
                // Увеличиваем рисунок, оставляя его нижнюю кромку на прежнем полу.
                node.transform.localPosition = new Vector3(0f, .63f * cellScale / heightScale, -.9f);
                node.transform.localRotation = Quaternion.identity;
                node.transform.localScale = new Vector3(.9f * cellScale / widthScale,
                    1.5f * cellScale / heightScale, 1f);
                SpriteRenderer art = node.GetComponent<SpriteRenderer>();
                art.sprite = ButtonUpArt;
                art.color = Color.white;
                art.sortingOrder = 10;
                Transform existingOutline = feature.transform.Find("Dream Button Outline");
                GameObject outlineNode = existingOutline != null ? existingOutline.gameObject :
                    new GameObject("Dream Button Outline", typeof(SpriteRenderer));
                if (existingOutline == null) outlineNode.transform.SetParent(feature.transform, false);
                outlineNode.transform.localPosition = new Vector3(0f, .13f * cellScale / heightScale, -.88f);
                outlineNode.transform.localRotation = Quaternion.identity;
                outlineNode.transform.localScale = new Vector3(.95f * cellScale / widthScale,
                    .50f * cellScale / heightScale, 1f);
                SpriteRenderer outline = outlineNode.GetComponent<SpriteRenderer>();
                outline.sprite = ButtonOutlineSprite();
                outline.sortingOrder = 9;
                buttonArt.Add((feature.GetComponent<SleepwalkingMechanism>(),
                    feature.GetComponentsInChildren<Renderer>(true), art, outline));
            }
            UpdateButtonArt();
        }

        static Sprite ButtonOutlineSprite()
        {
            if (buttonOutlineSprite != null) return buttonOutlineSprite;
            Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            texture.name = "Dream Button Outline";
            texture.filterMode = FilterMode.Point;
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                    texture.SetPixel(x, y, x == 0 || x == 15 || y == 0 || y == 15 ? Color.white : Color.clear);
            texture.Apply();
            buttonOutlineSprite = Sprite.Create(texture, new Rect(0, 0, 16, 16),
                new Vector2(.5f, .5f), 16f);
            buttonOutlineSprite.name = "Dream Button Outline";
            return buttonOutlineSprite;
        }

        void ApplyBedArt(Transform geometryRoot)
        {
            if (BedArt == null) return;
            foreach (SleepwalkingLevelFeature feature in geometryRoot.parent.GetComponentsInChildren<SleepwalkingLevelFeature>(true))
            {
                if (feature.Kind != LevelFeatureKind.Exit) continue;
                Transform bed = feature.transform;
                Transform existing = bed.Find("Dream Bed Art");
                GameObject node = existing != null ? existing.gameObject :
                    new GameObject("Dream Bed Art", typeof(SpriteRenderer));
                if (existing == null) node.transform.SetParent(bed, false);
                SpriteRenderer art = node.GetComponent<SpriteRenderer>();
                art.sprite = BedArt;
                art.color = Color.white;
                art.sortingOrder = 10;
                bedSprites.Add(art);
                node.transform.localRotation = Quaternion.identity;
                node.transform.localScale = Vector3.one;

                float bedX = bed.position.x;
                float floorY = bed.position.y;
                float halfWidth = BedArt.bounds.extents.x * Mathf.Abs(bed.lossyScale.x);
                float halfHeight = BedArt.bounds.extents.y * Mathf.Abs(bed.lossyScale.y);
                Bounds support = default;
                bool foundSupport = false;
                foreach (SleepwalkingTileSurface surface in geometryRoot.GetComponentsInChildren<SleepwalkingTileSurface>(true))
                {
                    Collider2D shape = surface.Solid;
                    if (shape == null || !shape.enabled) continue;
                    Bounds bounds = shape.bounds;
                    if (bedX < bounds.min.x || bedX > bounds.max.x ||
                        bounds.max.y > bed.position.y + bed.lossyScale.y * .25f) continue;
                    if (foundSupport && bounds.max.y <= support.max.y) continue;
                    support = bounds;
                    foundSupport = true;
                }
                if (foundSupport)
                {
                    float left = support.min.x + halfWidth;
                    float right = support.max.x - halfWidth;
                    if (left <= right) bedX = Mathf.Clamp(bedX, left, right);
                    float insetPixels = Id == DreamThemeId.Meat ? 3f :
                        Id == DreamThemeId.PostApocalypse ? 2f :
                        Id == DreamThemeId.DeepWater ? 8f : 6f;
                    floorY = support.max.y - insetPixels * Mathf.Abs(bed.lossyScale.y) / 32f;
                }
                Vector3 center = bed.InverseTransformPoint(new Vector3(bedX, floorY + halfHeight, bed.position.z));
                node.transform.localPosition = new Vector3(center.x, center.y, -.9f);

                foreach (Renderer renderer in bed.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == art) continue;
                    renderer.enabled = false;
                    bedBackings.Add(renderer);
                }
            }
        }

        void LateUpdate()
        {
            UpdateHazardArt();
            UpdateButtonArt();
            foreach (Renderer backing in tileBackings)
                if (backing != null)
                {
                    backing.enabled = false;
                    backing.forceRenderingOff = true;
                }
            foreach (Renderer backing in bedBackings)
                if (backing != null) backing.enabled = false;
            foreach (SpriteRenderer art in bedSprites)
                if (art != null) art.color = Color.white;
        }

        void UpdateButtonArt()
        {
            foreach (var item in buttonArt)
            {
                if (item.art == null) continue;
                foreach (Renderer renderer in item.oldRenderers)
                    if (renderer != null && renderer != item.art && renderer != item.outline)
                        renderer.enabled = false;
                bool pressed = item.mechanism != null && item.mechanism.IsPressed;
                item.art.sprite = pressed ? ButtonDownArt : ButtonUpArt;
                item.art.color = Color.white;
                if (item.outline != null)
                {
                    item.outline.enabled = true;
                    item.outline.color = pressed ? new Color(1f, .88f, .42f, 1f) :
                        Id == DreamThemeId.Heaven ? new Color(.26f, .35f, .53f, 1f) :
                        Id == DreamThemeId.DeepWater ? new Color(.58f, .84f, .87f, 1f) :
                        new Color(.94f, .83f, .71f, 1f);
                }
            }
        }

        void UpdateHazardArt()
        {
            foreach (var item in hazardArt)
            {
                if (item.art == null) continue;
                foreach (Renderer renderer in item.oldRenderers)
                    if (renderer != null && renderer != item.art) renderer.enabled = false;
                item.art.color = Color.white;
                item.art.enabled = item.shape == null || item.shape.enabled;
            }
        }

        void ApplyTiles(SleepwalkingTileSurface surface, bool staticGeometry)
        {
            Collider2D shape = surface.Solid;
            if (shape == null) return;
            Transform existing = surface.transform.Find("Dream Tiles");
            if (existing != null)
            {
                foreach (MeshRenderer renderer in existing.GetComponentsInChildren<MeshRenderer>(true))
                    renderer.sharedMaterial = Tiles;
                return;
            }

            Vector2 size;
            Vector2 offset;
            if (shape is BoxCollider2D box)
            {
                size = box.size;
                offset = box.offset;
            }
            else if (shape is PolygonCollider2D polygon)
            {
                Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
                for (int q = 0; q < polygon.pathCount; q++)
                    foreach (Vector2 point in polygon.GetPath(q))
                    {
                        min = Vector2.Min(min, point);
                        max = Vector2.Max(max, point);
                    }
                size = max - min;
                offset = (min + max) * .5f + polygon.offset;
            }
            else
            {
                Bounds bounds = shape.bounds;
                Vector3 center = surface.transform.InverseTransformPoint(bounds.center);
                size = new Vector2(bounds.size.x / Mathf.Abs(surface.transform.lossyScale.x),
                    bounds.size.y / Mathf.Abs(surface.transform.lossyScale.y));
                offset = new Vector2(center.x, center.y);
            }
            float width = Mathf.Abs(surface.transform.lossyScale.x * size.x);
            float height = Mathf.Abs(surface.transform.lossyScale.y * size.y);
            int columns = width < 1.2f ? 1 : Mathf.Max(3, Mathf.CeilToInt(width / .75f));
            int rows = staticGeometry ? Mathf.Max(3, Mathf.CeilToInt(height / .7f)) : 1;
            if (surface.IsShell)
            {
                GameObject shell = new GameObject("Dream Tiles", typeof(MeshFilter), typeof(MeshRenderer));
                shell.transform.SetParent(surface.transform, false);
                shell.transform.localPosition = new Vector3(0, 0, -.62f);
                shell.GetComponent<MeshFilter>().sharedMesh = ShellMesh(size, offset, columns, rows);
                shell.GetComponent<MeshRenderer>().sharedMaterial = Tiles;
                return;
            }

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

        static Mesh ShellMesh(Vector2 size, Vector2 offset, int columns, int rows)
        {
            var key = (size, offset, columns, rows);
            if (shellMeshes.TryGetValue(key, out Mesh cached) && cached != null) return cached;

            int cells = columns * rows;
            Vector3[] vertices = new Vector3[cells * 4];
            Vector2[] uv = new Vector2[cells * 4];
            int[] triangles = new int[cells * 6];
            for (int q = 0; q < columns; q++)
            for (int e = 0; e < rows; e++)
            {
                int column = columns == 1 ? 1 : q == 0 ? 0 : q == columns - 1 ? 2 : 1;
                int row = rows == 1 ? 0 : e == 0 ? 0 : e == rows - 1 ? 2 : 1;
                int cell = e * columns + q;
                int vertex = cell * 4;
                int triangle = cell * 6;
                float x0 = offset.x - size.x / 2 + q * size.x / columns;
                float x1 = offset.x - size.x / 2 + (q + 1) * size.x / columns;
                float y1 = offset.y + size.y / 2 - e * size.y / rows;
                float y0 = offset.y + size.y / 2 - (e + 1) * size.y / rows;
                vertices[vertex] = new Vector3(x0, y0, 0);
                vertices[vertex + 1] = new Vector3(x1, y0, 0);
                vertices[vertex + 2] = new Vector3(x1, y1, 0);
                vertices[vertex + 3] = new Vector3(x0, y1, 0);

                float u0 = (column * 32 + .5f) / 96f;
                float u1 = ((column + 1) * 32 - .5f) / 96f;
                float v0 = ((5 - row) * 32 + .5f) / 192f;
                float v1 = ((6 - row) * 32 - .5f) / 192f;
                uv[vertex] = new Vector2(u0, v0);
                uv[vertex + 1] = new Vector2(u1, v0);
                uv[vertex + 2] = new Vector2(u1, v1);
                uv[vertex + 3] = new Vector2(u0, v1);
                triangles[triangle] = vertex;
                triangles[triangle + 1] = vertex + 2;
                triangles[triangle + 2] = vertex + 1;
                triangles[triangle + 3] = vertex;
                triangles[triangle + 4] = vertex + 3;
                triangles[triangle + 5] = vertex + 2;
            }

            Mesh mesh = new Mesh { name = "Dream shell " + columns + "x" + rows };
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            shellMeshes[key] = mesh;
            return mesh;
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
