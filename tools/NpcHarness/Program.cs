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
            string phones = PhoneRulesSelfTest.Run();
            Console.WriteLine(phones);
            string live = SceneLookLiveSelfTest.Run();
            Console.WriteLine(live);
            string options = NewGameOptionsSelfTest.Run();
            Console.WriteLine(options);
            string gates = QuestsGateSelfTest.Run();
            Console.WriteLine(gates);
            string inventory = InventorySelfTest.Run();
            Console.WriteLine(inventory);
            return result == "npc routine table self-check ok"
                && walk == null
                && saved == "mod npc saved self-check ok"
                && phones == "phone rules self-check ok"
                && live == SceneLookLiveSelfTest.Ok
                && options == NewGameOptionsSelfTest.Ok
                && gates == QuestsGateSelfTest.Ok
                && inventory == InventorySelfTest.Ok ? 0 : 1;
        }
    }
}
