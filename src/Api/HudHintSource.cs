using System;
using System.Reflection;
using ObenseuerCore.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ObenseuerCore
{
    internal sealed class HudHintLook
    {
        internal TMP_FontAsset TitleFont;
        internal float TitleSize;
        internal Color TitleColor;
        internal TMP_FontAsset BodyFont;
        internal float BodySize;
        internal Color BodyColor;
        internal Color PanelColor;
        internal TMP_FontAsset PromptFont;
        internal float PromptSize;
        internal Color PromptColor;
    }

    internal static class HudHintSource
    {
        private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly FieldInfo HintTitleField = typeof(HintOverlay).GetField("titleText", Members);
        private static readonly FieldInfo HintBodyField = typeof(HintOverlay).GetField("bodyText", Members);
        private static readonly FieldInfo HintPanelField = typeof(HintOverlay).GetField("panel", Members);
        private static readonly FieldInfo TutorialTextField = typeof(Tutorial).GetField("TutorialText", Members);
        private static readonly FieldInfo TutorialOtherField = typeof(Tutorial).GetField("Other", Members);
        private static readonly PropertyInfo TutorialOtherProperty = typeof(Tutorial).GetProperty("Other", Members);

        internal static HudHintLook Read(float ourScale)
        {
            HudHintLook look = new HudHintLook();
            TMP_Text title = HintTitle();
            TMP_Text body = HintBody();
            TMP_Text prompt = TutorialText();
            if (title == null && body == null) ApiLog.Once(HudReport.Owner, "hint style: the game hint box is not loaded", "using the framework theme for the hint panel");
            if (prompt == null) ApiLog.Once(HudReport.Owner, "hint style: the game tutorial text is not loaded", "using the framework theme for the prompt");
            look.TitleFont = FontOf(title, FontOf(body, TMP_Settings.defaultFontAsset));
            look.TitleSize = SizeOf(title, UiTheme.TitleSize) * ourScale;
            look.TitleColor = ColorOf(title, UiTheme.Text);
            look.BodyFont = FontOf(body, FontOf(title, TMP_Settings.defaultFontAsset));
            look.BodySize = SizeOf(body, UiTheme.BodySize) * ourScale;
            look.BodyColor = ColorOf(body, UiTheme.Text);
            look.PanelColor = PanelColor();
            look.PromptFont = FontOf(prompt, TMP_Settings.defaultFontAsset);
            look.PromptSize = SizeOf(prompt, UiTheme.BodySize) * ourScale;
            look.PromptColor = ColorOf(prompt, UiTheme.Text);
            return look;
        }

        internal static float GameScale()
        {
            Canvas canvas = CanvasOf(HintOverlay.instance);
            if (canvas == null) canvas = CanvasOf(Tutorial.instance);
            if (canvas == null) return 1f;
            try
            {
                float scale = canvas.scaleFactor;
                return scale > 0f ? scale : 1f;
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "hint style: canvas scale", e);
                return 1f;
            }
        }

        internal static RectTransform HintPanelRect()
        {
            return RectOf(HintPanelField, HintOverlay.instance, "hint style: hint panel");
        }

        internal static RectTransform OtherRect()
        {
            object tutorial = Tutorial.instance;
            if (Missing(tutorial)) return null;
            try
            {
                object value = TutorialOtherField != null ? TutorialOtherField.GetValue(tutorial) : (TutorialOtherProperty != null ? TutorialOtherProperty.GetValue(tutorial, null) : null);
                return RectOfValue(value);
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "hint style: tutorial object", e);
                return null;
            }
        }

        private static TMP_Text HintTitle()
        {
            return Text(HintTitleField, HintOverlay.instance, "hint style: title text");
        }

        private static TMP_Text HintBody()
        {
            return Text(HintBodyField, HintOverlay.instance, "hint style: body text");
        }

        private static TMP_Text TutorialText()
        {
            return Text(TutorialTextField, Tutorial.instance, "hint style: tutorial text");
        }

        internal static string GameTutorialText()
        {
            TMP_Text label = TutorialText();
            if (label == null) return null;
            try
            {
                return label.text;
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "hint style: tutorial text", e);
                return null;
            }
        }

        private static TMP_Text Text(FieldInfo field, object owner, string what)
        {
            if (field == null || Missing(owner)) return null;
            try
            {
                return field.GetValue(owner) as TMP_Text;
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, what, e);
                return null;
            }
        }

        private static RectTransform RectOf(FieldInfo field, object owner, string what)
        {
            if (field == null || Missing(owner)) return null;
            try
            {
                return RectOfValue(field.GetValue(owner));
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, what, e);
                return null;
            }
        }

        private static RectTransform RectOfValue(object value)
        {
            if (value == null) return null;
            RectTransform rect = value as RectTransform;
            if (rect != null) return rect;
            Component component = value as Component;
            if (component != null) return component.transform as RectTransform;
            GameObject gameObject = value as GameObject;
            if (gameObject != null) return gameObject.transform as RectTransform;
            return null;
        }

        private static bool Missing(object owner)
        {
            if (owner == null) return true;
            if (!(owner is UnityEngine.Object)) return false;
            return (UnityEngine.Object)owner == null;
        }

        private static TMP_FontAsset FontOf(TMP_Text text, TMP_FontAsset fallback)
        {
            if (text == null) return fallback;
            try
            {
                TMP_FontAsset font = text.font;
                return font != null ? font : fallback;
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "hint style: font", e);
                return fallback;
            }
        }

        private static float SizeOf(TMP_Text text, float fallback)
        {
            if (text == null) return fallback;
            try
            {
                float size = text.fontSize;
                return size > 0f ? size : fallback;
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "hint style: font size", e);
                return fallback;
            }
        }

        private static Color ColorOf(TMP_Text text, Color fallback)
        {
            if (text == null) return fallback;
            try
            {
                return text.color;
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "hint style: color", e);
                return fallback;
            }
        }

        private static Color PanelColor()
        {
            try
            {
                RectTransform panel = HintPanelRect();
                if (panel == null) return UiTheme.Card;
                Image image = panel.GetComponent<Image>();
                return image != null ? image.color : UiTheme.Card;
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "hint style: panel color", e);
                return UiTheme.Card;
            }
        }

        private static Canvas CanvasOf(object owner)
        {
            if (Missing(owner)) return null;
            try
            {
                Component component = owner as Component;
                if (component == null) return null;
                Canvas canvas = component.GetComponent<Canvas>();
                if (canvas == null) canvas = component.GetComponentInParent<Canvas>();
                if (canvas == null) canvas = component.GetComponentInChildren<Canvas>(true);
                return canvas;
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "hint style: canvas", e);
                return null;
            }
        }
    }
}
