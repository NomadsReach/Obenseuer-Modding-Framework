using BepInEx;
#if BEPINEX6
using BepInEx.Unity.Mono;
#endif
using ObenseuerCore;
using UnityEngine;

[BepInPlugin("obenseuer.moddingframework.test.furniturev2", "FurnitureV2Test", "0.1.0")]
#if BEPINEX5
[BepInDependency("obenseuer.moddingframework", "0.10.0")]
#else
[BepInDependency("obenseuer.moddingframework", ">=0.10.0")]
#endif
public class FurnitureV2Test : BaseUnityPlugin
{
    public const string ModGuid = "obenseuer.moddingframework.test.furniturev2";
    private const string Table = "Assets/Content/Prefabs/Building System/Furniture/Tables/Wooden Table.asset";
    private const string Barrel = "Assets/Content/Prefabs/Building System/Furniture/Clutter/Blue Plastic Barrel.asset";
    private static readonly CoreLog Log = CoreLog.For("FurnitureV2Test");
    private string tableId;
    private string crateId;
    private ModKeybind giveBlueprint;
    private ModKeybind giveFlatpack;
    private ModKeybind logKnown;

    private void Awake()
    {
        tableId = FurnitureRegistry.Define(ModGuid, "bp-table", Definition(Table, null, "Test Mod Table"));
        Log.Info("[FV2] table " + tableId + ", blueprint " + DefineBlueprint(tableId));
        crateId = FurnitureRegistry.Define(ModGuid, "bp-crate", Definition(null, "Beer Crate", "Test Mod Crate"));
        Log.Info("[FV2] crate " + crateId + ", blueprint " + DefineBlueprint(crateId));
        giveBlueprint = Keybinds.Register(ModGuid, "give-blueprint", "Give test blueprint (hold Shift for the crate)", KeyCode.F6);
        giveFlatpack = Keybinds.Register(ModGuid, "give-flatpack", "Give 2 test table flatpacks", KeyCode.F7);
        logKnown = Keybinds.Register(ModGuid, "log-known", "Log test blueprints known", KeyCode.F8);
    }

    private void Update()
    {
        if (giveBlueprint.Down())
        {
            bool crate = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            Report("GiveBlueprint", FurnitureRegistry.GiveBlueprint(crate ? crateId : tableId), crate ? "crate" : "table");
        }
        if (giveFlatpack.Down()) Report("GiveFlatpack", FurnitureRegistry.GiveFlatpack(tableId, 2), "table, 2");
        if (logKnown.Down())
        {
            Report("IsBlueprintKnown", FurnitureRegistry.IsBlueprintKnown(tableId), "table");
            Report("IsBlueprintKnown", FurnitureRegistry.IsBlueprintKnown(crateId), "crate");
        }
    }

    private static FurnitureDefinition Definition(string path, string name, string title)
    {
        FurnitureDefinition definition = new FurnitureDefinition();
        definition.TemplatePath = path;
        definition.TemplateName = name;
        definition.Title = title;
        return definition;
    }

    private static string DefineBlueprint(string furnitureId)
    {
        BlueprintDefinition blueprint = new BlueprintDefinition();
        blueprint.Inputs[35010] = 2;
        blueprint.Inputs[35050] = 4;
        blueprint.FurnitureInputs.Add(Barrel);
        blueprint.ProcessTime = 600f;
        return FurnitureRegistry.DefineBlueprint(furnitureId, blueprint);
    }

    private static void Report(string action, bool result, string what)
    {
        Log.Info("[FV2] " + action + " -> " + result + " (" + what + ")");
    }
}
