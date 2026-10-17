using System.Collections.Generic;
using ObenseuerCore.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ObenseuerCore
{
    internal static class HudHintPanel
    {
        private const float Margin = 24f;
        private const float Width = 360f;
        private const float ReferenceWidth = 1920f;
        private const float ReferenceHeight = 1080f;
        private const float ReferenceMatch = 0.5f;
        private const int PlaceInterval = 15;
        private static readonly Vector3[] corners = new Vector3[4];
        private static readonly List<TextMeshProUGUI> bodyLabels = new List<TextMeshProUGUI>();
        private static GameObject root;
        private static Canvas canvas;
        private static RectTransform hint;
        private static Image hintBack;
        private static TextMeshProUGUI hintTitle;
        private static RectTransform prompt;
        private static Image promptBack;
        private static TextMeshProUGUI promptLabel;
        private static int appliedOrder;
        private static int lastPlaceFrame = -1000;

        internal static float Scale()
        {
            Ensure();
            if (canvas == null) return 1f;
            float ours = canvas.scaleFactor;
            if (ours <= 0f) ours = 1f;
            float theirs = HudHintSource.GameScale();
            return ours / theirs;
        }

        internal static void SetHint(string title, string[] body, HudHintLook look)
        {
            Ensure();
            if (hint == null || hintTitle == null) return;
            Style(hintTitle, look.TitleFont, look.TitleSize, look.TitleColor);
            hintTitle.text = title == null ? string.Empty : title;
            int count = body == null ? 0 : body.Length;
            while (bodyLabels.Count < count) bodyLabels.Add(Label(hint, UiTheme.BodySize, UiTheme.Text));
            for (int i = 0; i < bodyLabels.Count; i++)
            {
                TextMeshProUGUI label = bodyLabels[i];
                bool used = i < count;
                if (label.gameObject.activeSelf != used) label.gameObject.SetActive(used);
                if (!used) continue;
                Style(label, look.BodyFont, look.BodySize, look.BodyColor);
                label.text = body[i] == null ? string.Empty : body[i];
            }
            if (hintBack != null) hintBack.color = look.PanelColor;
            if (!hint.gameObject.activeSelf) hint.gameObject.SetActive(true);
        }

        internal static void SetPrompt(string text, HudHintLook look)
        {
            Ensure();
            if (prompt == null || promptLabel == null) return;
            Style(promptLabel, look.PromptFont, look.PromptSize, look.PromptColor);
            promptLabel.text = text == null ? string.Empty : text;
            if (promptBack != null) promptBack.color = look.PanelColor;
            if (!prompt.gameObject.activeSelf) prompt.gameObject.SetActive(true);
        }

        internal static void SetHintVisible(bool visible)
        {
            if (hint == null) return;
            if (hint.gameObject.activeSelf != visible) hint.gameObject.SetActive(visible);
        }

        internal static void SetPromptVisible(bool visible)
        {
            if (prompt == null) return;
            if (prompt.gameObject.activeSelf != visible) prompt.gameObject.SetActive(visible);
        }

        internal static void SetActive(bool hintVisible, bool promptVisible)
        {
            if (root == null) return;
            int order = HudHintGame.SortOrder();
            if (canvas != null && appliedOrder != order)
            {
                canvas.sortingOrder = order;
                appliedOrder = order;
            }
            bool want = hintVisible || promptVisible;
            if (root.activeSelf != want) root.SetActive(want);
        }

        internal static void Place(bool hintVisible, bool promptVisible, bool dirty)
        {
            if (root == null || (!hintVisible && !promptVisible)) return;
            if (!dirty && Time.frameCount - lastPlaceFrame < PlaceInterval) return;
            lastPlaceFrame = Time.frameCount;
            if (hintVisible) PlaceHint();
            if (promptVisible) PlacePrompt(hintVisible);
        }

        internal static void Reset()
        {
            lastPlaceFrame = -1000;
            if (hint != null) hint.gameObject.SetActive(false);
            if (prompt != null) prompt.gameObject.SetActive(false);
            if (root != null && root.activeSelf) root.SetActive(false);
            appliedOrder = 0;
        }

        internal static void Ensure()
        {
            if (root != null) return;
            bodyLabels.Clear();
            root = new GameObject("ObenseuerCore.HudHints", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            UnityEngine.Object.DontDestroyOnLoad(root);
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = HudHintGame.SortOrder();
            appliedOrder = canvas.sortingOrder;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.matchWidthOrHeight = ReferenceMatch;

            hint = Panel("Hint");
            hintBack = ModUi.Surface(hint, UiTheme.Card, UiTheme.CardRadius);
            Stack(hint);
            hintTitle = Label(hint, UiTheme.TitleSize, UiTheme.Text);

            prompt = Panel("Prompt");
            promptBack = ModUi.Surface(prompt, UiTheme.Card, UiTheme.CardRadius);
            Stack(prompt);
            promptLabel = Label(prompt, UiTheme.BodySize, UiTheme.Text);

            ApplyHint(false);
            ApplyPrompt(false);
            hint.gameObject.SetActive(false);
            prompt.gameObject.SetActive(false);
            root.SetActive(false);
        }

        private static RectTransform Panel(string name)
        {
            RectTransform rect = ModUi.NewRect(name, root.transform);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(Width, 0f);
            return rect;
        }

        private static void Stack(RectTransform rect)
        {
            VerticalLayoutGroup group = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            group.padding = new RectOffset(UiTheme.CardPadding, UiTheme.CardPadding, UiTheme.CardPadding, UiTheme.CardPadding);
            group.spacing = UiTheme.Gap;
            group.childAlignment = TextAnchor.UpperLeft;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            ContentSizeFitter fitter = rect.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private static TextMeshProUGUI Label(Transform parent, float size, Color color)
        {
            GameObject go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
            label.fontSize = size;
            label.color = color;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.richText = false;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
            return label;
        }

        private static void Style(TextMeshProUGUI label, TMP_FontAsset font, float size, Color color)
        {
            if (font != null && label.font != font) label.font = font;
            if (label.fontSize != size) label.fontSize = size;
            if (label.color != color) label.color = color;
        }

        private static void PlaceHint()
        {
            ApplyHint(false);
            Canvas.ForceUpdateCanvases();
            if (!Obstacle(hint)) return;
            ApplyHint(true);
            Canvas.ForceUpdateCanvases();
            if (!Obstacle(hint)) return;
            ApplyHint(false);
            LogFallback();
        }

        private static void PlacePrompt(bool hintVisible)
        {
            ApplyPrompt(false);
            Canvas.ForceUpdateCanvases();
            bool right = hintVisible && Overlaps(RectOf(hint), RectOf(prompt));
            ApplyPrompt(right);
            Canvas.ForceUpdateCanvases();
            if (!Obstacle(prompt)) return;
            ApplyPrompt(!right);
            Canvas.ForceUpdateCanvases();
            if (!Obstacle(prompt)) return;
            ApplyPrompt(right);
            LogFallback();
        }

        private static void ApplyHint(bool right)
        {
            hint.anchorMin = new Vector2(right ? 1f : 0f, 1f);
            hint.anchorMax = hint.anchorMin;
            hint.pivot = new Vector2(right ? 1f : 0f, 1f);
            hint.anchoredPosition = new Vector2(right ? -Margin : Margin, -Margin);
        }

        private static void ApplyPrompt(bool right)
        {
            prompt.anchorMin = new Vector2(right ? 1f : 0f, 0f);
            prompt.anchorMax = prompt.anchorMin;
            prompt.pivot = new Vector2(right ? 1f : 0f, 0f);
            prompt.anchoredPosition = new Vector2(right ? -Margin : Margin, Margin);
        }

        private static bool Obstacle(RectTransform mine)
        {
            Rect rect = RectOf(mine);
            RectTransform game = HudHintSource.HintPanelRect();
            if (Active(game) && Overlaps(rect, RectOf(game))) return true;
            RectTransform other = HudHintSource.OtherRect();
            if (Active(other) && Overlaps(rect, RectOf(other))) return true;
            return false;
        }

        private static bool Active(RectTransform rect)
        {
            return rect != null && rect.gameObject.activeInHierarchy;
        }

        private static bool Overlaps(Rect a, Rect b)
        {
            return a.Overlaps(b);
        }

        private static Rect RectOf(RectTransform rect)
        {
            rect.GetWorldCorners(corners);
            float minX = corners[0].x;
            float minY = corners[0].y;
            float maxX = corners[0].x;
            float maxY = corners[0].y;
            for (int i = 1; i < corners.Length; i++)
            {
                if (corners[i].x < minX) minX = corners[i].x;
                if (corners[i].y < minY) minY = corners[i].y;
                if (corners[i].x > maxX) maxX = corners[i].x;
                if (corners[i].y > maxY) maxY = corners[i].y;
            }
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        private static void LogFallback()
        {
            ApiLog.Once(HudReport.Owner, "hint placement", "the mod hint stays at its default corner, the game panel still overlaps it");
        }
    }
}
