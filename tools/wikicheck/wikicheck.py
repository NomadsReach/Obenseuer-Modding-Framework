#!/usr/bin/env python
"""Fetch the Stalburg wiki and cross-check the framework docs against it."""

import argparse
import json
import os
import sys
import tempfile
import time
import urllib.error
import urllib.parse
import urllib.request
from collections import Counter
from datetime import datetime
from pathlib import Path

import wikicheck_check as chk

API = "https://stalburg.net/api.php"
UA = "ObenseuerFrameworkDocCheck/1.0 (doc accuracy check)"
CONTENT_NAMESPACES = (0, 3000)
GAME_DIR = Path(r"C:\Program Files (x86)\Steam\steamapps\common\Obenseuer\Obenseuer_Data\StreamingAssets")
DECOMPILE_DIR = Path(r"C:\Users\Ricky\source\ObenseuerReverseEng\Game\generated\25969064\Assembly-CSharp")
DEFAULT_CACHE = Path(os.environ.get("LOCALAPPDATA", Path.home() / "AppData" / "Local")) / "ObenseuerWikiCache"
LAST_CALL = [0.0]


def api_get(params):
    query = urllib.parse.urlencode(dict(params, format="json", formatversion="2", maxlag="5"))
    for attempt in range(8):
        gap = 1.0 - (time.monotonic() - LAST_CALL[0])
        if gap > 0:
            time.sleep(gap)
        LAST_CALL[0] = time.monotonic()
        request = urllib.request.Request(f"{API}?{query}", headers={"User-Agent": UA})
        wait = 5 * 2 ** attempt
        try:
            with urllib.request.urlopen(request, timeout=60) as response:
                data = json.load(response)
        except urllib.error.HTTPError as error:
            if error.code not in (429, 503):
                raise
            retry_after = error.headers.get("Retry-After", "")
            wait = max(wait, float(retry_after) if retry_after.isdigit() else 0)
        except (urllib.error.URLError, TimeoutError):
            pass
        else:
            err = data.get("error")
            if not err:
                return data
            if err.get("code") not in ("maxlag", "ratelimited"):
                raise RuntimeError(f"API error {err.get('code')}: {err.get('info')}")
        print(f"  API busy or lagging, retry {attempt + 1} in {wait:.0f}s", file=sys.stderr, flush=True)
        time.sleep(wait)
    raise RuntimeError("API still failing after 8 attempts")


def list_pages(ns):
    pages, cont = {}, {}
    while True:
        data = api_get(dict(action="query", generator="allpages", gapnamespace=ns, gaplimit="500", prop="info", **cont))
        for page in data.get("query", {}).get("pages", []):
            pages[page["title"]] = {"ns": ns, "pageid": page["pageid"], "revid": page["lastrevid"]}
        if "continue" not in data:
            return pages
        cont = data["continue"]


def fetch_contents(titles):
    got, cont = {}, {}
    while True:
        data = api_get(dict(action="query", titles="|".join(titles), prop="revisions|categories",
                            rvprop="content|ids|timestamp", rvslots="main", cllimit="max", **cont))
        for page in data.get("query", {}).get("pages", []):
            if page.get("missing"):
                continue
            rec = got.get(page["title"])
            if rec is None and page.get("revisions"):
                rev = page["revisions"][0]
                rec = got[page["title"]] = {"pageid": page["pageid"], "revid": rev["revid"], "timestamp": rev["timestamp"],
                                            "wikitext": rev["slots"]["main"]["content"], "categories": []}
            if rec is not None:
                rec["categories"] += [c["title"].split(":", 1)[1] for c in page.get("categories", []) if ":" in c["title"]]
        if "continue" not in data:
            break
        cont = data["continue"]
    for rec in got.values():
        rec["categories"] = sorted(set(rec["categories"]))
    return got


def save_index(cache, index):
    tmp = cache / "index.json.tmp"
    tmp.write_text(json.dumps(index, ensure_ascii=False, indent=1), encoding="utf-8")
    os.replace(tmp, cache / "index.json")


