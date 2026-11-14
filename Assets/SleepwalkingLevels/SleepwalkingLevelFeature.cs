using UnityEngine;

namespace Sleepwalking.Levels
{
    public sealed class SleepwalkingLevelFeature : MonoBehaviour
    {
        [SerializeField] private LevelFeatureKind kind;
        [SerializeField] private string featureId;
        [SerializeField] private string linkedFeatureId;
        [SerializeField] private float activeSeconds;
        [SerializeField] private Vector2 moveOffset;
        [SerializeField] private float cycleSeconds;
        [SerializeField] private float phaseSeconds;
        [SerializeField] private float beamPixelWidth;

        public LevelFeatureKind Kind => kind;
        public string FeatureId => featureId;
        public string LinkedFeatureId => linkedFeatureId;
        public float ActiveSeconds => activeSeconds;
        public Vector2 MoveOffset => moveOffset;
        public float CycleSeconds => cycleSeconds;
        public float PhaseSeconds => phaseSeconds;
        public float BeamPixelWidth => beamPixelWidth;

        public void Configure(LevelFeatureKind featureKind, string id, string linkedId = "", float duration = 0,
            Vector2 movement = default, float cycle = 0, float phase = 0, float beamPixels = 0)
        {
            kind = featureKind;
            featureId = id;
            linkedFeatureId = linkedId;
            activeSeconds = duration;
            moveOffset = movement;
            cycleSeconds = cycle;
            phaseSeconds = phase;
            beamPixelWidth = beamPixels;
        }
    }
}
