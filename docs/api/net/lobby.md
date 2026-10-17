---
title: Lobby
parent: Multiplayer
grand_parent: Online play
nav_order: 4
---

# Lobby

A lobby is a group of players who are connected to each other so their mods can talk. One player hosts it, and the others join. There is one lobby for all mods at a time.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Lobby</code> <span>host, join and watch the lobby</span></p>

Call `Host` to make a lobby, `Join` to enter one, and `Leave` to get out. `MemberJoined`, `MemberLeft` and `OwnerChanged` tell you when players come and go.

<div class="api-pair" markdown="1">

```csharp
public static class Lobby
{
    public const int MaxMembers = 32;
    public static bool IsAvailable { get; }
    public static LobbyStatus Status { get; }
    public static string Error { get; }
    public static List<ulong> Members { get; }
    public static ulong Owner { get; }
    public static bool IsHost { get; }
    public static bool Host(string gameTag, string version, int maxMembers, bool openInvite);
    public static bool Join(ulong lobby, string gameTag, string version);
    public static bool Leave();
    public static bool Leave(string gameTag);
    public static bool Invite();
    public static string MemberName(ulong member);
    public static bool IsRefused(ulong member);
    public static event Action<ulong> MemberJoined;
    public static event Action<ulong> MemberLeft;
    public static event Action<ulong, ulong> OwnerChanged;
    public static event Action<ulong> JoinRequested;
}
```

| Method | Returns | What it does |
|---|---|---|
| `Host(gameTag, version, maxMembers, openInvite)` | `bool` | Makes a lobby. |
| `Join(lobby, gameTag, version)` | `bool` | Joins a lobby. |
| `Leave()` | `bool` | Leaves any lobby. |
| `Leave(gameTag)` | `bool` | Leaves only when the lobby belongs to this game tag. |
| `Invite()` | `bool` | Opens the invite window for the lobby you host. |
| `MemberName(member)` | `string` | A member's name. `""` when not in a lobby. |
| `IsRefused(member)` | `bool` | `true` when the host refused this member because of a version mismatch. |

| Property | Type | What it means |
|---|---|---|
| `MaxMembers` | `int` | The most players a lobby can hold. |
| `IsAvailable` | `bool` | `true` while Steam is ready. |
| `Status` | `LobbyStatus` | Where you are now. |
| `Error` | `string` | The text of the last failure. `""` when there is none. |
| `Members` | `List<ulong>` | Everyone in the lobby, including you and any player that was refused. |
| `Owner` | `ulong` | The host's SteamID64. `0` when you are not in a lobby or Steam is not ready. |
| `IsHost` | `bool` | `true` when you are in a lobby and you are its host. |

| Event | Gives you | When it fires |
|---|---|---|
| `MemberJoined` | The player's SteamID64. | A checked player has joined. |
| `MemberLeft` | The player's SteamID64. | A checked player has left or was refused. |
| `OwnerChanged` | The previous host and the new host. | The host changes. |
| `JoinRequested` | The lobby's id. | The player accepts a Steam invite. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>LobbyStatus</code> <span>where you are in a lobby</span></p>

`Lobby.Status` gives you this. Check it before you host or join, because only one lobby is allowed at a time.

<div class="api-pair" markdown="1">

```csharp
public enum LobbyStatus
{
    Offline,
    Pending,
    Hosting,
    Joined
}
```

| Value | Meaning |
|---|---|
| `Offline` | You are not in a lobby. |
| `Pending` | A host or join request is still working. |
| `Hosting` | You made the lobby. |
| `Joined` | You are in someone else's lobby. |

</div>
</div>

## Things to know

- `gameTag` and `version` name your mod's game. Players can only join a lobby that has the same tag and the same version as theirs.
- `gameTag` also keeps one mod from ending another mod's lobby (see `Leave`).
- `Host` makes a lobby that only friends can join. `openInvite` opens the Steam invite window for you. <span class="tech">(It sets the lobby data `game` to `gameTag`, `v` to `version` and `core` to the framework version, and it sets the connect rich presence, which is what lets friends join from Steam.)</span>
- If you are already hosting a lobby with the same game tag, version and size, `Host` only opens the invite window (when `openInvite` is set) and returns `true`.
- `Join` joins a lobby by its id. It needs your game tag and version because the framework checks that the host runs the same ones. It leaves your current lobby first. It returns `true` when you are already in that lobby.
- After Steam confirms the join, `Status` stays `Pending` for up to 3 seconds while the lobby's information arrives.
- The framework does not join a lobby by itself when the player accepts an invite. `JoinRequested` tells your mod that the player accepted one, and your mod decides to call `Join`. If nothing subscribes to the event, the request is ignored. <span class="tech">(It is raised for a Steam invite and for the launch option `+connect_lobby <id>`.)</span>
- `Leave` ends whatever you are doing with the lobby. It cancels a waiting request, leaves the lobby, closes the connection to each checked player, and raises `MemberLeft` for each of them. It returns `true`.
- `Leave(gameTag)` is the safe version, so use it instead of `Leave()`. It does nothing and returns `false` when the lobby or a waiting request belongs to another game tag, so one mod can't end another mod's session. Otherwise it works like `Leave()`.
- There is one lobby at a time for all mods. Check `Status` before you host or join.
- `Invite` opens the invite window for the lobby you host. It returns `false` when `Status` is not `Hosting`.
- `MemberName` returns a player's lobby name. If that is missing, it returns their Steam name, and otherwise `""`. It never throws.
- Every player sets their own name when they host or join, capped at 64 characters, so a name can be missing or late. Show a fallback until it arrives. The text is cleaned the same way as the other text from other players (see "Presence").
- `Members` is refreshed every second and whenever Steam reports a change. It includes players that were refused for a version mismatch, because Steam has no way to kick someone from a lobby. Use `Net.Peers` for the players you can really talk to.
- `Owner` reads the host from Steam each time you ask. It is `0` while `Status` is `Offline` or `Pending`, so a join is not reported as hosted until it is checked. `IsHost` is `true` only when `Owner` is not `0` and is you.
- `OwnerChanged` fires when the host changes. The first host seen after you join is reported as `(0, owner)`, so ignore events where the previous host is `0`. <span class="tech">(It is raised from the `Members` refresh, every second and on each Steam member change, when `Owner` differs from the last owner seen. The last owner resets to `0` on `Leave` and when the lobby is cleared.)</span>
- When you become the host because the old one left, `Status` becomes `Hosting` and the connect rich presence is set, as `Host` does, so `Invite` works.
- `Lobby.IsAvailable` is the same as `Steam.IsReady`. The lobby has no changes to the game of its own, so it works whenever Steam is ready.

## Limits

- `gameTag` and `version` must be 1 to 32 characters of letters, digits, `.`, `_` and `-`.
- `maxMembers` is from 1 to 32. With any other values (a different game tag, version or size from the lobby you already host), `Host` returns `false` and leaves the lobby as it is, so leave the lobby first. It also returns `false` while a request is waiting, and when you are in someone else's lobby.
- `Join` returns `false` for a lobby id of `0` or while a request is waiting. A lobby that is already for another game tag is refused at once.
- If a `Join` leaves your current lobby, or a `Host` is refused because the lobby you host has other settings, the framework writes a warning with both game tags to the log.
- A request that takes more than 20 seconds fails. `Error` then says "The lobby request timed out.", and a success that arrives late is left at once.
