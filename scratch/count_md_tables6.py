import os
import re

knowledge_dir = r"W:\UNIFIED COMMERCE\2nd Brain commerce\Pos-system-Knowledge\06_DATABASE_KNOWLEDGE\Tables"
ef_dir = r"C:\POS_PROPJECT\BACKEND\src\E_POS.Infrastructure\Modules"

all_tables = set()

for root, _, files in os.walk(knowledge_dir):
    for f in sorted(files):
        if f.endswith(".md"):
            path = os.path.join(root, f)
            with open(path, 'r', encoding='utf-8') as file:
                content = file.read()
                matches_header = re.findall(r'^##\s*(?:\d+\.\s*)?`?([a-z_]+)`?\s*$', content, re.MULTILINE)
                all_tables.update(matches_header)

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
print(f"Total MD Tables: {len(all_tables)}")
print(f"Total EF Tables: {len(actual_tables)}")
print(f"\nMissing tables in EF Core ({len(missing)}):")
for t in sorted(missing):
    print(f" - {t}")
