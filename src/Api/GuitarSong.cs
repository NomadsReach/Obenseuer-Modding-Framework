using System.Collections.Generic;

namespace ObenseuerCore
{
    internal sealed class GuitarSong
    {
        private const int MaxStrumsPerFrame = 4;
        private readonly List<float> delays;
        private int next;
        private float untilNext;

        internal GuitarSong(IList<float> delays)
        {
            this.delays = new List<float>(delays);
            if (this.delays.Count > 0) untilNext = this.delays[0];
        }

        internal bool Done
        {
            get
            {
                return next >= delays.Count;
            }
        }

        internal static bool Valid(IList<float> delays)
        {
            if (delays == null || delays.Count == 0) return false;
            foreach (float delay in delays)
            {
                if (float.IsNaN(delay) || float.IsInfinity(delay) || delay < 0f) return false;
            }
            return true;
        }

        internal int Advance(float seconds)
        {
            int due = 0;
            untilNext -= seconds;
            while (!Done && untilNext <= 0f && due < MaxStrumsPerFrame)
            {
                due++;
                next++;
                if (!Done) untilNext += delays[next];
            }
            return due;
        }
    }
}
