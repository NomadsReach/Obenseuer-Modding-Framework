# wikicheck

Downloads the Stalburg wiki (https://stalburg.net, the Obenseuer fan wiki) and checks the framework docs against it, so mislabeled game terms show up: an animal-looking device filed under Animals, NPC-run stalls called player-run, or internal class names used where the game shows a player name.

Files: `wikicheck.py` (CLI, fetch, selftest), `wikicheck_check.py` (offline analysis and report). Standard library only, Python 3.

## Run

    python tools\wikicheck\wikicheck.py selftest
    python tools\wikicheck\wikicheck.py fetch
    python tools\wikicheck\wikicheck.py check
    python tools\wikicheck\wikicheck.py check --page docs\api\npcs.md --out <folder>

- `fetch` is incremental: it lists every content page with its revision id and downloads only pages whose revision changed (`--force` refetches all). It is safe to stop and rerun.
- `check` needs a cache from `fetch`. It reads the game data from StreamingAssets (`--game`) and, when present, the decompiled Assembly-CSharp (`--decompile`, `''` to skip).
- `--page` checks one doc file (absolute, relative to the working folder, or relative to the repo root).

## Where things go

- Wiki cache: `%LOCALAPPDATA%\ObenseuerWikiCache` (`--cache DIR` to override). One JSON per page under `pages\`, plus `index.json`.
- Reports: `wikicheck-report.md` and `wikicheck-report.json`, written to `--out` (default: the cache folder).
- Both commands refuse a cache or report folder that is inside any folder containing `.git`, so wiki text never lands in a repo.

## Fetch

- API: `https://stalburg.net/api.php` (MediaWiki 1.43, no `/w/` prefix).
- Namespaces: 0 (main, 585 pages) and 3000 (Puzzle, 18 pages). Template, Module, Category, Help, MediaWiki and Project pages are not game content and are not fetched. `Obenseuer/...` titles are plain namespace 0 titles, not a namespace.
- Listing: `generator=allpages` with `prop=info` gives every title, page id and latest revision id in one pass. Redirects are included and flagged. This replaces `apfilterredir` and `recentchanges`, which cannot give a complete incremental diff.
- Content: 50 titles per request, `revisions|categories`, `rvslots=main`, continuation followed.
- Politeness: User-Agent `ObenseuerFrameworkDocCheck/1.0 (doc accuracy check)`, no e-mail, `maxlag=5`, at least 1 second between requests, retry with backoff (5 s doubling, up to 8 tries) on HTTP 429/503 and on `maxlag`/`ratelimited` errors.
- Deleted pages are removed from the cache on the next run.

## Check: what is compared

Wiki terms. Each page title and redirect title becomes keys: case-folded, punctuation and underscores to spaces, a leading `Obenseuer/` (repeated) removed, a trailing ` (disambiguation)` removed, and for subpages also the last segment (`Obenseuer/Notes/Osrat device` gives `osrat device`). Redirects point at their target page. Pages whose last title segment equals a category name (for example `Obenseuer/Items`) are list pages and carry no kind.

Kinds come from wiki categories and the infobox `type`, `category`, `class`, `kind` fields (any template with "infobox" in its name). Keywords (case-insensitive, first match wins for headings):

| Kind | Keywords |
|---|---|
| animal | animal, livestock |
| weapon | weapon, firearm, melee, wieldable, gun |
| minigame | card, casino, gambl, lotter, minigame, bet, poker |
| currency | currenc, money, coin, cash |
| shop | shop, stall, vendor, store |
| npc | npc, character, tenant, people, resident |
| quest | quest, mission, story |
| tool/device | tool, device, gadget, radio, phone, detector, appliance |
| location | location, place, world, district, town |
| item | item, food, drink, consumable, crafting, recipe, alcohol, meal |
| mechanic | mechanic, system, feature, setting |

Game names. Items.json `Title` (kind item plus category keywords; the description's first sentence is its "what it is" line). Characters.json `Name` (kind npc). Recipes.json `Name` (names only, no kind, so recipes never cause kind conflicts). Items and Characters are read as text with BOM handling; Characters.json is a comma-separated object stream and Recipes.json is UTF-16.

Doc terms. Every `.md` under `docs\` except `docs\code-notes\` and files starting with `design` or `release`, plus `README.md` and `Starter\README.md`. Per line:
- heading path (`#` to `######`) and the section kind (deepest heading with a keyword; a heading's own text counts);
- backticked identifiers (dotted parts split: `Osrats.Strength` gives two terms) and multi-word code spans;
- CamelCase identifiers inside fenced code blocks;
- capitalised multi-word phrases (leading stopwords dropped);
- lowercase or capitalised words of 3+ letters, minus a stopword list.

Matching uses the term, its camel-split form (`ChinaHand` gives `china hand`), and singular/plural variants.

Strength. A term is strong when it is capitalised, from code, or when a matching wiki page or game description starts with it ("OsRat is a device..."). A lowercase prose word that only matches incidentally is weak. Strong rows sort first.

Skipped as too generic to classify: the game and framework names (`Obenseuer`, `ObenseuerCore`, `Stalburg`, `INFRA`).

## Report sections

1. Kind conflicts: a doc term matches a wiki or game entry whose kinds do not include the doc section's kind. Shows file:line, heading, term, wiki title, categories, kinds, first sentence (150 characters).
2. Role words: a line with a role word (player-run, player's, NPC-run, NPC, animal, weapon, currency, real money) and a matching term whose wiki evidence points the other way. For player/NPC roles, "NPC evidence" is an NPC word in the first sentence or categories, or an infobox `people=` field; "player evidence" is a player word. Kind roles compare kinds.
3. Name mismatches:
   - 3a near matches: term is not a wiki key but has difflib ratio of at least 0.85 to one (only capitalised or code terms; words with a plural form in the index are skipped).
   - 3b case or spacing variants: a CamelCase code term equals a wiki title with different case or spacing (`BankAccount` and `Bank Account`).
   - 3c code names with a different player-facing name the doc file never mentions. Candidates come from game asset paths (Items.json prefab and sprite file names, NPCBehavior prefab names mapped to Characters), from the class file in the decompile (a `Class.cs` string literal that is a known name or a wiki link target, used only when the class has exactly one such literal), and from wiki text (CamelCase names as whole words). Strength: strong = game asset path; hint = decompile literal, which is often a UI string such as Backpack or Mail, so check by hand (ChinaHand gives Shittypants and Ristiseiska gives Crosseven this way); weak = wiki text only.
   - 3d code names with no candidate. Review list; many are API members and .NET types.
4. Unknown to the wiki: capitalised multi-word phrases that match no wiki title, redirect or game name (top 300).
5. Coverage: files, lines, terms, pages, game entries, decompile class files.

Markdown tables show every row for sections 1, 2 and 3c, at most 500 rows for 3a, 3b and 3d, and 300 for section 4; a table that is cut says so. The JSON report has every row.

## Known limits

- Kinds are keyword guesses from category names. A category with no keyword contributes nothing, so a page with no keyword category has no kind and cannot conflict.
- Generic item names (Mesh, Camera, Thread, Empty) match prose words, so expect some noise in sections 1 and 2. Strong rows are the ones to read first.
- The class file mapping depends on the decompile matching the game build (0.5.08 by default).
- The only network traffic is the wiki API. The game files and decompile are read locally.
