# Tenant Admin Reports Field-to-Table Mapping

Phase 1 verification only. No production code, schema, permission, or migration changes are included.

## Scope

- Backend repo inspected: `Unified-Commerce`.
- Frontend report contract inspected read-only from `Tenantadmin/Nytroz-POS-App`.
- Second Brain and DB Design folders were not present inside the writable workspace, so this mapping is based on the current backend model plus the Flutter report contract.

## Contract Sources

- Frontend paths expected:
  - `GET /api/v1/tenant-admin/reports/filter-options`
  - `GET /api/v1/tenant-admin/reports/dashboard`
  - `GET /api/v1/tenant-admin/reports/sales`
  - `GET /api/v1/tenant-admin/reports/sales/{orderId}`
  - `GET /api/v1/tenant-admin/reports/stock`
  - `GET /api/v1/tenant-admin/reports/outlets`
  - `POST /api/v1/tenant-admin/reports/exports`
  - `GET /api/v1/tenant-admin/reports/exports/{jobId}`
- Backend now exposes the `tenant-admin/reports` controller and generic report response DTOs.

## Common Envelope Fields

| Frontend field | Backend source | Status |
|---|---|---|
| `section` | Request `section` query value or resolved default | Available |
| `metrics[]` | Aggregated per report section | Needs implementation |
| `sections[]` | Aggregated chart/card/detail sections | Needs implementation |
| `records[]` / `items[]` / `rows[]` | Section-specific rows | Needs implementation |
| `pagination` | Query paging result | Needs implementation |
| `currencyCode` | `sales_orders.currency_code`, `tills.currency_code`, tenant default fallback | Partially available |
| `generatedAt` | Server clock | Available |

## Sales Reports

| Frontend field | Primary table/entity | Notes |
|---|---|---|
| `completedAt` | `sales_orders.completed_at` | Use completed orders for sales reporting. |
| `orderNumber` | `sales_orders.order_number` | Direct. |
| `salesChannelName` | `sales_orders.sales_channel_id` + sales channel table if available | ID exists; name table must be verified during implementation. |
| `outletName` | `outlets.outlet_name` | Gap: `sales_orders` has `fulfillment_method_outlet_id`, `till_id`, and returns have `outlet_id`; no explicit reporting outlet snapshot. |
| `tillName` | `sales_orders.till_id` -> `tills.till_name` | Available for POS orders. |
| `cashierName` | `sales_orders.created_by_tenant_user_id` or `till_session_summaries.cashier_tenant_user_id` -> `tenant_users` | Available, but display-name field must be verified. |
| `customerName` | `sales_orders.customer_name_snapshot` | Sensitive. |
| `lineCount` | Count `sales_order_lines` | Direct. |
| `totalQuantity` | Sum `sales_order_lines.quantity` | Direct. |
| `subtotalAmount` | `sales_orders.subtotal_amount` | Direct. |
| `discountAmount` | `sales_orders.discount_amount` | Direct. |
| `taxAmount` | `sales_orders.tax_amount` | Direct. |
| `chargeAmount` | `sales_orders.charge_amount` | Direct. |
| `roundingAmount` | `sales_orders.rounding_amount` | Direct. |
| `totalAmount` | `sales_orders.total_amount` | Direct. |
| `paidAmount` | `sales_orders.paid_amount` or sum `sales_payments.paid_amount` | Prefer order header for transaction list; payments for method breakdown. |
| `refundedAmount` | `sales_orders.refunded_amount` or `sales_refunds.refunded_amount` | Prefer order header for transaction list. |
| `netAmount` | Calculated from order totals | See calculation definitions. |
| `paymentMethodNames` | `sales_payments.payment_method_id` -> `payment_methods.method_name` | Available. |
| `paymentStatus` | `sales_orders.payment_status` | Direct. |
| `fulfilmentStatus` | `sales_orders.fulfillment_status` | Contract spelling uses `fulfilmentStatus`; backend field is `FulfillmentStatus`. |
| `orderStatus` | `sales_orders.status` | Direct. |

## Product and Category Sales

