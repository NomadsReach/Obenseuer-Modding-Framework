using System;
using ObenseuerCore;

namespace ModelHarness
{
    internal static class Program
    {
        private static int Main()
        {
            string result = ModelSelfTest.Run();
            Console.WriteLine(result);
            return result == "model parser self-check ok" ? 0 : 1;
        }
    }
}
