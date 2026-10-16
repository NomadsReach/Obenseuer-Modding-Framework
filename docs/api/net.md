---
title: Obenseuer Modding Framework net API (Steam, Net, Lobby, Presence, Blob)
parent: API reference
nav_order: 47
---

# Obenseuer Modding Framework net API (Steam, Net, Lobby, Presence, Blob)

Status: experimental.
Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Steamworks.NET comes from the game's own DLLs.
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.net`, written to `BepInEx\logs\obenseuer.moddingframework.net.log`.

Everything not listed here is internal.

Use this page to build online features on top of Steam. `Steam` gives you the local Steam identity, `Lobby` hosts or joins a lobby of players, `Net` sends short messages between lobby members on a channel you name, `Presence` shares each player's position and area, and `NetBlob` sends larger data to one player. Nothing runs until a mod uses it. Every player needs Steam and the same mods.

## Rules

- **Main thread only.** Every member is called from Unity's main thread. Steam callbacks and packet reads run from the `Update` of the framework's driver.
- `Lobby.IsAvailable` is `Steam.IsReady`. Lobby has no patches and no owner. Its events come from Steam callbacks, so it is available only while Steam is ready.
- **Safe at any time.** Every member can be called on the main menu, while loading, and with no save loaded. Reads return a neutral default (`0`, `false`, `""`, an empty array). Writes return `false`.
- **Bad arguments throw.** Only `ArgumentException` and `ArgumentNullException` are thrown, for an invalid argument. Mod ids must be 1 to 64 characters of `A-Z a-z 0-9 . _ -`. Channels, game tags and versions must be 1 to 32 characters of the same set.
- **Steam does the transport.** The framework does not call `SteamAPI.RunCallbacks`. The game's `SteamManager` already does, in its `Update`, so the framework does not pump it a second time.
- **One transport for all mods.** Every mod's traffic shares one Steam P2P channel (7). Messages are demuxed by channel name.
- **Dormant until used.** The framework's `Net` area does nothing at startup. The Steam callbacks and the driver start only after `Net.Register` succeeds, `Lobby.Host` or `Lobby.Join` is called, or something subscribes to `Lobby.JoinRequested`. The framework reads the Steam socket only while it is in a lobby of its own (`Hosting` or `Joined`), so a mod that reads Steam P2P channel 7 itself keeps all its packets until a mod built on the framework hosts or joins. Do not run a framework lobby and another mod's own lobby in the same session.
- **Only lobby members.** Inbound packets from a non-peer are dropped, and P2P session requests from a non-peer are refused. Outbound `Send` to a non-peer returns `false`.

## Steam

```csharp
public static class Steam
{
    public static bool IsReady { get; }                 // the game's SteamManager reports SteamAPI initialized
    public static ulong LocalId { get; }                 // own SteamID64, 0 when not ready
    public static bool OverlayEnabled { get; }          // SteamUtils.IsOverlayEnabled, false when not ready
    public static string Name(ulong steamId);           // persona name, "" when unknown or not ready; see Peer text
    public static List<ulong> FriendsPlaying(uint appId);   // immediate friends whose current game is appId
}
```

- `IsReady` reads the private `m_bInitialized` of the game's `SteamManager` through reflection. It does not use `SteamManager.Initialized`, because that getter creates a `SteamManager` object when none exists. It is checked at most once per second until it is true.
- `Name` asks Steam for the persona name of a non-friend when Steam reports it as empty or `[unknown]`, with `SteamFriends.RequestUserInformation(id, true)`, and returns `""` until the name arrives. Call it again on a later frame.
- `OverlayEnabled` is `false` when Steam is not ready, so a mod can say why an invite cannot show.
- `FriendsPlaying` is a plain scan of the immediate friend list. Use `951240` for Obenseuer.

## Net

```csharp
public static class Net
{
    public const int MaxPacketBytes = 1024;             // channel + '|' + payload, UTF-8 bytes
    public const int MaxIdentifierLength = 32;
    public static ulong Me { get; }                     // Steam.LocalId
    public static List<ulong> Peers { get; }                // verified lobby members, excluding me
    public static long Dropped { get; }                 // packets dropped because a sender's queue was full, since start
    public static bool Send(ulong peer, string channel, string payload, bool reliable);
    public static bool Register(string modId, string channel, Action<ulong, string> handler);
}
```

- `Send` returns `false` when `peer` is not in `Net.Peers`, when the packet is over `MaxPacketBytes`, or when Steam refuses the send. `payload` must not be null, and `channel` must be a valid identifier, or it throws.
- A handler runs on the main thread during the `Update` of the framework's driver, with the sender's SteamID64 and the payload with the channel prefix removed. Packets are read for up to 2 ms per frame, and at most 4 per sender are handed to handlers each frame. A packet over that per-sender budget waits in a queue for its sender, which holds 1024 packets, and is handed out first, in order, on the next frame; a newer packet from that sender waits behind it. When a sender's queue is full, its newest packets are dropped and the oldest 1024 are kept. `Net.Dropped` counts every such drop. A drop is logged at most once a minute per sender as `net: dropped N packets from <SteamID64>`, where N is that sender's drops since its last line, never once per packet. A queue is cleared when the framework leaves its lobby, and its packets are dropped when the sender is no longer a peer. Packets are only read while the framework is in a lobby. A handler that throws is logged with the handler's name, and the other handlers still run.
- `Register` returns `false` when another mod already owns `channel`, and logs one warning that names both mods. The same `modId` registering the same channel again replaces its handler. The channels `presence` and `blob` belong to the framework, and the framework's own id `obenseuer.moddingframework` cannot register at all, so `Register` returns `false` for both.
- The payload is UTF-8 text. The framework never deserializes types from the wire. Packets over `MaxPacketBytes` are dropped, and so are packets whose channel is not registered.
- The 1024 cap is the framework's own limit, `Net.MaxPacketBytes`. It counts the whole packet in UTF-8 bytes: the channel, the `|`, and the payload.

## Lobby

```csharp
public enum LobbyStatus { Offline, Pending, Hosting, Joined }

