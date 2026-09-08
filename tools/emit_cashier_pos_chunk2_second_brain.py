#!/usr/bin/env python3
"""Emit Chunk 2 Second Brain registry markdown from the JSON catalog."""
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
    / "Cashier_POS_Canonical_Permission_Registry_Chunk_2.md"
)

data = json.loads(JSON_PATH.read_text(encoding="utf-8"))
defs = data["definitions"]
new_defs = [d for d in defs if d["kind"] in ("new", "documented_resolved")]
split_defs = [d for d in defs if d["kind"] == "split"]
sens = [d for d in defs if d["sensitive"]]
pre = [d for d in defs if d["kind"] == "pre_auth"]

out: list[str] = []
out += [
    "<!-- title: Cashier POS Canonical Permission Registry — Chunk 2 -->",
    "<!-- status: Active — Definitions Complete; Runtime Deferred -->",
    "<!-- system: OneVerz POS MVP -->",
    "<!-- last_updated: 2026-09-04 -->",
    "",
    "# Cashier POS Canonical Permission Registry — Chunk 2",
    "",
    "## Scope",
    "",
    "Chunk 2 finalizes canonical permission **definitions**, parent→child hierarchy,",
    "semantic types, and sensitive-data metadata for the Cashier POS inventory",
    "classified in [[Cashier_POS_Canonical_Permission_Registry_Chunk_1]].",
    "",
    "This chunk does **not** seed permissions, assign roles, change effective-permission",
    "resolution, alter API authorization, filter DTOs, or hide/show Flutter UI.",
    "",
    "Naming authority remains [[Permission_Code_List]] and ADR_007:",
    "`domain.module.feature.action` (exactly four lowercase tiers).",
    "Phone, Tablet, and Desktop share the same codes — no device prefixes.",
    "",
    "## Final counts",
    "",
    "| Metric | Count |",
    "| --- | ---: |",
    "| Total requested capabilities (Chunk 1) | 366 |",
    "| EXACT existing (unchanged classification) | 8 |",
    "| EQUIVALENT existing (unchanged classification) | 56 |",
    "| New canonical codes finalized | 14 |",
    "| Split child codes finalized | 280 |",
    "| Documented-only resolved | 1 (`pos.sales.held_sales.cancel`) |",
    "| Pre-auth exclusions | 7 |",
    "| Duplicate requested entries (Chunk 1) | 3 |",
    "| Role-assignable catalog entries (existing+new+split+resolved) | 333 |",
    "",
    "### Difference from Chunk 1",
    "",
    "- Chunk 1 proposed namespaces; Chunk 2 approves exact leaf codes.",
    "- `sales.park.cancel` (DOCUMENTED_ONLY) is approved as `pos.sales.held_sales.cancel`.",
    "- Existing SoT parents use `pos.till.session.*` and `pos.cash_drawer.position.view` / `physical.manage` (not draft `tills` / `dashboard.view` / `physical.open` names).",
    "- Pre-auth capabilities use `pre_auth.login.*` classification codes outside role assignment.",
    "",
    "## Machine catalog",
    "",
    "- Backend: `E_POS.Domain/.../Catalog/CashierPos/CashierPosCanonicalPermissionCatalog.cs`",
    "- JSON mirror: `cashier_pos_canonical_permissions.chunk2.json`",
    "- Flutter: `lib/core/access/cashier_pos/cashier_pos_canonical_permission_codes.dart`",
    "",
    "## New + documented-resolved codes",
    "",
    "| Permission Code | Parent | Module | Type | Sensitive | Reason |",
    "| --- | --- | --- | --- | --- | --- |",
]
for d in new_defs:
    sens_flag = "YES" if d["sensitive"] else "NO"
    parent = d["parent"] or ""
    out.append(
        f"| `{d['code']}` | `{parent}` | {d['module']} | {d['type']} | {sens_flag} | {d['reason']} |"
    )

out += [
    "",
    "## Split child codes (280)",
    "",
    "Full leaf list is in the machine catalog. Summary by parent:",
    "",
    "| Parent | Child count |",
    "| --- | ---: |",
]
tree: dict[str, list[str]] = defaultdict(list)
for d in split_defs:
    tree[d["parent"]].append(d["code"])
for parent in sorted(tree):
    out.append(f"| `{parent}` | {len(tree[parent])} |")

