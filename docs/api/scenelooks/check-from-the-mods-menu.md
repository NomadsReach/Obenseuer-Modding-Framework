---
title: Check from the Mods menu
parent: Scene looks
grand_parent: Places and travel
nav_order: 3
---

# Check from the Mods menu

You can check that scene looks work on your copy of the game without writing any code. The framework's Settings page in the Mods menu has a section called `Scene looks` with a button, `Run scene looks checks`.

## Things to know

- The button runs a read-only check of the scene that is loaded now. It makes the list of characters, the list of every body root, and the names of the NPCs and characters. It reads the installed game files and changes nothing in the game.
- The full text goes to the log, and the menu shows one line. The log file is `BepInEx\logs\obenseuer.moddingframework.scenelooks.log`. The menu line looks like `Interior Tenement Speakeasy: 7 characters, 15 roots`.
- The classic Mods menu shows the same button under the framework's group, with the result next to it.
- No flag or console command is needed to use the button. The `scenelooks` console command does the same work and writes the same lines. <span class="tech">(no `[Debug]` flag is needed)</span>

## Limits

- If the check is refused, the menu line shows the reason. That happens when the Unity version is wrong or the loaded scene is not one of the game's scenes. The framework also writes the reason to the log once. <span class="tech">(the scene must be in the build settings)</span>
