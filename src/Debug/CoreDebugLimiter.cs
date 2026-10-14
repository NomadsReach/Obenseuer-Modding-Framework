namespace ObenseuerCore
{
    internal sealed class CoreDebugLimiter
    {
        internal const int FullLines = 20;
        internal const float WindowSeconds = 10f;
        internal long Total;
        internal object Last;
        private int full;
        private long pending;
        private float windowStart;

        internal CoreDebugLimiter(float now)
        {
            windowStart = now;
        }

        internal bool HasFullLines
        {
            get { return full < FullLines; }
        }

        internal bool Admit(object last)
        {
            Total++;
            Last = last;
            if (full < FullLines)
            {
                full++;
                return true;
            }
            pending++;
            return false;
        }

        internal bool Due(float now)
        {
            return pending > 0 && now - windowStart >= WindowSeconds;
        }

        internal long Take(float now)
        {
            long count = pending;
            pending = 0;
            windowStart = now;
            return count;
        }
    }
}
