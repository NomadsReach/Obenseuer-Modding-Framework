using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using BepInEx;

namespace ObenseuerCore
{
    internal static class ModIds
    {
        private const int MaxLength = 64;
        private static readonly Regex ReservedName = new Regex("^(CON|PRN|AUX|NUL|(COM|LPT)(\\d|[\\u00B9\\u00B2\\u00B3]))$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        internal const string CoreId = "obenseuer.moddingframework";
        internal const string LegacyCoreId = "nomad.obenseuer.core";

        internal static void Require(string modId, string param)
        {
            if (!IsValid(modId)) throw new ArgumentException(param + " must be 1 to 64 characters of letters, digits, '.', '_' or '-'", param);
        }

        internal static void RequireLegacy(string modId, string param)
        {
            if (string.IsNullOrEmpty(modId)) throw new ArgumentException(param + " must be a non-empty mod id", param);
        }

        internal static void RequireLegacyFileName(string modId, string param)
        {
            if (string.IsNullOrEmpty(modId) || modId.Length > MaxLength || modId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || modId.IndexOfAny(new[] { '/', '\\' }) >= 0 || modId.Contains("..") || IsReservedName(modId))
            {
                throw new ArgumentException("invalid mod id: " + modId, param);
            }
        }

        internal static bool IsReservedName(string name)
        {
            return ReservedName.IsMatch(name.Split('.')[0].TrimEnd(' '));
        }

        internal static string LegacyOf(string modId)
        {
            if (modId == CoreId) return LegacyCoreId;
            if (modId != null && modId.StartsWith(CoreId + ".", StringComparison.Ordinal)) return LegacyCoreId + modId.Substring(CoreId.Length);
            return null;
        }

        internal static string LoadId(string modId, Func<string, bool> hasFile, Func<string, DateTime> writeTimeUtc)
        {
            string legacy = LegacyOf(modId);
            if (legacy == null || !hasFile(legacy)) return modId;
            if (!hasFile(modId)) return legacy;
            return writeTimeUtc(legacy) > writeTimeUtc(modId) ? legacy : modId;
        }

        internal static bool IsInstalled(string modId, HashSet<string> usedThisSession)
        {
            if (usedThisSession.Contains(modId)) return true;
            PluginInfo info = Loader.Find(modId);
            return info != null && info.Instance != null;
        }

        internal static bool IsValid(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length > MaxLength) return false;
            foreach (char c in text)
            {
                bool ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '.' || c == '_' || c == '-';
                if (!ok) return false;
            }
            return true;
        }
    }
}
