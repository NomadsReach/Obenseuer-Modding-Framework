using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#pragma warning disable 618

namespace ObenseuerCore
{
    internal static class ModMenuMainMenu
    {
        private const string ButtonName = "Mod:Mods";
        private const string ButtonLabel = "MODS";
        private const string PartnerName = "Mod:Multiplayer";
        private const string MenuCell = "unknown";
        private const float RetryInterval = 1f;
        private static readonly string[] GridFields = { "newGame", "loadGame", "options", "unknown", "credits", "quit" };
        private static MainMenu attached;
        private static UnityEngine.UI.Button modsButton;
        private static bool navLocked;
        private static bool navControls;
        private static bool navigationBroken;
        private static bool missingLogged;
        private static bool failed;
        private static float nextAttempt;

        internal static void Init()
        {
            Patch("GetButtonAbove", "AbovePostfix");
            Patch("GetButtonBelow", "BelowPostfix");
            Patch("GetButtonOnTheLeft", "LeftPostfix");
            Patch("GetButtonOnTheRight", "RightPostfix");
        }

        internal static void Tick()
        {
            try
            {
                if (navLocked) HoldNavigation();
                MainMenu menu = MainMenu.Instance;
                if (menu == null)
                {
                    attached = null;
                    return;
                }
                if (menu == attached && modsButton != null) return;
                if (Time.unscaledTime < nextAttempt) return;
                nextAttempt = Time.unscaledTime + RetryInterval;
                if (!Core.PatchingAllowed || !ModMenuInput.Ready) return;
                Attach(menu);
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        private static void Patch(string name, string postfix)
        {
            MethodInfo method = typeof(ModMenuMainMenu).GetMethod(postfix, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            CorePatcher.PatchCore(typeof(MainMenuNavigationHelper), name, Type.EmptyTypes, null, method, null, false, ModMenu.Owner);
        }

        private static void AbovePostfix(MainMenuNavigationHelper __instance, ref UnityEngine.UI.Button __result)
        {
            try
            {
                if (navigationBroken || modsButton == null || __instance.CurrentObject != modsButton) return;
                UnityEngine.UI.Button cell = GridButton(__instance, MenuCell);
                if (cell != null) __result = cell;
            }
            catch (Exception e)
            {
                BreakNavigation(e);
            }
        }

        private static void BelowPostfix(MainMenuNavigationHelper __instance, ref UnityEngine.UI.Button __result)
        {
            try
            {
                if (navigationBroken || modsButton == null) return;
                UnityEngine.UI.Button cell = GridButton(__instance, MenuCell);
                if (cell != null && __instance.CurrentObject == cell) __result = modsButton;
            }
            catch (Exception e)
            {
                BreakNavigation(e);
            }
        }

        private static void LeftPostfix(MainMenuNavigationHelper __instance, ref UnityEngine.UI.Button __result)
        {
            Side(__instance, ref __result, -1f);
        }

        private static void RightPostfix(MainMenuNavigationHelper __instance, ref UnityEngine.UI.Button __result)
        {
            Side(__instance, ref __result, 1f);
        }

        private static void Side(MainMenuNavigationHelper helper, ref UnityEngine.UI.Button result, float direction)
        {
            try
            {
                if (navigationBroken || modsButton == null) return;
                UnityEngine.UI.Button other = Partner();
                if (other == null) return;
                float toward = Mathf.Sign(other.transform.position.x - modsButton.transform.position.x) * direction;
                if (helper.CurrentObject == modsButton && toward > 0f) result = other;
                else if (helper.CurrentObject == other && toward < 0f) result = modsButton;
            }
            catch (Exception e)
            {
                BreakNavigation(e);
            }
        }

        private static UnityEngine.UI.Button Partner()
        {
            if (modsButton.transform.parent == null) return null;
            Transform found = modsButton.transform.parent.Find(PartnerName);
            return found == null ? null : found.GetComponent<UnityEngine.UI.Button>();
        }

        private static void BreakNavigation(Exception e)
        {
            if (navigationBroken) return;
            navigationBroken = true;
            CoreLog.For(ModMenu.Owner).Error("main menu navigation to MODS disabled: " + e.GetType().Name + ": " + e.Message);
        }

        private static void Attach(MainMenu menu)
        {
            UnityEngine.UI.Button template = FindTemplate(menu);
            if (template == null)
            {
                MainMenuNavigationHelper grid = Grid();
                if (grid != null) template = GridButton(grid, MenuCell);
            }
            if (template == null)
            {
                Missing("Mods button not added: no main menu button to copy");
                return;
            }
            Transform parent = template.transform.parent;
            Transform existing = parent.Find(ButtonName);
            UnityEngine.UI.Button button = existing == null ? CloneButton(template) : existing.GetComponent<UnityEngine.UI.Button>();
            if (button == null)
            {
                Missing("Mods button not added: the template button has no TMP label");
                return;
            }
            modsButton = button;
            button.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + 1);
            PlaceBelowGrid(button.transform);
            attached = menu;
            CoreLog.For(ModMenu.Owner).Info("Mods button added to the main menu");
        }

        private static void Missing(string message)
        {
            if (missingLogged) return;
            missingLogged = true;
            CoreLog.For(ModMenu.Owner).Warn(message);
        }

        private static UnityEngine.UI.Button FindTemplate(MainMenu menu)
        {
            UnityEngine.UI.Button fallback = null;
            UnityEngine.UI.Button[] buttons = menu.GetComponentsInChildren<UnityEngine.UI.Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                for (int p = 0; p < buttons[i].onClick.GetPersistentEventCount(); p++)
                {
                    if (buttons[i].onClick.GetPersistentTarget(p) != menu) continue;
                    if (buttons[i].onClick.GetPersistentMethodName(p) == "LoadGame") return buttons[i];
                    if (fallback == null) fallback = buttons[i];
                }
            }
            return fallback;
        }

        private static UnityEngine.UI.Button CloneButton(UnityEngine.UI.Button template)
        {
            GameObject copy = UnityEngine.Object.Instantiate(template.gameObject, template.transform.parent, false);
            StripLocalization(copy);
            TMP_Text[] texts = copy.GetComponentsInChildren<TMP_Text>(true);
            if (texts.Length == 0)
            {
                UnityEngine.Object.Destroy(copy);
                return null;
            }
            for (int i = 0; i < texts.Length; i++) texts[i].text = ButtonLabel;
            copy.name = ButtonName;
            UnityEngine.UI.Button button = copy.GetComponent<UnityEngine.UI.Button>();
            ModMenuWidgets.Strip(button.onClick);
            button.onClick.AddListener(OpenPanel);
            copy.SetActive(true);
            return button;
        }

        private static void StripLocalization(GameObject root)
        {
            Component[] parts = root.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] != null && parts[i].GetType().FullName.StartsWith("UnityEngine.Localization", StringComparison.Ordinal))
                {
                    UnityEngine.Object.DestroyImmediate(parts[i]);
                }
            }
        }

        private static MainMenuNavigationHelper Grid()
        {
            MainMenuNavigationHelper[] helpers = UnityEngine.Object.FindObjectsByType<MainMenuNavigationHelper>(FindObjectsSortMode.None);
            for (int i = 0; i < helpers.Length; i++)
            {
                if (GridButton(helpers[i], MenuCell) != null) return helpers[i];
            }
            return null;
        }

        private static List<UnityEngine.UI.Button> GridCells(MainMenuNavigationHelper grid)
        {
            List<UnityEngine.UI.Button> cells = new List<UnityEngine.UI.Button>();
            if (grid == null) return cells;
            for (int i = 0; i < GridFields.Length; i++)
            {
                UnityEngine.UI.Button cell = GridButton(grid, GridFields[i]);
                if (cell != null) cells.Add(cell);
            }
            return cells;
        }

        private static UnityEngine.UI.Button GridButton(MainMenuNavigationHelper grid, string field)
        {
            FieldInfo info = grid.GetType().GetField(field, ModMenuReflect.Any);
            return info == null ? null : info.GetValue(grid) as UnityEngine.UI.Button;
        }

        private static void PlaceBelowGrid(Transform self)
        {
            MainMenuNavigationHelper grid = Grid();
            List<UnityEngine.UI.Button> cells = GridCells(grid);
            if (cells.Count == 0) return;
            Transform parent = self.parent;
            List<float> rows = new List<float>();
            for (int i = 0; i < cells.Count; i++)
            {
                float y = parent.InverseTransformPoint(cells[i].transform.position).y;
                bool known = false;
                for (int r = 0; r < rows.Count; r++)
                {
                    if (Mathf.Abs(rows[r] - y) < 0.5f) known = true;
                }
                if (!known) rows.Add(y);
            }
            rows.Sort();
            float step = rows.Count > 1 ? rows[1] - rows[0] : ((RectTransform)self).rect.height;
            UnityEngine.UI.Button unknown = GridButton(grid, MenuCell);
            float x = unknown == null ? 0f : parent.InverseTransformPoint(unknown.transform.position).x;
            self.localPosition = new Vector3(x, rows[0] - step, self.localPosition.z);
        }

        private static void OpenPanel()
        {
            if (ModMenuWindow.IsOpen) return;
            MainMenuNavigationHandler handler = MainMenuNavigationHandler.Instance;
            navControls = handler != null && handler.ControlsEnabled;
            navLocked = true;
            if (handler != null) handler.ControlsEnabled = false;
            ModMenuWindow.Open();
        }

        private static void HoldNavigation()
        {
            MainMenuNavigationHandler handler = MainMenuNavigationHandler.Instance;
            if (ModMenuWindow.IsOpen)
            {
                if (handler != null) handler.ControlsEnabled = false;
                return;
            }
            navLocked = false;
            if (handler != null) handler.ControlsEnabled = navControls;
            if (EventSystem.current == null) return;
            EventSystem.current.SetSelectedGameObject(null);
            if (modsButton != null) EventSystem.current.SetSelectedGameObject(modsButton.gameObject);
        }

        private static void Fail(Exception e)
        {
            if (failed) return;
            failed = true;
            CoreLog.For(ModMenu.Owner).Error("main menu Mods button failed: " + e.GetType().Name + ": " + e.Message);
        }
    }
}
