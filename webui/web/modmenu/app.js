const state = { mods: [], pages: {}, rows: {}, requested: {}, modId: null, pageId: null, query: "" };
const els = { search: document.getElementById("search"), count: document.getElementById("count"), mods: document.getElementById("mods"),
  pane: document.getElementById("pane"), status: document.getElementById("status"), close: document.getElementById("close"),
  modCount: document.getElementById("modCount"), tabs: document.getElementById("tabs") };
const pending = new Map();
const liveIds = new Set();
let frame = 0, busyOn = false, escHeld = false, escTimer = 0, deferred = false, post = null;

function send(msg) {
  if (post) post(Object.assign({ v: 1 }, msg));
}

function setStatus(text) {
  els.status.textContent = text;
}

function setTheme(mode) {
  if (mode === "light" || mode === "dark") document.documentElement.setAttribute("data-theme", mode);
}

function findMod(id) {
  return state.mods.find((m) => m.id === id) || null;
}

function byKey(key) {
  return Array.from(document.querySelectorAll("[data-key]")).find((e) => e.dataset.key === key) || null;
}

function textFocused() {
  const a = document.activeElement;
  return a.tagName === "INPUT" && a.type === "text";
}

function loadPage(id) {
  const info = state.mods.flatMap((m) => m.pages).find((p) => p.id === id);
  if (!info || info.kind === "classic" || state.pages[id] || state.requested[id]) return;
  state.requested[id] = true;
  send({ type: "page", id });
}

function loadAll() {
  for (const m of state.mods) for (const p of m.pages) loadPage(p.id);
}

function hitsFor(mod) {
  const q = state.query.toLowerCase();
  const hits = [];
  for (const p of mod.pages) {
    const data = state.pages[p.id];
    if (!data) continue;
    for (const s of data.sections || []) {
      for (const row of s.rows || []) {
        if ([mod.name, s.name, row.label, row.desc, row.kind === "key" ? row.value : ""].join(" ").toLowerCase().includes(q)) hits.push({ section: s.name, row });
      }
    }
  }
  return hits;
}

function settingCount(mod) {
  let n = 0;
  for (const p of mod.pages) {
    for (const s of (state.pages[p.id] || {}).sections || []) n += (s.rows || []).filter((r) => r.kind !== "note").length;
  }
  return n;
}

function sideMeta(mod) {
  if (state.query) {
    const n = hitsFor(mod).length;
    return n + (n === 1 ? " result" : " results");
  }
  const n = settingCount(mod);
  return [mod.version ? "v" + mod.version : "", n ? n + (n === 1 ? " setting" : " settings") : ""].filter(Boolean).join(" / ");
}

function renderMods() {
  els.modCount.textContent = state.mods.length + (state.mods.length === 1 ? " mod" : " mods");
  els.mods.textContent = "";
  for (const mod of state.mods) {
    const b = Rows.btn("side-item", "");
    b.dataset.key = "mod:" + mod.id;
    b.setAttribute("aria-current", String(mod.id === state.modId));
    b.append(Rows.el("span", "side-name", mod.name), Rows.el("span", "side-meta", sideMeta(mod)));
    b.addEventListener("click", () => {
      if (state.query) {
        els.search.value = "";
        state.query = "";
      }
      selectMod(mod.id);
    });
    els.mods.appendChild(Rows.el("li")).appendChild(b);
  }
}

function renderTabs(mod) {
  for (const p of mod.pages) {
    const t = Rows.btn(p.id === state.pageId ? "obx-tab active" : "obx-tab", p.title);
    t.dataset.key = "page:" + p.id;
    t.addEventListener("click", () => {
      state.pageId = p.id;
      loadPage(p.id);
      render();
    });
    els.tabs.appendChild(t);
  }
}

function classicCard(page) {
  const card = Rows.el("div", "obx-card classic");
  card.appendChild(Rows.el("p", "", "This page still uses the classic menu."));
  const b = Rows.btn("obx-btn primary", "Open classic page");
  b.dataset.key = "classic";
  b.addEventListener("click", () => send({ type: "classic", page: page.id }));
  card.appendChild(b);
  return card;
}

function renderResults() {
  let total = 0;
  for (const mod of state.mods) {
    const hits = hitsFor(mod);
    if (!hits.length) continue;
    total += hits.length;
    const card = Rows.el("div", "obx-card rows");
    for (const h of hits) card.appendChild(Rows.render(h.row));
    els.pane.append(Rows.el("h2", "obx-label section", mod.name), card);
  }
  els.count.textContent = total + (total === 1 ? " result" : " results");
  if (!total) els.pane.appendChild(Rows.el("p", "empty", "Nothing matches that search."));
}

