---
title: Player
parent: Player life and skills
grand_parent: API reference
has_children: true
nav_order: 6
---

# Player

This page uses `KeyCode` from Unity. The [glossary](../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

Status: experimental.

## What it is

The player is you in the game: a character with health, hunger, thirst and tiredness, who sleeps, waits, learns skills, gets drunk, holds subscriptions and travels between places. This part of the framework lets a mod read that state and change some of it. It also tells the mod when things happen, such as the player waking up, gaining a skill level or starting a status effect.

It covers the player's state (health, position, sleeping and waiting, god mode), the places where the player can wait, the time picker of a bed, damage intercepts, skills, status effects, subscriptions, fast travel and the camera. Most calls read or change the live game, so they are refused while a save is loading or being written. In that case they give back a harmless default, so a mod can call them at any time.

On this page:

- [When to use it](#when-to-use-it)
- [Quick example](#quick-example)
- [Common tasks](player/common-tasks.md#common-tasks)
- [Reference](player/playerstate.md#playerstate)
  - [PlayerState](player/playerstate.md#playerstate): health, position, god mode, sleeping and waiting
  - [Wait stations](player/wait-stations.md#wait-stations)
  - [Damage and breakdown intercepts](player/damage-and-breakdown-intercepts.md#damage-and-breakdown-intercepts)
  - [Skills](player/skills.md#skills)
  - [Effects](player/effects.md#effects)
  - [Subscriptions](player/subscriptions.md#subscriptions)
  - [Travel](player/travel.md#travel)
  - [Camera](player/camera.md#camera)
  - [A longer example](player/a-longer-example.md#a-longer-example)
- [Details](player/details.md#details): when you can call it, what comes back, what you can't do (yet), if something fails to set up

## When to use it

- Heal the player or hurt them when something happens in your mod.
- Reward the player with skill points, for example when they finish a job.
- Show a message when the player wakes up, gets drunk or learns a skill.
- Make a bed-like action where the player picks how long to wait or sleep.
- Add a travel point to a level, or send the player to a named place.

## Quick example

This heals the player by 20 when you press F8, and writes a log line whenever any skill goes up. The skill event also fires for gains the game itself makes. Healing is gradual: the game adds the health back over a short time, not at once.

```csharp
using ObenseuerCore;
using UnityEngine;

[ModInfo("example.playerbasics", "Player Basics", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class PlayerBasics : FrameworkMod
{
    private ModKeybind healKey;

    protected override void OnLoad()
    {
        healKey = Mod.Key("Heal", "Heal the player", KeyCode.F8);
        Skills.SkillIncreased += OnSkillIncreased;
    }

    private void OnSkillIncreased(SkillIncreasedArgs args)
    {
        Log.Info(args.Skill + " went up by " + args.Amount);
    }

    private void Update()
    {
        if (healKey == null || !healKey.Down()) return;
        if (PlayerState.Heal(20f)) Log.Info("Healing " + PlayerState.FirstName + ", health is now " + PlayerState.Health);
    }
}
```

`PlayerState.Heal` returns `false` when there is no living player or a save is loading or being written, so the line is only logged when the heal was accepted. See [Your first mod](../guides/getting-started/first-mod.md) for how to build and run a mod like this.
