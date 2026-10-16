using System;

namespace ObenseuerCore
{
    internal static class PlayerWaitMath
    {
        internal const int MaxMinutes = 1440;

        internal static int MinutesPassed(int seconds)
        {
            return seconds / 60;
        }

        internal static bool ValidRange(int minMinutes, int maxMinutes)
        {
            return minMinutes >= 0 && maxMinutes >= minMinutes && maxMinutes <= MaxMinutes;
        }

        internal static int Clamp(int minutes, int minMinutes, int maxMinutes)
        {
            if (minutes < minMinutes) return minMinutes;
            return minutes > maxMinutes ? maxMinutes : minutes;
        }

        internal static int MinutesFromSlider(float seconds)
        {
            return (int)Math.Floor(seconds / 60f + 0.5f);
        }

        internal static int Snap(int minutes, int minMinutes, int maxMinutes, int stepMinutes)
        {
            int top = minMinutes + (maxMinutes - minMinutes) / stepMinutes * stepMinutes;
            int clamped = Clamp(minutes, minMinutes, top);
            return Clamp(minMinutes + (clamped - minMinutes + stepMinutes / 2) / stepMinutes * stepMinutes, minMinutes, top);
        }

        internal static string SelfCheck()
        {
            if (MinutesPassed(0) != 0 || MinutesPassed(59) != 0 || MinutesPassed(60) != 1 || MinutesPassed(3599) != 59) return "minutes passed wrong";
            if (!ValidRange(0, 0) || !ValidRange(5, 1440) || ValidRange(-1, 5) || ValidRange(6, 5) || ValidRange(0, 1441)) return "range check wrong";
            if (Clamp(-3, 0, 10) != 0 || Clamp(4, 5, 10) != 5 || Clamp(11, 5, 10) != 10 || Clamp(7, 5, 10) != 7) return "clamp wrong";
            if (MinutesFromSlider(1830f) != 31 || MinutesFromSlider(1800f) != 30 || MinutesFromSlider(0f) != 0) return "slider minutes wrong";
            if (Snap(31, 0, 120, 30) != 30 || Snap(46, 0, 120, 30) != 60 || Snap(31, 5, 120, 1) != 31 || Snap(500, 0, 120, 30) != 120 || Snap(3, 5, 120, 10) != 5) return "step snap wrong";
            if (Snap(100, 0, 100, 60) != 60 || Snap(-5, 0, 100, 60) != 0 || Snap(45, 0, 100, 60) != 60) return "step snap is not capped at the last whole step";
            return "player wait math self-check ok";
        }
    }
}
