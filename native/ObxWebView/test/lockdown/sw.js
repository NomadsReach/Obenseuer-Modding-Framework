self.addEventListener("message", (e) => {
  e.waitUntil(
    fetch("https://example.invalid/worker.js", { mode: "no-cors" })
      .then(() => true, () => false)
      .then((answered) => e.source.postMessage({ answered: answered }))
  );
});
