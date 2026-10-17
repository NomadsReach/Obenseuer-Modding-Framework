---
title: Presence
parent: Multiplayer
grand_parent: Online play
nav_order: 6
---

# Presence

This page uses `Vector3` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

Presence shares where each player is, so your mod can show other players in its world. You publish your own position, and the framework sends it to everyone in the lobby ten times a second.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Presence</code> <span>share and read player positions</span></p>

Call `Publish` from `Update` to share your position, and read `Remotes` for the other players' positions.

<div class="api-pair" markdown="1">

```csharp
public static class Presence
{
    public static bool Publish(string area, Vector3 position, float yaw, bool moving);
    public static List<PresenceRecord> Remotes { get; }
}
```

| Method | Returns | What it does |
|---|---|---|
| `Publish(area, position, yaw, moving)` | `bool` | Shares your own position. Returns `true` when you are in a lobby. |

| Property | Type | What it means |
|---|---|---|
| `Remotes` | `List<PresenceRecord>` | One record for each other player who has sent a position. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>PresenceRecord</code> <span>the last position another player sent</span></p>

`Presence.Remotes` gives you one of these for each other player who has sent a position. Each new position replaces the record.

<div class="api-pair" markdown="1">

```csharp
public sealed class PresenceRecord
{
    public ulong Peer;
    public string Name;
    public string Area;
    public Vector3 Position;
    public float Yaw;
    public bool Moving;
    public float LastSeen;
}
```

| Property | Type | What it means |
|---|---|---|
| `Peer` | `ulong` | The player's SteamID64. |
| `Name` | `string` | The player's name, cleaned (see below). |
| `Area` | `string` | The area name the player sent, cleaned. `"?"` when it was empty. |
| `Position` | `Vector3` | Where the player is, in world coordinates. |
| `Yaw` | `float` | Which way the player faces. |
| `Moving` | `bool` | `true` while the player is moving. |
| `LastSeen` | `float` | When the sample arrived, in `Time.unscaledTime` seconds. |

</div>
</div>

## Things to know

- `Publish` shares your position for as long as you keep calling it. It stops one second after your last call. Call it from `Update`. <span class="tech">(The framework sends the sample to every peer at 10 Hz on the `presence` channel, unreliable, so a lost message is not resent because a newer one follows.)</span>
- `Remotes` has one record for each other player who sent a valid position. A record is removed after 5 seconds of silence, or when the player is no longer a checked peer.
- A record is replaced for each new position, so a record you hold does not change under you. Each read of `Remotes` or `Peers` gives you a new list, so read it once per frame, not once for every check. <span class="tech">(The wire payload is `area|x|y|z|yaw|moving`, with positions in `F3`, yaw in `F1` and `moving` as `0` or `1`. A sample that does not parse is dropped.)</span>
- Names and areas from other players are cleaned, but still treat them as untrusted text. The framework removes control characters, invisible characters, characters that reorder text, and `<` and `>`, so no formatting tag or scrambled text reaches your screen.
- The framework keeps at most 64 characters of a name or area. A presence area that is empty after that is `?`. `Steam.Name` and `Lobby.MemberName` clean text the same way. <span class="tech">(It removes control characters, zero-width and bidirectional-control characters, and `<` and `>`, and never splits a surrogate pair.)</span>

## Limits

- `area` must be 1 to 64 characters with no `|`, and `position` and `yaw` must be real numbers within 100000 of zero. Otherwise `Publish` throws an error.
