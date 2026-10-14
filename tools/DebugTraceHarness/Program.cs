using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using HarmonyLib;
using ObenseuerCore;

namespace DebugTraceHarness
{
    internal static class Program
    {
        private static readonly List<string> output = new List<string>();
        private static readonly List<string> failures = new List<string>();
        private static int checks;
        private static float now;
        private static int mainThread;

        private static int Main(string[] args)
        {
            mainThread = Thread.CurrentThread.ManagedThreadId;
            CoreDebugFormat.Describe = value => null;
            CoreDebugTrace.Clock = () => now;
            CoreDebugTrace.IsMain = () => Thread.CurrentThread.ManagedThreadId == mainThread;
            CoreDebugTrace.Sink = output.Add;
            string selfCheck = CoreDebugSelfTest.Run();
            Check("self-check", selfCheck == "debug trace self-check ok", selfCheck);
            Discovery();
            Tracing();
            Reentrancy();
            Limiter();
            Lifecycle();
            Refusal();
            Precedence();
            Console.WriteLine("harness: " + checks + " checks, " + failures.Count + " failed");
            return failures.Count == 0 ? 0 : 1;
        }

        private static void Discovery()
        {
            List<string> all = Names(CoreDebugTargets.Find(typeof(Program).Assembly, "", new MethodInfo[0]));
            string listed = string.Join(", ", all.ToArray());
            Check("discovery includes public members", all.Contains("Sample.Add") && all.Contains("Sample.Outer") && all.Contains("SampleStatic.get_Value") && all.Contains("SampleStatic.set_Value") && all.Contains("SampleBox.set_Value") && all.Contains("SampleBox.Get") && all.Contains("SamplePatches.Handle"), listed);
            Check("discovery excludes members", !all.Contains("SampleBox.get_Value") && !all.Contains("SampleBox.ToString") && !all.Contains("SampleInfo.Get") && !all.Contains("Sample.Id") && !all.Contains("Sample.Inner") && !all.Contains("Program.Main"), listed);
            List<string> boxes = Names(CoreDebugTargets.Find(typeof(Program).Assembly, "SampleBox", new MethodInfo[0]));
            Check("discovery filter limits types", boxes.Count > 0 && boxes.TrueForAll(n => n.StartsWith("SampleBox.", StringComparison.Ordinal)), string.Join(", ", boxes.ToArray()));
        }

        private static void Tracing()
        {
            Type sample = typeof(Sample);
            CoreDebugTrace.Start(new List<MethodBase>
            {
                sample.GetMethod("Add"),
                sample.GetMethod("Note"),
                sample.GetMethod("Boom"),
                sample.GetMethod("Fill"),
                sample.GetMethod("Outer"),
                sample.GetMethod("Inner", BindingFlags.Static | BindingFlags.NonPublic),
                sample.GetMethod("Id"),
            });
            Check("start line counts patched and refused", output.Count == 1 && output[0].StartsWith("debug trace on: patched 6 methods", StringComparison.Ordinal) && output[0].EndsWith("1 refused: Sample.Id", StringComparison.Ordinal), string.Join(" | ", output.ToArray()));
            output.Clear();
            Sample.Outer();
            List<string> nested = new List<string>(output);
            Check("nested calls keep depth and caller", nested.Count == 3 && Shape(nested[0]) == "    Sample.Add(1, 2) -> 3 (T ms)" && Shape(nested[1]) == "  Sample.Inner() -> void (T ms)" && Shape(nested[2]) == "Sample.Outer() -> void (T ms) [from DebugTraceHarness]", string.Join(" | ", nested.ToArray()));
            output.Clear();
            int value = 4;
            int doubled;
            Sample.Fill(out doubled, ref value);
            Check("out and ref arguments", output.Count == 1 && Shape(output[0]) == "Sample.Fill(out, ref 4) -> void (T ms) [from DebugTraceHarness]" && doubled == 8, string.Join(" | ", output.ToArray()));
            output.Clear();
            bool caught = false;
            try
            {
                Sample.Boom();
            }
            catch (InvalidOperationException)
            {
                caught = true;
            }
            Check("exception still propagates", caught, "not caught");
            Check("exception line", output.Count == 1 && output[0].StartsWith("!! Sample.Boom threw InvalidOperationException: boom at ", StringComparison.Ordinal), string.Join(" | ", output.ToArray()));
            CoreDebugTrace.Stop();
        }

