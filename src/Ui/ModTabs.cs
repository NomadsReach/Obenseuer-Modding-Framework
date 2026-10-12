using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ObenseuerCore.Ui
{
    public sealed class ModTabs
    {
        private readonly UiScope owner;
        private readonly Action<int> onSelect;
        private readonly UnityEngine.UI.Button[] buttons;
        private readonly TextMeshProUGUI[] labels;
        private readonly GameObject[] views;
        private readonly RectTransform[] contents;
        private int selected;

        internal ModTabs(UiScope owner, RectTransform bar, RectTransform pages, string[] names, Action<int> onSelect)
        {
            this.owner = owner;
            this.onSelect = onSelect;
            buttons = new UnityEngine.UI.Button[names.Length];
            labels = new TextMeshProUGUI[names.Length];
            views = new GameObject[names.Length];
            contents = new RectTransform[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                int index = i;
                buttons[i] = ModUi.Button(bar, names[i], ButtonStyle.Secondary, () => Pick(index));
                LayoutElement size = ModUi.Sizing(buttons[i].gameObject);
                size.minWidth = 0f;
                size.preferredWidth = 0f;
                size.flexibleWidth = 1f;
                labels[i] = buttons[i].GetComponentInChildren<TextMeshProUGUI>();
                views[i] = ModUi.ScrollPage(pages, out contents[i]).gameObject;
            }
            Show(0);
        }

        public int Selected
        {
            get { return selected; }
        }

        public RectTransform Page(int index)
        {
            return contents[index];
        }

        public void Set(int index)
        {
            if (index < 0 || index >= buttons.Length) throw new ArgumentOutOfRangeException("index");
            Show(index);
        }

        private void Pick(int index)
        {
            Show(index);
            owner.Run(onSelect, index);
        }

        private void Show(int index)
        {
            selected = index;
            for (int i = 0; i < buttons.Length; i++)
            {
                bool on = i == index;
                ModUi.Tint(buttons[i], buttons[i].targetGraphic, on ? UiTheme.Accent : UiTheme.Control, on ? UiTheme.AccentHover : UiTheme.Hover, on ? UiTheme.AccentPressed : UiTheme.Border);
                labels[i].color = on ? UiTheme.Text : UiTheme.Muted;
                views[i].SetActive(on);
            }
        }
    }

    public sealed class ModSegmented
    {
        private readonly UiScope owner;
        private readonly Action<int> onSelect;
        private readonly UnityEngine.UI.Button[] buttons;
        private readonly TextMeshProUGUI[] labels;

        internal ModSegmented(UiScope owner, RectTransform host, string[] names, int index, Action<int> onSelect)
        {
            this.owner = owner;
            this.onSelect = onSelect;
            Rect = host;
            buttons = new UnityEngine.UI.Button[names.Length];
            labels = new TextMeshProUGUI[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                int slot = i;
                buttons[i] = ModUi.Button(host, names[i], ButtonStyle.Secondary, () => Pick(slot));
                LayoutElement size = ModUi.Sizing(buttons[i].gameObject);
                size.minWidth = 0f;
                size.preferredWidth = 0f;
                size.flexibleWidth = 1f;
                size.minHeight = UiTheme.SegmentButtonHeight;
                size.preferredHeight = UiTheme.SegmentButtonHeight;
                labels[i] = buttons[i].GetComponentInChildren<TextMeshProUGUI>();
            }
            Show(index);
        }

        public RectTransform Rect { get; private set; }

        public void Set(int index)
        {
            Show(index);
        }

        private void Pick(int index)
        {
            Show(index);
            owner.Run(onSelect, index);
        }

        private void Show(int index)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                bool on = i == index;
                ModUi.Tint(buttons[i], buttons[i].targetGraphic, on ? UiTheme.Accent : UiTheme.Control, on ? UiTheme.AccentHover : UiTheme.Hover, on ? UiTheme.AccentPressed : UiTheme.Border);
                labels[i].color = on ? UiTheme.Text : UiTheme.Muted;
            }
        }
    }
}
