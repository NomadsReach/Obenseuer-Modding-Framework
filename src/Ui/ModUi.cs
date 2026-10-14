using System;
using BepInEx.Configuration;
using BepInEx.Unity.Mono.Configuration;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ObenseuerCore.Ui
{
    public enum ButtonStyle
    {
        Primary,
        Secondary,
        Ghost
    }

    public static class ModUi
    {
        private const int SwatchColumns = 8;
        private const float SwatchCell = 28f;
        private const float SwatchGap = 8f;

        public static TextMeshProUGUI Title(Transform parent, string text)
        {
            TextMeshProUGUI label = Label(parent, text, UiTheme.TitleSize, UiTheme.Text, TextAlignmentOptions.MidlineLeft, false);
            Height(label.gameObject, UiTheme.RowHeight);
            return label;
        }

        public static TextMeshProUGUI Text(Transform parent, string text, bool muted)
        {
            return Label(parent, text, muted ? UiTheme.MutedSize : UiTheme.BodySize, muted ? UiTheme.Muted : UiTheme.Text, TextAlignmentOptions.TopLeft, true);
        }

        public static RectTransform Card(Transform parent, string title)
        {
            RectTransform card = NewRect("Card", parent);
            Surface(card, UiTheme.Card, UiTheme.CardRadius);
            VerticalLayoutGroup stack = Stack(card);
            stack.padding = new RectOffset(UiTheme.CardPadding, UiTheme.CardPadding, UiTheme.CardPadding, UiTheme.CardPadding);
            if (parent.GetComponent<LayoutGroup>() == null) card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            if (!string.IsNullOrEmpty(title)) Height(Label(card, title, UiTheme.BodySize, UiTheme.Muted, TextAlignmentOptions.MidlineLeft, false).gameObject, UiTheme.RowHeight);
            return card;
        }

        public static RectTransform Scroll(Transform parent)
        {
            RectTransform content;
            ScrollPage(parent, out content);
            return content;
        }

        public static UnityEngine.UI.Button Button(Transform parent, string text, ButtonStyle style, Action onClick)
        {
            UiScope owner = UiScope.For(parent);
            RectTransform rect = NewRect("Button", parent);
            Image back = Shape(rect, UiTheme.ControlRadius);
            Color fill;
            Color hover;
            Color press;
            Color ink;
            if (style == ButtonStyle.Primary)
            {
                fill = UiTheme.Accent;
                hover = UiTheme.AccentHover;
                press = UiTheme.AccentPressed;
                ink = UiTheme.Text;
            }
            else if (style == ButtonStyle.Secondary)
            {
                fill = UiTheme.Control;
                hover = UiTheme.Hover;
                press = UiTheme.Border;
                ink = UiTheme.Text;
            }
            else
            {
                fill = UiTheme.Clear;
                hover = UiTheme.Hover;
                press = UiTheme.Control;
                ink = UiTheme.Muted;
            }
            UnityEngine.UI.Button button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = back;
            Tint(button, back, fill, hover, press);
            TextMeshProUGUI label = Label(rect, text, UiTheme.BodySize, ink, TextAlignmentOptions.Center, false);
            Place(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            LayoutElement size = Sizing(rect.gameObject);
            size.minWidth = UiTheme.MinButtonWidth;
            size.preferredWidth = Mathf.Max(UiTheme.MinButtonWidth, label.GetPreferredValues(text).x + UiTheme.Padding * 2);
            size.minHeight = UiTheme.ButtonHeight;
            size.preferredHeight = UiTheme.ButtonHeight;
            Click(button, () => owner.Run(onClick));
            return button;
        }

        public static ModSlider Slider(Transform parent, string text, float min, float max, float value, Action<float> onChange, int decimals)
        {
            UiScope owner = UiScope.For(parent);
            RectTransform control;
            RectTransform row = Field(parent, text, out control);
            RectTransform bar = NewRect("Slider", control);
            UnityEngine.UI.Slider slider = bar.gameObject.AddComponent<UnityEngine.UI.Slider>();
            Height(bar.gameObject, UiTheme.RowHeight);
            Sizing(bar.gameObject).flexibleWidth = 1f;
            Image hit = bar.gameObject.AddComponent<Image>();
            hit.color = UiTheme.Clear;
            RectTransform track = NewRect("Track", bar);
            Shape(track, UiTheme.TrackRadius).color = UiTheme.Control;
            Place(track, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -3f), new Vector2(0f, 3f));
            RectTransform fill = NewRect("Fill", track);
            Image fillImage = Shape(fill, UiTheme.TrackRadius);
            fillImage.color = UiTheme.Accent;
            fillImage.raycastTarget = false;
            Place(fill, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            RectTransform slide = NewRect("Handle Slide Area", bar);
            Place(slide, Vector2.zero, Vector2.one, new Vector2(UiTheme.KnobRadius, 0f), new Vector2(-UiTheme.KnobRadius, 0f));
            RectTransform knob = NewRect("Handle", slide);
            Image knobImage = Shape(knob, UiTheme.KnobRadius);
            knob.anchorMin = new Vector2(0f, 0.5f);
            knob.anchorMax = new Vector2(0f, 0.5f);
            knob.sizeDelta = new Vector2(UiTheme.KnobRadius * 2f, UiTheme.KnobRadius * 2f);
            slider.fillRect = fill;
            slider.handleRect = knob;
            slider.targetGraphic = knobImage;
            Tint(slider, knobImage, UiTheme.Text, UiTheme.Text, UiTheme.Muted);
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = false;
            slider.value = value;
            TextMeshProUGUI readout = Label(control, "", UiTheme.BodySize, UiTheme.Muted, TextAlignmentOptions.MidlineRight, false);
            LayoutElement readoutSize = Sizing(readout.gameObject);
            readoutSize.minWidth = UiTheme.ReadoutWidth;
            readoutSize.preferredWidth = UiTheme.ReadoutWidth;
            return new ModSlider(row, slider, readout, decimals, owner, onChange);
        }

        public static ModToggle Toggle(Transform parent, string text, bool value, Action<bool> onChange)
        {
            UiScope owner = UiScope.For(parent);
            RectTransform control;
            RectTransform row = Field(parent, text, out control);
            control.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleRight;
            RectTransform track = NewRect("Switch", control);
            Image back = Shape(track, UiTheme.ControlRadius);
            UnityEngine.UI.Toggle toggle = track.gameObject.AddComponent<UnityEngine.UI.Toggle>();
            toggle.targetGraphic = back;
            toggle.toggleTransition = UnityEngine.UI.Toggle.ToggleTransition.None;
            TextMeshProUGUI caption = row.GetComponentInChildren<TextMeshProUGUI>();
            caption.raycastTarget = true;
            UnityEngine.UI.Button captionHit = caption.gameObject.AddComponent<UnityEngine.UI.Button>();
            captionHit.targetGraphic = caption;
            captionHit.transition = Selectable.Transition.None;
            Click(captionHit, () => toggle.isOn = !toggle.isOn);
            LayoutElement size = Sizing(track.gameObject);
            size.preferredWidth = UiTheme.SwitchWidth;
            size.minWidth = UiTheme.SwitchWidth;
            size.preferredHeight = UiTheme.SwitchHeight;
            size.minHeight = UiTheme.SwitchHeight;
            RectTransform knob = NewRect("Knob", track);
            Image knobImage = Shape(knob, UiTheme.KnobRadius);
            knobImage.color = UiTheme.Text;
            knobImage.raycastTarget = false;
            knob.anchorMin = new Vector2(0.5f, 0.5f);
            knob.anchorMax = new Vector2(0.5f, 0.5f);
            knob.sizeDelta = new Vector2(UiTheme.KnobRadius * 2f, UiTheme.KnobRadius * 2f);
            return new ModToggle(row, toggle, back, knob, owner, value, onChange);
        }

        public static ModStepper Stepper(Transform parent, string text, string[] options, int index, Action<int> onChange)
        {
            UiScope owner = UiScope.For(parent);
            RectTransform control;
            RectTransform row = Field(parent, text, out control);
            return new ModStepper(row, control, owner, options, index, onChange);
        }

        public static ModTextField TextField(Transform parent, string label, string text, Action<string> onChange)
        {
            return TextField(parent, label, text, onChange, false);
        }

        internal static ModTextField TextField(Transform parent, string label, string text, Action<string> onChange, bool onEnd)
        {
            UiScope owner = UiScope.For(parent);
            RectTransform control;
            RectTransform row = Field(parent, label, out control);
            RectTransform box = NewRect("Input", control);
            Image back = Shape(box, UiTheme.ControlRadius);
            back.color = UiTheme.Control;
            TMP_InputField field = box.gameObject.AddComponent<TMP_InputField>();
            field.targetGraphic = back;
            TextMeshProUGUI view = Label(box, text ?? "", UiTheme.BodySize, UiTheme.Text, TextAlignmentOptions.MidlineLeft, false);
            Place(view.rectTransform, Vector2.zero, Vector2.one, new Vector2(UiTheme.Gap, 0f), new Vector2(-UiTheme.Gap, 0f));
            field.textComponent = view;
            field.text = text ?? "";
            LayoutElement size = Sizing(box.gameObject);
            size.minHeight = UiTheme.ButtonHeight;
            size.preferredHeight = UiTheme.ButtonHeight;
            size.flexibleWidth = 1f;
            ModFieldFocus focus = box.gameObject.AddComponent<ModFieldFocus>();
            focus.Field = field;
            field.onSelect.AddListener(value => focus.Take());
            field.onDeselect.AddListener(value => focus.Drop());
            return new ModTextField(row, field, owner, onChange, onEnd);
        }

        public static ModKeyBinding KeyBinding(Transform parent, string text, KeyboardShortcut value, Action<KeyboardShortcut> onChange)
        {
            UiScope owner = UiScope.For(parent);
            RectTransform control;
            RectTransform row = Field(parent, text, out control);
            UnityEngine.UI.Button button = Button(control, value.ToString(), ButtonStyle.Secondary, null);
            ModKeyBinding binding = new ModKeyBinding(row, button.GetComponentInChildren<TextMeshProUGUI>(), value, owner, onChange);
            KeyCapture capture = button.gameObject.AddComponent<KeyCapture>();
            capture.Binding = binding;
            button.onClick.AddListener(capture.Begin);
            return binding;
        }

        public static ModSwatches Swatches(Transform parent, Color[] colors, int index, Action<int> onChange)
        {
            UiScope owner = UiScope.For(parent);
            RectTransform grid = NewRect("Swatches", parent);
            GridLayoutGroup layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            int columns = Mathf.Max(1, Mathf.Min(SwatchColumns, colors.Length));
            layout.cellSize = new Vector2(SwatchCell, SwatchCell);
            layout.spacing = new Vector2(SwatchGap, SwatchGap);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = columns;
            int rows = (colors.Length + columns - 1) / columns;
            Height(grid.gameObject, Mathf.Max(0f, rows * (SwatchCell + SwatchGap) - SwatchGap));
            return new ModSwatches(grid, owner, colors, index, onChange);
        }

        public static ModTabs Tabs(Transform parent, string[] names, Action<int> onSelect)
        {
            UiScope owner = UiScope.For(parent);
            RectTransform host = NewRect("Tabs", parent);
            VerticalLayoutGroup group = host.gameObject.AddComponent<VerticalLayoutGroup>();
            group.spacing = UiTheme.Gap;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            Sizing(host.gameObject).flexibleHeight = 1f;
            RectTransform bar = Row(host);
            bar.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
            RectTransform pages = NewRect("Pages", host);
            Sizing(pages.gameObject).flexibleHeight = 1f;
            return new ModTabs(owner, bar, pages, names, onSelect);
        }

        public static ModSegmented Segmented(Transform parent, string[] names, int index, Action<int> onSelect)
        {
            UiScope owner = UiScope.For(parent);
            RectTransform host = NewRect("Segmented", parent);
            Surface(host, UiTheme.Control, UiTheme.ControlRadius);
            HorizontalLayoutGroup group = host.gameObject.AddComponent<HorizontalLayoutGroup>();
            group.padding = new RectOffset(UiTheme.SegmentInset, UiTheme.SegmentInset, UiTheme.SegmentInset, UiTheme.SegmentInset);
            group.spacing = UiTheme.SegmentInset;
            group.childAlignment = TextAnchor.MiddleCenter;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = true;
            LayoutElement size = Sizing(host.gameObject);
            size.preferredWidth = names.Length * UiTheme.SegmentWidth;
            size.minHeight = UiTheme.SegmentHeight;
            size.preferredHeight = UiTheme.SegmentHeight;
            return new ModSegmented(owner, host, names, index, onSelect);
        }

        public static ObenseuerCore.Ui.ModelPreview ModelPreview(RectTransform parent, Transform target, PreviewFraming framing)
        {
            UiScope owner = UiScope.For(parent);
            RectTransform stage = NewRect("ModelPreviewStage", parent);
            RawImage backdrop = stage.gameObject.AddComponent<RawImage>();
            backdrop.texture = UiSprites.Stage();
            backdrop.raycastTarget = false;
            Place(stage, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            RectTransform rect = NewRect("ModelPreview", parent);
            rect.gameObject.AddComponent<RawImage>().color = Color.white;
            ObenseuerCore.Ui.ModelPreview view = rect.gameObject.AddComponent<ObenseuerCore.Ui.ModelPreview>();
            Place(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            view.Init(owner, target, framing);
            return view;
        }

        public static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        internal static RectTransform NewRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Stack(Transform parent)
        {
            RectTransform rect = NewRect("Stack", parent);
            Stack(rect);
            return rect;
        }

        internal static VerticalLayoutGroup Stack(RectTransform rect)
        {
            VerticalLayoutGroup group = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            group.padding = new RectOffset(UiTheme.Padding, UiTheme.Padding, UiTheme.Padding, UiTheme.Padding);
            group.spacing = UiTheme.Gap;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            return group;
        }

        public static RectTransform Row(Transform parent)
        {
            RectTransform row = NewRect("Row", parent);
            HorizontalLayoutGroup group = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            group.spacing = UiTheme.Gap;
            group.childAlignment = TextAnchor.MiddleLeft;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;
            Height(row.gameObject, UiTheme.RowHeight);
            return row;
        }

        internal static RectTransform Field(Transform parent, string text, out RectTransform control)
        {
            RectTransform row = NewRect("Field", parent);
            Surface(row, UiTheme.Card, UiTheme.CardRadius);
            HorizontalLayoutGroup group = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            group.padding = new RectOffset(UiTheme.CardPadding, UiTheme.CardPadding, UiTheme.FieldPadding, UiTheme.FieldPadding);
            group.spacing = UiTheme.Gap;
            group.childAlignment = TextAnchor.MiddleLeft;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;
            Height(row.gameObject, UiTheme.FieldHeight);
            LayoutElement labelSize = Sizing(Label(row, text, UiTheme.BodySize, UiTheme.Text, TextAlignmentOptions.MidlineLeft, false).gameObject);
            labelSize.preferredWidth = 0f;
            labelSize.flexibleWidth = UiTheme.LabelShare;
            control = NewRect("Control", row);
            HorizontalLayoutGroup inner = control.gameObject.AddComponent<HorizontalLayoutGroup>();
            inner.spacing = UiTheme.Gap;
            inner.childAlignment = TextAnchor.MiddleLeft;
            inner.childControlWidth = true;
            inner.childControlHeight = true;
            inner.childForceExpandWidth = false;
            inner.childForceExpandHeight = false;
            LayoutElement share = Sizing(control.gameObject);
            share.preferredWidth = 0f;
            share.flexibleWidth = UiTheme.ControlShare;
            return row;
        }

        internal static TextMeshProUGUI Label(Transform parent, string text, float size, Color color, TextAlignmentOptions align, bool wrap)
        {
            GameObject go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
            TMP_FontAsset font = UiTheme.Font;
            if (font != null) label.font = font;
            label.fontSize = size;
            label.color = color;
            label.alignment = align;
            label.richText = false;
            label.raycastTarget = false;
            label.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            label.overflowMode = wrap ? TextOverflowModes.Overflow : TextOverflowModes.Ellipsis;
            label.text = text;
            return label;
        }

        internal static LayoutElement Sizing(GameObject go)
        {
            LayoutElement element = go.GetComponent<LayoutElement>();
            return element != null ? element : go.AddComponent<LayoutElement>();
        }

        internal static void Height(GameObject go, float height)
        {
            LayoutElement element = Sizing(go);
            element.minHeight = height;
            element.preferredHeight = height;
        }

        internal static Image Shape(RectTransform rect, int radius)
        {
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = UiSprites.Rounded(radius);
            image.type = Image.Type.Sliced;
            return image;
        }

        internal static Image Surface(RectTransform rect, Color color, int radius)
        {
            Image image = Shape(rect, radius);
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        internal static void Tint(UnityEngine.UI.Selectable control, Graphic graphic, Color fill, Color hover, Color press)
        {
            control.colors = UiTheme.Blocks(fill, hover, press);
            graphic.color = fill;
        }

        internal static void Click(UnityEngine.UI.Button button, Action action)
        {
            button.onClick.AddListener(() =>
            {
                if (EventSystem.current != null && !SubmitPressed()) EventSystem.current.SetSelectedGameObject(null);
                if (action != null) action();
            });
        }

        private static bool SubmitPressed()
        {
            return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.JoystickButton0);
        }

        internal static RectTransform ScrollPage(Transform parent, out RectTransform content)
        {
            RectTransform view = NewRect("Scroll", parent);
            Place(view, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            view.gameObject.AddComponent<Canvas>();
            view.gameObject.AddComponent<GraphicRaycaster>();
            view.gameObject.AddComponent<RectMask2D>();
            Image surface = view.gameObject.AddComponent<Image>();
            surface.color = UiTheme.Clear;
            Sizing(view.gameObject).flexibleHeight = 1f;
            ScrollRect scroll = view.gameObject.AddComponent<ScrollRect>();
            content = NewRect("Content", view);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            Stack(content);
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;
            scroll.viewport = view;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 25f;
            return view;
        }
    }
}