| Frontend field | Primary table/entity | Notes |
|---|---|---|
| `productName` | `sales_order_lines.product_name_snapshot` | Direct snapshot. |
| `variantName` | `sales_order_lines.variant_name_snapshot` | Direct snapshot. |
| `sku` | `sales_order_lines.sku_snapshot` | Direct snapshot. |
| `barcode` | `product_barcodes.barcode` | Gap: order line has no barcode snapshot; historical report may change if barcode changes. |
| `brandName` | `products.brand_id` -> `brands.brand_name` | Gap: order line has no brand snapshot. |
| `departmentName` | `product_categories.category_id` -> `categories.department_id` -> `departments.department_name` | Gap: order line has no category snapshot. |
| `categoryName` | `product_categories.category_id` -> `categories.category_name` | Gap: current category only. |
| `subcategoryName` | `categories.parent_category_id` relationship | Gap: naming hierarchy needs implementation decision. |
| `quantitySold` | Sum `sales_order_lines.quantity` | Exclude non-completed/cancelled lines. |
| `quantityReturned` | `sales_order_lines.returned_quantity` or `sales_return_lines.quantity_received` | Prefer return lines for return section; order line header for product sales. |
| `netQuantity` | Calculated | `quantitySold - quantityReturned`. |
| `grossSalesAmount` | Sum `sales_order_lines.line_subtotal_amount` before refunds | Available. |
| `discountAmount` | Sum `sales_order_lines.line_discount_amount` | Available. |
| `taxAmount` | Sum `sales_order_lines.line_tax_amount` or `sales_order_taxes` | Available. |
| `refundAmount` | `sales_refund_lines.amount` joined through return lines/order lines | Available but allocation by product requires join. |
| `netSalesAmount` | Calculated | See calculation definitions. |
| `transactionCount` | Count distinct `sales_orders.id` | Available. |
| `averageSellingPrice` | Calculated | `netSalesAmount / netQuantity` when non-zero. |
| `percentageOfTotal` | Calculated | Row net sales divided by report net sales. |

## Payment Reports

| Frontend field | Primary table/entity | Notes |
|---|---|---|
| `paymentMethodName` | `payment_methods.method_name` | Direct. |
| `paymentType` | `payment_methods.method_type` | Direct. |
| `transactionCount` | Count `sales_payments` or `sales_payment_transactions` | Use completed payment rows unless gateway transaction detail required. |
| `requestedAmount` | Sum `sales_payments.requested_amount` | Direct. |
| `tenderedAmount` | Sum `sales_payments.tendered_amount` | Direct. |
| `paidAmount` | Sum `sales_payments.paid_amount` | Direct. |
| `changeAmount` | Sum `sales_payments.change_amount` | Direct. |
| `refundedAmount` | Sum `sales_payments.refunded_amount` | Direct. |
| `netCollectedAmount` | Calculated | `paidAmount - refundedAmount - changeAmount`. |
| `percentage` | Calculated | Method net collected divided by total net collected. |

## Tax Reports

| Frontend field | Primary table/entity | Notes |
|---|---|---|
| `taxClassName` | `sales_order_taxes.tax_class_code_snapshot` or tax master | Snapshot code exists; display name may need tax master join. |
| `taxName` | `sales_order_taxes.tax_name_snapshot` | Direct. |
| `taxCode` | `sales_order_taxes.tax_rate_code_snapshot` | Direct. |
| `taxRate` | `sales_order_taxes.tax_rate_percent` | Direct. |
| `taxableAmount` | Sum `sales_order_taxes.taxable_amount` | Direct. |
| `taxAmount` | Sum `sales_order_taxes.tax_amount` | Direct. |
| `refundedTaxAmount` | `sales_refund_lines` tax allocation | Gap: no explicit tax refund allocation found. |
| `netTaxAmount` | Calculated | `taxAmount - refundedTaxAmount`. |
| `transactionCount` | Count distinct `sales_order_taxes.sales_order_id` | Available. |

## Discount Reports

| Frontend field | Primary table/entity | Notes |
|---|---|---|
| `discountName` | `sales_order_discounts.discount_name_snapshot` | Direct. |
| `discountCode` | `sales_order_discounts.discount_code_snapshot` | Direct. |
| `discountType` | `sales_order_discounts.discount_type_id` or snapshot | ID exists; label join required. |
| `discountScope` | `sales_order_discounts.discount_target_scope` | Direct. |
| `usageCount` | Count `sales_order_discounts` | Available. |
| `discountAmount` | Sum `sales_order_discounts.discount_amount` | Direct. |
| `averageDiscountAmount` | Calculated | `discountAmount / usageCount`. |
| `manualDiscountCount` | `manual_discount_reason` non-null or discount type | Available heuristic. |
| `managerApprovalCount` | Approval relationship | Gap: no explicit approval linkage found. |
| `netSalesAfterDiscount` | Calculated from related orders | Available. |

## Returns and Refunds

