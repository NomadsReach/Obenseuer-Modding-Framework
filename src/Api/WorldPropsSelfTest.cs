using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    internal static class WorldPropsSelfTest
    {
        private const string TestMod = "obenseuer.moddingframework.wpropscheck";
        private const string MissingMod = "obenseuer.moddingframework.wpropsmissing";
        private const string TestScene = "NoSuchSceneForWorldProps";
        private const int MaxTries = 24;
        private static int spawned;
        private static string spawnProblem;
        private static WorldPropUse used;
        private static readonly List<string> spawnedProps = new List<string>();

        internal static string Run()
        {
            try
            {
                string file = FileChecks();
                if (file != null) return file;
                string round = RoundTrip();
                if (round != null) return round;
                string refusals = Refusals();
                if (refusals != null) return refusals;
            }
            catch (Exception e)
            {
                return "exception: " + e.Message;
            }
            return "world prop self-check ok";
        }

        internal static string Play()
        {
            string scene = SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(scene)) return "no scene is loaded";
            if (SaveGate.Busy) return "a save or load is running, try again";
            if (SaveData.Folder == null) return "no save is loaded, load a save first";
            Vector3 at = PlayerState.Position + new Vector3(0f, 0.2f, 1f);
            spawned = 0;
            spawnProblem = null;
            used = null;
            spawnedProps.Clear();
            WorldProps.Spawned += OnSpawned;
            try
            {
                WorldProps.Clear(TestMod);
                SaveData.Flush(WorldProps.StoreModId);
                string path = SaveData.WritePath(WorldProps.StoreModId);
                string before = path == null ? null : FileText(path);
                if (before == null) return "the world prop file was not written";
                string basic = Basic(scene, at);
                if (basic != null) return basic;
                string builds = Builds(scene, at);
                if (builds != null) return builds;
                string restore = Restore(scene);
                if (restore != null) return restore;
                string settle = Settle();
                if (settle != null) return settle;
                string leak = Leak();
                if (leak != null) return leak;
                string missing = Missing();
                if (missing != null) return missing;
                WorldProps.Clear(TestMod);
                SaveData.Flush(WorldProps.StoreModId);
                string after = FileText(path);
                if (after == null) return "the world prop file was not written again";
                if (after != before) return "the world prop file written after the check differs from the one written before it";
                return "world prop live self-check ok in " + scene;
            }
            catch (Exception e)
            {
                return "exception: " + e.Message;
            }
            finally
            {
                WorldProps.Spawned -= OnSpawned;
                WorldProps.Clear(TestMod);
            }
        }

        private static string Basic(string scene, Vector3 at)
        {
            string key = "core check " + Time.frameCount;
            if (!WorldProps.OnUse(TestMod, key, Ignore)) return "OnUse refused the first handler for a key";
            if (WorldProps.OnUse(TestMod, key, Ignore)) return "a second OnUse for the same key was accepted";
            WorldPropOptions options = new WorldPropOptions();
            options.InteractKey = key;
            options.Data = "core";
            GameObject cube = WorldProps.Place(TestMod, "cube", scene, at, Quaternion.identity, Vector3.one, WorldPropSource.Primitive(WorldPrimitive.Cube), null);
            if (cube == null) return "the primitive was not placed in " + scene;
            GameObject light = WorldProps.Place(TestMod, "light", scene, at + Vector3.up, Quaternion.identity, Vector3.one, WorldPropSource.Light(LightType.Point, Color.white, 4f, 1f, 30f), null);
            if (light == null) return "the light was not placed";
            UnityEngine.Light placed = light.GetComponent<UnityEngine.Light>();
            if (placed == null || placed.type != LightType.Point || placed.range != 4f || placed.intensity != 1f) return "the placed light is not the light that was asked for";
            GameObject anchor = WorldProps.Place(TestMod, "anchor", scene, at + Vector3.right, Quaternion.identity, Vector3.one, WorldPropSource.Anchor(), options);
            if (anchor == null) return "the anchor was not placed";
            if (anchor.GetComponent<Collider>() == null) return "the anchor with an interaction has no collider";
            ModInteraction interaction = anchor.GetComponent<ModInteraction>();
            if (interaction == null) return "the interaction was not attached to the anchor";
            if (spawnProblem != null) return spawnProblem;
            if (spawned != 3) return "Spawned was raised " + spawned + " times for 3 props";
            if (WorldProps.Get(TestMod, "cube") != cube) return "Get does not return the live object";
            List<WorldPropInfo> list = WorldProps.List(TestMod, scene);
            if (list.Count != 3) return "List found " + list.Count + " props instead of 3";
            bool live = false;
            foreach (WorldPropInfo info in list)
            {
                if (info.PropId == "anchor") live = info.Live && info.InteractKey == key && info.Data == "core";
            }
            if (!live) return "List did not report the anchor as live with its interaction key and data";
            if (!WorldProps.Move(TestMod, "cube", at + Vector3.up * 2f, Quaternion.identity)) return "Move refused a live prop";
            if (cube.transform.position != at + Vector3.up * 2f) return "Move did not move the live object";
            if (spawned != 3) return "Spawned was raised again by Move";
            if (interaction.ModId != TestMod) return "the interaction is owned by another mod";
            interaction.Interact();
            if (used == null || used.PropId != "anchor" || used.Data != "core" || used.Object != anchor) return "the interaction did not hand the prop to the handler";
            if (!WorldProps.Remove(TestMod, "anchor")) return "Remove refused a live prop";
            if (WorldProps.Get(TestMod, "anchor") != null) return "Remove left the object live";
            if (WorldProps.List(TestMod, scene).Count != 2) return "Remove did not drop the record";
            WorldPropOptions fixedProp = new WorldPropOptions();
            fixedProp.IsStatic = true;
            if (WorldProps.Place(TestMod, "fixed", scene, at, Quaternion.identity, Vector3.one, WorldPropSource.Primitive(WorldPrimitive.Cylinder), fixedProp) == null) return "the static prop was not placed";
            if (WorldProps.Move(TestMod, "fixed", at + Vector3.up, Quaternion.identity)) return "Move accepted a static prop";
            return null;
        }

        private static string Builds(string scene, Vector3 at)
        {
            int prefabs = DestructibleList.instance == null ? -1 : DestructibleList.instance.prefabs.Count;
            int collectibles = Collectible.allCollectibles.Count;
            string furniture = Furniture(scene, at + Vector3.forward);
            if (furniture != null) return furniture;
            string item = Item(scene, at + Vector3.forward * 2f);
            if (item != null) return item;
            if (DestructibleList.instance != null && DestructibleList.instance.prefabs.Count != prefabs) return "a placed prop added " + (DestructibleList.instance.prefabs.Count - prefabs) + " entries to DestructibleList";
            if (Collectible.allCollectibles.Count != collectibles) return "a placed prop added " + (Collectible.allCollectibles.Count - collectibles) + " collectibles";
            return null;
        }

        private static string Furniture(string scene, Vector3 at)
        {
            List<FurnitureCatalogEntry> catalog = Building.Catalog();
            int tried = 0;
            foreach (FurnitureCatalogEntry entry in catalog)
            {
                if (tried >= MaxTries) break;
                tried++;
                WorldPropOptions options = new WorldPropOptions();
                options.InteractKey = "core furniture " + Time.frameCount;
                WorldProps.OnUse(TestMod, options.InteractKey, Ignore);
                GameObject go = WorldProps.Place(TestMod, "furniture", scene, at, Quaternion.identity, Vector3.one, WorldPropSource.Furniture(entry.Id, 0), options);
                if (go == null) continue;
                string bad = OnlyInteraction(go);
                if (bad != null) return "the furniture prop " + entry.Id + " kept " + bad;
                if (go.GetComponentInChildren<Collectible>(true) != null) return "the furniture prop " + entry.Id + " kept a Collectible";
                if (go.GetComponentInChildren<FurniturePlaceable>(true) != null) return "the furniture prop " + entry.Id + " kept a FurniturePlaceable";
                if (!WorldProps.Move(TestMod, "furniture", at + Vector3.right, Quaternion.identity)) return "Move refused the furniture prop";
                return null;
            }
            return "no furniture prefab could be placed in " + scene + " after " + tried + " of " + catalog.Count + " catalog entries";
        }

        private static string Item(string scene, Vector3 at)
        {
            int tried = 0;
            foreach (Item candidate in ItemDatabase.database)
            {
                if (tried >= MaxTries) break;
                if (candidate == null || candidate.ID <= 0 || candidate.Prefab == null) continue;
                tried++;
                GameObject go = WorldProps.Place(TestMod, "item", scene, at, Quaternion.identity, Vector3.one, WorldPropSource.Item(candidate.ID), null);
                if (go == null) continue;
                string bad = OnlyInteraction(go);
                if (bad != null) return "the item prop " + candidate.ID + " kept " + bad;
                if (go.GetComponentInChildren<Collectible>(true) != null) return "the item prop " + candidate.ID + " kept a Collectible";
                return null;
            }
            return "no item prefab could be placed in " + scene + " after " + tried + " of " + ItemDatabase.database.Count + " items";
        }

        private static string Restore(string scene)
        {
            GameObject live = WorldProps.Get(TestMod, "furniture");
            if (live == null) return "the furniture prop is not live before the restore check";
            UnityEngine.Object.DestroyImmediate(live);
            spawned = 0;
            spawnedProps.Clear();
            WorldProps.Restore(scene);
            if (spawnProblem != null) return spawnProblem;
            GameObject back = WorldProps.Get(TestMod, "furniture");
            if (back == null) return "Restore did not bring the furniture prop back";
            if (back == live) return "Restore kept the destroyed object";
            int count = Count("furniture");
            if (count != 1) return "Restore raised Spawned " + count + " times for the furniture prop";
            WorldProps.Restore(scene);
            if (Count("furniture") != count) return "Restore raised Spawned again for a prop that was already live";
            if (WorldProps.Get(TestMod, "furniture") != back) return "Restore replaced a live prop";
            return null;
        }

        private static string Settle()
        {
            GameObject go = new GameObject("ObenseuerCore.WorldPropsSettle");
            try
            {
                Rigidbody body = go.AddComponent<Rigidbody>();
                AudioSource source = go.AddComponent<AudioSource>();
                source.playOnAwake = true;
                ParticleSystem system = go.AddComponent<ParticleSystem>();
                ParticleSystem.MainModule main = system.main;
                main.playOnAwake = true;
                WorldPropsBuild.Settle(go);
                if (!body.isKinematic) return "a rigidbody on a prop is not kinematic";
                if (body.useGravity) return "a rigidbody on a prop still falls";
                if (source.playOnAwake) return "an audio source on a prop still plays on its own";
                ParticleSystem.MainModule settled = system.main;
                if (settled.playOnAwake) return "a particle system on a prop still runs on its own";
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
            return null;
        }

        private static string Leak()
        {
            GameObject source = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject go = new GameObject("ObenseuerCore.WorldPropsLeak");
            Mesh mesh = new Mesh();
            Material material = null;
            try
            {
                MeshRenderer renderer = source.GetComponent<MeshRenderer>();
                if (renderer == null || renderer.sharedMaterial == null) return "the leak check has no material to copy";
                material = new Material(renderer.sharedMaterial);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterials = new Material[] { material };
                go.AddComponent<WorldPropsModelAssets>().Hold(mesh, new Material[] { material });
                UnityEngine.Object.DestroyImmediate(go);
                go = null;
                if (mesh != null) return "the mesh of a destroyed model prop was not freed";
                if (material != null) return "a material of a destroyed model prop was not freed";
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
                if (mesh != null) UnityEngine.Object.DestroyImmediate(mesh);
                if (material != null) UnityEngine.Object.DestroyImmediate(material);
            }
            return null;
        }

        private static string Missing()
        {
            SavedPropEntry entry = Entry(MissingMod, "kept", (int)WorldPrimitive.Cube);
            entry.Scene = SceneManager.GetActiveScene().name;
            int count = spawned;
            if (WorldProps.Spawn(entry) != null) return "the framework built a prop of a mod that is not installed";
            if (spawned != count) return "Spawn raised Spawned for a prop of a mod that is not installed";
            if (WorldProps.Get(MissingMod, "kept") != null) return "a prop of a mod that is not installed is live";
            return null;
        }

        private static string OnlyInteraction(GameObject go)
        {
            foreach (MonoBehaviour part in go.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (part == null) continue;
                if (part is ModInteraction) continue;
                return part.GetType().Name;
            }
            return null;
        }

        private static int Count(string propId)
        {
            int count = 0;
            foreach (string prop in spawnedProps)
            {
                if (prop == propId) count++;
            }
            return count;
        }

        private static string FileText(string path)
        {
            try
            {
                return File.Exists(path) ? File.ReadAllText(path) : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void Ignore(WorldPropUse args)
        {
            used = args;
        }

        private static void OnSpawned(WorldPropSpawned args)
        {
            spawned++;
            if (args == null || args.Object == null)
            {
                spawnProblem = "Spawned was raised without an object";
                return;
            }
            spawnedProps.Add(args.PropId);
            if (!args.Object.activeInHierarchy) spawnProblem = "Spawned was raised with an inactive object";
            if (spawnProblem == null && args.Object.transform.position != args.Info.Position) spawnProblem = "Spawned was raised before the object was in its final pose";
        }

        private static string FileChecks()
        {
            SavedPropFile file = new SavedPropFile();
            file.Put(Entry("a.mod", "p1", 1));
            file.Put(Entry("a.mod", "p1", 2));
            if (file.Entries.Count != 1 || file.Entries[0].Alt != 2) return "the same prop id was not replaced";
            file.Put(Entry("a.mod", "p2", 1));
            file.Put(Entry("b.mod", "p1", 1));
            if (file.Count("a.mod") != 2 || file.Drop("a.mod", "p2") != 1 || file.Entries.Count != 2) return "drop of one prop of one mod is wrong";
            if (file.Drop("a.mod", null) != 1 || file.Entries.Count != 1 || file.Entries[0].Mod != "b.mod") return "clear did not drop only that mod";
            SavedPropFile full = new SavedPropFile();
            for (int i = 0; i < SavedPropFile.MaxEntries; i++) full.Put(Entry("m", "p" + i, 1));
            if (full.Room("m", "extra") || !full.Room("m", "p0") || !full.Room("o", "extra")) return "the cap is per mod, or it refused an existing prop or accepted a new one";
            if (SavedPropFile.Clean(null).Entries.Count != 0) return "a missing file did not load empty";
            SavedPropFile kept = new SavedPropFile();
            kept.Put(Entry(MissingMod, "kept", 1));
            SavedPropFile keptClean = SavedPropFile.Clean(kept);
            if (keptClean.Entries.Count != 1 || keptClean.Find(MissingMod, "kept") == null) return "the record of a mod that is not installed was dropped";
            SavedPropFile raw = new SavedPropFile();
            raw.Entries.Add(null);
            raw.Entries.Add(Entry("bad/mod", "p", 1));
            raw.Entries.Add(Entry("ok", "", 1));
            raw.Entries.Add(Entry("ok", new string('x', 65), 1));
            SavedPropEntry noScene = Entry("ok", "noscene", 1);
            noScene.Scene = "";
            raw.Entries.Add(noScene);
            raw.Entries.Add(Entry("ok", "kind", 1, "", 99));
            SavedPropEntry nan = Entry("ok", "nan", 1);
            nan.Position = new Vector3(float.NaN, 0f, 0f);
            raw.Entries.Add(nan);
            SavedPropEntry infinity = Entry("ok", "infinity", 1);
            infinity.Rotation = new Quaternion(0f, float.PositiveInfinity, 0f, 0f);
            raw.Entries.Add(infinity);
            SavedPropEntry tiny = Entry("ok", "tiny", 1);
            tiny.Scale = new Vector3(0.001f, 1f, 1f);
            raw.Entries.Add(tiny);
            SavedPropEntry huge = Entry("ok", "huge", 1);
            huge.Scale = new Vector3(1f, 101f, 1f);
            raw.Entries.Add(huge);
            SavedPropEntry zero = Entry("ok", "zero", 1);
            zero.Scale = Vector3.zero;
            raw.Entries.Add(zero);
            SavedPropEntry layer = Entry("ok", "layer", 1);
            layer.Layer = 1;
            raw.Entries.Add(layer);
            SavedPropEntry data = Entry("ok", "data", 1);
            data.Data = new string('d', SavedPropFile.MaxDataLength + 1);
            raw.Entries.Add(data);
            SavedPropEntry key = Entry("ok", "key", 1);
            key.InteractKey = new string('k', SavedPropFile.MaxInteractKeyLength + 1);
            raw.Entries.Add(key);
            SavedPropEntry model = Entry("ok", "model", 1);
            model.Kind = (int)WorldPropKind.Model;
            model.Key = "ok/thing";
            raw.Entries.Add(model);
            SavedPropEntry badModel = Entry("ok", "badmodel", 1);
            badModel.Kind = (int)WorldPropKind.Model;
            badModel.Key = "nokeyhere";
            raw.Entries.Add(badModel);
            SavedPropEntry light = Entry("ok", "light", 1);
            light.Kind = (int)WorldPropKind.Light;
            light.Key = "";
            light.LightKind = (int)LightType.Point;
            light.LightRange = 5f;
            light.LightIntensity = 1f;
            light.LightSpotAngle = 30f;
            raw.Entries.Add(light);
            SavedPropEntry badLight = Entry("ok", "badlight", 1);
            badLight.Kind = (int)WorldPropKind.Light;
            badLight.Key = "";
            badLight.LightKind = (int)LightType.Directional;
            badLight.LightRange = 5f;
            badLight.LightIntensity = 1f;
            badLight.LightSpotAngle = 30f;
            raw.Entries.Add(badLight);
            raw.Entries.Add(Entry("ok", "kept", 1));
            raw.Entries.Add(Entry("ok", "kept", 2));
            SavedPropEntry longScene = Entry("ok", "longscene", 1);
            longScene.Scene = new string('s', SavedPropFile.MaxSceneLength + 1);
            raw.Entries.Add(longScene);
            SavedPropEntry maxScene = Entry("ok", "maxscene", 1);
            maxScene.Scene = new string('s', SavedPropFile.MaxSceneLength);
            raw.Entries.Add(maxScene);
            SavedPropFile cleaned = SavedPropFile.Clean(raw);
            if (cleaned.Entries.Count != 4) return "a bad saved prop was kept, or a good one was dropped";
            if (cleaned.Entries[0].PropId != "model" || cleaned.Entries[1].PropId != "light" || cleaned.Entries[2].PropId != "kept" || cleaned.Entries[2].Alt != 2) return "a kept saved prop is not the expected one";
            if (cleaned.Entries[3].PropId != "maxscene" || cleaned.Entries[3].Scene.Length != SavedPropFile.MaxSceneLength) return "the longest allowed scene name was dropped";
            if (WorldPropsBuild.Check(WorldPropSource.Primitive(WorldPrimitive.Cube)) != null) return "a primitive source was refused before a scene is checked";
            return null;
        }

        private static string RoundTrip()
        {
            SavedPropFile file = new SavedPropFile();
            SavedPropEntry model = Entry("a.mod", "model", 0);
            model.Kind = (int)WorldPropKind.Model;
            model.Key = "a.mod/thing";
            file.Put(model);
            file.Put(Entry("a.mod", "primitive", (int)WorldPrimitive.Cylinder));
            SavedPropEntry furniture = Entry("a.mod", "furniture", 2, "desk", 0);
            furniture.Kind = (int)WorldPropKind.Furniture;
            file.Put(furniture);
            SavedPropEntry item = Entry("a.mod", "item", 42);
            item.Kind = (int)WorldPropKind.Item;
            file.Put(item);
            SavedPropEntry light = Entry("a.mod", "light", 0);
            light.Kind = (int)WorldPropKind.Light;
            light.Key = "";
            light.LightKind = (int)LightType.Spot;
            light.LightColor = new Color(0.25f, 0.5f, 0.75f, 1f);
            light.LightRange = 7.5f;
            light.LightIntensity = 1.5f;
            light.LightSpotAngle = 45f;
            file.Put(light);
            SavedPropEntry anchor = Entry("a.mod", "anchor", 0, "", (int)WorldPropKind.Anchor);
            anchor.InteractKey = "Open";
            anchor.Data = "mod data";
            anchor.Static = true;
            anchor.Collider = false;
            file.Put(anchor);
            bool fellBack;
            SavedPropFile back = SavedPropFile.Clean(SaveJson.Read<SavedPropFile>("world prop self-check", SaveJson.Write(file), out fellBack));
            if (fellBack) return "the world prop file did not read with Newtonsoft";
            if (back.Entries.Count != 6) return "the round trip lost a prop";
            SavedPropEntry readLight = back.Find("a.mod", "light");
            if (readLight == null || readLight.LightKind != (int)LightType.Spot || readLight.LightColor != light.LightColor || readLight.LightRange != 7.5f || readLight.LightIntensity != 1.5f || readLight.LightSpotAngle != 45f) return "a light field did not survive the round trip";
            SavedPropEntry readAnchor = back.Find("a.mod", "anchor");
            if (readAnchor == null || readAnchor.InteractKey != "Open" || readAnchor.Data != "mod data" || !readAnchor.Static || readAnchor.Collider) return "an option did not survive the round trip";
            SavedPropEntry readFurniture = back.Find("a.mod", "furniture");
            if (readFurniture == null || readFurniture.Kind != (int)WorldPropKind.Furniture || readFurniture.Key != "desk" || readFurniture.Alt != 2) return "a furniture prop did not survive the round trip";
            SavedPropEntry readItem = back.Find("a.mod", "item");
            if (readItem == null || readItem.Kind != (int)WorldPropKind.Item || readItem.Alt != 42) return "an item prop did not survive the round trip";
            if (back.Find("a.mod", "model").Key != "a.mod/thing") return "a model key did not survive the round trip";
            if (back.Find("a.mod", "primitive").Alt != (int)WorldPrimitive.Cylinder) return "a primitive kind did not survive the round trip";
            string old = "{\"Entries\":[{\"Mod\":\"a.mod\",\"PropId\":\"old\",\"Scene\":\"S\",\"Kind\":2,\"Key\":\"desk\",\"Alt\":1,\"Position\":{\"x\":1.0,\"y\":2.0,\"z\":3.0},\"Rotation\":{\"x\":0.0,\"y\":0.0,\"z\":0.0,\"w\":1.0},\"Scale\":{\"x\":1.0,\"y\":1.0,\"z\":1.0},\"Collider\":true,\"Static\":false,\"Layer\":0,\"InteractKey\":\"\",\"Data\":\"\"}]}";
            SavedPropFile oldFile = SavedPropFile.Clean(SaveJson.Read<SavedPropFile>("world prop self-check", old, out fellBack));
            if (fellBack || oldFile.Entries.Count != 1 || oldFile.Entries[0].LightKind != 0 || oldFile.Entries[0].LightRange != 0f) return "a file with no light fields did not load clean";
            SavedPropFile noScale = SavedPropFile.Clean(SaveJson.Read<SavedPropFile>("world prop self-check", "{\"Entries\":[{\"Mod\":\"a.mod\",\"PropId\":\"noscale\",\"Scene\":\"S\",\"Kind\":1,\"Alt\":0}]}", out fellBack));
            if (noScale.Entries.Count != 0) return "a prop with no scale was kept";
            return null;
        }

        private static string Refusals()
        {
            if (!SavedPropFile.PoseAllowed(Vector3.zero, Quaternion.identity, new Vector3(SavedPropFile.MinScale, SavedPropFile.MinScale, SavedPropFile.MinScale))) return "the smallest allowed scale was refused";
            if (!SavedPropFile.PoseAllowed(Vector3.zero, Quaternion.identity, new Vector3(SavedPropFile.MaxScale, SavedPropFile.MaxScale, SavedPropFile.MaxScale))) return "the largest allowed scale was refused";
            if (SavedPropFile.PoseAllowed(Vector3.zero, Quaternion.identity, new Vector3(0.009f, 1f, 1f))) return "a scale below the minimum was accepted";
            if (SavedPropFile.PoseAllowed(Vector3.zero, Quaternion.identity, new Vector3(1f, 1f, 100.1f))) return "a scale above the maximum was accepted";
            if (SavedPropFile.PoseAllowed(Vector3.zero, Quaternion.identity, new Vector3(float.NaN, 1f, 1f))) return "a NaN scale was accepted";
            if (!Threw(() => WorldPropSource.Primitive((WorldPrimitive)9))) return "an unknown primitive was accepted";
            if (!Threw(() => WorldPropSource.Furniture("", 0))) return "an empty furniture id was accepted";
            if (!Threw(() => WorldPropSource.Furniture(new string('f', 129), 0))) return "a furniture id over 128 characters was accepted";
            if (!Threw(() => WorldPropSource.Item(0))) return "item id 0 was accepted";
            if (!Threw(() => WorldPropSource.Light(LightType.Directional, Color.white, 1f, 1f, 30f))) return "a directional light was accepted";
            if (!Threw(() => WorldPropSource.Light(LightType.Point, Color.white, 0f, 1f, 30f))) return "a light with no range was accepted";
            if (!Threw(() => WorldPropSource.Light(LightType.Spot, Color.white, 1f, 1f, 200f))) return "a spot light with an impossible angle was accepted";
            if (!Threw(() => WorldProps.Place("bad/id", "p", TestScene, Vector3.zero, Quaternion.identity, Vector3.one, WorldPropSource.Anchor(), null))) return "an invalid mod id was accepted";
            if (!Threw(() => WorldProps.Place(TestMod, "p", TestScene, Vector3.zero, Quaternion.identity, Vector3.one, null, null))) return "a null source was accepted";
            if (!Threw(() => WorldProps.Place(TestMod, "", TestScene, Vector3.zero, Quaternion.identity, Vector3.one, WorldPropSource.Anchor(), null))) return "an empty prop id was accepted";
            if (!Threw(() => WorldProps.Place(TestMod, new string('p', 65), TestScene, Vector3.zero, Quaternion.identity, Vector3.one, WorldPropSource.Anchor(), null))) return "a prop id over 64 characters was accepted";
            if (!Threw(() => WorldProps.Place(TestMod, "p", "", Vector3.zero, Quaternion.identity, Vector3.one, WorldPropSource.Anchor(), null))) return "an empty scene was accepted";
            if (!Threw(() => WorldProps.Place(TestMod, "p", new string('s', SavedPropFile.MaxSceneLength + 1), Vector3.zero, Quaternion.identity, Vector3.one, WorldPropSource.Anchor(), null))) return "a scene name over the cap was accepted";
            WorldPropOptions longData = new WorldPropOptions();
            longData.Data = new string('d', SavedPropFile.MaxDataLength + 1);
            if (!Threw(() => WorldProps.Place(TestMod, "p", TestScene, Vector3.zero, Quaternion.identity, Vector3.one, WorldPropSource.Anchor(), longData))) return "too much mod data was accepted";
            WorldPropOptions longKey = new WorldPropOptions();
            longKey.InteractKey = new string('k', SavedPropFile.MaxInteractKeyLength + 1);
            if (!Threw(() => WorldProps.Place(TestMod, "p", TestScene, Vector3.zero, Quaternion.identity, Vector3.one, WorldPropSource.Anchor(), longKey))) return "too long an interaction key was accepted";
            if (!Threw(() => WorldProps.List(TestMod, ""))) return "an empty scene was accepted by List";
            if (!Threw(() => WorldProps.OnUse(TestMod, "", Ignore))) return "an empty OnUse key was accepted";
            if (!Threw(() => WorldProps.OnUse(TestMod, new string('k', SavedPropFile.MaxInteractKeyLength + 1), Ignore))) return "too long an OnUse key was accepted";
            if (!Threw(() => WorldProps.OnUse(TestMod, "k", null))) return "a null OnUse handler was accepted";
            if (!Threw(() => WorldProps.OnUse("bad/id", "k", Ignore))) return "an invalid OnUse mod id was accepted";
            if (!WorldProps.IsAvailable) return "the world prop store is not ready, the refusal checks need it";
            int recorded = WorldProps.List(TestMod, TestScene).Count;
            WorldPropRefusal source = WorldProps.Refuse(TestMod, "refused", TestScene, Vector3.zero, Quaternion.identity, Vector3.one, WorldPropSource.Model("a.mod", "nothandle"), null);
            if (source == null || source.Where.IndexOf("WorldProps.Place source") != 0) return "a model that is not loaded was accepted";
            WorldPropRefusal interaction = WorldProps.Refuse(TestMod, "refused", TestScene, Vector3.zero, Quaternion.identity, Vector3.one, WorldPropSource.Anchor(), Options("Open", false, 0));
            if (interaction == null || interaction.Where != "WorldProps.Place interaction") return "an interaction without a collider was accepted";
            WorldPropRefusal layer = WorldProps.Refuse(TestMod, "refused", TestScene, Vector3.zero, Quaternion.identity, Vector3.one, WorldPropSource.Anchor(), Options(null, true, 5));
            if (layer == null || layer.Where != "WorldProps.Place layer") return "a prop on another layer was accepted";
            WorldPropRefusal small = WorldProps.Refuse(TestMod, "refused", TestScene, Vector3.zero, Quaternion.identity, new Vector3(0.001f, 1f, 1f), WorldPropSource.Anchor(), null);
            if (small == null || small.Where != "WorldProps.Place pose") return "a prop with a scale below the minimum was accepted";
            WorldPropRefusal big = WorldProps.Refuse(TestMod, "refused", TestScene, Vector3.zero, Quaternion.identity, new Vector3(1f, 1f, 101f), WorldPropSource.Anchor(), null);
            if (big == null || big.Where != "WorldProps.Place pose") return "a prop with a scale above the maximum was accepted";
            WorldPropRefusal nan = WorldProps.Refuse(TestMod, "refused", TestScene, new Vector3(float.NaN, 0f, 0f), Quaternion.identity, Vector3.one, WorldPropSource.Anchor(), null);
            if (nan == null || nan.Where != "WorldProps.Place pose") return "a prop with a NaN position was accepted";
            WorldPropRefusal ready = WorldProps.Refuse(TestMod, "refused", TestScene, Vector3.zero, Quaternion.identity, Vector3.one, WorldPropSource.Anchor(), null);
            if (SaveData.Folder == null)
            {
                if (ready == null || ready.Where != "WorldProps.Place save") return "a prop was accepted before a save was loaded";
            }
            else if (ready != null)
            {
                return "a valid prop was refused: " + ready.Why;
            }
            if (WorldProps.List(TestMod, TestScene).Count != recorded) return "a refused prop was recorded";
            if (WorldProps.Move(TestMod, "missing", Vector3.zero, Quaternion.identity)) return "Move accepted an unknown prop";
            if (WorldProps.Remove(TestMod, "missing")) return "Remove accepted an unknown prop";
            if (WorldProps.Move(TestMod, "missing", new Vector3(float.NaN, 0f, 0f), Quaternion.identity)) return "Move accepted a NaN position";
            if (WorldProps.List(TestMod, TestScene).Count != recorded) return "a refused call changed the recorded props";
            return null;
        }

        private static WorldPropOptions Options(string interactKey, bool collider, int layer)
        {
            WorldPropOptions options = new WorldPropOptions();
            options.InteractKey = interactKey;
            options.Collider = collider;
            options.Layer = layer;
            return options;
        }

        private static bool Threw(Action action)
        {
            try
            {
                action();
            }
            catch (ArgumentException)
            {
                return true;
            }
            return false;
        }

        private static SavedPropEntry Entry(string mod, string propId, int alt)
        {
            return Entry(mod, propId, alt, "", (int)WorldPropKind.Primitive);
        }

        private static SavedPropEntry Entry(string mod, string propId, int alt, string key, int kind)
        {
            SavedPropEntry entry = new SavedPropEntry();
            entry.Mod = mod;
            entry.PropId = propId;
            entry.Scene = "S";
            entry.Kind = kind;
            entry.Key = key;
            entry.Alt = alt;
            entry.Position = new Vector3(1f, 2f, 3f);
            entry.Rotation = Quaternion.identity;
            entry.Scale = Vector3.one;
            entry.Collider = true;
            entry.InteractKey = "";
            entry.Data = "";
            entry.LightRange = 1f;
            entry.LightIntensity = 1f;
            entry.LightSpotAngle = 30f;
            return entry;
        }
    }
}
