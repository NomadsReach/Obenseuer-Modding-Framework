using System;
using System.IO;

namespace ObenseuerCore
{
    internal static class ModIds
    {
        private const int MaxLength = 64;
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
            if (string.IsNullOrEmpty(modId) || modId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || modId.IndexOfAny(new[] { '/', '\\' }) >= 0 || modId.Contains(".."))
            {
                throw new ArgumentException("invalid mod id: " + modId, param);
            }
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
