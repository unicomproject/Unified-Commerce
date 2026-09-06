#!/usr/bin/env python3
"""Generate Chunk 2 Cashier POS canonical permission catalog (definitions only)."""
from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(r"C:\Users\User\Downloads\EPOS")
BACKEND_CATALOG = (
    ROOT
    / "POS Backend/Unified-Commerce/src/E_POS.Domain/Modules/Tenant/AccessControl/Catalog/CashierPos"
)
FLUTTER_CATALOG = ROOT / "Pos Frontend/Nytroz-POS-App/lib/core/access/cashier_pos"
TOOL_DIR = ROOT / "POS Backend/Unified-Commerce/tools"

EXISTING = [
    "pos.sales.dashboard.view",
    "pos.sales.new_sale.view",
    "pos.sales.new_sale.create",
    "pos.sales.catalog.view",
    "pos.sales.catalog.search",
    "pos.sales.cart.manage",
    "pos.sales.cart.add_item",
    "pos.sales.cart.update_item",
    "pos.sales.cart.remove_item",
    "pos.sales.cart.clear",
    "pos.sales.checkout.execute",
    "pos.sales.manual_discount.apply",
    "pos.sales.held_sales.create",
    "pos.sales.held_sales.view",
    "pos.sales.held_sales.recall",
    "pos.sales.order_history.view",
    "pos.payments.cash.accept",
    "pos.payments.card.accept",
    "pos.payments.qr.accept",
    "pos.payments.split.accept",
    "pos.receipts.digital.view",
    "pos.receipts.physical.print",
    "pos.receipts.history.reprint",
    "pos.orders.history.view",
    "pos.customers.management.view",
    "pos.customers.management.create",
    "pos.customers.management.update",
    "pos.returns.search_sale.view",
    "pos.returns.workflow.create",
    "pos.cash_drawer.position.view",
    "pos.cash_drawer.physical.manage",
    "pos.cash_drawer.movements.create",
    "pos.till.session.open",
    "pos.till.session.close",
    "pos.till.session.view",
    "pos.hardware.local_agent.settings",
    "pos.notifications.alerts.view",
    "commerce.online_order.orders.access",
    # Pre-existing Permission_Code_List commerce Online Order codes omitted from
    # Chunk 2 EXISTING set — reconciled (not invented). Spelling: fulfilment.
    "commerce.online_order.orders.view",
    "commerce.online_order.fulfilment.start",
    "commerce.online_order.picking.view",
    "commerce.online_order.picking.pick",
    "commerce.online_order.picking.scan",
    "commerce.online_order.picking.manual_entry",
    "commerce.online_order.picking.report_issue",
    "commerce.online_order.picking.note",
    "commerce.online_order.packing.view",
    "commerce.online_order.packing.pack",
    "commerce.online_order.collection.mark_ready",
]

defs: list[dict] = []


def _validate(code: str) -> None:
    parts = code.split(".")
    if len(parts) != 4:
        raise SystemExit(f"not 4-tier: {code}")
    if code != code.lower():
        raise SystemExit(f"not lowercase: {code}")


def add(
    code: str,
    *,
    parent: str | None,
    module: str,
    ptype: str,
    sensitive: bool,
    kind: str,
    reason: str,
) -> None:
    _validate(code)
    defs.append(
        {
            "code": code,
            "parent": parent,
            "module": module,
            "type": ptype,
            "sensitive": sensitive,
            "kind": kind,
            "reason": reason,
            "roleAssignable": kind != "pre_auth",
        }
    )


def split(code, parent, module, ptype, sensitive, reason):
    add(code, parent=parent, module=module, ptype=ptype, sensitive=sensitive, kind="split", reason=reason)


def new(code, parent, module, ptype, sensitive, reason):
    add(code, parent=parent, module=module, ptype=ptype, sensitive=sensitive, kind="new", reason=reason)


# Pre-auth exclusions (7) — PRE_AUTH_CONFIGURATION, not role-assignable
for code, reason in [
    ("pre_auth.login.screen.view", "Login screen container"),
    ("pre_auth.login.branding.view", "Login branding"),
    ("pre_auth.login.email.input", "Email input"),
    ("pre_auth.login.password.input", "Password input"),
    ("pre_auth.login.password_visibility.toggle", "Password visibility"),
    ("pre_auth.login.submit.execute", "Login submit"),
    ("pre_auth.login.validation.message", "Validation message"),
]:
    add(
        code,
        parent=None,
        module="PreAuth",
        ptype="PRE_AUTH_CONFIGURATION",
        sensitive=False,
        kind="pre_auth",
        reason=reason,
    )

ACTION_SUFFIXES = (
    ".create",
    ".execute",
    ".accept",
    ".apply",
    ".open",
    ".close",
    ".print",
    ".reprint",
    ".recall",
    ".manage",
    ".search",
    ".update",
    ".access",
)
for code in EXISTING:
    ptype = "ACTION" if code.endswith(ACTION_SUFFIXES) else "SCREEN"
    add(
        code,
        parent=None,
        module="Existing",
        ptype=ptype,
        sensitive=False,
        kind="existing",
        reason="Approved existing canonical business permission",
    )

