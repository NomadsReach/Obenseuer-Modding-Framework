import os
import re
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
SRC = os.path.join(ROOT, "src")
DOCS = os.path.join(ROOT, "docs")
BASELINE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "doc-coverage-baseline.txt")
GAPS = os.path.join(os.path.dirname(os.path.abspath(__file__)), "doc-coverage-gaps.txt")
BIND = re.compile(r"\w*[Cc]onfig\w*\s*\.\s*Bind\s*(?:<[^>]*>)?\s*\(\s*\"([^\"]*)\"\s*,\s*\"([^\"]*)\"", re.S)
LIMIT = re.compile(r"(?:(?:public|internal|private|protected)\s+)?(?:static\s+)?(?:const|readonly)\s+(?:int|uint|short|byte|float|double|long)\s+(\w+)\s*=")
LIMIT_PROP = re.compile(r"(?:public|internal|private|protected)\s+(?:static\s+)?(?:int|uint|short|byte|float|double|long)\s+(\w+)\s*\{\s*get;\s*(?:internal\s+|private\s+)?set;")
VALUE_CONST = re.compile(r"(?:(?:public|internal|private|protected)\s+)?const (?:int|float|double|long)\s+(\w+)\s*=\s*([0-9][0-9_]*(?:\.[0-9_]+)?[fFdDlL]?)\s*;")
DECL = re.compile(r"(?:int|uint|short|byte|float|double|long)\s+(\w+)\s*(?:=|;|\{|,|\))")
SPAN = re.compile(r"`([^`\n]+)`")
LEAD = re.compile(r"^([A-Z][A-Za-z0-9_]{3,})")
MARKERS = ("Max", "Min", "Limit", "Cap")
BARE = ("Min", "Max", "Cap", "Limit", "MaxValue", "MinValue")
CACHE = {}


def word_pattern(name):
    pattern = CACHE.get(("w", name))
    if pattern is None:
        pattern = re.compile(r"(?<![A-Za-z0-9_])" + re.escape(name) + r"(?![A-Za-z0-9_])")
        CACHE[("w", name)] = pattern
    return pattern


def value_pattern(text):
    pattern = CACHE.get(("v", text))
    if pattern is None:
        pattern = re.compile(r"(?<![0-9.])" + re.escape(text) + r"(?![0-9]|\.[0-9])")
        CACHE[("v", text)] = pattern
    return pattern


def documented(name, docs):
    return word_pattern(name).search(docs) is not None


def cs_files():
    found = []
    for dirpath, dirnames, files in os.walk(SRC):
        dirnames.sort()
        for name in sorted(files):
            if name.endswith(".cs"):
                found.append(os.path.join(dirpath, name))
    return found


def doc_files():
    found = []
    readme = os.path.join(ROOT, "README.md")
    if os.path.exists(readme):
        found.append(readme)
    for dirpath, dirnames, files in os.walk(DOCS):
        dirnames.sort()
        for name in sorted(files):
            if name.endswith(".md"):
                found.append(os.path.join(dirpath, name))
    return [path for path in found
            if not os.path.relpath(path, ROOT).replace(os.sep, "/").startswith(("docs/code-notes/", "docs/reviews/"))]


def docs_text():
    chunks = []
    for path in doc_files():
        with open(path, encoding="utf-8", errors="ignore") as f:
            chunks.append(f.read())
    return "\n".join(chunks)


def src_text():
    chunks = []
    for path in cs_files():
        with open(path, encoding="utf-8", errors="ignore") as f:
            chunks.append(f.read())
    return "\n".join(chunks)


def api_doc_files():
    found = []
    for dirpath, dirnames, files in os.walk(DOCS):
        dirnames.sort()
        for name in sorted(files):
            if not name.endswith(".md"):
                continue
            path = os.path.join(dirpath, name)
            rel = os.path.relpath(path, ROOT).replace(os.sep, "/")
            if rel.startswith("docs/api"):
                found.append((path, rel))
    return found


def value_forms(literal):
    value = literal.replace(",", "").replace("_", "")
    if value and value[-1] in "fFdDlL":
        value = value[:-1]
    forms = {value}
    if "." in value:
        trimmed = value.rstrip("0").rstrip(".")
        if trimmed:
            forms.add(trimmed)
    return forms


def value_lines(name, doc_lines):
    pattern = word_pattern(name)
    return [line for line in doc_lines if pattern.search(line)]


def number_text(text):
    text = text.replace(",", "").replace("_", "").replace("`", "")
    text = re.sub(r"\([^)]*\)", "", text).strip()
    if text and text[-1] in "fFdDlL":
        text = text[:-1]
    return text.strip()


def as_float(text):
    try:
        return float(number_text(text))
    except ValueError:
        return None


def table_values(name, doc_lines):
    marker = "`" + name + "`"
    found = []
    for line in doc_lines:
        if not line.startswith("|"):
            continue
        cells = [cell.strip() for cell in line.split("|")]
        if len(cells) < 3 or cells[1] != marker:
            continue
        text = number_text(cells[2])
        try:
            found.append((text, float(text)))
        except ValueError:
            continue
    return found


def value_shown(forms, doc_lines):
    for line in doc_lines:
        normalised = line.replace(",", "").replace("_", "")
        for form in forms:
            if value_pattern(form).search(normalised):
                return True
    return False


