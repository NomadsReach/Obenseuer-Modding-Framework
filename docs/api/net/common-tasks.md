---
title: Common tasks
parent: Multiplayer
grand_parent: Online play
nav_order: 1
---

# Common tasks

This page uses `Vector3` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

Share the local player's position every frame and count the other players within five metres. `Publish` keeps sending for as long as you keep calling it, so call it from `Update`.

```csharp
private void Update()
{
    Presence.Publish("Bar", PlayerState.Position, 0f, false);
    int nearby = 0;
    foreach (PresenceRecord remote in Presence.Remotes)
        if (Vector3.Distance(remote.Position, PlayerState.Position) < 5f) nearby++;
}
```

Send a larger piece of data to one player, and receive it on the other side. `Send` returns -1 when it could not start. Check who sent it before you trust it.

```csharp
NetBlob.Received += (peer, modId, name, data) =>
{
    if (modId == Mod.Id && name == "note" && Net.Peers.Contains(peer))
        Log.Info("Got a note: " + Encoding.UTF8.GetString(data));
};
NetBlob.Finished += (id, delivered) => Log.Info("Transfer " + id + (delivered ? " arrived" : " failed"));

int id = NetBlob.Send(peer, Mod.Id, "note", Encoding.UTF8.GetBytes("Meet me at the bar."));
if (id < 0) Log.Info("The note could not be sent.");
```

Find friends who are playing Obenseuer, open the invite window, and leave the lobby when your mod is done.

```csharp
foreach (ulong friend in Steam.FriendsPlaying(951240))
    Log.Info(Steam.Name(friend) + " is playing Obenseuer");

if (Lobby.IsHost) Lobby.Invite();
Lobby.Leave("pingpong");
```
