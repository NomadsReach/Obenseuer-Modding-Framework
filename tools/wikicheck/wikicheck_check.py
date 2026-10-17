"""Offline analysis for wikicheck: match the framework docs against the cached wiki and game data."""

import difflib
import json
import re
from collections import Counter, defaultdict
from datetime import datetime
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
KINDS = [
    ("animal", r"animal|livestock"),
    ("weapon", r"weapon|firearm|melee|wieldable|\bguns?\b"),
    ("minigame", r"\bcards?\b|casino|gambl|lotter|minigame|\bbet(?:s|ting)?\b|poker"),
    ("currency", r"currenc|\bmoney\b|\bcoins?\b|\bcash\b"),
    ("shop", r"\bshops?\b|\bstalls?\b|vendor|\bstores?\b"),
    ("npc", r"\bnpcs?\b|character|tenant|\bpeople\b|resident"),
    ("quest", r"quest|mission|\bstory\b"),
    ("tool/device", r"\btools?\b|device|gadget|\bradios?\b|\bphones?\b|detector|appliance"),
    ("location", r"location|\bplaces?\b|\bworld\b|district|\btowns?\b"),
    ("item", r"\bitems?\b|food|drink|consumable|crafting|recipe|alcohol|meal"),
    ("mechanic", r"mechanic|\bsystems?\b|\bfeatures?\b|\bsettings?\b"),
]
KIND_RES = [(kind, re.compile(pattern, re.I)) for kind, pattern in KINDS]
ROLE_RULES = [
    ("player-run", re.compile(r"player[- ]?(?:run|owned|operated)|player's|players'", re.I), ("polarity", "player")),
    ("npc-run", re.compile(r"npc[- ]?(?:run|owned|operated)|\bnpc's\b", re.I), ("polarity", "npc")),
    ("NPC", re.compile(r"\bnpcs?\b", re.I), ("kind", "npc")),
    ("animal", re.compile(r"\banimals?\b", re.I), ("kind", "animal")),
    ("weapon", re.compile(r"\bweapons?\b", re.I), ("kind", "weapon")),
    ("currency", re.compile(r"\bcurrenc(?:y|ies)\b", re.I), ("kind", "currency")),
    ("real money", re.compile(r"real[- ]money|real[- ]world (?:money|currency)", re.I), ("kind", "currency")),
]
NPC_WORDS = re.compile(r"\bnpcs?\b|\bcharacters?\b|\btenants?\b", re.I)
PLAYER_WORDS = re.compile(r"\bplayers?\b", re.I)
SKIP_KEYS = {"obenseuer", "obenseuercore", "obenseuer modding framework", "stalburg", "stalburg wiki", "infra"}
CODE_SPAN = re.compile(r"`([^`\n]+)`")
CAP_RE = re.compile(r"\b[A-Z][A-Za-z0-9]*(?:\s+[A-Z][A-Za-z0-9]*)+")
WORD_RE = re.compile(r"[A-Za-z]{3,}")
CAMEL_TOKEN = re.compile(r"\b[A-Z][A-Za-z0-9]+\b")
HEADING_RE = re.compile(r"(#{1,6})\s+(.*)")
STOP = frozenset("""
a about above after again against all also am an and any are as at be because been before being below
between both but by can cannot could did do does doing down during each either else even ever every few
for from further get gets given has have having he her here hers him his how however i if in into is it
its itself just let may me might more most must my neither no nor not now of off on once only or other
our out over own per same she should so some such than that the their them then there these they this
those through to too under until up upon us use used uses using very via was we were what when where
whether which while who whom why will with within without would yet you your yours yourself etc eg ie
see note notes example examples true false null none new old first last next one two three default
defaults value values name names type types id ids class classes method methods property properties
event events field fields return returns result results param parameter parameters public private static
void string int bool float
""".split())


def norm(text):
    return " ".join(re.findall(r"[^\W_]+", text.casefold()))


def seg(text):
    return re.sub(r"[^a-z0-9]", "", text.lower())


def strip_disambig(title):
    return re.sub(r"\s*\([^()]*\)$", "", title).strip()


