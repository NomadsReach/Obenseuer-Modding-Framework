using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ObenseuerCore
{
    public static class MainMenuButtons
    {
        private const string Prefix = "Mod:";
        private const string MenuCell = "unknown";
        private const float RetryInterval = 1f;
        private static readonly string[] GridFields = { "newGame", "loadGame", "options", "unknown", "credits", "quit" };
        private static readonly string[] BottomFields = { "unknown", "credits", "quit" };
        private static readonly List<Entry> entries = new List<Entry>();
        private static global::MainMenu lastMenu;
        private static global::MainMenu attachedMenu;
        private static Transform row;
        private static UnityEngine.UI.Button lastPressed;
        private static readonly Dictionary<string, int> holds = new Dictionary<string, int>();
        private static bool heldControls;
        private static bool navigationBroken;
        private static bool missingLogged;
        private static bool failed;
        private static float nextAttempt;
        private static int rowChildren;
        private static int foreignSum;

        public static bool IsOpen
        {
            get { return global::MainMenu.Instance != null && global::MainMenu.Instance == attachedMenu; }
        }

        public static bool Add(string id, string label, Action onClick)
        {
            return Store(string.Empty, id, label, onClick);
        }

        public static bool Add(string modId, string id, string label, Action onClick)
        {
            ModIds.Require(modId, "modId");
            return Store(modId, id, label, onClick);
        }

        public static void SetControlsEnabled(bool enabled)
        {
            SetHold(string.Empty, enabled);
        }

        public static void SetControlsEnabled(string modId, bool enabled)
        {
            ModIds.Require(modId, "modId");
            SetHold(modId, enabled);
        }

        private static bool Store(string modId, string id, string label, Action onClick)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(label) || onClick == null) return false;
            foreach (Entry entry in entries)
            {
                if (entry.Id != id) continue;
                if (entry.ModId != modId || modId.Length == 0) CoreLog.For(ModMenu.Owner).Warn("main menu button " + id + " is already registered by " + Who(entry.ModId) + ", the call from " + Who(modId) + " is ignored");
                return false;
            }
            entries.Add(new Entry(modId, id, label, onClick));
            return true;
        }

        private static string Who(string modId)
        {
            return modId.Length == 0 ? "a mod without an id" : modId;
        }

        internal static void Init()
        {
            bool patched = Patch("GetButtonAbove", "AbovePostfix");
            patched &= Patch("GetButtonBelow", "BelowPostfix");
            patched &= Patch("GetButtonOnTheLeft", "LeftPostfix");
            patched &= Patch("GetButtonOnTheRight", "RightPostfix");
            if (patched) return;
            navigationBroken = true;
            CoreLog.For(ModMenu.Owner).Warn("main menu navigation disabled: a navigation patch did not install");
        }

        internal static void Tick()
        {
            try
            {
                global::MainMenu menu = global::MainMenu.Instance;
                if (menu != lastMenu)
                {
                    lastMenu = menu;
                    Forget();
                }
                if (holds.Count > 0) Hold(MainMenuNavigationHandler.Instance);
                if (menu != null && menu == attachedMenu && row != null && (row.childCount != rowChildren || ForeignSum() != foreignSum)) Layout();
                if (menu == null || (menu == attachedMenu && !Pending())) return;
                if (failed || Time.unscaledTime < nextAttempt) return;
                nextAttempt = Time.unscaledTime + RetryInterval;
                if (!Core.PatchingAllowed) return;
                Attach(menu);
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        private static bool Patch(string name, string postfix)
        {
            MethodInfo method = typeof(MainMenuButtons).GetMethod(postfix, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            return CorePatcher.PatchCore(typeof(MainMenuNavigationHelper), name, Type.EmptyTypes, null, method, null, false, ModMenu.Owner);
        }

        private static void AbovePostfix(MainMenuNavigationHelper __instance, ref UnityEngine.UI.Button __result)
        {
            try
            {
                UnityEngine.UI.Button current = __instance.CurrentObject as UnityEngine.UI.Button;
                if (navigationBroken || current == null || !Row().Contains(current)) return;
                UnityEngine.UI.Button cell = NearestBottomCell(__instance, current);
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
                UnityEngine.UI.Button cell = GridButton(__instance, MenuCell);
                if (navigationBroken || cell == null || __instance.CurrentObject != cell) return;
                List<UnityEngine.UI.Button> buttons = Row();
                if (buttons.Count > 0) __result = buttons[0];
            }
            catch (Exception e)
            {
                BreakNavigation(e);
            }
        }

        private static void LeftPostfix(MainMenuNavigationHelper __instance, ref UnityEngine.UI.Button __result)
        {
            Side(__instance, ref __result, -1);
        }

        private static void RightPostfix(MainMenuNavigationHelper __instance, ref UnityEngine.UI.Button __result)
        {
            Side(__instance, ref __result, 1);
        }

        private static void Side(MainMenuNavigationHelper helper, ref UnityEngine.UI.Button result, int direction)
        {
            try
            {
                UnityEngine.UI.Button current = helper.CurrentObject as UnityEngine.UI.Button;
                if (navigationBroken || current == null) return;
                List<UnityEngine.UI.Button> buttons = Row();
                int index = buttons.IndexOf(current);
                if (index < 0) return;
                index += direction;
                if (index >= 0 && index < buttons.Count) result = buttons[index];
            }
            catch (Exception e)
            {
                BreakNavigation(e);
            }
        }

        private static List<UnityEngine.UI.Button> Row()
        {
            List<UnityEngine.UI.Button> buttons = new List<UnityEngine.UI.Button>();
            if (row == null) return buttons;
            for (int i = 0; i < row.childCount; i++)
            {
                Transform child = row.GetChild(i);
                if (IsRowButton(child) && child.gameObject.activeInHierarchy) buttons.Add(child.GetComponent<UnityEngine.UI.Button>());
            }
            buttons.Sort((a, b) => a.transform.localPosition.x.CompareTo(b.transform.localPosition.x));
            return buttons;
        }

        private static UnityEngine.UI.Button NearestBottomCell(MainMenuNavigationHelper grid, UnityEngine.UI.Button from)
        {
            UnityEngine.UI.Button nearest = null;
            float best = float.MaxValue;
            for (int i = 0; i < BottomFields.Length; i++)
            {
                UnityEngine.UI.Button cell = GridButton(grid, BottomFields[i]);
                if (cell == null) continue;
                float distance = Mathf.Abs(cell.transform.position.x - from.transform.position.x);
                if (distance < best)
                {
                    best = distance;
                    nearest = cell;
                }
            }
            return nearest;
        }

        private static void BreakNavigation(Exception e)
        {
            if (navigationBroken) return;
            navigationBroken = true;
            CoreLog.For(ModMenu.Owner).Error("main menu navigation disabled: " + e.GetType().Name + ": " + e.Message);
        }

        private static bool Pending()
        {
            foreach (Entry entry in entries)
            {
                if (entry.Button == null) return true;
            }
            return false;
        }

        private static void Attach(global::MainMenu menu)
        {
            if (!Pending())
            {
                attachedMenu = menu;
                return;
            }
            UnityEngine.UI.Button template = FindTemplate(menu);
            if (template == null)
            {
                MainMenuNavigationHelper grid = Grid();
                if (grid != null) template = GridButton(grid, MenuCell);
            }
            if (template == null)
            {
                Missing("main menu buttons not added: no main menu button to copy");
                return;
            }
            row = template.transform.parent;
            foreach (Entry entry in entries)
            {
                if (entry.Button != null) continue;
                entry.Button = CloneButton(template, entry);
                if (entry.Button == null)
                {
                    Missing("main menu button " + entry.Id + " not added: the template button has no TMP label");
                    continue;
                }
                entry.Button.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + 1);
                CoreLog.For(ModMenu.Owner).Info("main menu button " + entry.Id + " added");
            }
            Layout();
            attachedMenu = menu;
        }

        private static void Missing(string message)
        {
            if (missingLogged) return;
            missingLogged = true;
            CoreLog.For(ModMenu.Owner).Warn(message);
        }

        private static UnityEngine.UI.Button FindTemplate(global::MainMenu menu)
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

        private static UnityEngine.UI.Button CloneButton(UnityEngine.UI.Button template, Entry entry)
        {
            GameObject copy = UnityEngine.Object.Instantiate(template.gameObject, template.transform.parent, false);
            StripLocalization(copy);
            TMP_Text[] texts = copy.GetComponentsInChildren<TMP_Text>(true);
            if (texts.Length == 0)
            {
                UnityEngine.Object.Destroy(copy);
                return null;
            }
            for (int i = 0; i < texts.Length; i++) texts[i].text = entry.Label;
            copy.name = Prefix + entry.Id;
            UnityEngine.UI.Button button = copy.GetComponent<UnityEngine.UI.Button>();
            button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            button.onClick.AddListener(() => Press(entry));
            copy.SetActive(true);
            return button;
        }

        private static void Press(Entry entry)
        {
            lastPressed = entry.Button;
            try
            {
                entry.OnClick();
            }
            catch (Exception e)
            {
                CoreLog.For(ModMenu.Owner).Error("main menu button " + entry.Id + " failed: " + e.GetType().Name + ": " + e.Message);
            }
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

        private static void Layout()
        {
            MainMenuNavigationHelper grid = Grid();
            List<UnityEngine.UI.Button> cells = GridCells(grid);
            rowChildren = row.childCount;
            foreignSum = ForeignSum();
            if (cells.Count == 0) return;
            List<float> rows = new List<float>();
            List<float> columns = new List<float>();
            for (int i = 0; i < cells.Count; i++)
            {
                Vector3 p = row.InverseTransformPoint(cells[i].transform.position);
                AddDistinct(rows, p.y);
                AddDistinct(columns, p.x);
            }
            rows.Sort();
            columns.Sort();
            RectTransform cell = (RectTransform)cells[0].transform;
            float step = rows.Count > 1 ? rows[1] - rows[0] : cell.rect.height;
            float column = columns.Count > 1 ? columns[1] - columns[0] : cell.rect.width;
            UnityEngine.UI.Button unknown = GridButton(grid, MenuCell);
            float x = unknown == null ? 0f : row.InverseTransformPoint(unknown.transform.position).x;
            List<float> taken = ForeignColumns();
            for (int i = 0; i < entries.Count; i++)
            {
                UnityEngine.UI.Button button = entries[i].Button;
                if (button == null) continue;
                float slot = FreeColumn(x, column, taken);
                taken.Add(slot);
                button.transform.localPosition = new Vector3(slot, rows[0] - step, button.transform.localPosition.z);
            }
        }

        private static List<float> ForeignColumns()
        {
            List<float> columns = new List<float>();
            for (int i = 0; i < row.childCount; i++)
            {
                Transform child = row.GetChild(i);
                if (IsRowButton(child) && !Owned(child)) columns.Add(child.localPosition.x);
            }
            return columns;
        }

        private static int ForeignSum()
        {
            int sum = 0;
            for (int i = 0; i < row.childCount; i++)
            {
                Transform child = row.GetChild(i);
                if (IsRowButton(child) && !Owned(child)) sum += Mathf.RoundToInt(child.localPosition.x);
            }
            return sum;
        }

        private static bool Owned(Transform item)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Button != null && entries[i].Button.transform == item) return true;
            }
            return false;
        }

        private static float FreeColumn(float x, float column, List<float> taken)
        {
            float slot = x;
            for (int k = 0; k < taken.Count && Overlaps(slot, column, taken); k++) slot = x + (k + 1) * column;
            return slot;
        }

        private static bool Overlaps(float slot, float column, List<float> taken)
        {
            for (int i = 0; i < taken.Count; i++)
            {
                if (Mathf.Abs(taken[i] - slot) < column * 0.5f) return true;
            }
            return false;
        }

        private static bool IsRowButton(Transform item)
        {
            return item.name.StartsWith(Prefix, StringComparison.Ordinal) && item.GetComponent<UnityEngine.UI.Button>() != null;
        }

        private static void AddDistinct(List<float> values, float value)
        {
            for (int i = 0; i < values.Count; i++)
            {
                if (Mathf.Abs(values[i] - value) < 0.5f) return;
            }
            values.Add(value);
        }

        private static void SetHold(string owner, bool enabled)
        {
            MainMenuNavigationHandler handler = MainMenuNavigationHandler.Instance;
            int count;
            holds.TryGetValue(owner, out count);
            if (!enabled)
            {
                if (holds.Count == 0) heldControls = handler == null || handler.ControlsEnabled;
                holds[owner] = count + 1;
                Hold(handler);
                return;
            }
            if (count == 0) return;
            if (count > 1)
            {
                holds[owner] = count - 1;
                return;
            }
            holds.Remove(owner);
            if (holds.Count > 0) return;
            if (handler != null) handler.ControlsEnabled = heldControls;
            Reselect();
        }

        private static void Hold(MainMenuNavigationHandler handler)
        {
            if (handler != null) handler.ControlsEnabled = false;
        }

        private static void Reselect()
        {
            if (lastPressed == null || EventSystem.current == null) return;
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(lastPressed.gameObject);
        }

        private static void Forget()
        {
            for (int i = 0; i < entries.Count; i++) entries[i].Button = null;
            attachedMenu = null;
            row = null;
            lastPressed = null;
            holds.Clear();
            failed = false;
            nextAttempt = 0f;
        }

        private static void Fail(Exception e)
        {
            if (failed) return;
            failed = true;
            CoreLog.For(ModMenu.Owner).Error("main menu buttons failed: " + e.GetType().Name + ": " + e.Message);
        }

        private sealed class Entry
        {
            internal readonly string ModId;
            internal readonly string Id;
            internal readonly string Label;
            internal readonly Action OnClick;
            internal UnityEngine.UI.Button Button;

            internal Entry(string modId, string id, string label, Action onClick)
            {
                ModId = modId;
                Id = id;
                Label = label;
                OnClick = onClick;
            }
        }
    }
}
