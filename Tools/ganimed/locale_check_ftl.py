import os
import re
import sys

LOCALE_DIR = "Resources/Locale"
LANGS = ["en-US", "ru-RU"]

KEY_RE = re.compile(r"^([\w\-]+)\s*=\s*(.*)")

def read_ftl_file(path):
    keys = {}
    duplicates = set()
    with open(path, encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if not line or line.startswith("#"):
                continue
            m = KEY_RE.match(line)
            if not m:
                continue
            key, val = m.groups()
            if key in keys:
                duplicates.add(key)
            keys[key] = val
    return keys, duplicates

def check_locales():
    lang0_path = os.path.join(LOCALE_DIR, LANGS[0])
    if not os.path.isdir(lang0_path):
        print(f"ERROR: Locale directory not found: {lang0_path}")
        sys.exit(1)

    files = [f for f in os.listdir(lang0_path) if f.endswith(".ftl")]

    errors_found = False

    for filename in files:
        print(f"\nChecking {filename}:")
        data = {}
        dups = {}

        for lang in LANGS:
            path = os.path.join(LOCALE_DIR, lang, filename)
            if not os.path.exists(path):
                print(f"  ERROR: Missing file for language '{lang}': {filename}")
                errors_found = True
                data[lang] = {}
                dups[lang] = set()
                continue

            keys, duplicates = read_ftl_file(path)
            data[lang] = keys
            dups[lang] = duplicates

            for dup_key in duplicates:
                print(f"  ERROR: Duplicate key '{dup_key}' in {lang}/{filename}")
                errors_found = True

        keys_sets = [set(data[lang].keys()) for lang in LANGS]
        all_keys = set.union(*keys_sets)

        for key in all_keys:
            for lang in LANGS:
                if key not in data[lang]:
                    print(f"  ERROR: Missing key '{key}' in {lang}/{filename}")
                    errors_found = True
                else:
                    if data[lang][key] == "":
                        print(f"  ERROR: Empty value for key '{key}' in {lang}/{filename}")
                        errors_found = True

    if errors_found:
        print("\nLocalization check FAILED.")
        sys.exit(1)
    else:
        print("\nLocalization check PASSED.")

if __name__ == "__main__":
    check_locales()