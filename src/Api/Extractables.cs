using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    public sealed class ExtractableInfo
    {
        public string Id { get; private set; }

        public string Name { get; private set; }

        public int Health { get; private set; }

        internal ExtractableInfo(string id, string name, int health)
        {
            Id = id;
            Name = name;
            Health = health;
        }
    }

    public sealed class ExtractedArgs
    {
        public string Id { get; private set; }

        internal ExtractedArgs(string id)
        {
            Id = id;
        }
    }

    public static class Extractables
    {
        internal const string Owner = "obenseuer.moddingframework.extractables";
        private static bool installed;

        public static event Action<ExtractedArgs> Extracted;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<ExtractableInfo> All()
        {
            List<ExtractableInfo> list = new List<ExtractableInfo>();
            try
            {
                foreach (Extractable extractable in UnityEngine.Object.FindObjectsByType<Extractable>(UnityEngine.FindObjectsSortMode.None))
                {
                    list.Add(ToInfo(extractable));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Extractables.All", e);
            }
            return list;
        }

        public static ExtractableInfo Get(string id)
        {
            Extractable extractable = Find(id);
            return extractable == null ? null : ToInfo(extractable);
        }

        public static int Health(string id)
        {
            Extractable extractable = Find(id);
            return extractable == null ? 0 : extractable.Health;
        }

        public static bool Damage(string id)
        {
            Extractable extractable = Find(id);
            if (extractable == null || extractable.Health < 1 || SaveGate.Busy) return false;
            try
            {
                int before = extractable.Health;
                extractable.Damage();
                return extractable.Health < before;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Extractables.Damage", e);
                return false;
            }
        }

        internal static void Init()
        {
            installed = ExtractablesPatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("extractable hooks incomplete, Extracted event is disabled");
        }

        internal static bool HasExtractedHandlers
        {
            get
            {
                return Extracted != null;
            }
        }

        internal static void RaiseExtracted(Extractable extractable)
        {
            ExtractedArgs args = new ExtractedArgs(SceneIds.Of(extractable));
            Events.Raise(Extracted, "Extractables.Extracted", h => h(args));
        }

        private static Extractable Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            try
            {
                foreach (Extractable extractable in UnityEngine.Object.FindObjectsByType<Extractable>(UnityEngine.FindObjectsSortMode.None))
                {
                    if (SceneIds.Of(extractable) == id) return extractable;
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Extractables.Find", e);
            }
            return null;
        }

        private static ExtractableInfo ToInfo(Extractable extractable)
        {
            return new ExtractableInfo(SceneIds.Of(extractable), extractable.name, extractable.Health);
        }
    }
}
