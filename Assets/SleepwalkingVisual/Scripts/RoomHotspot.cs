using UnityEngine;

namespace Sleepwalking.Visual
{
    public enum RoomAction { Dream, Settings, Credits, Quit }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class RoomHotspot : MonoBehaviour
    {
        public RoomAction Action;
        public string Label;
        public MeshRenderer Artwork;

        static readonly int Highlight = Shader.PropertyToID("_Highlight");
        Renderer[] artworkLayers;
        MaterialPropertyBlock properties;

        public bool ContainsArtworkPixel(Vector3 worldPoint)
        {
            if (Artwork == null) return true;

            Texture2D texture = (Texture2D)Artwork.sharedMaterial.mainTexture;
            Vector3 local = Artwork.transform.InverseTransformPoint(worldPoint);
            int x = Mathf.FloorToInt((local.x + .5f) * texture.width);
            int y = Mathf.FloorToInt((local.y + .5f) * texture.height);
            return x >= 0 && x < texture.width && y >= 0 && y < texture.height &&
                texture.GetPixel(x, y).a >= .5f;
        }

        public void SetHighlighted(bool highlighted)
        {
            if (Artwork == null) return;
            artworkLayers ??= Artwork.GetComponentsInChildren<Renderer>(true);
            properties ??= new MaterialPropertyBlock();
            foreach (Renderer layer in artworkLayers)
            {
                layer.GetPropertyBlock(properties);
                properties.SetFloat(Highlight, highlighted ? .14f : 0f);
                if (layer.sharedMaterial.HasProperty("_EmissionColor"))
                    properties.SetColor("_EmissionColor", layer.sharedMaterial.GetColor("_EmissionColor") +
                        (highlighted ? new Color(.14f, .13f, .12f, 0f) : Color.clear));
                layer.SetPropertyBlock(properties);
            }
        }
    }
}
