window.obxPreview = function (receive) {
  function tok(name) {
    return getComputedStyle(document.documentElement).getPropertyValue(name).trim();
  }
  const menu = {
    v: 1,
    type: "menu",
    theme: "dark",
    mods: [
      {
        id: "obenseuer.moddingframework", name: "Obenseuer Modding Framework", version: "0.12.1",
        pages: [
          { id: "p1_0", title: "Settings", kind: "config" },
          { id: "p1_3", title: "Controls", kind: "web" }
        ]
      },
      {
        id: "example.hello", name: "Example Hello", version: "1.0.0",
        pages: [
          { id: "p1_1", title: "Settings", kind: "config" },
          { id: "p1_4", title: "Appearance", kind: "classic" }
        ]
      },
      {
        id: "example.counter", name: "Example Counter", version: "",
        pages: [{ id: "p1_2", title: "Settings", kind: "config" }]
      }
    ]
  };
  const sections = {
    "p1_0": [
      { name: "General", rows: [
        { id: "c1_1", kind: "toggle", label: "ThirdPersonToggle", desc: "Switches the third-person camera.", value: "true", def: "false", dirty: true },
        { id: "c1_2", kind: "slider", label: "AutoCraftRadius", desc: "Metres from the workbench.", value: "6", def: "6", min: 0, max: 50, step: 0.01, dec: 2, dirty: false },
        { id: "c1_3", kind: "slider", label: "Capacity", desc: "", value: "20", def: "10", min: 1, max: 100, step: 1, dec: 0, dirty: true },
        { id: "c1_4", kind: "choice", label: "Currency", desc: "Five options use a list.", value: "USD", def: "USD", options: ["USD", "EUR", "GBP", "JPY", "CHF"], dirty: false },
        { id: "c1_5", kind: "segmented", label: "Mode", desc: "Three options use segments.", value: "Auto", def: "Off", options: ["Off", "Auto", "Manual"], dirty: true },
        { id: "c1_6", kind: "text", label: "Threshold", desc: "A number. Enter commits.", value: "1.5", def: "1.5", invalid: false, dirty: false },
        { id: "c1_7", kind: "text", label: "Nickname", desc: "", value: "Alpha", def: "Alpha", invalid: false, dirty: false },
        { id: "c1_8", kind: "readonly", label: "Version", desc: "Read only.", value: "0.12.1", def: "0.12.1", dirty: false }
      ] },
      { name: "Keys", rows: [
        { id: "c1_9", kind: "key", label: "ToggleKey", desc: "Switches the camera.", value: "F6", def: "F6", dirty: false },
        { id: "c1_10", kind: "key", label: "PullHotkey", desc: "Pulls items to the chest.", value: "Ctrl+K", def: "F8", dirty: true },
        { id: "c1_11", kind: "button", label: "Open folder", desc: "Opens the config folder.", value: "Open", dirty: false },
        { id: "c1_12", kind: "swatches", label: "Accent", desc: "Pick a colour.", value: "1", def: "0", options: [tok("--gm-ring-on"), tok("--gm-red"), tok("--gm-amber"), tok("--gm-muted")], dirty: true },
        { id: "c1_13", kind: "note", label: "Notes", desc: "A note row has no control." }
      ] }
    ],
    "p1_3": [
      { name: "Keyboard", rows: [
        { id: "c1_14", kind: "key", label: "Inventory", desc: "Opens the bag.", value: "Tab", def: "Tab", dirty: false },
        { id: "c1_15", kind: "key", label: "Crouch", desc: "Hold to crouch.", value: "C", def: "C", dirty: false }
      ] }
    ],
    "p1_1": [
      { name: "Body", rows: [
        { id: "c1_16", kind: "slider", label: "Height", desc: "Scale of the body.", value: "1", def: "1", min: 0.8, max: 1.2, step: 0.01, dec: 2, dirty: false }
      ] }
    ],
    "p1_2": [
      { name: "Rules", rows: [
        { id: "c1_17", kind: "toggle", label: "RespectOwnership", desc: "Only pool owned containers.", value: "false", def: "true", dirty: true }
      ] }
    ]
  };
  const rows = {};
  for (const id of Object.keys(sections)) {
    for (const s of sections[id]) for (const r of s.rows) rows[r.id] = r;
  }
  function echo(row) {
    return { v: 1, type: "row", id: row.id, value: row.value, dirty: row.value !== row.def, invalid: !!row.invalid };
  }
  function keyName(code, mods) {
    const held = mods.map((m) => m.replace(/Left$|Right$/, ""));
    return held.concat(code.replace(/^Key/, "").replace(/^Digit/, "")).join("+");
  }
  return function post(msg) {
    if (msg.type === "hello") {
      receive(menu);
    } else if (msg.type === "page") {
      const copy = JSON.parse(JSON.stringify(sections[msg.id] || []));
      receive({ v: 1, type: "page", id: msg.id, sections: copy });
    } else if (msg.type === "set") {
      const row = rows[msg.id];
      if (!row) return;
      if (msg.live) {
        row.value = msg.value;
        return;
      }
      if (row.label === "Threshold" && isNaN(Number(msg.value))) row.invalid = true;
      else {
        row.value = msg.value;
        row.invalid = false;
      }
      receive(echo(row));
    } else if (msg.type === "reset") {
      const row = rows[msg.id];
      if (row) {
        row.value = row.def;
        row.invalid = false;
        receive(echo(row));
      }
    } else if (msg.type === "key") {
      const row = rows[msg.id];
      if (row) {
        row.value = keyName(msg.code, msg.mods || []);
        receive(echo(row));
      }
    }
  };
};
