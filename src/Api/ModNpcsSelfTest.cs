using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    internal static class ModNpcsSelfTest
    {
        internal static string Run()
        {
            try
            {
                SavedNpcFile file = new SavedNpcFile();
                file.Put(Entry("a.mod", "n1", "S1"));
                file.Put(Entry("a.mod", "n1", "S2"));
                if (file.Entries.Count != 1 || file.Entries[0].Scene != "S2") return "the same npc id was not replaced";
                file.Put(Entry("a.mod", "n2", "S1"));
                file.Put(Entry("b.mod", "n1", "S1"));
                if (file.Restorable("S1", m => true).Count != 2) return "restore did not find the scene's npcs";
                if (file.Restorable("S1", m => m == "b.mod").Count != 1) return "restore ignored the installed filter";
                if (file.Restorable("S3", m => true).Count != 0) return "restore ran in a scene with no npc";
                if (SavedNpcFile.Clean(file).Entries.Count != 3) return "a saved npc of an absent mod was not kept through a save and load";
                if (file.Count("a.mod", null) != 2 || file.Count("a.mod", "n2") != 1) return "count of a mod's npcs is wrong";
                if (file.Drop("a.mod", "n2") != 1 || file.Entries.Count != 2) return "despawn dropped more than one npc";
                if (file.Drop("a.mod", null) != 1 || file.Entries.Count != 1 || file.Entries[0].Mod != "b.mod") return "clear did not drop only that mod";
                SavedNpcFile full = new SavedNpcFile();
                for (int i = 0; i < SavedNpcFile.MaxEntries; i++) full.Put(Entry("m", "n" + i, "S"));
                if (full.Room("m", "extra") || !full.Room("m", "n0") || !full.Room("o", "extra")) return "the cap is not per mod, or refused an existing npc or accepted a new one";
                if (SavedNpcFile.Clean(null).Entries.Count != 0) return "a missing file did not load empty";
                SavedNpcFile raw = new SavedNpcFile();
                raw.Entries.Add(null);
                raw.Entries.Add(Entry("bad/mod", "n1", "S"));
                raw.Entries.Add(Entry("ok", "", "S"));
                raw.Entries.Add(Entry("ok", new string('x', 65), "S"));
                raw.Entries.Add(Entry("ok", new string('x', 64), "S"));
                SavedNpcEntry noName = Entry("ok", "n1", "S");
                noName.Name = "";
                raw.Entries.Add(noName);
                raw.Entries.Add(Entry("ok", "n2", ""));
                SavedNpcEntry notFinite = Entry("ok", "n3", "S");
                notFinite.X = float.NaN;
                raw.Entries.Add(notFinite);
                raw.Entries.Add(Entry("ok", "n4", "S"));
                raw.Entries.Add(Entry("ok", "n4", "T"));
                SavedNpcFile cleaned = SavedNpcFile.Clean(raw);
                if (cleaned.Entries.Count != 2) return "a bad saved npc was kept, or a valid one was dropped";
                if (cleaned.Entries[1].NpcId != "n4" || cleaned.Entries[1].Scene != "T") return "the last saved npc with the same id did not win";
                SavedNpcFile looks = new SavedNpcFile();
                looks.Put(Entry("a.mod", "old", "S"));
                SavedNpcEntry withLook = Entry("a.mod", "new", "S");
                withLook.Look = new SavedNpcLook { Sliders = true };
                withLook.Look.Shapes.Add(Shape("body", "Fat", 0.5f));
                withLook.Look.Shapes.Add(Shape("body", "", 1f));
                withLook.Look.Shapes.Add(Shape("body", "Muscle", float.NaN));
                withLook.Look.Shapes.Add(Shape("", "Lip", 1f));
                withLook.Look.DialNames.Add("Height");
                withLook.Look.DialValues.Add(0.4f);
                withLook.Look.DialNames.Add("Chin");
                withLook.Look.DialValues.Add(float.NaN);
                withLook.Look.SlotNames.Add("Hat");
                withLook.Look.Wearables.Add("cap");
                looks.Put(withLook);
                SavedNpcFile lookClean = SavedNpcFile.Clean(looks);
                if (lookClean.Entries[0].Look != null) return "a saved npc without a look kept an empty look";
                if (lookClean.LookOf("a.mod", "new") == null || lookClean.LookOf("a.mod", "old") != null || lookClean.LookOf("a.mod", "none") != null) return "the saved look was not found by its npc id";
                SavedNpcLook cleanedLook = lookClean.Entries[1].Look;
                if (cleanedLook.Shapes.Count != 1 || cleanedLook.Shapes[0].Shape != "Fat") return "a shape with an empty name or mesh or a non-finite weight was kept";
                if (cleanedLook.DialNames.Count != 1 || cleanedLook.DialNames[0] != "Height") return "a dial with a non-finite value was kept, or a valid one dropped";
                if (cleanedLook.SlotNames.Count != 1 || cleanedLook.Wearables[0] != "cap") return "a wearable slot was dropped";
                SavedNpcFile shapeOnly = new SavedNpcFile();
                SavedNpcEntry shaped = Entry("a.mod", "shape", "S");
                shaped.Look = new SavedNpcLook();
                shaped.Look.Shapes.Add(Shape("body", "Fat", 0.5f));
                shapeOnly.Put(shaped);
                if (SavedNpcFile.Clean(shapeOnly).Entries[0].Look == null) return "a look with only one shape was dropped";
                SavedNpcLook nullWearable = new SavedNpcLook();
                nullWearable.SlotNames.Add("Shoes");
                nullWearable.Wearables.Add(null);
                if (SavedNpcFile.CleanLook(nullWearable).Wearables[0] != "") return "a null wearable name was not cleaned to an empty name";
                SavedNpcLook many = new SavedNpcLook();
                for (int i = 0; i <= SavedNpcLook.MaxShapes; i++) many.Shapes.Add(Shape("body", "s" + i, 1f));
                SavedNpcLook manyClean = SavedNpcFile.CleanLook(many);
                if (manyClean.Shapes.Count != SavedNpcLook.MaxShapes || manyClean.Shapes[SavedNpcLook.MaxShapes - 1].Shape != "s255") return "more than 256 shapes were kept";
                List<SavedNpcShape> diff = SavedNpcFile.DiffShapes(new List<string> { "a", "b", "c" }, new List<float> { 1f, 3f, 4f }, new List<string> { "a", "b" }, new List<float> { 1f, 2f }, "body");
                if (diff.Count != 2 || diff[0].Shape != "b" || diff[0].Weight != 3f || diff[1].Shape != "c" || diff[1].Mesh != "body") return "the shape difference did not keep only the changed and new shapes";
                if (SavedNpcFile.DiffShapes(new List<string> { "a" }, new List<float> { 1f }, null, null, "body").Count != 1) return "a mesh with no source shapes did not keep its shapes";
                if (SavedNpcFile.DiffShapes(new List<string> { "a" }, new List<float> { float.NaN }, null, null, "body").Count != 0) return "a non-finite shape weight was written";
            }
            catch (Exception e)
            {
                return "exception: " + e.Message;
            }
            return "mod npc saved self-check ok";
        }

        private static SavedNpcEntry Entry(string mod, string npcId, string scene)
        {
            return new SavedNpcEntry { Mod = mod, NpcId = npcId, Source = 1, Name = "Test", LastName = "", X = 1f, Y = 2f, Z = 3f, Yaw = 90f, Scene = scene, KeepDialogue = false };
        }

        private static SavedNpcShape Shape(string mesh, string shape, float weight)
        {
            return new SavedNpcShape { Mesh = mesh, Shape = shape, Weight = weight };
        }
    }
}
