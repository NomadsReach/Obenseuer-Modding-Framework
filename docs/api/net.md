# Obenseuer Modding Framework net API (Steam, Net, Lobby, Presence)

Status: experimental. Not run in game yet.
Game: Obenseuer 0.5.05 (build 25839246), Unity Mono, Steamworks.NET in `Assembly-CSharp-firstpass.dll`.
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.net`, written to `BepInEx\logs\obenseuer.moddingframework.net.log`.

Everything not listed here is internal.

## Rules

- **Main thread only.** Every member is called from Unity's main thread. Steam callbacks and packet reads run from the Core driver's `Update`.
- `Lobby.IsAvailable` is `Steam.IsReady`. Lobby has no patches and no owner. Its events come from Steam callbacks, so it is available only while Steam is ready.
- **Safe at any time.** Every member can be called on the main menu, while loading, and with no save loaded. Reads return a neutral default (`0`, `false`, `""`, an empty array). Writes return `false`.
- **Bad arguments throw.** Only `ArgumentException` and `ArgumentNullException` are thrown, for an invalid argument. Mod ids must be 1 to 64 characters of `A-Z a-z 0-9 . _ -`. Channels, game tags and versions must be 1 to 32 characters of the same set.
- **Steam does the transport.** Core does not call `SteamAPI.RunCallbacks`. The game's `SteamManager` already does, in its `Update`, so Core does not pump it a second time.
- **One transport for all mods.** Every mod's traffic shares one Steam P2P channel (7). Messages are demuxed by channel name.
- **Dormant until used.** Core Net does nothing at startup. The Steam callbacks and the driver start only after `Net.Register` succeeds, `Lobby.Host` or `Lobby.Join` is called, or something subscribes to `Lobby.JoinRequested`. Core reads the Steam socket only while it is in a lobby of its own (`Hosting` or `Joined`), so a mod that reads Steam P2P channel 7 itself keeps all its packets until a Core-based mod hosts or joins. Do not run a Core lobby and another mod's own lobby in the same session.
- **Only lobby members.** Inbound packets from a non-peer are dropped, and P2P session requests from a non-peer are refused. Outbound `Send` to a non-peer returns `false`.

## Steam

```csharp
public static class Steam
{
    public static bool IsReady { get; }                 // the game's SteamManager reports SteamAPI initialized
    public static ulong LocalId { get; }                 // own SteamID64, 0 when not ready
    public static string Name(ulong steamId);           // persona name, "" when unknown or not ready
    public static List<ulong> FriendsPlaying(uint appId);   // immediate friends whose current game is appId
}
```

- `IsReady` reads the private `m_bInitialized` of the game's `SteamManager` through reflection. It does not use `SteamManager.Initialized`, because that getter creates a `SteamManager` object when none exists. It is checked at most once per second until it is true.
- `FriendsPlaying` is a plain scan of the immediate friend list. Use `951240` for Obenseuer.

## Net

```csharp
public static class Net
{
    public const int MaxPacketBytes = 1024;             // channel + '|' + payload, UTF-8 bytes
    public const int MaxIdentifierLength = 32;
    public static ulong Me { get; }                     // Steam.LocalId
    public static List<ulong> Peers { get; }                // verified lobby members, excluding me
    public static bool Send(ulong peer, string channel, string payload, bool reliable);
    public static bool Register(string modId, string channel, Action<ulong, string> handler);
}
```

- `Send` returns `false` when `peer` is not in `Net.Peers`, when the packet is over `MaxPacketBytes`, or when Steam refuses the send. `payload` must not be null, and `channel` must be a valid identifier, or it throws.
- `Register` returns `false` when another mod already owns `channel`. The same `modId` registering the same channel again replaces its handler. The channel `presence` belongs to Core, so `Register` returns `false` for it.
- A handler runs on the main thread during the Core driver's `Update`, with the sender's SteamID64 and the payload with the channel prefix removed. Up to 16 packets are read per frame, and only while Core is in a lobby. A handler that throws is logged with the handler's name, and the other handlers still run (`Events.Raise`).
- The payload is UTF-8 text. Core never deserializes types from the wire. Packets over `MaxPacketBytes` are dropped, and so are packets whose channel is not registered.
- The 1024 cap is Core's rule. It is chosen to stay under the roughly 1200-byte unreliable limit that I recall from the Steamworks P2P documentation; that figure was not re-checked here.

## Lobby

```csharp
public enum LobbyStatus { Offline, Pending, Hosting, Joined }

