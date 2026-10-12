using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class NetPump
    {
        private static GameObject host;
        private static bool started;
        private static bool active;

        internal static void Ensure()
        {
            if (host != null) return;
            host = new GameObject("ObenseuerCore.Net");
            UnityEngine.Object.DontDestroyOnLoad(host);
            host.AddComponent<NetDriver>();
        }

        internal static void Activate()
        {
            active = true;
        }

        internal static void Tick()
        {
            if (!active) return;
            try
            {
                if (!Steam.IsReady) return;
                if (!started)
                {
                    started = true;
                    SteamBridge.Start();
                }
            }
            catch (Exception e)
            {
                NetLog.Once("NetPump.Start", e);
                return;
            }
            try
            {
                Net.Drain();
            }
            catch (Exception e)
            {
                NetLog.Once("NetPump.Drain", e);
            }
            Lobby.Tick();
            try
            {
                Presence.Tick();
            }
            catch (Exception e)
            {
                NetLog.Once("NetPump.Presence", e);
            }
        }
    }

    internal sealed class NetDriver : MonoBehaviour
    {
        private void Update()
        {
            NetPump.Tick();
        }
    }

    internal static class NetLog
    {
        private static readonly CoreLog Log = CoreLog.For("obenseuer.moddingframework.net");
        private static readonly HashSet<string> seen = new HashSet<string>();

        internal static void Info(string message)
        {
            Log.Info(message);
        }

        internal static void Debug(string key, string message)
        {
            Log.Debug(key, message);
        }

        internal static void Once(string site, string message)
        {
            if (seen.Add(site)) Log.Error(site + ": " + message);
        }

        internal static void Once(string site, Exception e)
        {
            Once(site, e.ToString());
        }
    }
}
