using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using UnityEngine;

namespace ObenseuerCore
{
    public class CoreLog
    {
        private static readonly Dictionary<string, CoreLog> loggers = new Dictionary<string, CoreLog>();
        private readonly Dictionary<string, string> lastByKey = new Dictionary<string, string>();
        private StreamWriter writer;

        private CoreLog(StreamWriter writer)
        {
            this.writer = writer;
        }

        public static CoreLog For(string modId)
        {
            CoreLog log;
            if (!loggers.TryGetValue(modId, out log))
            {
                log = new CoreLog(Open(modId));
                loggers[modId] = log;
            }
            return log;
        }

        public void Info(string message)
        {
            Write("INFO", message);
        }

        public void Warn(string message)
        {
            Write("WARN", message);
        }

        public void Error(string message)
        {
            Write("ERROR", message);
        }

        public void Debug(string key, string message)
        {
            if (!Core.Verbose) return;
            string last;
            if (lastByKey.TryGetValue(key, out last) && last == message) return;
            lastByKey[key] = message;
            Write("DEBUG", key + ": " + message);
        }

        private static StreamWriter Open(string modId)
        {
            try
            {
                string dir = Path.Combine(Paths.BepInExRootPath, "logs");
                Directory.CreateDirectory(dir);
                return new StreamWriter(Path.Combine(dir, modId + ".log"), false) { AutoFlush = true };
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("ObenseuerCore: cannot open log for " + modId + ": " + e.Message);
                return null;
            }
        }

        private void Write(string level, string message)
        {
            if (writer == null) return;
            try
            {
                writer.WriteLine(string.Format("[frame {0}] [{1:F3}] {2}: {3}", Time.frameCount, Time.unscaledTime, level, message));
            }
            catch (Exception e)
            {
                writer = null;
                UnityEngine.Debug.LogError("ObenseuerCore: log write failed: " + e.Message);
            }
        }
    }
}
