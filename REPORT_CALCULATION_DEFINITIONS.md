# Tenant Admin Reports Calculation Definitions

Phase 1 verification only. These are backend implementation definitions, not production code.

## Report Time Window

- Inputs: `from` and `to` are frontend `yyyy-MM-dd` dates.
- Required implementation rule: resolve `from` and `to` in tenant timezone, then convert to UTC/database instants.
- Current backend gap: tenant context does not expose timezone/business date; some existing endpoints default to `DateTime.UtcNow`.
- Recommended inclusive range:
  - start: tenant-local `from` at `00:00:00`
  - end: tenant-local day after `to` at `00:00:00`, exclusive

## Common Filters

- Always filter by `TenantRequestContext.TenantId`.
- Never accept `tenantId` from frontend query.
- Filter by authorized outlets before applying `outletId`, `tillId`, cashier, product, status, and search filters.
- Include only active/non-deleted master data unless reporting historical sales snapshots.

## Sales Metrics

| Metric | Definition |
|---|---|
| Gross sales | Sum `sales_orders.subtotal_amount` for completed sales orders in period. |
| Discounts | Sum `sales_orders.discount_amount`. |
| Tax | Sum `sales_orders.tax_amount`. |
| Charges | Sum `sales_orders.charge_amount`. |
| Rounding | Sum `sales_orders.rounding_amount`. |
| Total sales | Sum `sales_orders.total_amount`. |
| Paid amount | Sum `sales_orders.paid_amount`. |
| Refund amount | Sum `sales_orders.refunded_amount`, or refund table sum for return/refund reports. |
| Net sales | `totalAmount - refundedAmount`; if section requires pre-tax net, use `subtotalAmount - discountAmount - refundAmount`. |
| Transaction count | Count distinct completed `sales_orders.id`. |
| Average order value | `netSalesAmount / transactionCount`, zero when count is zero. |
| Total quantity | Sum `sales_order_lines.quantity` for included orders. |
| Net quantity | `quantitySold - quantityReturned`. |

## Payment Metrics

| Metric | Definition |
|---|---|
| Requested amount | Sum `sales_payments.requested_amount`. |
| Tendered amount | Sum `sales_payments.tendered_amount`. |
| Paid amount | Sum `sales_payments.paid_amount`. |
| Change amount | Sum `sales_payments.change_amount`. |
| Refunded amount | Sum `sales_payments.refunded_amount`. |
| Net collected | `paidAmount - refundedAmount - changeAmount`. |
| Payment percentage | Row `netCollectedAmount / totalNetCollectedAmount * 100`. |
| Payment transaction count | Count payment rows or payment transactions with completed/succeeded status. |

## Product and Category Metrics

| Metric | Definition |
|---|---|
| Quantity sold | Sum line `quantity`. |
| Quantity returned | Sum line `returned_quantity` or matching return lines. |
| Gross product sales | Sum line `line_subtotal_amount`. |
| Product discount | Sum line `line_discount_amount`. |
| Product tax | Sum line `line_tax_amount`. |
| Product refunds | Sum matching `sales_refund_lines.amount`. |
| Product net sales | `grossSalesAmount - discountAmount - refundAmount + taxAmount` when tax-inclusive reporting is required; otherwise `grossSalesAmount - discountAmount - refundAmount`. |
| Average selling price | `netSalesAmount / netQuantity`, zero when quantity is zero. |
| Percentage of total | Row `netSalesAmount / totalReportNetSalesAmount * 100`. |

## Tax Metrics

| Metric | Definition |
|---|---|
| Taxable amount | Sum `sales_order_taxes.taxable_amount`. |
| Output tax | Sum `sales_order_taxes.tax_amount`. |
| Refunded tax | Requires allocation from refunds; current explicit allocation not found. |
| Net tax | `taxAmount - refundedTaxAmount`. |
| Tax transaction count | Count distinct `sales_order_taxes.sales_order_id`. |

## Discount Metrics

| Metric | Definition |
|---|---|
| Discount amount | Sum `sales_order_discounts.discount_amount`. |
| Usage count | Count discount rows or distinct order-discount applications. |
| Average discount | `discountAmount / usageCount`, zero when count is zero. |
| Manual discount count | Count rows with `manual_discount_reason` or manual discount type. |
| Manager approval count | Not reliably calculable with current verified fields. |
| Net sales after discount | Sum related order `total_amount` after discount, net of refunds if report requires final net. |

## Return and Refund Metrics

| Metric | Definition |
|---|---|
| Requested quantity | Sum `sales_return_lines.quantity_requested` or return header total. |
| Received quantity | Sum `sales_return_lines.quantity_received` or return header total. |
| Requested amount | Sum `sales_return_lines.line_subtotal_amount + line_tax_amount`. |
| Approved amount | Sum `sales_refunds.approved_amount`. |
| Refunded amount | Sum `sales_refunds.refunded_amount`. |
| Return count | Count `sales_returns.id`. |
| Refund count | Count `sales_refunds.id`. |

## Stock Metrics

| Metric | Definition |
|---|---|
| Available quantity | `on_hand_quantity - reserved_quantity - damaged_quantity - quarantine_quantity`; already computed as `inventory_balances.available_quantity`. |
| Out of stock | `availableQuantity <= 0`. |
| Low stock | `availableQuantity > 0 && availableQuantity <= threshold`. |
| Threshold | `inventory_reorder_rules.min_stock_quantity ?? reorder_point_quantity`; fallback to product dashboard default threshold. |
| Shortage quantity | `max(threshold - availableQuantity, 0)`. |
| Expired | `product_batches.expiry_date < tenantLocalToday`. |
| Expiring soon | `expiry_date <= tenantLocalToday + configured alert window`. |
| Stock value | Sum remaining cost layers: `remaining_quantity * unit_cost`. |
| Average unit cost | `totalInventoryValue / remainingCostLayerQuantity`, zero when quantity is zero. |
| Last movement | Max `stock_movements.occurred_at` for balance/product/outlet scope. |

## Outlet and Till Metrics

| Metric | Definition |
|---|---|
| Outlet gross sales | Sum orders mapped to outlet, or till summaries by outlet. |
| Outlet net sales | Sum order net or till summary `net_sales_amount`. |
| Outlet stock value | Sum inventory valuation for outlet locations. |
| Till opening cash | `till_session_summaries.opening_cash_amount`. |
| Till cash in | Sum `till_cash_movements.amount` where movement type is cash-in. |
| Till cash drop | Sum `till_cash_movements.amount` where movement type is cash drop/drop-out. |
| Till expected cash | `till_session_summaries.expected_cash_amount`. |
| Till counted cash | `till_session_summaries.counted_cash_amount`. |
| Till cash difference | `till_session_summaries.cash_difference_amount`. |
| Approval status | Approved when `approved_at` is not null; otherwise pending/unapproved. |

## Comparison and Trend

- Percentage change: `(current - previous) / previous * 100`.
- If previous is zero:
  - current zero: `0`
  - current non-zero: `100`
- Trend direction:
  - positive: current greater than previous
  - negative: current less than previous
  - neutral: equal or unavailable

## Rounding

- Financial values should preserve database precision and only format at response/display level.
- Percentage values should round to one or two decimal places consistently.