def title_keys(title, ns=0):
    text = title.split(":", 1)[1] if ns and ":" in title else title
    text = strip_disambig(re.sub(r"^(?:obenseuer/)+", "", text, flags=re.I))
    keys = {norm(text)}
    if "/" in text:
        keys.add(norm(text.rsplit("/", 1)[1]))
    return {k for k in keys if k}


def camel_split(text):
    return re.sub(r"(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", " ", text)


def is_camel(text):
    return text[:1].isupper() and sum(c.isupper() for c in text) >= 2 and any(c.islower() for c in text)


def base_keys(raw):
    return {k for k in (norm(raw), norm(camel_split(raw))) if k}


def term_keys(raw):
    keys = base_keys(raw)
    for key in list(keys):
        if len(key) > 4 and key.endswith("es"):
            keys.add(key[:-2])
        if len(key) > 3 and key.endswith("s"):
            keys.add(key[:-1])
    return keys


def heading_kind(text):
    for kind, rx in KIND_RES:
        if rx.search(text):
            return kind
    return None


def kind_set(text):
    return {kind for kind, rx in KIND_RES if rx.search(text)}


def trim(text, limit):
    text = text.replace("\u2014", "-").replace("\u2013", "-").replace("\n", " ").strip()
    return text if len(text) <= limit else text[: limit - 3] + "..."


def strip_md(text):
    text = re.sub(r"\[([^\]]*)\]\([^)]*\)", r"\1", text)
    return text.replace("`", "").replace("*", "").strip()


def code_terms(span):
    text = span.strip()
    if re.fullmatch(r"[A-Za-z_][\w.]*(?:\(\))?", text):
        return [part for part in text.removesuffix("()").split(".") if part]
    if " " in text and re.fullmatch(r"[A-Za-z][A-Za-z ]*[A-Za-z]", text):
        return [text]
    return []


def line_terms(line):
    found = []
    for span in CODE_SPAN.findall(line):
        found += [(term, "code") for term in code_terms(span)]
    prose = CODE_SPAN.sub(" ", line)
    prose = re.sub(r"\[([^\]]*)\]\([^)]*\)", r"\1", prose)
    prose = re.sub(r"https?://\S+", " ", prose)
    for phrase in CAP_RE.findall(prose):
        words = phrase.split()
        while words and words[0].lower() in STOP:
            words.pop(0)
        if len(words) >= 2:
            found.append((" ".join(words), "phrase"))
    found += [(word, "word") for word in WORD_RE.findall(prose) if word.lower() not in STOP]
    return found


def scan_doc(rel, text):
    rows, stack, fence = [], [], False
    for n, line in enumerate(text.splitlines(), 1):
        stripped = line.strip()
        found = []
        if stripped.startswith("```"):
            fence = not fence
        elif fence:
            found = [(token, "code") for token in CAMEL_TOKEN.findall(line) if is_camel(token)]
        else:
            match = HEADING_RE.match(stripped)
            if match:
                level = len(match.group(1))
                stack = [h for h in stack if h[0] < level] + [(level, strip_md(match.group(2)))]
            found = line_terms(line)
        heading = " > ".join(title for _, title in stack)
        section = next((k for k in map(heading_kind, [t for _, t in reversed(stack)]) if k), None)
        rows.append((rel, n, heading, section, line, list(dict.fromkeys(found))))
    return rows


def terms_of(rows):
    for rel, n, heading, section, _line, found in rows:
        for raw, src in found:
            yield rel, n, heading, section, raw, src