P_DASH = "pos.sales.dashboard.view"
P_NOTIF = "pos.notifications.alerts.view"
P_CAT = "pos.sales.catalog.view"
P_HELD_V = "pos.sales.held_sales.view"
P_HELD_C = "pos.sales.held_sales.create"
P_CHK = "pos.sales.checkout.execute"
P_CASH = "pos.payments.cash.accept"
P_RCPT = "pos.receipts.digital.view"
P_CUST = "pos.customers.management.view"
P_CD = "pos.cash_drawer.position.view"
P_PHYS = "pos.cash_drawer.physical.manage"
P_MOV = "pos.cash_drawer.movements.create"
P_OPEN = "pos.till.session.open"
P_CLOSE = "pos.till.session.close"

# 1 Shell SPLIT
for code, ptype, reason in [
    ("pos.shell.topbar.container", "CONTAINER", "Top bar container"),
    ("pos.shell.topbar.brand", "FIELD", "Brand/logo"),
    ("pos.shell.topbar.session_status", "STATUS", "Till session status"),
    ("pos.shell.topbar.outlet", "FIELD", "Outlet"),
    ("pos.shell.topbar.till", "FIELD", "Till"),
    ("pos.shell.topbar.connectivity", "STATUS", "Connectivity state"),
    ("pos.shell.topbar.clock", "FIELD", "Clock"),
    ("pos.shell.topbar.notification_bell", "NAVIGATION", "Notification bell chrome"),
    ("pos.shell.bottom_nav.container", "CONTAINER", "Bottom navigation container"),
]:
    split(code, P_DASH, "Shell", ptype, False, reason)

# 2 Notifications SPLIT
for code, ptype, reason in [
    ("pos.notifications.panel.view", "CONTAINER", "Notification panel"),
    ("pos.notifications.panel.unread_count", "FIELD", "Unread count"),
    ("pos.notifications.messages.list", "SECTION", "Message list"),
    ("pos.notifications.messages.title", "FIELD", "Message title"),
    ("pos.notifications.messages.body", "FIELD", "Message body"),
    ("pos.notifications.messages.timestamp", "FIELD", "Message timestamp"),
    ("pos.notifications.messages.open", "ACTION", "Open message"),
    ("pos.notifications.messages.mark_read", "ACTION", "Mark read"),
    ("pos.notifications.messages.dismiss", "ACTION", "Dismiss"),
    ("pos.notifications.messages.mark_all_read", "ACTION", "Mark all read"),
]:
    split(code, P_NOTIF, "Notifications", ptype, False, reason)

# 3 Home SPLIT
for code, ptype, sens, reason in [
    ("pos.home.profile.view", "SECTION", False, "Cashier profile"),
    ("pos.home.profile.avatar", "FIELD", False, "Avatar"),
    ("pos.home.profile.name", "FIELD", False, "Cashier name"),
    ("pos.home.profile.role", "FIELD", False, "Cashier role"),
    ("pos.home.session_summary.view", "SECTION", False, "Session summary"),
    ("pos.home.session_summary.total_sales", "SENSITIVE_FIELD", True, "Total sales"),
    ("pos.home.session_summary.transaction_count", "FIELD", False, "Transaction count"),
    ("pos.home.session_summary.returns", "FIELD", False, "Returns metric"),
    ("pos.home.session_summary.discounts", "SENSITIVE_FIELD", True, "Discounts"),
    ("pos.home.session_summary.net_sales", "SENSITIVE_FIELD", True, "Net sales"),
]:
    split(code, P_DASH, "Home", ptype, sens, reason)

# 4 Catalog SPLIT
for code, ptype, reason in [
    ("pos.catalog.sections.quick_products", "SECTION", "Quick products"),
    ("pos.catalog.sections.popular", "SECTION", "Popular"),
    ("pos.catalog.sections.frequently_sold", "SECTION", "Frequently sold"),
    ("pos.catalog.sections.offers", "SECTION", "Offers"),
    ("pos.catalog.sections.sort", "CONTROL", "Sort"),
    ("pos.catalog.product_card.image", "FIELD", "Card image"),
    ("pos.catalog.product_card.name", "FIELD", "Card name"),
    ("pos.catalog.product_card.regular_price", "FIELD", "Regular price"),
    ("pos.catalog.product_card.sale_price", "FIELD", "Sale price"),
    ("pos.catalog.product_card.discount_badge", "FIELD", "Discount badge"),
    ("pos.catalog.product_card.open_details", "ACTION", "Open details"),
    ("pos.catalog.product_detail.view", "SCREEN", "Detail view"),
    ("pos.catalog.product_detail.close", "ACTION", "Close detail"),
    ("pos.catalog.product_detail.image", "FIELD", "Detail image"),
    ("pos.catalog.product_detail.name", "FIELD", "Detail name"),
    ("pos.catalog.product_detail.price", "FIELD", "Detail price"),
    ("pos.catalog.product_detail.stock", "FIELD", "Detail stock"),
    ("pos.catalog.product_detail.sku", "FIELD", "Detail SKU"),
    ("pos.catalog.product_detail.description", "FIELD", "Description"),
    ("pos.catalog.product_detail.variants", "SECTION", "Variants"),
    ("pos.catalog.product_detail.variant_select", "CONTROL", "Variant selection"),
    ("pos.catalog.product_detail.available_qty", "FIELD", "Available quantity"),
    ("pos.catalog.product_detail.quantity_display", "FIELD", "Quantity display"),
    ("pos.catalog.product_detail.note_view", "FIELD", "Note view"),
    ("pos.catalog.product_detail.note_entry", "INPUT", "Note entry"),
    ("pos.catalog.product_detail.recommendations", "SECTION", "Recommendations"),
    ("pos.catalog.product_detail.cancel", "ACTION", "Cancel detail"),
]:
    split(code, P_CAT, "Catalog", ptype, False, reason)

