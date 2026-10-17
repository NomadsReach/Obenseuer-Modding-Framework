---
title: What you can't do (yet)
parent: Mailboxes
grand_parent: Phones, mail and media
nav_order: 3
---

# What you can't do (yet)

- **You can't post a single piece of mail from this page.** The game's way of posting first hands in all the task mail the player holds, which finishes the goals that depend on it. It then sends and removes every mailable item in the inventory. A mod cannot post one letter without that sweep, and the framework has no way to make an item mailable, so it is left out.
  - Technical: this is `InsertMail`. It calls `TaskItemsManager.RemoveAllMail` first, which runs `TaskItemMail.OnMailed` on every mail task item the player holds and completes its `taskToFinish` goal.
- **You can't react to the box's own signals.** They are wiring inside the scene.
  - Technical: the output relays are `onMailTaken`, `onMailInserted`, `onMailReceived` and `onMailAmount`.
- **You can't subscribe to a mailbox receiving mail here.** The `Mail.Received` event on the [economy page](../economy/mail.md#mail) covers it.
