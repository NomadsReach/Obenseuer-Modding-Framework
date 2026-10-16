using System;
using System.Collections.Generic;
using System.Reflection;
using BeautifyEffect;
using HarmonyLib;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class ScreenFxBlur
    {
        internal const string Owner = "obenseuer.moddingframework.screenfx.blur";
        internal const float MaxIntensity = 4f;
        private const string Log = "ObenseuerCore";
        private const int MaxOwners = 4;
        private static readonly FieldInfo beautifyListField = AccessTools.Field(typeof(EffectsController), "beautifyList");
        private static readonly List<Entry> entries = new List<Entry>();
        private static readonly List<KeyValuePair<Beautify, float>> touched = new List<KeyValuePair<Beautify, float>>();
        private static bool installed;
        private static bool fading;
        private static bool gameWanted;

        internal static void Init()
        {
            installed = beautifyListField != null && ScreenFxPatches.InstallBlur();
            if (!installed) CorePatcher.DisableOwner(Owner);
        }

        internal static bool Set(string owner, float amount, float fadeSeconds)
        {
            try
            {
                if (!installed || CorePatcher.IsDisabled(Owner)) return false;
                Entry entry = Remove(owner);
                if (entry == null)
                {
                    if (entries.Count >= MaxOwners)
                    {
                        ApiLog.Once(Log, "ScreenFx.Blur owners", "the blur is already held by " + MaxOwners + " owners, this call is refused");
                        return false;
                    }
                    entry = new Entry();
                    Entry top = entries.Count == 0 ? null : entries[entries.Count - 1];
                    entry.ShownAmount = top != null ? top.ShownAmount : 0f;
                }
                entry.Owner = owner;
                entry.FromAmount = entry.ShownAmount;
                entry.TargetAmount = amount;
                entry.Duration = fadeSeconds;
                entry.Elapsed = 0f;
                if (fadeSeconds <= 0f)
                {
                    entry.ShownAmount = amount;
                }
                else
                {
                    fading = true;
                }
                entries.Add(entry);
                Refresh();
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Log, "ScreenFx.Blur", e);
                return false;
            }
        }

        internal static bool Clear(string owner)
        {
            try
            {
                if (Remove(owner) == null) return false;
                Refresh();
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Log, "ScreenFx.ClearBlur", e);
                return false;
            }
        }

        internal static void Tick()
        {
            if (!fading) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            bool running = false;
            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                if (entry.Elapsed >= entry.Duration) continue;
                entry.Elapsed += dt;
                float t = entry.Elapsed >= entry.Duration ? 1f : entry.Elapsed / entry.Duration;
                entry.ShownAmount = Mathf.Lerp(entry.FromAmount, entry.TargetAmount, t);
                if (entry.Elapsed < entry.Duration) running = true;
            }
            fading = running;
            Refresh();
        }

        internal static void Reset()
        {
            entries.Clear();
            fading = false;
            Release();
        }

        internal static void GameChanged(bool on)
        {
            try
            {
                gameWanted = on;
                if (touched.Count > 0) Refresh();
            }
            catch (Exception e)
            {
                ApiLog.Once(Log, "ScreenFx.Blur", e);
            }
        }

        internal static void ProfileChanged()
        {
            try
            {
                if (touched.Count == 0) return;
                for (int i = 0; i < touched.Count; i++)
                {
                    Beautify beautify = touched[i].Key;
                    if (beautify == null) continue;
                    gameWanted = beautify.blur;
                    touched[i] = new KeyValuePair<Beautify, float>(beautify, beautify.blurIntensity);
                }
                Refresh();
            }
            catch (Exception e)
            {
                ApiLog.Once(Log, "ScreenFx.Blur", e);
            }
        }

        internal static void Refresh()
        {
            Entry top = entries.Count == 0 ? null : entries[entries.Count - 1];
            if (top == null)
            {
                Release();
                return;
            }
            List<Beautify> live = Live();
            if (touched.Count == 0 && live.Count > 0) gameWanted = live[0].blur;
            float intensity = Mathf.Clamp01(top.ShownAmount) * MaxIntensity;
            for (int i = 0; i < live.Count; i++)
            {
                if (!IsTouched(live[i])) touched.Add(new KeyValuePair<Beautify, float>(live[i], live[i].blurIntensity));
                live[i].blur = true;
                live[i].blurIntensity = intensity;
            }
        }

        private static void Release()
        {
            for (int i = 0; i < touched.Count; i++)
            {
                Beautify beautify = touched[i].Key;
                if (beautify == null) continue;
                beautify.blurIntensity = touched[i].Value;
                beautify.blur = gameWanted;
            }
            touched.Clear();
        }

        private static List<Beautify> Live()
        {
            List<Beautify> live = new List<Beautify>();
            if (EffectsController.instance == null) return live;
            List<EffectBeautify> list = (List<EffectBeautify>)beautifyListField.GetValue(EffectsController.instance);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].beautify != null) live.Add(list[i].beautify);
            }
            return live;
        }

        private static bool IsTouched(Beautify beautify)
        {
            for (int i = 0; i < touched.Count; i++)
            {
                if (touched[i].Key == beautify) return true;
            }
            return false;
        }

        private static Entry Remove(string owner)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Owner != owner) continue;
                Entry entry = entries[i];
                entries.RemoveAt(i);
                return entry;
            }
            return null;
        }

        private sealed class Entry
        {
            public string Owner;
            public float ShownAmount;
            public float FromAmount;
            public float TargetAmount;
            public float Duration;
            public float Elapsed;
        }
    }
}