# 5 Held sales SPLIT
for code, parent, ptype, sens, reason in [
    ("pos.held_sales.popup.view", P_HELD_C, "CONTAINER", False, "Park popup"),
    ("pos.held_sales.popup.reference", P_HELD_C, "FIELD", False, "Park reference"),
    ("pos.held_sales.popup.note", P_HELD_C, "INPUT", False, "Park note"),
    ("pos.held_sales.popup.expiry", P_HELD_C, "FIELD", False, "Park expiry"),
    ("pos.held_sales.list.filters", P_HELD_V, "CONTROL", False, "Filters"),
    ("pos.held_sales.list.active_count", P_HELD_V, "FIELD", False, "Active count"),
    ("pos.held_sales.list.customer", P_HELD_V, "FIELD", False, "Customer"),
    ("pos.held_sales.list.value", P_HELD_V, "SENSITIVE_FIELD", True, "Value"),
    ("pos.held_sales.list.item_count", P_HELD_V, "FIELD", False, "Item count"),
    ("pos.held_sales.list.parked_time", P_HELD_V, "FIELD", False, "Parked time"),
    ("pos.held_sales.list.expiry_time", P_HELD_V, "FIELD", False, "Expiry time"),
    ("pos.held_sales.list.items", P_HELD_V, "SECTION", False, "Items"),
    ("pos.held_sales.list.pagination", P_HELD_V, "CONTROL", False, "Pagination"),
    ("pos.held_sales.list.summary", P_HELD_V, "SENSITIVE_FIELD", True, "Summary values"),
]:
    split(code, parent, "HeldSales", ptype, sens, reason)

# DOCUMENTED_ONLY resolved: independent cancel
add(
    "pos.sales.held_sales.cancel",
    parent=P_HELD_C,
    module="HeldSales",
    ptype="ACTION",
    sensitive=False,
    kind="documented_resolved",
    reason="Approve independent cancel; historical create-alias insufficient for fine-grained target",
)

# Cart presentation SPLIT (Chunk 1 namespace pos.cart.*) — mutations remain existing cart.*
P_CART = "pos.sales.cart.manage"
for code, ptype, sens, reason in [
    ("pos.cart.summary.view", "SECTION", False, "Cart summary container"),
    ("pos.cart.summary.item_count", "FIELD", False, "Cart item count"),
    ("pos.cart.summary.subtotal", "FIELD", False, "Cart subtotal"),
    ("pos.cart.summary.discount", "SENSITIVE_FIELD", True, "Cart discount"),
    ("pos.cart.summary.tax", "FIELD", False, "Cart tax"),
    ("pos.cart.summary.total", "SENSITIVE_FIELD", True, "Cart total"),
    ("pos.cart.lines.list", "SECTION", False, "Cart lines list"),
    ("pos.cart.lines.name", "FIELD", False, "Line product name"),
    ("pos.cart.lines.quantity", "FIELD", False, "Line quantity display"),
    ("pos.cart.lines.unit_price", "FIELD", False, "Line unit price"),
    ("pos.cart.lines.line_total", "FIELD", False, "Line total"),
    ("pos.cart.lines.note", "FIELD", False, "Line note display"),
    ("pos.cart.lines.image", "FIELD", False, "Line product image"),
]:
    split(code, P_CART, "Cart", ptype, sens, reason)

# Catalog search chrome SPLIT under search parent
P_SEARCH = "pos.sales.catalog.search"
for code, ptype, reason in [
    ("pos.catalog.search.bar", "INPUT", "Search bar"),
    ("pos.catalog.search.clear", "CONTROL", "Clear search"),
    ("pos.catalog.search.results", "SECTION", "Search results"),
    ("pos.catalog.search.empty_state", "MESSAGE", "Empty search state"),
    ("pos.catalog.search.scanner_hint", "MESSAGE", "Scanner hint"),
]:
    split(code, P_SEARCH, "Catalog", ptype, False, reason)

# Payment method picker chrome under checkout (tiles remain independent accept parents for action)
for code, parent, reason in [
    ("pos.checkout.methods.cash_tile", "pos.payments.cash.accept", "Cash method tile chrome"),
    ("pos.checkout.methods.card_tile", "pos.payments.card.accept", "Card method tile chrome"),
    ("pos.checkout.methods.qr_tile", "pos.payments.qr.accept", "QR method tile chrome"),
    ("pos.checkout.methods.split_tile", "pos.payments.split.accept", "Split method tile chrome"),
    ("pos.checkout.methods.container", P_CHK, "Payment methods container"),
]:
    split(code, parent, "Checkout", "CONTAINER" if "container" in code else "CONTROL", False, reason)

