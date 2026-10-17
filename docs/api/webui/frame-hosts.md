---
title: Frame hosts
parent: Web UI
grand_parent: Screen and UI
nav_order: 6
---

# Frame hosts

A frame is a window inside your page that shows another web page. `AllowFrameHost(host)` is the only way a page can show a live website. It allows one exact secure (HTTPS) website host, in frames of that view.

## Things to know

- The `host` must be an exact lowercase website name, such as `stalburg.net`, with at least two parts separated by dots. Each call adds one host, and there are no wildcards.
- Each part is 1 to 63 characters of lowercase letters, digits and hyphens, and does not start or end with a hyphen. The whole name is at most 253 characters. A name whose last part is all digits, such as `127.0.0.1`, is refused.
- A name with a scheme (`https://`), a port, a path, a user name or a wildcard gives `false` and sets `LastError`.
- A frame can go to `https://<host>/` and load requests from it. Only the default port works.
- A message from a frame of the allowed host is dropped. Only the top page of your view can send messages to your mod. <span class="tech">(The shim drops it as `message-source-blocked`.)</span>
- Your page must check where a message came from before it acts. A frame of the allowed host can send your page a message with `parent.postMessage`. Trust a message only when its `e.origin` is your own page's address and its `e.source` is the window your page expects.
- Treat the `url` in an `obx.frame` message as data, never as HTML. It comes from remote content. Write it with `textContent`, not `innerHTML`.
- Your page must allow the frame in its own Content-Security-Policy. Add `frame-src https://<host>`, or the page's own policy refuses the frame.
- Put the frame in a sandbox. Use `<iframe sandbox="allow-scripts" src="https://<host>/...">`. Do not add `allow-same-origin`, `allow-top-navigation` or `allow-popups`. A frame with `sandbox=""` runs no script, so the script limits below do not apply to it.
- A second website, for example one for images, needs its own `AllowFrameHost` call. The Currencies page on stalburg.net loads one image from `licensebuttons.net`, so a page that shows it must allow that host too.
- Each time a frame navigates, you are told with an `obx.frame` message, up to 64 times for each view. See [Messages from the shim](messages-from-the-shim.md#messages-from-the-shim).

## Limits

- Requests from an allowed host pass for the whole view, not just for the frame, because the shim cannot tell which frame sent a request. Requests to any other host are refused as `resource-blocked`, whether the page or a frame makes them. An external image inside a frame is refused too.
- The filter covers every kind of request that WebView2 reports. Other kinds of requests, such as `fetch` and `XMLHttpRequest` from a frame, have not been tested.
- The page's Content-Security-Policy does not reach the frame. A frame runs under its own site's rules, so the page's `connect-src` does not cover it.
- These are still blocked: a top-level navigation to the host; a frame on the shell host (a frame cannot load the page that owns the bridge to your mod); any other website; new windows, downloads, permission requests and links to other programs.
- Some browser features are removed from every page and frame: `WebSocket`, `WebSocketStream`, `EventSource`, `WebTransport`, `RTCPeerConnection`, `webkitRTCPeerConnection`, `Worker` and `navigator.sendBeacon`. They are removed before the page's scripts run, and the page cannot put them back. So a page or frame cannot open a socket, an event stream, a WebTransport session, a WebRTC connection or a dedicated worker. <span class="tech">(A dedicated worker has its own global scope without the init script, so `Worker` is removed rather than filtered. The SDK has no resource context for WebRTC and no source kind for dedicated workers, so the shim cannot filter those requests.)</span>
- It is not known whether the resource filter sees WebSocket and EventSource requests. Worklets (`audioWorklet`, `paintWorklet`) have not been checked. A frame that a script creates as `about:blank` got the init script in a hand test, but this is not covered by an automated test.
- Frames share one browser profile with the OS UI, so the website's cookies are not kept apart. <span class="tech">(The profile is `BepInEx\cache\ObxWebView`.)</span>
- Storage in the browser (`localStorage` and cookies) is shared by every save. Keep the data of one save in `SaveData`, not in the page's storage.
