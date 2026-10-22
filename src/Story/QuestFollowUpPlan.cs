namespace ObenseuerCore
{
    internal static class QuestFollowUpPlan
    {
        internal const int MaxDepth = 8;
        internal const string None = "none";
        internal const string Self = "self";
        internal const string Exists = "exists";
        internal const string Again = "again";
        internal const string Depth = "depth";
        internal const string Missing = "missing";

        internal static bool WithinDepth(int depth)
        {
            return depth > 0 && depth <= MaxDepth;
        }

        internal static string TargetId(string modId, string nextKey)
        {
            if (string.IsNullOrEmpty(modId) || string.IsNullOrEmpty(nextKey)) return null;
            return modId + "." + nextKey;
        }

        internal static string Skip(string ownKey, string nextKey, bool exists, bool again, int started)
        {
            if (string.IsNullOrEmpty(nextKey)) return None;
            if (nextKey == ownKey) return Self;
            if (exists) return Exists;
            if (again) return Again;
            return WithinDepth(started + 1) ? null : Depth;
        }
    }
}
