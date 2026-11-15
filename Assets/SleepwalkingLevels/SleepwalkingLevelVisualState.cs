using UnityEngine;

namespace Sleepwalking.Levels
{
    public sealed class SleepwalkingLevelVisualState : MonoBehaviour
    {
        private Collider2D shape;
        private SleepwalkingMechanism mechanism;
        private bool lastVisible;
        private MaterialPropertyBlock properties;
        private static readonly int Opacity = Shader.PropertyToID("_Opacity");

        private void Awake()
        {
            shape = GetComponent<Collider2D>();
            mechanism = GetComponent<SleepwalkingMechanism>();
            lastVisible = shape == null || shape.enabled;
        }

        private void LateUpdate()
        {
            if (shape == null) return;
            if (mechanism == null) mechanism = GetComponent<SleepwalkingMechanism>();
            bool visible = mechanism != null && mechanism.Kind == "LaserBase" || shape.enabled;
            if (mechanism != null && mechanism.Kind == "TimedPlatform")
            {
                bool warning = visible && mechanism.TimedSecondsUntilFade <= 1f &&
                    Mathf.FloorToInt(Time.time * 8f) % 2 == 0;
                foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer.name.StartsWith("Dream Warning Art"))
                    {
                        renderer.enabled = false;
                        continue;
                    }
                    if (renderer.name != "Dream Tiles") continue;
                    renderer.enabled = !warning;
                    properties ??= new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(properties);
                    properties.SetFloat(Opacity, visible ? 1f : .42f);
                    renderer.SetPropertyBlock(properties);
                }
                lastVisible = visible;
                return;
            }
            if (visible && visible == lastVisible) return;

            // Тайлы могут появиться после Awake, поэтому обновляем список при смене состояния.
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int q = 0; q < renderers.Length; q++)
                if (renderers[q] != null) renderers[q].enabled = visible;

            lastVisible = visible;
        }
    }
}
