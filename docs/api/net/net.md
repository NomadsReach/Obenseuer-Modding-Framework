---
title: Net
parent: Multiplayer
grand_parent: Online play
nav_order: 3
---

# Net

`Net` sends short text messages to other players in the lobby. Each message goes on a channel, a name you choose, so different mods and different features do not mix. Your mod registers a handler for a channel, and the framework runs it each time a message arrives there.

```csharp
public static class Net
{
    public const int MaxPacketBytes = 1024;        // the biggest a message can be: channel, a "|", and the text, counted in UTF-8 bytes
    public const int MaxIdentifierLength = 32;     // the longest a channel, game tag or version can be

    public static ulong Me { get; }                // the local player's SteamID64 (the same as Steam.LocalId)
    public static List<ulong> Peers { get; }       // the players you can send to: checked lobby members, not including you
    public static long Dropped { get; }            // how many messages were thrown away because a sender's queue was full, since the game started

    // Sends text to one player on a channel. reliable true means Steam resends it until it arrives.
    // Returns true when the message was sent.
    public static bool Send(ulong peer, string channel, string payload, bool reliable);

    // Gives your code to run when a message arrives on this channel. handler gets the sender's SteamID64 and the text.
    // Returns true when it was registered.
    public static bool Register(string modId, string channel, Action<ulong, string> handler);
}
```

- **`Send` returns `false` when it could not send.** That is when the player is not one of `Net.Peers`, when the message is over `MaxPacketBytes`, or when Steam refuses it. It throws an error when `payload` is `null` or `channel` is not a valid name.
- **Your handler runs on the main thread, once the framework has read the message.** It gets the sender's SteamID64 and the text, without the channel name. If your handler throws an error, the framework logs it with the handler's name, and the other handlers still run.
  - Technical: handlers run during the `Update` of the framework's driver. Packets are read for up to 2 ms per frame, and at most 4 per sender are handed to handlers each frame.
- **If a player sends faster than your handler keeps up, messages wait in a queue, and the newest are thrown away when it is full.** Up to 1024 messages wait for each sender, and they are handed out in order. When the queue is full, the oldest 1024 are kept and the newest are dropped. `Net.Dropped` counts every message dropped this way. The log gets a line at most once a minute for each sender. The queue is cleared when the framework leaves its lobby.
  - Technical: a packet over the per-sender budget is handed out first on the next frame, and a newer packet from that sender waits behind it. The log line reads `net: dropped N packets from <SteamID64>`, where N is that sender's drops since its last line. A queue's packets are also dropped when the sender is no longer a peer.
- **`Register` returns `false` when another mod already uses the channel.** The framework then writes one warning that names both mods. If the same `modId` registers the same channel again, the new handler replaces the old one. The channels `presence` and `blob` belong to the framework, and the framework's own id `obenseuer.moddingframework` can't register at all, so `Register` returns `false` for both.
- **The message is plain text, and the framework never turns it into game objects.** A message over `MaxPacketBytes` is dropped, and so is a message on a channel that nobody registered. Send structured data as your own text format and check it when it arrives.
  - Technical: the 1024 limit is the framework's own limit. It counts the whole packet in UTF-8 bytes, so a long channel name leaves less room for text.
- **All mods share one Steam channel, and the framework sorts the messages by channel name.**
  - Technical: Steam P2P channel 7.
