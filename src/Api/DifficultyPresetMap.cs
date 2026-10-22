using System;

namespace ObenseuerCore
{
    internal static class DifficultyPresetMap
    {
        internal static int IndexOf(int[] levels, int wanted)
        {
            if (levels == null) return -1;
            for (int i = 0; i < levels.Length; i++)
            {
                if (levels[i] == wanted) return i;
            }
            return -1;
        }

        internal static int Nearest(float value, float[] steps)
        {
            if (steps == null || steps.Length == 0) return -1;
            int best = 0;
            float gap = Math.Abs(steps[0] - value);
            for (int i = 1; i < steps.Length; i++)
            {
                float other = Math.Abs(steps[i] - value);
                if (other < gap)
                {
                    gap = other;
                    best = i;
                }
            }
            return best;
        }

        internal static int[] OrderByValue(float[] values)
        {
            if (values == null) return new int[0];
            int[] order = new int[values.Length];
            for (int i = 0; i < order.Length; i++) order[i] = i;
            for (int i = 1; i < order.Length; i++)
            {
                int key = order[i];
                int j = i - 1;
                while (j >= 0 && values[order[j]] > values[key])
                {
                    order[j + 1] = order[j];
                    j--;
                }
                order[j + 1] = key;
            }
            return order;
        }
    }
}
