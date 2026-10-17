# FurnitureV2Test

A small test plugin for furniture v2 in the Obenseuer modding framework: mod blueprints and flatpacks. It defines two test furniture pieces, a table made from the vanilla Wooden Table and a crate made from the vanilla Beer Crate. Each one has a blueprint, and keys give them to you and log their state.

It needs the framework (`obenseuer.moddingframework`, version 0.10.0 or newer) and BepInEx 5 (5.4.23 or later) or BepInEx 6 (Unity Mono). It is for testing only. Keep a copy of your save folder before you start. The save folder is usually `C:\Users\<you>\AppData\LocalLow\Loiste Interactive\Obenseuer\Saves\`.

## Install

1. Build the plugin with `dotnet build Examples\FurnitureV2Test\FurnitureV2Test.csproj -c Release`, or get `FurnitureV2Test.dll` from the framework download.
2. Create the folder `Obenseuer\BepInEx\plugins\FurnitureV2Test\` if it is not there.
3. Copy `FurnitureV2Test.dll` into that folder.
4. Start the game.

## Keys

You can change these in the Mods menu, page **Mod Keybinds**.

- **F6**: give the test table blueprint. Hold Shift and press F6 to give the crate blueprint instead.
- **F7**: give 2 table flatpacks.
- **F8**: log whether the table and the crate blueprints are known.

Each key writes a line such as `[FV2] GiveBlueprint -> true (table)`.

## Where to look

- `BepInEx\logs\FurnitureV2Test.log`: the `[FV2]` lines, including the ids of the test furniture and blueprints.
- `BepInEx\logs\obenseuer.moddingframework.furniture.log`: warnings and errors from the furniture code.
- `BepInEx\LogOutput.log`: everything BepInEx logs, including whether the plugin loaded.

## Test steps

1. Start the game. Open `obenseuer.moddingframework.furniture.log`. It must not contain "furniture patches not installed". Open `FurnitureV2Test.log`. It must show a table id and a crate id, each with a blueprint id that ends in `/blueprint`.
2. Load a save. Press F8. Both lines must say `false`.
3. Press F6. The line must say `GiveBlueprint -> true (table)`. A "New item" notice appears, and the test table blueprint is in the Items panel under Blueprints. Press F8. The table line must say `true`. Press F6 again. It must say `false`.
4. Hold Shift and press F6. The line must say `GiveBlueprint -> true (crate)`, and the test crate blueprint appears in the Items panel.
5. Go to a furniture crafting bench. The test table blueprint is listed with its inputs (2 planks, 4 nails and a blue plastic barrel) and its time (600 seconds). Without the inputs the craft must be refused. Get the items and craft it. The test table goes to the building stock.
6. Press F7. The line must say `GiveFlatpack -> true (table, 2)`. Two items called "Test Mod Table in Furniture Flatpack" are in your inventory.
7. Save, quit to the main menu and load the save. The blueprints are still listed (F8 says `true` for the table), and the flatpacks are still there. For a save file check, open `Globals.tnmt` in a text editor. The entries for the `obx:obenseuer.moddingframework.test.furniturev2` blueprints have `"result":null`.
8. Unpack one flatpack. The test table is in the building stock.
9. Remove the plugin only. Quit the game, move the `FurnitureV2Test` folder out of `BepInEx\plugins` to another folder, and start the game. Load the save. There must be no crash. The test blueprints are gone from the Items panel and from the bench. The packed flatpacks keep their name. Unpack one: a Wooden Table still titled "Test Mod Table" is in the building stock. Save.
10. Put the plugin back: move the `FurnitureV2Test` folder into `BepInEx\plugins` again. Load the save. The test blueprints are listed and craftable again, and the unpacked table is still in the building stock.
11. In one session, load the same save three times. The bench lists each blueprint once.

To remove the test, delete the `BepInEx\plugins\FurnitureV2Test` folder.
