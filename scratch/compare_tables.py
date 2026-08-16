import os
import re

knowledge_dir = r"W:\UNIFIED COMMERCE\2nd Brain commerce\Pos-system-Knowledge\06_DATABASE_KNOWLEDGE\Tables"
ef_dir = r"C:\POS_PROPJECT\BACKEND\src\E_POS.Infrastructure\Modules"

expected_tables = set()
for root, _, files in os.walk(knowledge_dir):
    for f in files:
        if f.endswith(".md"):
            path = os.path.join(root, f)
            with open(path, 'r', encoding='utf-8') as file:
                content = file.read()
                matches = re.findall(r'## `([a-z_]+)`', content)
                expected_tables.update(matches)

actual_tables = set()
for root, _, files in os.walk(ef_dir):
    for f in files:
        if f.endswith("Configuration.cs"):
            path = os.path.join(root, f)
            with open(path, 'r', encoding='utf-8') as file:
                content = file.read()
                matches = re.findall(r'builder\.ToTable\("([a-z_]+)"\)', content)
                actual_tables.update(matches)

missing_tables = expected_tables - actual_tables
extra_tables = actual_tables - expected_tables

print(f"Total expected tables from 2nd Brain: {len(expected_tables)}")
print(f"Total actual tables in EF Core: {len(actual_tables)}")
print(f"Number of missing tables: {len(missing_tables)}")
print("\nMissing tables (in 2nd Brain but NOT in EF Core):")
for t in sorted(missing_tables):
    print(f" - {t}")
