using System;
using System.Collections.Generic;
using System.Text;

namespace ObenseuerCore
{
    internal static class RegistrySelfTest
    {
        internal static string Run()
        {
            try
            {
                if (RegistryIds.Fnv1a32(Encoding.UTF8.GetBytes("")) != 0x811C9DC5u) return "fnv1a of empty text";
                if (RegistryIds.Fnv1a32(Encoding.UTF8.GetBytes("a")) != 0xE40C292Cu) return "fnv1a of a";
                if (RegistryIds.Fnv1a32(Encoding.UTF8.GetBytes("foobar")) != 0xBF9CF968u) return "fnv1a of foobar";
                if (RegistryIds.IdFor("example.spicy-jerky", "spicy-sewer-jerky") != 569330863) return "id stability vector";
                for (int i = 0; i < 1000; i++)
                {
                    int id = RegistryIds.IdFor("mod." + i, "key");
                    if (id < RegistryIds.FirstId || id > RegistryIds.FirstId + RegistryIds.IdCount - 1) return "id outside range for mod." + i;
                }
                Dictionary<int, string> owners = new Dictionary<int, string>();
                if (!RegistryIds.Claim(owners, 1234567, "m/a")) return "first claim refused";
                if (RegistryIds.Claim(owners, 1234567, "m/a")) return "repeat claim reported as new";
                try
                {
                    RegistryIds.Claim(owners, 1234567, "m/b");
                    return "collision not refused";
                }
                catch (ArgumentException)
                {
                }
                List<RegistryManifestEntry> target = new List<RegistryManifestEntry> { new RegistryManifestEntry { Id = 5, Mod = "a" } };
                List<RegistryManifestEntry> carried = new List<RegistryManifestEntry> { new RegistryManifestEntry { Id = 5, Mod = "old" }, new RegistryManifestEntry { Id = 7, Mod = "b" }, new RegistryManifestEntry { Id = 7, Mod = "b2" } };
                RegistryManifestStore.Merge(target, carried);
                if (target.Count != 2 || target[0].Mod != "a" || target[1].Id != 7 || target[1].Mod != "b") return "manifest merge";
                if (!Refused("bad guid", "k")) return "space in modGuid not refused";
                if (!Refused("", "k")) return "empty modGuid not refused";
                if (!Refused(new string('a', 65), "k")) return "65-character modGuid not refused";
                if (!Refused("ok", "bad/key")) return "slash in key not refused";
                if (Refused("ok", new string('k', 64))) return "64-character key refused";
            }
            catch (Exception e)
            {
                return "exception: " + e.Message;
            }
            return "registry self-check ok";
        }

        private static bool Refused(string modGuid, string key)
        {
            try
            {
                RegistryIds.IdFor(modGuid, key);
                return false;
            }
            catch (ArgumentException)
            {
                return true;
            }
        }
    }
}
