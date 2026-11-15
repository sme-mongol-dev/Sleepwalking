using UnityEngine;

namespace Sleepwalking
{
    public enum TutorialSignKind
    {
        Run,
        Jump,
        Grab,
        WallJump,
        Dash
    }

    // Маркер и содержимое знака живут в локальной сетке уровня: одна единица равна одному блоку.
    public sealed class SleepwalkingTutorialSign : MonoBehaviour
    {
        [SerializeField] TutorialSignKind kind;

        public TutorialSignKind Kind => kind;
        public Vector2 SizeInBlocks => kind == TutorialSignKind.Run ? new Vector2(3.4f, 2f) : new Vector2(2.6f, 2f);

        public void Configure(TutorialSignKind value) { kind = value; }
    }
}