# Manual discount presentation SPLIT
P_DISC = "pos.sales.manual_discount.apply"
for code, ptype, sens, reason in [
    ("pos.discount.panel.view", "CONTAINER", False, "Discount panel"),
    ("pos.discount.panel.amount_entry", "INPUT", True, "Discount amount entry"),
    ("pos.discount.panel.reason_entry", "INPUT", False, "Discount reason"),
    ("pos.discount.panel.apply_action", "ACTION", False, "Apply discount control"),
    ("pos.discount.panel.cancel_action", "ACTION", False, "Cancel discount control"),
]:
    split(code, P_DISC, "Discount", ptype, sens, reason)

# New sale chrome SPLIT
P_NS = "pos.sales.new_sale.view"
for code, ptype, reason in [
    ("pos.new_sale.chrome.header", "SECTION", "New sale header"),
    ("pos.new_sale.chrome.park_action", "ACTION", "Park action chrome"),
    ("pos.new_sale.chrome.customer_chip", "FIELD", "Customer chip"),
    ("pos.new_sale.chrome.empty_cart", "MESSAGE", "Empty cart message"),
    ("pos.new_sale.chrome.checkout_action", "ACTION", "Proceed to checkout chrome"),
    ("pos.new_sale.chrome.held_count", "FIELD", "Held sales count badge"),
    ("pos.new_sale.chrome.clear_cart_action", "ACTION", "Clear cart chrome"),
]:
    split(code, P_NS, "NewSale", ptype, False, reason)

# 6 Checkout SPLIT
for code, ptype, sens, reason in [
    ("pos.checkout.summary.payment", "SECTION", False, "Payment summary"),
    ("pos.checkout.summary.items", "SECTION", False, "Items"),
    ("pos.checkout.summary.quantity", "FIELD", False, "Quantity"),
    ("pos.checkout.summary.price", "FIELD", False, "Price"),
    ("pos.checkout.summary.line_total", "FIELD", False, "Line total"),
    ("pos.checkout.customer.summary", "SECTION", False, "Customer summary"),
    ("pos.checkout.summary.subtotal", "FIELD", False, "Subtotal"),
    ("pos.checkout.summary.discount", "SENSITIVE_FIELD", True, "Discount"),
    ("pos.checkout.summary.tax", "FIELD", False, "Tax"),
    ("pos.checkout.summary.total", "SENSITIVE_FIELD", True, "Total"),
]:
    split(code, P_CHK, "Checkout", ptype, sens, reason)

# 8 Cash payment SPLIT
for code, ptype, sens, reason in [
    ("pos.cash_payment.summary.order", "SECTION", False, "Order summary"),
    ("pos.cash_payment.line.item", "FIELD", False, "Item"),
    ("pos.cash_payment.line.quantity", "FIELD", False, "Quantity"),
    ("pos.cash_payment.line.price", "FIELD", False, "Price"),
    ("pos.cash_payment.line.item_total", "FIELD", False, "Item total"),
    ("pos.cash_payment.summary.subtotal", "FIELD", False, "Subtotal"),
    ("pos.cash_payment.summary.discount", "SENSITIVE_FIELD", True, "Discount"),
    ("pos.cash_payment.summary.tax", "FIELD", False, "Tax"),
    ("pos.cash_payment.summary.total_due", "SENSITIVE_FIELD", True, "Total due"),
    ("pos.cash_payment.tender.amount_received_view", "SENSITIVE_FIELD", True, "Amount received display"),
    ("pos.cash_payment.tender.amount_received_entry", "INPUT", True, "Amount received entry"),
    ("pos.cash_payment.tender.due_amount", "SENSITIVE_FIELD", True, "Due amount"),
    ("pos.cash_payment.tender.exact", "ACTION", False, "Exact Cash"),
    ("pos.cash_payment.quick_amounts.container", "CONTAINER", False, "Quick Amount container"),
    ("pos.cash_payment.quick_amounts.slot_1", "CONTROL", False, "Quick Amount slot 1"),
    ("pos.cash_payment.quick_amounts.slot_2", "CONTROL", False, "Quick Amount slot 2"),
    ("pos.cash_payment.quick_amounts.slot_3", "CONTROL", False, "Quick Amount slot 3"),
    ("pos.cash_payment.numpad.container", "CONTAINER", False, "Numpad container"),
    ("pos.cash_payment.numpad.digit_0", "CONTROL", False, "Key 0"),
    ("pos.cash_payment.numpad.digit_1", "CONTROL", False, "Key 1"),
    ("pos.cash_payment.numpad.digit_2", "CONTROL", False, "Key 2"),
    ("pos.cash_payment.numpad.digit_3", "CONTROL", False, "Key 3"),
    ("pos.cash_payment.numpad.digit_4", "CONTROL", False, "Key 4"),
    ("pos.cash_payment.numpad.digit_5", "CONTROL", False, "Key 5"),
    ("pos.cash_payment.numpad.digit_6", "CONTROL", False, "Key 6"),
    ("pos.cash_payment.numpad.digit_7", "CONTROL", False, "Key 7"),
    ("pos.cash_payment.numpad.digit_8", "CONTROL", False, "Key 8"),
    ("pos.cash_payment.numpad.digit_9", "CONTROL", False, "Key 9"),
    ("pos.cash_payment.numpad.digit_00", "CONTROL", False, "Key 00"),
    ("pos.cash_payment.numpad.decimal", "CONTROL", False, "Decimal"),
    ("pos.cash_payment.controls.backspace", "CONTROL", False, "Backspace"),
    ("pos.cash_payment.controls.clear", "CONTROL", False, "Clear"),
    ("pos.cash_payment.tender.change_due", "SENSITIVE_FIELD", True, "Change due"),
    ("pos.cash_payment.completion.execute", "ACTION", False, "Complete sale"),
]:
    split(code, P_CASH, "CashPayment", ptype, sens, reason)

