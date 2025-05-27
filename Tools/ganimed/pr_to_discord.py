#!/usr/bin/env python3
import os
import json
import re
import requests
from datetime import datetime

MAX_DESCRIPTION_LENGTH = 6000

def extract_section(text, header):
    pattern = rf"## {re.escape(header)}\s*(.*?)\s*(?=\n## |\Z)"
    match = re.search(pattern, text, re.DOTALL | re.IGNORECASE)
    if match:
        section = re.sub(r'<!--.*?-->', '', match.group(1), flags=re.DOTALL).strip()
        return section if section else None
    return None

def extract_changelog(text):
    match = re.search(r":cl:\s*(.*?)\s*(?:<!--|\Z)", text, re.DOTALL)
    if match:
        return match.group(1).strip()
    return None

def extract_all_image_urls(text):
    if not text:
        return []
    urls = re.findall(r'!\[.*?\]\((https?://[^\s)]+)\)', text)
    raw_urls = re.findall(r'(https?://\S+)', text)
    for url in raw_urls:
        if url.lower().endswith(('.png', '.jpg', '.jpeg', '.gif', '.webp')):
            if url not in urls:
                urls.append(url)
    return urls[:10]

def clean_media_text(media):
    if not media:
        return None
    cleaned = re.sub(r'!\[.*?\]\(.*?\)', '', media)
    cleaned = re.sub(r'\[.*?\]\(.*?\)', '', cleaned)
    cleaned = cleaned.strip()
    return cleaned if cleaned else None

def chunk_text(text, max_len):
    if len(text) <= max_len:
        return [text]
    parts = []
    lines = text.split('\n')
    current = ""
    for line in lines:
        if len(current) + len(line) + 1 > max_len:
            parts.append(current.strip())
            current = line + "\n"
        else:
            current += line + "\n"
    if current.strip():
        parts.append(current.strip())
    return parts

def get_color_by_changelog(changelog):
    if not changelog:
        return 0xE91E63
    changelog = changelog.lower()
    lines = changelog.splitlines()
    for line in lines:
        line = line.strip()
        if line.startswith('add:'):
            return 0x2ECC71
        elif line.startswith('fix:'):
            return 0x3498DB
        elif line.startswith('tweak:'):
            return 0xF1C40F
        elif line.startswith('remove:') or line.startswith('delete:'):
            return 0xE74C3C
    return 581478

def create_embed(title, description, footer_text, image_url=None, color=5814783):
    embed = {
        "title": title,
        "description": description,
        "color": color,
        "footer": {"text": footer_text},
        "timestamp": datetime.utcnow().isoformat()
    }
    if image_url:
        embed["image"] = {"url": image_url}
    return embed

def main():
    event_path = os.environ.get("GITHUB_EVENT_PATH")
    webhook_url = os.environ.get("DISCORD_WEBHOOK")

    if not event_path or not webhook_url:
        print("Missing required environment variables.")
        return

    with open(event_path, 'r', encoding='utf-8') as f:
        event = json.load(f)

    pr = event.get("pull_request")
    if not pr:
        print("No pull request data found in event.")
        return

    title = pr.get("title", "No title")
    body = pr.get("body", "")
    author = pr.get("user", {}).get("login", "Unknown")

    description = extract_section(body, "Описание PR")
    media = extract_section(body, "Медиа")
    changes = extract_section(body, "Список изменений")
    changelog = extract_changelog(body)

    media_text = clean_media_text(media)
    image_urls = extract_all_image_urls(media)

    sections = []
    if description:
        sections.append(f"📝 **Описание PR:**\n{description}")
    if media_text:
        sections.append(f"🖼 **Медиа:**\n{media_text}")
    if changes:
        sections.append(f"📌 **Список изменений:**\n{changes}")
    if changelog:
        sections.append(f"📋 **Changelog:**\n{changelog}")

    full_text = "\n\n".join(sections) if sections else "Нет описания."

    text_chunks = chunk_text(full_text, MAX_DESCRIPTION_LENGTH)

    color = get_color_by_changelog(changelog or changes)

    embeds = []
    for i, chunk in enumerate(text_chunks):
        image_url = image_urls[i] if i < len(image_urls) else None
        embeds.append(create_embed(title if i == 0 else f"{title} (часть {i+1})", chunk, author, image_url, color))

    headers = {"Content-Type": "application/json"}
    for embed in embeds:
        payload = {"embeds": [embed]}
        response = requests.post(webhook_url, headers=headers, data=json.dumps(payload))
        if response.status_code >= 400:
            print(f"Failed to send webhook: {response.status_code} - {response.text}")
        else:
            print("✅ Discord webhook sent successfully.")

if __name__ == "__main__":
    main()