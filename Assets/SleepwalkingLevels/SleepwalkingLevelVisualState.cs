using UnityEngine;

namespace Sleepwalking.Levels
{
    public sealed class SleepwalkingLevelVisualState : MonoBehaviour
    {
        private Collider2D shape;
        private bool lastVisible;

        private void Awake()
        {
            shape = GetComponent<Collider2D>();
            lastVisible = shape == null || shape.enabled;
        }

        private void LateUpdate()
        {
            if (shape == null) return;
            bool visible = shape.enabled;
            if (visible && visible == lastVisible) return;

            // Тайлы могут появиться после Awake, поэтому обновляем список при смене состояния.
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int q = 0; q < renderers.Length; q++)
                if (renderers[q] != null) renderers[q].enabled = visible;

            lastVisible = visible;
        }
    }
}