def stale_references(source):
    stale = {}
    seen = set()
    for path, rel in api_doc_files():
        with open(path, encoding="utf-8", errors="ignore") as f:
            text = f.read()
        for span in SPAN.findall(text):
            match = LEAD.match(span.strip())
            if not match:
                continue
            name = match.group(1)
            if name in seen:
                continue
            seen.add(name)
            if word_pattern(name).search(source) is None:
                stale[name] = rel
    return stale, len(seen)


def load_baseline():
    names = set()
    if not os.path.exists(BASELINE):
        return names
    with open(BASELINE, encoding="utf-8", errors="ignore") as f:
        for line in f:
            line = line.strip()
            if line:
                names.add(line)
    return names


def write_baseline(names):
    with open(BASELINE, "w", encoding="utf-8", newline="\n") as f:
        for name in names:
            f.write(name + "\n")


def load_gaps():
    entries = set()
    if not os.path.exists(GAPS):
        return entries
    with open(GAPS, encoding="utf-8", errors="ignore") as f:
        for line in f:
            line = line.strip()
            if line:
                entries.add(line)
    return entries


def write_gaps(entries):
    with open(GAPS, "w", encoding="utf-8", newline="\n") as f:
        for entry in entries:
            f.write(entry + "\n")


def main():
    docs = docs_text()
    doc_lines = docs.splitlines()
    source = src_text()
    update = "--update-baseline" in sys.argv[1:]
    update_gaps = "--update-gaps" in sys.argv[1:]
    problems = []
    settings = 0
    limits = 0
    values = 0
    seen = set()
    undoc_settings = []
    undoc_limits = []
    files = []
    for path in cs_files():
        rel = os.path.relpath(path, ROOT).replace(os.sep, "/")
        with open(path, encoding="utf-8", errors="ignore") as f:
            files.append((rel, f.read()))
    decl_counts = {}
    for _, text in files:
        for name in DECL.findall(text):
            decl_counts[name] = decl_counts.get(name, 0) + 1
    for rel, text in files:
        for _, key in BIND.findall(text):
            if key.endswith("SelfCheck") or (rel, "setting", key) in seen:
                continue
            seen.add((rel, "setting", key))
            settings += 1
            if not documented(key, docs):
                undoc_settings.append((rel, key))
        for name in LIMIT.findall(text) + LIMIT_PROP.findall(text):
            if not any(marker in name for marker in MARKERS) or (rel, "limit", name) in seen:
                continue
            seen.add((rel, "limit", name))
            limits += 1
            if not documented(name, docs):
                undoc_limits.append((rel, name))
        for name, literal in VALUE_CONST.findall(text):
            if not any(marker in name for marker in MARKERS) or name in BARE or (rel, "value", name) in seen:
                continue
            seen.add((rel, "value", name))
            if decl_counts.get(name, 0) != 1:
                continue
            if not documented(name, docs):
                continue
            lines = value_lines(name, doc_lines)
            if not lines:
                continue
            values += 1
            if not value_shown(value_forms(literal), lines):
                problems.append("%s: limit '%s' is documented but its value '%s' does not appear next to it" % (rel, name, literal))
                continue
            value = as_float(literal)
            if value is None:
                continue
            for text_value, number in table_values(name, doc_lines):
                if number != value:
                    problems.append("%s: limit '%s' is documented as '%s' in a table but the value is '%s'" % (rel, name, text_value, literal))
                    break
    if update_gaps:
        entries = sorted(set("setting " + key for _, key in undoc_settings)
                         | set("limit " + name for _, name in undoc_limits))
        write_gaps(entries)
        print("wrote %d gap entries to %s" % (len(entries), os.path.relpath(GAPS, ROOT).replace(os.sep, "/")))
        return 0
    stale, references = stale_references(source)
    baseline = load_baseline()
    resolved = sorted(set(baseline) - set(stale))
    if update:
        write_baseline(sorted(stale))
        print("wrote %d baseline entries" % len(stale))
        print("baseline entries now resolved: %d" % len(resolved))
        return 0
    gaps = load_gaps()
    undoc_keys = set(key for _, key in undoc_settings)
    undoc_names = set(name for _, name in undoc_limits)
    known_undoc = 0
    now_documented = 0
    for entry in gaps:
        kind, _, name = entry.partition(" ")
        if not name:
            continue
        if kind == "setting" and name in undoc_keys:
            known_undoc += 1
        elif kind == "limit" and name in undoc_names:
            known_undoc += 1
        elif kind in ("setting", "limit") and documented(name, docs):
            now_documented += 1
    for rel, key in undoc_settings:
        if "setting " + key in gaps:
            continue
        problems.append("%s: Config key '%s' is not documented" % (rel, key))
    for rel, name in undoc_limits:
        if "limit " + name in gaps:
            continue
        problems.append("%s: limit '%s' is not documented" % (rel, name))
    for name in sorted(stale):
        if name not in baseline:
            problems.append("stale doc reference '%s' (%s): not found in src" % (name, stale[name]))
    for item in problems:
        print(item)
    print("baseline entries now resolved: %d" % len(resolved))
    print("known undocumented: %d" % known_undoc)
    print("known gaps now documented: %d" % now_documented)
    print("checked %d settings, %d limits, %d values, %d references, %d problems" % (settings, limits, values, references, len(problems)))
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