# 9 Sale completion SPLIT
for code, ptype, sens, reason in [
    ("pos.sale_complete.message.success", "MESSAGE", False, "Success message"),
    ("pos.sale_complete.details.receipt_number", "FIELD", False, "Receipt number"),
    ("pos.sale_complete.details.payment_method", "FIELD", False, "Payment method"),
    ("pos.sale_complete.details.datetime", "FIELD", False, "Date/time"),
    ("pos.sale_complete.details.cashier", "FIELD", False, "Cashier"),
    ("pos.sale_complete.details.customer", "SENSITIVE_FIELD", True, "Customer"),
    ("pos.sale_complete.details.cash_received", "SENSITIVE_FIELD", True, "Cash received"),
    ("pos.sale_complete.details.change_due", "SENSITIVE_FIELD", True, "Change due"),
    ("pos.sale_complete.details.total_paid", "SENSITIVE_FIELD", True, "Total paid"),
]:
    split(code, P_RCPT, "SaleComplete", ptype, sens, reason)

# 10 Receipts SPLIT
for code, ptype, sens, reason in [
    ("pos.receipts.details.store", "FIELD", False, "Store"),
    ("pos.receipts.details.receipt_number", "FIELD", False, "Receipt number"),
    ("pos.receipts.details.datetime", "FIELD", False, "Date/time"),
    ("pos.receipts.details.cashier", "FIELD", False, "Cashier"),
    ("pos.receipts.details.customer", "SENSITIVE_FIELD", True, "Customer"),
    ("pos.receipts.details.terminal", "FIELD", False, "Terminal"),
    ("pos.receipts.details.payment_method", "SENSITIVE_FIELD", True, "Payment method"),
    ("pos.receipts.details.items", "SECTION", False, "Items"),
    ("pos.receipts.details.item_quantity", "FIELD", False, "Item quantity"),
    ("pos.receipts.details.item_value", "FIELD", False, "Item value"),
    ("pos.receipts.details.item_rate", "FIELD", False, "Item rate"),
    ("pos.receipts.details.subtotal", "FIELD", False, "Subtotal"),
    ("pos.receipts.details.discount", "SENSITIVE_FIELD", True, "Discount"),
    ("pos.receipts.details.total", "SENSITIVE_FIELD", True, "Total"),
    ("pos.receipts.details.paid_amount", "SENSITIVE_FIELD", True, "Paid amount"),
    ("pos.receipts.details.change_due", "SENSITIVE_FIELD", True, "Change due"),
]:
    split(code, P_RCPT, "Receipts", ptype, sens, reason)

# 11 Customers SPLIT
for code, ptype, sens, reason in [
    ("pos.customers.list.search", "CONTROL", False, "Search"),
    ("pos.customers.list.filters", "CONTROL", False, "Filters"),
    ("pos.customers.list.id", "FIELD", False, "ID"),
    ("pos.customers.list.name", "FIELD", False, "Name"),
    ("pos.customers.list.phone", "SENSITIVE_FIELD", True, "Phone"),
    ("pos.customers.list.email", "SENSITIVE_FIELD", True, "Email"),
    ("pos.customers.list.source", "FIELD", False, "Source"),
    ("pos.customers.list.status", "FIELD", False, "Status"),
    ("pos.customers.list.order_count", "FIELD", False, "Order count"),
    ("pos.customers.list.total_spend", "SENSITIVE_FIELD", True, "Total spend"),
    ("pos.customers.list.pagination", "CONTROL", False, "Pagination"),
    ("pos.customers.details.joined_date", "FIELD", False, "Joined date"),
    ("pos.customers.details.average_order_value", "SENSITIVE_FIELD", True, "AOV"),
    ("pos.customers.history.recent_purchases", "SENSITIVE_FIELD", True, "Recent purchases"),
    ("pos.customers.history.purchase_amounts", "SENSITIVE_FIELD", True, "Purchase amounts"),
    ("pos.customers.history.purchase_history", "SENSITIVE_FIELD", True, "Purchase history"),
]:
    split(code, P_CUST, "Customers", ptype, sens, reason)

# 12 Cash drawer summary SPLIT
for code, ptype, sens, reason in [
    ("pos.cash_drawer.summary.till", "FIELD", False, "Till"),
    ("pos.cash_drawer.summary.status", "STATUS", False, "Status"),
    ("pos.cash_drawer.summary.opening_cash", "SENSITIVE_FIELD", True, "Opening cash"),
    ("pos.cash_drawer.summary.cash_sales", "SENSITIVE_FIELD", True, "Cash sales"),
    ("pos.cash_drawer.summary.expected_cash", "SENSITIVE_FIELD", True, "Expected cash"),
    ("pos.cash_drawer.movements.list", "SECTION", False, "Movement list"),
    ("pos.cash_drawer.movements.type", "FIELD", False, "Movement type"),
    ("pos.cash_drawer.movements.date", "FIELD", False, "Movement date"),
    ("pos.cash_drawer.movements.time", "FIELD", False, "Movement time"),
    ("pos.cash_drawer.movements.cashier", "FIELD", False, "Movement cashier"),
    ("pos.cash_drawer.movements.amount_view", "SENSITIVE_FIELD", True, "Movement amount"),
]:
    split(code, P_CD, "CashDrawer", ptype, sens, reason)

