using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using ObenseuerCore.Ui;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class ConfigTheme
    {
        private const string ConfigurationManagerGuid = "com.bepis.bepinex.configurationmanager";
        private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private const float WindowShare = 0.9f;
        private const float LabelShare = 0.4f;
        private const int ButtonsWidth = 115;
        private const float TipWidth = 400f;
        private const float TipGap = 25f;
        private static bool wanted;
        private static bool searched;
        private static bool broken;
        private static GUISkin skin;
        private static GUISkin saved;
        private static GUIStyle tip;
        private static object manager;
        private static bool resolved;
        private static bool fitted;
        private static int fitWidth;
        private static int fitHeight;
        private static Func<bool> displaying;
        private static Func<Rect> currentRect;
        private static MethodInfo calculate;
        private static PropertyInfo windowRect;
        private static PropertyInfo leftWidth;
        private static PropertyInfo rightWidth;
        private static FieldInfo categoryHeader;
        private static FieldInfo pluginHeader;

        internal static void Init(ConfigFile config)
        {
            wanted = config.Bind("UI", "ThemeConfigManager", true, "Dark theme for the ConfigurationManager (F1) window. Takes effect on the next start.").Value;
        }

        internal static void Update()
        {
            if (!wanted || searched) return;
            PluginInfo info = Loader.Find(ConfigurationManagerGuid);
            if (info == null || info.Instance == null) return;
            searched = true;
            manager = info.Instance;
            resolved = Resolve(manager.GetType());
            if (!resolved) CoreLog.For(ModMenu.Owner).Warn("ConfigurationManager window size and headers left at the vanilla values");
            MethodInfo before = typeof(ConfigTheme).GetMethod("Before", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            MethodInfo after = typeof(ConfigTheme).GetMethod("After", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            bool patched = CorePatcher.PatchCore(manager.GetType(), "OnGUI", Type.EmptyTypes, before, null, after, false, ModMenu.Owner);
            CoreLog.For(ModMenu.Owner).Info(patched ? "ConfigurationManager theme applied" : "ConfigurationManager theme not applied");
            if (resolved) CorePatcher.PatchCore(manager.GetType(), "DrawTooltip", new[] { typeof(Rect) }, typeof(ConfigTheme).GetMethod("Tooltip", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic), null, null, false, ModMenu.Owner);
        }

        private static bool Resolve(Type type)
        {
            try
            {
                Type drawer = type.Assembly.GetType("ConfigurationManager.SettingFieldDrawer");
                PropertyInfo open = type.GetProperty("DisplayingWindow", Members);
                calculate = type.GetMethod("CalculateWindowRect", Members);
                windowRect = type.GetProperty("SettingWindowRect", Members);
                leftWidth = type.GetProperty("LeftColumnWidth", Members);
                rightWidth = type.GetProperty("RightColumnWidth", Members);
                categoryHeader = drawer == null ? null : drawer.GetField("_categoryHeaderSkin", Members);
                pluginHeader = drawer == null ? null : drawer.GetField("_pluginHeaderSkin", Members);
                if (open == null || calculate == null || windowRect == null || leftWidth == null || rightWidth == null || categoryHeader == null || pluginHeader == null) return false;
                displaying = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), manager, open.GetGetMethod(true));
                currentRect = (Func<Rect>)Delegate.CreateDelegate(typeof(Func<Rect>), manager, windowRect.GetGetMethod(true));
                return true;
            }
            catch (Exception e)
            {
                CoreLog.For(ModMenu.Owner).Error("ConfigurationManager members not read: " + e.GetType().Name + ": " + e.Message);
                return false;
            }
        }

        private static void Before()
        {
            if (broken) return;
            try
            {
                if (resolved && !displaying())
                {
                    fitted = false;
                    return;
                }
                saved = GUI.skin;
                GUI.skin = Skin();
                if (resolved) Fit();
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        private static void After()
        {
            if (saved == null) return;
            try
            {
                GUI.skin = saved;
            }
            catch (Exception e)
            {
                Fail(e);
            }
            saved = null;
        }

        private static bool Tooltip()
        {
            if (broken || !resolved) return true;
            try
            {
                string text = GUI.tooltip;
                if (string.IsNullOrEmpty(text)) return false;
                Rect area = currentRect();
                GUIContent content = new GUIContent(text);
                GUIStyle style = Tip();
                float width = Mathf.Min(TipWidth, area.width);
                float height = style.CalcHeight(content, width);
                Vector2 mouse = Event.current.mousePosition;
                float x = Mathf.Clamp(mouse.x, 0f, Mathf.Max(0f, area.width - width));
                float y = mouse.y + TipGap;
                if (y + height > area.height) y = mouse.y - height;
                y = Mathf.Clamp(y, 0f, Mathf.Max(0f, area.height - height));
                GUI.Label(new Rect(x, y, width, height), content, style);
                return false;
            }
            catch (Exception e)
            {
                Fail(e);
                return true;
            }
        }

        private static GUIStyle Tip()
        {
            if (tip != null) return tip;
            tip = new GUIStyle();
            Paint(tip, UiTheme.Hover, UiTheme.Hover);
            tip.wordWrap = true;
            tip.padding = new RectOffset(10, 10, 6, 6);
            return tip;
        }

        private static void Fit()
        {
            if (fitted && Screen.width == fitWidth && Screen.height == fitHeight) return;
            fitted = true;
            fitWidth = Screen.width;
            fitHeight = Screen.height;
            calculate.Invoke(manager, null);
            int width = Mathf.RoundToInt(Screen.width * WindowShare);
            int height = Mathf.RoundToInt(Screen.height * WindowShare);
            int left = Mathf.RoundToInt(width * LabelShare);
            Set(windowRect, new Rect(Mathf.RoundToInt((Screen.width - width) / 2f), Mathf.RoundToInt((Screen.height - height) / 2f), width, height));
            Set(leftWidth, left);
            Set(rightWidth, width - left - ButtonsWidth);
        }

        private static void Set(PropertyInfo property, object value)
        {
            property.GetSetMethod(true).Invoke(manager, new object[] { value });
        }

        private static GUISkin Skin()
        {
            if (skin != null) return skin;
            GUISkin copy = UnityEngine.Object.Instantiate(GUI.skin);
            copy.name = "ObenseuerCoreConfigTheme";
            Color glass = UiTheme.Card;
            glass.a = 0.6f;
            Paint(copy.window, UiTheme.Window, UiTheme.Window);
            Paint(copy.box, glass, glass);
            Paint(copy.label, Color.clear, Color.clear);
            Paint(copy.button, UiTheme.Hover, UiTheme.Border);
            Ink(copy.toggle);
            Paint(copy.textField, UiTheme.Card, UiTheme.Hover);
            Paint(copy.textArea, UiTheme.Card, UiTheme.Hover);
            Paint(copy.horizontalSlider, UiTheme.Card, UiTheme.Card);
            Paint(copy.horizontalSliderThumb, UiTheme.Accent, UiTheme.Accent);
            Paint(copy.verticalSlider, UiTheme.Card, UiTheme.Card);
            Paint(copy.verticalSliderThumb, UiTheme.Accent, UiTheme.Accent);
            Paint(copy.scrollView, UiTheme.Card, UiTheme.Card);
            Paint(copy.verticalScrollbar, UiTheme.Card, UiTheme.Card);
            Paint(copy.verticalScrollbarThumb, UiTheme.Hover, UiTheme.Border);
            Paint(copy.horizontalScrollbar, UiTheme.Card, UiTheme.Card);
            Paint(copy.horizontalScrollbarThumb, UiTheme.Hover, UiTheme.Border);
            if (resolved)
            {
                categoryHeader.SetValue(null, Band(14, UiTheme.Hover, UiTheme.Hover));
                pluginHeader.SetValue(null, Band(15, UiTheme.Card, UiTheme.Hover));
            }
            skin = copy;
            return copy;
        }

        private static GUIStyle Band(int size, Color back, Color over)
        {
            GUIStyle style = new GUIStyle();
            Paint(style, back, over);
            style.fontSize = size;
            style.wordWrap = true;
            style.stretchWidth = true;
            style.padding = new RectOffset(8, 8, 4, 4);
            return style;
        }

        private static void Paint(GUIStyle style, Color back, Color over)
        {
            Texture2D normal = Solid(back);
            Texture2D hover = Solid(over);
            style.normal.background = normal;
            style.focused.background = hover;
            style.hover.background = hover;
            style.active.background = hover;
            style.onNormal.background = normal;
            style.onFocused.background = hover;
            style.onHover.background = hover;
            style.onActive.background = hover;
            Ink(style);
        }

        private static void Ink(GUIStyle style)
        {
            style.normal.textColor = UiTheme.Text;
            style.hover.textColor = UiTheme.Text;
            style.active.textColor = UiTheme.Text;
            style.focused.textColor = UiTheme.Text;
            style.onNormal.textColor = UiTheme.Text;
            style.onHover.textColor = UiTheme.Text;
            style.onActive.textColor = UiTheme.Text;
            style.onFocused.textColor = UiTheme.Text;
        }

        private static Texture2D Solid(Color color)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private static void Fail(Exception e)
        {
            if (broken) return;
            broken = true;
            CoreLog.For(ModMenu.Owner).Error("ConfigurationManager theme stopped: " + e.GetType().Name + ": " + e.Message);
        }
    }
}
