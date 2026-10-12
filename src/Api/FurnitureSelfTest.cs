using System;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class FurnitureSelfTest
    {
        internal static string Run()
        {
            try
            {
                const string guid = "example.spicy-jerky";
                string id = FurnitureRegistry.IdFor(guid, "crate-red");
                if (id != "obx:example.spicy-jerky/crate-red") return "id text";
                if (!FurnitureRegistry.IsModId(id)) return "obx prefix not recognised";
                string modGuid;
                string key;
                if (!FurnitureRegistry.TryParse(id, out modGuid, out key)) return "obx id not parsed";
                if (modGuid != guid || key != "crate-red") return "round trip changed the parts";
                if (FurnitureRegistry.TryParse("obx:example.spicy-jerky/bad/key", out modGuid, out key)) return "slash in key parsed";
                if (FurnitureRegistry.TryParse("obx:bad guid/key", out modGuid, out key)) return "space in modGuid parsed";
                if (FurnitureRegistry.TryParse("obx:nokey", out modGuid, out key)) return "id without a key parsed";
                if (FurnitureRegistry.IsModId("Assets/Content/Prefabs/Building System/Furniture/Tables/Wooden Table.asset")) return "template path read as a mod id";
                if (!Refused("ok", "bad/key")) return "slash in key not refused";
                if (!Refused("bad guid", "k")) return "space in modGuid not refused";
                if (!Refused("ok", "")) return "empty key not refused";
                if (Refused("ok", "crate-red")) return "valid key refused";
                if (!ForeignModelRefused()) return "model not from ModelRegistry not refused";
                if (FurnitureBuild.AlignOffset(Box(0f, 0.5f, 0f, 1f, 1f, 1f), Box(2f, 0.5f, -1f, 2f, 1f, 2f)) != new Vector3(2f, 0f, -1f)) return "bottom-centre offset";
                if (FurnitureBuild.AlignOffset(Box(0f, 0f, 0f, 1f, 1f, 1f), Box(1f, 0.5f, 1f, 1f, 0.5f, 1f)) != new Vector3(1f, 0.75f, 1f)) return "bottom-centre offset above the floor";
                if (FurnitureBuild.AlignOffset(Box(3f, 0.5f, 3f, 1f, 1f, 1f), Box(3f, 0.5f, 3f, 1f, 1f, 1f)) != Vector3.zero) return "same bounds not zero";
                Vector3 scale;
                if (!FurnitureBuild.CompensatedScale(Vector3.one, new Vector3(0.5f, 0.5f, 0.5f), out scale) || scale != new Vector3(2f, 2f, 2f)) return "child 0 scale not compensated";
                if (FurnitureBuild.AlignOffset(FurnitureBuild.ScaledBounds(Box(1f, 2f, 0f, 1f, 2f, 1f), scale), Box(3f, 0f, 4f, 1f, 1f, 1f)) != new Vector3(1f, -2.5f, 4f)) return "scaled child 0 offset";
                if (FurnitureBuild.CompensatedScale(Vector3.one, new Vector3(0f, 1f, 1f), out scale)) return "zero child 0 scale not refused";
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
