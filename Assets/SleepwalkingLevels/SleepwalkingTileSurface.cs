using UnityEngine;

namespace Sleepwalking.Levels
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class SleepwalkingTileSurface : MonoBehaviour
    {
        // Коллайдер задаёт границы игрового блока; тайлы меняют только его вид.
        public BoxCollider2D Solid => GetComponent<BoxCollider2D>();
    }
}
