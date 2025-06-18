#!/usr/bin/env python3
import os
import json
import re
import requests
from datetime import datetime

EMOJI_MAP = {
    "add": "<:new1:1376955864698585248>",
    "remove": "<:remove1:1376955857438376036>",
    "delete": "<:remove1:1376955857438376036>",
    "tweak": "<:tweak:1376955868028993658>",
    "fix": "<:fix:1376955860030459985>"
}

EMOJI_ORDER = ["add", "remove", "delete", "tweak", "fix"]
DEFAULT_COLOR = 0xE91E63

CHANGELOG_RE = re.compile(
    r"^:cl:(?:\s*([\w.,;@\- ]+))?\s+((?:- ?(?:add|remove|delete|tweak|fix): ?.+\s*)+)(?=\Z|<!--)",
    re.IGNORECASE | re.MULTILINE
)

def extract_changelog(text):
    match = CHANGELOG_RE.search(text)
    if not match:
        return None

    changelog_body = match.group(2).strip()
    groups = {key: [] for key in EMOJI_MAP.keys()}

    for line in changelog_body.splitlines():
        line = line.strip()
        if not line.startswith("-"):
            continue
        line_content = line[1:].strip()
        for key in EMOJI_MAP:
            if line_content.lower().startswith(f"{key}:"):
                desc = line_content[len(key)+1:].strip().capitalize()
                groups[key].append(f"{EMOJI_MAP[key]} {desc}")
                break

    if all(len(v) == 0 for v in groups.values()):
        return None

    grouped_output = []
    for key in EMOJI_ORDER:
        if groups[key]:
            grouped_output.extend(groups[key])
            grouped_output.append("")

    if grouped_output and grouped_output[-1] == "":
        grouped_output.pop()
    return "\n".join(grouped_output)

def extract_coauthors(pr_body):
    coauthors = []
    for line in pr_body.splitlines():
        if line.lower().startswith("co-authored-by:"):
            try:
                parts = line[len("co-authored-by:"):].strip().rsplit(" ", 1)
                name = parts[0].strip()
                email = parts[1].strip("<>")
                coauthors.append((name, email))
            except Exception:
                continue
    return coauthors

def split_changelog_by_items(changelog, chunk_size=4096):
    parts = []
    current = ""

    items = changelog.split("\n\n")
    for item in items:
        item = item.strip()
        if not item:
            continue
        item_text = item + "\n\n"

        if len(item_text) > chunk_size:
            start = 0
            while start < len(item_text):
                end = start + chunk_size
                chunk = item_text[start:end]
                if len(current) + len(chunk) > chunk_size:
                    if current:
                        parts.append(current)
                    current = ""
                current += chunk
                start = end
        else:
            if len(current) + len(item_text) > chunk_size:
                if current:
                    parts.append(current)
                current = ""
            current += item_text

    if current:
        parts.append(current)

    return parts

def create_embeds(changelog, author_name, author_avatar, branch, coauthors=None):
    chunks = split_changelog_by_items(changelog)
    embeds = []

    if coauthors is None:
        coauthors = []

    footer_text = f"🆑 {author_name}"
    if coauthors:
        footer_text += ", " + ", ".join(c["name"] for c in coauthors)

    for i, part in enumerate(chunks):
        embed = {
            "description": f"\u200b\n{part}",
            "color": DEFAULT_COLOR,
            "footer": {
                "text": f"{footer_text}, {datetime.utcnow().strftime('%d.%m.%Y %H:%M UTC')}",
                "icon_url": author_avatar
            }
        }
        if i == 0:
            embed["title"] = "⭐ Обновление"
        embeds.append(embed)

    return embeds

def main():
    event_path = os.environ.get("GITHUB_EVENT_PATH")
    webhook_url = os.environ.get("DISCORD_WEBHOOK")
    token = os.environ.get("GITHUB_TOKEN")

    if not event_path or not webhook_url:
        print("Missing required environment variables.")
        return

    with open(event_path, 'r', encoding='utf-8') as f:
        event = json.load(f)

    pr = event.get("pull_request")
    if not pr or not pr.get("merged"):
        print("PR not merged or no pull request data.")
        return

    body = pr.get("body", "")
    author_login = pr.get("user", {}).get("login", "Unknown")
    avatar_url = pr.get("user", {}).get("avatar_url", "")
    branch = pr.get("base", {}).get("ref", "master")

    match = CHANGELOG_RE.search(body)
    if not match:
        print("No valid changelog found. Skipping PR.")
        return

    alias = match.group(1)
    if alias:
        author_names = re.split(r"[.,; ]+", alias.strip())
        author_display = author_names[0]
        coauthor_profiles = [{"name": name.strip(), "avatar": None} for name in author_names[1:] if name.strip()]
    else:
        author_display = author_login
        coauthor_profiles = []

    changelog = extract_changelog(body)
    if not changelog:
        print("No valid changelog after extraction. Skipping PR.")
        return

    coauthors_raw = extract_coauthors(body)
    if token and coauthors_raw:
        for name, email in coauthors_raw:
            user_req = requests.get(
                f"https://api.github.com/search/users?q={email}+in:email",
                headers={"Authorization": f"Bearer {token}"}
            )
            if user_req.ok:
                result = user_req.json()
                if result.get("total_count", 0) > 0:
                    user = result["items"][0]
                    coauthor_profiles.append({
                        "name": user["login"],
                        "avatar": user["avatar_url"]
                    })
                else:
                    coauthor_profiles.append({"name": name, "avatar": None})
            else:
                coauthor_profiles.append({"name": name, "avatar": None})

    embeds = create_embeds(changelog, author_display, avatar_url, branch, coauthors=coauthor_profiles)
    headers = {"Content-Type": "application/json"}

    for embed in embeds:
        payload = {"embeds": [embed]}
        response = requests.post(webhook_url, headers=headers, data=json.dumps(payload))
        if response.status_code >= 400:
            print(f"❌ Failed to send webhook: {response.status_code} - {response.text}")
            return

    if token:
        repo = event["repository"]["full_name"]
        pr_number = pr["number"]
        label_url = f"https://api.github.com/repos/{repo}/issues/{pr_number}/labels"
        headers["Authorization"] = f"Bearer {token}"
        label_response = requests.post(label_url, headers=headers, json={"labels": ["CL: Valid"]})
        if label_response.status_code >= 400:
            print(f"❌ Failed to add label: {label_response.status_code} - {label_response.text}")
        else:
            print("✅ Label [CL: Valid] added.")

    print("✅ Webhook sent successfully.")

if __name__ == "__main__":
    main()