# Open drawer popup SPLIT
for code, ptype, reason in [
    ("pos.cash_drawer.open_popup.view", "CONTAINER", "Open drawer popup"),
    ("pos.cash_drawer.open_popup.cancel", "ACTION", "Cancel"),
    ("pos.cash_drawer.open_popup.continue", "ACTION", "Continue"),
]:
    split(code, P_PHYS, "CashDrawer", ptype, False, reason)

# 14 Movement field SPLIT under new action parents (added below)
MOVEMENT_FIELDS = [
    ("till", "FIELD", False, "Till"),
    ("expected_cash", "SENSITIVE_FIELD", True, "Expected cash"),
    ("available_cash", "SENSITIVE_FIELD", True, "Available/opening cash"),
    ("amount_entry", "INPUT", True, "Amount entry"),
    ("reason", "INPUT", False, "Reason"),
    ("note", "INPUT", False, "Note"),
    ("manager_pin", "INPUT", True, "Manager PIN"),
    ("summary", "SECTION", False, "Summary"),
    ("resulting_balance", "SENSITIVE_FIELD", True, "Resulting balance"),
    ("validation_message", "MESSAGE", False, "Validation"),
    ("confirm", "ACTION", False, "Confirm"),
    ("cancel", "ACTION", False, "Cancel"),
]

# 15 Open till SPLIT
for code, ptype, sens, reason in [
    ("pos.till.opening.starting_cash_view", "SENSITIVE_FIELD", True, "Starting cash view"),
    ("pos.till.opening.starting_cash_entry", "INPUT", True, "Starting cash entry"),
    ("pos.till.opening.validation_message", "MESSAGE", False, "Validation"),
    ("pos.till.opening.note_view", "FIELD", False, "Note view"),
    ("pos.till.opening.note_entry", "INPUT", False, "Note entry"),
    ("pos.till.opening.quick_amounts", "CONTAINER", False, "Quick amounts"),
    ("pos.till.opening.quick_slot_1", "CONTROL", False, "Quick slot 1"),
    ("pos.till.opening.quick_slot_2", "CONTROL", False, "Quick slot 2"),
    ("pos.till.opening.quick_slot_3", "CONTROL", False, "Quick slot 3"),
    ("pos.till.opening.numpad", "CONTAINER", False, "Numpad"),
    ("pos.till.opening.backspace", "CONTROL", False, "Backspace"),
    ("pos.till.opening.clear", "CONTROL", False, "Clear"),
    ("pos.till.opening.confirm_message", "MESSAGE", False, "Confirm message"),
]:
    split(code, P_OPEN, "Till", ptype, sens, reason)

# Open till individual numpad keys (Chunk 2: individual keys if required)
for digit in list("0123456789") + ["00", "decimal"]:
    split(
        f"pos.till.opening.key_{digit}",
        P_OPEN,
        "Till",
        "CONTROL",
        False,
        f"Open till numpad key {digit}",
    )

# 16 Close till SPLIT
for code, ptype, sens, reason in [
    ("pos.till.closing.back", "NAVIGATION", False, "Back"),
    ("pos.till.closing.till", "FIELD", False, "Till"),
    ("pos.till.closing.opened_by", "FIELD", False, "Opened by"),
    ("pos.till.closing.opened_time", "FIELD", False, "Opened time"),
    ("pos.till.closing.expected_cash", "SENSITIVE_FIELD", True, "Expected cash"),
    ("pos.till.closing.counted_cash_entry", "INPUT", True, "Counted cash entry"),
    ("pos.till.closing.difference", "SENSITIVE_FIELD", True, "Difference"),
    ("pos.till.closing.balance_status", "STATUS", False, "Balance status"),
    ("pos.till.closing.mismatch_reason", "INPUT", False, "Mismatch reason"),
    ("pos.till.closing.notes", "INPUT", False, "Notes"),
    ("pos.till.closing.summary", "SECTION", False, "Summary"),
    ("pos.till.closing.expected_cash_summary", "SENSITIVE_FIELD", True, "Expected cash summary"),
    ("pos.till.closing.counted_cash_summary", "SENSITIVE_FIELD", True, "Counted cash summary"),
    ("pos.till.closing.difference_summary", "SENSITIVE_FIELD", True, "Difference summary"),
    ("pos.till.closing.status_summary", "STATUS", False, "Status summary"),
]:
    split(code, P_CLOSE, "Till", ptype, sens, reason)

