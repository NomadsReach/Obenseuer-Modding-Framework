---
title: Mail
parent: Economy
grand_parent: Money and shops
nav_order: 8
---

# Mail

`Mail` reads the player's mailbox and parcel locker, sends letters and packages to the player, and tells you when mail arrives. A letter goes to the mailbox and a package goes to the parcel locker.

```csharp
public enum MailKind
{
    Letter = 0,    // a letter, delivered to the mailbox
    Package = 1    // a package, delivered to the parcel locker
}

public sealed class MailInfo    // one piece of mail waiting in the mailbox or locker
{
    public string Name { get; }          // the name the game shows for the mail, used as the subject
    public MailKind Kind { get; }        // letter or package
    public int ItemId { get; }           // the item inside; 0 when the mail holds no item
    public int Count { get; }            // how many of the item
    public int Oc { get; }               // the OC inside; 0 when none
    public float Rm { get; }             // the RM inside
    public bool IsNote { get; }          // true when the letter carries a note (text)
    public string Body { get; }          // the note's text; "" when it is not a note
    public string Sender { get; }        // who an email note is from; "" when none
}

public static class Mail
{
    public static event Action<MailReceivedArgs> Received;          // a letter arrived; args.Name is the letter name and args.Kind is Letter
    public static event Action<MailReceivedArgs> PackageReceived;   // a package arrived; args.Name is the package name and args.Kind is Package
    public static event Action<MailSentArgs> Sent;                  // the game sent mail out into the world

    public static bool IsAvailable { get; }                         // false when the mail events could not be set up
    public static List<MailInfo> Inbox(MailKind kind);              // the mail that has arrived and is waiting
    public static bool SendItem(string mailName, int itemId, int count, MailKind kind);   // sends the player an item
    public static bool SendMoney(string mailName, MoneyCurrency currency, float amount, MailKind kind);   // sends the player money
    public static bool SendBundle(string mailName, IList<MailLine> lines, MailKind kind, int delayMinutes);   // sends several items in one mail
    public static List<PendingMail> Pending();                      // the player's mail that is still on its way
}

public sealed class MailLine    // one line of a bundle
{
    public int ItemId;   // the item; it must be above 1
    public int Count;    // how many; 1 to 1000
}

public sealed class MailSentArgs    // what you get when the game sends mail into the world
{
    public string Name { get; }          // the mail's name
    public int ReceiverNpcId { get; }    // the NPC it is for; 0 when it has no NPC receiver
    public int ItemId { get; }           // the item inside; 0 when the mail holds no item
    public int Count { get; }            // how many of the item
}

public sealed class PendingMail    // one piece of mail still on its way to the player
{
    public string Name { get; }          // the mail's name
    public MailKind Kind { get; }        // letter or package
    public int SecondsLeft { get; }      // seconds until it arrives
}
```

- **`Inbox` lists the mail that has arrived and is still waiting.** That is the letters in the mailbox or the packages in the parcel locker. It does not list mail that is still on its way, and the game clears the list when the player takes the mail. To read only letters with text, keep the ones where `IsNote` is `true`.
  - Technical: the game clears the list in `Mailbox.TakeMail`.
- **`IsNote`, `Body` and `Sender` read a letter's note.** A letter that carries a note has `IsNote` set to `true` and `Body` set to the text. `Sender` is who an email note is from, and is `""` for any other note. Letters with an item or money, and other letters, have `IsNote` set to `false` and an empty `Body` and `Sender`. `Name` is the letter's name, used as the subject.
  - Technical: a note letter is the game's `NoteMail`, and `Sender` is the `from` of the note when the note is an email asset.
- **`SendItem` and `SendMoney` send mail to the player, and the game's mail delay applies.** The mail may arrive later, as game mail does. A letter goes to the mailbox and a package to the parcel locker.
  - Technical: they use the game's `MailController.SendMail` with `Target.Player` (letter) or `Target.PlayerParcelLocker` (package).
- **`SendItem`, `SendMoney` and `SendBundle` make items or money out of nothing, so use them for rewards.** `SendItem` and `SendBundle` refuse a count outside 1 to 1000 and an item id of 1 or less. If the inventory is full, the game drops a large count into the world.
- **A package also makes the game send a "Parcel Notification" letter, so `Received` fires for it too.**
- **`Received` and `PackageReceived` fire when mail is delivered, whether at once or after the delay.** When a save loads, the game replays the letters and packages it has stored. The framework ignores that replay, but only for the names that were in the save's received lists when the load finished, one game notification for each stored name. Everything else is passed on, so a letter or package that the framework or the game delivers while the save is loading is reported.
  - Technical: they forward the game's `MailController.MailReceived` and `PackageReceived`. The game replays two frames after `MailController.OnLoadingGame` reads the save (`MailController.DelayedOnLoaded`).
- **`SendBundle` sends several items as one letter or one package.** Use `MailKind.Letter` for the player's mailbox or `MailKind.Package` for the parcel locker. Each line is checked like `SendItem`, and you need at least one line. `delayMinutes` must be 1 to 525600, and the game's own timer, in game time, delivers it. The game's default delay is not used.
- **`Pending` lists the player's letters and packages that are still on their way.** It does not list mail for NPCs. `SecondsLeft` is the timer's seconds remaining. How often the game updates that number has not been confirmed.
- **`SendBundle` returns `false` while the game is saving or loading, the same as `SendItem`.** `Pending` only reads, so it always works.
- **`Sent` tells you when the game sends mail out into the world.** The game raises it for every mail sent to the world side: a drop into a player's mailbox, and scene mail senders set to the world. It is not raised for the player's own letters and packages, so `SendItem`, `SendMoney` and `SendBundle` never raise it. A mail with a delay raises it when the game's timer ends, not when the mailbox drops it.
  - Technical: it forwards the game's `MailController.MailSent` for `Target.World`: `Mailbox.InsertMail` and scene mail senders whose `mailTarget` is `World` (`logic_mail_sender`, `logic_mail_bundle_sender`). A delayed mail raises it from `TimerSendMailWorld`.
- **`Sent` only says that the mail was delivered to the world side.** It does not say whether anything claimed the letter, and the order of handlers compared with the game's own listeners is not defined. `ReceiverNpcId` is 0 when the mail has no receiver, and `ItemId` and `Count` are 0 for money or other mail that holds no item. A delayed mail can fire at any game time, so a handler that writes money, items or quests must check `SaveData.Busy` first.
- **`Sent` does not fire while a save is loading.** A mail the old session sent that arrives during a load is dropped on purpose, not passed into the new save.
