using System.Collections.Generic;

namespace ObenseuerCore
{
    internal enum SceneLookLiveAction
    {
        Refuse,
        Despawn,
        Keep
    }

    internal static class SceneLookLivePlan
    {
        internal static SceneLookLiveAction Spawn(bool spawned, bool applied)
        {
            if (!spawned) return SceneLookLiveAction.Refuse;
            return applied ? SceneLookLiveAction.Keep : SceneLookLiveAction.Despawn;
        }

        internal static List<int> Watch(List<int> hidden, List<int> now)
        {
            List<int> watch = new List<int>(hidden.Count);
            for (int i = 0; i < hidden.Count; i++)
            {
                if (now.Contains(hidden[i])) watch.Add(i);
            }
            return watch;
        }

        internal static int Holder(List<int> before, List<int> after)
        {
            int found = 0;
            int count = 0;
            for (int i = 0; i < after.Count; i++)
            {
                if (before.Contains(after[i])) continue;
                found = after[i];
                count++;
            }
            return count == 1 ? found : 0;
        }
    }
}
