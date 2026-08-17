import os
import re

knowledge_dir = r"W:\UNIFIED COMMERCE\2nd Brain commerce\Pos-system-Knowledge\06_DATABASE_KNOWLEDGE\Tables"
ef_dir = r"C:\POS_PROPJECT\BACKEND\src\E_POS.Infrastructure\Modules"

all_tables = set()
table_by_file = {}

for root, _, files in os.walk(knowledge_dir):
    for f in sorted(files):
        if f.endswith(".md"):
            path = os.path.join(root, f)
            with open(path, 'r', encoding='utf-8') as file:
                content = file.read()
                
                # Match lines like "## `table_name`" or "## table_name"
                # Since table names are lowercase words with underscores, we can match:
                matches_header = re.findall(r'^##\s*`?([a-z_][a-z0-9_]+)`?\s*$', content, re.MULTILINE)
                
                # Also try to extract from table definitions just in case some are missed
                # Some tables have | Table | Purpose |
                # and rows | `table_name` | ... |
                matches_md_table = re.findall(r'\|\s*(?:\d+\s*\|\s*)?`([a-z_][a-z0-9_]+)`\s*\|', content)

                file_tables = set(matches_header + matches_md_table)
                table_by_file[f] = file_tables
                all_tables.update(file_tables)

print(f"Total unique tables across all MD files: {len(all_tables)}\n")
for f, tables in table_by_file.items():
    print(f"{f}: {len(tables)} tables")

# Now compare with EF Core
actual_tables = set()
for root, _, files in os.walk(ef_dir):
    for f in files:
        if f.endswith("Configuration.cs"):
            path = os.path.join(root, f)
            with open(path, 'r', encoding='utf-8') as file:
                content = file.read()
                matches = re.findall(r'builder\.ToTable\("([a-z_]+)"\)', content)
                actual_tables.update(matches)

missing = all_tables - actual_tables
print(f"\nMissing tables in EF Core ({len(missing)}):")
for t in sorted(missing):
    print(f" - {t}")