def first_sentence(wikitext):
    text = re.sub(r"<!--.*?-->|<ref[^>]*/>|<ref[^>]*>.*?</ref>", "", wikitext, flags=re.S | re.I)
    previous = None
    while previous != text:
        previous = text
        text = re.sub(r"\{\{[^{}]*\}\}", "", text)
    text = re.sub(r"\{\|.*?\|\}", "", text, flags=re.S)
    text = re.sub(r"\[\[(?:File|Image|Category):[^\]]*\]\]", "", text, flags=re.I)
    text = re.sub(r"\[\[(?:[^\]|]*\|)?([^\]]*)\]\]", r"\1", text)
    text = re.sub(r"\[https?://\S+\s*([^\]]*)\]", r"\1", text)
    text = re.sub(r"'{2,}", "", text)
    text = re.sub(r"<[^>]+>", "", text)
    for line in text.splitlines():
        line = line.strip()
        if line and line[0] not in "=|!{}*#;:" and re.search(r"[A-Za-z]", line):
            return re.split(r"(?<=[.!?])\s", line, maxsplit=1)[0].strip()[:300]
    return ""


def parse_infobox(wikitext):
    match = re.search(r"\{\{\s*([^|}\n]*infobox[^|}\n]*)", wikitext, re.I)
    if not match:
        return None
    body = wikitext[match.start(): match.start() + 4000]
    fields = {k.lower(): v.strip() for k, v in re.findall(r"\|\s*(type|category|class|kind|people)\s*=\s*([^|\n}]*)", body, re.I)}
    return {"template": match.group(1).strip(), "fields": fields}


def redirect_target(wikitext):
    match = re.match(r"\s*#redirect\s*\[\[([^\]|#]+)", wikitext, re.I)
    return match.group(1).strip() if match else None


def wiki_entry(page):
    info = parse_infobox(page["wikitext"])
    kinds = kind_set(" ".join(page["categories"]))
    if info:
        kinds |= kind_set(" ".join([info["template"], *info["fields"].values()]))
    links = {seg(target) for target in re.findall(r"\[\[([^\]|#]+)", page["wikitext"])}
    return {"title": page["title"], "source": "wiki", "categories": page["categories"], "kinds": kinds,
            "first": first_sentence(page["wikitext"]), "nospace": seg(page["wikitext"]), "infobox": info,
            "redirect_target": page["redirect_target"], "links": links, "wikitext": page["wikitext"]}


def game_entry(title, source, categories, kinds, first=""):
    return {"title": title, "source": source, "categories": categories, "kinds": kinds, "first": first,
            "nospace": "", "infobox": None, "redirect_target": None, "links": set(), "wikitext": ""}


def skipped(raw):
    return bool(term_keys(raw) & SKIP_KEYS)


def defines(raw, first):
    key = norm(raw)
    return bool(key) and re.match(rf"(?:(?:a|an|the) )?{re.escape(key)}\b", norm(first)) is not None


def strength(raw, src, hits):
    return "strong" if src != "word" or raw[:1].isupper() or any(defines(raw, e["first"]) for e in hits) else "weak"


def read_json(path):
    raw = path.read_bytes()
    text = raw.decode("utf-16") if raw[:2] in (b"\xff\xfe", b"\xfe\xff") else raw.decode("utf-8-sig")
    try:
        return json.loads(text)
    except json.JSONDecodeError:
        return json.loads(f"[{text}]")


def path_keys(node, out):
    if isinstance(node, dict):
        for key, value in node.items():
            if isinstance(value, str) and "path" in key.lower() and value:
                out.append(seg(value.replace("\\", "/").rsplit("/", 1)[-1]))
            else:
                path_keys(value, out)
    elif isinstance(node, list):
        for value in node:
            path_keys(value, out)
    return out


def load_game(game_dir):
    entries, internal = [], defaultdict(set)
    for item in read_json(game_dir / "Items.json"):
        title = (item.get("Title") or "").strip()
        if not title:
            continue
        cats = item.get("Categories") or []
        entries.append(game_entry(title, "Items.json", cats, {"item"} | kind_set(" ".join(cats)),
                                  first_sentence(item.get("Description") or "")))
        for key in path_keys(item, []):
            internal[key].add(title)
    characters = {}
    for char in read_json(game_dir / "Characters.json"):
        if char.get("Name"):
            characters[char.get("ID")] = char["Name"]
            entries.append(game_entry(char["Name"], "Characters.json", [], {"npc"}))
    for behaviour in read_json(game_dir / "NPCBehavior.json"):
        name = characters.get(behaviour.get("ID"))
        if name:
            for key in path_keys(behaviour, []):
                internal[key].add(name)
    recipes = read_json(game_dir / "Recipes.json")
    for recipe in (recipes.get("Recipes", []) if isinstance(recipes, dict) else recipes):
        if recipe.get("Name"):
            entries.append(game_entry(recipe["Name"], "Recipes.json", [], set()))
    return entries, internal, Counter(e["source"] for e in entries)


