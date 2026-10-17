---
title: Scene loads and saves
parent: NPC control
grand_parent: People
nav_order: 7
---

# Scene loads and saves

- **A scene load releases every claim.** After a single-scene load, every claim is released and every value still alive is put back. Take the NPC again from `Events.WhenReady` if you still need it. A `Take` called straight from an `Events.SceneLoaded` handler is refused, because the load gate is set for the whole of that event and `Take` returns `false` while it is set.
- **Saving does not change anything the framework holds.** `Take` is refused while the save runs, and `Release` still works.
