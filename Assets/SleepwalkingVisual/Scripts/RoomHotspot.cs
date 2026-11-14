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

        public static RoomHotspot Raycast(Camera camera, Vector2 screenPosition)
        {
            if (camera == null) return null;
            // Камера рисует в маленькую RenderTexture; координаты мыши приходят в размере окна.
            Vector2 pixelPosition = new Vector2(
                screenPosition.x * camera.pixelWidth / Mathf.Max(1, Screen.width),
                screenPosition.y * camera.pixelHeight / Mathf.Max(1, Screen.height));
            Ray ray = camera.ScreenPointToRay(pixelPosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
            RoomHotspot closest = null;
            float distance = float.MaxValue;
            foreach (RaycastHit hit in hits)
            {
                RoomHotspot hotspot = hit.collider.GetComponentInParent<RoomHotspot>();
                if (hotspot == null || hit.distance >= distance) continue;
                closest = hotspot;
                distance = hit.distance;
            }
            return closest;
        }
    }
}
