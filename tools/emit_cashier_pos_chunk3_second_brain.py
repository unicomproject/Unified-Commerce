#!/usr/bin/env python3
"""Emit Cashier POS Chunk 3 Second Brain seed/migration record."""
from __future__ import annotations

import json
from collections import defaultdict
from pathlib import Path

ROOT = Path(r"C:\Users\User\Downloads\EPOS")
JSON_PATH = (
    ROOT
    / "POS Backend/Unified-Commerce/src/E_POS.Domain/Modules/Tenant/AccessControl/Catalog/CashierPos"
    / "cashier_pos_canonical_permissions.chunk2.json"
)
OUT = (
    ROOT
    / "POS Secondbrain/Pos-system-Knowledge/02_ACCESS_CONTROL"
    / "Cashier_POS_Canonical_Permission_Registry_Chunk_3.md"
)

data = json.loads(JSON_PATH.read_text(encoding="utf-8"))
defs = data["definitions"]
role = [d for d in defs if d["roleAssignable"]]
fine = [d for d in defs if d["kind"] in ("new", "split", "documented_resolved")]
existing = [d for d in defs if d["kind"] == "existing"]
pre = [d for d in defs if d["kind"] == "pre_auth"]
pairs = [(d["parent"], d["code"], d["reason"]) for d in defs if d.get("parent") and d["roleAssignable"]]
by_parent: dict[str, list[tuple[str, str]]] = defaultdict(list)
for parent, child, reason in pairs:
    by_parent[parent].append((child, reason))

out: list[str] = []
out += [
    "<!-- title: Cashier POS Canonical Permission Registry — Chunk 3 -->",
    "<!-- status: Active — Seed + Compatibility Migration; Runtime Deferred -->",
    "<!-- system: OneVerz POS MVP -->",
    "<!-- last_updated: 2026-09-04 -->",
    "",
    "# Cashier POS Canonical Permission Registry — Chunk 3",
    "",
    "## Scope",
    "",
    "Chunk 3 seeds **role-assignable** Cashier POS canonical permission definitions",
    "into `permission_definitions` and performs **backward-compatible parent→child",
    "assignment backfill** for existing tenant role and user grants.",
    "",
    "This chunk does **not** implement runtime authorization, Flutter visibility,",
    "DTO filtering, effective-permission resolution, or Tenant Admin assignment UI.",
    "",
    "Canonical source: [[Cashier_POS_Canonical_Permission_Registry_Chunk_2]] /",
    "`CashierPosCanonicalPermissionCatalog`.",
    "",
    "## Strict four-tier format rule (BLOCKING)",
    "",
    "Every seeded code MUST match:",
    "",
    "`domain.module.feature.action`",
    "",
    "Exactly four lowercase non-empty segments. Wildcards (`*`), slashes (`/`),",
    "whitespace, empty segments, and shorthand patterns are **forbidden** as seed values.",
    "",
    "Validation helper: `CashierPosPermissionCodeTaxonomy`.",
    "",
    "## Seed mechanism",
    "",
    "| Item | Value |",
    "| --- | --- |",
    "| Mechanism | EF migration SQL via seed builder (existing architecture) |",
    "| Migration | `20260904140000_SeedCashierPosChunk3CanonicalPermissions` |",
    "| Seed class | `CashierPosChunk3PermissionSeedData` |",
    "| Catalog source | `CashierPosCanonicalPermissionCatalog.RoleAssignable` |",
    "| Upsert key | `permission_code` (`ON CONFLICT DO UPDATE`) |",
    "| New row IDs | `md5('cashier-pos-chunk3-permission:' \\|\\| code)::uuid` |",
    "| Schema change | None |",
    "",
    "## Counts",
    "",
    "| Metric | Count |",
    "| --- | ---: |",
    "| Role-assignable catalog codes | 333 |",
    "| Existing catalog codes reused (upsert) | 38 |",
    "| Fine-grained codes (new/split/resolved) | 295 |",
    "| Pre-auth excluded from seed | 7 |",
    "| Parent→child compatibility pairs | "
    + str(len(pairs))
    + " |",
    "",
    "## Pre-auth exclusions",
    "",
    "Not inserted into `permission_definitions` role catalog:",
    "",
]
for d in pre:
    out.append(f"- `{d['code']}`")

