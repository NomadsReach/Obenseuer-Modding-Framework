using System;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class BooksPatches
    {

        internal static bool Install()
        {
            try
            {
                return CorePatcher.PatchCore(typeof(NoteUIPanel), "Selected", Type.EmptyTypes, CorePatcher.Handler(typeof(BooksPatches), "SelectedPrefix"), CorePatcher.Handler(typeof(BooksPatches), "SelectedPostfix"), null, true, Books.Owner);
            }
            catch (Exception e)
            {
                CoreLog.For(Books.Owner).Error("book patch install failed: " + e.ToString());
                return false;
            }
        }

        private static void SelectedPrefix(NoteUIPanel __instance, out BookInfo __state)
        {
            __state = null;
            try
            {
                CollectedNoteInfo note = __instance.note;
                if (note != null && note.book != null && !note.read) __state = note.book;
            }
            catch (Exception e)
            {
                ApiLog.Once(Books.Owner, "Read prefix", e);
            }
        }

        private static void SelectedPostfix(NoteUIPanel __instance, BookInfo __state)
        {
            if (__state == null) return;
            try
            {
                CollectedNoteInfo note = __instance.note;
                if (note != null && note.read) Books.RaiseRead(__state);
            }
            catch (Exception e)
            {
                ApiLog.Once(Books.Owner, "Read postfix", e);
            }
        }
    }
}
