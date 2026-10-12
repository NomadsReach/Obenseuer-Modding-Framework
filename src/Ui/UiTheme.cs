using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ObenseuerCore.Ui
{
    public static class UiTheme
    {
        internal const float TitleSize = 22f;
        internal const float BodySize = 16f;
        internal const float MutedSize = 14f;
        internal const float RowHeight = 40f;
        internal const float FieldHeight = 52f;
        internal const int FieldPadding = 6;
        internal const int Padding = 16;
        internal const int CardPadding = 12;
        internal const float Gap = 8f;
        internal const float LabelShare = 45f;
        internal const float ControlShare = 55f;
        internal const float ReadoutWidth = 48f;
        internal const float SwitchWidth = 44f;
        internal const float SwitchHeight = 24f;
        internal const float FooterHeight = 56f;
        internal const float ButtonHeight = 40f;
        internal const float MinButtonWidth = 120f;
        internal const float SegmentWidth = 80f;
        internal const float SegmentHeight = 34f;
        internal const float SegmentButtonHeight = 28f;
        internal const int SegmentInset = 3;
        internal const float Margin = 24f;
        internal const float TopInset = 110f;
        internal const float PanelInset = 12f;
        internal const float SidePanelWidth = 440f;
        internal const float CenterWidth = 640f;
        internal const int WindowRadius = 12;
        internal const int CardRadius = 12;
        internal const int ControlRadius = 8;
        internal const int TrackRadius = 3;
        internal const int KnobRadius = 9;

        public static Color Window { get { return Hex(0x121418, 0.97f); } }
        public static Color Card { get { return Hex(0x1B1E24, 1f); } }
        public static Color Control { get { return Hex(0x262A32, 1f); } }
        public static Color Hover { get { return Hex(0x30353F, 1f); } }
        public static Color Border { get { return Hex(0x2E333D, 1f); } }
        public static Color Text { get { return Hex(0xE9EBEF, 1f); } }
        public static Color Muted { get { return Hex(0x9AA1AC, 1f); } }
        public static Color Accent { get { return Hex(0x4C8DFF, 1f); } }
        public static Color AccentHover { get { return Hex(0x6BA2FF, 1f); } }
        public static Color AccentPressed { get { return Hex(0x3A76DA, 1f); } }
        public static Color Clear { get { return new Color(0f, 0f, 0f, 0f); } }
        internal static readonly Color StageTop = Hex(0x2B3038, 1f);
        internal static readonly Color StageBottom = Hex(0x111318, 1f);
        private static TMP_FontAsset font;

        public static TMP_FontAsset Font
        {
            get
            {
                if (font == null) font = FindFont();
                return font;
            }
        }

        internal static string Glyph(char glyph, string fallback)
        {
            return Font != null && Font.HasCharacter(glyph) ? glyph.ToString() : fallback;
        }

        internal static ColorBlock Blocks(Color normal, Color highlighted, Color pressed)
        {
            ColorBlock block = ColorBlock.defaultColorBlock;
            block.normalColor = normal;
            block.highlightedColor = highlighted;
            block.pressedColor = pressed;
            block.selectedColor = highlighted;
            block.disabledColor = new Color(normal.r, normal.g, normal.b, normal.a * 0.5f);
            block.fadeDuration = 0.08f;
            return block;
        }

        private static Color Hex(int rgb, float alpha)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, alpha);
        }

        private static TMP_FontAsset FindFont()
        {
            TMP_Text[] texts = Resources.FindObjectsOfTypeAll<TMP_Text>();
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i].font != null) return texts[i].font;
            }
            return TMP_Settings.defaultFontAsset;
        }
    }
}
