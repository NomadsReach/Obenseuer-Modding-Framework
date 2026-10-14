using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    internal static class CraftingSourcesSelfTest
    {
        internal static string Run()
        {
            try
            {
                string[] own = { "a", "b" };
                string[] c = { "c" };
                string[] d = { "d" };
                string[] empty = new string[0];
                if (Join(CraftingSources.Combine(own, new List<string[]> { c, d })) != "a,b,c,d") return "extra containers are not appended in order";
                if (Join(CraftingSources.Combine(own, new List<string[]> { d, c })) != "a,b,d,c") return "registration order is not kept";
                if (Join(CraftingSources.Combine(own, new List<string[]> { c, c })) != "a,b,c") return "the same container is counted twice";
                if (Join(CraftingSources.Combine(own, new List<string[]> { own, c })) != "a,b,c") return "the station's own container is counted twice";
                if (Join(CraftingSources.Combine(own, new List<string[]> { null, empty, d })) != "a,b,d") return "a null or empty container is not skipped";
                if (Join(CraftingSources.Combine(empty, new List<string[]> { c })) != "c") return "an empty station does not keep its extras";
            }
            catch (Exception e)
            {
                return "exception: " + e.Message;
            }
            return "crafting sources self-check ok";
        }

        private static string Join(string[] items)
        {
            return string.Join(",", items);
        }
    }
}
