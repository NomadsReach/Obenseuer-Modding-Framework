using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ObenseuerCore
{
    public enum SaveAutosaveMode
    {
        Default,
        EveryDay,
        EveryHour
    }

    public sealed class SaveSlotInfo
    {
        public string Name { get; private set; }
        public string Character { get; private set; }
        public DateTime LastWritten { get; private set; }

        internal SaveSlotInfo(string name, string character, DateTime lastWritten)
        {
            Name = name;
            Character = character;
            LastWritten = lastWritten;
        }
    }

    public static class SaveSlots
    {
        internal const string Owner = "obenseuer.moddingframework.saveslots";

        private const float InFlightSeconds = 10f;

        private static readonly string[] SlotNames = { "Autosave", "Autosave2", "Slot1", "Slot2", "Slot3", "Slot4", "Slot5", "Slot6", "Slot7", "Slot8", "Slot9", "Slot10" };

        private static float inFlightUntil;

        public static string CurrentName
        {
            get
            {
                return Guard(() => NameOf(CurrentFolder()), null, "CurrentName");
            }
        }

        public static SaveAutosaveMode AutosaveMode
        {
            get
            {
                return Guard(() => AutosaveModeOf(), SaveAutosaveMode.Default, "AutosaveMode");
            }
        }

        public static bool CanSaveNow
        {
            get
            {
                return Guard(() => SaveAllowed(), false, "CanSaveNow");
            }
        }

        public static List<SaveSlotInfo> All()
        {
            return Guard(() => SlotsOf(), new List<SaveSlotInfo>(), "All");
        }

        public static bool SaveNow()
        {
            try
            {
                if (!SaveAllowed()) return false;
                SaveController.Autosave(SaveController.instance);
                inFlightUntil = Time.realtimeSinceStartup + InFlightSeconds;
                return true;
            }
            catch (Exception e)
            {
                CoreLog.For(Owner).Error("SaveNow failed: " + e.ToString());
                return false;
            }
        }

        internal static void Init()
        {
            SaveController.SavingDone += OnSavingDone;
        }

        private static void OnSavingDone()
        {
            inFlightUntil = 0f;
        }

        private static string CurrentFolder()
        {
            string folder = SaveController.LastSaveFolderPath;
            if (string.IsNullOrEmpty(folder) || PlayerIdentity.identity == null) return null;
            string trimmed = folder.TrimEnd('/', '\\');
            string owner = Path.GetFileName(Path.GetDirectoryName(trimmed));
            string expected = SaveController.SanitizeCharacterName(PlayerIdentity.identity.firstName, PlayerIdentity.identity.lastName);
            return string.Equals(owner, expected, StringComparison.OrdinalIgnoreCase) ? folder : null;
        }

        private static bool SaveInFlight()
        {
            if (Time.realtimeSinceStartup < inFlightUntil || SaveGate.Saving) return true;
            return Time.timeScale == 0f && !PauseMenu.instance.PauseIsActive();
        }

        private static string NameOf(string folder)
        {
            if (string.IsNullOrEmpty(folder)) return null;
            string name = Path.GetFileName(folder);
            return string.IsNullOrEmpty(name) ? null : name;
        }

        private static SaveAutosaveMode AutosaveModeOf()
        {
            DifficultyController controller = DifficultyController.instance;
            if (controller == null || controller.currentDifficultyInfo == null) return SaveAutosaveMode.Default;
            return (SaveAutosaveMode)(int)controller.currentDifficultyInfo.autosaveMode;
        }

        private static List<SaveSlotInfo> SlotsOf()
        {
            List<SaveSlotInfo> slots = new List<SaveSlotInfo>();
            foreach (SaveController.CharacterSaves character in SaveController.GetCharacters())
            {
                foreach (string slot in SlotNames)
                {
                    string info = character.FolderName + "/" + slot + "/Info.tnmt";
                    if (File.Exists(info)) slots.Add(new SaveSlotInfo(slot, character.CharacterDisplayName(), File.GetLastWriteTime(info)));
                }
            }
            return slots;
        }

        internal static bool WorldBusy()
        {
            if (SaveGate.Busy) return true;
            if (LoadingScreen.instance != null && (LoadingScreen.instance.Fading || !LoadingScreen.instance.LevelLoadDone)) return true;
            if (Death.instance != null && Death.instance.dead) return true;
            if (WaitingController.instance != null && WaitingController.instance.waitingType != WaitingController.Type.None) return true;
            if (Changelevel.changeLevelDelayActive) return true;
            if (Crime.instance != null && Crime.instance.ArrestInProgress) return true;
            if (SleepEventController.instance != null && SleepEventController.instance.eventInProgress) return true;
            return DialogueController.instance != null && DialogueController.instance.DialogueActive;
        }

        private static bool SaveAllowed()
        {
            SaveController controller = SaveController.instance;
            if (controller == null || !controller.gameObject.activeInHierarchy) return false;
            string folder = CurrentFolder();
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return false;
            if (PlayerIdentity.identity == null || GameController.instance == null || SavingUI.instance == null) return false;
            if (PauseMenu.instance == null || DestructibleList.instance == null) return false;
            if (WorldBusy()) return false;
            if (SaveInFlight()) return false;
            return SaveButtonEnabled();
        }

        private static bool SaveButtonEnabled()
        {
            GameObject button = PauseMenu.instance.saveMenuButton;
            if (button == null) return false;
            UnityEngine.UI.Button ui = button.GetComponent<UnityEngine.UI.Button>();
            return ui != null && ui.interactable;
        }

        private static T Guard<T>(Func<T> read, T fallback, string name)
        {
            try
            {
                return read();
            }
            catch (Exception e)
            {
                CoreLog.For(Owner).Debug(name, name + " failed: " + e.ToString());
                return fallback;
            }
        }
    }
}
