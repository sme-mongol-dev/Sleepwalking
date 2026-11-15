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
            odradek = 1;
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
            int index = completedDreams % DreamCount;
            float parSeconds = OdradekParSeconds[Mathf.Min(index, OdradekParSeconds.Length - 1)];
            int gain = CalculateOdradekGain(dreamElapsedSeconds, dreamDeaths, parSeconds);
            int previous = Odradek;
            // После сотни цикл начинается заново, без переноса остатка очков.
            bool wrapped = previous + gain > 100;
            odradek = wrapped ? 1 : previous + gain;
            LastOdradekGain = wrapped ? 0 : gain;
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
