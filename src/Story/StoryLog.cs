using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    internal static class StoryLog
    {
        private static readonly HashSet<string> refused = new HashSet<string>();


        internal static void Refuse(string owner, string member, string reason)
        {
            if (!refused.Add(owner + " " + member + " " + reason)) return;
            CoreLog.For(owner).Warn(member + " refused: " + reason + " (later refusals with this reason are not logged)");
        }
    }
}