def guard_outside_git(path):
    target = Path(path).resolve()
    for folder in (target, *target.parents):
        if (folder / ".git").exists():
            raise SystemExit(f"refusing {target}: {folder} contains .git (wiki text must stay outside git repos)")


def load_wiki(cache):
    index_path = cache / "index.json"
    if not index_path.exists():
        raise SystemExit(f"no wiki cache at {cache}: run 'fetch' first")
    meta = json.loads(index_path.read_text(encoding="utf-8"))
    return [json.loads((cache / m["file"]).read_text(encoding="utf-8")) for m in meta["pages"].values()]


def build_index(pages, entries, game_entries):
    by_norm = {norm(p["title"]): entries[p["title"]] for p in pages if not p["redirect_target"]}
    index = {}

    def add(keys, entry):
        for key in keys:
            bucket = index.setdefault(key, [])
            if not any(item is entry for item in bucket):
                bucket.append(entry)

    for page in pages:
        target = entries[page["title"]]
        if page["redirect_target"]:
            target = by_norm.get(norm(page["redirect_target"])) or target
        add(title_keys(page["title"], page["ns"]), target)
    for entry in game_entries:
        add(title_keys(entry["title"]), entry)
    return index


def lookup(index, raw):
    hits = []
    if skipped(raw):
        return hits
    for key in term_keys(raw):
        for entry in index.get(key, ()):
            if not any(item is entry for item in hits):
                hits.append(entry)
    return hits


def evidence(entry):
    info = entry["infobox"] or {"template": "", "fields": {}}
    return " ".join([entry["first"], *entry["categories"], info["template"], *info["fields"].values()])


def finding(rel, line, heading, raw, entry):
    return {"doc": rel, "line": line, "heading": heading, "term": raw, "wiki_title": entry["title"],
            "wiki_source": entry["source"], "wiki_categories": entry["categories"],
            "wiki_first": trim(entry["first"], 150)}


def kind_conflicts(terms, index):
    out, seen = [], set()
    for rel, line, heading, section, raw, src in terms:
        if not section:
            continue
        hits = lookup(index, raw)
        for entry in hits:
            if not entry["kinds"] or section in entry["kinds"] or (rel, line, raw, entry["title"]) in seen:
                continue
            seen.add((rel, line, raw, entry["title"]))
            out.append(finding(rel, line, heading, raw, entry) | {"strength": strength(raw, src, hits),
                                                                  "section_kind": section, "wiki_kinds": sorted(entry["kinds"])})
    return sorted(out, key=lambda f: f["strength"] != "strong")


def operated_by_npc(entry):
    info = entry["infobox"] or {"fields": {}}
    return bool(NPC_WORDS.search(evidence(entry))) or "people" in info["fields"]


def role_conflict(check, entry):
    kind, value = check
    if kind == "kind":
        return bool(entry["kinds"]) and value not in entry["kinds"]
    text = evidence(entry)
    if value == "player":
        return operated_by_npc(entry) and not PLAYER_WORDS.search(text)
    return PLAYER_WORDS.search(text) is not None and not operated_by_npc(entry)


def role_words(rows, index):
    out, seen = [], set()
    for rel, line, heading, _section, text, found in rows:
        labels = [(label, check) for label, rx, check in ROLE_RULES if rx.search(text)]
        if not labels:
            continue
        for raw, src in found:
            hits = lookup(index, raw)
            for entry in hits:
                for label, check in labels:
                    if (rel, line, raw, entry["title"], label) in seen or not role_conflict(check, entry):
                        continue
                    seen.add((rel, line, raw, entry["title"], label))
                    reason = (f"doc says {label}, wiki evidence says the opposite" if check[0] == "polarity"
                              else f"doc role {check[1]}, wiki kinds {', '.join(sorted(entry['kinds']))}")
                    out.append(finding(rel, line, heading, raw, entry) | {"strength": strength(raw, src, hits),
                                                                          "roles": label, "reason": reason})
    return sorted(out, key=lambda f: f["strength"] != "strong")


