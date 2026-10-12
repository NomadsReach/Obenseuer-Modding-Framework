using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class LanternInfo
    {
        public string Id { get; private set; }

        public Vector3 Position { get; private set; }

        public bool Lit { get; private set; }

        public int RemainingSeconds { get; private set; }

        internal LanternInfo(string id, Vector3 position, bool lit, int remainingSeconds)
        {
            Id = id;
            Position = position;
            Lit = lit;
            RemainingSeconds = remainingSeconds;
        }
    }

    public sealed class CandleBurntOutArgs
    {
        public string Id { get; private set; }

        public Vector3 Position { get; private set; }

        internal CandleBurntOutArgs(string id, Vector3 position)
        {
            Id = id;
            Position = position;
        }
    }

    public static class Lanterns
    {
        internal const string Owner = "obenseuer.moddingframework.lanterns";
        private static bool installed;

        public static event Action<CandleBurntOutArgs> CandleBurntOut;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<LanternInfo> All()
        {
            List<LanternInfo> list = new List<LanternInfo>();
            try
            {
                foreach (Lantern lantern in UnityEngine.Object.FindObjectsByType<Lantern>(FindObjectsSortMode.None))
                {
                    if (lantern != null) list.Add(ToInfo(lantern));
                }
                return list;
            }
            catch (Exception e)
            {
                ApiLog.Once(Lanterns.Owner, "All", e);
                return new List<LanternInfo>();
            }
        }

        public static LanternInfo Get(string id)
        {
            try
            {
                Lantern lantern = SceneIds.Find<Lantern>(id);
                return lantern == null ? null : ToInfo(lantern);
            }
            catch (Exception e)
            {
                ApiLog.Once(Lanterns.Owner, "Get", e);
                return null;
            }
        }

        internal static void Init()
        {
            installed = LanternsPatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("lantern hook incomplete, candle burnt-out events are disabled");
        }

        internal static LanternInfo ToInfo(Lantern lantern)
        {
            bool lit = lantern.candleTimer != null && lantern.candleTimer.remainingInSeconds > 0;
            return new LanternInfo(SceneIds.Of(lantern), lantern.transform.position, lit, lit ? lantern.candleTimer.remainingInSeconds : 0);
        }

        internal static void RaiseCandleBurntOut(Lantern lantern)
        {
            CandleBurntOutArgs args = new CandleBurntOutArgs(SceneIds.Of(lantern), lantern.transform.position);
            Events.Raise(CandleBurntOut, "Lanterns.CandleBurntOut", h => h(args));
        }
    }
}
