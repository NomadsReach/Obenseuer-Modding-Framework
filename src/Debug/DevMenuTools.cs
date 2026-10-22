using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;
using ObenseuerCore.Ui;
using UnityEngine;

namespace ObenseuerCore
{
    internal sealed class DevMenuTools
    {
        private const int MaxLogNames = 20;
        private const int TailBytes = 16384;
        private const int TailLines = 40;
        private readonly ModWindow window;
        private readonly RectTransform page;
        private string result = "";
        private string logFilter = "";
        private string shownLog = "";

        internal DevMenuTools(ModWindow window, RectTransform page)
        {
            this.window = window;
            this.page = page;
        }

        internal void Build()
        {
            for (int i = page.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(page.GetChild(i).gameObject);
            ChecksCard();
            LogCard();
        }

        private void ChecksCard()
        {
            RectTransform card = ModUi.Card(page, "Built-in checks");
            int first = card.childCount;
            try
            {
                ModUi.Text(card, "core_quests_check adds a finished test quest, and core_savedata_check changes the Inventory save entry. Use a scratch save. scenelooks here only reads.", true);
                ModUi.Button(card, "core_quests_check", ButtonStyle.Secondary, () => Run("core_quests_check", QuestAuthoringSelfTest.Play));
                ModUi.Button(card, "core_savedata_check", ButtonStyle.Secondary, () => Run("core_savedata_check", SaveData.SelfCheck));
                ModUi.Button(card, "scenelooks here", ButtonStyle.Secondary, () => Run("scenelooks here", SceneLooksConsole.RunChecks));
                if (result.Length > 0) ModUi.Text(card, result, false);
            }
            catch (Exception e)
            {
                ApiLog.Once(DevMenu.Owner, "dev menu checks card", e);
                for (int i = card.childCount - 1; i >= first; i--) UnityEngine.Object.DestroyImmediate(card.GetChild(i).gameObject);
                ModUi.Text(card, "Error building this card. See the log.", true);
            }
        }

        private void LogCard()
        {
            RectTransform card = ModUi.Card(page, "Logs");
            int first = card.childCount;
            try
            {
                ModUi.Text(card, "Lines dropped by the rate limiter this session: " + CoreLog.Dropped + ". Each drop is counted in the next summary line of its log.", true);
                ModUi.TextField(card, "Mod id (press Enter)", logFilter, text =>
                {
                    logFilter = text;
                    Later();
                }, true);
                List<string> names = LogNames(logFilter);
                if (names.Count == 0) ModUi.Text(card, "No log matches. Logs are in the BepInEx logs folder.", true);
                foreach (string name in names)
                {
                    string target = name;
                    ModUi.Button(card, target + ".log", ButtonStyle.Secondary, () =>
                    {
                        shownLog = target;
                        Later();
                    });
                }
                if (shownLog.Length > 0) ModUi.Text(card, Tail(shownLog), false);
            }
            catch (Exception e)
            {
                ApiLog.Once(DevMenu.Owner, "dev menu logs card", e);
                for (int i = card.childCount - 1; i >= first; i--) UnityEngine.Object.DestroyImmediate(card.GetChild(i).gameObject);
                ModUi.Text(card, "Error building this card. See the log.", true);
            }
        }

        private void Run(string name, Func<string> check)
        {
            try
            {
                result = check();
            }
            catch (Exception e)
            {
                ApiLog.Once(DevMenu.Owner, name, e);
                result = name + " failed: " + e.Message;
            }
            CoreLog.For(DevMenu.Owner).Info("check " + name + ": " + result);
            Later();
        }

        private void Later()
        {
            window.Defer(Build);
        }

        private static string LogDir()
        {
            return Path.Combine(Paths.BepInExRootPath, "logs");
        }

        private static List<string> LogNames(string filter)
        {
            List<string> names = new List<string>();
            string dir = LogDir();
            if (!Directory.Exists(dir)) return names;
            foreach (string file in Directory.GetFiles(dir, "*.log"))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                if (DevMenuRules.Matches(name, filter)) names.Add(name);
            }
            names.Sort(StringComparer.OrdinalIgnoreCase);
            return names.GetRange(0, Math.Min(names.Count, MaxLogNames));
        }

        private static string Tail(string name)
        {
            try
            {
                using (FileStream stream = new FileStream(Path.Combine(LogDir(), name + ".log"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    long start = Math.Max(0, stream.Length - TailBytes);
                    byte[] bytes = new byte[stream.Length - start];
                    stream.Seek(start, SeekOrigin.Begin);
                    int total = 0;
                    while (total < bytes.Length)
                    {
                        int read = stream.Read(bytes, total, bytes.Length - total);
                        if (read <= 0) break;
                        total += read;
                    }
                    string text = Encoding.UTF8.GetString(bytes, 0, total);
                    int cut = text.IndexOf('\n');
                    if (start > 0 && cut >= 0) text = text.Substring(cut + 1);
                    string[] lines = text.Split('\n');
                    int from = Math.Max(0, lines.Length - TailLines);
                    return string.Join("\n", lines, from, lines.Length - from);
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(DevMenu.Owner, "log view", e);
                return name + ".log could not be read: " + e.Message;
            }
        }
    }
}
