---
title: Presence
parent: Multiplayer
grand_parent: Online play
nav_order: 6
---

# Presence

Presence shares where each player is, so your mod can show other players in its world. You publish your own position, and the framework sends it to everyone in the lobby ten times a second.

```csharp
public sealed class PresenceRecord    // the last position another player sent
{
    public ulong Peer;          // the player's SteamID64
    public string Name;         // the player's name, cleaned (see below)
    public string Area;         // the area name the player sent, cleaned; "?" when it was empty
    public Vector3 Position;    // where the player is, in world coordinates
    public float Yaw;           // which way the player faces
    public bool Moving;         // true while the player is moving
    public float LastSeen;      // when the sample arrived, in Time.unscaledTime seconds
}

public static class Presence
{
    public static List<PresenceRecord> Remotes { get; }    // one record for each other player who has sent a position
    public static bool Publish(string area, Vector3 position, float yaw, bool moving);   // shares your own position; true when you are in a lobby
}
```

- **`Publish` shares your position for as long as you keep calling it.** It stops one second after your last call. Call it from `Update`. It returns `true` when you are in a lobby.
  - Technical: the framework sends the sample to every peer at 10 Hz on the `presence` channel, unreliable (a lost message is not resent, because a newer one follows).
- **`area` must be 1 to 64 characters with no `|`, and `position` and `yaw` must be real numbers within 100000 of zero.** Otherwise `Publish` throws an error.
- **`Remotes` has one record for each other player who sent a valid position.** A record is removed after 5 seconds of silence, or when the player is no longer a checked peer. A record is replaced for each new position, so a record you hold does not change under you. Each read of `Remotes` or `Peers` gives you a new list, so read it once per frame, not once for every check.
  - Technical: the wire payload is `area|x|y|z|yaw|moving`, with positions in `F3`, yaw in `F1` and `moving` as `0` or `1`. A sample that does not parse is dropped.
- **Names and areas from other players are cleaned, but still treat them as untrusted text.** The framework removes control characters, invisible characters, characters that reorder text, and `<` and `>`, so no formatting tag or scrambled text reaches your screen. It keeps at most 64 characters. A presence area that is empty after that is `?`. `Steam.Name` and `Lobby.MemberName` clean text the same way.
  - Technical: it removes control characters, zero-width and bidirectional-control characters, and `<` and `>`, and never splits a surrogate pair.
