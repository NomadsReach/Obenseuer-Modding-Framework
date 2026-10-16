using System;
using System.Collections.Generic;
using System.IO;

namespace ObenseuerCore
{
    internal sealed class SceneLookFiles : IDisposable
    {
        private readonly string dataDir;
        private readonly Dictionary<string, SceneLookFile> open = new Dictionary<string, SceneLookFile>();
        private readonly Dictionary<string, string> refused = new Dictionary<string, string>();

        internal SceneLookFiles(string dataDir)
        {
            this.dataDir = dataDir;
        }

        internal SceneLookFile Get(string name, out string reason)
        {
            reason = null;
            SceneLookFile file;
            if (open.TryGetValue(name, out file)) return file;
            if (refused.TryGetValue(name, out reason)) return null;
            if (!IsPlainName(name)) reason = "bad file name " + name;
            else if (!Installed(name, 0)) reason = "not installed " + name;
            else
            {
                file = SceneLookFile.Open(this, Path.Combine(dataDir, name), name, out reason);
                if (file != null)
                {
                    open[name] = file;
                    return file;
                }
            }
            refused[name] = reason;
            return null;
        }

        internal void CloseAllExcept(string keep)
        {
            foreach (string name in new List<string>(open.Keys))
            {
                if (name == keep) continue;
                open[name].Dispose();
                open.Remove(name);
            }
        }

        internal byte[] ReadStream(string name, long offset, long count)
        {
            if (!IsPlainName(name)) throw new InvalidDataException("bad stream name " + name);
            if (count < 0 || count > int.MaxValue) throw new InvalidDataException("bad stream size " + count);
            using (FileStream stream = new FileStream(Path.Combine(dataDir, name), FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (offset < 0 || offset > stream.Length - count) throw new InvalidDataException("stream is outside " + name);
                return SceneLookFile.ReadAt(stream, offset, (int)count);
            }
        }

        internal bool Installed(string name, long minimumLength)
        {
            if (!IsPlainName(name)) return false;
            try
            {
                string path = Path.Combine(dataDir, name);
                return File.Exists(path) && new FileInfo(path).Length >= minimumLength;
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal static bool IsPlainName(string name)
        {
            return !string.IsNullOrEmpty(name) && Path.GetFileName(name) == name && name.IndexOf(':') < 0;
        }

        public void Dispose()
        {
            foreach (SceneLookFile file in open.Values) file.Dispose();
            open.Clear();
        }
    }
}
