using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ObenseuerCore.Ui
{
    public sealed class ModStepper
    {
        private readonly Coalesce<int> changed;
        private readonly TextMeshProUGUI value;
        private readonly Action stepBack;
        private readonly Action stepForward;
        private string[] options = new string[0];
        private int current;

        internal ModStepper(RectTransform row, RectTransform control, UiScope owner, string[] names, int index, Action<int> onChange)
        {
            Rect = row;
            changed = new Coalesce<int>(owner, onChange);
            stepBack = () => Step(-1);
            stepForward = () => Step(1);
            Narrow(ModUi.Button(control, UiTheme.Glyph('\u2039', "<"), ButtonStyle.Secondary, stepBack));
            value = ModUi.Label(control, "", UiTheme.BodySize, UiTheme.Text, TextAlignmentOptions.Center, false);
            LayoutElement share = ModUi.Sizing(value.gameObject);
            share.preferredWidth = 0f;
            share.flexibleWidth = 1f;
            Narrow(ModUi.Button(control, UiTheme.Glyph('\u203A', ">"), ButtonStyle.Secondary, stepForward));
            SetOptions(names, index);
        }

        public RectTransform Rect { get; private set; }

        public void Set(int index)
        {
            if (options.Length == 0) return;
            current = Mathf.Clamp(index, 0, options.Length - 1);
            value.text = options[current];
        }

        public void SetOptions(string[] names, int index)
        {
            options = names ?? new string[0];
            value.text = "";
            Set(index);
        }

        private void Step(int delta)
        {
            int count = options.Length;
            if (count == 0) return;
            current = (current + delta + count) % count;
            value.text = options[current];
            changed.Set(current);
        }

        private static void Narrow(UnityEngine.UI.Button button)
        {
            LayoutElement size = ModUi.Sizing(button.gameObject);
            size.minWidth = 40f;
            size.preferredWidth = 40f;
        }
    }

    public sealed class ModSwatches
    {
        private readonly UiScope owner;
        private readonly Action<int> onChange;
        private readonly GameObject[] rings;

        internal ModSwatches(RectTransform grid, UiScope owner, Color[] colors, int index, Action<int> onChange)
        {
            this.owner = owner;
            this.onChange = onChange;
            rings = new GameObject[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                int slot = i;
                Color color = colors[i];
                RectTransform cell = ModUi.NewRect("Swatch", grid);
                RectTransform ring = ModUi.NewRect("Ring", cell);
                Image ringImage = ModUi.Shape(ring, UiTheme.ControlRadius);
                ringImage.color = UiTheme.Text;
                ringImage.raycastTarget = false;
                ModUi.Place(ring, Vector2.zero, Vector2.one, new Vector2(-3f, -3f), new Vector2(3f, 3f));
                RectTransform chip = ModUi.NewRect("Chip", cell);
                Image chipImage = ModUi.Shape(chip, UiTheme.ControlRadius);
                UnityEngine.UI.Button button = chip.gameObject.AddComponent<UnityEngine.UI.Button>();
                button.targetGraphic = chipImage;
                ModUi.Tint(button, chipImage, color, Color.Lerp(color, Color.white, 0.2f), Color.Lerp(color, Color.black, 0.2f));
                ModUi.Place(chip, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                ModUi.Click(button, () => Pick(slot));
                rings[i] = ring.gameObject;
            }
            Set(index);
        }

        public void Set(int index)
        {
            for (int i = 0; i < rings.Length; i++) rings[i].SetActive(i == index);
        }

        private void Pick(int index)
        {
            Set(index);
            owner.Run(onChange, index);
        }
    }
}
