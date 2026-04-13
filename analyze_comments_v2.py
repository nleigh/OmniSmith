import json
import re

file_path = r'C:\Users\nleig\.local\share\opencode\tool-output\tool_d83f119f5001ZQPoV0x7jlB9l4'

def analyze():
    try:
        with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
            content = f.read().strip()
        
        # The file is likely a JSON array of objects: [{}, {}, ...]
        # If json.loads fails on the whole thing, we'll try to extract objects manually.
        try:
            # Try to fix common issues like trailing commas or extra data
            start = content.find('[')
            end = content.rfind(']')
            if start != -1 and end != -1:
                data = json.loads(content[start:end+1])
            else:
                data = json.loads(content)
        except json.JSONDecodeError:
            # Fallback: Extract objects using a regex that matches { ... }
            # This is imperfect for nested objects but might work for this flat structure.
            data = []
            # This regex finds strings that look like JSON objects.
            # We look for things starting with { and ending with }, 
            # taking care of the fact that there might be multiple.
            # A more robust way is to use a simple parser.
            objs = re.findall(r'\{.*?\}', content, re.DOTALL)
            for obj_str in objs:
                try:
                    data.append(json.loads(obj_str))
                except:
                    continue

        if not data:
            print("No comments found to analyze.")
            return

        issues = []
        for c in data:
            body = c.get('body', '')
            url = c.get('html_url', '')
            
            is_issue = False
            if '?' in body:
                is_issue = True
            elif '```suggestion' in body:
                is_issue = True
            elif any(word in body.lower() for word in ['suggest', 'incorrect', 'wrong', 'bug', 'fix', 'should', 'missing', 'flaw']):
                is_issue = True
            
            if is_issue:
                # Extract PR number
                pr_match = re.search(r'/pull/(\d+)', url)
                pr_num = pr_match.group(1) if pr_match else 'Unknown'
                
                issues.append({
                    'id': c.get('id'),
                    'pr': pr_num,
                    'body': body
                })

        # Check for replies. 
        # In GitHub's API, replies have 'in_reply_to_id'.
        missed = []
        for issue in issues:
            issue_id = issue['id']
            has_reply = any(c.get('in_reply_to_id') == issue_id for c in data)
            
            # Also check if the author of the PR responded to this comment 
            # (this is harder without knowing the PR author).
            # For now, any reply is a sign of "addressed" or "discussed".
            
            if not has_reply:
                missed.append(f"PR {issue['pr']}: {issue['body'].strip()[:150].replace(chr(10), ' ')}...")

        if not missed:
            print("All closed PR comments were addressed")
        else:
            for m in missed:
                print(m)

    except Exception as e:
        print(f"Error during analysis: {e}")

if __name__ == '__main__':
    analyze()
