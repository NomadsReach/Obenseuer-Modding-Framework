using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    internal sealed class PayoutFactors<TKey>
    {
        internal const float MaxFactor = 4f;
        private readonly string owner;
        private readonly List<Entry> entries = new List<Entry>();

        internal PayoutFactors(string owner)
        {
            this.owner = owner;
        }

        internal bool Any
        {
            get
            {
                return entries.Count > 0;
            }
        }

        internal static bool Valid(float factor)
        {
            return factor >= 0f && factor <= MaxFactor;
        }

        internal static int Scale(int amount, float factor)
        {
            if (amount <= 0 || factor == 1f) return amount;
            return (int)Math.Min(Math.Round(amount * (double)factor), Cash.MaxAmount);
        }

        internal float Of(TKey key)
        {
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (EqualityComparer<TKey>.Default.Equals(entries[i].Key, key)) return entries[i].Factor;
            }
            return 1f;
        }

        internal void Set(string modId, TKey key, float factor)
        {
            foreach (Entry entry in entries)
            {
                if (entry.ModId != modId && EqualityComparer<TKey>.Default.Equals(entry.Key, key))
                {
                    ApiLog.Once(owner, "factor " + key + " " + entry.ModId + " " + modId, "factor for " + key + " is set by " + entry.ModId + " and " + modId + ", the newest call (" + modId + ") applies");
                }
            }
            entries.RemoveAll(e => e.ModId == modId && EqualityComparer<TKey>.Default.Equals(e.Key, key));
            entries.Add(new Entry { ModId = modId, Key = key, Factor = factor });
        }

        internal bool Clear(string modId)
        {
            return entries.RemoveAll(e => e.ModId == modId) > 0;
        }

        internal void Reset()
        {
            entries.Clear();
        }

        private sealed class Entry
        {
            internal string ModId;
            internal TKey Key;
            internal float Factor;
        }
    }
}