out += [
    "",
    "No separate pre-auth configuration store was created in Chunk 3.",
    "",
    "## Compatibility migration rule",
    "",
    "MIGRATION TIME ONLY:",
    "",
    "1. If a tenant role has parent permission granted (`revoked_at IS NULL`),",
    "   grant each catalog child of that parent to the **same** `(tenant_id, role_id)`.",
    "2. If a tenant user has parent permission granted (`revoked_at IS NULL`),",
    "   grant each catalog child to the **same** `(tenant_id, user_id)`.",
    "3. If parent is absent for a tenant/role/user → **do not** grant children.",
    "4. Idempotent: `ON CONFLICT DO NOTHING` / deterministic backfill IDs.",
    "5. Role backfill rows are marked with notes:",
    "   `Chunk 3 parent→child compatibility backfill.`",
    "",
    "This is **not** permanent auto-grant semantics. After migration, parent and",
    "child grants are independently configurable. Runtime",
    "`effective(child) = effective(parent) AND effective(child)` remains deferred.",
    "",
    "## Tenant safety",
    "",
    "- Backfill copies only existing authorization intent within the same tenant.",
    "- No cross-tenant grants.",
    "- No global grant-all-roles behaviour.",
    "",
    "## Idempotency",
    "",
    "- Definition upsert is conflict-safe on `permission_code`.",
    "- Assignment backfill is conflict-safe; second run inserts zero duplicates.",
    "",
    "## Rollback",
    "",
    "Down migration:",
    "",
    "1. Deletes role assignments with Chunk 3 compatibility notes for fine-grained codes.",
    "2. Deletes user assignments whose deterministic Chunk 3 compat IDs match.",
    "3. Deletes fine-grained `permission_definitions` rows whose IDs match the",
    "   Chunk 3 md5 prefix (does **not** delete historically seeded existing parents).",
    "",
    "## Parent → child backfill matrix",
    "",
    "| Existing Parent Permission | New Child Permission | Migration Rule | Reason |",
    "| --- | --- | --- | --- |",
]
for parent in sorted(by_parent):
    for child, reason in sorted(by_parent[parent], key=lambda x: x[0]):
        out.append(
            f"| `{parent}` | `{child}` | If parent granted → grant child (same tenant/role or user) | {reason} |"
        )

out += [
    "",
    "## Special business migration decisions",
    "",
    "| Capability | Decision |",
    "| --- | --- |",
    "| Held Sale Cancel | Backfill from `pos.sales.held_sales.create` |",
    "| Cash In / Out / Drop | Backfill from `pos.cash_drawer.movements.create` |",
    "| Customer Attach to Sale | Backfill from `pos.customers.management.view` |",
    "| Customer Deactivate | Backfill from `pos.customers.management.update` only |",
    "| Settings navigation | Backfill from `pos.sales.dashboard.view` |",
    "| Returns home entry | Backfill from `pos.returns.search_sale.view` |",
    "| Online Orders home entry | Backfill from `commerce.online_order.orders.access` |",
    "",
    "## Implementation status",
    "",
    "| Item | Status |",
    "| --- | --- |",
    "| Definition seed | DONE |",
    "| Compatibility backfill | DONE |",
    "| Format validation tests | DONE |",
    "| Idempotency / tenant-scope SQL contract tests | DONE |",
    "| Runtime authorization | DEFERRED Chunk 4+ |",
    "| Flutter PermissionGate / UI hide | DEFERRED |",
    "| Effective permission resolver | DEFERRED |",
    "| Tenant Admin assignment UI | DEFERRED |",
    "",
    "## Deferred to Chunk 4+",
    "",
    "- Ongoing role-assignment workflows",
    "- Effective permission resolver",
    "- Backend endpoint HasPermission expansion for fine-grained codes",
    "- Flutter visibility gates",
    "- Sensitive DTO filtering",
    "- Tenant Admin permission configuration UI",
    "",
]

OUT.write_text("\n".join(out) + "\n", encoding="utf-8")
print(f"wrote {OUT} ({OUT.stat().st_size} bytes) pairs={len(pairs)}")