        private static void Reentrancy()
        {
            CoreDebugTrace.Start(new List<MethodBase> { typeof(Sample).GetMethod("Note"), typeof(Sample).GetMethod("Add") });
            output.Clear();
            bool reentered = false;
            CoreDebugTrace.Sink = line =>
            {
                output.Add(line);
                if (!reentered && line.StartsWith("Sample.Note", StringComparison.Ordinal))
                {
                    reentered = true;
                    Sample.Add(7, 7);
                }
            };
            Sample.Note("hi");
            CoreDebugTrace.Sink = output.Add;
            Check("calls made from the sink are not traced", reentered && output.Count == 1 && Shape(output[0]) == "Sample.Note(\"hi\") -> void (T ms) [from DebugTraceHarness]", string.Join(" | ", output.ToArray()));
            CoreDebugTrace.Stop();
        }

        private static void Limiter()
        {
            now = 0f;
            CoreDebugTrace.Start(new List<MethodBase> { typeof(Sample).GetMethod("Add") });
            output.Clear();
            for (int i = 0; i < 25; i++) Sample.Add(i, 1);
            now = 11f;
            Sample.Add(1, 1);
            Check("first 20 calls in full then one summary", output.Count == 21 && output[20] == "Sample.Add: 6 more calls in the last 10 s (last: 1, 1 -> 2)", output.Count + " lines: " + string.Join(" | ", output.Skip(19).ToArray()));
            List<string> top = CoreDebugTrace.Top();
            Check("top lists calls per method", top.Count == 1 && top[0] == "  1. Sample.Add 26 calls", string.Join(" | ", top.ToArray()));
            CoreDebugTrace.Stop();
            now = 0f;
        }

        private static void Lifecycle()
        {
            MethodInfo add = typeof(Sample).GetMethod("Add");
            new Harmony("harness.foreign").Patch(add, new HarmonyMethod(typeof(Sample).GetMethod("Foreign")), null, null, null, null);
            CoreDebugTrace.Start(new List<MethodBase> { add });
            output.Clear();
            Thread worker = new Thread(() => Sample.Add(3, 4));
            worker.Start();
            worker.Join();
            Check("calls off the main thread are tagged", output.Count == 1 && Shape(output[0]).StartsWith("[thread ", StringComparison.Ordinal) && Shape(output[0]).Contains("Sample.Add(3, 4) -> 7 (T ms)"), string.Join(" | ", output.ToArray()));
            CoreDebugTrace.Stop();
            output.Clear();
            Sample.Add(9, 9);
            Check("stop removes the trace", output.Count == 0, string.Join(" | ", output.ToArray()));
            IEnumerable<string> owners = Harmony.GetPatchInfo(add).Owners;
            Check("stop unpatches only its own owner", owners.Contains("harness.foreign") && !owners.Contains(CoreDebugTrace.OwnerId), string.Join(",", owners.ToArray()));
        }

        private static void Refusal()
        {
            MethodInfo typed = typeof(Sample).GetMethod("Typed");
            MethodInfo add = typeof(Sample).GetMethod("Add");
            CoreDebugTrace.Start(new List<MethodBase> { typed, add });
            bool leftover = Owned(typed);
            CoreDebugTrace.Stop();
            Check("refused method leaves no tracer patch", !leftover && !Owned(typed) && !Owned(add), "during trace typed owned " + leftover + ", after stop typed owned " + Owned(typed) + ", add owned " + Owned(add));
        }

        private static void Precedence()
        {
            MethodInfo use = typeof(Sample).GetMethod("Use");
            new Harmony("harness.precedence").Patch(use, new HarmonyMethod(typeof(Sample).GetMethod("Forty")) { priority = Priority.First }, null, null, null, null);
            CoreDebugTrace.Start(new List<MethodBase> { use });
            int value = 1;
            Sample.Use(ref value);
            CoreDebugTrace.Stop();
            new Harmony("harness.precedence").UnpatchSelf();
            Check("tracer keeps a ref change made by another prefix", value == 41, "value " + value);
        }

        private static bool Owned(MethodBase method)
        {
            Patches patches = Harmony.GetPatchInfo(method);
            return patches != null && patches.Owners.Contains(CoreDebugTrace.OwnerId);
        }

        private static List<string> Names(List<MethodBase> methods)
        {
            List<string> names = new List<string>();
            foreach (MethodBase method in methods) names.Add(CoreDebugFormat.Method(method));
            return names;
        }

        private static string Shape(string line)
        {
            return Regex.Replace(line, @"\(\d+\.\d{3} ms\)", "(T ms)");
        }

        private static void Check(string name, bool ok, string detail)
        {
            checks++;
            if (!ok) failures.Add(name);
            Console.WriteLine((ok ? "PASS " : "FAIL ") + name + (ok ? "" : ": " + detail));
        }
    }
}