| Frontend field | Primary table/entity | Notes |
|---|---|---|
| `returnNumber` | `sales_returns.return_number` | Direct. |
| `originalOrderNumber` | `sales_returns.sales_order_id` -> `sales_orders.order_number` | Available. |
| `requestedAt` | `sales_returns.requested_at` | Direct. |
| `outletName` | `sales_returns.outlet_id` -> `outlets.outlet_name` | Processing outlet. |
| `customerName` | `sales_orders.customer_name_snapshot` or customer table | Sensitive. |
| `returnChannel` | `sales_returns.return_channel` | Direct. |
| `returnReasonName` | `sales_returns.return_reason_id` / line reason | Join required. |
| `requestedQuantity` | `sales_returns.total_requested_qty` or sum lines | Direct. |
| `receivedQuantity` | `sales_returns.total_received_qty` or sum lines | Direct. |
| `approvedQuantity` | Return-line approved quantity | Gap: no explicit approved quantity field found. |
| `requestedAmount` | Sum `sales_return_lines.line_subtotal_amount + line_tax_amount` | Available. |
| `approvedAmount` | `sales_refunds.approved_amount` | Available. |
| `refundedAmount` | `sales_refunds.refunded_amount` | Available. |
| `returnStatus` | `sales_returns.return_status` | Direct. |
| `refundStatus` | `sales_refunds.refund_status` | Direct. |
| `completedAt` | `sales_returns.completed_at` or `sales_refunds.completed_at` | Prefer return completed date for return report. |

## Cashier, Daily, Outlet, and Till Reports

| Frontend field | Primary table/entity | Notes |
|---|---|---|
| `cashierName` | `tenant_users` via `created_by_tenant_user_id` or `till_session_summaries.cashier_tenant_user_id` | Available. |
| `businessDate` | Derived from tenant timezone | Gap: no persisted business date found on `sales_orders` or till summaries. |
| `grossSalesAmount` | `sales_orders.subtotal_amount` or `till_session_summaries.gross_sales_amount` | Choose source by section. |
| `discountAmount` | `sales_orders.discount_amount` or `till_session_summaries.discount_amount` | Available. |
| `refundAmount` | `sales_orders.refunded_amount` or `till_session_summaries.refund_amount` | Available. |
| `taxAmount` | `sales_orders.tax_amount` or `till_session_summaries.tax_amount` | Available. |
| `netSalesAmount` | Calculated or `till_session_summaries.net_sales_amount` | Available. |
| `totalCollectedAmount` | Sum completed payments net of refunds/change | Available. |
| `transactionCount` | Count orders or `till_session_summaries.order_count` | Available. |
| `averageOrderValue` | Calculated | `netSalesAmount / transactionCount`. |
| `averageItemsPerOrder` | Calculated from order lines | Available. |
| `voidCount` | `till_session_summaries.void_count` | Available for session summaries. |
| `returnCount` | `till_session_summaries.refund_count` or returns count | Available. |
| `cashDifference` | `till_session_summaries.cash_difference_amount` | Direct. |
| `sessionNumber` | `till_sessions` | Field name must be verified. |
| `openedAt` | `till_session_summaries.session_opened_at` | Direct. |
| `closedAt` | `till_session_summaries.session_closed_at` | Direct. |
| `openingCashAmount` | `till_session_summaries.opening_cash_amount` | Direct. |
| `cashInAmount` | `till_cash_movements` where movement type is cash-in | Available. |
| `cashDropAmount` | `till_cash_movements` where movement type is cash drop | Available. |
| `expectedCashAmount` | `till_session_summaries.expected_cash_amount` | Direct. |
| `countedCashAmount` | `till_session_summaries.counted_cash_amount` | Direct. |
| `sessionStatus` | `till_session_summaries.summary_status` or session status | Needs source decision. |
| `approvalStatus` | `till_session_summaries.approved_at/approved_by_tenant_user_id` | Derived. |

## Stock Reports