function renderPane() {
  els.pane.textContent = "";
  els.tabs.textContent = "";
  els.count.textContent = "";
  if (state.query) return renderResults();
  const mod = findMod(state.modId);
  if (!mod) return els.pane.appendChild(Rows.el("p", "empty", "No mods have settings yet."));
  renderTabs(mod);
  els.pane.appendChild(Rows.el("h1", "pane-name", mod.name));
  const page = mod.pages.find((p) => p.id === state.pageId);
  if (!page) return;
  if (page.kind === "classic") return els.pane.appendChild(classicCard(page));
  const data = state.pages[page.id];
  if (!data) return els.pane.appendChild(Rows.el("p", "empty", "Loading..."));
  const sections = data.sections || [];
  if (!sections.length) return els.pane.appendChild(Rows.el("p", "empty", "This page has no settings."));
  for (const s of sections) {
    if (s.name) els.pane.appendChild(Rows.el("h2", "obx-label section", s.name));
    const card = Rows.el("div", "obx-card rows");
    for (const r of s.rows || []) card.appendChild(Rows.render(r));
    els.pane.appendChild(card);
  }
}

function navs() {
  return Array.from(document.querySelectorAll("[data-nav]")).filter((e) => !e.disabled);
}

function rove(cur) {
  const list = navs();
  const first = list.includes(cur) ? cur : list[0];
  for (const e of list) e.tabIndex = e === first ? 0 : -1;
}

function moveFocus(dir) {
  const list = navs();
  if (!list.length) return;
  const i = list.indexOf(document.activeElement);
  const k = i < 0 ? (dir > 0 ? 0 : list.length - 1) : Math.min(list.length - 1, Math.max(0, i + dir));
  list[k].focus();
}

function render() {
  Rows.cancel();
  const held = document.activeElement.closest("[data-key]");
  const key = held ? held.dataset.key : null;
  renderMods();
  if (textFocused()) {
    deferred = true;
    syncBusy();
    return;
  }
  deferred = false;
  renderPane();
  const again = key ? byKey(key) : null;
  const target = again && (again.matches("[data-nav]") ? again : again.querySelector("[data-nav]:not(:disabled)"));
  if (target) target.focus();
  rove(document.activeElement);
  syncBusy();
}

function selectMod(id) {
  const mod = findMod(id);
  state.modId = mod ? mod.id : null;
  state.pageId = mod && mod.pages.length ? mod.pages[0].id : null;
  loadPage(state.pageId);
  render();
}

function clearSearch() {
  els.search.value = "";
  state.query = "";
  render();
}

function setValue(id, value, live) {
  if (state.rows[id]) state.rows[id].value = value;
  if (live) {
    liveIds.add(id);
    pending.set(id, value);
    if (!frame) frame = requestAnimationFrame(flushSets);
    return;
  }
  liveIds.delete(id);
  pending.delete(id);
  send({ type: "set", id, value, live: false });
}

function settle(id, value) {
  if (liveIds.has(id)) setValue(id, value, false);
}

function flushSets() {
  frame = 0;
  for (const [id, value] of pending) send({ type: "set", id, value, live: true });
  pending.clear();
}

function syncBusy() {
  if (escHeld) return;
  const a = document.activeElement;
  const typing = a === els.search || a.tagName === "SELECT" || textFocused();
  const on = Rows.capturing() || state.query !== "" || typing;
  if (on !== busyOn) {
    busyOn = on;
    send({ type: "busy", on });
  }
}

function onMenu(m) {
  document.activeElement.blur();
  escHeld = false;
  setTheme(m.theme);
  state.mods = Array.isArray(m.mods) ? m.mods : [];
  state.pages = {};
  state.rows = {};
  state.requested = {};
  liveIds.clear();
  els.search.value = "";
  state.query = "";
  loadAll();
  const mod = findMod(state.modId);
  if (!mod) {
    selectMod(state.mods.length ? state.mods[0].id : null);
  } else {
    if (!mod.pages.some((p) => p.id === state.pageId)) state.pageId = mod.pages.length ? mod.pages[0].id : null;
    loadPage(state.pageId);
    render();
  }
  if (document.activeElement === document.body) els.mods.querySelector('[aria-current="true"]')?.focus();
}

function onPage(m) {
  if (!m.id) return;
  state.pages[m.id] = m;
  for (const s of m.sections || []) for (const r of s.rows || []) state.rows[r.id] = r;
  if (state.query) loadAll();
  render();
}