def near_matches(terms, index):
    keys_by_len = defaultdict(list)
    for key in index:
        keys_by_len[len(key)].append(key)
    cache, out, seen = {}, [], set()
    for rel, line, _heading, _section, raw, src in terms:
        if (rel, raw) in seen or (src == "word" and not raw[0].isupper()) or skipped(raw):
            continue
        seen.add((rel, raw))
        for key in base_keys(raw):
            if len(key) < 4 or any(k in index for k in term_keys(raw)):
                continue
            if key not in cache:
                pool = [k for n in range(int(len(key) * 0.7), int(len(key) * 1.4) + 2) for k in keys_by_len.get(n, ())]
                cache[key] = difflib.get_close_matches(key, pool, n=1, cutoff=0.85)
            if cache[key]:
                out.append({"doc": rel, "line": line, "term": raw, "near_title": index[cache[key][0]][0]["title"]})
                break
    return out


def case_variants(terms, index):
    spaceless = defaultdict(list)
    for key, entries in index.items():
        spaceless[key.replace(" ", "")].extend(entries)
    out, seen = [], set()
    for rel, line, _heading, _section, raw, src in terms:
        nospace = norm(raw).replace(" ", "")
        if src != "code" or not is_camel(raw) or len(nospace) < 4 or skipped(raw):
            continue
        for entry in spaceless.get(nospace, ()):
            if entry["title"] == raw or (rel, raw, entry["title"]) in seen:
                continue
            seen.add((rel, raw, entry["title"]))
            out.append({"doc": rel, "line": line, "term": raw, "wiki_title": entry["title"], "wiki_source": entry["source"]})
    return out


def class_files(root):
    if not root or not root.exists():
        return {}
    return {path.stem: path for path in root.rglob("*.cs")}


def class_literals(files, name):
    path = files.get(name)
    if not path:
        return set()
    return set(re.findall(r'"([^"\\\n]{3,60})"', path.read_text(encoding="utf-8", errors="replace")))


def code_names(terms, index, internal, wiki_entries, doc_seg, files):
    mismatches, unmapped, seen = [], [], set()
    known = {seg(key) for key in index}
    linked = set().union(*(entry["links"] for entry in wiki_entries))
    for rel, line, _heading, _section, raw, src in terms:
        if src != "code" or not raw[0].isupper() or not (is_camel(raw) or len(raw) >= 6) or skipped(raw):
            continue
        if (rel, raw) in seen or any(key in index for key in term_keys(raw)):
            continue
        seen.add((rel, raw))
        code = seg(raw)
        found_by = {name: "game asset" for name in internal.get(code, ())}
        if is_camel(raw):
            for entry in wiki_entries:
                if code in entry["nospace"] and re.search(rf"\b{re.escape(raw)}\b", entry["wikitext"]):
                    found_by.setdefault(entry["title"], "wiki text")
        literal_names = {strip_disambig(lit) for lit in class_literals(files, raw)}
        good = [name for name in literal_names if (seg(name) in known or seg(name) in linked) and seg(name) != code]
        if len(good) == 1:
            found_by.setdefault(good[0], "decompile")
        candidates = [{"name": name, "via": via} for name, via in sorted(found_by.items())
                      if len(seg(name)) >= 4 and seg(name) != code]
        if not candidates:
            unmapped.append({"doc": rel, "line": line, "term": raw})
        elif not any(seg(c["name"]) in doc_seg[rel] for c in candidates):
            vias = {c["via"] for c in candidates}
            level = "strong" if "game asset" in vias else "hint" if "decompile" in vias else "weak"
            mismatches.append({"doc": rel, "line": line, "term": raw, "strength": level, "candidates": candidates[:5]})
    return sorted(mismatches, key=lambda f: ["strong", "hint", "weak"].index(f["strength"])), unmapped


