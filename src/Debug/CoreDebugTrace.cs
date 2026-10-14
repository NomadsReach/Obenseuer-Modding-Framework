using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using HarmonyLib;

namespace ObenseuerCore
{
    internal sealed class CoreDebugTarget
    {
        internal readonly string Name;
        internal readonly ParameterInfo[] Parameters;
        internal readonly bool IsVoid;
        internal readonly bool Api;
        internal readonly CoreDebugLimiter Limiter;

        internal CoreDebugTarget(MethodBase method, float now)
        {
            MethodInfo info = method as MethodInfo;
            if (method.ContainsGenericParameters) throw new NotSupportedException("generic method");
            if (info != null && info.ReturnType.IsByRef) throw new NotSupportedException("by-ref return");
            Name = CoreDebugFormat.Method(method);
            Parameters = method.GetParameters();
            IsVoid = info == null || info.ReturnType == typeof(void);
            Api = method.IsPublic && CoreDebugTrace.PublicType(method.DeclaringType);
            Limiter = new CoreDebugLimiter(now);
        }
    }

    internal sealed class CoreDebugCall
    {
        internal CoreDebugTarget Target;
        internal object[] Args;
        internal object Result;
        internal int Depth;
        internal int Thread;
        internal long Start;
        internal string From;
    }

    internal static class CoreDebugTrace
    {
        internal const string OwnerId = "obenseuer.moddingframework.debugtrace";
        private const int TopCount = 10;
        private const int NamesShown = 30;
        private const float SweepSeconds = 10f;
        private static readonly HarmonyMethod PrefixMethod = new HarmonyMethod(Handler("Prefix")) { priority = int.MaxValue };
        private static readonly HarmonyMethod ResultMethod = new HarmonyMethod(Handler("PostfixResult"));
        private static readonly HarmonyMethod FinalizerMethod = new HarmonyMethod(Handler("Finalizer"));
        private static readonly object sync = new object();
        private static readonly List<string> refused = new List<string>();
        private static Dictionary<MethodBase, CoreDebugTarget> active;
        private static float nextSweep;
        private static long patchMs;
        private static bool warned;
        [ThreadStatic] private static int depth;
        [ThreadStatic] private static bool busy;

        internal static Action<string> Sink = delegate { };
        internal static Func<float> Clock = () => 0f;
        internal static Func<bool> IsMain = () => true;

        internal static int PatchedCount
        {
            get { return active == null ? 0 : active.Count; }
        }

        internal static bool PublicType(Type type)
        {
            return type.IsPublic || (type.IsNestedPublic && PublicType(type.DeclaringType));
        }

        internal static bool Matches(string typeName, string filter)
        {
            bool any = false;
            if (!string.IsNullOrEmpty(filter))
            {
                foreach (string part in filter.Split(','))
                {
                    string wanted = part.Trim();
                    if (wanted.Length == 0) continue;
                    any = true;
                    if (typeName.StartsWith(wanted, StringComparison.OrdinalIgnoreCase)) return true;
                }
            }
            return !any;
        }

        internal static void Start(IList<MethodBase> methods)
        {
            Stop();
            Stopwatch watch = Stopwatch.StartNew();
            float now = Clock();
            Dictionary<MethodBase, CoreDebugTarget> map = new Dictionary<MethodBase, CoreDebugTarget>();
            refused.Clear();
            Patch(new Harmony(OwnerId), map, methods, now);
            patchMs = watch.ElapsedMilliseconds;
            nextSweep = now + SweepSeconds;
            active = map;
            Say("debug trace on: patched " + map.Count + " methods in " + patchMs + " ms, " + refused.Count + " refused" + Names());
        }

        internal static void Add(IList<MethodBase> methods)
        {
            Dictionary<MethodBase, CoreDebugTarget> map = active;
            if (map == null || methods.Count == 0) return;
            Dictionary<MethodBase, CoreDebugTarget> next = new Dictionary<MethodBase, CoreDebugTarget>(map);
            Patch(new Harmony(OwnerId), next, methods, Clock());
            lock (sync)
            {
                active = next;
            }
        }

