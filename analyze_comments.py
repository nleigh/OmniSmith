import json
import re

file_path = r'C:\Users\nleig\.local\share\opencode\tool-output\tool_d83f119f5001ZQPoV0x7jlB9l4'

def extract_json_array(text):
    start = text.find('[')
    if start == -1:
        return None
    
    bracket_count = 0
    for i in range(start, len(text)):
        if text[i] == '[':
            bracket_count += 1
        elif text[i] == ']':
            bracket_count -= 1
            if bracket_count == 0:
                return text[start:i+1]
    return None

try:
    with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
        content = f.read()
        json_str = extract_json_array(content)
        if not json_str:
            print("No JSON array found")
            exit(1)
        
        data = json.loads(json_str)
        
        results = []
        for c in data:
            body = c.get('body', '')
            html_url = c.get('html_url', '')
            
            is_potential_issue = False
            if '?' in body:
                is_potential_issue = True
            if any(word in body.lower() for word in ['suggest', 'incorrect', 'wrong', 'bug', 'fix', 'should', 'missing', 'flaw']):
                is_potential_issue = True
            
            if is_potential_issue:
                match = re.search(r'/pull/(\d+)', html_url)
                pr_num = match.group(1) if match else 'Unknown'
                results.append({
                    'pr': pr_num,
                    'body': body,
                    'id': c.get('id'),
                    'url': html_url
                })
        
        final_missed = []
        for issue in results:
            issue_id = issue['id']
            has_reply = any(c.get('in_reply_to_id') == issue_id for c in data)
            if not has_reply:
                final_missed.append(f"PR {issue['pr']}: {issue['body'].strip()[:200]}...")

        if not final_missed:
            print("All closed PR comments were addressed")
        else:
            for item in final_missed:
                print(item)

except Exception as e:
    print(f"Error: {e}")
