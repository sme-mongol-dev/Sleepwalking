using System.IO;
using UnityEditor;
using UnityEngine;

namespace Sleepwalking.Editor
{
    public static class TutorialSignBuilder
    {
        const string TextureRoot = "Assets/SleepwalkingUI/Textures/Tutorial";
        const string PrefabRoot = "Assets/SleepwalkingUI/Prefabs/Tutorial";

        [MenuItem("Sleepwalking/UI/Build Tutorial Signs")]
        public static void BuildPrefabs()
        {
            Directory.CreateDirectory(PrefabRoot);
            AssetDatabase.Refresh();

            Sprite a = ImportSprite("T_A");
            Sprite d = ImportSprite("T_D");
            Sprite space = ImportSprite("T_Space");
            Sprite shift = ImportSprite("T_Shift");
            Sprite arrow = ImportSprite("T_Arrow");
            Font font = Resources.Load<Font>("SleepwalkingFonts/SpaceMono-Bold");
            if (font == null) throw new System.InvalidOperationException("Missing Space Mono tutorial font.");

            Build(TutorialSignKind.Run, "A / D\nMOVE", font, a, d, arrow);
            Build(TutorialSignKind.Jump, "SPACE\nJUMP", font, space, null, arrow);
            Build(TutorialSignKind.Grab, "SPACE\nCLING", font, space, null, arrow);
            Build(TutorialSignKind.WallJump, "SPACE\nWALL JUMP", font, space, null, arrow);
            Build(TutorialSignKind.Dash, "SHIFT\nDASH", font, shift, null, arrow);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Built five world-space tutorial signs in " + PrefabRoot);
        }

        public static void BuildFromCommandLine() { BuildPrefabs(); }

        static Sprite ImportSprite(string name)
        {
            string path = TextureRoot + "/" + name + ".png";
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new FileNotFoundException("Tutorial key image is missing.", path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 32;
            importer.SaveAndReimport();
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new System.InvalidOperationException("Could not import tutorial sprite: " + path);
            return sprite;
        }

        static void Build(TutorialSignKind kind, string caption, Font font, Sprite first, Sprite second, Sprite arrow)
        {
            GameObject root = new GameObject("Tutorial_" + kind);
            root.AddComponent<SleepwalkingTutorialSign>().Configure(kind);

            if (second == null)
            {
                Icon(root.transform, "Key", first, -.65f, .25f, 0f, 1.25f);
                float angle = kind == TutorialSignKind.Jump ? 90f : kind == TutorialSignKind.WallJump ? 45f : 0f;
                Icon(root.transform, "Direction", arrow, .65f, .25f, angle, 1.25f);
            }
            else
            {
                Icon(root.transform, "Key A", first, -1.1f, .25f, 0f, 1.05f);
                Icon(root.transform, "Key D", second, 0f, .25f, 0f, 1.05f);
                Icon(root.transform, "Direction", arrow, 1.1f, .25f, 0f, 1.05f);
            }

            GameObject textObject = new GameObject("Caption", typeof(TextMesh));
            textObject.transform.SetParent(root.transform, false);
            textObject.transform.localPosition = new Vector3(0f, -.78f, -2.1f);
            TextMesh text = textObject.GetComponent<TextMesh>();
            text.text = caption;
            text.font = font;
            text.fontSize = 64;
            text.characterSize = .07f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = SleepwalkingUI.SecondaryText;
            MeshRenderer renderer = textObject.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = font.material;
            renderer.sortingOrder = 21;

            PrefabUtility.SaveAsPrefabAsset(root, PrefabRoot + "/Tutorial_" + kind + ".prefab");
            Object.DestroyImmediate(root);
        }

        static void Icon(Transform parent, string name, Sprite sprite, float x, float y, float angle, float scale)
        {
            GameObject icon = new GameObject(name, typeof(SpriteRenderer));
            icon.transform.SetParent(parent, false);
            icon.transform.localPosition = new Vector3(x, y, -2f);
            icon.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            icon.transform.localScale = new Vector3(scale, scale, 1f);
            SpriteRenderer renderer = icon.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 20;
        }
    }
}