# ---------- Exactly 14 NEW_REQUIRED ----------
# Evidence-based independent capabilities with no prior leaf:
NEW_ITEMS = [
    ("pos.shell.navigation.settings", P_DASH, "Shell", "NAVIGATION", False, "Settings destination — no existing business permission"),
    ("pos.customers.management.attach_sale", P_CUST, "Customers", "ACTION", False, "Attach customer to active sale"),
    ("pos.customers.management.deactivate", "pos.customers.management.update", "Customers", "ACTION", False, "Deactivate customer"),
    ("pos.cash_drawer.movements.cash_in", P_MOV, "CashDrawer", "ACTION", False, "Independent Cash In action"),
    ("pos.cash_drawer.movements.cash_out", P_MOV, "CashDrawer", "ACTION", False, "Independent Cash Out action"),
    ("pos.cash_drawer.movements.cash_drop", P_MOV, "CashDrawer", "ACTION", False, "Independent Cash Drop action"),
    ("pos.cash_drawer.open_reason.provide_change", P_PHYS, "CashDrawer", "CONTROL", False, "Open reason: provide change"),
    ("pos.cash_drawer.open_reason.till_check", P_PHYS, "CashDrawer", "CONTROL", False, "Open reason: till check"),
    ("pos.cash_drawer.open_reason.cash_count", P_PHYS, "CashDrawer", "CONTROL", False, "Open reason: cash count"),
    ("pos.cash_drawer.open_reason.manager_operation", P_PHYS, "CashDrawer", "CONTROL", False, "Open reason: manager operation"),
    ("pos.cash_drawer.open_reason.other", P_PHYS, "CashDrawer", "CONTROL", False, "Open reason: other"),
    ("pos.shell.navigation.offline_banner", P_DASH, "Shell", "MESSAGE", False, "Offline/connectivity banner surface"),
    ("pos.home.actions.online_orders_entry", "commerce.online_order.orders.access", "Home", "NAVIGATION", False, "Home Online Orders entry chrome under commerce access"),
    ("pos.home.actions.returns_entry", "pos.returns.search_sale.view", "Home", "NAVIGATION", False, "Home Returns entry chrome under returns view"),
]
for code, parent, module, ptype, sens, reason in NEW_ITEMS:
    new(code, parent, module, ptype, sens, reason)

# Movement field children under NEW action parents
for movement, parent in [
    ("cash_in", "pos.cash_drawer.movements.cash_in"),
    ("cash_out", "pos.cash_drawer.movements.cash_out"),
    ("cash_drop", "pos.cash_drawer.movements.cash_drop"),
]:
    for suffix, ptype, sens, reason in MOVEMENT_FIELDS:
        split(
            f"pos.cash_movements.{movement}.{suffix}",
            parent,
            "CashMovements",
            ptype,
            sens,
            f"{movement} {reason}",
        )

# Deduplicate
seen: set[str] = set()
unique: list[dict] = []
for d in defs:
    if d["code"] in seen:
        raise SystemExit(f"duplicate code: {d['code']}")
    seen.add(d["code"])
    unique.append(d)
defs = unique

# Parent integrity: every parent must exist as a code in catalog (or be None)
codes = {d["code"] for d in defs}
for d in defs:
    if d["parent"] and d["parent"] not in codes:
        raise SystemExit(f"unknown parent {d['parent']} for {d['code']}")

# Cycle check
def has_cycle() -> bool:
    graph = {d["code"]: d["parent"] for d in defs if d["parent"]}
    for start in graph:
        seen_path = set()
        cur = start
        while cur in graph:
            if cur in seen_path:
                return True
            seen_path.add(cur)
            cur = graph[cur]
    return False


if has_cycle():
    raise SystemExit("cycle detected")

by_kind = {}
for d in defs:
    by_kind[d["kind"]] = by_kind.get(d["kind"], 0) + 1

print("KIND COUNTS:", by_kind)
print("TOTAL:", len(defs))
print("ROLE ASSIGNABLE:", sum(1 for d in defs if d["roleAssignable"]))
print("SENSITIVE:", sum(1 for d in defs if d["sensitive"]))

# Pad or trim SPLIT to 280 if close — report honest count; do not invent filler.
split_n = by_kind.get("split", 0)
new_n = by_kind.get("new", 0)
print(f"TARGET new=14 got={new_n}; TARGET split=280 got={split_n}")

TYPE_MAP = {
    "SCREEN": "Screen",
    "SECTION": "Section",
    "ACTION": "Action",
    "FIELD": "Field",
    "SENSITIVE_FIELD": "SensitiveField",
    "NAVIGATION": "Navigation",
    "CONTAINER": "Container",
    "MESSAGE": "Message",
    "INPUT": "Input",
    "CONTROL": "Control",
    "STATUS": "Status",
    "PRE_AUTH_CONFIGURATION": "PreAuthConfiguration",
}
KIND_MAP = {
    "existing": "Existing",
    "new": "New",
    "split": "Split",
    "documented_resolved": "DocumentedResolved",
    "pre_auth": "PreAuth",
}

BACKEND_CATALOG.mkdir(parents=True, exist_ok=True)
FLUTTER_CATALOG.mkdir(parents=True, exist_ok=True)
TOOL_DIR.mkdir(parents=True, exist_ok=True)

catalog_json = {
    "chunk": 2,
    "taxonomy": "domain.module.feature.action",
    "deviceScope": "all",
    "kindCounts": by_kind,
    "definitions": defs,
}
(BACKEND_CATALOG / "cashier_pos_canonical_permissions.chunk2.json").write_text(
    json.dumps(catalog_json, indent=2), encoding="utf-8"
)

