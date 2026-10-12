using System;
using System.Collections.Generic;
using System.Text;

namespace ObenseuerCore
{
    internal static class RegistryIds
    {
        internal const string Owner = "obenseuer.moddingframework.registry";
        internal const int FirstId = 1000000;
        internal const int IdCount = 1999000000;
        internal const int Refused = -1;

        internal static int IdFor(string modGuid, string key)
        {
            ModIds.Require(modGuid, "modGuid");
            if (!ModIds.IsValid(key)) throw new ArgumentException("key must be 1 to 64 characters of letters, digits, '.', '_' or '-'", "key");
            return FirstId + (int)(Fnv1a32(Encoding.UTF8.GetBytes(modGuid + "/" + key)) % (uint)IdCount);
        }

        internal static uint Fnv1a32(byte[] bytes)
        {
            uint hash = 2166136261u;
            foreach (byte b in bytes)
            {
                hash = unchecked((hash ^ b) * 16777619u);
            }
            return hash;
        }

        internal static void CheckText(string value, int min, int max, string field)
        {
            if (value != null && (value.Length < min || value.Length > max))
            {
                throw new ArgumentException(field + " must be " + min + " to " + max + " characters", "definition");
            }
        }

        internal static bool Claim(Dictionary<int, string> owners, int id, string owner)
        {
            string taken;
            if (!owners.TryGetValue(id, out taken))
            {
                owners.Add(id, owner);
                return true;
            }
            if (taken != owner)
            {
                throw new ArgumentException("id " + id + " is already used by " + taken, "key");
            }
            return false;
        }
    }
}
