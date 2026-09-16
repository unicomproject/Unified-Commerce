using System.Text.Json;
using E_POS.Application.Modules.Tenant.HardwareCash.Contracts;
using E_POS.Application.Modules.Tenant.HardwareCash.Services;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.HardwareCash.Services;

public sealed class HardwareDashboardQuery(EPosDbContext db, HardwareQueryScope? scope = null) : IHardwareDashboardQuery
{
    public async Task<JsonElement> GetAsync(Guid tenantId, Guid? outletId, string search, string type,
        string status, string sort, int page, int pageSize, CancellationToken cancellationToken)
    {
        var supported = HardwareCompatibilityCatalog.Profiles.Where(p => p.SupportLevel != "UNSUPPORTED")
            .Select(p => p.DeviceType + ":" + p.ConnectionType).ToArray();
        var offset = (page - 1) * pageSize;
        var scopedTill = scope?.TillId;
        var scopedTenant = scope?.TenantId;
        var scopedOutlet = scope?.OutletId;
        var scopedUser = scope?.UserId;
        // One bounded response and one SQL statement; all dynamic values are parameters.
        var json = await db.Database.SqlQuery<string>($"""
            WITH base AS (
              SELECT d.*, o.outlet_name, a.id AS assignment_id, a.till_id, a.pos_device_id, a.assigned_at,
                assigned_till.till_name AS assigned_till_name,
                assigned_pos.device_name AS assigned_pos_device_name,
                CASE
                  WHEN NOT ((d.hardware_device_type || ':' || d.connection_type) = ANY({supported})) THEN 'Unsupported'
                  WHEN d.status <> 'ACTIVE' THEN 'Issues'
                  WHEN a.id IS NULL THEN 'Not Configured'
                  WHEN h.test_status IN ('FAILED','WARNING') THEN 'Issues'
                  WHEN d.last_seen_at IS NULL THEN 'Unknown'
                  WHEN d.last_seen_at > now() + interval '30 seconds' THEN 'Unknown'
                  WHEN d.last_seen_at < now() - interval '90 seconds' THEN 'Disconnected'
                  WHEN t.test_status IN ('FAILED','ERROR','TIMEOUT','WARNING') THEN 'Issues'
                  WHEN t.test_status IN ('PASSED','SUCCESS') AND t.physical_confirmation = true THEN 'Ready'
                  ELSE 'Not Configured'
                END AS own_status
              FROM hardware_devices d
              JOIN outlets o ON o.id=d.outlet_id AND o.tenant_id=d.tenant_id
              LEFT JOIN LATERAL (
                SELECT * FROM hardware_device_assignments a WHERE a.tenant_id=d.tenant_id
                  AND a.hardware_device_id=d.id AND a.released_at IS NULL ORDER BY a.assigned_at DESC LIMIT 1
              ) a ON true
              LEFT JOIN tills assigned_till ON assigned_till.id=a.till_id
                AND assigned_till.tenant_id=d.tenant_id AND assigned_till.outlet_id=d.outlet_id
              LEFT JOIN pos_devices assigned_pos ON assigned_pos.id=a.pos_device_id
                AND assigned_pos.tenant_id=d.tenant_id AND assigned_pos.outlet_id=d.outlet_id
              LEFT JOIN LATERAL (
                SELECT * FROM hardware_test_logs t WHERE t.tenant_id=d.tenant_id AND t.hardware_device_id=d.id
                  AND t.configuration_version=d.configuration_version AND t.test_type <> 'TELEMETRY'
                  AND t.tested_at >= a.assigned_at
                  AND ((a.pos_device_id IS NOT NULL AND t.initiated_from_pos_device_id=a.pos_device_id)
                    OR (a.till_id IS NOT NULL AND t.till_id=a.till_id AND EXISTS (
                      SELECT 1 FROM till_device_assignments ta WHERE ta.tenant_id=d.tenant_id
                        AND ta.till_id=a.till_id AND ta.pos_device_id=t.initiated_from_pos_device_id
                        AND ta.released_at IS NULL AND ta.assigned_at <= t.tested_at)))
                ORDER BY t.tested_at DESC, t.id DESC LIMIT 1
              ) t ON true
              LEFT JOIN LATERAL (
                SELECT test_status FROM hardware_test_logs h WHERE h.tenant_id=d.tenant_id
                  AND h.hardware_device_id=d.id AND h.test_type='TELEMETRY'
                  AND h.configuration_version=d.configuration_version AND h.tested_at >= a.assigned_at
                ORDER BY h.tested_at DESC, h.id DESC LIMIT 1
              ) h ON true
              WHERE d.tenant_id={tenantId} AND d.status <> 'DELETED'
                AND ({scopedTill}::uuid IS NULL OR (
                  d.tenant_id={scopedTenant} AND d.outlet_id={scopedOutlet} AND
                  ((a.till_id={scopedTill} AND a.pos_device_id IS NULL AND a.outlet_id={scopedOutlet})
                   OR (a.id IS NULL AND d.created_by_tenant_user_id={scopedUser}))))
            ), resolved AS (
              SELECT b.*, CASE WHEN b.hardware_device_type='CASH_DRAWER' AND b.status='ACTIVE'
                AND b.own_status <> 'Unsupported' AND NOT EXISTS (
                SELECT 1 FROM base p WHERE p.id::text=b.config_json->>'parentPrinterId'
                  AND p.outlet_id=b.outlet_id AND p.hardware_device_type='RECEIPT_PRINTER'
                  AND p.config_json->>'cashDrawer'='true' AND p.own_status='Ready'
                  AND p.till_id IS NOT DISTINCT FROM b.till_id
                  AND p.pos_device_id IS NOT DISTINCT FROM b.pos_device_id
              ) THEN 'Not Configured' ELSE b.own_status END AS readiness
              FROM base b
            ), filtered AS (
              SELECT * FROM resolved WHERE ({outletId}::uuid IS NULL OR outlet_id={outletId})
                AND ({type}='' OR hardware_device_type={type})
                AND ({status}='' OR readiness={status})
                AND ({search}='' OR position(lower({search}) in lower(hardware_device_name || ' ' || hardware_device_code || ' ' || outlet_name))>0)
            ), page_rows AS (
              SELECT * FROM filtered ORDER BY
                CASE WHEN {sort}='name' THEN hardware_device_name END ASC,
                CASE WHEN {sort}='name_desc' THEN hardware_device_name END DESC,
                CASE WHEN {sort}='last_seen' THEN last_seen_at END DESC NULLS LAST,
                id LIMIT {pageSize} OFFSET {offset}
            )
            SELECT jsonb_build_object(
              'checkedAt',now(), 'freshnessSeconds',90,
              'page',{page},'pageSize',{pageSize},'totalCount',(SELECT count(*) FROM filtered),
              'notAssignedCount',(SELECT count(*) FROM filtered WHERE assignment_id IS NULL),
              'summary',coalesce((SELECT jsonb_object_agg(readiness,n) FROM
                (SELECT readiness,count(*) AS n FROM filtered GROUP BY readiness) c),jsonb_build_object()),
              'items',coalesce((SELECT jsonb_agg(jsonb_build_object(
                'hardwareDeviceId',id,'hardwareDeviceCode',hardware_device_code,'hardwareDeviceName',hardware_device_name,
                'hardwareDeviceType',hardware_device_type,'connectionType',connection_type,'status',status,
                'manufacturer',manufacturer,'model',model,
                'outletId',outlet_id,'outletName',outlet_name,'lastSeenAt',last_seen_at,'isAssigned',assignment_id IS NOT NULL,
                'assignedTillId',till_id,'assignedPosDeviceId',pos_device_id,'readiness',readiness,
                'assignedTillName',assigned_till_name,'assignedPosDeviceName',assigned_pos_device_name
              )) FROM page_rows),'[]'::jsonb), 'testAction','TEST_ON_POS_REQUIRED'
            )::text AS "Value"
            """).SingleAsync(cancellationToken);
        return JsonSerializer.Deserialize<JsonElement>(json);
    }
}
