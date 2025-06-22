import os
import re

LOCALE_DIR = "Resources/Locale"
LANGS = ["en-EN", "ru-RU"]

KEY_RE = re.compile(r"^([\w-]+)\s*=\s*(.*)")

def read_ftl_file(path):
    keys = {}
    duplicates = set()
    with open(path, encoding="utf-8") as f:
        for i, line in enumerate(f, 1):
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
    files = [f for f in os.listdir(lang0_path) if f.endswith(".ftl")]

    errors = False

    for filename in files:
        print(f"\nChecking {filename}:")
        data = {}
        dups = {}
        for lang in LANGS:
            path = os.path.join(LOCALE_DIR, lang, filename)
            if not os.path.exists(path):
                print(f"  ERROR: File missing in {lang}: {filename}")
                errors = True
                data[lang] = {}
                dups[lang] = set()
                continue
            keys, duplicates = read_ftl_file(path)
            data[lang] = keys
            dups[lang] = duplicates
            for dup in duplicates:
                print(f"  ERROR: Duplicate key '{dup}' in {lang}/{filename}")
                errors = True

        keys_sets = [set(data[lang].keys()) for lang in LANGS]
        all_keys = set.union(*keys_sets)

        for key in all_keys:
            for lang in LANGS:
                if key not in data[lang]:
                    print(f"  ERROR: Key '{key}' missing in {lang}/{filename}")
                    errors = True
                else:
                    if data[lang][key] == "":
                        print(f"  ERROR: Empty value for key '{key}' in {lang}/{filename}")
                        errors = True

    if errors:
        print("\nErrors found in localization files.")
        exit(1)
    else:
        print("\nLocalization check passed successfully.")

if __name__ == "__main__":
    check_locales()