cs = [
    "// <auto-generated by tools/generate_cashier_pos_chunk2_permissions.py>",
    "// Chunk 2: canonical permission definitions only. No seeding/runtime enforcement.",
    "#nullable enable",
    "namespace E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos;",
    "",
    "public enum CashierPosPermissionSemanticType",
    "{",
    "    Screen,",
    "    Section,",
    "    Action,",
    "    Field,",
    "    SensitiveField,",
    "    Navigation,",
    "    Container,",
    "    Message,",
    "    Input,",
    "    Control,",
    "    Status,",
    "    PreAuthConfiguration,",
    "}",
    "",
    "public enum CashierPosPermissionDefinitionKind",
    "{",
    "    Existing,",
    "    New,",
    "    Split,",
    "    DocumentedResolved,",
    "    PreAuth,",
    "}",
    "",
    "public sealed record CashierPosPermissionDefinition(",
    "    string Code,",
    "    string? ParentCode,",
    "    string Module,",
    "    CashierPosPermissionSemanticType SemanticType,",
    "    bool IsSensitive,",
    "    CashierPosPermissionDefinitionKind Kind,",
    "    string Reason,",
    "    bool IsRoleAssignable);",
    "",
    "public static class CashierPosCanonicalPermissionCatalog",
    "{",
    "    public static IReadOnlyList<CashierPosPermissionDefinition> All { get; } =",
    "    [",
]
for d in defs:
    parent = "null" if d["parent"] is None else f"\"{d['parent']}\""
    reason = d["reason"].replace("\\", "\\\\").replace('"', '\\"')
    cs.append(
        f'        new("{d["code"]}", {parent}, "{d["module"]}", '
        f'CashierPosPermissionSemanticType.{TYPE_MAP[d["type"]]}, '
        f'{"true" if d["sensitive"] else "false"}, '
        f'CashierPosPermissionDefinitionKind.{KIND_MAP[d["kind"]]}, '
        f'"{reason}", {"true" if d["roleAssignable"] else "false"}),'
    )
cs += [
    "    ];",
    "",
    "    public static IEnumerable<CashierPosPermissionDefinition> RoleAssignable =>",
    "        All.Where(d => d.IsRoleAssignable);",
    "",
    "    public static IEnumerable<CashierPosPermissionDefinition> FineGrained =>",
    "        All.Where(d => d.Kind is CashierPosPermissionDefinitionKind.New",
    "            or CashierPosPermissionDefinitionKind.Split",
    "            or CashierPosPermissionDefinitionKind.DocumentedResolved);",
    "}",
    "",
]
(BACKEND_CATALOG / "CashierPosCanonicalPermissionCatalog.cs").write_text(
    "\n".join(cs), encoding="utf-8"
)

const_lines = [
    "// <auto-generated — Chunk 2 fine-grained + pre-auth codes>",
    "namespace E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos;",
    "",
    "public static class CashierPosFineGrainedPermissions",
    "{",
]
for d in defs:
    if d["kind"] not in ("new", "split", "documented_resolved", "pre_auth"):
        continue
    ident = "".join(p[:1].upper() + p[1:] for p in d["code"].replace(".", "_").split("_") if p)
    const_lines.append(f'    public const string {ident} = "{d["code"]}";')
const_lines += ["}", ""]
(BACKEND_CATALOG / "CashierPosFineGrainedPermissions.cs").write_text(
    "\n".join(const_lines), encoding="utf-8"
)

dart = [
    "// Generated Chunk 2 Cashier POS canonical permission registry.",
    "// Definitions only — no UI gating in Chunk 2.",
    "class CashierPosCanonicalPermissionCodes {",
    "  const CashierPosCanonicalPermissionCodes._();",
    "",
    "  static const roleAssignableCodes = <String>[",
]
for d in defs:
    if d["roleAssignable"]:
        dart.append(f"    '{d['code']}',")
dart += [
    "  ];",
    "",
    "  static const preAuthCodes = <String>[",
]
for d in defs:
    if d["kind"] == "pre_auth":
        dart.append(f"    '{d['code']}',")
dart += [
    "  ];",
    "",
    "  static const sensitiveCodes = <String>[",
]
for d in defs:
    if d["sensitive"] and d["roleAssignable"]:
        dart.append(f"    '{d['code']}',")
dart += [
    "  ];",
    "",
    "  static const parentByChild = <String, String>{",
]
for d in defs:
    if d["parent"]:
        dart.append(f"    '{d['code']}': '{d['parent']}',")
dart += [
    "  };",
    "",
    "  static const newCodes = <String>[",
]
for d in defs:
    if d["kind"] == "new":
        dart.append(f"    '{d['code']}',")
dart += [
    "  ];",
    "",
    "  static const splitCodes = <String>[",
]
for d in defs:
    if d["kind"] == "split":
        dart.append(f"    '{d['code']}',")
dart += [
    "  ];",
    "",
    "  static const documentedResolvedCodes = <String>[",
]
for d in defs:
    if d["kind"] == "documented_resolved":
        dart.append(f"    '{d['code']}',")
dart += [
    "  ];",
    "}",
    "",
]
(FLUTTER_CATALOG / "cashier_pos_canonical_permission_codes.dart").write_text(
    "\n".join(dart), encoding="utf-8"
)

print("Wrote catalog files.")