        internal static void Stop()
        {
            Dictionary<MethodBase, CoreDebugTarget> map = active;
            if (map == null) return;
            List<Pending> due;
            List<string> top;
            lock (sync)
            {
                due = TakeDue(map, Clock(), true);
                top = TopLines(map);
                active = null;
            }
            Quietly(() =>
            {
                foreach (Pending pending in due) Sink(Line(pending));
                Sink("debug trace off: patched " + map.Count + " methods in " + patchMs + " ms, " + refused.Count + " refused" + Names());
                foreach (string line in top) Sink(line);
            });
            Harmony harmony = new Harmony(OwnerId);
            foreach (MethodBase method in map.Keys) Unpatch(harmony, method);
        }

        private static void Patch(Harmony harmony, Dictionary<MethodBase, CoreDebugTarget> map, IEnumerable<MethodBase> methods, float now)
        {
            foreach (MethodBase method in methods)
            {
                if (map.ContainsKey(method)) continue;
                try
                {
                    CoreDebugTarget target = new CoreDebugTarget(method, now);
                    try
                    {
                        harmony.Patch(method, PrefixMethod, target.IsVoid ? null : ResultMethod, null, FinalizerMethod, null);
                    }
                    catch (Exception)
                    {
                        Unpatch(harmony, method);
                        throw;
                    }
                    map[method] = target;
                }
                catch (Exception)
                {
                    refused.Add(CoreDebugFormat.Method(method));
                }
            }
        }

        private static void Unpatch(Harmony harmony, MethodBase method)
        {
            try
            {
                harmony.Unpatch(method, HarmonyPatchType.All, OwnerId);
            }
            catch (Exception e)
            {
                Warn(e);
            }
        }

        internal static List<string> Top()
        {
            Dictionary<MethodBase, CoreDebugTarget> map = active;
            lock (sync)
            {
                return map == null ? new List<string>() : TopLines(map);
            }
        }

        private static List<string> TopLines(Dictionary<MethodBase, CoreDebugTarget> map)
        {
            List<CoreDebugTarget> ranked = new List<CoreDebugTarget>();
            foreach (CoreDebugTarget target in map.Values)
            {
                if (target.Limiter.Total > 0) ranked.Add(target);
            }
            ranked.Sort(ByTotal);
            List<string> lines = new List<string>();
            for (int i = 0; i < ranked.Count && i < TopCount; i++)
            {
                lines.Add("  " + (i + 1) + ". " + ranked[i].Name + " " + ranked[i].Limiter.Total + " calls");
            }
            return lines;
        }

