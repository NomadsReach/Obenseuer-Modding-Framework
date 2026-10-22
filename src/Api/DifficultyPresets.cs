using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    [Serializable]
    internal sealed class DifficultyPresetChoice
    {
        public string Preset = "";
    }

    public sealed class DifficultyPresetSettings
    {
        internal DifficultyPresetSettings(DifficultySettings basis)
        {
            UserSavesDisabled = basis.UserSavesDisabled;
            MentalHealthDisabled = basis.MentalHealthDisabled;
            MandatoryStartingAddictionsDisabled = basis.MandatoryStartingAddictionsDisabled;
            NeedsSpeed = basis.NeedsSpeed;
            AddictionNeedsSpeed = basis.AddictionNeedsSpeed;
            AddictionRecoverySpeed = basis.AddictionRecoverySpeed;
            RenovationCost = basis.RenovationCost;
            RenovationResourceCosts = basis.RenovationResourceCosts;
            RenovationSpeed = basis.RenovationSpeed;
            RentIncome = basis.RentIncome;
            Prices = basis.Prices;
            RespawningTime = basis.RespawningTime;
            SmvProgressionRate = basis.SmvProgressionRate;
            TraderWealth = basis.TraderWealth;
            StackSize = basis.StackSize;
        }

        public bool UserSavesDisabled { get; set; }
        public bool MentalHealthDisabled { get; set; }
        public bool MandatoryStartingAddictionsDisabled { get; set; }
        public float NeedsSpeed { get; set; }
        public float AddictionNeedsSpeed { get; set; }
        public float AddictionRecoverySpeed { get; set; }
        public float RenovationCost { get; set; }
        public float RenovationResourceCosts { get; set; }
        public float RenovationSpeed { get; set; }
        public float RentIncome { get; set; }
        public float Prices { get; set; }
        public float RespawningTime { get; set; }
        public float SmvProgressionRate { get; set; }
        public float TraderWealth { get; set; }
        public float StackSize { get; set; }
    }

    internal sealed class DifficultyPreset
    {
        internal DifficultyPreset(string modId, string id, string name, string description, DifficultyLevel basedOn, Action<DifficultyPresetSettings> configure)
        {
            ModId = modId;
            Id = id;
            Key = modId + "." + id;
            Name = name ?? "";
            Description = description ?? "";
            BasedOn = basedOn;
            Configure = configure;
        }

        public string ModId { get; private set; }
        public string Id { get; private set; }
        public string Key { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public DifficultyLevel BasedOn { get; private set; }
        internal Action<DifficultyPresetSettings> Configure { get; private set; }
    }

    internal static class DifficultyPresets
    {
        private const string SlotKey = "preset";
        private static readonly Dictionary<string, DifficultyPreset> presets = new Dictionary<string, DifficultyPreset>(StringComparer.Ordinal);
        private static readonly DefaultMultiplier[] gameLevels = GameLevels();
        private static readonly float[] gameValues = GameValues();
        private static readonly int[] gameOrder = DifficultyPresetMap.OrderByValue(gameValues);
        private static readonly float[] gameSteps = GameSteps();
        private static readonly DefaultMultiplier[] gameStepLevels = GameStepLevels();
        private static SaveSlot<DifficultyPresetChoice> choice;
        private static string pending;

        internal static void Init()
        {
            try
            {
                choice = SaveData.Bind<DifficultyPresetChoice>(GameDifficulty.Owner, SlotKey);
            }
            catch (Exception e)
            {
                CoreLog.For(GameDifficulty.Owner).Error("difficulty preset store failed: " + e.ToString());
            }
            Events.SessionReady += OnSession;
            Events.AfterSave += OnAfterSave;
        }

        internal static bool Register(DifficultyPreset preset)
        {
            if (preset == null) return false;
            if (!ModIds.IsValid(preset.ModId) || !ModIds.IsValid(preset.Id) || string.IsNullOrEmpty(preset.Name))
            {
                ApiLog.Once(GameDifficulty.Owner, "preset", "a preset needs a valid modId, an id of 1 to 64 letters, digits, '.', '_' or '-', and a name");
                return false;
            }
            if (preset.BasedOn == DifficultyLevel.Custom || !Enum.IsDefined(typeof(DifficultyLevel), preset.BasedOn))
            {
                ApiLog.Once(GameDifficulty.Owner, "preset level", "a preset is based on one of the game's levels below Custom");
                return false;
            }
            if (presets.ContainsKey(preset.Key))
            {
                ApiLog.Once(GameDifficulty.Owner, "preset " + preset.Key, "a preset with this mod id and id is already registered");
                return false;
            }
            presets.Add(preset.Key, preset);
            return true;
        }

        internal static DifficultyPreset[] Registered()
        {
            DifficultyPreset[] all = new DifficultyPreset[presets.Count];
            presets.Values.CopyTo(all, 0);
            Array.Sort(all, ByKey);
            return all;
        }

        private static int ByKey(DifficultyPreset left, DifficultyPreset right)
        {
            return string.CompareOrdinal(left.Key, right.Key);
        }

        internal static DifficultyInfo Entry(DifficultyPreset preset, DifficultyInfo[] all)
        {
            if (preset == null || all == null) return null;
            int basedOn = DifficultyPresetMap.IndexOf(Levels(all), (int)preset.BasedOn);
            if (basedOn < 0) return null;
            DifficultyInfo info = Copy(all[basedOn]);
            if (preset.Configure != null)
            {
                DifficultyPresetSettings settings = new DifficultyPresetSettings(new DifficultySettings(all[basedOn]));
                preset.Configure(settings);
                Values(info, settings);
                info.difficulty = Difficulty.Custom;
            }
            info.difficultyName = preset.Name;
            info.description = preset.Description;
            return info;
        }

        internal static void Choose(DifficultyPreset preset)
        {
            if (choice == null) return;
            pending = preset == null ? "" : preset.Key;
            choice.Value.Preset = pending;
        }

        private static void OnSession(SessionKind kind)
        {
            if (choice == null) return;
            if (SaveData.Folder != null)
            {
                pending = null;
                return;
            }
            string restored = DifficultyModPresets.Restore(pending, choice.Value.Preset);
            if (!string.Equals(restored, choice.Value.Preset, StringComparison.Ordinal)) choice.Value.Preset = restored;
        }

        private static void OnAfterSave(string folder)
        {
            pending = null;
        }

        internal static bool Set(string key)
        {
            DifficultyPreset preset;
            if (!presets.TryGetValue(key, out preset))
            {
                ApiLog.Once(GameDifficulty.Owner, "preset " + key, "no preset is registered with this mod id and id");
                return false;
            }
            if (choice == null)
            {
                ApiLog.Once(GameDifficulty.Owner, "preset storage", "the framework's store for the chosen preset is unavailable");
                return false;
            }
            if (SaveGate.Busy)
            {
                ApiLog.Once(GameDifficulty.Owner, "preset " + key, "a save is being written, so the difficulty cannot change now");
                return false;
            }
            if (!Apply(preset)) return false;
            choice.Value.Preset = key;
            return true;
        }

        internal static bool Clear(string modId)
        {
            if (choice == null) return false;
            string stored = choice.Value.Preset;
            if (string.IsNullOrEmpty(stored)) return false;
            DifficultyPreset preset;
            if (!presets.TryGetValue(stored, out preset))
            {
                choice.Value.Preset = "";
                return false;
            }
            if (!string.Equals(preset.ModId, modId, StringComparison.Ordinal)) return false;
            choice.Value.Preset = "";
            return true;
        }

        internal static string Active()
        {
            if (choice == null) return null;
            string stored = choice.Value.Preset;
            DifficultyPreset preset;
            if (string.IsNullOrEmpty(stored) || !presets.TryGetValue(stored, out preset)) return null;
            DifficultyController controller = DifficultyController.instance;
            if (controller == null) return null;
            return Holds(preset, controller.currentDifficultyInfo) ? preset.Key : null;
        }

        private static bool Apply(DifficultyPreset preset)
        {
            try
            {
                DifficultyController controller = DifficultyController.instance;
                if (controller == null)
                {
                    ApiLog.Once(GameDifficulty.Owner, "preset " + preset.Key, "the game's difficulty controller is not ready");
                    return false;
                }
                DifficultyInfo[] all = controller.GetDifficulties();
                if (all == null || all.Length == 0)
                {
                    ApiLog.Once(GameDifficulty.Owner, "preset " + preset.Key, "the game has no difficulty levels to choose from");
                    return false;
                }
                int[] levels = Levels(all);
                bool changesValues = preset.Configure != null;
                int basedOn = DifficultyPresetMap.IndexOf(levels, (int)preset.BasedOn);
                int wanted = changesValues ? DifficultyPresetMap.IndexOf(levels, (int)Difficulty.Custom) : basedOn;
                if (wanted < 0 || wanted >= levels.Length)
                {
                    ApiLog.Once(GameDifficulty.Owner, "preset " + preset.Key, changesValues ? "this game has no Custom level to hold a preset's own values" : "the game has no level matching the preset's base");
                    return false;
                }
                if (changesValues && basedOn < 0)
                {
                    ApiLog.Once(GameDifficulty.Owner, "preset " + preset.Key, "the game has no level matching the preset's base");
                    return false;
                }
                DifficultyInfo info = all[wanted];
                if (changesValues)
                {
                    DifficultyPresetSettings settings = new DifficultyPresetSettings(new DifficultySettings(all[basedOn]));
                    preset.Configure(settings);
                    info = Copy(all[basedOn]);
                    Values(info, settings);
                    info.difficulty = Difficulty.Custom;
                    info.difficultyName = preset.Name;
                    info.description = preset.Description;
                }
                controller.ChangeDifficulty((Difficulty)levels[wanted]);
                controller.currentDifficultyInfo = info;
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(GameDifficulty.Owner, "preset " + preset.Key, e);
                return false;
            }
        }

        private static bool Holds(DifficultyPreset preset, DifficultyInfo info)
        {
            if (info == null) return false;
            if (preset.Configure != null) return info.difficulty == Difficulty.Custom && info.difficultyName == preset.Name;
            return (int)info.difficulty == (int)preset.BasedOn;
        }

        private static int[] Levels(DifficultyInfo[] all)
        {
            if (all == null) return new int[0];
            int[] levels = new int[all.Length];
            for (int i = 0; i < all.Length; i++) levels[i] = all[i] == null ? -1 : (int)all[i].difficulty;
            return levels;
        }

        private static DefaultMultiplier[] GameLevels()
        {
            return (DefaultMultiplier[])Enum.GetValues(typeof(DefaultMultiplier));
        }

        private static float[] GameValues()
        {
            float[] all = new float[gameLevels.Length];
            for (int i = 0; i < gameLevels.Length; i++) all[i] = DifficultyInfo.GetMultiplierValue(gameLevels[i]);
            return all;
        }

        private static float[] GameSteps()
        {
            float[] all = new float[gameOrder.Length];
            for (int i = 0; i < gameOrder.Length; i++) all[i] = gameValues[gameOrder[i]];
            return all;
        }

        private static DefaultMultiplier[] GameStepLevels()
        {
            DefaultMultiplier[] all = new DefaultMultiplier[gameOrder.Length];
            for (int i = 0; i < gameOrder.Length; i++) all[i] = gameLevels[gameOrder[i]];
            return all;
        }

        private static DefaultMultiplier Step(float value, DefaultMultiplier fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return fallback;
            int index = DifficultyPresetMap.Nearest(value, gameSteps);
            return index < 0 || index >= gameStepLevels.Length ? fallback : gameStepLevels[index];
        }

        private static DefaultMultiplier PositiveStep(float value, DefaultMultiplier fallback)
        {
            DefaultMultiplier step = Step(value, fallback);
            return DifficultyInfo.GetMultiplierValue(step) > 0f ? step : fallback;
        }

        private static void Values(DifficultyInfo info, DifficultyPresetSettings settings)
        {
            info.userSavesDisabled = settings.UserSavesDisabled;
            info.mentalHealthDisabled = settings.MentalHealthDisabled;
            info.mandatoryStartingAddictionsDisabled = settings.MandatoryStartingAddictionsDisabled;
            info.generalNeedsSpeed = Step(settings.NeedsSpeed, info.generalNeedsSpeed);
            info.addictionNeedsSpeed = Step(settings.AddictionNeedsSpeed, info.addictionNeedsSpeed);
            info.addictionRecoverySpeed = Step(settings.AddictionRecoverySpeed, info.addictionRecoverySpeed);
            info.renovationCost = Step(settings.RenovationCost, info.renovationCost);
            info.renovationResourceCosts = Step(settings.RenovationResourceCosts, info.renovationResourceCosts);
            info.renovationSpeed = PositiveStep(settings.RenovationSpeed, info.renovationSpeed);
            info.rentIncome = Step(settings.RentIncome, info.rentIncome);
            info.prices = PositiveStep(settings.Prices, info.prices);
            info.respawningTime = Step(settings.RespawningTime, info.respawningTime);
            info.smvProgressionRateSpeed = Step(settings.SmvProgressionRate, info.smvProgressionRateSpeed);
            info.traderWealth = Step(settings.TraderWealth, info.traderWealth);
            info.stackSize = Step(settings.StackSize, info.stackSize);
        }

        private static DifficultyInfo Copy(DifficultyInfo source)
        {
            DifficultyInfo copy = new DifficultyInfo();
            copy.difficultyName = source.difficultyName;
            copy.difficulty = source.difficulty;
            copy.description = source.description;
            copy.userSavesDisabled = source.userSavesDisabled;
            copy.autosaveMode = source.autosaveMode;
            copy.mandatoryStartingAddictionsDisabled = source.mandatoryStartingAddictionsDisabled;
            copy.generalNeedsSpeed = source.generalNeedsSpeed;
            copy.addictionNeedsSpeed = source.addictionNeedsSpeed;
            copy.addictionRecoverySpeed = source.addictionRecoverySpeed;
            copy.mentalHealthDisabled = source.mentalHealthDisabled;
            copy.renovationCost = source.renovationCost;
            copy.renovationResourceCosts = source.renovationResourceCosts;
            copy.renovationSpeed = source.renovationSpeed;
            copy.rentIncome = source.rentIncome;
            copy.rentPayments = source.rentPayments;
            copy.furnitureCraftingSpeed = source.furnitureCraftingSpeed;
            copy.itemCraftingSpeed = source.itemCraftingSpeed;
            copy.prices = source.prices;
            copy.respawningTime = source.respawningTime;
            copy.smvProgressionRateSpeed = source.smvProgressionRateSpeed;
            copy.traderWealth = source.traderWealth;
            copy.stackSize = source.stackSize;
            return copy;
        }
    }
}