def unknown_phrases(terms, index):
    out, seen = [], set()
    for rel, line, _heading, _section, raw, src in terms:
        if src != "phrase" or (rel, norm(raw)) in seen or any(key in index for key in term_keys(raw)):
            continue
        seen.add((rel, norm(raw)))
        out.append({"doc": rel, "line": line, "phrase": raw})
    return out


def doc_files(only):
    if only:
        return [only]
    docs = REPO / "docs"
    files = [p for p in docs.rglob("*.md")
             if "code-notes" not in p.relative_to(docs).parts and not p.name.lower().startswith(("design", "release"))]
    return sorted(files + [REPO / "README.md", REPO / "Starter" / "README.md"])


def rel_path(path):
    try:
        return path.relative_to(REPO).as_posix()
    except ValueError:
        return str(path)


def resolve_page(text):
    path = Path(text)
    if not path.is_absolute() and not path.exists():
        path = REPO / path
    if not path.is_file():
        raise SystemExit(f"no such doc: {text}")
    return path


def cell(value):
    return str(value).replace("|", "\\|").replace("\n", " ")


def trail(items):
    return trim("; ".join(items), 120)


def render_md(report):
    sections = [
        ("1. Kind conflicts", ["Doc", "Heading", "Term", "Strength", "Wiki title", "Wiki categories", "Wiki kinds", "Wiki first sentence"],
         [(f"{f['doc']}:{f['line']}", trim(f["heading"], 70), f["term"], f["strength"], f["wiki_title"], trail(f["wiki_categories"]),
           ", ".join(f["wiki_kinds"]), f["wiki_first"]) for f in report["kind_conflicts"]],
         "Doc section kind differs from every kind the wiki or game data gives the matching term. "
         "Strong: capitalised, code, or the term opens the wiki or game description. Weak: lowercase prose word. Strong rows come first.", None),
        ("2. Role words", ["Doc", "Heading", "Term", "Strength", "Role", "Wiki title", "Wiki categories", "Reason"],
         [(f"{f['doc']}:{f['line']}", trim(f["heading"], 70), f["term"], f["strength"], f["roles"], f["wiki_title"],
           trail(f["wiki_categories"]), f["reason"]) for f in report["role_words"]],
         "Role words on the same line as a matching term, where the wiki evidence points the other way. Review by hand. Strong rows come first.", None),
        ("3a. Near matches to a wiki title", ["Doc", "Term", "Closest wiki title"],
         [(f"{f['doc']}:{f['line']}", f["term"], f["near_title"]) for f in report["near_matches"]],
         "Doc term is not a wiki title but is at least 0.85 similar to one.", 500),
        ("3b. Code names that differ only in case or spacing", ["Doc", "Code term", "Wiki title"],
         [(f"{f['doc']}:{f['line']}", f["term"], f["wiki_title"]) for f in report["case_variants"]],
         "Backticked identifier matches a wiki title with different case or spacing.", 500),
        ("3c. Code names with a player-facing name the docs never use", ["Doc", "Code name", "Strength", "Player-facing candidates (evidence)"],
         [(f"{f['doc']}:{f['line']}", f["term"], f["strength"],
           ", ".join(f"{c['name']} ({c['via']})" for c in f["candidates"])) for f in report["code_name_mismatches"]],
         "Code name maps to a player name (game asset path, the class file's one name literal, or wiki text) that the doc file never mentions. "
         "Strong: game asset path. Hint: class file name literal (often a UI string, check by hand). Weak: wiki text only.", None),
        ("3d. Code names with no game or wiki counterpart", ["Doc", "Code name"],
         [(f"{f['doc']}:{f['line']}", f["term"]) for f in report["code_names_unmapped"]],
         "Review list. Many are API members or types with no player-facing name.", 500),
        ("4. Unknown to the wiki", ["Doc", "Phrase"],
         [(f"{f['doc']}:{f['line']}", f["phrase"]) for f in report["unknown_phrases"]],
         "Capitalised multi-word doc phrases that match no wiki title, redirect or game name. Lower priority.", 300),
    ]
    lines = ["# Stalburg wiki check", "", f"Generated {report['generated']}. Wiki cache: {report['coverage']['cache']}.", ""]
    for title, headers, rows, note, limit in sections:
        shown = rows if limit is None else rows[:limit]
        lines += [f"## {title} ({len(rows)})", "", note, ""]
        if not rows:
            lines += ["None.", ""]
            continue
        lines += ["| " + " | ".join(headers) + " |", "|" + "---|" * len(headers)]
        lines += ["| " + " | ".join(cell(c) for c in row) + " |" for row in shown]
        if len(shown) < len(rows):
            lines += ["", f"Showing {len(shown)} of {len(rows)}; the JSON report has all of them."]
        lines.append("")
    lines += ["## 5. Coverage", ""]
    lines += [f"- {key}: {value}" for key, value in report["coverage"].items()]
    return "\n".join(lines) + "\n"


