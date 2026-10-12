using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    internal static class ApiLog
    {
        private static readonly HashSet<string> reported = new HashSet<string>();

        internal static void Once(string owner, string what, Exception e)
        {
            if (!reported.Add(owner + " " + what)) return;
            CoreLog.For(owner).Error(what + " failed (later failures of this member are not logged): " + e.ToString());
        }
    }
}
