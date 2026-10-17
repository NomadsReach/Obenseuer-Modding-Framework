using System;

namespace ObenseuerCore
{
    internal static class PhoneRulesSelfTest
    {
        internal static string Run()
        {
            try
            {
                int[] digits;
                if (PhoneRules.TryParse(null, out digits)) return "a null number parsed";
                if (PhoneRules.TryParse("", out digits)) return "an empty number parsed";
                if (PhoneRules.TryParse("12 34", out digits)) return "a space parsed";
                if (PhoneRules.TryParse("12x34", out digits)) return "a letter parsed";
                if (PhoneRules.TryParse("12.34", out digits)) return "a dot parsed";
                if (PhoneRules.TryParse("12345678901", out digits)) return "an eleven key number parsed";
                if (!PhoneRules.TryParse("12*34#5678", out digits)) return "a star and hash number did not parse";
                if (digits.Length != 10 || digits[0] != 1 || digits[2] != 10 || digits[5] != 11) return "the star and hash digits are wrong";
                if (PhoneRules.Digits(digits) != "12*34#5678") return "the digits text is wrong";
                if (PhoneRules.Digits(null) != "") return "null digits text is not empty";
                if (PhoneRules.Check("example.phonetest", digits, PhoneNetwork.Obenseuer, 1f) != PhoneRefusal.None) return "a valid number was refused";
                if (PhoneRules.Check("example.phonetest", digits, PhoneNetwork.Obenseuer, 0f) != PhoneRefusal.None) return "answer chance 0 was refused";
                if (PhoneRules.Check("example.phonetest", digits, PhoneNetwork.Wasteland | PhoneNetwork.Outside, 0.5f) != PhoneRefusal.None) return "a network pair was refused";
                if (PhoneRules.Check("example.phonetest", null, PhoneNetwork.Obenseuer, 1f) != PhoneRefusal.BadNumber) return "null digits were not refused";
                if (PhoneRules.Check("example.phonetest", new int[0], PhoneNetwork.Obenseuer, 1f) != PhoneRefusal.BadNumber) return "no digits were not refused";
                if (PhoneRules.Check("example.phonetest", new int[11], PhoneNetwork.Obenseuer, 1f) != PhoneRefusal.BadNumber) return "eleven digits were not refused";
                if (PhoneRules.Check("bad id", digits, PhoneNetwork.Obenseuer, 1f) != PhoneRefusal.BadInput) return "a bad mod id was not refused";
                if (PhoneRules.Check(null, digits, PhoneNetwork.Obenseuer, 1f) != PhoneRefusal.BadInput) return "a null mod id was not refused";
                if (PhoneRules.Check("example.phonetest", digits, PhoneNetwork.None, 1f) != PhoneRefusal.BadNetwork) return "network None was not refused";
                if (PhoneRules.Check("example.phonetest", digits, (PhoneNetwork)8, 1f) != PhoneRefusal.BadNetwork) return "an unknown network was not refused";
                if (PhoneRules.Check("example.phonetest", digits, PhoneNetwork.Obenseuer, -0.1f) != PhoneRefusal.BadAnswerChance) return "a negative answer chance was not refused";
                if (PhoneRules.Check("example.phonetest", digits, PhoneNetwork.Obenseuer, 1.1f) != PhoneRefusal.BadAnswerChance) return "an answer chance above 1 was not refused";
                if (PhoneRules.Check("example.phonetest", digits, PhoneNetwork.Obenseuer, float.NaN) != PhoneRefusal.BadAnswerChance) return "a NaN answer chance was not refused";
            }
            catch (Exception e)
            {
                return "exception: " + e.Message;
            }
            return "phone rules self-check ok";
        }
    }
}
