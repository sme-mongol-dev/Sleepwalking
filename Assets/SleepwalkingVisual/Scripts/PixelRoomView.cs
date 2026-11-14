using UnityEngine;
using UnityEngine.UI;

namespace Sleepwalking.Visual
{
    [RequireComponent(typeof(Camera))]
    public sealed class PixelRoomView : MonoBehaviour
    {
        [Min(64)] public int BaseWidth = 320;
        [Min(64)] public int BaseHeight = 180;
        [Min(0)] public float MinimumHalfWorldWidth;
        public RoomEnvironment RoomRoot;

        Camera roomCamera;
        RenderTexture target;
        Canvas outputCanvas;
        RawImage output;
        int screenWidth;
        int screenHeight;
        float originalOrthographicSize;
        Vector3 originalRoomPosition;

        void OnEnable()
        {
            if (!Application.isPlaying) return;
            roomCamera = GetComponent<Camera>();
            originalOrthographicSize = roomCamera.orthographicSize;
            originalRoomPosition = transform.position;
            GameObject canvasObject = new GameObject("Pixel Room Output", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            outputCanvas = canvasObject.GetComponent<Canvas>();
            outputCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            outputCanvas.overrideSorting = true;
            outputCanvas.sortingOrder = -100;
            GameObject imageObject = new GameObject("Point Filtered 3D", typeof(RectTransform), typeof(RawImage));
            imageObject.transform.SetParent(canvasObject.transform, false);
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            output = imageObject.GetComponent<RawImage>();
            output.raycastTarget = false;
            Refresh();
        }

        void Update()
        {
            if (!Application.isPlaying) return;
            if (screenWidth != Screen.width || screenHeight != Screen.height) Refresh();
            ApplyFraming();
        }

        void ApplyFraming()
        {
            if (roomCamera == null || !roomCamera.orthographic || MinimumHalfWorldWidth <= 0) return;
            // Общая камера выходит из комнаты на время сна; её масштаб там задаёт геймплей.
            if (RoomRoot == null || !RoomRoot.gameObject.activeInHierarchy) return;
            if (Vector3.Distance(transform.position, originalRoomPosition) > .05f) return;
            float aspect = (float)Mathf.Max(1, Screen.width) / Mathf.Max(1, Screen.height);
            roomCamera.orthographicSize = Mathf.Max(originalOrthographicSize, MinimumHalfWorldWidth / aspect);
        }

        void Refresh()
        {
            screenWidth = Mathf.Max(1, Screen.width);
            screenHeight = Mathf.Max(1, Screen.height);
            float aspect = (float)screenWidth / screenHeight;
            int height = Mathf.Max(64, BaseHeight, Mathf.CeilToInt(BaseWidth / aspect));
            int width = Mathf.Max(64, Mathf.RoundToInt(height * aspect));
            if (target != null && target.width == width && target.height == height) return;
            if (target != null)
            {
                roomCamera.targetTexture = null;
                target.Release();
                Destroy(target);
            }
            target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            target.name = "Live Room Pixels";
            target.filterMode = FilterMode.Point;
            target.wrapMode = TextureWrapMode.Clamp;
            target.antiAliasing = 1;
            target.Create();
            roomCamera.targetTexture = target;
            output.texture = target;
            ApplyFraming();
        }

        void OnDisable()
        {
            if (!Application.isPlaying) return;
            if (roomCamera != null) roomCamera.targetTexture = null;
            if (target != null) { target.Release(); Destroy(target); }
            if (outputCanvas != null) Destroy(outputCanvas.gameObject);
        }
    }
}
