---
title: Banners
parent: Custom skills
grand_parent: Player life and skills
nav_order: 4
---

# Banners

The banner is the message that slides in when a skill goes up. `QueueBanner` lets you show your own text in it. The framework queues banners because the game can only show one at a time.

## Things to know

- `QueueBanner(text)` shows your text in the skill banner, after any banners already waiting. It returns `true` when the text was queued. It throws an `ArgumentException` when `text` is null or empty. <span class="tech">(The banner is the game's `PlayerSkillIncreaseUI`. The call also returns `false` when `ShowNotification` is missing.)</span>
- Banners are shown one at a time, nine seconds apart, in the order they were queued. The game's own banner stays on screen for 8 seconds and then fades out, so a second banner has to wait. If nothing is waiting, a banner shows straight away. <span class="tech">(The game's private `PlayerSkillIncreaseUI.ShowNotification` sets the text and restarts a coroutine that waits 8 seconds in real time and then fades the alpha by 0.1 each frame, about ten frames. A second call replaces the first at once, so without a queue banners would overwrite each other. The queue advances once per frame.)</span>
- The game's own skill banners join the same queue. When nothing is showing and nothing is waiting, the game's banner shows as in the original game. When a banner is showing, the game's text is queued and shown after the others, so no banner is lost. <span class="tech">(Prefixes on `PlayerSkillIncreaseUI.ShowSkillIncrease` and `ShowNewSkill` check the queue. When free, the prefix marks 9 seconds busy and the game's call runs unchanged. Otherwise the text is queued and the game's call is skipped.)</span>
- `Add` queues a banner when the level goes up. Going from level 0 to a higher level shows the game's "new skill" text: `New skill`, the name, `learned!`. Any other rise shows the name, `increased to`, `level N!`. `Set` and loading a save show no banner.
- A new banner for a skill that already has one waiting replaces that banner's text, so the newest level shows.

## Limits

- `QueueBanner` returns `false`, and queues nothing, in four cases. There are already 5 of your text banners waiting, a save is loading or being written, the skill feature was turned off, or the banner is not on screen to use. In the last case it also writes one line to the log.
- There is a limit on how many banners can wait. Skill banners and text banners have 5 places each, so one kind cannot push out the other. If a sixth skill banner is queued, the oldest waiting skill banner is dropped. A queued text banner is never dropped to make room, so `QueueBanner` returns `false` when 5 are already waiting. The game's own banners are never dropped. <span class="tech">(The limits are `CustomSkillsMath.BannerQueue.MaxCustom` (5, for skill banners from `Add`) and `MaxQueued` (5, for text banners from `QueueBanner`).)</span>
- Waiting banners are dropped when a save loads.
- If the banner cannot be found, your banners are dropped and the game's banners are left alone. The framework finds the banner's code once when it starts. If a game update moves it, custom banners are dropped and one line goes to the log. <span class="tech">(`ShowNotification` is found once by reflection and cached. When it is missing, the two game prefixes are not installed. When no banner UI is on screen, a banner is dropped and logged once.)</span>
