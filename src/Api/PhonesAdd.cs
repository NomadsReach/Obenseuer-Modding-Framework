using System;

namespace ObenseuerCore
{
    public static partial class Phones
    {
        public static PhoneContact Add(string modId, string number, PhoneNetwork network, float answerChance, string conversationTitle, out PhoneRefusal refusal)
        {
            refusal = PhoneRefusal.None;
            try
            {
                if (!ModIds.IsValid(modId))
                {
                    refusal = PhoneRefusal.BadInput;
                    return null;
                }
                int[] digits;
                if (!PhoneRules.TryParse(number, out digits))
                {
                    refusal = PhoneRefusal.BadNumber;
                    Refuse(modId, number, "the number must be 1 to " + PhoneRules.MaxDigits + " characters of 0 to 9, star or hash");
                    return null;
                }
                refusal = PhoneRules.Check(modId, digits, network, answerChance);
                if (refusal != PhoneRefusal.None)
                {
                    Refuse(modId, number, "the number was refused as " + refusal);
                    return null;
                }
                if (SaveGate.Busy)
                {
                    refusal = PhoneRefusal.SaveBusy;
                    return null;
                }
                if (!PhoneBook.KnownConversation(conversationTitle))
                {
                    refusal = PhoneRefusal.UnknownConversation;
                    Refuse(modId, number, "no conversation named " + conversationTitle + " is loaded, so the call would have nothing to say");
                    return null;
                }
                PhoneEntry existing = PhoneBook.Find(modId, number);
                if (existing != null)
                {
                    existing.Digits = digits;
                    existing.Network = network;
                    existing.AnswerChance = answerChance;
                    existing.Conversation = conversationTitle;
                    if (!PhoneBook.Refresh(existing)) refusal = PhoneRefusal.NotReady;
                    return Contact(existing);
                }
                PhoneBook.GameBookFilled();
                if (PhoneBook.Taken(digits, modId, number))
                {
                    refusal = PhoneRefusal.Duplicate;
                    Refuse(modId, number, "that number is already in the phone book");
                    return null;
                }
                PhoneEntry entry = new PhoneEntry();
                entry.ModId = modId;
                entry.Number = number;
                entry.Digits = digits;
                entry.Network = network;
                entry.AnswerChance = answerChance;
                entry.Conversation = conversationTitle;
                PhoneBook.Entries.Add(entry);
                if (!PhoneBook.Attach(entry))
                {
                    if (!PhoneBook.Entries.Contains(entry))
                    {
                        refusal = PhoneRefusal.Duplicate;
                        return null;
                    }
                    refusal = PhoneRefusal.NotReady;
                }
                return Contact(entry);
            }
            catch (Exception e)
            {
                refusal = PhoneRefusal.Failed;
                ApiLog.Once(Npcs.Owner, "Phones.Add", e);
                return null;
            }
        }

        public static bool Remove(string modId, string number)
        {
            try
            {
                if (!ModIds.IsValid(modId) || string.IsNullOrEmpty(number)) return false;
                PhoneEntry entry = PhoneBook.Find(modId, number);
                if (entry == null) return false;
                PhoneBook.Detach(entry);
                PhoneBook.Entries.Remove(entry);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Npcs.Owner, "Phones.Remove", e);
                return false;
            }
        }

        public static int Clear(string modId)
        {
            try
            {
                if (!ModIds.IsValid(modId)) return 0;
                int removed = 0;
                for (int i = PhoneBook.Entries.Count - 1; i >= 0; i--)
                {
                    PhoneEntry entry = PhoneBook.Entries[i];
                    if (entry.ModId != modId) continue;
                    PhoneBook.Detach(entry);
                    PhoneBook.Entries.RemoveAt(i);
                    removed++;
                }
                return removed;
            }
            catch (Exception e)
            {
                ApiLog.Once(Npcs.Owner, "Phones.Clear", e);
                return 0;
            }
        }

        internal static PhoneContact Contact(PhoneEntry entry)
        {
            return new PhoneContact(entry.ModId + " " + entry.Number, entry.Number, entry.Network, entry.AnswerChance);
        }

        private static void Refuse(string modId, string number, string why)
        {
            ApiLog.Once(Npcs.Owner, "Phones.Add " + modId + " " + number, why);
        }
    }
}
