import json

with open('comments.json', 'r', encoding='utf-16') as f:
    comments = json.load(f)

for c in comments:
    if "copilot" in c.get('user', {}).get('login', '').lower():
        print(f"[{c.get('path')}:{c.get('line')}]")
        print(c.get('body'))
        print("-" * 40)
