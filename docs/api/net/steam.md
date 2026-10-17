---
title: Steam
parent: Multiplayer
grand_parent: Online play
nav_order: 2
---

# Steam

`Steam` tells you whether Steam is working and who the local player and their friends are. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

A few words used on this page. A SteamID64 is the long number that identifies one Steam account. A peer is another player in your lobby whom the framework has checked and accepts. A handler is the piece of your code that the framework runs when something arrives.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Steam</code> <span>Steam status and friends</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Steam
{
    public static bool IsReady { get; }
    public static ulong LocalId { get; }
    public static bool OverlayEnabled { get; }
    public static string Name(ulong steamId);
    public static List<ulong> FriendsPlaying(uint appId);
}
```

| Method | Returns | What it does |
|---|---|---|
| `Name(ulong steamId)` | `string` | A player's Steam name. `""` when unknown or not ready. |
| `FriendsPlaying(uint appId)` | `List<ulong>` | Friends who are playing this game right now. |

| Property | Type | What it means |
|---|---|---|
| `IsReady` | `bool` | `true` when Steam is running and the game has started it. |
| `LocalId` | `ulong` | The local player's SteamID64. `0` when Steam is not ready. |
| `OverlayEnabled` | `bool` | `true` when the Steam overlay can show. `false` when Steam is not ready. |

</div>
</div>

## Things to know

- `IsReady` tells you whether Steam is up. <span class="tech">(It reads the private `m_bInitialized` of the game's `SteamManager` through reflection. It does not use `SteamManager.Initialized`, because that getter creates a `SteamManager` object when none exists. It is checked at most once per second until it is `true`.)</span>
- `OverlayEnabled` lets you tell the player why an invite can't show. It is `false` when Steam is not ready or the overlay is turned off.
- `FriendsPlaying(appId)` lists your friends who are in a game right now. Use `951240` for Obenseuer. It only looks at the player's own friends.

## Limits

- Until `IsReady` is `true`, hosting and joining a lobby do not work.
- `Name` can be empty at first, so ask again on a later frame. For someone who is not your friend, Steam may not know the name yet. The first call asks Steam for it and returns `""`, and the name arrives a moment later. The text is cleaned the same way as the other text from other players (see "Presence"). <span class="tech">(It calls `SteamFriends.RequestUserInformation(id, true)` when Steam reports the name as empty or `[unknown]`.)</span>
