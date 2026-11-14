using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Sleepwalking.Visual.Editor
{
    public static class RoomDynamicLighting
    {
        [MenuItem("Sleepwalking/Apply Dynamic Window Lighting")]
        public static void ApplyToExistingMenu()
        {
            GameObject room = PrefabUtility.LoadPrefabContents(VisualAssetBuilder.MenuRoomPrefab);
            try
            {
                Apply(room);
                PrefabUtility.SaveAsPrefabAsset(room, VisualAssetBuilder.MenuRoomPrefab);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(room);
            }
        }

        public static void Apply(GameObject room)
        {
            Transform window = room.transform.Find("Window");
            if (window == null) throw new InvalidOperationException("The menu room must contain its window.");

            foreach (Transform child in room.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "Soft Window Shadow" || child.name == "Window Light On Wall" ||
                    child.name == "Window Light On Floor")
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
            }

            foreach (MeshRenderer renderer in room.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer.transform == window || renderer.transform.IsChildOf(window))
                {
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    continue;
                }

                renderer.sharedMaterial = LitMaterial(renderer.sharedMaterial);
                renderer.shadowCastingMode = ShadowCastingMode.TwoSided;
                renderer.receiveShadows = true;
            }

            Transform source = room.transform.Find("Window Light");
            if (source == null)
            {
                source = new GameObject("Window Light").transform;
                source.SetParent(room.transform, false);
            }
            source.localPosition = window.localPosition + new Vector3(0, 2.2f, -4f);
            source.LookAt(room.transform.TransformPoint(new Vector3(0f, 1f, 1.6f)));
            Light light = source.GetComponent<Light>();
            if (light == null) light = source.gameObject.AddComponent<Light>();
            light.type = LightType.Spot;
            light.lightmapBakeType = LightmapBakeType.Realtime;
            light.color = new Color(.78f, .86f, 1f);
            light.intensity = 24f;
            light.range = 24f;
            light.spotAngle = 130f;
            light.innerSpotAngle = 100f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = .8f;
            light.shadowBias = .025f;
            light.shadowNormalBias = .05f;
            light.shadowResolution = LightShadowResolution.VeryHigh;
            UniversalAdditionalLightData data = source.GetComponent<UniversalAdditionalLightData>();
            if (data == null) data = source.gameObject.AddComponent<UniversalAdditionalLightData>();
            data.usePipelineSettings = false;
        }

        static Material LitMaterial(Material original)
        {
            const string folder = "Assets/SleepwalkingVisual/Materials/";
            string path = folder + "Menu_Lit_" + original.name.Replace("Menu_Lit_", "").Replace("Menu_", "") + ".mat";
            Texture texture = original.mainTexture;
            Color color = original.HasProperty("_BaseColor") ? original.GetColor("_BaseColor") : Color.white;
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0f);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Cull", 0f);
            material.SetFloat("_Cutoff", .5f);
            material.SetFloat("_AlphaClip", texture != null ? 1f : 0f);
            material.SetFloat("_ReceiveShadows", 1f);
            material.SetTexture("_EmissionMap", texture);
            material.SetColor("_EmissionColor", texture != null ? Color.white * .1f : Color.black);
            material.EnableKeyword("_EMISSION");
            if (texture != null) material.EnableKeyword("_ALPHATEST_ON");
            else material.DisableKeyword("_ALPHATEST_ON");
            material.renderQueue = texture != null ? (int)RenderQueue.AlphaTest : (int)RenderQueue.Geometry;
            material.SetOverrideTag("RenderType", texture != null ? "TransparentCutout" : "Opaque");
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
