using System;
using System.Collections.Generic;
using ObenseuerCore.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ObenseuerCore
{
    internal static class HudBars
    {
        internal const int MaxBars = 4;
        private const int SortOrder = 400;
        private const float SmoothSeconds = 0.15f;
        private const float ReferenceWidth = 1920f;
        private const float ReferenceHeight = 1080f;
        private const float ReferenceMatch = 0.5f;
        private const float BarWidth = 360f;
        private const float TopOffset = 24f;
        private const float TitleHeight = 22f;
        private const float TrackHeight = 14f;
        private const float ItemGap = 4f;
        private const float StackGap = 8f;
        private static readonly List<Entry> entries = new List<Entry>();
        private static GameObject root;
        private static RectTransform stack;

        private sealed class Entry
        {
            internal string Owner;
            internal GameObject Go;
            internal TextMeshProUGUI Title;
            internal Image Fill;
            internal RectTransform FillRect;
            internal float Target;
            internal float Shown;
        }

        internal static bool Set(string owner, string title, float fill, Color color)
        {
            try
            {
                Entry entry = Find(owner);
                if (entry == null)
                {
                    if (entries.Count >= MaxBars) return false;
                    entry = Create(owner);
                    if (entry == null) return false;
                    entries.Add(entry);
                    entry.Shown = fill;
                    ApplyShown(entry);
                }
                entry.Title.text = title ?? string.Empty;
                entry.Title.gameObject.SetActive(!string.IsNullOrEmpty(title));
                entry.Fill.color = color;
                entry.Target = fill;
                Refresh();
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "Bar", e);
                return false;
            }
        }

        internal static bool Clear(string owner)
        {
            try
            {
                Entry entry = Find(owner);
                if (entry == null) return false;
                Remove(entry);
                Refresh();
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "ClearBar", e);
                return false;
            }
        }

        internal static void ClearAll()
        {
            while (entries.Count > 0) Remove(entries[entries.Count - 1]);
            Refresh();
        }

        internal static void Tick()
        {
            Prune();
            if (entries.Count == 0) return;
            float step = Time.unscaledDeltaTime / SmoothSeconds;
            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                if (entry.Shown == entry.Target) continue;
                entry.Shown = Mathf.MoveTowards(entry.Shown, entry.Target, step);
                ApplyShown(entry);
            }
            Refresh();
        }

        private static Entry Create(string owner)
        {
            EnsureRoot();
            if (root == null) return null;
            RectTransform item = ModUi.NewRect("Bar " + owner, stack);
            VerticalLayoutGroup group = item.gameObject.AddComponent<VerticalLayoutGroup>();
            group.spacing = ItemGap;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            TextMeshProUGUI title = ModUi.Label(item, string.Empty, UiTheme.BodySize, UiTheme.Text, TextAlignmentOptions.MidlineLeft, false);
            ModUi.Height(title.gameObject, TitleHeight);
            RectTransform track = ModUi.NewRect("Track", item);
            ModUi.Surface(track, UiTheme.Control, UiTheme.TrackRadius);
            ModUi.Height(track.gameObject, TrackHeight);
            RectTransform fill = ModUi.NewRect("Fill", track);
            Image image = ModUi.Surface(fill, UiTheme.Accent, UiTheme.TrackRadius);
            ModUi.Place(fill, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
            return new Entry { Owner = owner, Go = item.gameObject, Title = title, Fill = image, FillRect = fill };
        }

        private static void EnsureRoot()
        {
            if (root != null) return;
            entries.Clear();
            root = new GameObject("ObenseuerCore.HudBars", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            UnityEngine.Object.DontDestroyOnLoad(root);
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortOrder;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.matchWidthOrHeight = ReferenceMatch;
            stack = ModUi.NewRect("Bars", root.transform);
            stack.anchorMin = new Vector2(0.5f, 1f);
            stack.anchorMax = new Vector2(0.5f, 1f);
            stack.pivot = new Vector2(0.5f, 1f);
            stack.sizeDelta = new Vector2(BarWidth, 0f);
            stack.anchoredPosition = new Vector2(0f, -TopOffset);
            VerticalLayoutGroup group = stack.gameObject.AddComponent<VerticalLayoutGroup>();
            group.spacing = StackGap;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            ContentSizeFitter fitter = stack.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private static void Refresh()
        {
            if (root == null) return;
            bool want = entries.Count > 0 && !Hidden();
            if (root.activeSelf != want) root.SetActive(want);
        }

        private static bool Hidden()
        {
            GameUIController ui = GameUIController.instance;
            return Hud.GameUiHidden || (ui != null && !ui.GameUIVisible()) || UiState.IsMenuOpen || UiState.IsPaused || SaveGate.Busy;
        }

        private static void ApplyShown(Entry entry)
        {
            entry.FillRect.anchorMax = new Vector2(entry.Shown, 1f);
        }

        private static void Prune()
        {
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (entries[i].Go == null) entries.RemoveAt(i);
            }
        }

        private static void Remove(Entry entry)
        {
            entries.Remove(entry);
            if (entry.Go == null) return;
            entry.Go.SetActive(false);
            UnityEngine.Object.Destroy(entry.Go);
        }

        private static Entry Find(string owner)
        {
            Prune();
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Owner == owner) return entries[i];
            }
            return null;
        }
    }
}
