using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.Mono;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class DifficultyModPresetsPatches
    {
        internal const string Owner = "obenseuer.moddingframework.difficultyselector";
        private const string CoreGuid = "obenseuer.moddingframework";
        private const string SettingSection = "UI";
        private const string SettingKey = "ModPresetsInSelector";
        private static ConfigEntry<bool> setting;
        private static bool installed;
        private static DifficultyController host;
        private static int vanilla = -1;
        private static DifficultyPreset[] tail = new DifficultyPreset[0];

        internal static bool Install()
        {
            try
            {
                bool ok = Hook(typeof(DifficultyUI), "Start", Type.EmptyTypes, "ScreenHook", null);
                ok &= Hook(typeof(DifficultyUI), "OnEnable", Type.EmptyTypes, null, "ScreenHook");
                ok &= Hook(typeof(DifficultyController), "ChangeDifficulty", new[] { typeof(bool) }, null, "ChangePostfix");
                ok &= Hook(typeof(SaveController), "DeSerializeData", new[] { typeof(UnityEngine.Object), typeof(bool) }, null, "LoadedPostfix");
                installed = ok;
                return ok;
            }
            catch (Exception e)
            {
                CoreLog.For(Owner).Error("difficulty selector hook install failed: " + e.ToString());
                return false;
            }
        }

        private static bool Hook(Type type, string name, Type[] args, string prefix, string postfix)
        {
            return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(DifficultyModPresetsPatches), prefix), CorePatcher.Handler(typeof(DifficultyModPresetsPatches), postfix), null, false, Owner);
        }

        private static bool Enabled()
        {
            if (setting == null) Bind();
            return setting == null || setting.Value;
        }

        private static void Bind()
        {
            try
            {
                PluginInfo info = Loader.Find(CoreGuid);
                BaseUnityPlugin plugin = info == null ? null : info.Instance as BaseUnityPlugin;
                if (plugin == null)
                {
                    ApiLog.Once(Owner, "config setting", "the core plugin was not found, the difficulty selector setting is not in the config");
                    return;
                }
                setting = plugin.Config.Bind(SettingSection, SettingKey, true, new ConfigDescription("List the difficulty presets mods registered in the game's own difficulty selector.", (AcceptableValueBase)null, new object[] { new ModMenuTitle("List mod difficulty presets in the game's selector") }));
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "config setting", e);
            }
        }

        private static void ScreenHook()
        {
            if (!installed || !Enabled()) return;
            try
            {
                Build();
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "difficulty selector", e);
            }
        }

        private static void Build()
        {
            DifficultyController controller = DifficultyController.instance;
            if (controller == null) return;
            DifficultyInfo[] all = controller.GetDifficulties();
            if (all == null || all.Length == 0) return;
            int count = Vanilla(controller, all);
            DifficultyPreset[] presets = DifficultyPresets.Registered();
            List<DifficultyInfo> built = new List<DifficultyInfo>(DifficultyModPresets.Length(count, presets.Length));
            for (int i = 0; i < count && i < all.Length; i++) built.Add(all[i]);
            List<DifficultyPreset> kept = new List<DifficultyPreset>();
            for (int i = 0; i < presets.Length; i++)
            {
                DifficultyInfo reuse = built.Count < all.Length ? all[built.Count] : null;
                DifficultyInfo entry = Entry(presets[i], all, reuse);
                if (entry == null) continue;
                built.Add(entry);
                kept.Add(presets[i]);
            }
            DifficultyInfo[] next = built.ToArray();
            tail = kept.ToArray();
            if (Same(all, next)) return;
            ModMenuReflect.Set(controller, "difficulties", next);
        }

        private static int Vanilla(DifficultyController controller, DifficultyInfo[] all)
        {
            if (host != controller || vanilla < 0 || vanilla > all.Length)
            {
                host = controller;
                vanilla = all.Length;
                tail = new DifficultyPreset[0];
            }
            return vanilla;
        }

        private static DifficultyInfo Entry(DifficultyPreset preset, DifficultyInfo[] all, DifficultyInfo reuse)
        {
            if (reuse != null && string.Equals(reuse.difficultyName, preset.Name, StringComparison.Ordinal)) return reuse;
            DifficultyInfo entry;
            try
            {
                entry = DifficultyPresets.Entry(preset, all);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "preset " + preset.Key, e);
                return null;
            }
            if (entry == null) ApiLog.Once(Owner, "preset " + preset.Key, "the game has no level matching the preset's base");
            return entry;
        }

        private static bool Same(DifficultyInfo[] left, DifficultyInfo[] right)
        {
            if (left.Length != right.Length) return false;
            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i]) return false;
            }
            return true;
        }

        private static void ChangePostfix(DifficultyController __instance)
        {
            if (!installed || !Enabled() || __instance != host) return;
            try
            {
                int index = Index(__instance);
                DifficultyPresets.Choose(DifficultyModPresets.IsListed(index, vanilla, tail.Length) ? tail[index - vanilla] : null);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "difficulty choice", e);
            }
        }

        private static void LoadedPostfix(UnityEngine.Object data, ref bool __result)
        {
            if (!installed || !Enabled() || !__result) return;
            DifficultyController controller = data as DifficultyController;
            if (controller == null) return;
            try
            {
                DifficultyInfo[] all = controller.GetDifficulties();
                if (all == null || all.Length == 0) return;
                int index = Index(controller);
                int wanted = DifficultyModPresets.Clamp(index, all.Length, Custom(all));
                if (wanted != index) ModMenuReflect.Set(controller, "currentDifficultyIndex", wanted);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "difficulty index", e);
            }
        }

        private static int Custom(DifficultyInfo[] all)
        {
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].difficulty == Difficulty.Custom) return i;
            }
            return -1;
        }

        private static int Index(DifficultyController controller)
        {
            object value = ModMenuReflect.Value(controller, "currentDifficultyIndex");
            return value == null ? 0 : (int)value;
        }
    }
}
