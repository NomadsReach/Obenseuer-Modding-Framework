using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    internal static class NewGameOptionsSelfTest
    {
        internal const string Ok = "new game options self-check ok";

        internal static string Run()
        {
            try
            {
                NewGameOptionStore store = new NewGameOptionStore();
                if (store.RegisterToggle("example.options", "loud", "Loud", "Play a sound", true) != NewGameOptionResult.Registered) return "the first registration was refused";
                if (store.RegisterToggle("example.options", "loud", "Loud again", "", false) != NewGameOptionResult.Duplicate) return "a repeated modId and key was accepted";
                if (store.RegisterToggle("example.other", "loud", "Loud", "", false) != NewGameOptionResult.Registered) return "another mod using the same key was refused";
                if (!store.IsRegistered("example.options", "loud")) return "a registered option was not found";
                if (store.IsRegistered("example.options", "quiet")) return "an unregistered option was found";
                if (store.Value("example.options", "quiet") != null) return "an absent key returned a value";
                if (store.Options().Count != 2) return "the option list does not hold both registrations";
                if (store.Options()[0].DefaultBool != true) return "the toggle default was lost";
                if (store.Options()[1].ModId != "example.other") return "the option list is out of registration order";
                if (!Throws(delegate { store.RegisterToggle("bad id!", "key", "Title", "", false); })) return "a bad modId was accepted";
                if (!Throws(delegate { store.RegisterToggle("example.options", "", "Title", "", false); })) return "an empty key was accepted";
                if (!Throws(delegate { store.RegisterToggle("example.options", "key", "", "", false); })) return "an empty title was accepted";
                if (!Throws(delegate { store.RegisterChoice("example.options", "key", "Title", "", null, new string[] { "A" }, 0); })) return "null ids were accepted";
                if (!Throws(delegate { store.RegisterChoice("example.options", "key", "Title", "", new string[] { "A" }, null, 0); })) return "null titles were accepted";
                if (!Throws(delegate { store.RegisterChoice("example.options", "key", "Title", "", new string[0], new string[0], 0); })) return "empty ids were accepted";
                if (!Throws(delegate { store.RegisterChoice("example.options", "key", "Title", "", new string[] { "" }, new string[] { "A" }, 0); })) return "an empty id was accepted";
                if (!Throws(delegate { store.RegisterChoice("example.options", "key", "Title", "", new string[] { "A" }, new string[] { "" }, 0); })) return "an empty title in the list was accepted";
                if (!Throws(delegate { store.RegisterChoice("example.options", "key", "Title", "", new string[] { "A", "B" }, new string[] { "A" }, 0); })) return "mismatched ids and titles were accepted";
                if (!Throws(delegate { store.RegisterChoice("example.options", "key", "Title", "", new string[] { "A" }, new string[] { "A" }, 1); })) return "a default index past the end was accepted";
                if (!Throws(delegate { store.RegisterChoice("example.options", "key", "Title", "", new string[] { "A" }, new string[] { "A" }, -1); })) return "a negative default index was accepted";
                if (!Throws(delegate { store.RegisterInteger("example.options", "key", "Title", "", 5, 2, 0); })) return "min greater than max was accepted";
                if (store.RegisterInteger("example.options", "count", "Count", "", 1, 10, 3) != NewGameOptionResult.Registered) return "a valid registration after a refused one failed";
                store.CapturePending();
                if (!store.IsFrozen) return "the option list was not fixed when a new game started";
                if (store.RegisterToggle("example.options", "late", "Late", "", false) != NewGameOptionResult.Late) return "a registration after the option list was fixed was accepted";

                NewGameOptionStore values = new NewGameOptionStore();
                values.RegisterInteger("example.options", "count", "Count", "", 1, 10, 3);
                if (values.Value("example.options", "count") != null) return "a value existed before a new game";
                values.Choose("example.options", "count", "7");
                if (values.Value("example.options", "count") != "7") return "the draft was not readable";
                values.CapturePending();
                if (values.Value("example.options", "count") != "7") return "the pending value is not readable before the first save";
                values.Load(null, false);
                if (values.Value("example.options", "count") != "7") return "a reset dropped the pending value";
                NewGameOptionsFile file = values.BuildForSave();
                if (file == null || file.Entries.Count != 1) return "the pending value was not written to the save file";
                if (file.Entries[0].ModId != "example.options" || file.Entries[0].Key != "count" || file.Entries[0].Value != "7") return "the written entry does not name the option";
                if (values.Value("example.options", "count") != "7") return "the saved value is not readable after the first save";
                if (values.BuildForSave() == null) return "a second save dropped the saved value";
                values.Load(null, true);
                if (values.Value("example.options", "count") != null) return "a load with no file kept the pending value";
                if (values.BuildForSave() != null) return "an empty store produced a save file";

                List<NewGameOptionSavedEntry> saved = new List<NewGameOptionSavedEntry>();
                saved.Add(new NewGameOptionSavedEntry { ModId = "example.options", Key = "mode", Value = "mid" });
                NewGameOptionStore first = new NewGameOptionStore();
                first.RegisterChoice("example.options", "mode", "Mode", "", new string[] { "low", "mid", "high" }, new string[] { "Low", "Mid", "High" }, 0);
                NewGameOptionStore second = new NewGameOptionStore();
                second.RegisterChoice("example.options", "mode", "Mode", "", new string[] { "high", "low", "mid" }, new string[] { "High", "Low", "Mid" }, 0);
                first.Load(saved, true);
                second.Load(saved, true);
                if (first.Value("example.options", "mode") != "mid" || second.Value("example.options", "mode") != "mid") return "a choice id did not survive a reordered list";
                if (first.Options()[0].Ids[0] != "low" || second.Options()[0].Ids[0] != "high") return "the two choice lists were not kept apart";
                if (second.Options()[0].Ids[second.Options()[0].DefaultIndex] != "high") return "the default index no longer points at the first entry";
            }
            catch (Exception e)
            {
                return "exception: " + e.Message;
            }
            return Ok;
        }

        private static bool Throws(Action action)
        {
            try
            {
                action();
                return false;
            }
            catch (ArgumentException)
            {
                return true;
            }
        }
    }
}
