namespace ObenseuerCore
{
    internal static class BankExchangeMath
    {
        internal static int Credit(float amount, float wallet, float supply, float ratio)
        {
            float num = amount;
            if (num > wallet) num = wallet;
            if (num > supply) num = supply;
            return (int)(num * ratio);
        }

        internal static bool WithinLimits(float amount, float wallet, float supply)
        {
            return amount <= wallet && amount <= supply;
        }

        internal static string SelfCheck()
        {
            if (Credit(1f, 1f, 100f, 0.5f) != 0) return "half a unit credits nothing";
            if (Credit(10f, 5f, 100f, 0.5f) != 2) return "wallet clamp not applied";
            if (Credit(10f, 100f, 3f, 1f) != 3) return "supply clamp not applied";
            if (Credit(7f, 100f, 100f, 0.3f) != 2) return "credit not truncated";
            if (WithinLimits(10f, 5f, 100f) || WithinLimits(10f, 100f, 9f) || !WithinLimits(5f, 5f, 5f)) return "limit check wrong";
            return "bank exchange math self-check ok";
        }
    }
}
