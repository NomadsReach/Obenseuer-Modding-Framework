using System;
using System.Collections.Generic;
using OS.Items;
using UnityEngine;

namespace ObenseuerCore
{
    public enum AnimalKind
    {
        Other = 0,
        Cat = 1,
        Chick = 2,
        Chicken = 3,
        Pig = 4,
        Rat = 5
    }

    public sealed class AnimalInfo
    {
        public AnimalKind Kind { get; private set; }
        public string Name { get; private set; }
        public Vector3 Position { get; private set; }
        public bool Tame { get; private set; }
        public bool Following { get; private set; }
        public bool Running { get; private set; }
        public int Id { get; private set; }
        public float Health { get; private set; }
        public int MaxHealth { get; private set; }

        internal AnimalInfo(int id, AnimalKind kind, string name, Vector3 position, bool tame, bool following, bool running, float health, int maxHealth)
        {
            Id = id;
            Kind = kind;
            Name = name;
            Position = position;
            Tame = tame;
            Following = following;
            Running = running;
            Health = health;
            MaxHealth = maxHealth;
        }
    }

    public static class Animals
    {
        public static int Count
        {
            get
            {
                try
                {
                    return AnimalController.instance == null ? 0 : AnimalController.instance.animalCounter;
                }
                catch (Exception e)
                {
                    ApiLog.Once(Npcs.Owner, "Animals.Count", e);
                    return 0;
                }
            }
        }

        public static List<AnimalInfo> All()
        {
            try
            {
                List<AnimalInfo> result = new List<AnimalInfo>();
                List<Animal> following = AnimalController.instance == null ? new List<Animal>() : AnimalController.instance.followingAnimals;
                foreach (Animal animal in UnityEngine.Object.FindObjectsByType<Animal>(FindObjectsSortMode.None))
                {
                    AnimalStats stats = animal.GetComponent<AnimalStats>();
                    ItemAnimalData data = stats == null ? null : stats.animalData;
                    result.Add(new AnimalInfo(animal.GetInstanceID(), KindOf(animal), animal.name, animal.transform.position, animal.tame, following.Contains(animal), animal.Running(), data == null ? 0f : data.currentHealth, data == null ? 0 : data.maxHealth));
                }
                return result;
            }
            catch (Exception e)
            {
                ApiLog.Once(Npcs.Owner, "Animals.All", e);
                return new List<AnimalInfo>();
            }
        }

        public static bool Damage(int animalId, float amount)
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount <= 0f) throw new ArgumentException("amount must be a finite number above 0", "amount");
            try
            {
                if (SaveGate.Busy || DamageController.instance == null) return false;
                foreach (Animal animal in UnityEngine.Object.FindObjectsByType<Animal>(FindObjectsSortMode.None))
                {
                    if (animal.GetInstanceID() != animalId) continue;
                    AnimalStats stats = animal.GetComponent<AnimalStats>();
                    if (stats == null) return false;
                    if (stats.animalData == null || stats.animalData.currentHealth <= 0f) return false;
                    stats.Damage(DamageController.instance.GetDamageType(DamageTypeNames.Violence), amount);
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                ApiLog.Once(Npcs.Owner, "Animals.Damage", e);
                return false;
            }
        }

        private static AnimalKind KindOf(Animal animal)
        {
            if (animal is Cat) return AnimalKind.Cat;
            if (animal is Chick) return AnimalKind.Chick;
            if (animal is Chicken) return AnimalKind.Chicken;
            if (animal is Pig) return AnimalKind.Pig;
            if (animal is Rat) return AnimalKind.Rat;
            return AnimalKind.Other;
        }
    }

    public static class Osrats
    {
        public static int Strength
        {
            get
            {
                try
                {
                    return OsratManager.instance == null ? 0 : OsratManager.instance.GetStrength();
                }
                catch (Exception e)
                {
                    ApiLog.Once(Npcs.Owner, "Osrats.Strength", e);
                    return 0;
                }
            }
        }

        public static float RadiusStrength
        {
            get
            {
                try
                {
                    return OsratManager.instance == null ? 0f : OsratManager.instance.GetSignalStrenght();
                }
                catch (Exception e)
                {
                    ApiLog.Once(Npcs.Owner, "Osrats.RadiusStrength", e);
                    return 0f;
                }
            }
        }

        public static bool IsBeeping
        {
            get { return OsratManager.instance != null && OsratManager.instance.beepOn; }
        }

        public static bool IsEquipped
        {
            get { return OsratManager.instance != null && OsratManager.instance.osratVisible; }
        }

        public static bool IsSignalDisturbed
        {
            get { return OsratManager.instance != null && OsratManager.instance.disturbedSignal; }
        }
    }
}
