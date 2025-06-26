import os
import re
import sys

LOCALE_DIR = "Resources/Locale"
LANGS = ["en-US", "ru-RU"]

KEY_RE = re.compile(r"^\s*([\w\-]+)\s*=\s*(.*)")

IGNORE_KEYS = {
    "cmd-whitelistadd-desc",
}

def read_ftl_file(path):
    keys = {}
    duplicates = set()
    with open(path, encoding="utf-8") as f:
        lines = f.readlines()

    i = 0
    while i < len(lines):
        line = lines[i].rstrip('\n')
        if not line.strip() or line.strip().startswith("#"):
            i += 1
            continue

        m = KEY_RE.match(line)
        if m:
            key = m.group(1)
            val = m.group(2).strip()

            if val == "":
                val_lines = []
                i += 1
                while i < len(lines):
                    next_line = lines[i]
                    if next_line.strip() and not next_line.startswith((" ", "\t")):
                        break
                    val_lines.append(next_line.rstrip('\n'))
                    i += 1
                val = "\n".join(val_lines).strip()
            else:
                i += 1

            if key in keys:
                duplicates.add(key)
            keys[key] = val
        else:
            i += 1

    return keys, duplicates

def lang_code(lang):
    return lang.split("-")[-1].upper()

def check_locales():
    lang0_path = os.path.join(LOCALE_DIR, LANGS[0])
    if not os.path.isdir(lang0_path):
        print(f"ERROR: Locale directory not found: {lang0_path}")
        sys.exit(1)

    files = []
    for root, _, filenames in os.walk(lang0_path):
        for f in filenames:
            if f.endswith(".ftl"):
                rel_path = os.path.relpath(os.path.join(root, f), lang0_path)
                files.append(rel_path)

    errors_found = False

    for filename in files:
        data = {}
        dups = {}

        for lang in LANGS:
            path = os.path.join(LOCALE_DIR, lang, filename)
            if not os.path.exists(path):
                print(f"{filename}({lang_code(lang)}) missing")
                errors_found = True
                data[lang] = {}
                dups[lang] = set()
                continue

            keys, duplicates = read_ftl_file(path)
            data[lang] = keys
            dups[lang] = duplicates

            for dup_key in duplicates:
                if dup_key not in IGNORE_KEYS:
                    print(f"duplicate key '{dup_key}' ({lang_code(lang)})")
                    errors_found = True

        all_keys = set()
        for lang in LANGS:
            all_keys.update(data[lang].keys())

        for key in all_keys:
            if key in IGNORE_KEYS:
                continue
            for lang in LANGS:
                if key not in data[lang]:
                    print(f"{key}({lang_code(lang)}) missing")
                    errors_found = True
                else:
                    if data[lang][key] == "":
                        print(f"{key}({lang_code(lang)}) empty")
                        errors_found = True

    if errors_found:
        print("\nLocalization check FAILED.")
        sys.exit(1)
    else:
        print("\nLocalization check PASSED.")

if __name__ == "__main__":
    check_locales()