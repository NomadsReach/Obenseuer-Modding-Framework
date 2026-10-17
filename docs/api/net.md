---
title: Multiplayer
parent: Online play
grand_parent: API reference
has_children: true
nav_order: 1
---

# Multiplayer

Status: experimental.

## What it is

This API lets your mod build online features on top of Steam. It has five parts. `Steam` tells you who the local player is on Steam and who their friends are. `Lobby` hosts or joins a lobby, a group of players who are connected to each other. `Net` sends short text messages between the players in a lobby, on a channel (a name) that you choose. `Presence` shares where each player is and which area they are in. `NetBlob` sends larger data to one player. Nothing runs until a mod uses it. Every player in a lobby needs Steam and the same mods.

## When to use it

- Host a lobby for friends, or join one when the player accepts a Steam invite.
- Send small messages between players, such as chat lines or game events.
- Show other players' positions in your own world.
- Send a file or a save-sized piece of data from one player to another.

## Quick example

This mod hosts a lobby for up to four players once a save is loaded, joins a lobby when the player accepts an invite, and sends a hello to each player who joins. Messages that arrive on the `ping` channel are written to the log.

```csharp
using ObenseuerCore;

[ModInfo("example.pingpong", "Ping Pong", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class PingPong : FrameworkMod
{
    protected override void OnLoad()
    {
        Net.Register(Mod.Id, "ping", (peer, text) => Log.Info(Lobby.MemberName(peer) + " says: " + text));
        Lobby.JoinRequested += lobby => Lobby.Join(lobby, "pingpong", "0.1.0");
        Lobby.MemberJoined += peer => Net.Send(peer, "ping", "Hello from " + Steam.Name(Net.Me), true);

        Events.WhenReady(() =>
        {
            if (Steam.IsReady && Lobby.Status == LobbyStatus.Offline)
                Lobby.Host("pingpong", "0.1.0", 4, true);
        });
    }
}
```

`Register` gives the framework a piece of your code to run each time a message arrives on a channel. `JoinRequested` and `MemberJoined` are events: the framework calls your code each time they happen. `WhenReady` waits until a save is loaded before it runs your code. See [Using events](../guides/getting-started/using-events.md) for more on events and `WhenReady`.
