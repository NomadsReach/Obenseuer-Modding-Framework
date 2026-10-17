using System;
using System.Collections.Generic;
using UnityEngine;
using PCDS = PixelCrushers.DialogueSystem;

namespace ObenseuerCore
{
    internal sealed class PhoneEntry
    {
        internal string ModId;
        internal string Number;
        internal int[] Digits;
        internal PhoneNetwork Network;
        internal float AnswerChance;
        internal string Conversation;
        internal PhoneNumber Asset;
    }

    internal static class PhoneBook
    {
        private const string ResourcePath = "Dialogue/Phone";
        private static readonly List<PhoneEntry> entries = new List<PhoneEntry>();
        private static bool pending;

        internal static List<PhoneEntry> Entries { get { return entries; } }

        internal static bool GameBookFilled()
        {
            if (PhoneNumberDatabase.database == null) return false;
            if (PhoneNumberDatabase.database.Count > 0) return true;
            PhoneNumber[] loaded = Resources.LoadAll<PhoneNumber>(ResourcePath);
            if (loaded == null || loaded.Length == 0) return false;
            PhoneNumberDatabase.database.AddRange(loaded);
            return PhoneNumberDatabase.database.Count > 0;
        }

        internal static bool Taken(int[] digits, string exceptModId, string exceptNumber)
        {
            foreach (PhoneNumber number in PhoneNumberDatabase.database)
            {
                if (number == null) continue;
                if (number.number == null || number.number.Length == 0) continue;
                if (Same(number.number, digits)) return true;
            }
            foreach (PhoneEntry entry in entries)
            {
                if (exceptModId != null && entry.ModId == exceptModId && entry.Number == exceptNumber) continue;
                if (Same(entry.Digits, digits)) return true;
            }
            return false;
        }

        internal static bool Same(int[] left, int[] right)
        {
            if (left == null || right == null) return false;
            if (left.Length != right.Length) return false;
            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i]) return false;
            }
            return true;
        }

        internal static PhoneEntry Find(string modId, string number)
        {
            foreach (PhoneEntry entry in entries)
            {
                if (entry.ModId == modId && entry.Number == number) return entry;
            }
            return null;
        }

        internal static bool Attach(PhoneEntry entry)
        {
            if (!GameBookFilled())
            {
                pending = true;
                return false;
            }
            if (Taken(entry.Digits, entry.ModId, entry.Number))
            {
                ApiLog.Once(Npcs.Owner, "Phones.Add taken " + entry.ModId + " " + entry.Number, "the phone book already holds that number, so the mod number is not added");
                entries.Remove(entry);
                return false;
            }
            if (entry.Asset == null)
            {
                PhoneNumber asset = ScriptableObject.CreateInstance<PhoneNumber>();
                asset.name = entry.ModId + " " + entry.Number;
                asset.number = entry.Digits;
                asset.network = (PhoneNumber.Network)(int)entry.Network;
                asset.notIndexed = false;
                asset.answerChance = entry.AnswerChance;
                asset.conversation = entry.Conversation;
                asset.database = Master();
                entry.Asset = asset;
            }
            if (!PhoneNumberDatabase.database.Contains(entry.Asset)) PhoneNumberDatabase.database.Add(entry.Asset);
            return true;
        }

        internal static bool Refresh(PhoneEntry entry)
        {
            if (entry.Asset == null) return Attach(entry);
            entry.Asset.number = entry.Digits;
            entry.Asset.network = (PhoneNumber.Network)(int)entry.Network;
            entry.Asset.answerChance = entry.AnswerChance;
            entry.Asset.conversation = entry.Conversation;
            entry.Asset.database = Master();
            if (!PhoneNumberDatabase.database.Contains(entry.Asset)) PhoneNumberDatabase.database.Add(entry.Asset);
            return true;
        }

        internal static void Detach(PhoneEntry entry)
        {
            if (entry.Asset == null) return;
            PhoneNumberDatabase.database.Remove(entry.Asset);
            UnityEngine.Object.Destroy(entry.Asset);
            entry.Asset = null;
        }

        internal static void Flush()
        {
            if (!pending) return;
            if (!GameBookFilled()) return;
            pending = false;
            PhoneEntry[] queued = entries.ToArray();
            foreach (PhoneEntry entry in queued)
            {
                if (entry.Asset == null) Attach(entry);
            }
        }

        internal static PCDS.DialogueDatabase Master()
        {
            return Dialogue.IsAvailable && PCDS.DialogueManager.hasInstance ? PCDS.DialogueManager.masterDatabase : null;
        }

        internal static bool KnownConversation(string title)
        {
            PCDS.DialogueDatabase master = Master();
            return master != null && master.GetConversation(title) != null;
        }
    }
}
