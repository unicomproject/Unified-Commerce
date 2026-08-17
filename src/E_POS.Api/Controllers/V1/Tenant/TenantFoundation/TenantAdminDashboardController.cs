using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers.V1.Tenant.TenantFoundation;

[ApiController]
[Route("api/v1/tenant-admin/dashboard")]
[Authorize(Policy = "TenantOnly")]
public sealed class TenantAdminDashboardController : ControllerBase
{
    [HttpGet("summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetDashboardSummary(CancellationToken cancellationToken)
    {
        var tenantIdValue = User.FindFirstValue("tenant_id");
        if (!Guid.TryParse(tenantIdValue, out var tenantId))
        {
            return Unauthorized(new
            {
                success = false,
                message = "Invalid tenant identity claims."
            });
        }

        // Mock/Seed data for the Tenant Admin Dashboard Summary
        var data = new
        {
            todaySales = new
            {
                amount = 125430.50,
                currency = "LKR",
                growthPercent = 15.2
            },
            orders = new
            {
                count = 142,
                growthPercent = 5.4
            },
            activeOutlets = new
            {
                count = 3,
                onlineCount = 3
            },
            stockAlerts = new
            {
                count = 12
            },
            tills = new
            {
                onlineCount = 8,
                offlineCount = 1
            },
            needsAttention = new
            {
                offlineTills = 1,
                lowStockItems = 12,
                pendingStaffInvites = 3,
                paymentDue = new
                {
                    amount = 50000.00,
                    currency = "LKR",
                    dueDate = DateTime.UtcNow.AddDays(15).ToString("yyyy-MM-dd")
                }
            }
        };

        return Ok(new { success = true, data });
    }
}
