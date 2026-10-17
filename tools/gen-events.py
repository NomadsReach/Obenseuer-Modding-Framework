"""Write docs/events.md from every public static event in src/.

Run from anywhere: python tools/gen-events.py
Check only, exits 1 when docs/events.md is stale: python tools/gen-events.py --check
"""
import os
import re
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
SRC = os.path.join(ROOT, "src")
OUT = os.path.join(ROOT, "docs", "events.md")

EVENT = re.compile(r"public (static )?event\s+(.+?)\s+(\w+)\s*(?:;|\{|$)")
CLASS = re.compile(r"\b(?:class|struct)\s+(\w+)")
SUMMARY = re.compile(r"<summary>(.*?)</summary>", re.S)


def clean(text):
    text = re.sub(r"</?[a-zA-Z][^>]*>", "", text)
    text = text.replace("\u2014", "-").replace("\u2013", "-")
    return " ".join(text.split())


def summary_above(lines, index):
    doc = []
    i = index - 1
    while i >= 0 and lines[i].strip().startswith("///"):
        doc.insert(0, lines[i].strip()[3:])
        i -= 1
    match = SUMMARY.search(" ".join(doc))
    return clean(match.group(1)) if match else ""


def page_map():
    pages = {
        "BankLedger": ("Economy", "api/economy.md"),
        "Survival": ("Player", "api/player.md"),
    }
    for word in ("CameraView", "ComputerApps", "Consumables", "PlayerLook"):
        pages[word] = ("Framework core classes", "api-core.md")
    links = re.compile(r"^- \[([^\]]+)\]\(([^)]+)\):(.*)$")
    with open(os.path.join(ROOT, "docs", "API.md"), encoding="utf-8") as f:
        for line in f:
            match = links.match(line.strip())
            if match:
                for word in re.findall(r"`(\w+)", match.group(3)):
                    pages.setdefault(word, (match.group(1), match.group(2)))
    return pages


def collect():
    rows = []
    for dirpath, _, files in os.walk(SRC):
        for name in sorted(files):
            if not name.endswith(".cs"):
                continue
            path = os.path.join(dirpath, name)
            with open(path, encoding="utf-8") as f:
                lines = f.read().splitlines()
            owner = None
            for i, line in enumerate(lines):
                cls = CLASS.search(line)
                if cls and not line.strip().startswith("//"):
                    owner = cls.group(1)
                match = EVENT.search(line)
                if match and owner:
                    rows.append((owner, match.group(3), " ".join(match.group(2).split()), summary_above(lines, i), bool(match.group(1))))
    return rows


