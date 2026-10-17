---
title: Lobby
parent: Multiplayer
grand_parent: Online play
nav_order: 4
---

# Lobby

A lobby is a group of players who are connected to each other so their mods can talk. One player hosts it, and the others join. There is one lobby for all mods at a time.

```csharp
public enum LobbyStatus
{
    Offline,   // not in a lobby
    Pending,   // a host or join request is still working
    Hosting,   // you made the lobby
    Joined     // you are in someone else's lobby
}

public static class Lobby
{
    public const int MaxMembers = 32;              // the most players a lobby can hold

    public static bool IsAvailable { get; }        // true while Steam is ready
    public static LobbyStatus Status { get; }      // where you are now
    public static string Error { get; }            // the text of the last failure; "" when none
    public static List<ulong> Members { get; }     // everyone in the lobby, including you and any player that was refused
    public static ulong Owner { get; }             // the host's SteamID64; 0 when not in a lobby or Steam is not ready
    public static bool IsHost { get; }             // true when you are in a lobby and you are its host

    public static bool Host(string gameTag, string version, int maxMembers, bool openInvite);   // makes a lobby
    public static bool Join(ulong lobby, string gameTag, string version);                       // joins a lobby
    public static bool Leave();                    // leaves any lobby
    public static bool Leave(string gameTag);      // leaves only when the lobby belongs to this game tag
    public static bool Invite();                   // opens the invite window for the lobby you host
    public static string MemberName(ulong member); // a member's name; "" when not in a lobby
    public static bool IsRefused(ulong member);    // true when the host refused this member because of a version mismatch

    public static event Action<ulong> MemberJoined;   // a checked player has joined; you get their SteamID64
    public static event Action<ulong> MemberLeft;     // a checked player has left or was refused
    public static event Action<ulong, ulong> OwnerChanged;   // the host changed: (previous host, new host)
    public static event Action<ulong> JoinRequested;  // the player accepted a Steam invite; you get the lobby's id
}
```

- **`gameTag` and `version` name your mod's game.** Both are 1 to 32 characters of letters, digits, `.`, `_` and `-`. Players can only join a lobby that has the same tag and the same version as theirs. `gameTag` also keeps one mod from ending another mod's lobby (see `Leave`).
- **`Host` makes a lobby that only friends can join.** `maxMembers` is from 1 to 32. `openInvite` opens the Steam invite window for you. If you are already hosting a lobby with the same game tag, version and size, `Host` only opens the invite window (when `openInvite` is set) and returns `true`. With any other values it returns `false` and leaves the lobby as it is, so leave the lobby first. It also returns `false` while a request is waiting, and when you are in someone else's lobby.
  - Technical: it sets the lobby data `game` to `gameTag`, `v` to `version` and `core` to the framework version, and it sets the connect rich presence, which is what lets friends join from Steam.
- **`Join` joins a lobby by its id.** It needs your game tag and version because the framework checks that the host runs the same ones. It returns `false` for a lobby id of `0` or while a request is waiting, and `true` when you are already in that lobby. It leaves your current lobby first. After Steam confirms the join, `Status` stays `Pending` for up to 3 seconds while the lobby's information arrives. A lobby that is already for another game tag is refused at once.
- **The framework does not join a lobby by itself when the player accepts an invite.** `JoinRequested` tells your mod that the player accepted one, and your mod decides to call `Join`. If nothing subscribes to the event, the request is ignored.
  - Technical: it is raised for a Steam invite and for the launch option `+connect_lobby <id>`.
- **`Leave` ends whatever you are doing with the lobby.** It cancels a waiting request, leaves the lobby, closes the connection to each checked player, and raises `MemberLeft` for each of them. It returns `true`.
- **`Leave(gameTag)` is the safe version, so use it instead of `Leave()`.** It does nothing and returns `false` when the lobby or a waiting request belongs to another game tag, so one mod can't end another mod's session. Otherwise it works like `Leave()`.
- **There is one lobby at a time for all mods.** If a `Join` leaves your current lobby, or a `Host` is refused because the lobby you host has other settings, the framework writes a warning with both game tags to the log. Check `Status` before you host or join.
- **`Invite` opens the invite window for the lobby you host.** It returns `false` when `Status` is not `Hosting`.
- **`MemberName` returns a player's lobby name.** If that is missing, it returns their Steam name, and otherwise `""`. Every player sets their own name when they host or join, capped at 64 characters, so a name can be missing or late. Show a fallback until it arrives. The text is cleaned the same way as the other text from other players (see "Presence"). It never throws.
- **A request that takes more than 20 seconds fails.** `Error` then says "The lobby request timed out.", and a success that arrives late is left at once.
- **`Members` is refreshed every second and whenever Steam reports a change.** It includes players that were refused for a version mismatch, because Steam has no way to kick someone from a lobby. Use `Net.Peers` for the players you can really talk to.
- **`Owner` reads the host from Steam each time you ask.** It is `0` while `Status` is `Offline` or `Pending`, so a join is not reported as hosted until it is checked. `IsHost` is `true` only when `Owner` is not `0` and is you.
- **`OwnerChanged` fires when the host changes.** The first host seen after you join is reported as `(0, owner)`, so ignore events where the previous host is `0`. When you become the host because the old one left, `Status` becomes `Hosting` and the connect rich presence is set, as `Host` does, so `Invite` works.
  - Technical: it is raised from the `Members` refresh (every second and on each Steam member change) when `Owner` differs from the last owner seen. The last owner resets to `0` on `Leave` and when the lobby is cleared.
- **`Lobby.IsAvailable` is the same as `Steam.IsReady`.** The lobby has no changes to the game of its own, so it works whenever Steam is ready.
