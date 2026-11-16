using System;

namespace ObenseuerCore
{
    internal sealed class CloseRequestGate
    {
        private bool checking;

        internal bool Check(Func<bool> request)
        {
            if (checking) return false;
            checking = true;
            try { return request(); }
            finally { checking = false; }
        }
    }
}