def main():
    rows = collect()
    pages = page_map()

    def page(owner):
        if owner not in pages:
            return ""
        return "[%s](%s)" % pages[owner]

    by_class = {}
    instance = []
    for owner, name, kind, summary, is_static in rows:
        if is_static:
            by_class.setdefault(owner, []).append((name, kind, summary))
        else:
            instance.append((owner, name, kind, summary))
    out = [
        "---",
        "title: Events",
        "nav_order: 7",
        "---",
        "",
        "# Events",
        "",
        "## What it is",
        "",
        "An event is a message the game sends when something happens: a day passes, the player's money changes, a door opens. Your mod subscribes to an event with a small piece of code (a handler), and the framework runs it each time that thing happens. You do not have to watch the game and check for changes yourself. This page lists every event the framework offers, grouped by the class that owns it.",
        "",
        "## When to use it",
        "",
        "- Show a message when a new day starts (`Events.DayPassed`).",
        "- Write to your log, or pay a bonus, when the player's money changes (`Cash.MoneyChanged`).",
        "- Run set-up work once a save has loaded (`Events.WhenReady`, which is a method, not an event).",
        "- React when the player is fined, arrested, hurt or asleep (`Law.Fined`, `Law.Arrested`, `Survival.Damaged`, `PlayerState.SleepStarted`).",
        "",
        "## Quick example",
        "",
        "This mod shows a notification at the start of each day, and writes every money change to its log. See [Using events](guides/getting-started/using-events.md) for a longer walk-through.",
        "",
        "```csharp",
        "using ObenseuerCore;",
        "",
        "[ModInfo(\"example.eventwatcher\", \"Event Watcher\", \"0.1.0\")]",
        "[ModDependency(\"obenseuer.moddingframework\", \"0.11.0\")]",
        "public sealed class EventWatcher : FrameworkMod",
        "{",
        "    protected override void OnLoad()",
        "    {",
        "        Events.DayPassed += day => Hud.Notify(\"Day \" + day + \" has started.\");",
        "        Cash.MoneyChanged += OnMoneyChanged;",
        "    }",
        "",
        "    private void OnMoneyChanged(MoneyChangedArgs args)",
        "    {",
        "        Log.Info(args.Currency + \": \" + args.Before + \" to \" + args.After);",
        "    }",
        "}",
        "```",
        "",
        "The `+=` adds your handler. Most events pass one argument that describes what happened (here `MoneyChangedArgs`). Some pass a plain value, such as the day number, and some pass nothing.",
        "",
        "## Common tasks",
        "",
        "- Stop listening with `-=`, using a named method (an inline lambda has no name to remove): `Cash.MoneyChanged -= OnMoneyChanged;`.",
        "- Run something once a save is loaded, instead of subscribing to `Events.Loaded`: `Events.WhenReady(() => Log.Info(\"save is ready\"));`. `Events.Loaded` also runs on every door change.",
        "- Find an event for a feature: look for the class of the feature in the list below (`Doors`, `Trains`, `Quests` and so on), then read that class's page in the [API reference](API.md) for what the arguments hold.",
        "",
        "## Reference",
        "",
        "These are all the events the framework offers, grouped by the part of the game they belong to. The optional WebUi module's events are not listed here. The Handler type column says what your handler receives: `Action<ReplantedArgs>` is a method that takes one `ReplantedArgs` value, `Action<int>` takes a plain number, and `Action` takes nothing. The last column links to the page that explains the arguments. Most events belong to the class itself (`Events.DayPassed`). A few belong to one object, such as one `ModSetting`, and are listed last.",
        "",
    ]
    for owner in sorted(by_class):
        out.append("### " + owner)
        out.append("")
        out.append("| Event | Handler type | Page with details |")
        out.append("| --- | --- | --- |")
        for name, kind, summary in sorted(by_class[owner]):
            kind = kind.replace("|", "\\|")
            summary = summary.replace("|", "\\|")
            out.append("| `%s.%s` | `%s` | %s |" % (owner, name, kind, summary or page(owner)))
        out.append("{: .events-table}")
        out.append("")
    if instance:
        out.append("### Instance events")
        out.append("")
        out.append("These belong to one object, not to the class itself. For example, each `ModSetting` has its own change event.")
        out.append("")
        out.append("| Event | Handler type | Page with details |")
        out.append("| --- | --- | --- |")
        for owner, name, kind, summary in sorted(instance):
            kind = kind.replace("|", "\\|")
            summary = summary.replace("|", "\\|")
            out.append("| `%s.%s` | `%s` | %s |" % (owner, name, kind, summary or page(owner)))
        out.append("{: .events-table}")
        out.append("")
    out += [
        "## Details",
        "",
        "- This page is generated by `tools/gen-events.py` from the source. Do not edit it by hand.",
        "- Subscribe with `+=` in `OnLoad` of a `FrameworkMod`, or in `Awake` of a plain BepInEx plugin.",
        "- Handlers run in the order they were added. The first throw from each handler is logged, and the other handlers still run.",
        "- The game raises events from its normal game loop. Use the framework from your handler as usual, and do not call it from a background thread you started. See [main thread](glossary.md#main-thread).",
        "",
    ]
    text = "\n".join(out)
    if "--check" in sys.argv[1:]:
        with open(OUT, encoding="utf-8") as f:
            current = f.read()
        if current != text:
            print("%s is stale, run python tools/gen-events.py" % OUT)
            return 1
        print("%s is up to date (%d events)" % (OUT, len(rows)))
        return 0
    with open(OUT, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)
    print("%d events in %d classes written to %s" % (len(rows), len(by_class), OUT))
    return 0


if __name__ == "__main__":
    sys.exit(main())
