namespace E_POS.Infrastructure.Persistence.Seed;

/// <summary>
/// Development-only repair: backfill NULL sales_order_lines.barcode_snapshot for
/// Click &amp; Collect fixture orders from the unambiguous primary product barcode.
/// Does not rewrite non-null snapshots or non-fixture historical rows.
/// </summary>
public static class DevelopmentClickCollectBarcodeSnapshotRepairSeedData
{
    public const string RepairSql = """
        UPDATE sales_order_lines AS sol
        SET barcode_snapshot = matched.barcode,
            updated_at = now()
        FROM (
            SELECT
                sol2.id AS line_id,
                MIN(pb.barcode) AS barcode
            FROM sales_order_lines AS sol2
            INNER JOIN sales_orders AS so
              ON so.id = sol2.sales_order_id
             AND so.tenant_id = sol2.tenant_id
            INNER JOIN product_barcodes AS pb
              ON pb.tenant_id = sol2.tenant_id
             AND pb.product_variant_id = sol2.product_variant_id
             AND pb.is_primary_barcode = TRUE
             AND pb.status = 'ACTIVE'
            WHERE sol2.barcode_snapshot IS NULL
              AND sol2.product_variant_id IS NOT NULL
              AND (
                    so.order_number LIKE 'ECOMM-SEED-%'
                 OR so.order_number LIKE 'OVZ-ECOMM-SEED-%'
              )
              AND (
                  SELECT COUNT(DISTINCT pb2.barcode)
                  FROM product_barcodes AS pb2
                  WHERE pb2.tenant_id = sol2.tenant_id
                    AND pb2.product_variant_id = sol2.product_variant_id
                    AND pb2.is_primary_barcode = TRUE
                    AND pb2.status = 'ACTIVE'
              ) = 1
            GROUP BY sol2.id
        ) AS matched
        WHERE sol.id = matched.line_id
          AND sol.barcode_snapshot IS NULL;
        """;
}
