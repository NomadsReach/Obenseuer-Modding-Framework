using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    internal sealed class DeskCloseGate<T> where T : class
    {
        private readonly CloseRequestGate gate = new CloseRequestGate();

        internal bool Request(Func<List<T>> snapshot, Func<T, bool> canClose, Func<T, T, bool> same, Func<bool> finish)
        {
            return gate.Check(delegate
            {
                List<T> before = snapshot();
                foreach (T host in before) if (!canClose(host)) return false;
                List<T> after = snapshot();
                if (before.Count != after.Count) return false;
                for (int i = 0; i < before.Count; i++) if (!same(before[i], after[i])) return false;
                return finish();
            });
        }
    }
}
