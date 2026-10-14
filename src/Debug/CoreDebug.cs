using System;
using System.Reflection;
using BepInEx.Configuration;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class CoreDebug
    {
        internal const string LogId = "ObenseuerCore.debug";
        private const int LinesPerSecond = 1000;
        internal static readonly object writeLock = new object();
        private static ConfigEntry<bool> master;
        private static ConfigEntry<string> traceFilter;
        private static bool configuredVerbose;

        internal static bool Enabled { get; private set; }

        internal static void Init(ConfigFile config)
        {
            master = config.Bind("Debug", "Master", false, "Master switch for framework debugging: traces framework API calls, game hooks, events and saves with arguments, results, timings and errors to BepInEx\\logs\\ObenseuerCore.debug.log. Slows the game; leave off for normal play.");
            traceFilter = config.Bind("Debug", "TraceFilter", "", "Comma-separated type name prefixes to trace, for example PlayerState,GameTime. Empty traces every framework type.");
            CoreDebugFormat.Describe = CoreDebugUnity.Describe;
            CoreDebugTrace.Clock = () => Time.unscaledTime;
            CoreDebugTrace.IsMain = Core.OnMain;
            CoreDebugTrace.Sink = Write;
            CoreDebugEvents.Init();
            master.SettingChanged += delegate { Apply(); };
            traceFilter.SettingChanged += delegate { if (Enabled) Start(); };
            if (!ConsoleCommands.Register(ModIds.CoreId, "core_debug", "Turns the framework debug trace on or off, or shows its status. The setting is saved in the config.", Command))
            {
                CoreLog.For("ObenseuerCore").Info("core_debug not registered, the console is not available");
            }
            if (master.Value) Enable();
        }

        internal static void Write(string line)
        {
            lock (writeLock)
            {
                CoreLog.For(LogId).Info(line);
            }
        }

        internal static void Extend(MethodInfo[] handlers)
        {
            if (!Enabled) return;
            try
            {
                CoreDebugTrace.Add(CoreDebugTargets.Handlers(typeof(CoreDebug).Assembly, traceFilter.Value, handlers));
            }
            catch (Exception e)
            {
                CoreLog.For("ObenseuerCore").Error("debug trace not extended: " + e.ToString());
            }
        }

        private static void Apply()
        {
            if (master.Value) Enable();
            else Disable();
        }

        private static void Enable()
        {
            if (Enabled) return;
            Enabled = true;
            configuredVerbose = Core.Verbose;
            Core.Verbose = true;
            CoreLog log = CoreLog.For(LogId);
            log.SetLineLimit(LinesPerSecond);
            CoreLog.Mirror = log;
            Write("debug on, game " + Core.GameBuild + ", patching " + Core.PatchingAllowed);
            CoreDebugEvents.Begin();
            Start();
        }

        private static void Disable()
        {
            if (!Enabled) return;
            CoreDebugTrace.Stop();
            Write("debug off");
            CoreLog.Mirror = null;
            Enabled = false;
            Core.Verbose = configuredVerbose;
        }

        private static void Start()
        {
            try
            {
                CoreDebugTrace.Start(CoreDebugTargets.Find(typeof(CoreDebug).Assembly, traceFilter.Value, CorePatcher.InstalledHandlers));
            }
            catch (Exception e)
            {
                CoreLog.For("ObenseuerCore").Error("debug trace not started: " + e.ToString());
            }
        }

        private static void Command(string[] args)
        {
            string verb = args == null || args.Length == 0 ? "status" : args[0].ToLowerInvariant();
            if (verb == "on" || verb == "off")
            {
                master.Value = verb == "on";
                Apply();
            }
            else if (verb != "status")
            {
                ConsoleCommands.Print("usage: core_debug on|off|status");
                return;
            }
            ConsoleCommands.Print("debug " + (Enabled ? "on" : "off") + ", " + CoreDebugTrace.PatchedCount + " methods patched");
            foreach (string line in CoreDebugTrace.Top()) ConsoleCommands.Print(line);
        }
    }
}
