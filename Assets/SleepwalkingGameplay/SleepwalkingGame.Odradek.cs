using UnityEngine;

namespace Sleepwalking
{
    public sealed partial class SleepwalkingGame
    {
        [SerializeField, Range(1, 100)] int odradek = 1;
        static readonly float[] OdradekParSeconds = { 12f, 14f, 16f, 18f, 28f, 35f, 32f };

        float dreamElapsedSeconds;
        int dreamDeaths;

        public int Odradek => Mathf.Clamp(odradek, 1, 100);
        public float OdradekRunMultiplier => Mathf.Lerp(1f, .95f, (Odradek - 1) / 99f);
        public float OdradekJumpMultiplier => Mathf.Lerp(1f, .99f, (Odradek - 1) / 99f);
        public float OdradekDisableMultiplier => Mathf.Lerp(1f, .9f, (Odradek - 1) / 99f);
        public float DreamElapsedSeconds => dreamElapsedSeconds;
        public int DreamDeaths => dreamDeaths;
        public int LastOdradekGain { get; private set; }

        void ResetOdradek()
        {
            odradek = Random.Range(20, 36);
            dreamElapsedSeconds = 0f;
            dreamDeaths = 0;
            LastOdradekGain = 0;
        }

        void BeginDreamPerformance()
        {
            dreamElapsedSeconds = 0f;
            dreamDeaths = 0;
        }

        void RewardDreamPerformance()
        {
            float parSeconds = OdradekParSeconds[completedDreams % OdradekParSeconds.Length];
            int gain = CalculateOdradekGain(dreamElapsedSeconds, dreamDeaths, parSeconds);
            int previous = Odradek;
            odradek = Mathf.Clamp(previous + gain, 1, 100);
            LastOdradekGain = odradek - previous;
        }

        static int CalculateOdradekGain(float seconds, int deaths, float parSeconds)
        {
            // Скорость и аккуратность дают независимые бонусы, суммарно до шести очков.
            float pace = Mathf.InverseLerp(parSeconds * 2f, parSeconds * .6f, seconds);
            int speedBonus = Mathf.RoundToInt(3f * pace);
            int survivalBonus = Mathf.Clamp(3 - deaths, 0, 3);
            return speedBonus + survivalBonus;
        }
    }
}
