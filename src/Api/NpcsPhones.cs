using System;
using System.Collections.Generic;
using System.Text;

namespace ObenseuerCore
{
    [Flags]
    public enum PhoneNetwork
    {
        None = 0,
        Obenseuer = 1,
        Outside = 2,
        Wasteland = 4
    }

    public sealed class PhoneContact
    {
        public string Id { get; private set; }
        public string Number { get; private set; }
        public PhoneNetwork Network { get; private set; }
        public float AnswerChance { get; private set; }

        internal PhoneContact(string id, string number, PhoneNetwork network, float answerChance)
        {
            Id = id;
            Number = number;
            Network = network;
            AnswerChance = answerChance;
        }
    }

    public static class Phones
    {
        public static List<PhoneContact> All()
        {
            try
            {
                List<PhoneContact> result = new List<PhoneContact>();
                foreach (PhoneNumber entry in PhoneNumberDatabase.database)
                {
                    if (entry == null || entry.notIndexed || entry.number == null || entry.number.Length == 0) continue;
                    result.Add(new PhoneContact(entry.name, Digits(entry.number), (PhoneNetwork)(int)entry.network, entry.answerChance));
                }
                return result;
            }
            catch (Exception e)
            {
                ApiLog.Once(Npcs.Owner, "Phones.All", e);
                return new List<PhoneContact>();
            }
        }

        internal static string Digits(int[] digits)
        {
            if (digits == null) return "";
            StringBuilder builder = new StringBuilder();
            foreach (int digit in digits)
            {
                builder.Append(digit == 10 ? "*" : digit == 11 ? "#" : digit.ToString());
            }
            return builder.ToString();
        }
    }
}
