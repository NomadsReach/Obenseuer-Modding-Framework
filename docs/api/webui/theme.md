---
title: Theme
parent: Web UI
grand_parent: Screen and UI
nav_order: 9
---

# Theme

A page that links `obx-theme.css` gets its colours from the `data-theme` setting on `<html>`.

- **The first `data-theme` is set before your page runs.** The framework gives every view a small start-up script. It sets the attribute on `<html>` as soon as that element exists, from `WebUi.Theme`.
- **When the setting changes, the framework sends your page a message.** It looks like `{"v":1,"type":"theme","mode":"light"}` (or `"dark"`), and it is sent to every open view. Your page should set the attribute from that message. The framework does not change the attribute itself after the page has loaded.
- **A page you load later also starts in the new look.** The framework replaces each open view's start-up script with the one for the new look. A view keeps one such script.
