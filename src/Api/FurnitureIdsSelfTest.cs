using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    internal static class FurnitureIdsSelfTest
    {
        private const string Template = "Assets/Content/Prefabs/Building System/Furniture/Tables/Wooden Table.asset";

        internal static string Run()
        {
            string modGuid;
            string key;
            string furniture;
            if (!FurnitureIds.IsModId("obx:example.spicy-jerky/crate-red")) return "obx prefix not recognised";
            if (!FurnitureIds.TryParse("obx:example.spicy-jerky/crate-red", out modGuid, out key)) return "obx id not parsed";
            if (modGuid != "example.spicy-jerky" || key != "crate-red") return "round trip changed the parts";
            if (FurnitureIds.TryParse("obx:example.spicy-jerky/bad/key", out modGuid, out key)) return "slash in key parsed";
            if (FurnitureIds.TryParse("obx:bad guid/key", out modGuid, out key)) return "space in modGuid parsed";
            if (FurnitureIds.TryParse("obx:nokey", out modGuid, out key)) return "id without a key parsed";
            if (FurnitureIds.IsModId(Template)) return "template path read as a mod id";
            if (FurnitureIds.BlueprintIdFor("obx:a.b/table") != "obx:a.b/table/blueprint") return "blueprint id text";
            if (!FurnitureIds.TryParseBlueprint("obx:a.b/table/blueprint", out furniture) || furniture != "obx:a.b/table") return "blueprint id not parsed back";
            if (FurnitureIds.BlueprintIdFor(Template) != null || FurnitureIds.BlueprintIdFor("") != null || FurnitureIds.BlueprintIdFor(null) != null || FurnitureIds.BlueprintIdFor("obx:a.b/table/blueprint") != null) return "blueprint id made from a non-furniture";
            if (FurnitureIds.TryParse("obx:a.b/table/blueprint", out modGuid, out key)) return "blueprint id parsed as furniture";
            if (FurnitureIds.Classify("") != FurnitureIdKind.Vanilla || FurnitureIds.Classify(null) != FurnitureIdKind.Vanilla || FurnitureIds.Classify(Template) != FurnitureIdKind.Vanilla) return "vanilla id not classified";
            if (FurnitureIds.Classify("OBX:a/b") != FurnitureIdKind.Vanilla) return "upper case prefix classified as mod";
            if (FurnitureIds.Classify("obx:a/b") != FurnitureIdKind.Furniture) return "furniture id not classified";
            if (FurnitureIds.Classify("obx:a/b/blueprint") != FurnitureIdKind.Blueprint) return "blueprint id not classified";
            if (FurnitureIds.Classify("obx:a/blueprint") != FurnitureIdKind.Furniture || FurnitureIds.BlueprintIdFor("obx:a/blueprint") != "obx:a/blueprint/blueprint") return "furniture keyed blueprint id";
            if (!FurnitureIds.TryParseBlueprint(FurnitureIds.BlueprintIdFor("obx:a/blueprint"), out furniture) || furniture != "obx:a/blueprint") return "furniture keyed blueprint round trip";
            foreach (string bad in new[] { "obx:", "obx:a", "obx:a/b/other", "obx:a/b/blueprint/x", "obx:bad guid/b" })
            {
                if (FurnitureIds.Classify(bad) != FurnitureIdKind.Malformed) return "malformed id not classified: " + bad;
            }
            float[] times = { float.NaN, float.PositiveInfinity, float.NegativeInfinity, 0f, -1f, 10000001f };
            foreach (float time in times)
            {
                if (FurnitureIds.CheckBlueprint(time, 1, false, 0, 10) == null) return "process time accepted: " + time;
            }
            if (FurnitureIds.CheckBlueprint(3600f, 0, false, 0, 10) == null || FurnitureIds.CheckBlueprint(3600f, 101, false, 0, 10) == null) return "amount outside 1 to 100 accepted";
            if (FurnitureIds.CheckBlueprint(3600f, 1, false, -1, 10) == null || FurnitureIds.CheckBlueprint(3600f, 1, true, 11, 10) == null || FurnitureIds.CheckBlueprint(3600f, 1, false, 1, 10) == null) return "minimum skill outside 0 to max, or set without a skill, accepted";
            if (FurnitureIds.CheckBlueprint(3600f, 1, false, 0, 10) != null) return "valid blueprint refused";
            List<KeyValuePair<int, int>> seventeen = new List<KeyValuePair<int, int>>();
            for (int i = 0; i < 17; i++) seventeen.Add(new KeyValuePair<int, int>(100 + i, 1));
            List<string> nine = new List<string>();
            for (int i = 0; i < 9; i++) nine.Add("Assets/Test/" + i + ".asset");
            if (FurnitureIds.CheckInputs(seventeen, new List<string>()) == null) return "17 inputs accepted";
            if (FurnitureIds.CheckInputs(new Dictionary<int, int> { { 0, 1 } }, new List<string>()) == null) return "input key 0 accepted";
            if (FurnitureIds.CheckInputs(new Dictionary<int, int> { { 1, 1 } }, new List<string>()) == null) return "input key 1 accepted";
            if (FurnitureIds.CheckInputs(new Dictionary<int, int> { { 35010, 0 } }, new List<string>()) == null) return "input amount 0 accepted";
            if (FurnitureIds.CheckInputs(new Dictionary<int, int> { { 35010, 1001 } }, new List<string>()) == null) return "input amount 1001 accepted";
            if (FurnitureIds.CheckInputs(new Dictionary<int, int>(), nine) == null) return "9 furniture inputs accepted";
            if (FurnitureIds.CheckInputs(new Dictionary<int, int>(), new List<string> { null }) == null) return "null furniture input accepted";
            if (FurnitureIds.CheckInputs(new Dictionary<int, int>(), new List<string> { "" }) == null) return "empty furniture input accepted";
            if (FurnitureIds.CheckInputs(new Dictionary<int, int>(), new List<string> { Template, Template }) == null) return "duplicate furniture input accepted";
            if (FurnitureIds.CheckInputs(new Dictionary<int, int>(), new List<string> { "obx:a.b/table/blueprint" }) == null) return "blueprint id as furniture input accepted";
            if (FurnitureIds.CheckInputs(new Dictionary<int, int>(), new List<string> { "obx:a" }) == null) return "malformed obx id as furniture input accepted";
            if (FurnitureIds.CheckInputs(null, new List<string>()) == null || FurnitureIds.CheckInputs(new Dictionary<int, int>(), null) == null) return "null input list accepted";
            if (FurnitureIds.CheckInputs(new Dictionary<int, int>(), new List<string>()) != null) return "empty inputs refused";
            if (FurnitureIds.CheckInputs(new Dictionary<int, int> { { 35010, 2 } }, new List<string> { Template }) != null) return "valid inputs refused";
            if (FurnitureIds.CheckPools(null) == null) return "null pool list accepted";
            if (FurnitureIds.CheckPools(nine) == null) return "9 pools accepted";
            if (FurnitureIds.CheckPools(new List<string> { null }) == null) return "null pool entry accepted";
            if (FurnitureIds.CheckPools(new List<string> { new string('a', 65) }) == null) return "65 character pool accepted";
            if (FurnitureIds.CheckPools(new List<string> { "Start", "Start" }) == null) return "repeated pool accepted";
            if (FurnitureIds.CheckPools(new List<string>()) != null || FurnitureIds.CheckPools(new List<string> { "", "Start", "Improved" }) != null || FurnitureIds.CheckPools(new List<string> { "Start", "start" }) != null) return "valid pools refused";
            if (FurnitureIds.CheckPools(new List<string> { new string('a', 64) }) != null) return "64 character pool refused";
            if (FurnitureIds.CheckPools(new List<string> { "a", "b", "c", "d", "e", "f", "g", "h" }) != null) return "8 pools refused";
            if (FurnitureIds.CheckLoot(null, 1) == null || FurnitureIds.CheckLoot("", 1) == null) return "empty loot category accepted";
            if (FurnitureIds.CheckLoot("Toolbox", 0) == null || FurnitureIds.CheckLoot("Toolbox", 101) == null) return "loot weight outside 1 to 100 accepted";
            if (FurnitureIds.CheckLoot("Toolbox", 1) != null || FurnitureIds.CheckLoot("Toolbox", 100) != null) return "valid loot refused";
            if (FurnitureIds.LootWeight(new List<int>()) != 0) return "empty loot weight not 0";
            if (FurnitureIds.LootWeight(new List<int> { 30, 20 }) != 50) return "loot weight not the sum";
            if (FurnitureIds.LootWeight(new List<int> { 60, 60 }) != 100) return "loot weight over 100 not capped";
            if (FurnitureIds.LootWeight(new List<int> { 100, 1 }) != 100) return "loot weight 101 not capped";
            List<int> pair = new List<int> { 3, 1 };
            if (FurnitureIds.PickWeighted(new List<int>(), 0) != -1 || FurnitureIds.PickWeighted(null, 0) != -1) return "empty weights picked";
            if (FurnitureIds.PickWeighted(new List<int> { 0, 0 }, 0) != -1) return "zero total picked";
            if (FurnitureIds.PickWeighted(pair, 0) != 0 || FurnitureIds.PickWeighted(pair, 2) != 0 || FurnitureIds.PickWeighted(pair, 3) != 1) return "weighted pick wrong";
            if (FurnitureIds.PickWeighted(pair, 4) != -1 || FurnitureIds.PickWeighted(pair, -1) != -1) return "roll outside the total picked";
            if (FurnitureIds.PickWeighted(new List<int> { 0, 5 }, 0) != 1) return "zero weight picked";
            if (FurnitureIds.PickWeighted(new List<int> { 2, 0, 2 }, 2) != 2) return "zero weight in the middle picked";
            if (FurnitureIds.PickWeighted(new List<int> { -5, 3 }, 0) != 1 || FurnitureIds.PickWeighted(new List<int> { -5, 3 }, 3) != -1) return "negative weight counted";
            return "furniture id self-check ok";
        }
    }
}
