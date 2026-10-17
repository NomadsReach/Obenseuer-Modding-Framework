---
title: Blob
parent: Multiplayer
grand_parent: Online play
nav_order: 7
---

# Blob

A blob is a larger piece of data, such as a file, sent to one player. The framework compresses it, cuts it into pieces, sends the pieces, checks they all arrived intact and puts the blob back together on the other side.

```csharp
public static class NetBlob
{
    public static event Action<ulong, string, string, byte[]> Received;   // a blob arrived: sender, mod id, name, data
    public static event Action<int, bool> Finished;                       // a blob you sent is done: transfer id, true when delivered

    // Starts sending data to one player. Returns the transfer id, or -1 when it could not start.
    public static int Send(ulong peer, string modId, string name, byte[] data);
    public static float Progress(int id);                  // for a blob you are sending: how much was acknowledged, 0 to 1; -1 when not active
    public static float Progress(ulong peer, int id);      // for a blob you are receiving: how much has arrived, 0 to 1; -1 when not active
    public static bool Cancel(int id);                     // stops a blob you are sending; false when it is not active
}
```

- **`Send` returns the transfer id, or `-1` when it can't start.** That is when the player is not a checked peer, when the compressed blob would need more than 8192 pieces, when the blob is over 16 MB, or when 16 transfers are already running. `modId` must be a valid mod id and `name` a name of 1 to 32 characters, or it throws. `data` must not be `null`. An empty blob is allowed.
  - Technical: it compresses `data` with gzip and sends it on the framework's `blob` channel, in base64 pieces that fit `MaxPacketBytes`.
- **`Received` runs on the other player once the whole blob has arrived and is intact.** A blob that fails the check is dropped, and the sender's `Finished` gets `false`. Any player can send any `modId`, so check who sent a blob (the `peer` argument) before your mod acts on it.
  - Technical: it runs when every chunk has arrived, the gzip decodes and the CRC32 (a check number) of the uncompressed data matches.
- **`Finished` runs on the sender when the transfer ends.** It is `true` when the other player has every piece. It is `false` on a timeout, a rejection, a cancel, or when the player leaves the lobby.
- **`Progress` is for a progress bar, and `Finished` or `Received` is the final answer.** Both versions return `-1` once the transfer is no longer active, so wait for `Finished` or `Received` instead of waiting for `1`.
- **`Cancel(id)` stops a blob you are sending.** It tells the other player and raises `Finished(id, false)`. It returns `false` for an id that is not active.
- **The sending is paced so it does not flood the connection.** At most 120 packets a second go to each player, shared by all their transfers, and at most 32 pieces wait without acknowledgement for each player. The receiver acknowledges every 8 pieces. A sender with no progress for 2 seconds resends from the last acknowledged piece. A transfer fails after 60 seconds with no acknowledged progress, and the log says why.
- **Transfer ids are local to the sender.** The receiver keeps its state by sender and id.
- **The receiver drops a partial blob that gets no new piece for 60 seconds.** A finished blob stops counting toward the 16 running transfers at once. A piece over 960 characters fails its transfer. The partial blobs of one player can use at most 16 MB of memory in total, so a second blob of the maximum size from the same player at the same time is refused. A player leaving the lobby ends its transfers in both directions.
  - Technical: memory is counted at 2 bytes per character. The gzip and CRC32 path, the chunking, the receive limits and the progress timeouts are covered by `NetBlobSelfTest`, which runs with `[Debug] NetBlobSelfCheck`.
