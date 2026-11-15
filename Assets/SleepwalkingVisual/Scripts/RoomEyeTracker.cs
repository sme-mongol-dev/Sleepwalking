using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Sleepwalking.Visual
{
    public sealed class RoomEyeTracker : MonoBehaviour
    {
        public Transform Pupil;
        public float TravelPixels = 10f;

        Vector3 pupilRest;

        void Awake()
        {
            if (Pupil != null) pupilRest = Pupil.localPosition;
        }

        void OnEnable()
        {
            if (Pupil != null) Pupil.localPosition = pupilRest;
        }

        void LateUpdate()
        {
            if (Pupil == null) return;
            Camera camera = SleepwalkingGame.Instance != null ? SleepwalkingGame.Instance.GameCamera : Camera.main;
            if (camera == null) return;
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            if (mouse == null) return;
            Vector2 pointer = mouse.position.ReadValue();
#else
            Vector2 pointer = Input.mousePosition;
#endif
            float depth = Mathf.Abs(transform.position.z - camera.transform.position.z);
            Vector3 screen = new Vector3(pointer.x * camera.pixelWidth / Mathf.Max(1, Screen.width),
                pointer.y * camera.pixelHeight / Mathf.Max(1, Screen.height), depth);
            LookAtWorld(camera.ScreenToWorldPoint(screen));
        }

        public void LookAtWorld(Vector2 target)
        {
            if (Pupil == null) return;
            Vector3 local = transform.InverseTransformPoint(target);
            Vector2 offset = Vector2.ClampMagnitude(new Vector2(local.x, local.y) * .5f, 1f) *
                (TravelPixels / 64f);
            Vector3 destination = pupilRest + new Vector3(offset.x, offset.y, 0f);
            Pupil.localPosition = destination;
        }
    }
}
