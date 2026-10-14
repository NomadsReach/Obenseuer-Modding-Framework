using System;

namespace ObenseuerCore
{
    internal static class AnimalActionsSelfTest
    {
        internal static string Run()
        {
            try
            {
                AnimalTameFile file = new AnimalTameFile();
                if (!file.Set("a.mod", "Level1", "g1", true)) return "first choice refused";
                if (!file.Set("a.mod", "Level1", "g1", false) || file.Entries.Count != 1 || file.Entries[0].Tame) return "the same animal did not update its choice";
                file.Set("b.mod", "Level1", "g1", true);
                if (file.Entries.Count != 2) return "a second mod on the same animal was not kept apart";
                file.Set("a.mod", "Level2", "g1", true);
                if (file.Entries.Count != 3) return "the same animal guid in another scene was not kept apart";
                if (file.For("Level1", "g1", m => true).Count != 2) return "restore did not find both mods in the scene";
                if (file.For("Level1", "g1", m => m == "b.mod").Count != 1) return "restore ignored the installed filter";
                if (file.For("Level3", "g1", m => true).Count != 0) return "restore ran in a scene with no choice";
                if (file.For("Level1", "g2", m => true).Count != 0) return "restore matched another animal";
                if (AnimalTameFile.Clean(file).Entries.Count != 3) return "a choice of an absent mod was not kept through a save and load";
                AnimalTameFile drop = new AnimalTameFile();
                drop.Set("a.mod", "Level1", "g1", true);
                drop.Set("b.mod", "Level1", "g1", true);
                drop.Set("a.mod", "Level1", "g2", true);
                if (drop.DropStale(m => m == "a.mod", e => e.Scene == "Level1" && e.Guid == "g1" ? (bool?)false : null) != 1 || drop.Entries.Count != 2 || drop.Count("b.mod") != 1) return "an installed choice that differs from its live animal was not dropped, or an absent mod's choice was dropped";
                if (file.Forget("a.mod") != 2 || file.Entries.Count != 1 || file.Count("b.mod") != 1) return "clear did not drop only that mod";
                AnimalTameFile full = new AnimalTameFile();
                for (int i = 0; i < AnimalTameFile.MaxEntries; i++) full.Set("m", "s", "g" + i, true);
                if (full.Set("m", "s", "extra", true) || !full.Set("m", "s", "g0", false) || !full.Set("o", "s", "extra", true)) return "the cap is not per mod, or refused an existing animal or accepted a new one";
                if (AnimalTameFile.Clean(null).Entries.Count != 0) return "a missing file did not load empty";
                AnimalTameFile raw = new AnimalTameFile();
                raw.Entries.Add(null);
                raw.Entries.Add(new AnimalTameEntry { Mod = "bad/mod", Scene = "s", Guid = "g", Tame = true });
                raw.Entries.Add(new AnimalTameEntry { Mod = "ok", Scene = "s", Guid = "", Tame = true });
                raw.Entries.Add(new AnimalTameEntry { Mod = "ok", Scene = "s", Guid = "g", Tame = true });
                raw.Entries.Add(new AnimalTameEntry { Mod = "ok", Scene = "s", Guid = "g", Tame = false });
                AnimalTameFile cleaned = AnimalTameFile.Clean(raw);
                if (cleaned.Entries.Count != 1 || cleaned.Entries[0].Tame) return "a bad or duplicate saved choice was kept, or the last choice did not win";
                AnimalTameFile many = new AnimalTameFile();
                for (int i = 0; i <= AnimalTameFile.MaxTotalEntries; i++) many.Entries.Add(new AnimalTameEntry { Mod = "m" + (i / AnimalTameFile.MaxEntries), Scene = "s", Guid = "g" + i, Tame = true });
                AnimalTameFile capped = AnimalTameFile.Clean(many);
                if (capped.Entries.Count != AnimalTameFile.MaxTotalEntries || capped.Set("z", "s", "g", true)) return "the total cap was not applied on load or on a new choice";
            }
            catch (Exception e)
            {
                return "exception: " + e.Message;
            }
            return "animal tame self-check ok";
        }
    }
}
