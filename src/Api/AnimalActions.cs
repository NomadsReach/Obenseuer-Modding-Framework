using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    public static class AnimalActions
    {
        internal const string Owner = "obenseuer.moddingframework.animalactions";
        private const string SharedGuid = "two live animals in this scene share a save GUID, so their tame choices are neither saved nor restored";
        private static readonly List<HuntRecord> hunts = new List<HuntRecord>();
        private static readonly List<SleepRecord> sleeps = new List<SleepRecord>();
        private static readonly HashSet<string> used = new HashSet<string>();
        private static AnimalTameFile savedTame = new AnimalTameFile();
        private static readonly FieldInfo stateField = typeof(Animal).GetField("currentState", BindingFlags.Instance | BindingFlags.NonPublic);

        public static bool Sit(string modId, int animalId)
        {
            ModIds.Require(modId, "modId");
            Animal animal = Live(animalId);
            if (animal == null || IsSleepHeld(animal)) return false;
            try
            {
                animal.Sit();
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "AnimalActions.Sit", e);
                return false;
            }
        }

        public static bool Sleep(string modId, int animalId)
        {
            ModIds.Require(modId, "modId");
            Animal animal = Live(animalId);
            if (animal == null || IsSleepHeld(animal)) return false;
            try
            {
                animal.Sleep(false);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "AnimalActions.Sleep", e);
                return false;
            }
        }

        public static bool SleepPermanently(string modId, int animalId)
        {
            ModIds.Require(modId, "modId");
            Animal animal = Live(animalId);
            if (animal == null) return false;
            try
            {
                animal.Sleep(true);
                sleeps.RemoveAll(s => s.Animal == null);
                if (!sleeps.Exists(s => s.ModId == modId && s.Animal == animal)) sleeps.Add(new SleepRecord { ModId = modId, Animal = animal });
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "AnimalActions.SleepPermanently", e);
                return false;
            }
        }

        public static bool Hunt(string modId, int animalId, Transform target)
        {
            ModIds.Require(modId, "modId");
            Animal animal = Live(animalId);
            if (animal == null || target == null || IsSleepHeld(animal)) return false;
            try
            {
                Stats stats = target.GetComponentInChildren<Stats>();
                if (stats == null) return false;
                animal.HuntTarget(target);
                if (animal.CurrentTarget() != stats) return false;
                hunts.RemoveAll(h => h.Animal == null);
                hunts.Add(new HuntRecord { ModId = modId, Animal = animal, Target = stats });
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "AnimalActions.Hunt", e);
                return false;
            }
        }

        public static bool StopHunt(string modId, int animalId)
        {
            ModIds.Require(modId, "modId");
            Animal animal = Live(animalId);
            if (animal == null || IsSleepHeld(animal)) return false;
            try
            {
                animal.BreakOffHunt();
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "AnimalActions.StopHunt", e);
                return false;
            }
        }

        public static bool Clear(string modId)
        {
            ModIds.Require(modId, "modId");
            if (SaveGate.Busy && savedTame.Count(modId) > 0) return false;
            List<HuntRecord> mine = hunts.FindAll(h => h.ModId == modId);
            hunts.RemoveAll(h => h.ModId == modId);
            foreach (HuntRecord hunt in mine) BreakOff(hunt);
            List<SleepRecord> asleep = sleeps.FindAll(s => s.ModId == modId);
            sleeps.RemoveAll(s => s.ModId == modId);
            foreach (SleepRecord sleep in asleep) WakeUp(sleep);
            int forgotten = savedTame.Forget(modId);
            return mine.Count > 0 || asleep.Count > 0 || forgotten > 0;
        }

        public static bool Run(string modId, int animalId, float seconds)
        {
            ModIds.Require(modId, "modId");
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds <= 0f) return false;
            Animal animal = Live(animalId);
            if (animal == null || IsSleepHeld(animal)) return false;
            try
            {
                animal.Run(seconds);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "AnimalActions.Run", e);
                return false;
            }
        }

        public static bool SetTame(string modId, int animalId, bool tame)
        {
            ModIds.Require(modId, "modId");
            Animal animal = Live(animalId);
            if (animal == null) return false;
            try
            {
                AnimalStats stats = animal.GetComponent<AnimalStats>();
                if (stats == null || stats.animalData == null) return false;
                used.Add(modId);
                string scene = animal.gameObject.scene.name;
                if (!string.IsNullOrEmpty(stats.GUID))
                {
                    if (Holding(LiveAnimals(), scene, stats.GUID).Count > 1)
                    {
                        ApiLog.Once(Owner, "AnimalActions.shared guid", SharedGuid);
                    }
                    else if (!savedTame.Set(modId, scene, stats.GUID, tame))
                    {
                        ApiLog.Once(Owner, "AnimalActions.SetTame full", "the saved tame choices are full (" + AnimalTameFile.MaxEntries + " animals per mod, " + AnimalTameFile.MaxTotalEntries + " in total), this choice was not set");
                        return false;
                    }
                }
                ApplyTame(animal, stats, tame);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "AnimalActions.SetTame", e);
                return false;
            }
        }

        internal static void Init()
        {
            Events.SceneLoadedWithMode += OnSceneLoaded;
            RegisterTameStore();
        }

        private static void OnSceneLoaded(string scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            List<HuntRecord> ended = new List<HuntRecord>(hunts);
            hunts.Clear();
            foreach (HuntRecord hunt in ended) BreakOff(hunt);
            List<SleepRecord> asleep = new List<SleepRecord>(sleeps);
            sleeps.Clear();
            foreach (SleepRecord sleep in asleep) WakeUp(sleep);
            Events.WhenReady(() => RestoreTame(scene));
        }

        private static void ApplyTame(Animal animal, AnimalStats stats, bool tame)
        {
            animal.ChangeTameStatus(tame);
            stats.animalData.tame = tame;
            if (!tame && AnimalController.instance != null) AnimalController.instance.followingAnimals.Remove(animal);
        }

        private static void RestoreTame(string scene)
        {
            if (savedTame.Entries.Count == 0) return;
            try
            {
                Animal[] live = LiveAnimals();
                foreach (Animal animal in live)
                {
                    AnimalStats stats = animal.GetComponent<AnimalStats>();
                    if (stats == null || stats.animalData == null || string.IsNullOrEmpty(stats.GUID) || animal.gameObject.scene.name != scene) continue;
                    List<AnimalTameEntry> choices = savedTame.For(scene, stats.GUID, m => ModIds.IsInstalled(m, used));
                    if (choices.Count == 0) continue;
                    if (Holding(live, scene, stats.GUID).Count > 1)
                    {
                        ApiLog.Once(Owner, "AnimalActions.shared guid", SharedGuid);
                        continue;
                    }
                    foreach (AnimalTameEntry entry in choices) ApplyTame(animal, stats, entry.Tame);
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "AnimalActions.RestoreTame", e);
            }
        }

        private static void RegisterTameStore()
        {
            try
            {
                SaveData.Register<AnimalTameFile>(Owner, SaveFile, file => savedTame = AnimalTameFile.Clean(file));
            }
            catch (Exception e)
            {
                CoreLog.For(Owner).Error("animal tame data not registered, tame choices last for the session only: " + e);
            }
        }

        private static AnimalTameFile SaveFile()
        {
            try
            {
                Animal[] live = LiveAnimals();
                savedTame.DropStale(m => ModIds.IsInstalled(m, used), entry => LiveTame(live, entry));
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "AnimalActions.SaveFile", e);
            }
            return savedTame;
        }

        private static bool? LiveTame(Animal[] live, AnimalTameEntry entry)
        {
            List<Animal> holders = Holding(live, entry.Scene, entry.Guid);
            return holders.Count == 1 ? (bool?)holders[0].tame : null;
        }

        private static Animal[] LiveAnimals()
        {
            return UnityEngine.Object.FindObjectsByType<Animal>(FindObjectsSortMode.None);
        }

        private static List<Animal> Holding(Animal[] live, string scene, string guid)
        {
            List<Animal> holders = new List<Animal>();
            foreach (Animal animal in live)
            {
                AnimalStats stats = animal.GetComponent<AnimalStats>();
                if (stats != null && stats.GUID == guid && animal.gameObject.scene.name == scene) holders.Add(animal);
            }
            return holders;
        }

        private static void BreakOff(HuntRecord hunt)
        {
            try
            {
                if (hunt.Animal == null || hunt.Animal.CurrentTarget() != hunt.Target || IsHunted(hunt.Animal)) return;
                hunt.Animal.BreakOffHunt();
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "AnimalActions.BreakOff", e);
            }
        }

        private static void WakeUp(SleepRecord sleep)
        {
            try
            {
                if (sleep.Animal == null || !IsAsleep(sleep.Animal) || IsSleepHeld(sleep.Animal)) return;
                sleep.Animal.OnSleepingDone();
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "AnimalActions.WakeUp", e);
            }
        }

        private static bool IsHunted(Animal animal)
        {
            return hunts.Exists(h => h.Animal == animal);
        }

        private static bool IsSleepHeld(Animal animal)
        {
            if (IsAsleep(animal)) return sleeps.Exists(s => s.Animal == animal);
            sleeps.RemoveAll(s => s.Animal == animal);
            return false;
        }

        private static bool IsAsleep(Animal animal)
        {
            if (stateField == null)
            {
                ApiLog.Once(Owner, "AnimalActions.IsAsleep", "Animal.currentState not found, holds are kept");
                return true;
            }
            return (Animal.AnimalState)stateField.GetValue(animal) == Animal.AnimalState.Sleeping;
        }

        private static Animal Live(int animalId)
        {
            try
            {
                if (SaveGate.Busy) return null;
                foreach (Animal animal in SceneIds.Active<Animal>())
                {
                    if (animal.GetInstanceID() == animalId) return animal.isActiveAndEnabled ? animal : null;
                }
                return null;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "AnimalActions.Live", e);
                return null;
            }
        }

        private sealed class HuntRecord
        {
            internal string ModId;
            internal Animal Animal;
            internal Stats Target;
        }

        private sealed class SleepRecord
        {
            internal string ModId;
            internal Animal Animal;
        }
    }
}
