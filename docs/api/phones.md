---
title: Phones
parent: Phones, mail and media
grand_parent: API reference
nav_order: 9
---

# Phones

## What it is

This API reads the phone book, the numbers the player can dial. Calls are on [Phone lines](phone-lines.md).

## Reference

`Phones.All()` reads the phone book; calls go on [Phone lines](phone-lines.md).

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Phones</code> <span>the phone book</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Phones
{
    public static List<PhoneContact> All();
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<PhoneContact>` | Every dialable number. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>PhoneContact</code> <span>one phone entry</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class PhoneContact
{
    public string Id { get; }
    public string Number { get; }
    public PhoneNetwork Network { get; }
    public float AnswerChance { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | Internal name. |
| `Number` | `string` | Digits; `*` is 10, `#` is 11. |
| `Network` | `PhoneNetwork` | Networks; values combine. |
| `AnswerChance` | `float` | 0 to 1: chance someone answers. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>PhoneNetwork</code> <span>networks; values combine</span></p>
<div class="api-pair" markdown="1">

```csharp
[Flags]
public enum PhoneNetwork
{
    None = 0,
    Obenseuer = 1,
    Outside = 2,
    Wasteland = 4
}
```

| Value | Meaning |
|---|---|
| `None` | No network. |
| `Obenseuer` | Phones in Obenseuer. |
| `Outside` | Phones outside Obenseuer. |
| `Wasteland` | Phones in the wasteland. |

</div>
</div>

### Putting it together

Logs Obenseuer numbers with their answer chance.

<div class="api-example" markdown="1">

```csharp
using ObenseuerCore;

[ModInfo("example.phonebook", "Phone Book", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class PhoneBook : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            foreach (PhoneContact contact in Phones.All())
            {
                if (!contact.Network.HasFlag(PhoneNetwork.Obenseuer)) continue;
                int percent = (int)(contact.AnswerChance * 100);
                Log.Info(contact.Id + ": dial " + contact.Number + ", picks up " + percent + "% of the time");
            }
        });
    }
}
```

| Step | What it does |
|---|---|
| `contact.Network.HasFlag(PhoneNetwork.Obenseuer)` | Keeps Obenseuer numbers. |
| `contact.AnswerChance * 100` | Makes a percentage. |

</div>

### Things to know

- Hidden entries and entries with no digits are left out of `All()`.
- Off-network numbers get an "outside the network" reply, with no answer roll.

### Limits

- You can't add numbers; added ones vanish on load.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later). Logs go to `BepInEx\logs\obenseuer.moddingframework.npcs.log`.

### When you can call it

- Call from the [main thread](../glossary.md#main-thread), not a thread you started.
- Safe at any time; empty reads return `0`, `false`, `null` or an empty list.

## Under the hood

How the framework reads the game, for modders who want to dig deeper.

- `PhoneNumberDatabase.database` fills from the game's `Resources` folder at start. `notIndexed` and empty `number` arrays are skipped.
- `network.HasFlag(number.network)` is checked before `AnswerChance` is rolled.
- `Phone.additionalNumbers` is set in the Unity editor. No save code touches the book.