public static class Lobby
{
    public const int MaxMembers = 32;
    public static LobbyStatus Status { get; }
    public static string Error { get; }                  // last failure text, "" when none
    public static List<ulong> Members { get; }               // every member of the lobby, including me and refused ones
    public static bool Host(string gameTag, string version, int maxMembers, bool openInvite);
    public static bool Join(ulong lobby, string gameTag, string version);
    public static bool Leave();
    public static bool Invite();
    public static event Action<ulong> MemberJoined;      // a verified peer appeared
    public static event Action<ulong> MemberLeft;        // a verified peer left or was refused
    public static event Action<ulong> JoinRequested;     // Steam invite accepted or +connect_lobby, lobby id
}
```

- `Host` creates a friends-only lobby with `maxMembers` slots (1 to 32), sets the lobby data `game` = `gameTag`, `v` = `version` and `core` = the Core version, and sets the connect rich presence. `openInvite` opens the Steam invite overlay. Calling it while already hosting only opens the overlay, when `openInvite` is set. It returns `false` while a request is pending, and when joined to another lobby.
- `Join` takes the game tag and version of the mod that joins, because the version check needs them. It returns `false` for `lobby == 0` or while a request is pending, and `true` when already in that lobby. It leaves the current lobby first. After Steam confirms the join, `Status` stays `Pending` for up to 3 s while the lobby data (`game`, `v`, `core`) syncs; a lobby whose `game` is already another tag is refused at once.
- `JoinRequested` is raised for a Steam invite and for `+connect_lobby <id>`. Core does not join on its own. A mod subscribes and calls `Join` with its own tag and version. If nothing subscribes, the request is ignored.
- `Leave` abandons a pending request, leaves the lobby, closes the P2P session of each verified peer, and raises `MemberLeft` for each of them. It returns `true`. A peer that stops being verified while the lobby stays also has its session closed.
- `Invite` opens the overlay for the hosted lobby. It returns `false` when `Status` is not `Hosting`.
- Requests time out after 20 s and set `Error` to "The lobby request timed out.". A late success is left at once.
- `Members` is refreshed every second and on each Steam member change.

### Version check

Each side sets its Core version and mod version as lobby data (host) and as member data (every member).

- **Joining.** `Join` leaves the lobby and sets `Error` when the lobby's `game` is not `gameTag`, or when its `v` or `core` is not the joiner's values. The messages are:
  - `Not a <gameTag> lobby.`
  - `Version mismatch: the lobby runs <hostVersion> (Core <hostCore>), you run <version> (Core <coreVersion>).`
- **Members.** A member whose member data `v` or `core` differs from ours is not a peer. Its packets are dropped, its session requests are refused, and it is missing from `Net.Peers`. Steam has no kick, so the member stays in `Members`. A refusal is logged once when the count changes: `<n> member(s) refused: mod version is not <version> or Core is not <coreVersion>`.
- A P2P session request from a lobby member whose member data has not synced yet is held, and accepted as soon as `Refresh` verifies that member. A request from anyone who is not a lobby member is ignored.
- The version data is set by the other side, so this is a compatibility guard, not protection against a modified client.

## Presence

```csharp
public sealed class PresenceRecord
{
    public ulong Peer;
    public string Name;
    public string Area;
    public Vector3 Position;
    public float Yaw;
    public bool Moving;
    public float LastSeen;      // Time.unscaledTime of the last sample
}

public static class Presence
{
    public static List<PresenceRecord> Remotes { get; }
    public static bool Publish(string area, Vector3 position, float yaw, bool moving);
}
```

- `Publish` stores the local sample. Core sends it to every peer at 10 Hz on the `presence` channel, unreliable, for as long as `Publish` keeps being called (it stops one second after the last call). It returns `true` when in a lobby.
- `area` must be 1 to 64 characters with no `|`. `position` and `yaw` must be finite and within ±100000, or it throws.
- The wire payload is `area|x|y|z|yaw|moving`, with positions in `F3`, yaw in `F1`, and `moving` as `0` or `1`. Inbound samples that do not parse are dropped.
- `Remotes` holds one record per peer that has sent a valid sample. A record is removed after 5 s of silence or when the peer is not a verified peer. Records are replaced on each sample, so a record you hold does not change.
