---
title: Blob
parent: Multiplayer
grand_parent: Online play
nav_order: 7
---

# Blob

A blob is a larger piece of data, such as a file, sent to one player. The framework compresses it, cuts it into pieces, sends the pieces, checks they all arrived intact and puts the blob back together on the other side.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>NetBlob</code> <span>sends and receives large data</span></p>

Call `Send` with a player, your mod's id and a name to send a blob. Subscribe to `Received` to get one, and to `Finished` to hear whether your send arrived.

<div class="api-pair" markdown="1">

```csharp
public static class NetBlob
{
    public static event Action<ulong, string, string, byte[]> Received;
    public static event Action<int, bool> Finished;
    public static int Send(ulong peer, string modId, string name, byte[] data);
    public static float Progress(int id);
    public static float Progress(ulong peer, int id);
    public static bool Cancel(int id);
}
```

| Method | Returns | What it does |
|---|---|---|
| `Send(peer, modId, name, data)` | `int` | Starts sending data to one player. Returns the transfer id, or `-1` when it could not start. |
| `Progress(id)` | `float` | For a blob you are sending: how much was acknowledged, from 0 to 1. `-1` when not active. |
| `Progress(peer, id)` | `float` | For a blob you are receiving: how much has arrived, from 0 to 1. `-1` when not active. |
| `Cancel(id)` | `bool` | Stops a blob you are sending. Returns `false` when it is not active. |

| Event | Gives you | When it fires |
|---|---|---|
| `Received` | The sender, the mod id, the name and the data. | A blob has arrived whole and intact. |
| `Finished` | The transfer id, and `true` when it was delivered. | A blob you sent is done. `true` when the other player has every piece. |

</div>
</div>

## Things to know

- `Send` takes the blob and returns its transfer id. An empty blob is allowed. <span class="tech">(It compresses `data` with gzip and sends it on the framework's `blob` channel, in base64 pieces that fit `MaxPacketBytes`.)</span>
- `Received` runs on the other player once the whole blob has arrived and is intact. A blob that fails the check is dropped, and the sender's `Finished` gets `false`. <span class="tech">(It runs when every chunk has arrived, the gzip decodes and the CRC32 (a check number) of the uncompressed data matches.)</span>
- Any player can send any `modId`, so check who sent a blob (the `peer` argument) before your mod acts on it.
- `Finished` runs on the sender when the transfer ends. It is `false` on a timeout, a rejection, a cancel, or when the player leaves the lobby.
- `Progress` is for a progress bar, and `Finished` or `Received` is the final answer. Both versions return `-1` once the transfer is no longer active, so wait for `Finished` or `Received` instead of waiting for `1`.
- `Cancel(id)` stops a blob you are sending. It tells the other player and raises `Finished(id, false)`.
- The sending is paced so it does not flood the connection. At most 120 packets a second go to each player, shared by all their transfers, and at most 32 pieces wait without acknowledgement for each player.
- The receiver acknowledges every 8 pieces. A sender with no progress for 2 seconds resends from the last acknowledged piece.
- Transfer ids are local to the sender. The receiver keeps its state by sender and id.
- The gzip and CRC32 path, the chunking, the receive limits and the progress timeouts are covered by a self-test. <span class="tech">(It is `NetBlobSelfTest`, which runs with `[Debug] NetBlobSelfCheck`.)</span>

## Limits

- `Send` returns `-1` when it can't start. That is when the player is not a checked peer, when the compressed blob would need more than 8192 pieces, when the blob is over 16 MB, or when 16 transfers are already running.
- `modId` must be a valid mod id and `name` a name of 1 to 32 characters, or `Send` throws. `data` must not be `null`.
- `Cancel` returns `false` for an id that is not active.
- A transfer fails after 60 seconds with no acknowledged progress, and the log says why.
- The receiver drops a partial blob that gets no new piece for 60 seconds. A finished blob stops counting toward the 16 running transfers at once.
- A piece over 960 characters fails its transfer.
- The partial blobs of one player can use at most 16 MB of memory in total, so a second blob of the maximum size from the same player at the same time is refused. <span class="tech">(Memory is counted at 2 bytes per character.)</span>
- A player leaving the lobby ends its transfers in both directions.
