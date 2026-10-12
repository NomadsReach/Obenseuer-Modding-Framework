using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    public sealed class BookEntryInfo
    {
        public string Id { get; private set; }
        public string Title { get; private set; }
        public int PageCount { get; private set; }

        internal BookEntryInfo(string id, string title, int pageCount)
        {
            Id = id;
            Title = title;
            PageCount = pageCount;
        }
    }

    public sealed class BookReadArgs
    {
        public string Id { get; private set; }
        public string Title { get; private set; }

        internal BookReadArgs(string id, string title)
        {
            Id = id;
            Title = title;
        }
    }

    public static class Books
    {
        internal const string Owner = "obenseuer.moddingframework.books";
        private static bool installed;

        public static event Action<BookReadArgs> Read;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<BookEntryInfo> All()
        {
            return Guard(() => Collect(), new List<BookEntryInfo>(), "All");
        }

        public static bool IsRead(string id)
        {
            if (id == null) throw new ArgumentException("id must not be null", "id");
            return Guard(() => FindRead(id), false, "IsRead");
        }

        internal static void Init()
        {
            installed = BooksPatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("book events not installed, Read is disabled");
        }

        internal static void RaiseRead(BookInfo book)
        {
            BookReadArgs args = new BookReadArgs(book.Id, book.title ?? "");
            Events.Raise(Read, "Books.Read", h => h(args));
        }

        private static List<CollectedNoteInfo> Notes()
        {
            NoteController controller = NoteController.instance;
            return controller == null || controller.notes == null ? new List<CollectedNoteInfo>() : controller.notes;
        }

        private static List<BookEntryInfo> Collect()
        {
            List<BookEntryInfo> books = new List<BookEntryInfo>();
            foreach (CollectedNoteInfo note in Notes())
            {
                if (note == null || note.book == null) continue;
                books.Add(new BookEntryInfo(note.book.Id ?? "", note.book.title ?? "", note.book.pages == null ? 0 : note.book.pages.Count));
            }
            return books;
        }

        private static bool FindRead(string id)
        {
            foreach (CollectedNoteInfo note in Notes())
            {
                if (note != null && note.book != null && note.book.Id == id) return note.read;
            }
            return false;
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
