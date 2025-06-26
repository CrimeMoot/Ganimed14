import os
import re
import sys
import subprocess
from googletrans import Translator

LOCALE_DIR = "Resources/Locale"
BASE_LANG = "en-US"
TARGET_LANG = "ru-RU"

KEY_RE = re.compile(r"^\s*([\w\-]+)\s*=\s*(.*)")

IGNORE_KEYS = {
    "cmd-whitelistadd-desc",
}

translator = Translator()

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
                    val_lines.append(lines[i].rstrip('\n'))
                    i += 1
                val = "\n".join(val_lines).strip()
            else:
                i += 1

            keys[key] = val
        else:
            i += 1

    return keys

def write_missing_keys(missing_keys, target_file):
    os.makedirs(os.path.dirname(target_file), exist_ok=True)

    if not os.path.exists(target_file):
        with open(target_file, "w", encoding="utf-8") as f:
            f.write("")  # create empty file

    with open(target_file, "a", encoding="utf-8") as f:
        for key, val in missing_keys.items():
            f.write(f"\n{key} = {val}\n")

def translate_text(text, src_lang="en", dest_lang="ru"):
    try:
        result = translator.translate(text, src=src_lang, dest=dest_lang)
        return result.text
    except Exception as e:
        print(f"Translation error: {e}")
        return text  # return original if error

def lang_code(lang):
    return lang.split("-")[-1].upper()

def collect_files(lang):
    path = os.path.join(LOCALE_DIR, lang)
    files = []
    for root, _, filenames in os.walk(path):
        for f in filenames:
            if f.endswith(".ftl"):
                rel = os.path.relpath(os.path.join(root, f), path)
                files.append(rel)
    return files

def read_all_keys(lang):
    path = os.path.join(LOCALE_DIR, lang)
    all_keys = {}
    for f in collect_files(lang):
        keys = read_ftl_file(os.path.join(path, f))
        all_keys[f] = keys
    return all_keys

def git_commit_and_push(files_to_commit, commit_message="Auto: add missing translations"):
    try:
        subprocess.run(["git", "add"] + files_to_commit, check=True)
        subprocess.run(["git", "commit", "-m", commit_message], check=True)
        subprocess.run(["git", "push"], check=True)
        print("Git commit and push done.")
    except subprocess.CalledProcessError as e:
        print(f"Git command failed: {e}")

def main():
    base_path = os.path.join(LOCALE_DIR, BASE_LANG)
    target_path = os.path.join(LOCALE_DIR, TARGET_LANG)

    base_files = collect_files(BASE_LANG)
    target_files = collect_files(TARGET_LANG)

    base_keys_all = read_all_keys(BASE_LANG)
    target_keys_all = read_all_keys(TARGET_LANG)

    changes_made = False
    changed_files = []

    for file_rel in base_files:
        base_keys = base_keys_all.get(file_rel, {})
        target_keys = target_keys_all.get(file_rel, {})

        missing_keys = {}

        for key, val in base_keys.items():
            if key in IGNORE_KEYS:
                continue
            if key not in target_keys or target_keys[key].strip() == "":
                src_code = BASE_LANG.split("-")[0].lower()
                dest_code = TARGET_LANG.split("-")[0].lower()
                translated_val = translate_text(val, src_lang=src_code, dest_lang=dest_code)
                missing_keys[key] = translated_val
                changes_made = True
                print(f"Adding translation for key '{key}' to {TARGET_LANG}/{file_rel}")

        if missing_keys:
            target_file_path = os.path.join(target_path, file_rel)
            write_missing_keys(missing_keys, target_file_path)
            changed_files.append(target_file_path)

    if changes_made:
        print("\nTranslation completed with new entries added.")
        git_commit_and_push(changed_files)
    else:
        print("\nAll translations are complete; nothing to add.")

if __name__ == "__main__":
    main()