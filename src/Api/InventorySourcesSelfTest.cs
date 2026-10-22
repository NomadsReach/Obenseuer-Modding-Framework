using System;
using System.Collections.Generic;
using System.Reflection;
using OS.Items;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class InventorySourcesSelfTest
    {
        private const string ModA = "selfcheck.inventorya";
        private const string ModB = "selfcheck.inventoryb";

        internal static string Run()
        {
            List<GameObject> made = new List<GameObject>();
            try
            {
                List<InventorySources.Entry> providers = Providers();
                if (providers == null) return "the registered provider list was not found";
                List<InventorySources.Entry> saved = new List<InventorySources.Entry>(providers);
                providers.Clear();
                try
                {
                    string error = CheckArguments();
                    if (error == null) error = CheckEmpty();
                    if (error == null) error = CheckScope(providers, made);
                    if (error == null) error = CheckAmmo(providers, made);
                    if (error == null) error = CheckLiquid(providers, made);
                    if (error != null) return error;
                }
                finally
                {
                    providers.Clear();
                    providers.AddRange(saved);
                }
            }
            catch (Exception e)
            {
                return "exception: " + e.Message;
            }
            finally
            {
                for (int i = 0; i < made.Count; i++) UnityEngine.Object.DestroyImmediate(made[i]);
            }
            return "inventory sources self-check ok";
        }

        private static string CheckArguments()
        {
            if (!Throws<ArgumentException>(() => InventorySources.Register(null, Given))) return "a null mod id was accepted by Register";
            if (!Throws<ArgumentException>(() => InventorySources.Register("", Given))) return "an empty mod id was accepted by Register";
            if (!Throws<ArgumentException>(() => InventorySources.Register("selfcheck bad", Given))) return "a mod id with a space was accepted by Register";
            if (!Throws<ArgumentNullException>(() => InventorySources.Register(ModA, null))) return "a missing provider was accepted by Register";
            if (!Throws<ArgumentException>(() => InventorySources.Unregister(null))) return "a null mod id was accepted by Unregister";
            if (!Throws<ArgumentException>(() => InventorySources.Unregister(""))) return "an empty mod id was accepted by Unregister";
            if (InventorySources.Unregister(ModA)) return "Unregister reported removing a mod that was never registered";
            return null;
        }

        private static string CheckEmpty()
        {
            if (InventorySources.IsOpen) return "a scope was already open before the check";
            if (InventorySources.Open(InventorySources.FurnitureBlueprint, "selfcheck.empty", false) != null) return "a scope opened with no provider registered";
            if (InventorySources.IsOpen) return "the empty provider list left a scope open";
            if (InventorySources.Count(10, false, true, -1, false) != 0) return "counting outside a scope reported items";
            if (InventorySources.Take(10, 1, true, -1) != 0) return "taking outside a scope reported items";
            return null;
        }

        private static string CheckScope(List<InventorySources.Entry> providers, List<GameObject> made)
        {
            StorageSlot[] first = { Slot(10, 5), Slot(20, 2) };
            StorageSlot[] second = { Slot(10, 4), Slot(30, 2) };
            Storage a = Make(made, first);
            Storage b = Make(made, second);
            Storage blank = Make(made, new StorageSlot[0]);
            providers.Add(new InventorySources.Entry(ModA, context => new Storage[] { a, a, null, blank }));
            providers.Add(new InventorySources.Entry(ModB, context => new Storage[] { a, b }));
            InventorySources.Scope scope = null;
            try
            {
                scope = InventorySources.Open(InventorySources.FurnitureBlueprint, "selfcheck.collect", true);
                if (scope == null) return "a scope with providers returned null";
                if (!InventorySources.IsOpen) return "an open scope does not report open";
                if (!InventorySources.Consuming) return "a consuming scope does not report consuming";
                if (scope.Storages.Count != 2) return "a repeated, missing or empty container left " + scope.Storages.Count + " containers";
                if (!ReferenceEquals(scope.Storages[0], a) || !ReferenceEquals(scope.Storages[1], b)) return "the two distinct containers were not kept in provider order";
                int count = InventorySources.Count(10, false, true, -1, false);
                if (count != 9) return "counting item 10 over the containers gave " + count;
                count = InventorySources.Count(20, false, true, -1, false);
                if (count != 2) return "counting item 20 over the containers gave " + count;
                if (InventorySources.Count(99, false, true, -1, false) != 0) return "counting an item no container holds reported items";
                float taken = InventorySources.Take(10, 2, true, -1);
                if (taken != 2) return "taking 2 of item 10 reported " + taken;
                if (first[0].itemAmount != 3) return "the take left " + first[0].itemAmount + " of item 10 in the first slot";
                if (second[0].itemAmount != 4) return "a take the first container covered lowered a later container";
                taken = InventorySources.Take(30, 1, true, -1);
                if (taken != 1) return "taking 1 of item 30 reported " + taken;
                if (second[1].itemAmount != 1) return "the take left " + second[1].itemAmount + " of item 30 in the last slot";
                taken = InventorySources.Take(20, 1, true, -1);
                if (taken != 1) return "taking 1 of item 20 reported " + taken;
                if (first[1].itemAmount != 1) return "the take left " + first[1].itemAmount + " of item 20 in the second slot";
                if (InventorySources.Take(99, 1, true, -1) != 0) return "taking an item no container holds reported items";
                if (InventorySources.Take(10, 0, true, -1) != 0) return "taking no items reported items";
                InventorySources.Close(scope);
                scope = null;
                if (InventorySources.IsOpen) return "a closed scope still reports open";
                if (InventorySources.Count(10, false, true, -1, false) != 0) return "a closed scope still counted containers";
                if (InventorySources.Take(10, 1, true, -1) != 0) return "a closed scope still took items";
                scope = InventorySources.Open(InventorySources.FurnitureBlueprint, "selfcheck.check", false);
                if (scope == null) return "a checking scope returned null";
                if (InventorySources.Consuming) return "a checking scope reports consuming";
                if (InventorySources.Take(10, 2, true, -1) != 0) return "a checking scope took items";
                if (first[0].itemAmount != 3) return "a checking scope lowered a container";
                count = InventorySources.Count(10, false, true, -1, false);
                if (count != 7) return "a checking scope counted " + count + " of item 10";
                return null;
            }
            finally
            {
                InventorySources.Close(scope);
            }
        }

        private static string CheckAmmo(List<InventorySources.Entry> providers, List<GameObject> made)
        {
            StorageSlot[] slots = { Slot(40, 1), Slot(41, 1) };
            ItemAmmodata ammo = new ItemAmmodata();
            ammo.ammo = 3f;
            slots[0].Meta = new object[] { ammo };
            Storage store = Make(made, slots);
            providers.Clear();
            providers.Add(new InventorySources.Entry(ModA, context => new Storage[] { store }));
            InventorySources.Scope scope = null;
            try
            {
                scope = InventorySources.Open(InventorySources.FurnitureBlueprint, "selfcheck.ammo", true);
                if (scope == null) return "an ammo scope returned null";
                int rounds = InventorySources.CountAmmo(40);
                if (rounds != 3) return "counting the rounds in a container gave " + rounds;
                rounds = InventorySources.CountAmmo(41);
                if (rounds != 0) return "counting rounds of a stack without ammo data gave " + rounds;
                if (!InventorySources.TakeAmmo(40, -1, true, true)) return "taking a round from a container reported nothing taken";
                rounds = InventorySources.CountAmmo(40);
                if (rounds != 2) return "taking a round left " + rounds + " rounds";
                if (InventorySources.TakeAmmo(41, -1, true, true)) return "taking a round from a stack without ammo data reported a take";
                if (InventorySources.TakeAmmo(99, -1, true, true)) return "taking a round of an item no container holds reported a take";
                InventorySources.Close(scope);
                scope = InventorySources.Open(InventorySources.FurnitureBlueprint, "selfcheck.ammocheck", false);
                if (scope == null) return "a checking ammo scope returned null";
                if (InventorySources.TakeAmmo(40, -1, true, true)) return "a checking scope took a round";
                rounds = InventorySources.CountAmmo(40);
                if (rounds != 2) return "a checking scope lowered the rounds to " + rounds;
                return null;
            }
            finally
            {
                InventorySources.Close(scope);
                providers.Clear();
            }
        }

        private static string CheckLiquid(List<InventorySources.Entry> providers, List<GameObject> made)
        {
            StorageSlot[] slots = { Slot(50, 1) };
            slots[0].Meta = new object[] { LiquidMeta(60, 0.5f) };
            Storage store = Make(made, slots);
            providers.Clear();
            providers.Add(new InventorySources.Entry(ModA, context => new Storage[] { store }));
            InventorySources.Scope scope = null;
            try
            {
                scope = InventorySources.Open(InventorySources.FurnitureBlueprint, "selfcheck.liquid", false);
                if (scope == null) return "a liquid scope returned null";
                float litres = InventorySources.CountAmount(60, false, true, -1, true);
                if (litres != 0.5f) return "counting half a litre in a container gave " + litres;
                litres = InventorySources.CountAmount(60, false, true, -1, false);
                if (litres != 0f) return "counting a liquid as a plain item gave " + litres;
                return null;
            }
            finally
            {
                InventorySources.Close(scope);
                providers.Clear();
            }
        }

        private static ItemLiquidData LiquidMeta(int itemId, float amount)
        {
            ItemLiquidData liquid = new ItemLiquidData();
            liquid.liquidItem = LiquidReference(itemId);
            liquid.liquidAmount = amount;
            return liquid;
        }

        private static ItemReference LiquidReference(int itemId)
        {
            ItemReference reference = new ItemReference((Item)null);
            reference.ID = itemId;
            return reference;
        }

        private static bool Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
                return false;
            }
            catch (T)
            {
                return true;
            }
        }

        private static IEnumerable<Storage> Given(InventorySourceContext context)
        {
            return new Storage[0];
        }

        private static List<InventorySources.Entry> Providers()
        {
            FieldInfo field = typeof(InventorySources).GetField("providers", BindingFlags.Static | BindingFlags.NonPublic);
            return field == null ? null : field.GetValue(null) as List<InventorySources.Entry>;
        }

        private static Storage Make(List<GameObject> made, StorageSlot[] slots)
        {
            GameObject go = new GameObject("ObenseuerCore.InventorySourcesCheck");
            made.Add(go);
            Storage storage = go.AddComponent<Storage>();
            storage.storageSlots = slots;
            SlotController[] controllers = new SlotController[slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                controllers[i] = new SlotController();
                controllers[i].itemStack = slots[i];
            }
            storage.Slots = controllers;
            return storage;
        }

        private static StorageSlot Slot(int itemId, int amount)
        {
            StorageSlot slot = new StorageSlot();
            slot.itemId = itemId;
            slot.itemAmount = amount;
            slot.Meta = new object[] { new ItemQualityData() };
            return slot;
        }
    }
}
