using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class NoteEntryInfo
    {
        public string Id { get; private set; }
        public string Title { get; private set; }

        internal NoteEntryInfo(string id, string title)
        {
            Id = id;
            Title = title;
        }
    }

    public sealed class NoteCollectedArgs
    {
        public string Id { get; private set; }
        public string Title { get; private set; }

        internal NoteCollectedArgs(string id, string title)
        {
            Id = id;
            Title = title;
        }
    }

    public static class Notes
    {
        internal const string Owner = "obenseuer.moddingframework.notes";
        private static bool installed;
        private static List<NoteInfo> cache;

        public static event Action<NoteCollectedArgs> Collected;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<NoteEntryInfo> All()
        {
            return Guard(() => Catalogue(), new List<NoteEntryInfo>(), "All");
        }

        public static List<NoteEntryInfo> Held()
        {
            return Guard(() => Collect(), new List<NoteEntryInfo>(), "Held");
        }

        public static bool Has(string id)
        {
            if (id == null) throw new ArgumentException("id must not be null", "id");
            return Guard(() => Holds(Find(id)), false, "Has");
        }

        public static bool Give(string id)
        {
            if (id == null) throw new ArgumentException("id must not be null", "id");
            return Guard(() => GiveNote(id), false, "Give");
        }

        internal static void Init()
        {
            installed = NotesPatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("note events not installed, Collected is disabled");
        }

        internal static void RaiseCollected(NoteInfo note)
        {
            NoteCollectedArgs args = new NoteCollectedArgs(note.Id ?? "", note.title ?? "");
            Events.Raise(Collected, "Notes.Collected", h => h(args));
        }

        internal static bool Holds(NoteInfo note)
        {
            NoteController controller = NoteController.instance;
            if (note == null || controller == null || controller.notes == null) return false;
            foreach (CollectedNoteInfo entry in controller.notes)
            {
                if (entry != null && entry.book == null && entry.note != null && note.Compare(entry.note)) return true;
            }
            return false;
        }

        private static bool GiveNote(string id)
        {
            NoteController controller = NoteController.instance;
            NoteInfo note = Find(id);
            if (controller == null || note == null || Holds(note)) return false;
            controller.AddNote(note);
            return Holds(note);
        }

        private static List<NoteInfo> Assets()
        {
            if (cache != null && Alive(cache)) return cache;
            List<NoteInfo> assets = new List<NoteInfo>();
            foreach (NoteInfo note in Resources.LoadAll<NoteInfo>("Notes"))
            {
                if (note != null && note.book == null) assets.Add(note);
            }
            cache = assets.Count > 0 ? assets : null;
            return assets;
        }

        private static bool Alive(List<NoteInfo> notes)
        {
            foreach (NoteInfo note in notes)
            {
                if (note == null) return false;
            }
            return true;
        }

        private static NoteInfo Find(string id)
        {
            NoteInfo found = null;
            int matches = 0;
            foreach (NoteInfo note in Assets())
            {
                if (note.Id != id) continue;
                found = note;
                matches++;
            }
            return matches == 1 ? found : null;
        }

        private static List<NoteEntryInfo> Catalogue()
        {
            List<NoteEntryInfo> notes = new List<NoteEntryInfo>();
            foreach (NoteInfo note in Assets())
            {
                notes.Add(new NoteEntryInfo(note.Id ?? "", note.title ?? ""));
            }
            return notes;
        }

        private static List<NoteEntryInfo> Collect()
        {
            List<NoteEntryInfo> notes = new List<NoteEntryInfo>();
            NoteController controller = NoteController.instance;
            if (controller == null || controller.notes == null) return notes;
            foreach (CollectedNoteInfo entry in controller.notes)
            {
                if (entry != null && entry.book == null && entry.note != null) notes.Add(new NoteEntryInfo(entry.note.Id ?? "", entry.note.title ?? ""));
            }
            return notes;
        }

        private static T Guard<T>(Func<T> read, T fallback, string name)
        {
            try
            {
                return read();
            }
            catch (Exception e)
            {
                CoreLog.For(Owner).Debug(name, name + " failed: " + e.ToString());
                return fallback;
            }
        }
    }
}
