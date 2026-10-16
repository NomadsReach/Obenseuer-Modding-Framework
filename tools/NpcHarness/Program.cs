using System;
using ObenseuerCore;

namespace NpcHarness
{
    internal static class Program
    {
        private static int Main()
        {
            string result = NpcRoutineSelfTest.Run();
            Console.WriteLine(result);
            string walk = NpcWalkMathCheck.Run();
            Console.WriteLine(walk == null ? "npc walk math self-check ok" : walk);
            string saved = ModNpcsSelfTest.Run();
            Console.WriteLine(saved);
            string live = SceneLookLiveSelfTest.Run();
            Console.WriteLine(live);
            return result == "npc routine table self-check ok" && walk == null && saved == "mod npc saved self-check ok" && live == SceneLookLiveSelfTest.Ok ? 0 : 1;
        }
    }
}
