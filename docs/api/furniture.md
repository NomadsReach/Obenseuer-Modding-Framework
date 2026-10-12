# Obenseuer Modding Framework furniture API (custom furniture v1)

Experimental. Not checked in game.
Game: Obenseuer 0.5.05 (build 25839246), Unity Mono.
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.furniture`. Logs go to `BepInEx\logs\obenseuer.moddingframework.furniture.log`.

Everything not listed here is internal. Main thread only.

Scope: a mod furniture is a copy of one vanilla `Furniture` with its own title, details, price, icon and shop listing. It keeps the template's prefabs and behaviour. With `Model` set, its mesh is a mod OBJ model (see Models); otherwise it keeps the template's mesh. Flatpack items, blueprints and a console list are not in v1.

## Rules

- **Template.** `TemplatePath` is the Addressables key of a vanilla furniture, for example `Assets/Content/Prefabs/Building System/Furniture/Tables/Wooden Table.asset`. `TemplateName` is the asset's name, looked up in the developer console's furniture list. Set exactly one. Surfaces and trains are refused. A `TemplateName` whose asset has no addressable path is refused at the first build.
- **Saved identity.** A mod furniture is saved with the id `obx:<modGuid>/<key>`. Its path stays the template's path, so without the mod the game loads the template and no placed piece is lost. Reinstalling the mod restores the real model.
- **Placeholder.** With Core present and the mod missing, a saved piece loads as its template, keeps its id, title and prices, and logs one warning per id. Placing it again is expected to give the plain template; unverified in game.
- **Define before the first build.** `Define` returns null once a furniture has been built or a save with mod furniture has loaded. The same modGuid and key return the same id and keep the first definition.
- **Shops.** A mod furniture is added to each listed shop at every restock, and once to an existing save on load. A piece the player bought out does not come back on load. A load that never finishes leaves the manifest unwritten until the main menu resets it, so stock sold in that session can come back on the next load. A listed shop buys the piece back.
- **Writes.** `Give` and shop stock wait while `SaveGate.Busy` and while a save loads. Shop stock queued during a load is applied once the furniture state is restored, or at the next in-game hour.
- **Templates are checked at first build.** Addressables may not be ready in `Define`, so an unknown or unusable template is logged when the first piece is built, not refused by `Define`.
- **Bad arguments.** An invalid modGuid or key, a Title or Details outside its length, a price below -1, or `ShopStock` below 1 throws `ArgumentException`. `Give` with an amount below 1 or above 1000 throws too.

## API

```csharp
public static class FurnitureRegistry
{
    public static bool IsAvailable { get; }
    public static string IdFor(string modGuid, string key);
    public static string Define(string modGuid, string key, FurnitureDefinition definition);
    public static bool Give(string furnitureId, int amount);
    public static List<string> ShopGuids();
    public static event Action Loaded;
}

public sealed class FurnitureDefinition
{
    public string TemplatePath;
    public string TemplateName;
    public string Title;                             // 1 to 200 characters, null keeps the template's
    public string Details;                           // up to 2000 characters, null keeps the template's
    public int PriceOC = -1;                         // 0 or more, -1 keeps the template's
    public int PriceRM = -1;
    public Sprite Icon;                              // null keeps the template's image
    public string[] ShopIds;                         // BaseShop.GUID values
    public int ShopStock = 3;                        // 1 or more, per listed shop, at each restock
    public ModelHandle Model;                        // null keeps the template's mesh, else a handle from ModelRegistry.LoadObj
}
```

- `IsAvailable` is `patches installed && !CorePatcher.IsDisabled(owner)`. `Define` needs it. `IdFor`, `Give` and `ShopGuids` do not.
- `IdFor` uses the item registry's character rules (`ModIds`): letters, digits, `.`, `_` and `-`, 1 to 64 characters each. The key may not contain `/`.
- `Give` adds `amount` pieces to the player's stock and returns false while `SaveGate.Busy` or a save is loading, when the id is unknown, or when no building is loaded. A failure inside `Give` is logged once and returns false.
- `Loaded` is raised each time the furniture state is restored: when a save loads, and when the main menu resets it.
- `ShopGuids` lists the furniture shops in the loaded levels.

## Models

- **Handle.** `Model` takes a handle from `ModelRegistry.LoadObj` (see `registry.md`, Models). Any other handle throws `ArgumentException` in `Define`. A handle that is not ready when the piece is first built logs one error, and the template's mesh is kept. The piece is still built, sold and placed.
- **Size.** The model keeps its real size times `ModelOptions.Scale`. `FitToTemplate` is ignored: a furniture model is never scaled to the template.
- **Standing.** The model's bottom centre (x and z centre, lowest y) is put on the template's visible bottom centre, so the model stands where the template stood. The alignment cancels `Pivot` and `PositionOffset`, so they do not move the model on the piece. `EulerOffset` turns it.
- **Front.** A model's front is +Z, Unity's forward. For a typical OBJ export, check that the backrest is on -Z and the open front is on +Z after `FlipX`. Use `EulerOffset` to turn a model that faces another way. Sitting does not read the model's transform, so this only changes how the piece looks.
- **Template look.** Every renderer under the template's child 0 is disabled, not destroyed. This includes skinned, particle and line renderers, so anything the template draws inside child 0 is hidden. A renderer outside child 0 stays visible and is logged once per build.
- **Colliders.** The template's solid (non-trigger, enabled) colliders are disabled. Each GameObject that had one gets one `BoxCollider` fitted to the model, on that same GameObject, so interaction and preview keep the same owner. A template with no solid collider gets one box on child 0. `ModelOptions.Collider` is ignored. A box has its GameObject's layer and its first solid collider's material. A solid collider outside child 0 is disabled and logged once per build.
- **Triggers.** Trigger colliders stay enabled, so interaction zones, seats and attach points keep the template's positions. They may not fit the new shape.
- **Preview and shop.** `previewPrefab` and `customDisplayPrefab` get the model like `prefab`, so the preview and the shop display show it.
- **Saves.** Nothing new is saved. Without the mod the piece loads as its template, as in v1.
- **Not checked in game.** The whole section.
