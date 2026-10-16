using System;
using System.Collections.Generic;
using System.IO;

namespace ObenseuerCore
{
    internal struct SceneLookEntry
    {
        public long Start;
        public int Size;
        public int ClassId;
    }

    internal sealed class SceneLookTarget
    {
        public SceneLookFile File;
        public long PathId;
        public string Error;
    }

    internal sealed class SceneLookFile : IDisposable
    {
        internal const string UnityVersion = "6000.0.77f1";
        private const int FormatVersion = 22;
        private const int HeaderSize = 48;

        private readonly SceneLookFiles files;
        private readonly FileStream stream;
        private readonly string name;
        private readonly string unity;
        private readonly long dataOffset;
        private readonly long length;
        private readonly Dictionary<long, SceneLookEntry> entries = new Dictionary<long, SceneLookEntry>();
        private readonly List<long> order = new List<long>();
        private readonly List<string> externals = new List<string>();
        private readonly SceneLookMemo memo = new SceneLookMemo();
        private long bytesRead;

        private SceneLookFile(SceneLookFiles files, string name, FileStream stream)
        {
            this.files = files;
            this.name = name;
            this.stream = stream;
            byte[] header = ReadAt(stream, 0, HeaderSize);
            int version = BigEndianInt(header, 8);
            if (version != FormatVersion) throw new InvalidDataException("serialized format " + version + " is not handled");
            if (header[16] != 0) throw new InvalidDataException("big-endian metadata is not handled");
            int metadataSize = BigEndianInt(header, 20);
            long start = BigEndianLong(header, 32);
            length = stream.Length;
            if (metadataSize <= 0 || HeaderSize + (long)metadataSize > length || start < 0 || start > length) throw new InvalidDataException("metadata is outside the file");
            dataOffset = start;
            SceneLookCursor cursor = new SceneLookCursor(ReadAt(stream, HeaderSize, metadataSize), 0);
            unity = cursor.CString();
            if (unity != UnityVersion) throw new InvalidDataException("Unity version " + unity + " is not " + UnityVersion);
            cursor.I32();
            if (cursor.U8() != 0) throw new InvalidDataException("type trees are not handled");
            int types = cursor.Count();
            if (types > cursor.Length) throw new InvalidDataException("type count is larger than the metadata");
            int[] classIds = new int[types];
            for (int i = 0; i < types; i++)
            {
                classIds[i] = cursor.I32();
                cursor.U8();
                cursor.I16();
                if (classIds[i] == SceneLookClass.MonoBehaviour) cursor.Skip(16);
                cursor.Skip(16);
            }
            int objects = cursor.Count();
            cursor.Align(4);
            long room = length - dataOffset;
            for (int i = 0; i < objects; i++)
            {
                long pathId = cursor.I64();
                long objectStart = cursor.I64();
                uint size = cursor.U32();
                int typeIndex = cursor.I32();
                if (size > (uint)int.MaxValue || typeIndex < 0 || typeIndex >= types || objectStart < 0 || objectStart > room || (long)size > room - objectStart) throw new InvalidDataException("object record " + pathId + " is invalid");
                SceneLookEntry entry = new SceneLookEntry();
                entry.Start = objectStart;
                entry.Size = (int)size;
                entry.ClassId = classIds[typeIndex];
                entries[pathId] = entry;
                order.Add(pathId);
            }
            int scripts = cursor.Count();
            cursor.Skip(12L * scripts);
            int external = cursor.Count();
            for (int i = 0; i < external; i++)
            {
                cursor.CString();
                cursor.Skip(16);
                cursor.I32();
                externals.Add(cursor.CString());
            }
            int refTypes = cursor.Count();
            for (int i = 0; i < refTypes; i++)
            {
                cursor.I32();
                cursor.U8();
                cursor.I16();
                cursor.Skip(16);
                cursor.Skip(16);
            }
            cursor.CString();
            if (cursor.Position != cursor.Length) throw new InvalidDataException("metadata is not fully read (" + cursor.Position + " of " + cursor.Length + ")");
        }

        internal string Name
        {
            get { return name; }
        }

        internal string UnityString
        {
            get { return unity; }
        }

        internal IList<long> Order
        {
            get { return order; }
        }

        internal SceneLookMemo Memo
        {
            get { return memo; }
        }

        internal long BytesRead
        {
            get { return bytesRead; }
        }

        internal static SceneLookFile Open(SceneLookFiles files, string path, string name, out string reason)
        {
            reason = null;
            FileStream stream = null;
            try
            {
                stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                return new SceneLookFile(files, name, stream);
            }
            catch (Exception e)
            {
                if (stream != null) stream.Dispose();
                reason = e.Message;
                return null;
            }
        }

        internal bool Contains(long pathId)
        {
            return entries.ContainsKey(pathId);
        }

        internal int ClassId(long pathId)
        {
            return Entry(pathId).ClassId;
        }

        internal byte[] ReadObject(long pathId)
        {
            return ReadObjectHead(pathId, int.MaxValue);
        }

        internal byte[] ReadObjectHead(long pathId, int limit)
        {
            SceneLookEntry entry = Entry(pathId);
            int count = Math.Min(limit, entry.Size);
            byte[] bytes = ReadAt(stream, dataOffset + entry.Start, count);
            bytesRead += count;
            return bytes;
        }

        internal SceneLookTarget Target(SceneLookRef reference)
        {
            SceneLookTarget target = new SceneLookTarget();
            target.PathId = reference.PathId;
            if (reference.PathId == 0)
            {
                target.Error = "null";
                return target;
            }
            if (reference.FileId == 0)
            {
                target.File = this;
            }
            else if (reference.FileId < 0 || reference.FileId > externals.Count)
            {
                target.Error = "bad fileID";
                return target;
            }
            else
            {
                string reason;
                target.File = files.Get(externals[reference.FileId - 1], out reason);
                if (target.File == null)
                {
                    target.Error = "external not installed";
                    return target;
                }
            }
            if (!target.File.Contains(reference.PathId)) target.Error = "pptr target missing";
            return target;
        }

        internal static byte[] ReadAt(FileStream stream, long offset, int count)
        {
            byte[] bytes = new byte[count];
            stream.Seek(offset, SeekOrigin.Begin);
            int done = 0;
            while (done < count)
            {
                int read = stream.Read(bytes, done, count - done);
                if (read <= 0) throw new InvalidDataException("file ends early");
                done += read;
            }
            return bytes;
        }

        public void Dispose()
        {
            stream.Dispose();
        }

        private SceneLookEntry Entry(long pathId)
        {
            SceneLookEntry entry;
            if (!entries.TryGetValue(pathId, out entry)) throw new InvalidDataException("object " + pathId + " is not in " + name);
            return entry;
        }

        private static int BigEndianInt(byte[] bytes, int at)
        {
            return (bytes[at] << 24) | (bytes[at + 1] << 16) | (bytes[at + 2] << 8) | bytes[at + 3];
        }

        private static long BigEndianLong(byte[] bytes, int at)
        {
            long value = 0;
            for (int i = 0; i < 8; i++) value = (value << 8) | bytes[at + i];
            return value;
        }
    }
}
