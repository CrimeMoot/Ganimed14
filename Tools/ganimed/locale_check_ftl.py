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

            keys[key] = val
        else:
            i += 1

    return keys

def lang_code(lang):
    return lang.split("-")[-1].upper()

def collect_all_keys_for_lang(lang):
    lang_path = os.path.join(LOCALE_DIR, lang)
    if not os.path.isdir(lang_path):
        print(f"ERROR: Locale directory not found: {lang_path}")
        sys.exit(1)

    all_keys = {}

    for root, _, files in os.walk(lang_path):
        for f in files:
            if not f.endswith(".ftl"):
                continue
            path = os.path.join(root, f)
            keys = read_ftl_file(path)

            for k, v in keys.items():
                all_keys[k] = v

    return all_keys

def check_locales():
    base_lang = LANGS[0]
    base_keys = collect_all_keys_for_lang(base_lang)

    errors_found = False

    langs_keys = {}
    for lang in LANGS[1:]:
        keys = collect_all_keys_for_lang(lang)
        langs_keys[lang] = keys

    for key in base_keys.keys():
        if key in IGNORE_KEYS:
            continue
        for lang in LANGS[1:]:
            if key not in langs_keys[lang]:
                print(f"{key}({lang_code(lang)}) missing")
                errors_found = True
            else:
                if langs_keys[lang][key] == "":
                    print(f"{key}({lang_code(lang)}) empty")
                    errors_found = True

    if errors_found:
        print("\nLocalization check FAILED.")
        sys.exit(1)
    else:
        print("\nLocalization check PASSED.")

if __name__ == "__main__":
    check_locales()