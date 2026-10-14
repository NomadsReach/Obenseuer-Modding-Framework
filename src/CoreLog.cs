using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;
using UnityEngine;

namespace ObenseuerCore
{
    public class CoreLog
    {
        private const string CoreFile = "ObenseuerCore";
        private const int MaxPrefix = 64;
        private const long MaxBytes = 4L * 1024 * 1024;
        private const float RetrySeconds = 5f;
        private const float ErrorRepeatSeconds = 10f;
        private const float ErrorWindowSeconds = 10f;
        private const int MaxErrorLines = 20;
        private const int MaxErrorKeys = 256;
        private const long HardMaxBytes = 8L * 1024 * 1024;
        private const int MaxLinesPerSecond = 100;
        private static readonly Dictionary<string, CoreLog> loggers = new Dictionary<string, CoreLog>(StringComparer.OrdinalIgnoreCase);
        internal static CoreLog Mirror;
        private readonly Dictionary<string, string> lastByKey = new Dictionary<string, string>();
        private readonly Dictionary<string, float> errorAt = new Dictionary<string, float>();
        private readonly string path;
        private readonly CoreLog core;
        private readonly string prefix;
        private StreamWriter writer;
        private float retryAt;
        private bool reported;
        private float windowStart;
        private int windowLines;
        private int suppressed;
        private float errorWindowStart;
        private int errorWindowLines;
        private int errorDropped;
        private readonly string id;
        private int lineLimit = MaxLinesPerSecond;

        private CoreLog(string modId)
        {
            id = modId;
            try
            {
                string dir = Path.Combine(Paths.BepInExRootPath, "logs");
                Directory.CreateDirectory(dir);
                path = Path.Combine(dir, modId + ".log");
                Fresh();
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        private CoreLog(CoreLog core, string prefix)
        {
            this.core = core;
            this.prefix = prefix;
        }

        public static CoreLog For(string modId)
        {
            string key = modId ?? "";
            CoreLog log;
            if (!loggers.TryGetValue(key, out log))
            {
                log = Usable(key) ? new CoreLog(key) : new CoreLog(For(CoreFile), "[" + Sanitised(key) + "] ");
                loggers[key] = log;
            }
            return log;
        }

        internal void SetLineLimit(int limit)
        {
            lineLimit = limit;
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
            ErrorOnce("message " + (message ?? ""), message);
        }

        public void Error(string message, Exception e)
        {
            if (e == null)
            {
                Error(message);
                return;
            }
            ErrorOnce("exception " + e.GetType().FullName + " " + FirstFrame(e), message + ": " + e);
        }

        private void ErrorOnce(string key, string text)
        {
            float now = Time.unscaledTime;
            float last;
            if (!CoreDebug.Enabled && errorAt.TryGetValue(key, out last) && now - last < ErrorRepeatSeconds) return;
            if (!CoreDebug.Enabled && !ErrorBudget(now)) return;
            if (errorAt.Count >= MaxErrorKeys) errorAt.Clear();
            errorAt[key] = now;
            Write("ERROR", text);
        }

        private bool ErrorBudget(float now)
        {
            if (now - errorWindowStart >= ErrorWindowSeconds)
            {
                SummariseErrors();
                errorWindowStart = now;
                errorWindowLines = 0;
            }
            if (errorWindowLines < MaxErrorLines)
            {
                errorWindowLines++;
                return true;
            }
            errorDropped++;
            return false;
        }

        private void SummariseErrors()
        {
            if (errorDropped == 0) return;
            int count = errorDropped;
            errorDropped = 0;
            Emit("WARN", count + " error lines suppressed");
        }

        private static string FirstFrame(Exception e)
        {
            string trace = e.StackTrace ?? "";
            int end = trace.IndexOf('\n');
            return end < 0 ? trace : trace.Substring(0, end);
        }

        public void Debug(string key, string message)
        {
            if (!Core.Verbose) return;
            string last;
            if (lastByKey.TryGetValue(key, out last) && last == message) return;
            lastByKey[key] = message;
            Write("DEBUG", key + ": " + message);
        }

        private static bool Usable(string modId)
        {
            try
            {
                ModIds.RequireLegacyFileName(modId, "modId");
            }
            catch (ArgumentException)
            {
                return false;
            }
            return modId.TrimEnd(' ', '.').Length == modId.Length && !ModIds.IsReservedName(modId);
        }

        private static string Sanitised(string modId)
        {
            StringBuilder text = new StringBuilder();
            foreach (char c in modId)
            {
                if (text.Length == MaxPrefix) break;
                text.Append(char.IsControl(c) || Array.IndexOf(Path.GetInvalidFileNameChars(), c) >= 0 ? '_' : c);
            }
            return text.ToString();
        }

        private void Fresh()
        {
            Rotate();
            writer = Open(true);
        }

        private void Rotate()
        {
            try
            {
                if (!File.Exists(path)) return;
                string old = path + ".1";
                if (File.Exists(old)) File.Replace(path, old, null);
                else File.Move(path, old);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private StreamWriter Open(bool append)
        {
            return new StreamWriter(path, append) { AutoFlush = true };
        }

        private void Write(string level, string message)
        {
            float now = Time.unscaledTime;
            if (now - windowStart >= 1f)
            {
                windowStart = now;
                windowLines = 0;
                Summarise();
            }
            if (windowLines >= lineLimit)
            {
                suppressed++;
                return;
            }
            windowLines++;
            Emit(level, message);
        }

        private void Summarise()
        {
            if (suppressed == 0) return;
            int count = suppressed;
            suppressed = 0;
            Emit("WARN", count + " lines suppressed");
        }

        internal static void FlushSuppressed()
        {
            foreach (CoreLog log in new List<CoreLog>(loggers.Values))
            {
                log.Summarise();
                log.SummariseErrors();
            }
            CoreLog coreLog;
            if (loggers.TryGetValue(CoreFile, out coreLog))
            {
                coreLog.Summarise();
                coreLog.SummariseErrors();
            }
        }

        private void Emit(string level, string message)
        {
            if (core != null)
            {
                core.Write(level, prefix + message);
                return;
            }
            if (writer == null && Time.unscaledTime < retryAt) return;
            try
            {
                if (writer == null) writer = Open(true);
                if (writer.BaseStream.Position > MaxBytes)
                {
                    writer.Close();
                    Rotate();
                    writer = Open(true);
                }
                if (writer.BaseStream.Position > HardMaxBytes) return;
                writer.WriteLine(string.Format("[frame {0}] [{1:F3}] {2}: {3}", Time.frameCount, Time.unscaledTime, level, message));
                if (Mirror != null && Mirror != this) lock (CoreDebug.writeLock) Mirror.Write(level, id + ": " + message);
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        private void Fail(Exception e)
        {
            StreamWriter broken = writer;
            writer = null;
            retryAt = Time.unscaledTime + RetrySeconds;
            if (!reported)
            {
                reported = true;
                UnityEngine.Debug.LogError("ObenseuerCore: log failed, retrying: " + e.Message);
            }
            try
            {
                if (broken != null) broken.Close();
            }
            catch (IOException)
            {
            }
        }
    }
}
