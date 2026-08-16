import os
import re

knowledge_dir = r"W:\UNIFIED COMMERCE\2nd Brain commerce\Pos-system-Knowledge\06_DATABASE_KNOWLEDGE\Tables"

all_tables = set()
table_by_file = {}

for root, _, files in os.walk(knowledge_dir):
    for f in sorted(files):
        if f.endswith(".md"):
            path = os.path.join(root, f)
            with open(path, 'r', encoding='utf-8') as file:
                content = file.read()
                
                # Try finding from the markdown table | `table_name` |
                matches_table = re.findall(r'\|\s*`([a-z_0-9]+)`\s*\|', content)
                # Try finding from headers ## `table_name`
                matches_header = re.findall(r'##\s*`([a-z_0-9]+)`', content)
                
                # Combine and remove duplicates for this file
                file_tables = set(matches_table + matches_header)
                table_by_file[f] = file_tables
                all_tables.update(file_tables)

print(f"Total unique tables across all MD files: {len(all_tables)}\n")
for f, tables in table_by_file.items():
    print(f"{f}: {len(tables)} tables")

# Save the full list of tables to a text file for review
with open(r"C:\POS_PROPJECT\BACKEND\scratch\all_md_tables.txt", 'w', encoding='utf-8') as out:
    for t in sorted(all_tables):
        out.write(t + "\n")
