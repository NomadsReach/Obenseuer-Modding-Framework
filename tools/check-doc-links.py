"""Fail on dead relative links in the public docs pages and README.md.

Public pages are README.md and every .md file under docs/ that git does not export
(export-ignore in .gitattributes, applied to the file and each folder above it).
Run: python tools/check-doc-links.py
"""
import os
import re
import subprocess
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
DOCS = os.path.join(ROOT, "docs")
LINK = re.compile(r"\]\(([^)\s]+)\)")
HEADING = re.compile(r"^#{1,6}\s+(.*?)\s*$")


def shipped(rels):
    split = dict((rel, rel.split("/")) for rel in rels)
    prefixes = set("/".join(parts[:k]) for parts in split.values() for k in range(1, len(parts) + 1))
    out = subprocess.run(["git", "check-attr", "export-ignore", "--"] + sorted(prefixes), cwd=ROOT,
                         capture_output=True, text=True, check=True).stdout
    ignored = set(line.rsplit(": ", 2)[0] for line in out.splitlines() if line.endswith(": set"))
    return [rel for rel, parts in sorted(split.items())
            if not any("/".join(parts[:k]) in ignored for k in range(1, len(parts) + 1))]


def prose_lines(path):
    fenced = False
    with open(path, encoding="utf-8") as f:
        for number, line in enumerate(f, 1):
            if line.lstrip().startswith("```"):
                fenced = not fenced
                continue
            if not fenced:
                yield number, line.rstrip("\n")


def slug(text):
    text = re.sub(r"[`*_]", "", text).strip().lower()
    return re.sub(r"[^a-z0-9 _-]", "", text).replace(" ", "-")


def anchors(path):
    found = set()
    for _, line in prose_lines(path):
        m = HEADING.match(line)
        if m:
            found.add(slug(m.group(1)))
    return found


def main():
    rels = ["README.md"]
    for dirpath, _, files in os.walk(DOCS):
        for name in files:
            if name.endswith(".md"):
                rels.append(os.path.relpath(os.path.join(dirpath, name), ROOT).replace(os.sep, "/"))
    pages = [os.path.join(ROOT, rel) for rel in shipped(rels)]

    checked = 0
    dead = []
    anchor_cache = {}
    for page in sorted(pages):
        base = os.path.dirname(page)
        for number, line in prose_lines(page):
            for target in LINK.findall(line):
                if re.match(r"^(https?:|mailto:|\{%)", target):
                    continue
                checked += 1
                path, _, frag = target.partition("#")
                if path == "":
                    dest = page
                else:
                    dest = os.path.normpath(os.path.join(base, path))
                if not os.path.exists(dest):
                    dead.append("%s:%d: missing %s" % (os.path.relpath(page, ROOT), number, target))
                    continue
                if frag and dest.endswith(".md"):
                    if dest not in anchor_cache:
                        anchor_cache[dest] = anchors(dest)
                    if frag not in anchor_cache[dest]:
                        dead.append("%s:%d: no anchor #%s in %s" % (os.path.relpath(page, ROOT), number, frag, target))

    for item in dead:
        print(item)
    print("checked %d relative links in %d pages, %d dead" % (checked, len(pages), len(dead)))
    return 1 if dead else 0


if __name__ == "__main__":
    sys.exit(main())
