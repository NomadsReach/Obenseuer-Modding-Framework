(async function () {
  "use strict";
  let bridge = false;
  try {
    chrome.webview.postMessage({ type: "frame" });
    bridge = true;
  } catch (e) {
  }
  let own = -1;
  try {
    own = (await fetch("data.txt")).status;
  } catch (e) {
  }
  parent.postMessage({ bridge: bridge, own: own, sw: navigator.serviceWorker === undefined }, "*");
})();
