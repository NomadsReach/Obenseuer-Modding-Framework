using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class FurnitureSelfTest
    {
        internal static string Run()
        {
            try
            {
                string ids = FurnitureIdsSelfTest.Run();
                if (ids != "furniture id self-check ok") return ids;
                if (FurnitureRegistry.IdFor("example.spicy-jerky", "crate-red") != "obx:example.spicy-jerky/crate-red") return "id text";
                if (!Refused("ok", "bad/key")) return "slash in key not refused";
                if (!Refused("bad guid", "k")) return "space in modGuid not refused";
                if (!Refused("ok", "")) return "empty key not refused";
                if (Refused("ok", "crate-red")) return "valid key refused";
                if (!ForeignModelRefused()) return "model not from ModelRegistry not refused";
                if (!BlueprintRefused("obx:example.spicy-jerky/crate-red", new BlueprintDefinition(), "furnitureId")) return "blueprint for an undefined furniture not refused";
                if (!BlueprintRefused("obx:example.spicy-jerky/crate-red", null, "definition")) return "null blueprint definition not refused";
                if (!BlueprintRefused("obx:example.spicy-jerky/crate-red", new BlueprintDefinition { MinimumSkill = 1 }, "definition")) return "minimum skill without a skill not refused";
                if (!BlueprintRefused("obx:example.spicy-jerky/crate-red", new BlueprintDefinition { Pools = new List<string> { null } }, "definition")) return "null pool entry not refused";
                if (FurnitureIds.CheckLoot("Toolbox", 0) == null) return "loot weight 0 accepted";
                if (!LootRefused("obx:a/b", "Category", 0)) return "loot weight 0 through AddFlatpackLoot not refused";
                if (FurnitureBuild.AlignOffset(Box(0f, 0.5f, 0f, 1f, 1f, 1f), Box(2f, 0.5f, -1f, 2f, 1f, 2f)) != new Vector3(2f, 0f, -1f)) return "bottom-centre offset";
                if (FurnitureBuild.AlignOffset(Box(0f, 0f, 0f, 1f, 1f, 1f), Box(1f, 0.5f, 1f, 1f, 0.5f, 1f)) != new Vector3(1f, 0.75f, 1f)) return "bottom-centre offset above the floor";
                if (FurnitureBuild.AlignOffset(Box(3f, 0.5f, 3f, 1f, 1f, 1f), Box(3f, 0.5f, 3f, 1f, 1f, 1f)) != Vector3.zero) return "same bounds not zero";
                Vector3 scale;
                if (!FurnitureBuild.CompensatedScale(Vector3.one, new Vector3(0.5f, 0.5f, 0.5f), out scale) || scale != new Vector3(2f, 2f, 2f)) return "child 0 scale not compensated";
                if (FurnitureBuild.AlignOffset(FurnitureBuild.ScaledBounds(Box(1f, 2f, 0f, 1f, 2f, 1f), scale), Box(3f, 0f, 4f, 1f, 1f, 1f)) != new Vector3(1f, -2.5f, 4f)) return "scaled child 0 offset";
                if (FurnitureBuild.CompensatedScale(Vector3.one, new Vector3(0f, 1f, 1f), out scale)) return "zero child 0 scale not refused";
                if (!Building.IsFinite(Vector3.one) || !Building.IsFinite(Quaternion.identity)) return "finite pose refused";
                if (Building.IsFinite(new Vector3(float.NaN, 0f, 0f)) || Building.IsFinite(new Vector3(0f, float.PositiveInfinity, 0f)) || Building.IsFinite(new Quaternion(0f, 0f, 0f, float.NaN))) return "NaN or Infinity pose accepted";
            }
            catch (Exception e)
            {
                return "exception: " + e.Message;
            }
            return "furniture self-check ok";
        }

        private static Bounds Box(float cx, float cy, float cz, float sx, float sy, float sz)
        {
            return new Bounds(new Vector3(cx, cy, cz), new Vector3(sx, sy, sz));
        }

        private static bool ForeignModelRefused()
        {
            try
            {
                FurnitureRegistry.Define("example.spicy-jerky", "crate-model", new FurnitureDefinition
                {
                    TemplateName = "Beer Crate",
                    Title = "Crate",
                    Model = new ModelHandle("crate", null, null, null, new ModelOptions(), "not from the registry")
                });
                return false;
            }
            catch (ArgumentException)
            {
                return true;
            }
        }

        private static bool BlueprintRefused(string furnitureId, BlueprintDefinition definition, string paramName)
        {
            try
            {
                FurnitureRegistry.DefineBlueprint(furnitureId, definition);
                return false;
            }
            catch (ArgumentException e)
            {
                return e.ParamName == paramName;
            }
        }

        private static bool LootRefused(string furnitureId, string category, int weight)
        {
            try
            {
                FurnitureRegistry.AddFlatpackLoot(furnitureId, category, weight);
                return false;
            }
            catch (ArgumentException)
            {
                return true;
            }
        }

        private static bool Refused(string modGuid, string key)
        {
            try
            {
                FurnitureRegistry.IdFor(modGuid, key);
                return false;
            }
            catch (ArgumentException)
            {
                return true;
            }
        }
    }
}