out += ["", "## Parent / child hierarchy (domain groups)", "", "```text"]
for parent in sorted(tree):
    out.append(parent)
    children = sorted(tree[parent])
    extras = [d["code"] for d in new_defs if d["parent"] == parent]
    all_children = children + extras
    for i, child in enumerate(all_children):
        branch = "└──" if i == len(all_children) - 1 else "├──"
        suffix = "  [NEW/RESOLVED]" if child in extras else ""
        out.append(f"  {branch} {child}{suffix}")
out.append("```")

out += [
    "",
    "## Sensitive permission registry",
    "",
    "| Permission | Data Protected | Parent |",
    "| --- | --- | --- |",
]
for d in sens:
    parent = d["parent"] or "—"
    out.append(f"| `{d['code']}` | {d['reason']} | `{parent}` |")

out += [
    "",
    "## Pre-auth exclusions (PRE_AUTH_CONFIGURATION)",
    "",
    "These are not logged-in cashier role permissions:",
    "",
]
for d in pre:
    out.append(f"- `{d['code']}` — {d['reason']}")

out += [
    "",
    "Assigning login controls through cashier roles would be circular and is forbidden.",
    "",
    "## Existing permissions reused (representative)",
    "",
    "| Capability family | Existing canonical | Decision |",
    "| --- | --- | --- |",
    "| POS Home / dashboard | `pos.sales.dashboard.view` | REUSE |",
    "| New Sale route | `pos.sales.new_sale.view` | REUSE |",
    "| Create sale | `pos.sales.new_sale.create` | REUSE |",
    "| Catalog view/search | `pos.sales.catalog.view` / `search` | REUSE |",
    "| Cart mutations | `pos.sales.cart.*` | REUSE |",
    "| Checkout execute | `pos.sales.checkout.execute` | REUSE |",
    "| Held sales create/view/recall | `pos.sales.held_sales.*` | REUSE |",
    "| Cash/Card/QR/Split accept | `pos.payments.*.accept` | REUSE — remain independent |",
    "| Receipts view/print/reprint | `pos.receipts.*` | REUSE |",
    "| Customers view/create/update | `pos.customers.management.*` | REUSE |",
    "| Notifications inbox | `pos.notifications.alerts.view` | REUSE parent |",
    "| Cash drawer view/open/movements | `pos.cash_drawer.*` | REUSE parents; movement types NEW |",
    "| Till open/close/view | `pos.till.session.*` | REUSE |",
    "| Online orders entry | `commerce.online_order.orders.access` | REUSE |",
    "| Returns entry | `pos.returns.search_sale.view` | REUSE |",
    "| Print again | `pos.receipts.physical.print` / `history.reprint` | REUSE |",
    "| Start new sale (success) | `pos.sales.new_sale.create` | REUSE |",
    "",
    "## Runtime semantics (contract only)",
    "",
    "Future Chunk 3+ expected evaluation:",
    "",
    "`effective(child) = effective(parent) AND effective(child)`",
    "",
    "Parent grant does **not** auto-grant all children forever without an explicit",
    "migration/default policy. Resolver implementation is deferred.",
    "",
    "## Implementation status",
    "",
    "| Item | Status |",
    "| --- | --- |",
    "| Leaf code approval | DONE |",
    "| Parent/child metadata | DONE |",
    "| Sensitive flags | DONE |",
    "| Pre-auth classification | DONE |",
    "| Backend/Flutter catalog sync | DONE |",
    "| Integrity tests | DONE |",
    "| DB migration / seeds / role assignment | DEFERRED Chunk 3+ |",
    "| Effective permission resolver | DEFERRED |",
    "| API authorization / DTO filtering | DEFERRED |",
    "| Flutter PermissionGate / UI hide | DEFERRED |",
    "",
    "## Deferred to Chunk 3+",
    "",
    "- `permission_definitions` migration/seed for new+split codes",
    "- Role/user assignment defaults and migration behavior",
    "- Effective permission resolver",
    "- Backend HasPermission checks and DTO field filtering",
    "- Flutter UI visibility / route guards for fine-grained children",
    "- Tenant Admin permission configuration UI",
    "",
]

OUT.write_text("\n".join(out) + "\n", encoding="utf-8")
print(f"wrote {OUT} ({OUT.stat().st_size} bytes)")