def cmd_fetch(args):
    cache = Path(args.cache)
    chk.guard_outside_git(cache)
    (cache / "pages").mkdir(parents=True, exist_ok=True)
    index_path = cache / "index.json"
    index = json.loads(index_path.read_text(encoding="utf-8")) if index_path.exists() else {"pages": {}}
    started = time.monotonic()
    listed = {}
    for ns in CONTENT_NAMESPACES:
        listed.update(list_pages(ns))
    removed = [t for t in index["pages"] if t not in listed]
    for title in removed:
        (cache / index["pages"].pop(title)["file"]).unlink(missing_ok=True)
    todo = [t for t, meta in listed.items()
            if args.force or index["pages"].get(t, {}).get("revid") != meta["revid"]
            or not (cache / index["pages"][t]["file"]).exists()]
    print(f"listed {len(listed)} pages in namespaces {list(CONTENT_NAMESPACES)}, fetching {len(todo)}", flush=True)
    for start in range(0, len(todo), 50):
        for title, rec in fetch_contents(todo[start:start + 50]).items():
            rec.update(title=title, ns=listed[title]["ns"], redirect_target=chk.redirect_target(rec["wikitext"]))
            file = f"pages/{rec['pageid']}.json"
            (cache / file).write_text(json.dumps(rec, ensure_ascii=False), encoding="utf-8")
            index["pages"][title] = {"ns": rec["ns"], "pageid": rec["pageid"], "revid": rec["revid"], "file": file,
                                     "redirect": bool(rec["redirect_target"])}
        index.update(fetched_at=datetime.now().isoformat(timespec="seconds"), api=API, user_agent=UA,
                     namespaces=list(CONTENT_NAMESPACES))
        save_index(cache, index)
        print(f"  {min(start + 50, len(todo))}/{len(todo)}", flush=True)
    save_index(cache, index)
    categories = set()
    for meta in index["pages"].values():
        categories.update(json.loads((cache / meta["file"]).read_text(encoding="utf-8"))["categories"])
    per_ns = Counter(meta["ns"] for meta in index["pages"].values())
    redirects = sum(1 for meta in index["pages"].values() if meta["redirect"])
    print(f"pages: {len(index['pages'])} (per namespace: {dict(sorted(per_ns.items()))})")
    print(f"redirects: {redirects}")
    print(f"categories: {len(categories)}")
    print(f"fetched now: {len(todo)}, unchanged: {len(listed) - len(todo)}, removed: {len(removed)}")
    print(f"duration: {time.monotonic() - started:.1f} s")
    print(f"cache: {cache}")


def selftest(_args=None):
    assert chk.norm("OsRat") == "osrat"
    assert chk.title_keys("Obenseuer/Notes/Osrat_device") >= {"osrat device", "notes osrat device"}
    assert "osrat" in chk.title_keys("Osrat (item)")
    assert chk.title_keys("Puzzle:Blackrock NPP Crane", ns=3000) == {"blackrock npp crane"}
    assert chk.camel_split("ChinaHand") == "China Hand"
    assert chk.camel_split("MarketShop") == "Market Shop"
    assert chk.camel_split("NPCBehavior") == "NPC Behavior"
    assert "china hand" in chk.term_keys("ChinaHand")
    assert chk.heading_kind("Animals and osrats") == "animal"
    assert chk.heading_kind("Card games") == "minigame"
    assert chk.heading_kind("Market stalls") == "shop"
    assert chk.heading_kind("Osrat signal detector") == "tool/device"
    assert chk.heading_kind("Overview") is None
    assert chk.first_sentence("{{Infobox Item|name=x}}\n'''Osrat''' is a [[hand-held]] [[signal detector|detector]].<ref>x</ref> It beeps.") == "Osrat is a hand-held detector."
    info = chk.parse_infobox("{{Infobox Animal\n| type = Animal\n}}")
    assert info["template"] == "Infobox Animal" and info["fields"]["type"] == "Animal"
    assert chk.redirect_target("#REDIRECT [[Osrat#Notes]]") == "Osrat"
    assert chk.code_terms("Osrats.Strength") == ["Osrats", "Strength"]
    rows = chk.scan_doc("docs/x.md", "## Animals\n\nThe `ChinaHand` game and Open Sewer Coin.\n")
    found = {(raw, src) for *_rest, raw, src in chk.terms_of(rows)}
    assert ("ChinaHand", "code") in found and ("Open Sewer Coin", "phrase") in found
    assert rows[2][3] == "animal"
    osrat = {"title": "Osrat", "source": "wiki", "categories": ["Tools"], "kinds": {"tool/device"}, "first": "",
             "nospace": "", "infobox": None, "redirect_target": None}
    index = {"osrat": [osrat]}
    assert len(chk.kind_conflicts([("docs/x.md", 3, "Animals", "animal", "Osrat", "word")], index)) == 1
    assert chk.kind_conflicts([("docs/x.md", 3, "Devices", "tool/device", "Osrat", "word")], index) == []
    with tempfile.TemporaryDirectory() as tmp:
        repo = Path(tmp) / "repo"
        (repo / ".git").mkdir(parents=True)
        try:
            chk.guard_outside_git(repo / "cache")
        except SystemExit:
            pass
        else:
            raise AssertionError("cache inside a .git folder was accepted")
        chk.guard_outside_git(Path(tmp) / "plain" / "cache")
    print("selftest ok")


def main(argv=None):
    parser = argparse.ArgumentParser(description="Stalburg wiki fetch and framework doc check")
    sub = parser.add_subparsers(dest="cmd", required=True)
    fetch = sub.add_parser("fetch", help="download or refresh the wiki cache")
    fetch.add_argument("--cache", default=str(DEFAULT_CACHE))
    fetch.add_argument("--force", action="store_true", help="refetch every page")
    check = sub.add_parser("check", help="cross-check the docs against the cache")
    check.add_argument("--cache", default=str(DEFAULT_CACHE))
    check.add_argument("--out", help="report folder (default: the cache folder)")
    check.add_argument("--page", help="check one doc file")
    check.add_argument("--game", default=str(GAME_DIR), help="StreamingAssets folder")
    check.add_argument("--decompile", default=str(DECOMPILE_DIR), help="decompiled Assembly-CSharp folder ('' to skip)")
    sub.add_parser("selftest", help="offline checks of the pure functions")
    args = parser.parse_args(argv)
    {"fetch": cmd_fetch, "check": chk.cmd_check, "selftest": selftest}[args.cmd](args)


if __name__ == "__main__":
    main()
