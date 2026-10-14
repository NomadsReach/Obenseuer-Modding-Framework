const Rows = (() => {
  const modCodes = ["ControlLeft", "ControlRight", "ShiftLeft", "ShiftRight", "AltLeft", "AltRight"];
  const skipCodes = ["MetaLeft", "MetaRight"];
  const resettable = ["toggle", "choice", "segmented", "slider", "key", "text", "swatches"];
  let ctx = null;
  let cap = null;

  function el(tag, cls, text) {
    const e = document.createElement(tag);
    if (cls) e.className = cls;
    if (text !== undefined && text !== null) e.textContent = text;
    return e;
  }

  function btn(cls, text) {
    const b = el("button", cls, text);
    b.type = "button";
    b.dataset.nav = "";
    return b;
  }

  function fmt(v, dec) {
    return Number(v).toFixed(Math.max(0, Number(dec) || 0));
  }

  function fill(range) {
    const span = Number(range.max) - Number(range.min) || 1;
    range.style.setProperty("--fill", ((Number(range.value) - Number(range.min)) / span * 100) + "%");
  }

  function named(e, row) {
    e.setAttribute("aria-label", row.label || "");
    return e;
  }

  function toggle(row) {
    const on = row.value === "true";
    const b = named(btn("obx-switch", null), row);
    b.setAttribute("role", "switch");
    b.setAttribute("aria-checked", String(on));
    b.addEventListener("click", () => ctx.set(row.id, on ? "false" : "true", false));
    return b;
  }

  function segmented(row) {
    const group = el("div", "obx-seg");
    group.setAttribute("role", "group");
    for (const opt of row.options || []) {
      const b = btn("", opt);
      b.setAttribute("aria-pressed", String(opt === row.value));
      b.addEventListener("click", () => ctx.set(row.id, opt, false));
      group.appendChild(b);
    }
    return group;
  }

  function select(row) {
    const s = named(el("select", "obx-input select"), row);
    s.dataset.nav = "";
    for (const opt of row.options || []) {
      const o = el("option", "", opt);
      o.value = opt;
      s.appendChild(o);
    }
    s.value = row.value;
    s.addEventListener("change", () => ctx.set(row.id, s.value, false));
    const wrap = el("span", "select-wrap");
    wrap.appendChild(s);
    return wrap;
  }

  function slider(row) {
    const wrap = el("div", "slider");
    const r = named(el("input", "range"), row);
    r.type = "range";
    r.min = row.min;
    r.max = row.max;
    r.step = row.step;
    r.value = row.value;
    r.dataset.nav = "";
    const out = el("span", "readout", fmt(row.value, row.dec));
    fill(r);
    r.addEventListener("input", () => {
      out.textContent = fmt(r.value, row.dec);
      fill(r);
      ctx.set(row.id, r.value, true);
    });
    const settle = () => ctx.settle(row.id, r.value);
    for (const type of ["change", "pointerup", "keyup", "blur"]) r.addEventListener(type, settle);
    wrap.append(r, out);
    return wrap;
  }

  function keyChip(row) {
    const b = named(btn("key-chip", row.value || "None"), row);
    b.addEventListener("click", () => begin(row, b));
    return b;
  }

  function text(row) {
    const input = named(el("input", "obx-input text-input"), row);
    input.type = "text";
    input.maxLength = 2000;
    input.value = row.value || "";
    input.dataset.value = input.value;
    input.dataset.nav = "";
    input.spellcheck = false;
    input.autocomplete = "off";
    if (row.invalid) {
      input.classList.add("invalid");
      input.setAttribute("aria-invalid", "true");
    }
    input.addEventListener("keydown", (e) => {
      if (e.key === "Enter") {
        e.preventDefault();
        input.blur();
      }
    });
    input.addEventListener("change", () => {
      if (input.value !== row.value) ctx.set(row.id, input.value, false);
    });
    return input;
  }

  function swatches(row) {
    const group = el("div", "swatches");
    (row.options || []).forEach((color, i) => {
      const b = btn("swatch", "");
      if (/^#[0-9a-f]{6}$/i.test(color)) b.style.backgroundColor = color;
      b.setAttribute("aria-label", color);
      b.setAttribute("aria-pressed", String(String(i) === row.value));
      b.addEventListener("click", () => ctx.set(row.id, String(i), false));
      group.appendChild(b);
    });
    return group;
  }

  function action(row) {
    const b = btn("obx-btn primary sm", row.value || "Open");
    b.addEventListener("click", () => ctx.action(row.id));
    return b;
  }

  function control(row) {
    switch (row.kind) {
      case "toggle": return toggle(row);
      case "choice": return (row.options || []).length <= 4 ? segmented(row) : select(row);
      case "segmented": return segmented(row);
      case "slider": return slider(row);
      case "key": return keyChip(row);
      case "text": return text(row);
      case "swatches": return swatches(row);
      case "button": return action(row);
      case "readonly": return el("span", "muted", row.value);
      default: return null;
    }
  }

  function resetButton(row) {
    const b = btn("obx-btn ghost sm reset", "\u21BA");
    b.title = "Reset to default";
    b.setAttribute("aria-label", "Reset to default");
    b.disabled = row.value === row.def;
    b.addEventListener("click", () => ctx.reset(row.id));
    return b;
  }

  function render(row) {
    const wrap = el("div", "row");
    wrap.dataset.key = "row:" + row.id;
    const info = el("div", "row-text");
    const label = el("div", "row-label");
    if (row.dirty) label.appendChild(el("span", "dirty"));
    label.appendChild(el("span", "", row.label));
    info.appendChild(label);
    if (row.desc) info.appendChild(el("div", "row-desc", row.desc));
    const ctl = el("div", "row-control");
    const c = control(row);
    if (c) ctl.appendChild(c);
    if (resettable.includes(row.kind)) ctl.appendChild(resetButton(row));
    wrap.append(info, ctl);
    return wrap;
  }

  function begin(row, b) {
    if (cap) return;
    cap = { row, b, held: new Set() };
    b.classList.add("capturing");
    b.textContent = "Press a key (Esc)";
    window.addEventListener("keydown", onCapture, true);
    window.addEventListener("keyup", onCapture, true);
    window.addEventListener("blur", cancelCapture);
    document.addEventListener("pointerdown", onPointer, true);
    ctx.status("Press a key. Escape cancels.");
    ctx.changed();
  }

  function end() {
    const c = cap;
    cap = null;
    window.removeEventListener("keydown", onCapture, true);
    window.removeEventListener("keyup", onCapture, true);
    window.removeEventListener("blur", cancelCapture);
    document.removeEventListener("pointerdown", onPointer, true);
    c.b.classList.remove("capturing");
    c.b.textContent = c.row.value || "None";
    ctx.changed();
    return c;
  }

  function cancelCapture() {
    if (!cap) return;
    end();
    ctx.status("");
  }

  function onPointer(e) {
    if (e.target !== cap.b) cancelCapture();
  }

  function onCapture(e) {
    e.preventDefault();
    e.stopPropagation();
    if (modCodes.includes(e.code)) {
      if (e.type === "keydown") cap.held.add(e.code);
      else cap.held.delete(e.code);
      return;
    }
    if (e.type !== "keydown" || e.repeat) return;
    if (e.code === "Escape") {
      cancelCapture();
      return;
    }
    if (skipCodes.includes(e.code)) return;
    if (e.code === "F10") {
      ctx.status("F10 cannot be bound here. Press another key, or Escape.");
      return;
    }
    const c = end();
    ctx.status("");
    ctx.key(c.row.id, e.code, Array.from(c.held));
  }

  return {
    init(c) { ctx = c; },
    el,
    btn,
    render,
    cancel: cancelCapture,
    capturing() { return cap !== null; },
    capturingId() { return cap ? cap.row.id : null; }
  };
})();
