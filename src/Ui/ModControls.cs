using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ObenseuerCore.Ui
{
    internal sealed class Coalesce<T>
    {
        private readonly UiScope owner;
        private readonly Action<T> apply;
        private readonly Action flush;
        private T latest;
        private bool queued;

        internal Coalesce(UiScope owner, Action<T> apply)
        {
            this.owner = owner;
            this.apply = apply;
            flush = Flush;
        }

        internal void Set(T value)
        {
            latest = value;
            if (queued) return;
            queued = true;
            owner.Defer(flush);
        }

        private void Flush()
        {
            queued = false;
            owner.Run(apply, latest);
        }
    }

    public sealed class ModSlider
    {
        private const double MaxShown = 1000000000d;
        private const int MaxDecimals = 6;
        private readonly UnityEngine.UI.Slider slider;
        private readonly TextMeshProUGUI readout;
        private readonly int decimals;
        private readonly Coalesce<float> changed;
        private readonly char[] digits = new char[24];

        internal ModSlider(RectTransform row, UnityEngine.UI.Slider slider, TextMeshProUGUI readout, int decimals, UiScope owner, Action<float> onChange)
        {
            Rect = row;
            this.slider = slider;
            this.readout = readout;
            this.decimals = Mathf.Clamp(decimals, 0, MaxDecimals);
            changed = new Coalesce<float>(owner, onChange);
            slider.onValueChanged.AddListener(OnChanged);
            Show(slider.value);
        }

        public RectTransform Rect { get; private set; }

        public void Set(float value)
        {
            slider.SetValueWithoutNotify(value);
            Show(slider.value);
        }

        private void OnChanged(float value)
        {
            Show(value);
            changed.Set(value);
        }

        private void Show(float value)
        {
            readout.SetCharArray(digits, 0, Write(value));
        }

        private int Write(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                digits[0] = '-';
                return 1;
            }
            long scale = 1;
            for (int i = 0; i < decimals; i++) scale *= 10;
            long scaled = (long)Math.Round(Math.Min(Math.Abs(value), MaxShown) * scale);
            int length = 0;
            if (value < 0f && scaled != 0) digits[length++] = '-';
            long whole = scaled / scale;
            long fraction = scaled % scale;
            int start = length;
            do
            {
                digits[length++] = (char)('0' + (int)(whole % 10));
                whole /= 10;
            }
            while (whole > 0);
            Array.Reverse(digits, start, length - start);
            if (decimals > 0)
            {
                digits[length++] = '.';
                for (int i = 0; i < decimals; i++)
                {
                    digits[length + decimals - 1 - i] = (char)('0' + (int)(fraction % 10));
                    fraction /= 10;
                }
                length += decimals;
            }
            return length;
        }
    }

    public sealed class ModToggle
    {
        private readonly UnityEngine.UI.Toggle toggle;
        private readonly Image back;
        private readonly RectTransform knob;
        private readonly UiScope owner;
        private readonly Action<bool> onChange;

        internal ModToggle(RectTransform row, UnityEngine.UI.Toggle toggle, Image back, RectTransform knob, UiScope owner, bool value, Action<bool> onChange)
        {
            Rect = row;
            this.toggle = toggle;
            this.back = back;
            this.knob = knob;
            this.owner = owner;
            this.onChange = onChange;
            toggle.isOn = value;
            toggle.onValueChanged.AddListener(OnChanged);
            Paint(value);
        }

        public RectTransform Rect { get; private set; }

        public void Set(bool on)
        {
            toggle.SetIsOnWithoutNotify(on);
            Paint(on);
        }

        private void OnChanged(bool on)
        {
            Paint(on);
            owner.Run(onChange, on);
        }

        private void Paint(bool on)
        {
            if (on) ModUi.Tint(toggle, back, UiTheme.Accent, UiTheme.AccentHover, UiTheme.AccentPressed);
            else ModUi.Tint(toggle, back, UiTheme.Control, UiTheme.Hover, UiTheme.Border);
            knob.anchoredPosition = new Vector2(on ? 10f : -10f, 0f);
        }
    }

    public sealed class ModTextField
    {
        private readonly TMP_InputField field;
        private readonly UiScope owner;
        private readonly Action<string> onChange;

        internal ModTextField(RectTransform row, TMP_InputField field, UiScope owner, Action<string> onChange, bool onEnd)
        {
            Rect = row;
            this.field = field;
            this.owner = owner;
            this.onChange = onChange;
            if (onEnd) field.onEndEdit.AddListener(OnChanged);
            else field.onValueChanged.AddListener(OnChanged);
        }

        public RectTransform Rect { get; private set; }

        public void Set(string text)
        {
            field.SetTextWithoutNotify(text ?? "");
        }

        private void OnChanged(string text)
        {
            owner.Run(onChange, text);
        }
    }

    public sealed class ModKeyBinding
    {
        private static readonly KeyCode[] Modifiers = { KeyCode.LeftControl, KeyCode.RightControl, KeyCode.LeftShift, KeyCode.RightShift, KeyCode.LeftAlt, KeyCode.RightAlt };
        private readonly TextMeshProUGUI label;
        private readonly UiScope owner;
        private readonly Action<KeyboardShortcut> onChange;
        private KeyboardShortcut value;

        internal ModKeyBinding(RectTransform row, TextMeshProUGUI label, KeyboardShortcut value, UiScope owner, Action<KeyboardShortcut> onChange)
        {
            Rect = row;
            this.label = label;
            this.value = value;
            this.owner = owner;
            this.onChange = onChange;
            Show();
        }

        public RectTransform Rect { get; private set; }

        public void Set(KeyboardShortcut shortcut)
        {
            value = shortcut;
            Show();
        }

        internal void Capturing(bool on)
        {
            if (label == null) return;
            label.text = on ? "Press a key (Esc)" : value.ToString();
        }

        internal void Bind(KeyCode key)
        {
            value = new KeyboardShortcut(key, Held());
            Show();
            owner.Run(onChange, value);
        }

        internal static bool IsModifier(KeyCode key)
        {
            return Array.IndexOf(Modifiers, key) >= 0;
        }

        private static KeyCode[] Held()
        {
            List<KeyCode> held = new List<KeyCode>();
            for (int i = 0; i < Modifiers.Length; i++)
            {
                if (Input.GetKey(Modifiers[i])) held.Add(Modifiers[i]);
            }
            return held.ToArray();
        }

        private void Show()
        {
            if (label != null) label.text = value.ToString();
        }
    }
}
