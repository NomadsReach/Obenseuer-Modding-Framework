---
title: Computer files
parent: Phones, mail and media
grand_parent: API reference
nav_order: 3
---

# Computer files

This page uses `GameObject` from Unity. The [glossary](../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

Status: experimental.

## What it is

Some computers in the game hold things you can read: files in the recycle bin and emails in the inbox. This API lets your mod find the computers in the scene you are in and read the title, sender and text of each file and email. It only reads. It can't add or change anything on a computer.

The class is called `ComputerContents` because `ComputerFiles` is already the name of a class in the game.

## When to use it

- List the computers in the current scene and show what is on them.
- Read the emails or the recycle bin files of one computer, for example to react to a clue the player can find.
- Get the id of a computer your mod already holds as a game object.

## Quick example

This mod logs every computer in the scene and the sender and subject of each email in its inbox. It waits until a save is loaded, because there are no computers to read before that.

```csharp
using ObenseuerCore;

[ModInfo("example.computerpeek", "Computer Peek", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class ComputerPeek : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            foreach (ComputerInfo computer in ComputerContents.All())
            {
                Log.Info("Computer \"" + computer.Title + "\" in scene " + computer.Scene);
                foreach (ComputerDocInfo email in ComputerContents.Emails(computer.Id))
                    Log.Info("  Email from " + email.Sender + ": " + email.Title);
            }
        });
    }
}
```

`Events.WhenReady` runs your code once a save is loaded, or at once if one already is. See [Using events](../guides/getting-started/using-events.md).

## Common tasks

Read the recycle bin files of one computer. For a text file, `Body` holds the text.

```csharp
foreach (ComputerInfo computer in ComputerContents.All())
    foreach (ComputerDocInfo file in ComputerContents.Files(computer.Id))
        Log.Info(computer.Title + ": " + file.Title + " (" + file.Body.Length + " characters)");
```

Get the id of a computer you already have as a game object. The result is `null` when the object is not a computer.

```csharp
void ShowId(UnityEngine.GameObject computerObject)
{
    string id = ComputerContents.IdOf(computerObject);
    Log.Info(id == null ? "Not a computer." : "Computer id: " + id);
}
```

## Reference

This is the full list of what the API gives you. Text in brackets at the end of a note is extra detail you can skip on a first read.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ComputerContents</code> <span>find computers and read their files and emails</span></p>

Call `All()` to find the computers in the scene, then `Files` or `Emails` with a computer's `Id` to read what is on it.

<div class="api-pair" markdown="1">

```csharp
public static class ComputerContents
{
    public static List<ComputerInfo> All();
    public static string IdOf(GameObject computer);
    public static List<ComputerDocInfo> Files(string computerId);
    public static List<ComputerDocInfo> Emails(string computerId);
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<ComputerInfo>` | Every computer in the scene you are in. |
| `IdOf(computer)` | `string` | The id of this computer. `null` when the object is not a computer. |
| `Files(computerId)` | `List<ComputerDocInfo>` | The files in this computer's recycle bin. |
| `Emails(computerId)` | `List<ComputerDocInfo>` | The emails in this computer's inbox. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ComputerInfo</code> <span>one computer</span></p>

`ComputerContents.All()` gives you one of these for each computer in the scene. Pass its `Id` to `Files` or `Emails`. <span class="tech">(It is built from the game's `InteractableComputer` objects.)</span>

<div class="api-pair" markdown="1">

```csharp
public sealed class ComputerInfo
{
    public string Id { get; }
    public string Title { get; }
    public string Scene { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The computer's id. Pass it to `Files` and `Emails`. |
| `Title` | `string` | The name the computer shows. An empty text when it has none. |
| `Scene` | `string` | The name of the scene the computer is in. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ComputerDocInfo</code> <span>one file or one email</span></p>

`Files` and `Emails` return these. A file has no `Sender`, and for an email `Title` is the subject. <span class="tech">(It is built from the game's `ComputerFile` and `Email` entries.)</span>

<div class="api-pair" markdown="1">

```csharp
public sealed class ComputerDocInfo
{
    public string Title { get; }
    public string Sender { get; }
    public string Body { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Title` | `string` | A file's name, or an email's subject. |
| `Sender` | `string` | An email's sender. An empty text for a file. |
| `Body` | `string` | The text of the file or the email. |

</div>
</div>

### Things to know

- A computer id is a piece of text that names one computer. `All()` and `IdOf` give it to you. It is stable as long as the scene does not change. Do not keep it for a computer that the game creates while it runs. <span class="tech">(The id is the computer's world GUID when the game gave it one. Most computers have none, so the usual id is the scene path, such as `Scene:Root[0]/Child[2]/Computer[1]`, with each node followed by its position among its siblings.)</span>
- `All()` only lists computers that are switched on in the scene you are in. A computer in another scene, or one the game has switched off, is not listed, and `Files` and `Emails` return an empty list for it. <span class="tech">(It searches the active objects of the active scene for `InteractableComputer`.)</span>
- `Files` and `Emails` give you copies, in the order the game keeps them. Each call reads what the computer holds right now.
- For a file, `Sender` is an empty text, and `Body` is empty when the file is not a text file. For an email, `Title` is the subject, `Sender` is the sender's name and `Body` is the message. <span class="tech">(For a file, `Title` is `ComputerFile.fileName` and `Body` is `textFile.content`. For an email they are `Email.title`, `from` and `content`.)</span>
- A missing computer gives an empty list, and a missing id throws an error. An id that matches no computer, or a computer that is not loaded, gives an empty list. A `null` id throws an `ArgumentException`. An empty text is not an error: it matches nothing and gives an empty list.
- `IdOf` gives `null` for a `null` object and for an object that is not a computer.

### Limits

- You can only read. You can't add, change or delete files or emails. The game builds each computer's screen once, when the computer starts. Adding a file or email later would show the old rows a second time next to the new ones, and nothing the framework could add would be saved. <span class="tech">(The lists are `InteractableComputer.emails` and `recycleBinFiles`, set in the Unity editor and marked `[fsIgnore]`, so they are not saved. The game copies them to the screen once, in the computer's `Start`. `ComputerScreen.AddEmails` and `ComputerScreen.AddRecycleBinFiles` each replace the list and build a row for every entry, so a second call shows the old rows twice. The framework does not wrap them. The wallpaper is a separate case: see [Computer desk](computerdesk.md).)</span>

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.computerfiles.log`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

### When you can call it

- Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started. The game and Unity only run safely on the main thread (see the [glossary](../glossary.md#main-thread)).

### What to check for and what comes back

- This API changes nothing in the game, so it has no `IsAvailable` and no feature group. The framework changes no game method for it, and it has no events.
- If the game itself fails while the framework reads, you get an empty answer instead of an error. The answer is an empty list, or `null` for `IdOf`. The failure goes to the log at debug level only.
