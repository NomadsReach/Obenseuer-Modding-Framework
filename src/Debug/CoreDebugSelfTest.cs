using System;
using System.Collections.Generic;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class CoreDebugSelfTest
    {
        internal static string Run()
        {
            try
            {
                string failed = Failure();
                return failed == null ? "debug trace self-check ok" : "debug trace self-check failed: " + failed;
            }
            catch (Exception e)
            {
                return "debug trace self-check exception: " + e.GetType().Name + ": " + e.Message;
            }
        }

        private static string Failure()
        {
            if (CoreDebugFormat.CallLine(2, 0, "A.B", "1", "2", 0.5, null) != "    A.B(1) -> 2 (0.500 ms)") return "call line";
            if (CoreDebugFormat.CallLine(0, 3, "A.B", "", "void", 0, "Mod") != "[thread 3] A.B() -> void (0.000 ms) [from Mod]") return "thread and caller";
            if (CoreDebugFormat.ErrorLine(1, 0, "A.B", new InvalidOperationException("boom")) != "  !! A.B threw InvalidOperationException: boom") return "error line";
            if (CoreDebugFormat.MoreLine("A.B", 5, "1, 2", "3") != "A.B: 5 more calls in the last 10 s (last: 1, 2 -> 3)") return "more line";
            if (CoreDebugFormat.Value(null) != "null" || CoreDebugFormat.Value("ab") != "\"ab\"") return "value";
            if (CoreDebugFormat.Cut(new string('x', 130)) != new string('x', 120) + "...") return "cut";
            if (CoreDebugFormat.Name(typeof(List<int>)) != "List") return "generic name";
            MethodInfo sample = typeof(CoreDebugSelfTest).GetMethod("Sample", BindingFlags.Static | BindingFlags.NonPublic);
            if (CoreDebugFormat.Args(sample.GetParameters(), new object[] { 1, 2, null }) != "1, ref 2, out") return "ref and out";
            CoreDebugLimiter limiter = new CoreDebugLimiter(0f);
            for (int i = 0; i < CoreDebugLimiter.FullLines; i++)
            {
                if (!limiter.Admit(i)) return "limiter full";
            }
            if (limiter.Admit(-1)) return "limiter summary";
            if (limiter.Due(9f) || !limiter.Due(10f)) return "limiter window";
            if (limiter.Take(10f) != 1 || limiter.Due(20f)) return "limiter take";
            if (!CoreDebugTrace.Matches("PlayerState", "") || !CoreDebugTrace.Matches("PlayerState", "Save, Player") || CoreDebugTrace.Matches("PlayerState", "Save")) return "filter";
            return null;
        }

        private static void Sample(int a, ref int b, out int c)
        {
            c = a + b;
        }
    }
}