| Frontend field | Primary table/entity | Notes |
|---|---|---|
| `outletName` | `inventory_locations.outlet_id` -> `outlets.outlet_name` | Available. |
| `inventoryLocationName` | `inventory_locations` | Name column must be verified during implementation. |
| `productName` | `products.product_name` | Current master data. |
| `variantName` | `product_variants.variant_name` | Current master data. |
| `sku` | `product_variants.sku` | Current master data. |
| `barcode` | `product_barcodes.barcode` | Current master data. |
| `batchNumber` | `product_batches.batch_number` | Available. |
| `manufacturedDate` | `product_batches.manufactured_at` | Available. |
| `firstReceivedAt` | `product_batches.first_received_at` | Available. |
| `expiryDate` | `product_batches.expiry_date` | Available. |
| `daysUntilExpiry` | Calculated from tenant-local date | Available. |
| `onHandQuantity` | `inventory_balances.on_hand_quantity` | Direct. |
| `reservedQuantity` | `inventory_balances.reserved_quantity` | Direct. |
| `damagedQuantity` | `inventory_balances.damaged_quantity` | Direct. |
| `quarantineQuantity` | `inventory_balances.quarantine_quantity` | Direct. |
| `availableQuantity` | `inventory_balances.available_quantity` | Computed column: on-hand minus reserved/damaged/quarantine. |
| `reorderPoint` | `inventory_reorder_rules.reorder_point_quantity` or `min_stock_quantity` | Available. |
| `reorderQuantity` | `inventory_reorder_rules.reorder_quantity` | Available. |
| `safetyStock` | `inventory_reorder_rules.min_stock_quantity` | Available if interpreted as safety stock. |
| `shortageQuantity` | Calculated | `max(reorderPoint - availableQuantity, 0)`. |
| `unitCost` | `inventory_cost_layers.unit_cost` | Sensitive. |
| `stockValue` | Cost-layer sum | Sensitive. |
| `stockStatus` / `status` | Calculated | Out of stock, low stock, in stock. |
| `expiryStatus` | Calculated from `expiry_date` | Available. |
| `lastMovementAt` | Max `stock_movements.occurred_at` | Available. |
| `lastInStockAt` | Derived from stock movements | Gap: not stored directly. |
| `batchStatus` | `product_batches.status` | Direct. |
| `occurredAt` | `stock_movements.occurred_at` | Direct. |
| `movementNumber` | `stock_movements.movement_number` | Direct. |
| `movementType` | `stock_movements.movement_type` | Direct. |
| `quantityBefore` | `stock_movements.quantity_before` | Direct. |
| `quantityChanged` | `stock_movements.quantity_change` | Contract key uses `quantityChanged`; entity uses `QuantityChange`. |
| `quantityAfter` | `stock_movements.quantity_after` | Direct. |
| `totalCost` | `stock_movements.total_cost` | Sensitive. |
| `referenceType` | `stock_movement_references.reference_type` | Available. |
| `referenceNumber` | Reference entity/table based on type | Gap: reference table stores IDs, not display number. |
| `reason` | `stock_adjustment_reasons` or `stock_movements.movement_note` | Gap: movement has note, not normalized reason on all movement types. |
| `notes` | `stock_movements.movement_note` | Direct. |
| `performedByName` | `stock_movements.created_by_tenant_user_id` -> `tenant_users` | Available. |
| `costingMethod` | Product/inventory setting | Gap: source must be verified. |
| `remainingCostLayerQuantity` | Sum `inventory_cost_layers.remaining_quantity` | Available. |
| `averageUnitCost` | Calculated from remaining cost layers | Available. |
| `totalInventoryValue` | Sum `remaining_quantity * unit_cost` | Available. |

## Filter Options

| Group | Backend source |
|---|---|
| `outlets` | `outlets` filtered by tenant and authorized outlet scope |
| `tills` | `tills` joined to authorized outlets |
| `cashiers` | `tenant_users` with POS/cashier roles or created-by usage |
| `customers` | `customers` and sales snapshots; sensitive |
| `departments` | `departments` |
| `categories` / `subcategories` | `categories` hierarchy |
| `brands` | `brands` |
| `products` | `products` |
| `productVariants` | `product_variants` |
| `salesChannels` | Sales channel source must be verified |
| `paymentMethods` | `payment_methods` |
| `orderStatuses` | Sales order constants/status enum |
| `paymentStatuses` | Sales payment/order payment status constants |
| `stockStatuses` | Calculated status options |
| `expiryStatuses` | Calculated expiry status options |
| `movementTypes` | Stock movement constants/status enum |

## Mapping Gaps That Block Exact Reporting

- `sales_orders` does not expose a persisted tenant-local `business_date`.
- `TenantAdminContextDto` does not expose tenant timezone/currency/locale/business date.
- Product/category/brand/barcode sales reports require current catalog joins because order lines do not snapshot those labels.
- Tax refund allocation is not explicit.
- Discount manager approval count is not explicit.
- Stock movement reference number is not stored directly in `stock_movement_references`.
- Outlet scope filtering must be enforced consistently; current tenant context returns tenant outlets, not clearly user-assigned outlets.
