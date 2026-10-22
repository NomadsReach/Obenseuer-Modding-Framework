using System;
using System.Collections.Generic;
using ObenseuerCore.Ui;
using UnityEngine;

namespace ObenseuerCore
{
    internal sealed class DevMenuState
    {
        private const string ModPrefix = "mod:";
        private readonly ModWindow window;
        private readonly RectTransform page;
        private string timeText = "";
        private string scaleText = "1";
        private string moneyText = "";
        private string itemFilter = "";
        private string countText = "1";
        private string flagFilter = "";
        private string xText = "";
        private string yText = "";
        private string zText = "";
        private GameObject teleportPoint;
        private readonly DevMenuWorld world;

        internal DevMenuState(ModWindow window, RectTransform page)
        {
            this.window = window;
            this.page = page;
            world = new DevMenuWorld(page, Later);
        }

        internal void Build()
        {
            for (int i = page.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(page.GetChild(i).gameObject);
            ModUi.Text(page, "These edits apply to the live save. Load a scratch save first.", true);
            Card("Time", TimeCard);
            Card("Money", MoneyCard);
            Card("Items", ItemsCard);
            Card("Global flags", FlagsCard);
            Card("Teleport", TeleportCard);
            Card("World", world.Build);
        }

        private void Card(string title, Action build)
        {
            int first = page.childCount;
            try
            {
                build();
            }
            catch (Exception e)
            {
                ApiLog.Once(DevMenu.Owner, "dev menu state card " + title, e);
                for (int i = page.childCount - 1; i >= first; i--) UnityEngine.Object.DestroyImmediate(page.GetChild(i).gameObject);
                ModUi.Text(page, title + ": error, see log.", true);
            }
        }

        private void Later()
        {
            window.Defer(Build);
        }

        private void TimeCard()
        {
            RectTransform card = ModUi.Card(page, "Time");
            ModUi.Text(card, "Now: day " + GameTime.Day + ", " + GameTime.Hour.ToString("00") + ":" + GameTime.Minute.ToString("00") + ", time scale " + DevMenuRules.Number(TimeControl.TimeScale) + " of " + DevMenuRules.Number(TimeControl.MinScale) + " to " + DevMenuRules.Number(TimeControl.MaxScale) + ", 1 is normal. The framework refuses a time earlier than the current one, so the menu cannot go back in time, and the clock forgets a time scale when a save loads.", true);
            ModUi.TextField(card, "Set time (HH:MM)", timeText, text => timeText = text);
            ModUi.Button(card, "Set time", ButtonStyle.Secondary, () => Perform("Set time", timeText, () => SetTime(timeText)));
            ModUi.TextField(card, "Time scale", scaleText, text => scaleText = text);
            ModUi.Button(card, "Set time scale", ButtonStyle.Secondary, () => Perform("Set time scale", scaleText, () => SetTimeScale(scaleText)));
            ModUi.Button(card, "Clear time scale", ButtonStyle.Secondary, () => Perform("Clear time scale", ModIds.CoreId, () => TimeControl.ClearTimeScale(ModIds.CoreId)));
            ModUi.Button(card, "Fire HourPassed", ButtonStyle.Secondary, () => Perform("Fire HourPassed", "hour " + GameTime.Hour, FireHourPassed));
            ModUi.Button(card, "Fire DayPassed", ButtonStyle.Secondary, () => Perform("Fire DayPassed", "day " + GameTime.Day, FireDayPassed));
        }

        private void MoneyCard()
        {
            RectTransform card = ModUi.Card(page, "Money");
            ModUi.Text(card, "Balance: " + Wallet.Balance + " OC.", true);
            ModUi.TextField(card, "Amount (OC)", moneyText, text => moneyText = text);
            ModUi.Button(card, "Give money", ButtonStyle.Secondary, () => Perform("Give money", moneyText, () => GiveMoney(moneyText)));
        }

        private void ItemsCard()
        {
            RectTransform card = ModUi.Card(page, "Items");
            ModUi.TextField(card, "Item id, name or mod:<id> (press Enter)", itemFilter, text =>
            {
                itemFilter = text;
                Later();
            }, true);
            ModUi.TextField(card, "Count", countText, text => countText = text);
            List<ItemDef> matches = ItemMatches(itemFilter);
            ModUi.Text(card, matches.Count == 0 ? "No item matches." : "Showing " + Math.Min(matches.Count, DevMenuRules.MaxItemRows) + " of " + matches.Count + " items.", true);
            for (int i = 0; i < matches.Count && i < DevMenuRules.MaxItemRows; i++)
            {
                ItemDef item = matches[i];
                int id = item.Id;
                ModUi.Button(card, "Give " + item.DisplayName + " (id " + id + ")", ButtonStyle.Secondary, () => DevMenu.Act("Give item", id + " x" + countText, () => GiveItem(id, countText)));
            }
        }

        private void FlagsCard()
        {
            RectTransform card = ModUi.Card(page, "Global flags");
            ModUi.TextField(card, "Filter by key (press Enter)", flagFilter, text =>
            {
                flagFilter = text;
                Later();
            }, true);
            List<string> keys = GlobalFlags.All();
            keys.Sort(StringComparer.Ordinal);
            List<string> matches = keys.FindAll(flag => DevMenuRules.Matches(flag, flagFilter));
            ModUi.Text(card, "Showing " + Math.Min(matches.Count, DevMenuRules.MaxFlagRows) + " of " + matches.Count + " flags. A key the game has not set reads as off.", true);
            for (int i = 0; i < matches.Count && i < DevMenuRules.MaxFlagRows; i++)
            {
                string key = matches[i];
                ModToggle toggle = null;
                toggle = ModUi.Toggle(card, key, GlobalFlags.Get(key), on =>
                {
                    if (!DevMenu.Act("Set flag", key + " to " + on, () => GlobalFlags.Set(key, on))) toggle.Set(GlobalFlags.Get(key));
                });
            }
        }

        private void TeleportCard()
        {
            RectTransform card = ModUi.Card(page, "Teleport");
            FillPoint();
            ModUi.TextField(card, "X", xText, text => xText = text);
            ModUi.TextField(card, "Y", yText, text => yText = text);
            ModUi.TextField(card, "Z", zText, text => zText = text);
            ModUi.Button(card, "Go to coordinates", ButtonStyle.Secondary, () => Perform("Teleport to", xText + " " + yText + " " + zText, GoToPoint));
            List<string> names = Travel.Entrypoints();
            if (names.Count == 0) ModUi.Text(card, "No entrypoint is loaded in this scene.", true);
            foreach (string name in names)
            {
                string target = name;
                ModUi.Button(card, "Go to " + target, ButtonStyle.Secondary, () => DevMenu.Act("Teleport to", target, () => Travel.TeleportTo(target)));
            }
        }

        private void Perform(string action, string target, Func<bool> call)
        {
            DevMenu.Act(action, target, call);
            Later();
        }

        private static bool SetTime(string text)
        {
            int hour;
            int minute;
            return DevMenuRules.ParseTime(text, out hour, out minute) && WorldClock.SetTimeOfDay(hour, minute);
        }

        private static bool SetTimeScale(string text)
        {
            float scale;
            return DevMenuRules.ParseTimeScale(text, out scale) && TimeControl.SetTimeScale(ModIds.CoreId, scale);
        }

        private static bool FireHourPassed()
        {
            if (!WorldClock.IsAvailable) return false;
            Events.RaiseHourPassed(WorldClock.Now.Hour);
            return true;
        }

        private static bool FireDayPassed()
        {
            if (!WorldClock.IsAvailable) return false;
            Events.RaiseDayPassed(WorldClock.Now.Day);
            return true;
        }

        private bool GoToPoint()
        {
            Vector3 point;
            if (!DevMenuRules.ParseVector(xText, yText, zText, out point)) return false;
            if (teleportPoint == null) teleportPoint = new GameObject("ObenseuerCore dev menu teleport point");
            teleportPoint.transform.position = point;
            return PlayerState.Teleport(teleportPoint.transform);
        }

        private void FillPoint()
        {
            if (!PlayerState.IsAvailable || xText.Length > 0 || yText.Length > 0 || zText.Length > 0) return;
            Vector3 point = PlayerState.Position;
            xText = DevMenuRules.Number(point.x);
            yText = DevMenuRules.Number(point.y);
            zText = DevMenuRules.Number(point.z);
        }

        private static bool GiveMoney(string text)
        {
            int amount;
            return DevMenuRules.ParseInt(text, out amount) && Wallet.Give(amount);
        }

        private static bool GiveItem(int id, string countText)
        {
            int count;
            return DevMenuRules.ParseInt(countText, out count) && count > 0 && Items.Get(id) != null && PlayerInventory.Give(id, count);
        }

        private static List<ItemDef> ItemMatches(string text)
        {
            try
            {
                if (string.IsNullOrEmpty(text)) return new List<ItemDef>();
                if (text.StartsWith(ModPrefix, StringComparison.OrdinalIgnoreCase)) return ByMod(text.Substring(ModPrefix.Length).Trim());
                return Search(text);
            }
            catch (Exception e)
            {
                ApiLog.Once(DevMenu.Owner, "item search", e);
                return new List<ItemDef>();
            }
        }

        private static List<ItemDef> ByMod(string mod)
        {
            List<ItemDef> list = new List<ItemDef>();
            if (mod.Length == 0) return list;
            foreach (ItemDef item in Items.All())
            {
                if (ItemRegistry.ModOf(item.Id).StartsWith(mod, StringComparison.OrdinalIgnoreCase)) list.Add(item);
            }
            return list;
        }

        private static List<ItemDef> Search(string text)
        {
            int id;
            if (!DevMenuRules.ParseInt(text, out id)) return Items.Search(text);
            List<ItemDef> one = new List<ItemDef>();
            ItemDef item = Items.Get(id);
            if (item != null) one.Add(item);
            return one;
        }
    }
}
