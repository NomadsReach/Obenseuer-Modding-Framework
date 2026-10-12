using System;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class NotesPatches
    {

        internal static bool Install()
        {
            try
            {
                return CorePatcher.PatchCore(typeof(NoteController), "AddNote", new[] { typeof(NoteInfo), typeof(bool) }, CorePatcher.Handler(typeof(NotesPatches), "AddPrefix"), CorePatcher.Handler(typeof(NotesPatches), "AddPostfix"), null, true, Notes.Owner);
            }
            catch (Exception e)
            {
                CoreLog.For(Notes.Owner).Error("note patch install failed: " + e.ToString());
                return false;
            }
        }

        private static void AddPrefix(NoteInfo note, out bool __state)
        {
            __state = false;
            try
            {
                __state = note != null && note.book == null && !Notes.Holds(note);
            }
            catch (Exception e)
            {
                ApiLog.Once(Notes.Owner, "AddNote prefix", e);
            }
        }

        private static void AddPostfix(NoteInfo note, bool __state)
        {
            if (!__state) return;
            try
            {
                if (Notes.Holds(note)) Notes.RaiseCollected(note);
            }
            catch (Exception e)
            {
                ApiLog.Once(Notes.Owner, "AddNote postfix", e);
            }
        }
    }
}
