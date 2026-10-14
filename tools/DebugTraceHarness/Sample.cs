using System;
using System.Runtime.CompilerServices;

namespace DebugTraceHarness
{
    public static class Sample
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int Add(int a, int b)
        {
            return a + b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Note(string text)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Boom()
        {
            throw new InvalidOperationException("boom");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Fill(out int c, ref int b)
        {
            c = b * 2;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Outer()
        {
            Inner();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Inner()
        {
            Add(1, 2);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static T Id<T>(T value)
        {
            return value;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Foreign()
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Typed(TypedReference r)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Use(ref int v)
        {
        }

        public static void Forty(ref int v)
        {
            v = 41;
        }
    }

    public static class SampleStatic
    {
        public static int Value { get; set; }
    }

    public class SampleBox
    {
        public int Value { get; set; }

        public int Get()
        {
            return Value;
        }

        public override string ToString()
        {
            return "box";
        }
    }

    public class SampleInfo
    {
        public int Get()
        {
            return 0;
        }
    }

    internal static class SamplePatches
    {
        internal static void Handle()
        {
        }
    }
}
