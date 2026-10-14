using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class SignInfo
    {
        public string Id { get; private set; }

        public string Text { get; private set; }

        public int MaxLines { get; private set; }

        public int MaxLength { get; private set; }

        internal SignInfo(string id, string text, int maxLines, int maxLength)
        {
            Id = id;
            Text = text;
            MaxLines = maxLines;
            MaxLength = maxLength;
        }
    }

    public sealed class SignTextChangedArgs
    {
        public string Id { get; private set; }

        public string Text { get; private set; }

        internal SignTextChangedArgs(string id, string text)
        {
            Id = id;
            Text = text;
        }
    }

    public static class Signs
    {
        internal const string Owner = "obenseuer.moddingframework.signs";
        private static bool installed;
        private static EditableSign editedSign;
        private static string editStartText;

        public static event Action<SignTextChangedArgs> TextChanged;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<SignInfo> All()
        {
            List<SignInfo> list = new List<SignInfo>();
            try
            {
                foreach (EditableSign sign in SceneIds.Active<EditableSign>())
                {
                    if (sign != null) list.Add(ToInfo(sign));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Signs.All", e);
            }
            return list;
        }

        public static SignInfo Get(string id)
        {
            try
            {
                EditableSign sign = SceneIds.Find<EditableSign>(id);
                return sign == null ? null : ToInfo(sign);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Signs.Get", e);
                return null;
            }
        }

        public static bool SetText(string id, string text)
        {
            if (text == null) throw new ArgumentException("text must not be null", "text");
            try
            {
                EditableSign sign = SceneIds.Find<EditableSign>(id);
                if (sign == null || SaveGate.Busy || !Accepts(sign, text)) return false;
                string before = sign.text;
                sign.text = text;
                sign.UpdateText();
                if (before != text && HasHandlers) RaiseTextChanged(sign);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Signs.SetText", e);
                return false;
            }
        }

        internal static void Init()
        {
            installed = SignsPatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("sign hooks incomplete, TextChanged for player edits is disabled");
        }

        internal static bool HasHandlers
        {
            get
            {
                return TextChanged != null;
            }
        }

        internal static void BeginEdit(EditableSign sign)
        {
            editedSign = sign;
            editStartText = sign.text;
        }

        internal static void EndEdit(EditableSign sign)
        {
            if (editedSign != sign) return;
            bool changed = sign.text != editStartText;
            editedSign = null;
            editStartText = null;
            if (changed && HasHandlers) RaiseTextChanged(sign);
        }

        private static void RaiseTextChanged(EditableSign sign)
        {
            SignTextChangedArgs args = new SignTextChangedArgs(SceneIds.Of(sign), sign.text);
            Events.Raise(TextChanged, "Signs.TextChanged", h => h(args));
        }

        private static bool Accepts(EditableSign sign, string text)
        {
            if (sign.textComponent == null || sign.textComponent.interactable) return false;
            int limit = sign.textComponent.characterLimit;
            if (limit > 0 && text.Length > limit) return false;
            return text.Split('\n').Length <= Math.Max(sign.maxLines, 1);
        }

        private static SignInfo ToInfo(EditableSign sign)
        {
            int maxLength = sign.textComponent == null ? 0 : sign.textComponent.characterLimit;
            return new SignInfo(SceneIds.Of(sign), sign.text ?? "", sign.maxLines, maxLength);
        }
    }
}
