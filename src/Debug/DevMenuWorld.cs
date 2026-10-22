using System;
using System.Collections.Generic;
using ObenseuerCore.Ui;
using UnityEngine;

namespace ObenseuerCore
{
    internal sealed class DevMenuWorld
    {
        private readonly RectTransform page;
        private readonly Action later;
        private string healText = "10";
        private string dispositionText = "";
        private string npcMod = "";
        private string npcId = "";
        private string npcSource = "";
        private string npcName = "";
        private string statText = "";
        private string skillText = "";
        private GameObject teleportPoint;

        internal DevMenuWorld(RectTransform page, Action later)
        {
            this.page = page;
            this.later = later;
        }

        internal void Build()
        {
            Card("Player", PlayerCard);
            Card("Stats and skills", StatsCard);
            Card("Missing items", MissingCard);
            Card("Relationships", RelationshipCard);
            Card("Mod NPCs", NpcCard);
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
                ApiLog.Once(DevMenu.Owner, "dev menu world card " + title, e);
                for (int i = page.childCount - 1; i >= first; i--) UnityEngine.Object.DestroyImmediate(page.GetChild(i).gameObject);
                ModUi.Text(page, title + ": error, see log.", true);
            }
        }

        private void PlayerCard()
        {
            RectTransform card = ModUi.Card(page, "Player");
            if (!PlayerState.IsAvailable)
            {
                ModUi.Text(card, "No player is loaded.", true);
                return;
            }
            ModUi.Text(card, "Health " + PlayerState.Health + ".", true);
            ModToggle god = null;
            god = ModUi.Toggle(card, "God mode", PlayerState.GodMode, on => SetGodMode(god, on));
            DevMenuNoClip.Build(card);
            ModUi.Text(card, "WASD moves you through walls, hold Left Shift to move faster, and the game's Up and Down actions go up and down. The game sets no default key for those two, so bind them under Key bindings if nothing happens.", true);
            ModUi.TextField(card, "Heal amount", healText, text => healText = text);
            ModUi.Button(card, "Heal", ButtonStyle.Secondary, () => Perform("Heal", healText, () => Heal(healText)));
        }

        private void MissingCard()
        {
            RectTransform card = ModUi.Card(page, "Missing items");
            List<MissingItemInfo> missing = ItemRegistry.MissingItems();
            if (missing.Count == 0)
            {
                ModUi.Text(card, "No item stack is missing. A stack from a mod that is not loaded shows here.", true);
                return;
            }
            ModUi.Text(card, missing.Count + " missing stacks. Removing a mod's stacks deletes them from the inventory and loaded containers.", true);
            List<string> mods = new List<string>();
            foreach (MissingItemInfo item in missing)
            {
                if (item.Mod.Length > 0 && !mods.Contains(item.Mod)) mods.Add(item.Mod);
            }
            foreach (string mod in mods)
            {
                string target = mod;
                ModUi.Button(card, "Remove stacks of " + target, ButtonStyle.Secondary, () => Perform("Remove missing items", target, () => ItemRegistry.RemoveMissingItems(target)));
            }
            for (int i = 0; i < missing.Count && i < DevMenuRules.MaxItemRows; i++)
            {
                MissingItemInfo item = missing[i];
                ModUi.Text(card, item.Title + " (id " + item.Id + ", " + item.Mod + ") x" + item.Amount + " in " + item.Where, false);
            }
        }

        private void RelationshipCard()
        {
            RectTransform card = ModUi.Card(page, "Relationships");
            if (!Relationships.IsAvailable)
            {
                ModUi.Text(card, "Relationships are not available in this build.", true);
                return;
            }
            List<RelationshipValue> all = Relationships.All();
            if (all.Count == 0)
            {
                ModUi.Text(card, "No relationship is loaded yet.", true);
                return;
            }
            ModUi.TextField(card, "Disposition (number)", dispositionText, text => dispositionText = text);
            ModUi.Text(card, "Setting a disposition also sets its target, so it does not drift back.", true);
            for (int i = 0; i < all.Count && i < DevMenuRules.MaxItemRows; i++)
            {
                RelationshipValue value = all[i];
                string id = value.Id;
                ModUi.Button(card, "Set " + value.Name + " (now " + value.Disposition.ToString("0.##") + ")", ButtonStyle.Secondary, () => Perform("Set disposition", id, () => SetDisposition(id, dispositionText)));
            }
        }

