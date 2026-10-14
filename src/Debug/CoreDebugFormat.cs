using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace ObenseuerCore
{
    internal static class CoreDebugFormat
    {
        internal const int MaxText = 120;
        internal static Func<object, string> Describe;

        internal static string Value(object value)
        {
            if (value == null) return "null";
            string text = value as string;
            if (text != null) return "\"" + Cut(text) + "\"";
            string described = Describe == null ? null : Describe(value);
            if (described != null) return described;
            Delegate handlers = value as Delegate;
            if (handlers != null) return Name(handlers.GetType()) + "[" + handlers.GetInvocationList().Length + "]";
            Array array = value as Array;
            if (array != null) return Name(array.GetType().GetElementType()) + "[" + array.Length + "]";
            ICollection collection = value as ICollection;
            if (collection != null) return Name(value.GetType()) + "[" + collection.Count + "]";
            try
            {
                IFormattable number = value as IFormattable;
                return Cut(number != null ? number.ToString(null, CultureInfo.InvariantCulture) : (value.ToString() ?? "null"));
            }
            catch (Exception e)
            {
                return "<ToString threw " + e.GetType().Name + ">";
            }
        }

        internal static string Name(Type type)
        {
            string name = type.Name;
            int tick = name.IndexOf('`');
            return tick < 0 ? name : name.Substring(0, tick);
        }

        internal static string Method(MethodBase method)
        {
            return method.DeclaringType == null ? method.Name : Name(method.DeclaringType) + "." + method.Name;
        }

        internal static string Args(ParameterInfo[] parameters, object[] values)
        {
            StringBuilder text = new StringBuilder();
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i > 0) text.Append(", ");
                object value = values != null && i < values.Length ? values[i] : null;
                ParameterInfo parameter = parameters[i];
                if (!parameter.ParameterType.IsByRef) text.Append(Value(value));
                else if (parameter.IsOut) text.Append("out");
                else text.Append("ref ").Append(Value(value));
            }
            return text.ToString();
        }

        internal static string Result(bool isVoid, object value)
        {
            return isVoid ? "void" : Value(value);
        }

        internal static string Cut(string text)
        {
            return text.Length > MaxText ? text.Substring(0, MaxText) + "..." : text;
        }

        internal static string CallLine(int depth, int thread, string method, string args, string result, double ms, string from)
        {
            return Indent(depth) + Tag(thread) + method + "(" + args + ") -> " + result + " (" + Ms(ms) + " ms)" + (from == null ? "" : " [from " + from + "]");
        }

        internal static string ErrorLine(int depth, int thread, string method, Exception error)
        {
            string frame = FirstFrame(error);
            return Indent(depth) + Tag(thread) + "!! " + method + " threw " + error.GetType().Name + ": " + Cut(error.Message ?? "") + (frame == null ? "" : " at " + frame);
        }

        internal static string MoreLine(string method, long count, string args, string result)
        {
            return method + ": " + count + " more calls in the last " + (int)CoreDebugLimiter.WindowSeconds + " s (last: " + (args.Length == 0 ? "" : args + " ") + "-> " + result + ")";
        }

        private static string Indent(int depth)
        {
            return new string(' ', depth * 2);
        }

        private static string Tag(int thread)
        {
            return thread == 0 ? "" : "[thread " + thread + "] ";
        }

        private static string Ms(double ms)
        {
            return ms.ToString("0.000", CultureInfo.InvariantCulture);
        }

        private static string FirstFrame(Exception error)
        {
            string trace = error.StackTrace;
            if (string.IsNullOrEmpty(trace)) return null;
            string line = trace.Split('\n')[0].Trim();
            return line.StartsWith("at ", StringComparison.Ordinal) ? line.Substring(3) : line;
        }
    }
}
