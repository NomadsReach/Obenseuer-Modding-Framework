using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.Mono;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ObenseuerCore
{
    internal static class NewGameOptionsScreen
    {
        internal const string Owner = "obenseuer.moddingframework.newgamescreen";
        private const string RowPrefix = "ModNewGameOption";
        private const string CoreGuid = "obenseuer.moddingframework";
        private const string SettingSection = "UI";
        private const string SettingKey = "NewGameRowsOnAllDifficulties";
        private static DifficultyUI built;
        private static GameObject added;
        private static int valueIndex = -1;
        private static bool installed;
        private static ConfigEntry<bool> rowsOnAll;

        internal static void Init()
        {
            Bind();
            bool start = Hook("Start", "BuildPostfix");
            bool onEnable = Hook("OnEnable", "BuildPostfix");
            Hook("UpdateDifficulty", "KeepPostfix");
            if (start && onEnable)
            {
                installed = true;
                return;
            }
            CoreLog.For(Owner).Warn("the New Game option rows are off: a DifficultyUI hook did not install");
        }

        private static void Bind()
        {
            try
            {
                PluginInfo info = Loader.Find(CoreGuid);
                BaseUnityPlugin plugin = info == null ? null : info.Instance as BaseUnityPlugin;
                if (plugin == null)
                {
                    ApiLog.Once(Owner, "config setting", "the core plugin was not found, the new game option rows setting is not in the config");
                    return;
                }
                rowsOnAll = plugin.Config.Bind(SettingSection, SettingKey, true, new ConfigDescription("Show mod option rows on every difficulty, turn off if the rows overlap the difficulty description.", (AcceptableValueBase)null, new object[] { new ModMenuTitle("Show mod option rows on every difficulty") }));
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "config setting", e);
            }
        }

        private static bool RowsOnAll()
        {
            if (rowsOnAll == null) Bind();
            return rowsOnAll == null || rowsOnAll.Value;
        }

        private static bool Hook(string name, string handler)
        {
            try
            {
                return CorePatcher.PatchCore(typeof(DifficultyUI), name, Type.EmptyTypes, null, CorePatcher.Handler(typeof(NewGameOptionsScreen), handler), null, false, Owner);
            }
            catch (Exception e)
            {
                CoreLog.For(Owner).Error("DifficultyUI." + name + " hook failed: " + e.ToString());
                return false;
            }
        }

        private static void BuildPostfix(DifficultyUI __instance)
        {
            if (!installed || __instance == null) return;
            if (built == __instance && added != null) return;
            if (NewGameOptions.ScreenOptions().Count == 0) return;
            try
            {
                Build(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "new game options screen", e);
            }
        }

        private static void Build(DifficultyUI ui)
        {
            built = ui;
            MainCharacterMenuDifficultyNavigationHelper helper = Helper();
            if (helper == null)
            {
                ApiLog.Once(Owner, "screen", "the character menu difficulty screen was not found, this framework's new game options are not shown");
                return;
            }
            GameObject panel = ModMenuReflect.Get<GameObject>(helper, "buttonsPanel");
            if (panel == null)
            {
                ApiLog.Once(Owner, "panel", "the character menu difficulty screen has no row panel, this framework's new game options are not shown");
                return;
            }
            Purge(panel);
            OptionsButton template = Template(panel, ui);
            if (template == null)
            {
                ApiLog.Once(Owner, "template", "the character menu difficulty screen has no setting row this framework can copy, this framework's new game options are not shown");
                return;
            }
            List<NewGameOptionInfo> options = NewGameOptions.ScreenOptions();
            if (options.Count == 0) return;
            AddBlock(options, NewGamePlacement.BelowDifficulty, template, panel, ui);
            AddBlock(options, NewGamePlacement.CustomPanel, template, panel, ui);
            RefreshList(helper, panel);
            Keep(ui);
        }

        private static void KeepPostfix(DifficultyUI __instance)
        {
            try
            {
                Keep(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "difficulty rows", e);
            }
        }

        private static void Keep(DifficultyUI ui)
        {
            if (!installed || ui == null || built != ui || added == null) return;
            bool on = RowsOnAll();
            MainCharacterMenuDifficultyNavigationHelper helper = Helper();
            if (helper == null) return;
            GameObject panel = ModMenuReflect.Get<GameObject>(helper, "buttonsPanel");
            GameObject settings = ModMenuReflect.Get<GameObject>(ui, "difficultySettings");
            if (panel == null || settings == null) return;
            if (!panel.transform.IsChildOf(settings.transform))
            {
                ApiLog.Once(Owner, "difficulty rows", "the character menu row panel is not inside the difficulty settings block, this framework's new game option rows stay hidden outside the custom difficulty");
                return;
            }
            DifficultyInfo info = DifficultyController.instance == null ? null : DifficultyController.instance.currentDifficultyInfo;
            bool custom = info != null && info.difficulty == Difficulty.Custom;
            OptionsButton[] rows = settings.GetComponentsInChildren<OptionsButton>(true);
            if (!on)
            {
                foreach (OptionsButton row in rows)
                {
                    if (row == null) continue;
                    if (Own(row.gameObject)) continue;
                    if (row.GetComponent<DifficultySettingDescriptionUI>() == null) continue;
                    if (!row.gameObject.activeSelf) row.gameObject.SetActive(true);
                }
                return;
            }
            if (!settings.activeSelf) settings.SetActive(true);
            foreach (OptionsButton row in rows)
            {
                if (row == null) continue;
                if (Own(row.gameObject)) continue;
                if (row.GetComponent<DifficultySettingDescriptionUI>() == null) continue;
                if (row.gameObject.activeSelf != custom) row.gameObject.SetActive(custom);
            }
            RefreshList(helper, panel);
        }

        private static void Purge(GameObject panel)
        {
            added = null;
            Transform[] found = panel.GetComponentsInChildren<Transform>(true);
            foreach (Transform node in found)
            {
                if (node == panel.transform) continue;
                if (node.name.StartsWith(RowPrefix, StringComparison.Ordinal)) UnityEngine.Object.Destroy(node.gameObject);
            }
        }

        private static void AddBlock(List<NewGameOptionInfo> options, NewGamePlacement placement, OptionsButton template, GameObject panel, DifficultyUI ui)
        {
            foreach (NewGameOptionInfo info in options)
            {
                if (info.Placement != placement) continue;
                Row row = RowOf(info, ui, Count(options, info.ModId) > 1);
                GameObject go = Clone(template, panel, RowPrefix + " " + info.ModId + "." + info.Key);
                if (!Configure(row, go, valueIndex))
                {
                    UnityEngine.Object.Destroy(go);
                    continue;
                }
                if (added == null) added = go;
            }
        }

        private static bool Configure(Row row, GameObject go, int index)
        {
            try
            {
                OptionsButton[] cycles = go.GetComponentsInChildren<OptionsButton>(true);
                if (cycles.Length != 1) return false;
                OptionsButton cycle = cycles[0];
                if (cycle.buttonLeft == null || cycle.buttonRight == null) return false;
                TMP_Text value = ValueText(cycle, index);
                if (value == null) return false;
                cycle.slider = null;
                cycle.Enable();
                Strip(cycle.buttonLeft);
                Strip(cycle.buttonRight);
                Remove(go, typeof(DifficultySettingDescriptionUI));
                row.Cycle = cycle;
                row.ValueText = value;
                if (cycle.text != null) cycle.text.text = row.Label;
                cycle.buttonLeft.onClick.AddListener(delegate { Move(row, false); });
                cycle.buttonRight.onClick.AddListener(delegate { Move(row, true); });
                cycle.SelectionChanged += delegate(bool selected) { Show(row, selected); };
                Render(row);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "option row " + row.ModId + "." + row.Key, e);
                return false;
            }
        }

        private static void Move(Row row, bool right)
        {
            try
            {
                if (row.Kind == NewGameOptionKind.Toggle) row.Flag = !row.Flag;
                else if (row.Kind == NewGameOptionKind.Choice) row.Index = Step(row.Index, right ? 1 : -1, row.Titles.Length);
                else row.Number = Wrap(row.Number + (right ? 1 : -1), row.Min, row.Max);
                NewGameOptions.ScreenChoose(row.ModId, row.Key, Stored(row));
                Render(row);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, row.ModId + "." + row.Key, e);
            }
        }

        private static void Show(Row row, bool selected)
        {
            try
            {
                if (row.Ui == null) return;
                if (!selected || string.IsNullOrEmpty(row.Description))
                {
                    row.Ui.HideSettingDescription();
                    return;
                }
                TextMeshProUGUI title = ModMenuReflect.Get<TextMeshProUGUI>(row.Ui, "settingDescriptionTitle");
                TextMeshProUGUI text = ModMenuReflect.Get<TextMeshProUGUI>(row.Ui, "settingDescriptionText");
                GameObject panel = ModMenuReflect.Get<GameObject>(row.Ui, "settingDescriptionPanel");
                if (title != null) title.text = row.Title;
                if (text != null) text.text = row.Description;
                if (panel != null) panel.SetActive(true);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "option description", e);
            }
        }

        private static void Render(Row row)
        {
            string display = Display(row);
            if (row.ValueText != null)
            {
                row.ValueText.text = display;
                return;
            }
            if (row.Cycle != null && row.Cycle.text != null) row.Cycle.text.text = row.Label + ": " + display;
        }

        private static string Stored(Row row)
        {
            if (row.Kind == NewGameOptionKind.Toggle) return row.Flag ? "1" : "0";
            if (row.Kind == NewGameOptionKind.Choice) return row.Ids[row.Index];
            return row.Number.ToString();
        }

        private static string Display(Row row)
        {
            if (row.Kind == NewGameOptionKind.Toggle) return row.Flag ? "Enabled" : "Disabled";
            if (row.Kind == NewGameOptionKind.Choice) return row.Titles[row.Index];
            if (row.FloatStep > 0f) return (row.FloatMin + row.Number * row.FloatStep).ToString("0.##", CultureInfo.InvariantCulture);
            return row.Number.ToString();
        }

        private static Row RowOf(NewGameOptionInfo info, DifficultyUI ui, bool grouped)
        {
            Row row = new Row();
            row.ModId = info.ModId;
            row.Key = info.Key;
            row.Title = info.Title;
            row.Description = info.Description;
            row.Kind = info.Kind;
            row.Ids = info.Ids;
            row.Titles = info.Titles;
            row.Min = info.Min;
            row.Max = info.Max;
            row.FloatMin = info.FloatMin;
            row.FloatStep = info.FloatStep;
            row.Ui = ui;
            row.Label = grouped ? info.ModId + ": " + info.Title : info.Title;
            string value = NewGameOptions.DraftValue(info.ModId, info.Key);
            if (value == null) value = info.DefaultValue;
            if (info.Kind == NewGameOptionKind.Toggle) row.Flag = value == "1";
            else if (info.Kind == NewGameOptionKind.Choice) row.Index = Choose(info, value);
            else row.Number = Number(info, value);
            return row;
        }

        private static int Choose(NewGameOptionInfo info, string value)
        {
            for (int i = 0; i < info.Ids.Length; i++)
            {
                if (info.Ids[i] == value) return i;
            }
            return info.DefaultIndex;
        }

        private static int Number(NewGameOptionInfo info, string value)
        {
            int number;
            if (!int.TryParse(value, out number)) return info.DefaultInteger;
            return Wrap(number, info.Min, info.Max);
        }

        private static int Wrap(int number, int min, int max)
        {
            if (number < min) return max;
            if (number > max) return min;
            return number;
        }

        private static int Step(int index, int step, int length)
        {
            int next = index + step;
            if (next < 0) return length - 1;
            if (next >= length) return 0;
            return next;
        }

        private static int Count(List<NewGameOptionInfo> options, string modId)
        {
            int count = 0;
            foreach (NewGameOptionInfo info in options)
            {
                if (info.ModId == modId) count++;
            }
            return count;
        }

        private static MainCharacterMenuDifficultyNavigationHelper Helper()
        {
            MainMenuNavigationHandler handler = MainMenuNavigationHandler.Instance;
            if (handler != null)
            {
                MainCharacterMenuDifficultyNavigationHelper known = ModMenuReflect.Get<MainCharacterMenuDifficultyNavigationHelper>(handler, "characterMenuDifficultyNavigationHelper");
                if (known != null) return known;
            }
            List<MainCharacterMenuDifficultyNavigationHelper> found = SceneIds.Active<MainCharacterMenuDifficultyNavigationHelper>(FindObjectsInactive.Include);
            return found.Count == 0 ? null : found[0];
        }

        private static OptionsButton Template(GameObject panel, DifficultyUI ui)
        {
            valueIndex = -1;
            OptionsButton[] found = panel.GetComponentsInChildren<OptionsButton>(true);
            foreach (OptionsButton candidate in found)
            {
                if (Own(candidate.gameObject)) continue;
                int index = ValueIndex(candidate, ui);
                if (index < 0) continue;
                valueIndex = index;
                return candidate;
            }
            return null;
        }

        private static int ValueIndex(OptionsButton template, DifficultyUI ui)
        {
            List<TMP_Text> known = Known(ui);
            if (known.Count == 0) return -1;
            TMP_Text[] texts = template.GetComponentsInChildren<TMP_Text>(true);
            int found = -1;
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] == template.text) continue;
                if (Under(texts[i], template.buttonLeft) || Under(texts[i], template.buttonRight)) continue;
                if (!Holds(known, texts[i])) continue;
                if (found >= 0) return -1;
                found = i;
            }
            return found;
        }

        private static List<TMP_Text> Known(DifficultyUI ui)
        {
            List<TMP_Text> known = new List<TMP_Text>();
            FieldInfo[] fields = ui.GetType().GetFields(ModMenuReflect.Any);
            foreach (FieldInfo field in fields)
            {
                if (!typeof(TMP_Text).IsAssignableFrom(field.FieldType)) continue;
                TMP_Text text = field.GetValue(ui) as TMP_Text;
                if (text != null && !Holds(known, text)) known.Add(text);
            }
            return known;
        }

        private static bool Holds(List<TMP_Text> texts, TMP_Text text)
        {
            foreach (TMP_Text known in texts)
            {
                if (ReferenceEquals(known, text)) return true;
            }
            return false;
        }

        private static bool Under(TMP_Text text, UnityEngine.UI.Button button)
        {
            return button != null && text.transform.IsChildOf(button.transform);
        }

        private static GameObject Clone(OptionsButton template, GameObject panel, string name)
        {
            Transform parent = template.transform.parent;
            GameObject source = parent == null || parent == panel.transform ? template.gameObject : parent.gameObject;
            GameObject go = UnityEngine.Object.Instantiate(source, panel.transform);
            go.name = name;
            go.SetActive(true);
            return go;
        }

        private static TMP_Text ValueText(OptionsButton cycle, int index)
        {
            TMP_Text[] texts = cycle.GetComponentsInChildren<TMP_Text>(true);
            if (index < 0 || index >= texts.Length) return null;
            return texts[index];
        }

        private static void RefreshList(MainCharacterMenuDifficultyNavigationHelper helper, GameObject panel)
        {
            try
            {
                OptionsButton[] found = panel.GetComponentsInChildren<OptionsButton>();
                List<OptionsButton> list = ModMenuReflect.Get<List<OptionsButton>>(helper, "optionButtons");
                if (list != null)
                {
                    list.Clear();
                    list.AddRange(found);
                    return;
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "row navigation list", e);
            }
            if (panel.activeInHierarchy) helper.UpdateList();
        }

        private static bool Own(GameObject go)
        {
            for (Transform node = go.transform; node != null; node = node.parent)
            {
                if (node.name.StartsWith(RowPrefix, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private static void Strip(UnityEngine.UI.Button button)
        {
            if (button == null) return;
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++) button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
            button.onClick.RemoveAllListeners();
        }

        private static void Remove(GameObject go, Type type)
        {
            Component[] found = go.GetComponentsInChildren(type, true);
            foreach (Component component in found) UnityEngine.Object.DestroyImmediate(component);
        }

        private sealed class Row
        {
            internal string ModId;
            internal string Key;
            internal string Title;
            internal string Description;
            internal string Label;
            internal NewGameOptionKind Kind;
            internal string[] Ids;
            internal string[] Titles;
            internal int Min;
            internal int Max;
            internal float FloatMin;
            internal float FloatStep;
            internal bool Flag;
            internal int Index;
            internal int Number;
            internal DifficultyUI Ui;
            internal OptionsButton Cycle;
            internal TMP_Text ValueText;
        }
    }
}