def cmd_check(args):
    cache = Path(args.cache)
    out = Path(args.out) if args.out else cache
    guard_outside_git(cache)
    guard_outside_git(out)
    pages = load_wiki(cache)
    game_entries, internal, game_counts = load_game(Path(args.game))
    entries = {p["title"]: wiki_entry(p) for p in pages}
    category_names = {norm(c) for p in pages for c in p["categories"]}
    for entry in entries.values():
        if norm(entry["title"].rsplit("/", 1)[-1]) in category_names:
            entry["kinds"] = set()
    index = build_index(pages, entries, game_entries)
    files = doc_files(resolve_page(args.page) if args.page else None)
    rows = []
    for path in files:
        rows += scan_doc(rel_path(path), path.read_text(encoding="utf-8", errors="replace"))
    terms = list(terms_of(rows))
    doc_seg = defaultdict(str)
    for rel, _line, _heading, _section, text, _found in rows:
        doc_seg[rel] += seg(text)
    wiki_entries = [entries[p["title"]] for p in pages if not p["redirect_target"]]
    classes = class_files(Path(args.decompile)) if args.decompile else {}
    mismatches, unmapped = code_names(terms, index, internal, wiki_entries, doc_seg, classes)
    report = {
        "generated": datetime.now().isoformat(timespec="seconds"),
        "kind_conflicts": kind_conflicts(terms, index),
        "role_words": role_words(rows, index),
        "near_matches": near_matches(terms, index),
        "case_variants": case_variants(terms, index),
        "code_name_mismatches": mismatches,
        "code_names_unmapped": unmapped,
        "unknown_phrases": unknown_phrases(terms, index),
        "coverage": {
            "cache": str(cache),
            "doc_files_scanned": len(files),
            "doc_lines": len(rows),
            "term_occurrences": len(terms),
            "distinct_terms": len({raw.casefold() for *_rest, raw, _src in terms}),
            "wiki_pages_loaded": len(pages),
            "wiki_redirects": sum(1 for p in pages if p["redirect_target"]),
            "wiki_namespaces": sorted({p["ns"] for p in pages}),
            "game_items_loaded": game_counts["Items.json"],
            "game_characters_loaded": game_counts["Characters.json"],
            "game_recipes_loaded": game_counts["Recipes.json"],
            "decompile_class_files": len(classes),
            "index_keys": len(index),
        },
    }
    out.mkdir(parents=True, exist_ok=True)
    (out / "wikicheck-report.json").write_text(json.dumps(report, ensure_ascii=False, indent=1), encoding="utf-8")
    (out / "wikicheck-report.md").write_text(render_md(report), encoding="utf-8")
    for key in ("kind_conflicts", "role_words", "near_matches", "case_variants", "code_name_mismatches",
                "code_names_unmapped", "unknown_phrases"):
        print(f"{key}: {len(report[key])}")
    print(f"doc files: {len(files)}, doc lines: {len(rows)}, terms: {len(terms)}, wiki pages: {len(pages)}, game items: {game_counts['Items.json']}")
    print(f"report: {out / 'wikicheck-report.md'}")
