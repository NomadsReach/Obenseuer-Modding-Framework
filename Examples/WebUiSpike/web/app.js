(function () {
  "use strict";

  const RANGES = [
    { id: "1H", n: 60, step: 0.12 },
    { id: "1D", n: 78, step: 0.5 },
    { id: "1W", n: 35, step: 1.2 },
    { id: "1M", n: 22, step: 2.2 },
  ];
  const DAYS = ["Mon", "Tue", "Wed", "Thu", "Fri"];
  const THEMES = ["light", "dark", "system"];
  const PAD = { l: 12, r: 72, t: 16, b: 16 };
  const HOLDINGS = { GSH: 12, OSP: 40, SBK: 6 };
  const TABS = {
    markets: { title: "Markets", url: "https://exchange.stalburg.com/markets" },
    portfolio: { title: "Portfolio", url: "https://exchange.stalburg.com/portfolio" },
    news: { title: "News", url: "https://exchange.stalburg.com/news" },
  };
  const STOCKS = [
    { sym: "OSP", name: "Open Sewer Power", price: 42.5, vol: 0.9, shares: 1.8e9, div: 3.1 },
    { sym: "SBK", name: "Stalburg Savings Bank", price: 118.4, vol: 0.5, shares: 5.2e8, div: 4.2 },
    { sym: "MNS", name: "Mines and Smelting OS", price: 27.9, vol: 1.4, shares: 9e8, div: 0 },
    { sym: "GSH", name: "Goldshop Holdings", price: 196.2, vol: 1.1, shares: 2.1e8, div: 1.4 },
    { sym: "RBF", name: "Riverbend Foundry", price: 63.1, vol: 0.8, shares: 3.4e8, div: 2.6 },
    { sym: "NGT", name: "Northgate Telecom", price: 84.7, vol: 0.7, shares: 7.6e8, div: 3.8 },
    { sym: "LTN", name: "Lantern Pharma", price: 151.3, vol: 1.6, shares: 1.1e8, div: 0 },
    { sym: "CPL", name: "Copperline Freight", price: 19.4, vol: 1.2, shares: 1.5e9, div: 0.9 },
  ];
  const BY = {};
  const host = window.chrome && window.chrome.webview;
  const css = getComputedStyle(document.documentElement);
  const state = { sym: "GSH", ri: 1, side: "buy", cash: null, hover: -1, data: [] };
  const $ = (id) => document.getElementById(id);

  function send(msg) {
    if (host) host.postMessage(msg);
  }

  function token(name) {
    return css.getPropertyValue(name).trim();
  }

  function seed(text) {
    let h = 2166136261;
    for (let i = 0; i < text.length; i++) h = Math.imul(h ^ text.charCodeAt(i), 16777619);
    return h >>> 0;
  }

  function prng(a) {
    return () => {
      a = (a + 0x6d2b79f5) | 0;
      let t = Math.imul(a ^ (a >>> 15), 1 | a);
      t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
      return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
    };
  }

  function series(s, r) {
    const rnd = prng(seed(s.sym + r.id));
    const k = (s.vol / 100) * r.step;
    const v = [1];
    for (let i = 1; i < r.n; i++) v.push(v[i - 1] * (1 + (rnd() * 2 - 1) * k));
    return v.map((x) => (s.price * x) / v[r.n - 1]);
  }

  function hhmm(min) {
    return ("0" + Math.floor(min / 60)).slice(-2) + ":" + ("0" + (min % 60)).slice(-2);
  }

  function stamp(r, i) {
    if (r.id === "1H") return i === r.n - 1 ? "Now" : `${r.n - 1 - i} min ago`;
    if (r.id === "1D") return hhmm(570 + i * 5);
    if (r.id === "1W") return `${DAYS[Math.floor(i / 7)]} ${hhmm(570 + (i % 7) * 65)}`;
    return `Day ${i + 1}`;
  }

  function money(n) {
    return n.toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  function sign(n) {
    return n >= 0 ? "+" : "";
  }

  function pct(v) {
    return `${sign(v)}${v.toFixed(2)}%`;
  }

  function compact(n) {
    const [div, unit] = n >= 1e9 ? [1e9, "B"] : n >= 1e6 ? [1e6, "M"] : n >= 1e3 ? [1e3, "K"] : [1, ""];
    return (n / div).toFixed(div > 1 ? 2 : 0) + unit;
  }

  function change(d) {
    const abs = d[d.length - 1] - d[0];
    return { abs, pct: (abs / d[0]) * 100 };
  }

  function tone(v) {
    return v >= 0 ? "obx-up" : "obx-down";
  }

  function sparkPoints(d) {
    const lo = Math.min(...d), hi = Math.max(...d), span = hi - lo || 1;
    return d
      .map((v, i) => `${((i * 56) / (d.length - 1)).toFixed(1)},${(2 + (1 - (v - lo) / span) * 16).toFixed(1)}`)
      .join(" ");
  }

  function at(sym) {
    return BY[sym];
  }

  function stock() {
    return BY[state.sym];
  }

  function load() {
    state.data = series(stock(), RANGES[state.ri]);
    state.hover = -1;
  }

  function buildWatch() {
    $("watch").innerHTML = STOCKS.map((s) => {
      const c = change(s.day), t = tone(c.pct);
      return (
        `<tr class="watch-row" data-sym="${s.sym}">` +
        `<td><span class="meta"><span class="sym">${s.sym}</span><span class="name">${s.name}</span></span></td>` +
        `<td><svg class="spark ${t}" viewBox="0 0 56 20" preserveAspectRatio="none">` +
        `<polyline fill="none" stroke="currentColor" stroke-width="1.5" points="${sparkPoints(s.day)}"/></svg></td>` +
        `<td class="quote-cell"><span class="price">${money(s.price)}</span><span class="obx-badge ${t}">${pct(c.pct)}</span></td>` +
        `</tr>`
      );
    }).join("");
  }

  function buildChips() {
    $("chips").innerHTML = RANGES.map((r, i) => `<button type="button" data-i="${i}">${r.id}</button>`).join("");
  }

  function buildPositions() {
    $("positions").innerHTML = Object.keys(HOLDINGS).map((sym) => {
      const s = at(sym), q = HOLDINGS[sym], c = change(s.day);
      return (
        `<tr class="pos-row" data-sym="${sym}">` +
        `<td><span class="sym">${sym}</span><span class="sub">${q} shares</span></td>` +
        `<td class="num"><span class="obx-value">${money(q * s.price)}</span><span class="obx-badge ${tone(c.pct)}">${pct(c.pct)}</span></td>` +
        `</tr>`
      );
    }).join("");
  }

  function renderQuote() {
    const s = stock(), c = change(state.data);
    $("sym").textContent = s.sym;
    $("name").textContent = s.name;
    $("price").textContent = money(s.price);
    $("change").className = `change ${tone(c.abs)}`;
    $("change").innerHTML = `${sign(c.abs)}${money(c.abs)} (${pct(c.pct)}) <span class="tf">${RANGES[state.ri].id}</span>`;
  }

  function renderStats() {
    const s = stock(), d = s.day;
    const cards = [
      ["Open", money(d[0])],
      ["High", money(Math.max(...d))],
      ["Low", money(Math.min(...d))],
      ["Volume", compact(s.volume)],
      ["Market cap", compact(s.price * s.shares)],
      ["Dividend yield", `${s.div.toFixed(1)}%`],
    ];
    $("stats").innerHTML = cards
      .map(([k, v]) => `<div class="obx-card"><span class="obx-label">${k}</span><span class="obx-value">${v}</span></div>`)
      .join("");
  }

  function qty() {
    const n = Number($("qty").value);
    return Number.isInteger(n) && n > 0 ? n : 0;
  }

  function maxQty() {
    const s = stock();
    if (state.side === "sell") return HOLDINGS[s.sym] || 0;
    return state.cash === null ? 0 : Math.floor(state.cash / s.price);
  }

  function renderTicket() {
    const s = stock(), buy = state.side === "buy", held = HOLDINGS[s.sym] || 0;
    const q = qty(), cost = q * s.price;
    let msg = "";
    if (buy && state.cash === null) msg = "Waiting for wallet.";
    else if (!q) msg = "Enter a whole number of shares.";
    else if (buy && cost > state.cash) msg = "Not enough buying power.";
    else if (!buy && q > held) msg = `You hold ${held} shares.`;
    $("submit").disabled = msg !== "";
    $("note").textContent = msg;
    $("submit").className = `obx-btn block ${buy ? "primary" : "danger"}`;
    $("submit").textContent = `${buy ? "Buy" : "Sell"} ${s.sym}`;
    $("est").previousElementSibling.textContent = buy ? "Est. cost" : "Est. proceeds";
    $("est").textContent = money(cost);
    $("power").previousElementSibling.textContent = buy ? "Buying power" : "Shares held";
    $("power").textContent = buy ? (state.cash === null ? "—" : money(state.cash)) : String(held);
    document.querySelectorAll(".side").forEach((b) => b.setAttribute("aria-pressed", String(b.dataset.side === state.side)));
  }

  function renderAccount() {
    const known = state.cash !== null;
    let holdings = 0, pl = 0;
    Object.keys(HOLDINGS).forEach((sym) => {
      const s = at(sym), q = HOLDINGS[sym];
      holdings += q * s.price;
      pl += q * change(s.day).abs;
    });
    $("cash").textContent = known ? money(state.cash) : "—";
    $("portfolio").textContent = known ? money(state.cash + holdings) : "—";
    $("dayPl").textContent = `${sign(pl)}${money(pl)}`;
    $("dayPl").className = `obx-value ${tone(pl)}`;
  }

  function trace(ctx, d, xs, ys) {
    ctx.beginPath();
    d.forEach((v, i) => {
      if (i) ctx.lineTo(xs(i), ys(v));
      else ctx.moveTo(xs(i), ys(v));
    });
  }

  function showTip(hx, hy, w) {
    const el = $("tip");
    el.innerHTML = `<strong>${money(state.data[state.hover])}</strong> <span class="obx-label">${stamp(RANGES[state.ri], state.hover)}</span>`;
    let x = hx + 12;
    if (x + el.offsetWidth > w) x = hx - el.offsetWidth - 12;
    el.style.transform = `translate(${x}px, ${Math.max(PAD.t, hy - 44)}px)`;
    el.classList.add("show");
  }

  function draw() {
    const cv = $("chart"), ctx = cv.getContext("2d"), dpr = window.devicePixelRatio || 1;
    const w = cv.clientWidth, h = cv.clientHeight, d = state.data, n = d.length;
    cv.width = Math.round(w * dpr);
    cv.height = Math.round(h * dpr);
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    ctx.clearRect(0, 0, w, h);
    let lo = Math.min(...d), hi = Math.max(...d);
    const pad = (hi - lo) * 0.08 || 1;
    lo -= pad;
    hi += pad;
    const pw = w - PAD.l - PAD.r, ph = h - PAD.t - PAD.b;
    const xs = (i) => PAD.l + (i * pw) / (n - 1);
    const ys = (v) => PAD.t + (1 - (v - lo) / (hi - lo)) * ph;
    const line = token("--obx-amber-line"), muted = token("--obx-muted");
    ctx.font = `12px ${getComputedStyle(document.body).fontFamily}`;
    ctx.textBaseline = "middle";
    ctx.lineWidth = 1;
    ctx.strokeStyle = token("--obx-grid");
    ctx.fillStyle = muted;
    for (let k = 0; k <= 4; k++) {
      const v = lo + ((hi - lo) * k) / 4, y = Math.round(ys(v)) + 0.5;
      ctx.beginPath();
      ctx.moveTo(PAD.l, y);
      ctx.lineTo(w - PAD.r, y);
      ctx.stroke();
      ctx.fillText(money(v), w - PAD.r + 8, y);
    }
    const g = ctx.createLinearGradient(0, PAD.t, 0, PAD.t + ph);
    g.addColorStop(0, token("--obx-chart-fill-top"));
    g.addColorStop(1, token("--obx-chart-fill-bottom"));
    trace(ctx, d, xs, ys);
    ctx.lineTo(xs(n - 1), PAD.t + ph);
    ctx.lineTo(xs(0), PAD.t + ph);
    ctx.closePath();
    ctx.fillStyle = g;
    ctx.fill();
    trace(ctx, d, xs, ys);
    ctx.strokeStyle = line;
    ctx.lineWidth = 2;
    ctx.lineJoin = "round";
    ctx.stroke();
    if (state.hover < 0) {
      $("tip").classList.remove("show");
      return;
    }
    const hx = xs(state.hover), hy = ys(d[state.hover]);
    ctx.setLineDash([3, 3]);
    ctx.lineWidth = 1;
    ctx.strokeStyle = muted;
    ctx.beginPath();
    ctx.moveTo(hx, PAD.t);
    ctx.lineTo(hx, PAD.t + ph);
    ctx.moveTo(PAD.l, hy);
    ctx.lineTo(w - PAD.r, hy);
    ctx.stroke();
    ctx.setLineDash([]);
    ctx.beginPath();
    ctx.arc(hx, hy, 4, 0, Math.PI * 2);
    ctx.fillStyle = line;
    ctx.fill();
    showTip(hx, hy, w);
  }

  function indexAt(x) {
    const r = $("chart").getBoundingClientRect(), n = state.data.length;
    const i = Math.round(((x - r.left - PAD.l) / (r.width - PAD.l - PAD.r)) * (n - 1));
    return Math.max(0, Math.min(n - 1, i));
  }

  function render() {
    document.querySelectorAll(".watch-row").forEach((row) => row.classList.toggle("selected", row.dataset.sym === state.sym));
    document.querySelectorAll("#chips button").forEach((b) => b.setAttribute("aria-pressed", String(Number(b.dataset.i) === state.ri)));
    renderQuote();
    renderStats();
    renderTicket();
    renderAccount();
    draw();
  }

  function select(sym) {
    state.sym = sym;
    load();
    render();
  }

  function setTab(tab) {
    document.body.dataset.tab = tab;
    document.querySelectorAll(".obx-tab").forEach((t) => {
      const on = t.dataset.tab === tab;
      t.classList.toggle("active", on);
      t.setAttribute("aria-selected", String(on));
    });
    $("url").textContent = TABS[tab].url;
    $("title").textContent = `Stalburg Exchange - ${TABS[tab].title}`;
    $("soon").textContent = tab === "news" ? "News is coming soon." : "Portfolio analytics are coming soon.";
  }

  function setTheme(mode) {
    if (!THEMES.includes(mode)) return;
    if (mode === "system") delete document.documentElement.dataset.theme;
    else document.documentElement.dataset.theme = mode;
    document.querySelectorAll("[data-theme-mode]").forEach((b) => b.setAttribute("aria-pressed", String(b.dataset.themeMode === mode)));
    draw();
  }

  function showClock() {
    $("clock").textContent = new Date().toTimeString().slice(0, 5);
  }

  STOCKS.forEach((s) => {
    s.day = series(s, RANGES[1]);
    s.volume = Math.round(prng(seed(`${s.sym}V`))() * 8e6 + 2e5);
    BY[s.sym] = s;
  });
  buildWatch();
  buildChips();
  buildPositions();
  load();
  setTab("markets");
  showClock();
  setInterval(showClock, 30000);
  render();

  document.querySelector(".obx-tabs").addEventListener("click", (e) => {
    const t = e.target.closest(".obx-tab");
    if (t) setTab(t.dataset.tab);
  });
  $("watch").addEventListener("click", (e) => {
    const row = e.target.closest(".watch-row");
    if (row) select(row.dataset.sym);
  });
  $("positions").addEventListener("click", (e) => {
    const row = e.target.closest(".pos-row");
    if (row) select(row.dataset.sym);
  });
  $("chips").addEventListener("click", (e) => {
    const b = e.target.closest("button");
    if (!b) return;
    state.ri = Number(b.dataset.i);
    load();
    render();
  });
  document.querySelector(".side-toggle").addEventListener("click", (e) => {
    const b = e.target.closest(".side");
    if (!b) return;
    state.side = b.dataset.side;
    render();
  });
  document.querySelector(".theme-toggle").addEventListener("click", (e) => {
    const b = e.target.closest("[data-theme-mode]");
    if (!b) return;
    setTheme(b.dataset.themeMode);
    send({ type: "theme", mode: b.dataset.themeMode });
  });
  document.querySelector(".presets").addEventListener("click", (e) => {
    const b = e.target.closest("button");
    if (!b) return;
    $("qty").value = b.dataset.q === "max" ? maxQty() : b.dataset.q;
    render();
  });
  $("minus").addEventListener("click", () => {
    $("qty").value = Math.max(1, qty() - 1);
    render();
  });
  $("plus").addEventListener("click", () => {
    $("qty").value = qty() + 1;
    render();
  });
  $("qty").addEventListener("input", render);
  $("submit").addEventListener("click", () => {
    const s = stock(), q = qty();
    send({ type: "order", side: state.side, symbol: s.sym, qty: q, orderType: "market", price: s.price });
    $("note").textContent = `Sent ${state.side.toUpperCase()} ${q} ${s.sym} to the exchange.`;
  });
  $("search").addEventListener("input", (e) => {
    const q = e.target.value.trim().toLowerCase();
    document.querySelectorAll(".watch-row").forEach((row) => {
      const s = at(row.dataset.sym);
      row.hidden = q !== "" && !s.sym.toLowerCase().includes(q) && !s.name.toLowerCase().includes(q);
    });
  });
  $("search").addEventListener("keydown", (e) => {
    if (e.key !== "Enter") return;
    const first = document.querySelector(".watch-row:not([hidden])");
    if (first) select(first.dataset.sym);
  });
  $("chart").addEventListener("mousemove", (e) => {
    state.hover = indexAt(e.clientX);
    draw();
  });
  $("chart").addEventListener("mouseleave", () => {
    state.hover = -1;
    draw();
  });
  $("close").addEventListener("click", () => send({ type: "close" }));
  document.addEventListener("keydown", (e) => {
    if (e.key === "Escape") send({ type: "close" });
  });
  window.addEventListener("resize", draw);
  window.matchMedia("(prefers-color-scheme: dark)").addEventListener("change", draw);
  window.obx = { theme: { set: setTheme } };
  window.addEventListener("error", (e) => send({ type: "error", message: String(e.message) }));
  window.addEventListener("load", () => send({ type: "ready" }));
  if (host) {
    host.addEventListener("message", (e) => {
      const m = typeof e.data === "string" ? JSON.parse(e.data) : e.data;
      if (m && m.type === "theme") setTheme(m.mode);
      if (m && m.type === "wallet") {
        state.cash = Number(m.cash);
        render();
      }
    });
  }
})();