function onRow(m) {
  const row = state.rows[m.id];
  if (!row) return;
  if (Rows.capturingId() === row.id) Rows.cancel();
  const sent = row.value;
  for (const k of ["value", "dirty", "invalid"]) if (k in m) row[k] = m[k];
  if (m.invalid) setStatus(row.kind === "key" ? "That key cannot be bound. Press another key." : "That value is not valid. The old value is kept.");
  else if (m.invalid === false) setStatus("");
  if (textFocused()) { deferred = true; return; }
  const old = byKey("row:" + row.id);
  if (!old) return;
  const kept = old.querySelector(".range") === document.activeElement && !m.invalid;
  if (kept) row.value = sent;
  const fresh = Rows.render(row);
  if (kept) {
    for (const sel of [".row-label", ".ghost"]) old.querySelector(sel).replaceWith(fresh.querySelector(sel));
    rove(document.activeElement);
    return;
  }
  const had = old.contains(document.activeElement);
  old.replaceWith(fresh);
  if (had) {
    const t = fresh.querySelector("[data-nav]:not(:disabled)");
    if (t) t.focus();
  }
  rove(document.activeElement);
}

function receive(m) {
  if (!m || typeof m !== "object") return;
  if (m.type === "menu") onMenu(m);
  else if (m.type === "page") onPage(m);
  else if (m.type === "row") onRow(m);
  else if (m.type === "theme") setTheme(m.mode);
}

function focusSearch() { els.search.focus(); els.search.select(); }

function onEscape(e, a) {
  e.preventDefault();
  if (a === els.search && state.query) {
    clearSearch();
  } else if (a === els.search || a.tagName === "SELECT") {
    a.blur();
  } else if (a.tagName === "INPUT" && a.type === "text") {
    a.value = a.dataset.value || "";
    a.blur();
  } else if (state.query) {
    clearSearch();
  }
}

function releaseEsc() { clearTimeout(escTimer); escHeld = false; syncBusy(); }
window.addEventListener("keydown", (e) => { if (e.key === "Escape") { clearTimeout(escTimer); escHeld = true; escTimer = setTimeout(releaseEsc, 400); } }, true);
window.addEventListener("keyup", (e) => { if (e.key === "Escape") releaseEsc(); });
window.addEventListener("blur", releaseEsc);
document.addEventListener("visibilitychange", releaseEsc);

document.addEventListener("keydown", (e) => {
  const a = document.activeElement;
  if (e.key === "Escape") {
    onEscape(e, a);
  } else if (e.ctrlKey && e.key.toLowerCase() === "f") {
    e.preventDefault();
    focusSearch();
  } else if (a === els.search) {
    if (e.key === "ArrowDown") {
      e.preventDefault();
      moveFocus(1);
    }
  } else if (e.key === "/" && a.tagName !== "INPUT" && a.tagName !== "SELECT") {
    e.preventDefault();
    focusSearch();
  } else if ((e.key === "ArrowLeft" || e.key === "ArrowRight") && a.closest(".obx-tabs, .obx-seg")) {
    e.preventDefault();
    const items = Array.from(a.parentElement.children);
    items[Math.min(items.length - 1, Math.max(0, items.indexOf(a) + (e.key === "ArrowRight" ? 1 : -1)))].focus();
  } else if ((e.key === "ArrowDown" || e.key === "ArrowUp") && a.tagName !== "SELECT") {
    e.preventDefault();
    moveFocus(e.key === "ArrowDown" ? 1 : -1);
  }
});

document.addEventListener("focusin", () => {
  rove(document.activeElement);
  syncBusy();
});

document.addEventListener("focusout", () => {
  setTimeout(() => {
    syncBusy();
    if (deferred && !textFocused()) render();
  }, 0);
});

els.search.addEventListener("input", () => {
  state.query = els.search.value.trim();
  if (state.query) loadAll();
  render();
});

els.close.addEventListener("click", () => send({ type: "close" }));

function fit() {
  document.documentElement.style.setProperty("--obx-zoom", String(Math.max(1, innerHeight / 1080)));
}

fit();
addEventListener("resize", fit);

Rows.init({
  set: setValue,
  settle,
  reset: (id) => send({ type: "reset", id }),
  action: (id) => send({ type: "action", id }),
  key: (id, code, mods) => send({ type: "key", id, code, mods }),
  changed: syncBusy,
  status: setStatus
});

const webview = window.chrome && window.chrome.webview;
if (webview) {
  webview.addEventListener("message", (e) => receive(e.data));
  post = (m) => webview.postMessage(m);
  send({ type: "hello" });
} else {
  const s = document.createElement("script");
  s.src = "preview.js";
  s.onload = () => {
    post = window.obxPreview(receive);
    send({ type: "hello" });
  };
  document.head.appendChild(s);
}
