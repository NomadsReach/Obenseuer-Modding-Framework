using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    internal static class ModIdsSelfTest
    {
        internal static string Run()
        {
            try
            {
                if (ModIds.LegacyOf("obenseuer.moddingframework") != "nomad.obenseuer.core") return "bare id does not map to the legacy id";
                if (ModIds.LegacyOf("obenseuer.moddingframework.bankledger") != "nomad.obenseuer.core.bankledger") return "owner id does not map to the legacy owner id";
                if (ModIds.LegacyOf("obenseuer.moddingframeworkx") != null) return "prefix without a dot matched";
                if (ModIds.LegacyOf("nomad.obenseuer.core.bankledger") != null) return "legacy id mapped again";
                if (ModIds.LegacyOf("example.hellocore") != null || ModIds.LegacyOf(null) != null) return "other id mapped";
                const string id = "obenseuer.moddingframework.bankledger";
                HashSet<string> none = new HashSet<string>();
                HashSet<string> legacyOnly = new HashSet<string> { "nomad.obenseuer.core.bankledger" };
                HashSet<string> both = new HashSet<string> { "nomad.obenseuer.core.bankledger", id };
                DateTime older = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                DateTime newer = older.AddDays(1);
                if (ModIds.LoadId(id, n => legacyOnly.Contains(n), n => older) != "nomad.obenseuer.core.bankledger") return "legacy file not loaded when the new file is missing";
                if (ModIds.LoadId(id, n => both.Contains(n), n => n == id ? older : newer) != "nomad.obenseuer.core.bankledger") return "legacy file not chosen when it is newer";
                if (ModIds.LoadId(id, n => both.Contains(n), n => n == id ? newer : older) != id) return "new file not chosen when it is newer";
                if (ModIds.LoadId(id, n => none.Contains(n), n => older) != id) return "no file changed the id";
                if (ModIds.LoadId("example.hellocore", n => legacyOnly.Contains(n), n => older) != "example.hellocore") return "other id changed";
                if (!ModIds.IsReservedName("con") || !ModIds.IsReservedName("LPT\u00B9.save") || ModIds.IsReservedName("console")) return "device name check wrong";
            }
            catch (Exception e)
            {
                return "exception: " + e.Message;
            }
            return "modids self-check ok";
        }
    }
}
