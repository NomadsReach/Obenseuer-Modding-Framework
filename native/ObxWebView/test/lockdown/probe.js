(function () {
  "use strict";
  const post = (o) => chrome.webview.postMessage(o);
  const wait = (ms) => new Promise((r) => setTimeout(r, ms));
  const needle = "OBX-OUTSIDE" + "-SECRET";

  async function traversal() {
    let leaked = false;
    for (const p of ["/..%2fsecret.txt", "/%2e%2e/secret.txt", "/../secret.txt", "/..%5csecret.txt", "/%2e%2e%5csecret.txt"]) {
      try {
        const text = await (await fetch(p)).text();
        if (text.indexOf(needle) >= 0) leaked = true;
      } catch (e) {
      }
    }
    post({ type: "step", name: "traversal", leaked: leaked });
  }

  async function worker() {
    post({ type: "step", name: "sw-top", absent: navigator.serviceWorker === undefined });
    try {
      await navigator.serviceWorker.register("sw.js");
      post({ type: "step", name: "worker-register", impossible: false });
    } catch (e) {
      post({ type: "step", name: "worker-register", impossible: true, result: String(e) });
    }
  }

  async function run() {
    await traversal();
    worker();
    const img = new Image();
    img.src = "https://example.invalid/pixel.png";
    fetch("https://example.invalid/", { mode: "no-cors" }).catch(() => {});
    try {
      new WebSocket("wss://example.invalid/");
    } catch (e) {
    }
    let cross = "denied";
    try {
      cross = String((await fetch("https://obx.other/data.txt")).status);
    } catch (e) {
    }
    post({ type: "step", name: "cross-host fetch", result: cross });
    const shell = document.createElement("iframe");
    shell.setAttribute("sandbox", "allow-scripts allow-same-origin allow-forms");
    shell.src = "https://obx.other/frame.html";
    document.body.appendChild(shell);
    await wait(1500);
    const opened = window.open("https://example.invalid/");
    post({ type: "step", name: "window.open", result: opened === null ? "null" : "window" });
    const frame = document.createElement("iframe");
    frame.src = "https://example.invalid/";
    document.body.appendChild(frame);
    const link = document.createElement("a");
    link.href = URL.createObjectURL(new Blob(["x"]));
    link.download = "obx-lockdown-test.txt";
    document.body.appendChild(link);
    link.click();
    await wait(800);
    post({ type: "big", pad: "x".repeat(2 * 1024 * 1024) });
    for (let i = 0; i < 3000; i++) post({ type: "flood", i: i });
    location.href = "obxtest-x:launch";
    await wait(500);
    location.href = "https://example.invalid/";
    await wait(1000);
    alert("blocked");
    post({ type: "step", name: "alive end" });
    location.href = "https://obx.other/frame.html";
  }

  window.addEventListener("message", (e) => post({ type: "step", name: "frame", bridge: e.data.bridge, own: e.data.own, sw: e.data.sw }));

  chrome.webview.addEventListener("message", (e) => {
    if (e.data && e.data.type === "wallet" && !window.started) {
      window.started = true;
      run();
    }
  });
  post({ type: "ready" });
})();
