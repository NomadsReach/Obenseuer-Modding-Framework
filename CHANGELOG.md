# Changelog

## 0.9.0-beta

First public release.

### Included

#### Game API

- More than 70 game API areas, listed in `docs/API.md`, with one page per area in `docs/api/`.
- Keypads and lockpicking (`Locks`), track switches and wagon loaders (`Trains`), blackout wake-up (`WorldPower.ScheduleWakeUp`, `WorldPower.WakeUp`), `Janitors.All`, and the orange mushroom effect (`Effect.OrangeMushroom`).
- `PlayerLook`, the player's look and costume API.
- Banking (`Banking`), and a bank ledger (`BankLedger`) that keeps the balance changes of each bank account, saved with each save.
- Story API (experimental): Quests, Dialogue and Law. See `docs/api-story.md`.
- Net (experimental): Steam, lobby and presence helpers. See `docs/api/net.md`.

#### Items, recipes and models

- Registry: mod-defined items and recipes. See `docs/api/registry.md`.
- OBJ models with `ModelRegistry.LoadObj`, set on an item with `ItemDefinition.Model`. A bad model file does not throw: `ModelHandle.IsReady` is false and `ModelHandle.Error` says why.
- PNG item icons with `ItemDefinition.IconPath`.
- A dropped mod model gets a rigidbody with gravity, so it falls to the floor.

#### Furniture

- Custom furniture: `FurnitureRegistry.Define` adds a copy of one vanilla furniture, with its own title, details, price, icon and shop listing.
- `FurnitureDefinition.Model` sets an OBJ model on a mod furniture.
- `FurnitureRegistry.Give` adds pieces to the player's stock, and `FurnitureRegistry.Loaded` is raised each time the furniture state is restored.

#### Shops, loot and mail

- `ShopStock`: mod items in traders and vending machines, with fixed stock per shop.
- `LootPools.DefineCategory` adds a spawn category for a mod.
- `Mail.SendBundle` sends several item lines as one letter or package, delivered after a delay, and `Mail.Pending` lists the ones still waiting.

#### WebUi, Mods menu and computer apps

- WebUi: mod UIs in HTML and CSS, shown in a WebView2 view (`ObxWebView.dll`), with a shared theme, in light and dark. Views can sit on the in-game computer screen. See `docs/api/webui.md`.
- The Mods menu web page is styled like the game's own menu, with page tabs, a sidebar of mods, a settings search box and a key hint footer. The classic window stays available with `[UI] ModMenuStyle`.
- The UI kit (`ModUi`) works in any host, and its theme (`UiTheme`) is public.
- Computer app registry: mods add icons to the desks, open full-screen app windows, and get `ComputerApps.AppClosed` when a window closes.

#### Core

- Per-save data with `SaveData`, and `SaveData.Busy`, true while the game loads and in the frame a save starts. A save data file that cannot be read is copied to `.bad`, and the mod starts empty.
- Safe patching through `CorePatcher`, with a game build check and `[Safety] RefuseUnknownBuild`.
- Optional gameplay tweaks, all off by default.
- `[Debug]` flags `ModIdsSelfCheck`, `MailSelfCheck` and `WebMenuSelfCheck` run offline self-checks at start and log the result.
