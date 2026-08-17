import os
import re

knowledge_dir = r"W:\UNIFIED COMMERCE\2nd Brain commerce\Pos-system-Knowledge\06_DATABASE_KNOWLEDGE\Tables"

all_tables = set()
table_by_file = {}
total_tables_count = 0

for root, _, files in os.walk(knowledge_dir):
    for f in sorted(files):
        if f.endswith(".md"):
            path = os.path.join(root, f)
            with open(path, 'r', encoding='utf-8') as file:
                content = file.read()
                
                # Match lines starting with ##, optional numbers, optional backticks
                # E.g., ## `business_types` or ## 1. `document_number_sequences`
                matches_header = re.findall(r'^##\s*(?:\d+\.\s*)?`?([a-z_]+)`?\s*$', content, re.MULTILINE)
                
                file_tables = set(matches_header)
                table_by_file[f] = file_tables
                all_tables.update(file_tables)
                total_tables_count += len(file_tables)

print(f"Total unique tables across all MD files: {len(all_tables)}\n")
for f, tables in table_by_file.items():
    print(f"{f}: {len(tables)} tables")
