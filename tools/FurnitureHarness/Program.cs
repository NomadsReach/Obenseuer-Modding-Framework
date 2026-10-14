using System;
using ObenseuerCore;

namespace FurnitureHarness
{
    internal static class Program
    {
        private static int Main()
        {
            string result = FurnitureIdsSelfTest.Run();
            Console.WriteLine(result);
            string shops = ShopsPlayerMathSelfTest.Run();
            Console.WriteLine(shops);
            return result == "furniture id self-check ok" && shops == "player shop math self-check ok" ? 0 : 1;
        }
    }
}
