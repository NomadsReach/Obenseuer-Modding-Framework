using System;
using System.Reflection;
using ObenseuerCore.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ObenseuerCore
{
    [Obsolete("Use ObenseuerCore.Ui.ModUi")]
    public sealed class ModMenuWidgets
    {
        public float FontSize
        {
            get { return UiTheme.BodySize; }
        }

        public float RowHeight
        {
            get { return UiTheme.RowHeight; }
        }

        public Color TextColor
        {
            get { return UiTheme.Text; }
        }

        public Color AccentColor
        {
            get { return UiTheme.Accent; }
        }

        public Color MutedColor
        {
            get { return UiTheme.Muted; }
        }

        public TextMeshProUGUI Label(Transform parent, string text, float size, Color color, TextAlignmentOptions align)
        {
            return ModUi.Label(parent, text, size, color, align, false);
        }

        public void Header(Transform parent, string text, float size, Color color)
        {
            ModUi.Height(Label(parent, text, size, color, TextAlignmentOptions.BottomLeft).gameObject, Mathf.Max(RowHeight * 0.8f, size * 1.4f));
        }

        public TMP_InputField TextField(Transform parent, string text)
        {
            return ModUi.TextField(parent, "", text, null).Rect.GetComponentInChildren<TMP_InputField>();
        }

        public UnityEngine.UI.Button ActionButton(Transform parent, string text, UnityAction onClick)
        {
            return ModUi.Button(parent, text, ButtonStyle.Secondary, onClick == null ? null : new Action(onClick.Invoke));
        }

        public OptionsButton CloneCycle(Transform parent)
        {
            OptionsButton template = CycleTemplate();
            if (template == null) throw new InvalidOperationException("this menu has no cycle control");
            GameObject go = UnityEngine.Object.Instantiate(template.gameObject, parent);
            go.SetActive(true);
            OptionsButton cycle = go.GetComponent<OptionsButton>();
            cycle.slider = null;
            Strip(cycle.buttonLeft.onClick);
            Strip(cycle.buttonRight.onClick);
            cycle.Enable();
            if (cycle.text != null) cycle.text.fontSize = FontSize;
            return cycle;
        }

        public Slider CloneSlider(Transform parent)
        {
            Slider template = SliderTemplate();
            if (template == null) return ModUi.Slider(parent, "", 0f, 1f, 0f, null, 2).Rect.GetComponentInChildren<Slider>();
            GameObject go = UnityEngine.Object.Instantiate(template.gameObject, parent);
            go.SetActive(true);
            Slider slider = go.GetComponent<Slider>();
            Strip(slider.onValueChanged);
            return slider;
        }

        public static void Place(RectTransform rect, float x0, float y0, float x1, float y1)
        {
            ModUi.Place(rect, new Vector2(x0, y0), new Vector2(x1, y1), Vector2.zero, Vector2.zero);
        }

        internal static void Clear(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--) UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }

        internal static void Strip(UnityEvent events)
        {
            for (int i = 0; i < events.GetPersistentEventCount(); i++) events.SetPersistentListenerState(i, UnityEventCallState.Off);
            events.RemoveAllListeners();
        }

        internal static void Strip(UnityEvent<float> events)
        {
            for (int i = 0; i < events.GetPersistentEventCount(); i++) events.SetPersistentListenerState(i, UnityEventCallState.Off);
            events.RemoveAllListeners();
        }

        private static OptionsMenu Options()
        {
            return PauseMenu.instance == null ? null : ModMenuReflect.Get<OptionsMenu>(PauseMenu.instance, "optionsMenu");
        }

        private static OptionsButton CycleTemplate()
        {
            OptionsMenu options = Options();
            return options == null ? null : ModMenuReflect.Get<OptionsButton>(options, "languageButton");
        }

        private static Slider SliderTemplate()
        {
            OptionsMenu options = Options();
            return options == null ? null : ModMenuReflect.Get<Slider>(options, "fovSlider");
        }
    }

    internal static class ModMenuReflect
    {
        internal const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        internal static object Value(object target, string field)
        {
            if (target == null) return null;
            FieldInfo info = target.GetType().GetField(field, Any);
            if (info == null) throw new MissingFieldException(target.GetType().FullName, field);
            return info.GetValue(target);
        }

        internal static T Get<T>(object target, string field) where T : class
        {
            return Value(target, field) as T;
        }

        internal static void Set(object target, string field, object value)
        {
            FieldInfo info = target.GetType().GetField(field, Any);
            if (info == null) throw new MissingFieldException(target.GetType().FullName, field);
            info.SetValue(target, value);
        }
    }
}
