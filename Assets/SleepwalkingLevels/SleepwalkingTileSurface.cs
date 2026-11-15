using UnityEngine;

namespace Sleepwalking.Levels
{
    [DisallowMultipleComponent]
    public sealed class SleepwalkingTileSurface : MonoBehaviour
    {
        public bool IsShell;

        // Физический контур задаётся общим шаблоном тайла для всех четырёх тем.
        public Collider2D Solid => GetComponent<Collider2D>();
    }
}