public static class Lobby
{
    public const int MaxMembers = 32;
    public static LobbyStatus Status { get; }
    public static string Error { get; }                  // last failure text, "" when none
    public static List<ulong> Members { get; }               // every member of the lobby, including me and refused ones
    public static ulong Owner { get; }                   // SteamID64 of the lobby owner, 0 when not in a lobby or Steam is not ready
    public static bool IsHost { get; }                   // in a lobby and Owner is me
    public static bool Host(string gameTag, string version, int maxMembers, bool openInvite);
    public static bool Join(ulong lobby, string gameTag, string version);
    public static bool Leave();
    public static bool Leave(string gameTag);            // leaves only when the current session has this tag
    public static bool Invite();
    public static string MemberName(ulong member);       // lobby name tag, else Steam.Name, "" when not in a lobby
    public static event Action<ulong> MemberJoined;      // a verified peer appeared
    public static event Action<ulong> MemberLeft;        // a verified peer left or was refused
    public static event Action<ulong, ulong> OwnerChanged; // (previous, current) lobby owner, raised on each change; first owner seen after joining is (0, owner); consumers should ignore previous == 0
    public static bool IsRefused(ulong member);          // the host refused this member for a version or game build mismatch
    public static event Action<ulong> JoinRequested;     // Steam invite accepted or +connect_lobby, lobby id
}
```

- `Host` creates a friends-only lobby with `maxMembers` slots (1 to 32), sets the lobby data `game` = `gameTag`, `v` = `version` and `core` = the framework version, and sets the connect rich presence. `openInvite` opens the Steam invite overlay. Calling it while already hosting with the same `gameTag`, `version` and `maxMembers` only opens the overlay, when `openInvite` is set, and returns `true`. With any other value it returns `false` and leaves the lobby as it is; leave the lobby first. It also returns `false` while a request is pending, and when joined to another lobby.
- `Join` takes the game tag and version of the mod that joins, because the version check needs them. It returns `false` for `lobby == 0` or while a request is pending, and `true` when already in that lobby. It leaves the current lobby first. After Steam confirms the join, `Status` stays `Pending` for up to 3 s while the lobby data (`game`, `v`, `core`) syncs; a lobby whose `game` is already another tag is refused at once.
- `JoinRequested` is raised for a Steam invite and for `+connect_lobby <id>`. The framework does not join on its own. A mod subscribes and calls `Join` with its own tag and version. If nothing subscribes, the request is ignored.
- `Leave` abandons a pending request, leaves the lobby, closes the P2P session of each verified peer, and raises `MemberLeft` for each of them. It returns `true`. A peer that stops being verified while the lobby stays also has its session closed.
- `Leave(gameTag)` returns `false` and does nothing when a lobby or a pending request belongs to another game tag, so one mod cannot end another mod's session. Otherwise it is `Leave()`. Use it in place of `Leave()`.
- There is one lobby for all mods. A `Join` that leaves the current lobby, and a `Host` refused because the hosted lobby has other settings, each log a warning with both game tags in `obenseuer.moddingframework.net.log`. Check `Status` before you host or join.
- `Invite` opens the overlay for the hosted lobby. It returns `false` when `Status` is not `Hosting`.
- `MemberName` returns the member's `name` lobby member data, or `Steam.Name` when that is empty, or `""`. Each member sets `name` to their own persona name when they host or join, capped at 64 characters, so a name can be missing or late; show a fallback until it arrives. The text is untrusted and is cleaned with the same rules as Peer text. It returns `""` when not in a lobby and never throws.
- Requests time out after 20 s and set `Error` to "The lobby request timed out.". A late success is left at once.
- `Members` is refreshed every second and on each Steam member change.
- `Owner` reads the lobby owner from Steam (`SteamMatchmaking.GetLobbyOwner`) on each call. It is `0` while `Status` is `Offline` or `Pending`, so a join is not reported as hosted until it is verified. `IsHost` is `true` only when `Owner` is non-zero and equal to `Steam.LocalId`. A `Host` call sets the owner to me.
- `OwnerChanged` is raised from `Members` refresh (every second and on each Steam member change) when `Owner` differs from the last owner seen. The last owner resets to `0` on `Leave` and when the lobby is cleared, so the first refresh after joining raises `(0, owner)`. Consumers should ignore `previous == 0`. When the owner becomes you (the old host left), `Status` becomes `Hosting` and the connect rich presence is set, as `Host` does, so `Invite` works.

### Version check

Each side sets its framework version, game version and Steam build id as lobby data (host) and as member data (every member). The game version is `Application.version`, stored under `gv`. The Steam build id, from the appmanifest, is stored under `gb`, and is empty when it cannot be read.

- **Joining.** `Join` leaves the lobby and sets `Error` when the lobby's `game` is not `gameTag`, or when its `v` or `core` is not the joiner's values, or when its `gv` is not `Application.version`, or when both sides have a non-empty `gb` and they differ. The messages are:
  - `Not a <gameTag> lobby.`
  - `Version mismatch: the lobby runs <hostVersion> (framework <hostCore>), you run <version> (framework <coreVersion>).`
  - `The host runs game version <hostVersion>, you run <gameVersion>.`
  - `The host runs game build <hostBuild>, you run <steamBuild>.`
- **Members.** A member whose member data `v` or `core` differs from ours, whose `gv` differs from `Application.version`, or whose `gb` and ours are both non-empty and differ, is not a peer, and `Lobby.IsRefused` returns `true` for it. Its packets are dropped, its session requests are refused, and it is missing from `Net.Peers`. Steam has no kick, so the member stays in `Members`. A refusal is logged once when the count changes: `<n> member(s) refused: mod version is not <version>, Core is not <coreVersion>, game version is not <gameVersion> or game build differs`.
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

- `Publish` stores the local sample. The framework sends it to every peer at 10 Hz on the `presence` channel, unreliable, for as long as `Publish` keeps being called (it stops one second after the last call). It returns `true` when in a lobby.
- `area` must be 1 to 64 characters with no `|`. `position` and `yaw` must be finite and within ±100000, or it throws.
- The wire payload is `area|x|y|z|yaw|moving`, with positions in `F3`, yaw in `F1`, and `moving` as `0` or `1`. Inbound samples that do not parse are dropped.
- `Remotes` holds one record per peer that has sent a valid sample. A record is removed after 5 s of silence or when the peer is not a verified peer. Records are replaced on each sample, so a record you hold does not change. Each read of `Remotes` or `Peers` returns a new list, so read it once per frame, not once per check.
- Peer text. `Name` and `Area` come from other players. The framework removes control characters, zero-width and bidirectional-control characters, and `<` and `>` from them, so no rich-text tag or reordered text reaches your UI. It keeps at most 64 characters, never splitting a surrogate pair. A presence `Area` that is empty after that is `?`. Treat them as untrusted text anyway. `Steam.Name` does the same to a persona name.

## Blob

```csharp
public static class NetBlob
{
    public static event Action<ulong, string, string, byte[]> Received;   // peer, modId, name, data
    public static event Action<int, bool> Finished;                       // transfer id, delivered
    public static int Send(ulong peer, string modId, string name, byte[] data);
    public static float Progress(int id);                                  // sender: acknowledged fraction, -1 when not active
    public static float Progress(ulong peer, int id);                      // receiver: received fraction, -1 when not active
    public static bool Cancel(int id);
}
```

- `Send` compresses `data` with gzip and sends it to one lobby peer on the framework's `blob` channel, in base64 chunks that fit `MaxPacketBytes`. It returns the transfer id, or `-1` when `peer` is not a verified peer, the compressed blob needs more than 8192 chunks, the blob is over 16 MB, or 16 transfers are already running. `modId` must be a valid mod id and `name` a 1 to 32 character identifier, or it throws. `data` must not be null. Empty data is sent.
- `Received` runs on the receiver once every chunk has arrived, the gzip decodes and the CRC32 of the uncompressed data matches. A blob that fails the check is dropped, and the sender's `Finished` gets `false`. Any peer can send any `modId`, so a receiver must check the sender (the `peer` argument) before it acts on a blob for its mod.
- `Finished` runs on the sender when the transfer ends: `true` when the receiver has every chunk, `false` on a timeout, a rejection, a cancel, or when the peer leaves the lobby.
- `Progress(id)` is the sender's acknowledged fraction. `Progress(peer, id)` is the receiver's received fraction. Both return `-1` once the transfer is no longer active, so wait for `Finished` or `Received` instead.
- `Cancel(id)` stops a transfer you are sending, tells the receiver, and raises `Finished(id, false)`. It returns `false` for an id that is not active.
- Pacing: at most 120 packets a second to each peer, shared by all its transfers, and at most 32 chunks unacknowledged per peer. The receiver acknowledges every 8 chunks. A sender with no progress for 2 s resends from the last acknowledged chunk. A transfer fails after 60 s with no acknowledged progress, and the log says why.
- Transfer ids are local to the sender. The receiver keys its state by sender and id.
- The receiver drops a partial blob that gets no new chunk for 60 s. A finished blob stops counting toward the 16 running transfers at once. A chunk over 960 characters fails its transfer, and the partial blobs of one peer are capped at 16 MB in memory (2 bytes per character), so a second maximum-size blob from the same peer at the same time is refused. A peer leaving the lobby ends its transfers in both directions.
- The gzip and CRC32 path and the chunking are covered by `NetBlobSelfTest`, which runs with `[Debug] NetBlobSelfCheck`, and so are the receive limits and the progress timeouts.
