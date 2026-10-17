namespace ObenseuerCore
{
    internal sealed class RetryGate
    {
        internal const int MaxAttempts = 600;
        private int attempts;
        private bool waiting;
        private bool done;

        internal bool Waiting
        {
            get { return waiting; }
        }

        internal bool Done
        {
            get { return done; }
        }

        internal int Attempts
        {
            get { return attempts; }
        }

        internal void Arm()
        {
            if (done) return;
            waiting = true;
            attempts = 0;
        }

        internal void Clear()
        {
            waiting = false;
            attempts = 0;
        }

        internal void Reset()
        {
            waiting = false;
            attempts = 0;
            done = false;
        }

        internal bool Try(bool ready)
        {
            if (!waiting) return false;
            attempts++;
            if (ready)
            {
                waiting = false;
                done = true;
                return true;
            }
            if (attempts >= MaxAttempts) waiting = false;
            return false;
        }
    }
}
