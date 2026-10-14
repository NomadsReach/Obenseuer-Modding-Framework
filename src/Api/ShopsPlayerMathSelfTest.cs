namespace ObenseuerCore
{
    internal static class ShopsPlayerMathSelfTest
    {
        internal static string Run()
        {
            if (ShopsPlayerMath.Factor(0f) != 1.2f) return "default markup not 1.2";
            if (ShopsPlayerMath.Factor(1.5f) != 1.5f) return "set markup changed";
            if (ShopsPlayerMath.Price(100, 1.2f, false, 0) != 120) return "markup price not 120";
            if (ShopsPlayerMath.Price(5, 1.2f, false, 0) != 6) return "5 at 1.2 not 6";
            if (ShopsPlayerMath.Price(3, 1.5f, false, 0) != 4) return "4.5 not rounded to even 4";
            if (ShopsPlayerMath.Price(5, 1.5f, false, 0) != 8) return "7.5 not rounded to even 8";
            if (ShopsPlayerMath.Price(100, 0f, true, 77) != 77) return "override did not win over zero factor";
            if (ShopsPlayerMath.Price(100, 1.2f, true, 50) != 50) return "override did not replace markup";
            if (ShopsPlayerMath.Price(100, 1.2f, true, -5) != 0) return "negative override not clamped";
            if (ShopsPlayerMath.Price(100, -1f, false, 0) != 0) return "negative factor not clamped";
            if (ShopsPlayerMath.Price(int.MaxValue, 2f, false, 0) != int.MaxValue) return "overflow not capped";
            if (ShopsPlayerMath.Price(100, float.NaN, false, 0) != 0) return "NaN factor not 0";
            if (!ShopsPlayerMath.CanTake(3, 0, 5, 5)) return "whole stack refused";
            if (!ShopsPlayerMath.CanTake(3, 2, 5, 1)) return "last slot refused";
            if (ShopsPlayerMath.CanTake(3, 3, 5, 1)) return "slot past the end taken";
            if (ShopsPlayerMath.CanTake(3, -1, 5, 1)) return "negative slot taken";
            if (ShopsPlayerMath.CanTake(3, 0, 5, 0)) return "zero count taken";
            if (ShopsPlayerMath.CanTake(3, 0, 5, 6)) return "more than the stack taken";
            if (ShopsPlayerMath.CanTake(3, 0, 0, 1)) return "empty stack taken";
            if (!ShopsPlayerMath.CanSell(5, 5, 1f)) return "matching sale refused";
            if (ShopsPlayerMath.CanSell(5, 6, 1f)) return "other item sold";
            if (ShopsPlayerMath.CanSell(5, 5, 0f)) return "zero price sold";
            if (ShopsPlayerMath.CanSell(5, 5, -1f)) return "negative price sold";
            if (!ShopsPlayerMath.CanDebit(10f, 10f)) return "exact take refused";
            if (ShopsPlayerMath.CanDebit(9f, 10f)) return "take past the till allowed";
            if (ShopsPlayerMath.CanDebit(10f, 0f)) return "zero take allowed";
            if (ShopsPlayerMath.CanDebit(10f, -1f)) return "negative take allowed";
            if (ShopsPlayerMath.AreaFlag(true, true)) return "converted shop kept its area flag";
            if (ShopsPlayerMath.AreaFlag(true, false)) return "converted shop got an area flag";
            if (!ShopsPlayerMath.AreaFlag(false, true)) return "adopted shop lost its area flag";
            if (ShopsPlayerMath.AreaFlag(false, false)) return "adopted shop got an area flag";
            return "player shop math self-check ok";
        }
    }
}
