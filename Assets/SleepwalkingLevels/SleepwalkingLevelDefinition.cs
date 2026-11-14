using UnityEngine;

namespace Sleepwalking.Levels
{
    public enum LevelFeatureKind
    {
        Spikes,
        Saw,
        LaserBase,
        LaserBeam,
        TimedPlatform,
        MovingPlatform,
        Button,
        Exit,
        FallReset
    }

    public enum DreamStyle
    {
        Meat,
        PostApocalypse,
        DeepWater,
        Heaven
    }

    public sealed class SleepwalkingLevelDefinition : MonoBehaviour
    {
        [SerializeField] private int levelIndex;
        [SerializeField] private DreamStyle firstCycleStyle;
        [SerializeField] private Transform playerStart;
        [SerializeField] private Transform bedExit;

        public int LevelIndex => levelIndex;
        public DreamStyle FirstCycleStyle => firstCycleStyle;
        public Transform PlayerStart => playerStart;
        public Transform BedExit => bedExit;

        public void Configure(int index, DreamStyle style, Transform start, Transform exit)
        {
            levelIndex = index;
            firstCycleStyle = style;
            playerStart = start;
            bedExit = exit;
        }
    }

}
