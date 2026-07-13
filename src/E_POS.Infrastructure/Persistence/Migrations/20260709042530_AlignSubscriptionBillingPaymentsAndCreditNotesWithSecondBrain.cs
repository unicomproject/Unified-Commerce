using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignSubscriptionBillingPaymentsAndCreditNotesWithSecondBrain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "currency_code",
                table: "subscription_payment_transactions",
                type: "char(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "failed_at",
                table: "subscription_payment_transactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "failure_reason",
                table: "subscription_payment_transactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "idempotency_key",
                table: "subscription_payment_transactions",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "invoice_id",
                table: "subscription_payment_transactions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "net_amount",
                table: "subscription_payment_transactions",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "paid_at",
                table: "subscription_payment_transactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "payment_link_id",
                table: "subscription_payment_transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "provider_fee",
                table: "subscription_payment_transactions",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "provider_name",
                table: "subscription_payment_transactions",
                type: "varchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "provider_response_json",
                table: "subscription_payment_transactions",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_transaction_id",
                table: "subscription_payment_transactions",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "subscription_payment_transactions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "transaction_status",
                table: "subscription_payment_transactions",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "transaction_type",
                table: "subscription_payment_transactions",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_platform_user_id",
                table: "subscription_payment_links",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "invoice_id",
                table: "subscription_payment_links",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_reminder_at",
                table: "subscription_payment_links",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "link_status",
                table: "subscription_payment_links",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "payment_url",
                table: "subscription_payment_links",
                type: "varchar(700)",
                maxLength: 700,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "provider_name",
                table: "subscription_payment_links",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_payment_link_id",
                table: "subscription_payment_links",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "reminder_count",
                table: "subscription_payment_links",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "revoked_at",
                table: "subscription_payment_links",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "sent_at",
                table: "subscription_payment_links",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sent_to_email",
                table: "subscription_payment_links",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "subscription_payment_links",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "token_hash",
                table: "subscription_payment_links",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "used_at",
                table: "subscription_payment_links",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "balance_due",
                table: "subscription_invoices",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "billing_details_json",
                table: "subscription_invoices",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "billing_period_end",
                table: "subscription_invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "billing_period_start",
                table: "subscription_invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "currency_code",
                table: "subscription_invoices",
                type: "char(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "discount_amount",
                table: "subscription_invoices",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "invoice_type",
                table: "subscription_invoices",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "issued_at",
                table: "subscription_invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "paid_amount",
                table: "subscription_invoices",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "paid_at",
                table: "subscription_invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "subscription_id",
                table: "subscription_invoices",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "subtotal_amount",
                table: "subscription_invoices",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "tax_amount",
                table: "subscription_invoices",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "voided_at",
                table: "subscription_invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "subscription_invoice_lines",
                type: "varchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "discount_amount",
                table: "subscription_invoice_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "invoice_id",
                table: "subscription_invoice_lines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "item_code",
                table: "subscription_invoice_lines",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "item_reference_id",
                table: "subscription_invoice_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "item_type",
                table: "subscription_invoice_lines",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "line_number_int",
                table: "subscription_invoice_lines",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "line_total",
                table: "subscription_invoice_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "tax_amount",
                table: "subscription_invoice_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "unit_price",
                table: "subscription_invoice_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "applied_at",
                table: "subscription_credit_notes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_platform_user_id",
                table: "subscription_credit_notes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "currency_code",
                table: "subscription_credit_notes",
                type: "char(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "invoice_id",
                table: "subscription_credit_notes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "issued_at",
                table: "subscription_credit_notes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reason",
                table: "subscription_credit_notes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "subscription_credit_notes",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "subtotal_amount",
                table: "subscription_credit_notes",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "tax_amount",
                table: "subscription_credit_notes",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "total_amount",
                table: "subscription_credit_notes",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "credit_note_id",
                table: "subscription_credit_note_lines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "subscription_credit_note_lines",
                type: "varchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "invoice_line_id",
                table: "subscription_credit_note_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "line_number_int",
                table: "subscription_credit_note_lines",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "line_total",
                table: "subscription_credit_note_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "quantity",
                table: "subscription_credit_note_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "tax_amount",
                table: "subscription_credit_note_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "unit_amount",
                table: "subscription_credit_note_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("""
                UPDATE subscription_invoices
                SET
                    subscription_id = tenant_subscription_id,
                    invoice_type = COALESCE(NULLIF(invoice_type, ''), 'SUBSCRIPTION'),
                    currency_code = COALESCE(
                        NULLIF(currency_code, ''),
                        (
                            SELECT NULLIF(ts.currency_code, '')
                            FROM tenant_subscriptions ts
                            WHERE ts.id = subscription_invoices.tenant_subscription_id),
                        (
                            SELECT NULLIF(t.base_currency_code, '')
                            FROM tenants t
                            WHERE t.id = subscription_invoices.tenant_id),
                        'LKR'),
                    subtotal_amount = GREATEST(total_amount::numeric(18,4), 0),
                    discount_amount = 0,
                    tax_amount = 0,
                    paid_amount = CASE
                        WHEN UPPER(COALESCE(invoice_status, '')) = 'PAID'
                            THEN GREATEST(total_amount::numeric(18,4), 0)
                        ELSE 0 END,
                    balance_due = CASE
                        WHEN UPPER(COALESCE(invoice_status, '')) = 'PAID'
                            THEN 0
                        ELSE GREATEST(total_amount::numeric(18,4), 0) END
                WHERE subscription_id = '00000000-0000-0000-0000-000000000000'::uuid
                   OR COALESCE(invoice_type, '') = ''
                   OR COALESCE(currency_code, '') = ''
                   OR subtotal_amount = 0;

                UPDATE subscription_invoice_lines
                SET
                    invoice_id = subscription_invoice_id,
                    line_number_int = CASE
                        WHEN line_number ~ '^[0-9]+$' THEN line_number::int
                        ELSE NULL END,
                    item_type = COALESCE(NULLIF(item_type, ''), 'SUBSCRIPTION'),
                    description = COALESCE(NULLIF(description, ''), 'Subscription line'),
                    unit_price = CASE
                        WHEN quantity > 0 THEN (line_total_amount / quantity)::numeric(18,4)
                        ELSE line_total_amount::numeric(18,4) END,
                    discount_amount = 0,
                    tax_amount = 0,
                    line_total = GREATEST(line_total_amount::numeric(18,4), 0)
                WHERE invoice_id = '00000000-0000-0000-0000-000000000000'::uuid
                   OR COALESCE(item_type, '') = ''
                   OR COALESCE(description, '') = '';

                UPDATE subscription_payment_links
                SET
                    invoice_id = subscription_payment_links.subscription_invoice_id,
                    tenant_id = si.tenant_id,
                    token_hash = COALESCE(NULLIF(subscription_payment_links.token_hash, ''), subscription_payment_links.payment_link_token_hash),
                    link_status = COALESCE(NULLIF(subscription_payment_links.link_status, ''), 'ACTIVE'),
                    payment_url = COALESCE(NULLIF(subscription_payment_links.payment_url, ''), '/pay/legacy'),
                    reminder_count = COALESCE(subscription_payment_links.reminder_count, 0)
                FROM subscription_invoices si
                WHERE si.id = subscription_payment_links.subscription_invoice_id
                  AND (
                      subscription_payment_links.invoice_id = '00000000-0000-0000-0000-000000000000'::uuid
                      OR subscription_payment_links.tenant_id = '00000000-0000-0000-0000-000000000000'::uuid
                      OR COALESCE(subscription_payment_links.token_hash, '') = ''
                      OR COALESCE(subscription_payment_links.payment_url, '') = '');

                UPDATE subscription_payment_transactions
                SET
                    invoice_id = subscription_payment_transactions.subscription_invoice_id,
                    tenant_id = si.tenant_id,
                    payment_link_id = NULLIF(subscription_payment_transactions.subscription_payment_link_id, '00000000-0000-0000-0000-000000000000'::uuid),
                    transaction_type = COALESCE(NULLIF(subscription_payment_transactions.transaction_type, ''), 'PAYMENT'),
                    provider_name = COALESCE(NULLIF(subscription_payment_transactions.provider_name, ''), 'LEGACY'),
                    provider_transaction_id = COALESCE(NULLIF(subscription_payment_transactions.provider_transaction_id, ''), subscription_payment_transactions.provider_transaction_reference),
                    transaction_status = COALESCE(NULLIF(subscription_payment_transactions.transaction_status, ''), 'SUCCEEDED'),
                    currency_code = COALESCE(
                        NULLIF(subscription_payment_transactions.currency_code, ''),
                        NULLIF(si.currency_code, ''),
                        NULLIF(ts.currency_code, ''),
                        'LKR'),
                    net_amount = GREATEST(subscription_payment_transactions.amount::numeric(18,4), 0),
                    provider_fee = 0
                FROM subscription_invoices si
                INNER JOIN tenant_subscriptions ts ON ts.id = si.tenant_subscription_id
                WHERE si.id = subscription_payment_transactions.subscription_invoice_id
                  AND (
                      subscription_payment_transactions.invoice_id = '00000000-0000-0000-0000-000000000000'::uuid
                      OR subscription_payment_transactions.tenant_id = '00000000-0000-0000-0000-000000000000'::uuid
                      OR COALESCE(subscription_payment_transactions.provider_transaction_id, '') = ''
                      OR COALESCE(subscription_payment_transactions.provider_name, '') = '');

                UPDATE subscription_credit_notes
                SET
                    invoice_id = subscription_credit_notes.subscription_invoice_id,
                    currency_code = COALESCE(
                        NULLIF(subscription_credit_notes.currency_code, ''),
                        NULLIF(si.currency_code, ''),
                        'LKR'),
                    subtotal_amount = GREATEST(subscription_credit_notes.total_credit_amount::numeric(18,4), 0),
                    tax_amount = 0,
                    total_amount = GREATEST(subscription_credit_notes.total_credit_amount::numeric(18,4), 0),
                    status = COALESCE(NULLIF(subscription_credit_notes.status, ''), 'DRAFT')
                FROM subscription_invoices si
                WHERE si.id = subscription_credit_notes.subscription_invoice_id
                  AND (
                      subscription_credit_notes.invoice_id = '00000000-0000-0000-0000-000000000000'::uuid
                      OR COALESCE(subscription_credit_notes.currency_code, '') = ''
                      OR COALESCE(subscription_credit_notes.status, '') = '');

                UPDATE subscription_credit_note_lines
                SET
                    credit_note_id = subscription_credit_note_id,
                    line_number_int = CASE
                        WHEN line_number ~ '^[0-9]+$' THEN line_number::int
                        ELSE NULL END,
                    description = COALESCE(NULLIF(description, ''), 'Credit note line'),
                    quantity = CASE WHEN quantity = 0 THEN 1 ELSE quantity END,
                    unit_amount = GREATEST(line_credit_amount::numeric(18,4), 0),
                    tax_amount = 0,
                    line_total = GREATEST(line_credit_amount::numeric(18,4), 0)
                WHERE credit_note_id = '00000000-0000-0000-0000-000000000000'::uuid
                   OR quantity = 0
                   OR COALESCE(description, '') = '';
                """);

            migrationBuilder.CreateIndex(
                name: "ix_subscription_payment_transactions_invoice_id",
                table: "subscription_payment_transactions",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_subscription_payment_transactions_payment_link_id",
                table: "subscription_payment_transactions",
                column: "payment_link_id");

            migrationBuilder.CreateIndex(
                name: "ix_subscription_payment_transactions_tenant_id",
                table: "subscription_payment_transactions",
                column: "tenant_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_subscription_payment_transactions_net_amount",
                table: "subscription_payment_transactions",
                sql: "net_amount IS NULL OR net_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_subscription_payment_transactions_provider_fee",
                table: "subscription_payment_transactions",
                sql: "provider_fee IS NULL OR provider_fee >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payment_links_created_by_platform_user_id",
                table: "subscription_payment_links",
                column: "created_by_platform_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_subscription_payment_links_invoice_id",
                table: "subscription_payment_links",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_subscription_payment_links_tenant_id",
                table: "subscription_payment_links",
                column: "tenant_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_subscription_payment_links_reminder_count",
                table: "subscription_payment_links",
                sql: "reminder_count IS NULL OR reminder_count >= 0");

            migrationBuilder.CreateIndex(
                name: "ix_subscription_invoices_subscription_id",
                table: "subscription_invoices",
                column: "subscription_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_subscription_invoices_balance_due",
                table: "subscription_invoices",
                sql: "balance_due IS NULL OR balance_due >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_subscription_invoices_discount_amount",
                table: "subscription_invoices",
                sql: "discount_amount IS NULL OR discount_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_subscription_invoices_paid_amount",
                table: "subscription_invoices",
                sql: "paid_amount IS NULL OR paid_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_subscription_invoices_subtotal_amount",
                table: "subscription_invoices",
                sql: "subtotal_amount IS NULL OR subtotal_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_subscription_invoices_tax_amount",
                table: "subscription_invoices",
                sql: "tax_amount IS NULL OR tax_amount >= 0");

            migrationBuilder.CreateIndex(
                name: "ix_subscription_invoice_lines_invoice_id",
                table: "subscription_invoice_lines",
                column: "invoice_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_subscription_invoice_lines_discount_amount",
                table: "subscription_invoice_lines",
                sql: "discount_amount IS NULL OR discount_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_subscription_invoice_lines_line_total",
                table: "subscription_invoice_lines",
                sql: "line_total IS NULL OR line_total >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_subscription_invoice_lines_tax_amount",
                table: "subscription_invoice_lines",
                sql: "tax_amount IS NULL OR tax_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_subscription_invoice_lines_unit_price",
                table: "subscription_invoice_lines",
                sql: "unit_price IS NULL OR unit_price >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_credit_notes_created_by_platform_user_id",
                table: "subscription_credit_notes",
                column: "created_by_platform_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_subscription_credit_notes_invoice_id",
                table: "subscription_credit_notes",
                column: "invoice_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_subscription_credit_notes_subtotal_amount",
                table: "subscription_credit_notes",
                sql: "subtotal_amount IS NULL OR subtotal_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_subscription_credit_notes_tax_amount",
                table: "subscription_credit_notes",
                sql: "tax_amount IS NULL OR tax_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_subscription_credit_notes_total_amount",
                table: "subscription_credit_notes",
                sql: "total_amount IS NULL OR total_amount >= 0");

            migrationBuilder.CreateIndex(
                name: "ix_subscription_credit_note_lines_credit_note_id",
                table: "subscription_credit_note_lines",
                column: "credit_note_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_credit_note_lines_invoice_line_id",
                table: "subscription_credit_note_lines",
                column: "invoice_line_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_subscription_credit_note_lines_line_total",
                table: "subscription_credit_note_lines",
                sql: "line_total IS NULL OR line_total >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_subscription_credit_note_lines_quantity",
                table: "subscription_credit_note_lines",
                sql: "quantity IS NULL OR quantity > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_subscription_credit_note_lines_tax_amount",
                table: "subscription_credit_note_lines",
                sql: "tax_amount IS NULL OR tax_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_subscription_credit_note_lines_unit_amount",
                table: "subscription_credit_note_lines",
                sql: "unit_amount IS NULL OR unit_amount >= 0");

            migrationBuilder.AddForeignKey(
                name: "fk_subscription_credit_note_lines_credit_note_id_subscription_credit_notes",
                table: "subscription_credit_note_lines",
                column: "credit_note_id",
                principalTable: "subscription_credit_notes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_subscription_credit_note_lines_invoice_line_id_subscription_invoice_lines",
                table: "subscription_credit_note_lines",
                column: "invoice_line_id",
                principalTable: "subscription_invoice_lines",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_subscription_credit_notes_created_by_platform_user_id_platform_users",
                table: "subscription_credit_notes",
                column: "created_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_subscription_credit_notes_invoice_id_subscription_invoices",
                table: "subscription_credit_notes",
                column: "invoice_id",
                principalTable: "subscription_invoices",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_subscription_invoice_lines_invoice_id_subscription_invoices",
                table: "subscription_invoice_lines",
                column: "invoice_id",
                principalTable: "subscription_invoices",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_subscription_invoices_subscription_id_tenant_subscriptions",
                table: "subscription_invoices",
                column: "subscription_id",
                principalTable: "tenant_subscriptions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_subscription_payment_links_created_by_platform_user_id_platform_users",
                table: "subscription_payment_links",
                column: "created_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_subscription_payment_links_invoice_id_subscription_invoices",
                table: "subscription_payment_links",
                column: "invoice_id",
                principalTable: "subscription_invoices",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_subscription_payment_links_tenant_id_tenants",
                table: "subscription_payment_links",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_subscription_payment_transactions_invoice_id_subscription_invoices",
                table: "subscription_payment_transactions",
                column: "invoice_id",
                principalTable: "subscription_invoices",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_subscription_payment_transactions_payment_link_id_subscription_payment_links",
                table: "subscription_payment_transactions",
                column: "payment_link_id",
                principalTable: "subscription_payment_links",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_subscription_payment_transactions_tenant_id_tenants",
                table: "subscription_payment_transactions",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_subscription_credit_note_lines_credit_note_id_subscription_credit_notes",
                table: "subscription_credit_note_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_subscription_credit_note_lines_invoice_line_id_subscription_invoice_lines",
                table: "subscription_credit_note_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_subscription_credit_notes_created_by_platform_user_id_platform_users",
                table: "subscription_credit_notes");

            migrationBuilder.DropForeignKey(
                name: "fk_subscription_credit_notes_invoice_id_subscription_invoices",
                table: "subscription_credit_notes");

            migrationBuilder.DropForeignKey(
                name: "fk_subscription_invoice_lines_invoice_id_subscription_invoices",
                table: "subscription_invoice_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_subscription_invoices_subscription_id_tenant_subscriptions",
                table: "subscription_invoices");

            migrationBuilder.DropForeignKey(
                name: "fk_subscription_payment_links_created_by_platform_user_id_platform_users",
                table: "subscription_payment_links");

            migrationBuilder.DropForeignKey(
                name: "fk_subscription_payment_links_invoice_id_subscription_invoices",
                table: "subscription_payment_links");

            migrationBuilder.DropForeignKey(
                name: "fk_subscription_payment_links_tenant_id_tenants",
                table: "subscription_payment_links");

            migrationBuilder.DropForeignKey(
                name: "fk_subscription_payment_transactions_invoice_id_subscription_invoices",
                table: "subscription_payment_transactions");

            migrationBuilder.DropForeignKey(
                name: "fk_subscription_payment_transactions_payment_link_id_subscription_payment_links",
                table: "subscription_payment_transactions");

            migrationBuilder.DropForeignKey(
                name: "fk_subscription_payment_transactions_tenant_id_tenants",
                table: "subscription_payment_transactions");

            migrationBuilder.DropIndex(
                name: "ix_subscription_payment_transactions_invoice_id",
                table: "subscription_payment_transactions");

            migrationBuilder.DropIndex(
                name: "ix_subscription_payment_transactions_payment_link_id",
                table: "subscription_payment_transactions");

            migrationBuilder.DropIndex(
                name: "ix_subscription_payment_transactions_tenant_id",
                table: "subscription_payment_transactions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_subscription_payment_transactions_net_amount",
                table: "subscription_payment_transactions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_subscription_payment_transactions_provider_fee",
                table: "subscription_payment_transactions");

            migrationBuilder.DropIndex(
                name: "IX_subscription_payment_links_created_by_platform_user_id",
                table: "subscription_payment_links");

            migrationBuilder.DropIndex(
                name: "ix_subscription_payment_links_invoice_id",
                table: "subscription_payment_links");

            migrationBuilder.DropIndex(
                name: "ix_subscription_payment_links_tenant_id",
                table: "subscription_payment_links");

            migrationBuilder.DropCheckConstraint(
                name: "ck_subscription_payment_links_reminder_count",
                table: "subscription_payment_links");

            migrationBuilder.DropIndex(
                name: "ix_subscription_invoices_subscription_id",
                table: "subscription_invoices");

            migrationBuilder.DropCheckConstraint(
                name: "ck_subscription_invoices_balance_due",
                table: "subscription_invoices");

            migrationBuilder.DropCheckConstraint(
                name: "ck_subscription_invoices_discount_amount",
                table: "subscription_invoices");

            migrationBuilder.DropCheckConstraint(
                name: "ck_subscription_invoices_paid_amount",
                table: "subscription_invoices");

            migrationBuilder.DropCheckConstraint(
                name: "ck_subscription_invoices_subtotal_amount",
                table: "subscription_invoices");

            migrationBuilder.DropCheckConstraint(
                name: "ck_subscription_invoices_tax_amount",
                table: "subscription_invoices");

            migrationBuilder.DropIndex(
                name: "ix_subscription_invoice_lines_invoice_id",
                table: "subscription_invoice_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_subscription_invoice_lines_discount_amount",
                table: "subscription_invoice_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_subscription_invoice_lines_line_total",
                table: "subscription_invoice_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_subscription_invoice_lines_tax_amount",
                table: "subscription_invoice_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_subscription_invoice_lines_unit_price",
                table: "subscription_invoice_lines");

            migrationBuilder.DropIndex(
                name: "IX_subscription_credit_notes_created_by_platform_user_id",
                table: "subscription_credit_notes");

            migrationBuilder.DropIndex(
                name: "ix_subscription_credit_notes_invoice_id",
                table: "subscription_credit_notes");

            migrationBuilder.DropCheckConstraint(
                name: "ck_subscription_credit_notes_subtotal_amount",
                table: "subscription_credit_notes");

            migrationBuilder.DropCheckConstraint(
                name: "ck_subscription_credit_notes_tax_amount",
                table: "subscription_credit_notes");

            migrationBuilder.DropCheckConstraint(
                name: "ck_subscription_credit_notes_total_amount",
                table: "subscription_credit_notes");

            migrationBuilder.DropIndex(
                name: "ix_subscription_credit_note_lines_credit_note_id",
                table: "subscription_credit_note_lines");

            migrationBuilder.DropIndex(
                name: "IX_subscription_credit_note_lines_invoice_line_id",
                table: "subscription_credit_note_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_subscription_credit_note_lines_line_total",
                table: "subscription_credit_note_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_subscription_credit_note_lines_quantity",
                table: "subscription_credit_note_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_subscription_credit_note_lines_tax_amount",
                table: "subscription_credit_note_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_subscription_credit_note_lines_unit_amount",
                table: "subscription_credit_note_lines");

            migrationBuilder.DropColumn(
                name: "currency_code",
                table: "subscription_payment_transactions");

            migrationBuilder.DropColumn(
                name: "failed_at",
                table: "subscription_payment_transactions");

            migrationBuilder.DropColumn(
                name: "failure_reason",
                table: "subscription_payment_transactions");

            migrationBuilder.DropColumn(
                name: "idempotency_key",
                table: "subscription_payment_transactions");

            migrationBuilder.DropColumn(
                name: "invoice_id",
                table: "subscription_payment_transactions");

            migrationBuilder.DropColumn(
                name: "net_amount",
                table: "subscription_payment_transactions");

            migrationBuilder.DropColumn(
                name: "paid_at",
                table: "subscription_payment_transactions");

            migrationBuilder.DropColumn(
                name: "payment_link_id",
                table: "subscription_payment_transactions");

            migrationBuilder.DropColumn(
                name: "provider_fee",
                table: "subscription_payment_transactions");

            migrationBuilder.DropColumn(
                name: "provider_name",
                table: "subscription_payment_transactions");

            migrationBuilder.DropColumn(
                name: "provider_response_json",
                table: "subscription_payment_transactions");

            migrationBuilder.DropColumn(
                name: "provider_transaction_id",
                table: "subscription_payment_transactions");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "subscription_payment_transactions");

            migrationBuilder.DropColumn(
                name: "transaction_status",
                table: "subscription_payment_transactions");

            migrationBuilder.DropColumn(
                name: "transaction_type",
                table: "subscription_payment_transactions");

            migrationBuilder.DropColumn(
                name: "created_by_platform_user_id",
                table: "subscription_payment_links");

            migrationBuilder.DropColumn(
                name: "invoice_id",
                table: "subscription_payment_links");

            migrationBuilder.DropColumn(
                name: "last_reminder_at",
                table: "subscription_payment_links");

            migrationBuilder.DropColumn(
                name: "link_status",
                table: "subscription_payment_links");

            migrationBuilder.DropColumn(
                name: "payment_url",
                table: "subscription_payment_links");

            migrationBuilder.DropColumn(
                name: "provider_name",
                table: "subscription_payment_links");

            migrationBuilder.DropColumn(
                name: "provider_payment_link_id",
                table: "subscription_payment_links");

            migrationBuilder.DropColumn(
                name: "reminder_count",
                table: "subscription_payment_links");

            migrationBuilder.DropColumn(
                name: "revoked_at",
                table: "subscription_payment_links");

            migrationBuilder.DropColumn(
                name: "sent_at",
                table: "subscription_payment_links");

            migrationBuilder.DropColumn(
                name: "sent_to_email",
                table: "subscription_payment_links");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "subscription_payment_links");

            migrationBuilder.DropColumn(
                name: "token_hash",
                table: "subscription_payment_links");

            migrationBuilder.DropColumn(
                name: "used_at",
                table: "subscription_payment_links");

            migrationBuilder.DropColumn(
                name: "balance_due",
                table: "subscription_invoices");

            migrationBuilder.DropColumn(
                name: "billing_details_json",
                table: "subscription_invoices");

            migrationBuilder.DropColumn(
                name: "billing_period_end",
                table: "subscription_invoices");

            migrationBuilder.DropColumn(
                name: "billing_period_start",
                table: "subscription_invoices");

            migrationBuilder.DropColumn(
                name: "currency_code",
                table: "subscription_invoices");

            migrationBuilder.DropColumn(
                name: "discount_amount",
                table: "subscription_invoices");

            migrationBuilder.DropColumn(
                name: "invoice_type",
                table: "subscription_invoices");

            migrationBuilder.DropColumn(
                name: "issued_at",
                table: "subscription_invoices");

            migrationBuilder.DropColumn(
                name: "paid_amount",
                table: "subscription_invoices");

            migrationBuilder.DropColumn(
                name: "paid_at",
                table: "subscription_invoices");

            migrationBuilder.DropColumn(
                name: "subscription_id",
                table: "subscription_invoices");

            migrationBuilder.DropColumn(
                name: "subtotal_amount",
                table: "subscription_invoices");

            migrationBuilder.DropColumn(
                name: "tax_amount",
                table: "subscription_invoices");

            migrationBuilder.DropColumn(
                name: "voided_at",
                table: "subscription_invoices");

            migrationBuilder.DropColumn(
                name: "description",
                table: "subscription_invoice_lines");

            migrationBuilder.DropColumn(
                name: "discount_amount",
                table: "subscription_invoice_lines");

            migrationBuilder.DropColumn(
                name: "invoice_id",
                table: "subscription_invoice_lines");

            migrationBuilder.DropColumn(
                name: "item_code",
                table: "subscription_invoice_lines");

            migrationBuilder.DropColumn(
                name: "item_reference_id",
                table: "subscription_invoice_lines");

            migrationBuilder.DropColumn(
                name: "item_type",
                table: "subscription_invoice_lines");

            migrationBuilder.DropColumn(
                name: "line_number_int",
                table: "subscription_invoice_lines");

            migrationBuilder.DropColumn(
                name: "line_total",
                table: "subscription_invoice_lines");

            migrationBuilder.DropColumn(
                name: "tax_amount",
                table: "subscription_invoice_lines");

            migrationBuilder.DropColumn(
                name: "unit_price",
                table: "subscription_invoice_lines");

            migrationBuilder.DropColumn(
                name: "applied_at",
                table: "subscription_credit_notes");

            migrationBuilder.DropColumn(
                name: "created_by_platform_user_id",
                table: "subscription_credit_notes");

            migrationBuilder.DropColumn(
                name: "currency_code",
                table: "subscription_credit_notes");

            migrationBuilder.DropColumn(
                name: "invoice_id",
                table: "subscription_credit_notes");

            migrationBuilder.DropColumn(
                name: "issued_at",
                table: "subscription_credit_notes");

            migrationBuilder.DropColumn(
                name: "reason",
                table: "subscription_credit_notes");

            migrationBuilder.DropColumn(
                name: "status",
                table: "subscription_credit_notes");

            migrationBuilder.DropColumn(
                name: "subtotal_amount",
                table: "subscription_credit_notes");

            migrationBuilder.DropColumn(
                name: "tax_amount",
                table: "subscription_credit_notes");

            migrationBuilder.DropColumn(
                name: "total_amount",
                table: "subscription_credit_notes");

            migrationBuilder.DropColumn(
                name: "credit_note_id",
                table: "subscription_credit_note_lines");

            migrationBuilder.DropColumn(
                name: "description",
                table: "subscription_credit_note_lines");

            migrationBuilder.DropColumn(
                name: "invoice_line_id",
                table: "subscription_credit_note_lines");

            migrationBuilder.DropColumn(
                name: "line_number_int",
                table: "subscription_credit_note_lines");

            migrationBuilder.DropColumn(
                name: "line_total",
                table: "subscription_credit_note_lines");

            migrationBuilder.DropColumn(
                name: "quantity",
                table: "subscription_credit_note_lines");

            migrationBuilder.DropColumn(
                name: "tax_amount",
                table: "subscription_credit_note_lines");

            migrationBuilder.DropColumn(
                name: "unit_amount",
                table: "subscription_credit_note_lines");
        }
    }
}
