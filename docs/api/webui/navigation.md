---
title: Navigation
parent: Web UI
grand_parent: Screen and UI
nav_order: 7
---

# Navigation

`Navigate` and `Open` take a relative path, such as `index.html` or `pages/shop.html`. A path is refused when it:

- is empty, or starts with `/` or `\`;
- contains `:`, `\`, `%` or `..`.

A refused path gives `null` from `Open` and `false` from `Navigate`. The shim also blocks navigation outside the addresses it mapped for your view (frames can also load the hosts you named with `AllowFrameHost`, see [Frame hosts](frame-hosts.md#frame-hosts)), new windows, downloads, permission requests and links to other programs.