        private static MethodInfo Handler(string name)
        {
            return typeof(CoreDebugTrace).GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

        private static int ByTotal(CoreDebugTarget a, CoreDebugTarget b)
        {
            return b.Limiter.Total.CompareTo(a.Limiter.Total);
        }

        private static void Prefix(MethodBase __originalMethod, object[] __args, out CoreDebugCall __state)
        {
            __state = null;
            if (busy) return;
            try
            {
                Dictionary<MethodBase, CoreDebugTarget> map = active;
                CoreDebugTarget target;
                if (map == null || !map.TryGetValue(__originalMethod, out target)) return;
                CoreDebugCall call = new CoreDebugCall();
                call.Target = target;
                call.Args = __args;
                call.Depth = depth;
                call.Thread = IsMain() ? 0 : Thread.CurrentThread.ManagedThreadId;
                if (depth == 0 && target.Api && target.Limiter.HasFullLines) call.From = CallerAssembly();
                call.Start = Stopwatch.GetTimestamp();
                depth++;
                __state = call;
            }
            catch (Exception e)
            {
                Warn(e);
            }
        }

        private static void PostfixResult(object __result, ref CoreDebugCall __state)
        {
            if (__state != null) __state.Result = __result;
        }

        private static void Finalizer(Exception __exception, ref CoreDebugCall __state)
        {
            if (__state == null) return;
            depth = __state.Depth;
            Finish(__state, __exception);
        }

        private static void Finish(CoreDebugCall call, Exception error)
        {
            try
            {
                double ms = (Stopwatch.GetTimestamp() - call.Start) * 1000.0 / Stopwatch.Frequency;
                float now = Clock();
                bool full;
                List<Pending> due = null;
                lock (sync)
                {
                    full = call.Target.Limiter.Admit(call);
                    if (active != null && now >= nextSweep) due = TakeDue(active, now, false);
                }
                if (!full && (due == null || due.Count == 0)) return;
                Quietly(() =>
                {
                    if (full) Sink(error == null ? CallLine(call, ms) : CoreDebugFormat.ErrorLine(call.Depth, call.Thread, call.Target.Name, error));
                    if (due != null) foreach (Pending pending in due) Sink(Line(pending));
                });
            }
            catch (Exception e)
            {
                Warn(e);
            }
        }

        private static List<Pending> TakeDue(Dictionary<MethodBase, CoreDebugTarget> map, float now, bool all)
        {
            List<Pending> due = new List<Pending>();
            foreach (CoreDebugTarget target in map.Values)
            {
                if (!all && !target.Limiter.Due(now)) continue;
                long count = target.Limiter.Take(now);
                if (count > 0) due.Add(new Pending(target, count, (CoreDebugCall)target.Limiter.Last));
            }
            nextSweep = now + SweepSeconds;
            return due;
        }

        private static string CallLine(CoreDebugCall call, double ms)
        {
            return CoreDebugFormat.CallLine(call.Depth, call.Thread, call.Target.Name, CoreDebugFormat.Args(call.Target.Parameters, call.Args), CoreDebugFormat.Result(call.Target.IsVoid, call.Result), ms, call.From);
        }

        private static string Line(Pending pending)
        {
            return CoreDebugFormat.MoreLine(pending.Target.Name, pending.Count, CoreDebugFormat.Args(pending.Target.Parameters, pending.Last.Args), CoreDebugFormat.Result(pending.Target.IsVoid, pending.Last.Result));
        }

        private static string Names()
        {
            if (refused.Count == 0) return "";
            int shown = Math.Min(refused.Count, NamesShown);
            string text = ": " + string.Join(", ", refused.GetRange(0, shown).ToArray());
            return refused.Count > shown ? text + " (+" + (refused.Count - shown) + " more)" : text;
        }

        private static string CallerAssembly()
        {
            StackFrame[] frames = new StackTrace(false).GetFrames();
            if (frames == null) return null;
            foreach (StackFrame frame in frames)
            {
                MethodBase method = frame.GetMethod();
                Type type = method == null ? null : method.DeclaringType;
                if (type != null && !Plumbing(type)) return type.Assembly.GetName().Name;
            }
            return null;
        }

        private static bool Plumbing(Type type)
        {
            string space = type.Namespace ?? "";
            return space == "ObenseuerCore" || space.StartsWith("ObenseuerCore.", StringComparison.Ordinal) || space == "System" || space.StartsWith("System.", StringComparison.Ordinal) || space.StartsWith("HarmonyLib", StringComparison.Ordinal) || space.StartsWith("MonoMod", StringComparison.Ordinal);
        }

        private static void Say(string line)
        {
            Quietly(() => Sink(line));
        }

        private static void Warn(Exception e)
        {
            if (warned) return;
            warned = true;
            Say("debug trace failed, some lines may be missing: " + e.GetType().Name + ": " + e.Message);
        }

        private static void Quietly(Action work)
        {
            bool was = busy;
            busy = true;
            try
            {
                work();
            }
            catch (Exception e)
            {
                Warn(e);
            }
            finally
            {
                busy = was;
            }
        }

        private sealed class Pending
        {
            internal readonly CoreDebugTarget Target;
            internal readonly long Count;
            internal readonly CoreDebugCall Last;

            internal Pending(CoreDebugTarget target, long count, CoreDebugCall last)
            {
                Target = target;
                Count = count;
                Last = last;
            }
        }
    }
}