        private void StatsCard()
        {
            RectTransform card = ModUi.Card(page, "Stats and skills");
            if (!PlayerState.IsAvailable)
            {
                ModUi.Text(card, "No player is loaded.", true);
                return;
            }
            Stat[] stats = (Stat[])Enum.GetValues(typeof(Stat));
            ModUi.TextField(card, "Stat value (number)", statText, text => statText = text);
            ModUi.Text(card, "The framework keeps a stat inside its own range, so a value outside it lands on the nearest end.", true);
            for (int i = 0; i < stats.Length && i < DevMenuRules.MaxFlagRows; i++)
            {
                Stat stat = stats[i];
                string name = stat.ToString();
                ModUi.Button(card, "Set " + name + " (now " + DevMenuRules.Number(Survival.Get(stat)) + ")", ButtonStyle.Secondary, () => Perform("Set stat", name, () => SetStat(stat, statText)));
            }
            List<SkillValue> skills = Skills.All();
            ModUi.TextField(card, "Skill level (number)", skillText, text => skillText = text);
            ModUi.Text(card, "A skill runs from 0 to " + Skills.MaxLevel + ". Setting one lower works too, and a level outside that range lands on the nearest end. Only a skill already at the top turns a higher level down.", true);
            for (int i = 0; i < skills.Count && i < DevMenuRules.MaxFlagRows; i++)
            {
                SkillValue value = skills[i];
                Skill skill = value.Skill;
                string name = skill.ToString();
                ModUi.Button(card, "Set " + name + " (now " + DevMenuRules.Number(value.Value) + ")", ButtonStyle.Secondary, () => Perform("Set skill", name, () => SetSkill(skill, skillText)));
            }
        }

        private void NpcCard()
        {
            RectTransform card = ModUi.Card(page, "Mod NPCs");
            ModUi.TextField(card, "Mod id (press Enter)", npcMod, text =>
            {
                npcMod = text;
                later();
            }, true);
            if (!ModIds.IsValid(npcMod))
            {
                ModUi.Text(card, "Type the id of the mod that spawned the NPCs, for example example.devtest.", true);
                return;
            }
            string mod = npcMod;
            List<ModNpcInfo> npcs = ModNpcs.List(mod);
            ModUi.Text(card, npcs.Count == 0 ? "No NPC of " + mod + " is loaded in this scene." : npcs.Count + " NPCs of " + mod + " are loaded in this scene.", true);
            bool canGo = PlayerState.IsAvailable;
            if (!canGo) ModUi.Text(card, "Go to NPC moves the player to the NPC, so it is skipped while no player is loaded.", true);
            for (int i = 0; i < npcs.Count && i < DevMenuRules.MaxItemRows; i++)
            {
                ModNpcInfo npc = npcs[i];
                string id = npc.Id;
                ModUi.Button(card, "Despawn " + npc.DisplayName + " (" + id + ")", ButtonStyle.Secondary, () => Perform("Despawn NPC", id, () => ModNpcs.Despawn(mod, id)));
                ModUi.Button(card, "Move " + npc.DisplayName + " to me", ButtonStyle.Secondary, () => Perform("Move NPC", id, () => MoveToPlayer(mod, id)));
                if (canGo) ModUi.Button(card, "Go to " + npc.DisplayName, ButtonStyle.Secondary, () => Perform("Teleport to", id, () => GoToNpc(mod, id)));
            }
            ModUi.TextField(card, "NPC id", npcId, text => npcId = text);
            ModUi.TextField(card, "Source character id", npcSource, text => npcSource = text);
            ModUi.TextField(card, "Name", npcName, text => npcName = text);
            ModUi.Button(card, "Spawn at my position", ButtonStyle.Secondary, () => Perform("Spawn NPC", npcId, () => SpawnNpc(mod, npcId, npcSource, npcName)));
        }

        private static void SetGodMode(ModToggle toggle, bool on)
        {
            if (!DevMenu.Act("God mode", on ? "on" : "off", () => PlayerState.SetGodMode(on))) toggle.Set(PlayerState.GodMode);
        }

        private void Perform(string action, string target, Func<bool> call)
        {
            DevMenu.Act(action, target, call);
            later();
        }

        private static bool Heal(string text)
        {
            int amount;
            return DevMenuRules.ParseInt(text, out amount) && PlayerState.Heal(amount);
        }

        private static bool SetDisposition(string id, string text)
        {
            float value;
            return DevMenuRules.ParseFloat(text, out value) && Relationships.SetDisposition(id, value);
        }

        private static bool MoveToPlayer(string mod, string id)
        {
            return PlayerState.IsAvailable && ModNpcs.MoveTo(mod, id, PlayerState.Position);
        }

        private bool GoToNpc(string mod, string id)
        {
            if (!PlayerState.IsAvailable) return false;
            ModNpcInfo npc = ModNpcs.List(mod).Find(entry => entry.Id == id);
            if (npc == null) return false;
            if (teleportPoint == null) teleportPoint = new GameObject("ObenseuerCore dev menu NPC point");
            teleportPoint.transform.position = npc.Position;
            return PlayerState.Teleport(teleportPoint.transform);
        }

        private static bool SetStat(Stat stat, string text)
        {
            float value;
            return DevMenuRules.ParseFloat(text, out value) && Survival.Set(stat, value);
        }

        private static bool SetSkill(Skill skill, string text)
        {
            float value;
            return DevMenuRules.ParseFloat(text, out value) && Skills.Add(skill, value - Skills.Get(skill));
        }

        private static bool SpawnNpc(string mod, string id, string source, string name)
        {
            int sourceId;
            if (!PlayerState.IsAvailable || !DevMenuRules.ParseInt(source, out sourceId)) return false;
            return ModNpcs.Spawn(mod, id, sourceId, name, "", PlayerState.Position, 0f) != 0;
        }
    }
}
