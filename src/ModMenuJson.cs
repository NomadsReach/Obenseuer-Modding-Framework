using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ObenseuerCore
{
    internal sealed class ModMenuJson
    {
        private readonly StringBuilder text = new StringBuilder();
        private readonly Stack<char> closers = new Stack<char>();
        private readonly Stack<int> counts = new Stack<int>();
        private bool afterKey;

        internal ModMenuJson Begin(char open)
        {
            Item();
            text.Append(open);
            closers.Push(open == '{' ? '}' : ']');
            counts.Push(0);
            return this;
        }

        internal ModMenuJson End()
        {
            text.Append(closers.Pop());
            counts.Pop();
            return this;
        }

        internal ModMenuJson Key(string name)
        {
            Item();
            Quote(name ?? "");
            text.Append(':');
            afterKey = true;
            return this;
        }

        internal ModMenuJson Str(string value)
        {
            Item();
            if (value == null) text.Append("null");
            else Quote(value);
            return this;
        }

        internal ModMenuJson Num(double value)
        {
            Item();
            text.Append(double.IsNaN(value) || double.IsInfinity(value) ? "null" : value.ToString("R", CultureInfo.InvariantCulture));
            return this;
        }

        internal ModMenuJson Num(float value)
        {
            Item();
            text.Append(float.IsNaN(value) || float.IsInfinity(value) ? "null" : value.ToString("R", CultureInfo.InvariantCulture));
            return this;
        }

        internal ModMenuJson Bool(bool value)
        {
            Item();
            text.Append(value ? "true" : "false");
            return this;
        }

        public override string ToString()
        {
            return text.ToString();
        }

        internal static string SelfCheck()
        {
            string quoted = new ModMenuJson().Str("a\"b\\c\n" + (char)1 + (char)0x2028 + (char)0x2029).ToString();
            if (quoted != "\"a\\\"b\\\\c\\n\\u0001\\u2028\\u2029\"") return "string escaping " + quoted;
            if (new ModMenuJson().Str("\r\t\b\f").ToString() != "\"\\r\\t\\b\\f\"") return "short escapes";
            if (new ModMenuJson().Str("a" + (char)0xD800 + "b").ToString() != "\"a\\" + "ud800b\"") return "lone high surrogate";
            if (new ModMenuJson().Str("" + (char)0xDC00).ToString() != "\"\\" + "udc00\"") return "lone low surrogate";
            string pair = "" + (char)0xD83D + (char)0xDE00;
            if (new ModMenuJson().Str(pair).ToString() != "\"" + pair + "\"") return "valid surrogate pair";
            if (new ModMenuJson().Num(double.PositiveInfinity).ToString() != "null") return "infinity";
            if (new ModMenuJson().Num(0.00001).ToString() != "1E-05") return "exponent " + new ModMenuJson().Num(0.00001);
            string nested = new ModMenuJson().Begin('{').Key("k").Str("v").Key("arr").Begin('[').Num(0.1f).Num(double.NaN).Bool(true).Str(null).End().Key("o").Begin('{').End().End().ToString();
            if (nested != "{\"k\":\"v\",\"arr\":[0.1,null,true,null],\"o\":{}}") return "nesting " + nested;
            return null;
        }

        private void Item()
        {
            if (afterKey)
            {
                afterKey = false;
                return;
            }
            if (counts.Count == 0) return;
            int count = counts.Pop();
            if (count > 0) text.Append(',');
            counts.Push(count + 1);
        }

        private void Quote(string value)
        {
            text.Append('"');
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                switch (c)
                {
                    case '"': text.Append("\\\""); break;
                    case '\\': text.Append("\\\\"); break;
                    case '\n': text.Append("\\n"); break;
                    case '\r': text.Append("\\r"); break;
                    case '\t': text.Append("\\t"); break;
                    case '\b': text.Append("\\b"); break;
                    case '\f': text.Append("\\f"); break;
                    default:
                        if (c < ' ' || c == (char)0x2028 || c == (char)0x2029 || IsLoneSurrogate(value, i)) text.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else text.Append(c);
                        break;
                }
            }
            text.Append('"');
        }

        private static bool IsLoneSurrogate(string value, int i)
        {
            if (char.IsHighSurrogate(value[i])) return i + 1 == value.Length || !char.IsLowSurrogate(value[i + 1]);
            if (char.IsLowSurrogate(value[i])) return i == 0 || !char.IsHighSurrogate(value[i - 1]);
            return false;
        }
    }
}
