using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    internal static class CustomSkillsMath
    {
        internal const int MaxPerMod = 64;
        internal const int MaxSkillValue = 1000000;

        internal sealed class BannerQueue
        {
            internal const float Seconds = 9f;
            internal const int MaxCustom = 5;
            internal const int MaxQueued = 5;
            private readonly List<Entry> pending = new List<Entry>();
            private float freeAt;

            internal int Count
            {
                get
                {
                    return pending.Count;
                }
            }

            internal void Enqueue(string text)
            {
                pending.Add(new Entry { Key = null, Text = text });
            }

            internal bool EnqueueCustom(string text)
            {
                if (Queued() >= MaxQueued) return false;
                pending.Add(new Entry { Key = null, Text = text, Queued = true });
                return true;
            }

            internal void EnqueueSkill(string key, string text)
            {
                Entry same = pending.Find(e => e.Key == key);
                if (same != null)
                {
                    same.Text = text;
                    return;
                }
                if (pending.FindAll(e => e.Key != null).Count >= MaxCustom) pending.RemoveAt(pending.FindIndex(e => e.Key != null));
                pending.Add(new Entry { Key = key, Text = text });
            }

            private int Queued()
            {
                int count = 0;
                for (int i = 0; i < pending.Count; i++)
                {
                    if (pending[i].Queued) count++;
                }
                return count;
            }

            internal void Clear()
            {
                pending.Clear();
            }

            internal bool TryClaim(float now)
            {
                if (pending.Count != 0 || now < freeAt) return false;
                freeAt = now + Seconds;
                return true;
            }

            internal bool Next(float now, out string text)
            {
                text = null;
                if (pending.Count == 0 || now < freeAt) return false;
                freeAt = now + Seconds;
                text = pending[0].Text;
                pending.RemoveAt(0);
                return true;
            }

            private sealed class Entry
            {
                internal string Key;
                internal string Text;
                internal bool Queued;
            }
        }

        internal static string Key(string modId, string skillId)
        {
            return modId + ":" + skillId;
        }

        internal static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        internal static float Clamp(float value, float max)
        {
            if (value < 0f) return 0f;
            return value > max ? max : value;
        }

        internal static int Level(float value)
        {
            return (int)Math.Floor(value);
        }

        internal static string LevelUpText(string name, int level)
        {
            return name + "\nincreased to\nlevel " + level + "!";
        }

        internal static string LearnedText(string name)
        {
            return "New skill\n" + name + "\nlearned!";
        }

        internal static bool UnderCap(int registered)
        {
            return registered < MaxPerMod;
        }

        internal static bool Accepts(bool registered, bool busy)
        {
            return registered && !busy;
        }

        internal static bool Valid(CustomSkillSaveEntry entry)
        {
            return entry != null && ModIds.IsValid(entry.Mod) && ModIds.IsValid(entry.Id) && Finite(entry.Value);
        }

        internal static void Load(IList<CustomSkillSaveEntry> entries, IDictionary<string, CustomSkillSaveEntry> values)
        {
            foreach (CustomSkillSaveEntry entry in entries)
            {
                if (Valid(entry)) values[Key(entry.Mod, entry.Id)] = entry;
            }
        }

        internal static List<CustomSkillSaveEntry> Save(IDictionary<string, CustomSkillSaveEntry> values)
        {
            return new List<CustomSkillSaveEntry>(values.Values);
        }

        internal static string SelfCheck()
        {
            if (Clamp(-1f, 10f) != 0f || Clamp(12f, 10f) != 10f || Clamp(4.5f, 10f) != 4.5f) return "clamp is not 0..max";
            if (Level(0.999f) != 0 || Level(1f) != 1 || Level(9.99f) != 9 || Level(10f) != 10) return "level is not the floor of the value";
            if (!Accepts(true, false) || Accepts(false, false) || Accepts(true, true)) return "an unregistered skill or a busy save accepts a change";
            if (!UnderCap(MaxPerMod - 1) || UnderCap(MaxPerMod)) return "the cap per mod is not " + MaxPerMod;
            if (LevelUpText("Music", 3) != "Music\nincreased to\nlevel 3!") return "level-up text differs from the game";
            if (LearnedText("Music") != "New skill\nMusic\nlearned!") return "learned text differs from the game";
            BannerQueue queue = new BannerQueue();
            if (!queue.TryClaim(0f)) return "a free queue does not take a game banner";
            if (queue.TryClaim(BannerQueue.Seconds - 0.5f)) return "a game banner took a queue that is still showing";
            queue.Enqueue("a");
            queue.Enqueue("b");
            queue.Enqueue("c");
            if (queue.TryClaim(BannerQueue.Seconds)) return "a game banner skipped a non-empty queue";
            string text;
            if (queue.Next(BannerQueue.Seconds - 0.5f, out text)) return "a banner showed before the previous one's time was up";
            if (!queue.Next(BannerQueue.Seconds, out text) || text != "a") return "first banner is not the first queued";
            if (!queue.Next(2f * BannerQueue.Seconds, out text) || text != "b") return "second banner is out of order";
            if (!queue.Next(3f * BannerQueue.Seconds, out text) || text != "c") return "third banner is out of order";
            if (queue.Next(1000f, out text) || queue.Count != 0) return "an empty queue returned a banner";
            queue.Enqueue("d");
            queue.Clear();
            if (queue.Next(2000f, out text) || queue.Count != 0) return "a cleared queue still returns a banner";
            BannerQueue skills = new BannerQueue();
            skills.EnqueueSkill("a.one:s", "level 2");
            skills.EnqueueSkill("a.one:s", "level 3");
            if (skills.Count != 1 || !skills.Next(0f, out text) || text != "level 3") return "two banners for one skill are not one entry with the newest text";
            BannerQueue capped = new BannerQueue();
            capped.Enqueue("game first");
            for (int i = 1; i <= BannerQueue.MaxCustom + 1; i++) capped.EnqueueSkill("m.one:s" + i, "skill " + i);
            capped.Enqueue("game last");
            if (capped.Count != BannerQueue.MaxCustom + 2) return "the custom cap dropped a game banner or kept too many custom ones";
            List<string> order = new List<string>();
            float at = 0f;
            while (capped.Next(at, out text))
            {
                order.Add(text);
                at += BannerQueue.Seconds;
            }
            if (order.Count != BannerQueue.MaxCustom + 2 || order[0] != "game first" || order[1] != "skill 2" || order[order.Count - 1] != "game last" || order.Contains("skill 1")) return "the custom cap did not drop the oldest custom banner";
            BannerQueue queued = new BannerQueue();
            for (int i = 1; i <= BannerQueue.MaxQueued; i++)
            {
                if (!queued.EnqueueCustom("queued " + i)) return "a queued banner was refused below its own cap";
            }
            if (queued.EnqueueCustom("queued over")) return "a queued banner was accepted above its own cap";
            queued.EnqueueSkill("m.one:s", "level 2");
            queued.Enqueue("game after");
            if (queued.Count != BannerQueue.MaxQueued + 2) return "a queued banner was added above its cap or dropped a level-up banner";
            List<string> queueOrder = new List<string>();
            float queueAt = 0f;
            while (queued.Next(queueAt, out text))
            {
                queueOrder.Add(text);
                queueAt += BannerQueue.Seconds;
            }
            if (queueOrder.Count != BannerQueue.MaxQueued + 2 || queueOrder[0] != "queued 1" || queueOrder[BannerQueue.MaxQueued] != "level 2" || queueOrder[queueOrder.Count - 1] != "game after") return "the queued banners, the level-up banner and the game banner did not keep their order";
            Dictionary<string, CustomSkillSaveEntry> values = new Dictionary<string, CustomSkillSaveEntry>();
            Load(new List<CustomSkillSaveEntry>
            {
                new CustomSkillSaveEntry { Mod = "a.one", Id = "s", Value = 2f },
                new CustomSkillSaveEntry { Mod = "b.two", Id = "s", Value = 3f },
                new CustomSkillSaveEntry { Mod = "a.one", Id = "t", Value = 1f },
                new CustomSkillSaveEntry { Mod = "bad mod", Id = "s", Value = 1f },
                new CustomSkillSaveEntry { Mod = "b.two", Id = "s", Value = 4f }
            }, values);
            if (values.Count != 3 || !values.ContainsKey("b.two:s")) return "entries of a mod that is not installed were not loaded";
            List<CustomSkillSaveEntry> saved = Save(values);
            HashSet<string> keys = new HashSet<string>();
            foreach (CustomSkillSaveEntry entry in saved)
            {
                if (!keys.Add(Key(entry.Mod, entry.Id))) return "save writes a key twice";
            }
            if (saved.Count != 3) return "save does not write every entry";
            return "custom skills self-check ok";
        }
    }
}
