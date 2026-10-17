---
title: Messages from the shim
parent: Web UI
grand_parent: Screen and UI
nav_order: 10
---

# Messages from the shim

The framework reads the shim's queue each frame. Some messages come from the shim itself, and not from your page: `obx.key`, `obx.error` and `obx.status`. They never reach your `Message` handler, and a page cannot send a message whose `type` starts with `obx.`. The one exception is `obx.frame`, which the framework passes on to the page unchanged. When the queue overflows, the number of dropped messages is reported as an `obx.error` with the text `messages-dropped`, and it is logged as a warning at most once a minute. A burst inside that minute is added to the next warning, so a count with no later burst is not logged.

- **`obx.key` means a view got Escape or F10.** It goes to `KeyPressed`, and Escape may then hide the view (see [Step 7 of the UI guide](../../guides/ui.md#step-7-hide-close-and-escape)).
- **`obx.status` means the view is set up and ready.** The shim sends one when it has set the view up. A modal view that gets none within 20 seconds of showing is closed by the framework.
- **`obx.error` means the shim reports an error.** The framework stores it in `LastError` and logs it. A serious error closes the view, or switches the session to composition mode in thread mode.
- **`obx.frame` means a frame in the view navigated.** The framework posts `{"type":"obx.frame","view":<id>,"ok":true|false,"status":<number>,"url":"..."}` to the page. Treat `url` as data (see [Frame hosts](frame-hosts.md#frame-hosts)).
