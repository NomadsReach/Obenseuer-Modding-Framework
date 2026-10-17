using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.Mono;
using BepInEx.Unity.Mono.Configuration;
using ObenseuerCore.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ObenseuerCore
{
    internal sealed class ModMenuActionRow
    {
        private const string Prompt = "run again to confirm";
        private const float ConfirmWindow = 8f;
        private readonly ModMenuAction action;
        internal readonly string Search;
        private RectTransform rect;
        private TextMeshProUGUI status;
        private bool attempted;
        private bool armed;
        private float armedAt;

        internal ModMenuActionRow(ModMenuAction action, string plugin)
        {
            this.action = action;
            Search = ((plugin ?? "") + " " + action.Section + " " + action.Title).ToLowerInvariant();
        }

        internal void SetVisible(bool visible)
        {
            if (rect != null) rect.gameObject.SetActive(visible);
        }

        internal void Build(RectTransform content)
        {
            if (attempted) return;
            attempted = true;
            try
            {
                RectTransform control;
                GameObject dot;
                rect = ModUi.Field(content, action.Title, action.Description, false, out control, out dot);
                status = ModUi.Label(control, "", UiTheme.MutedSize, UiTheme.Muted, TextAlignmentOptions.MidlineRight, false);
                LayoutElement share = ModUi.Sizing(status.gameObject);
                share.preferredWidth = 0f;
                share.flexibleWidth = 1f;
                UnityEngine.UI.Button run = ModUi.Button(control, ModMenuActions.ButtonText, ButtonStyle.Secondary, Run);
                ModMenuWindow.Focus(run, ModMenuColumn.Body, action.ModId + "." + action.Name, false);
                ModMenuActionWatch watch = rect.gameObject.AddComponent<ModMenuActionWatch>();
                watch.Row = this;
            }
            catch (Exception e)
            {
                if (rect != null) UnityEngine.Object.DestroyImmediate(rect.gameObject);
                rect = null;
                CoreLog.For(ModMenu.Owner).Warn("menu action " + action.ModId + " " + action.Name + " skipped: " + e.GetType().Name + ": " + e.Message);
            }
        }

        internal static bool RunNow(bool confirm, bool armed, float age)
        {
            return !confirm || (armed && age >= 0f && age <= ConfirmWindow);
        }

        internal static bool Expired(bool armed, float age)
        {
            return armed && age > ConfirmWindow;
        }

        internal void Expire()
        {
            if (status == null || !Expired(armed, Time.unscaledTime - armedAt)) return;
            armed = false;
            status.text = "";
        }

        private void Run()
        {
            Expire();
            if (!RunNow(action.Confirm, armed, Time.unscaledTime - armedAt))
            {
                armed = true;
                armedAt = Time.unscaledTime;
                if (status != null) status.text = Prompt;
                return;
            }
            armed = false;
            string text = ModMenuActions.Run(action);
            if (status != null) status.text = text;
        }
    }

    internal sealed class ModMenuActionWatch : MonoBehaviour
    {
        internal ModMenuActionRow Row;

        private void Update()
        {
            if (Row != null) Row.Expire();
        }
    }
}
