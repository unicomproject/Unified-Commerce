using E_POS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace E_POS.Api.Common;

public sealed class HardwareTelemetryTransactionFilter(EPosDbContext db, ITenantRequestContextFactory contexts) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext action, ActionExecutionDelegate next)
    {
        if (!contexts.TryCreate(action.HttpContext.User, out var context)) { await next(); return; }
        await using var transaction = await db.Database.BeginTransactionAsync(action.HttpContext.RequestAborted);
        if (db.Database.ProviderName?.Contains("Npgsql") == true)
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended({"hardware:" + context.TenantId.ToString("N")}, 0))", action.HttpContext.RequestAborted);
        var result = await next();
        if (result.Exception is null && (result.Result as IStatusCodeActionResult)?.StatusCode is >= 200 and < 300)
            await transaction.CommitAsync(action.HttpContext.RequestAborted);
    }
}
