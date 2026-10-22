namespace ObenseuerCore
{
    internal static class DifficultyModPresets
    {
        internal static int Clamp(int index, int count, int preferred)
        {
            if (count <= 0) return 0;
            if (index >= 0 && index < count) return index;
            if (preferred >= 0 && preferred < count) return preferred;
            return index < 0 ? 0 : count - 1;
        }

        internal static int Length(int vanilla, int listed)
        {
            if (vanilla < 0) vanilla = 0;
            if (listed < 0) listed = 0;
            return vanilla + listed;
        }

        internal static bool IsListed(int index, int vanilla, int listed)
        {
            return listed > 0 && index >= vanilla && index - vanilla < listed;
        }

        internal static string Restore(string pending, string stored)
        {
            if (string.IsNullOrEmpty(pending)) return stored;
            return string.IsNullOrEmpty(stored) ? pending : stored;
        }
    }
}
