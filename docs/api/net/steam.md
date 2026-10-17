---
title: Steam
parent: Multiplayer
grand_parent: Online play
nav_order: 2
---

# Steam

This is the full list of what the API gives you, one section for each part. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

A few words used on this page. A SteamID64 is the long number that identifies one Steam account. A peer is another player in your lobby whom the framework has checked and accepts. A handler is the piece of your code that the framework runs when something arrives.

`Steam` tells you whether Steam is working and who the local player and their friends are.

```csharp
public static class Steam
{
    public static bool IsReady { get; }                    // true when Steam is running and the game has started it
    public static ulong LocalId { get; }                   // the local player's SteamID64; 0 when Steam is not ready
    public static bool OverlayEnabled { get; }             // true when the Steam overlay can show; false when Steam is not ready
    public static string Name(ulong steamId);              // a player's Steam name; "" when unknown or not ready
    public static List<ulong> FriendsPlaying(uint appId);  // friends who are playing this game right now
}
```

- **`IsReady` tells you whether Steam is up.** Until it is `true`, hosting and joining a lobby do not work.
  - Technical: it reads the private `m_bInitialized` of the game's `SteamManager` through reflection. It does not use `SteamManager.Initialized`, because that getter creates a `SteamManager` object when none exists. It is checked at most once per second until it is `true`.
- **`Name` can be empty at first, so ask again on a later frame.** For someone who is not your friend, Steam may not know the name yet. The first call asks Steam for it and returns `""`, and the name arrives a moment later. The text is cleaned the same way as the other text from other players (see "Presence").
  - Technical: it calls `SteamFriends.RequestUserInformation(id, true)` when Steam reports the name as empty or `[unknown]`.
- **`OverlayEnabled` lets you tell the player why an invite can't show.** It is `false` when Steam is not ready or the overlay is turned off.
- **`FriendsPlaying(appId)` lists your friends who are in a game right now.** Use `951240` for Obenseuer. It only looks at the player's own friends.
