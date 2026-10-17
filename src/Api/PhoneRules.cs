using System;
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

    public enum PhoneRefusal
    {
        None,
        NotReady,
        SaveBusy,
        BadInput,
        BadNumber,
        BadNetwork,
        BadAnswerChance,
        Duplicate,
        UnknownConversation,
        Failed
    }

    internal static class PhoneRules
    {
        internal const int MaxDigits = 10;
        private const PhoneNetwork Networks = PhoneNetwork.Obenseuer | PhoneNetwork.Outside | PhoneNetwork.Wasteland;

        internal static bool TryParse(string number, out int[] digits)
        {
            digits = null;
            if (string.IsNullOrEmpty(number) || number.Length > MaxDigits) return false;
            int[] parsed = new int[number.Length];
            for (int i = 0; i < number.Length; i++)
            {
                char c = number[i];
                if (c == '*') parsed[i] = 10;
                else if (c == '#') parsed[i] = 11;
                else if (c >= '0' && c <= '9') parsed[i] = c - '0';
                else return false;
            }
            digits = parsed;
            return true;
        }

        internal static string Digits(int[] number)
        {
            if (number == null) return "";
            StringBuilder builder = new StringBuilder();
            foreach (int digit in number)
            {
                builder.Append(digit == 10 ? "*" : digit == 11 ? "#" : digit.ToString());
            }
            return builder.ToString();
        }

        internal static PhoneRefusal Check(string modId, int[] digits, PhoneNetwork network, float answerChance)
        {
            if (!ModIds.IsValid(modId)) return PhoneRefusal.BadInput;
            if (digits == null || digits.Length == 0 || digits.Length > MaxDigits) return PhoneRefusal.BadNumber;
            if (network == PhoneNetwork.None || (network & ~Networks) != PhoneNetwork.None) return PhoneRefusal.BadNetwork;
            if (float.IsNaN(answerChance) || answerChance < 0f || answerChance > 1f) return PhoneRefusal.BadAnswerChance;
            return PhoneRefusal.None;
        }
    }
}
