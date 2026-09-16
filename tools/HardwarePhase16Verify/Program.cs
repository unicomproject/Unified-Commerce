using System.Text.Json;
using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Modules.Tenant.HardwareCash.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using E_POS.Infrastructure.Modules.Tenant.HardwareCash.Repositories;
using E_POS.Application.Modules.Tenant.HardwareCash.Dtos;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Infrastructure.Modules.Shared.Idempotency.Services;

try
{
    var connection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
    if (string.IsNullOrWhiteSpace(connection))
    {
        using var secrets = JsonDocument.Parse(File.ReadAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Microsoft", "UserSecrets", "epos-api-development-secrets", "secrets.json")));
        connection = secrets.RootElement.GetProperty("ConnectionStrings:DefaultConnection").GetString();
    }
    var builder = new NpgsqlConnectionStringBuilder(connection) { Timeout = 5, CommandTimeout = 30, IncludeErrorDetail = false };
    await using var db = new EPosDbContext(new DbContextOptionsBuilder<EPosDbContext>().UseNpgsql(builder.ConnectionString).Options);
    await db.Database.OpenConnectionAsync();
    Console.WriteLine("PostgreSQL connection: PASS (secure local configuration)");
    var query = new HardwareDashboardQuery(db);
    var absent = await query.GetAsync(Guid.NewGuid(), null, "", "", "", "name", 1, 5, default);
    if (absent.GetProperty("totalCount").GetInt32() != 0) throw new InvalidOperationException("Tenant isolation failed");
    Console.WriteLine("Dashboard unknown-tenant isolation: PASS");
    var tenant = await db.HardwareDevices.AsNoTracking().Select(d => (Guid?)d.TenantId).FirstOrDefaultAsync();
    if (tenant is not null)
    {
        foreach (var sort in new[] { "name", "name_desc", "last_seen" })
        {
            var result = await query.GetAsync(tenant.Value, null, "", "", "", sort, 1, 5, default);
            if (result.GetProperty("items").GetArrayLength() > 5) throw new InvalidOperationException("Pagination failed");
            var total = result.GetProperty("summary").EnumerateObject().Sum(p => p.Value.GetInt32());
            if (total != result.GetProperty("totalCount").GetInt32()) throw new InvalidOperationException("Summary failed");
        }
        Console.WriteLine("Dashboard actual-tenant page/summary/sort: PASS (identifiers and records redacted)");
    }
    else Console.WriteLine("Dashboard actual-tenant cases: NOT RUN (no hardware records)");

    if (tenant is { } tenantId)
    {
        var outletId = await db.Outlets.Where(x => x.TenantId == tenantId).Select(x => x.Id).FirstAsync();
        var userId = await db.TenantUsers.Where(x => x.TenantId == tenantId).Select(x => x.Id).FirstAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var now = DateTimeOffset.UtcNow;
        var pos = PosDevice.Create(Guid.NewGuid(), tenantId, outletId, "PH16-" + Guid.NewGuid().ToString("N"),
            "Phase 16 rollback fixture", "TABLET", "ACTIVE", userId, now);
        pos.PairForActivation("Phase 16 rollback fixture", "TABLET", "ANDROID", "fixture", "fixture-hash", userId, now);
        db.PosDevices.Add(pos);
        var hardware = HardwareDevice.Create(Guid.NewGuid(), tenantId, outletId, null,
            "PH16-" + Guid.NewGuid().ToString("N"), "Phase 16 rollback printer", "RECEIPT_PRINTER", "NETWORK",
            null, null, null, null, null, "{}", "ACTIVE", userId, now);
        db.HardwareDevices.Add(hardware);
        db.HardwareDeviceAssignments.Add(HardwareDeviceAssignment.Create(Guid.NewGuid(), tenantId, outletId,
            hardware.Id, null, pos.Id, true, userId, now));
        await db.SaveChangesAsync();
        var repository = new PosHardwareRepository(db);
        var configurations = await repository.GetConfigurationsAsync(tenantId, pos.Id, default);
        if (configurations.Count != 1 || configurations[0].HardwareType != "receiptPrinter")
            throw new InvalidOperationException("Registry mapping failed");
        var request = new SavePosHardwareConfigurationRequest(pos.Id, outletId, null, "receiptPrinter", "NETWORK",
            "Phase 16 rollback printer", true, 1, null, null, null, null, null);
        var saved = await repository.SaveConfigurationAsync(tenantId, userId, request, "{}", now, default);
        if (saved.Configuration?.ConfigurationId != hardware.Id || saved.Configuration.ConfigurationVersion != 2)
            throw new InvalidOperationException("Registry device duplicated or version lost");
        var stale = await repository.SaveConfigurationAsync(tenantId, userId, request, "{}", now, default);
        if (stale.ErrorCode != "pos_hardware.version_conflict") throw new InvalidOperationException("Stale save accepted");
        var testRequest = new CreateHardwareTestRequest(Guid.NewGuid(), pos.Id, null, hardware.Id, "receiptPrinter", "print", 2);
        var test = await repository.CreateTestAsync(tenantId, userId, testRequest, "fixture-hash", now, default);
        if (test.Operation is null) throw new InvalidOperationException("Test creation failed");
        var replay = await repository.CreateTestAsync(tenantId, userId, testRequest, "fixture-hash", now, default);
        if (replay.Operation?.TestId != test.Operation.TestId) throw new InvalidOperationException("Test replay duplicated");
        await repository.SaveConfigurationAsync(tenantId, userId, request with { ExpectedVersion = 2 }, "{}", now, default);
        var rejected = await repository.CompleteTestAsync(tenantId, userId, test.Operation.TestId,
            new("PASSED", "SUCCESS", null, true), null, now, default);
        if (rejected.ErrorCode != "pos_hardware.version_conflict") throw new InvalidOperationException("Stale physical result accepted");
        var next = await repository.CreateTestAsync(tenantId, userId, testRequest with { RequestId = Guid.NewGuid(), ConfigurationVersion = 3 },
            "next-fixture-hash", now, default);
        if (next.Operation is null) throw new InvalidOperationException("Current test creation failed");
        await repository.CompleteTestAsync(tenantId, userId, next.Operation.TestId,
            new("PASSED", "SUCCESS", null, true), null, now, default);
        if (hardware.LastSeenAt != now)
            throw new InvalidOperationException("Physical confirmation did not record observation freshness");
        await db.SaveChangesAsync();
        var ready = await query.GetAsync(tenantId, outletId, hardware.HardwareDeviceCode, "", "", "name", 1, 5, default);
        if (ready.GetProperty("items")[0].GetProperty("readiness").GetString() != "Ready")
            throw new InvalidOperationException("Current physical evidence projection failed");
        var mutation = new HardwareMutationRunner(db, new IdempotencyService(db, new VerificationClock()));
        var context = new TenantRequestContext(tenantId, userId, []);
        var key = Guid.NewGuid().ToString("N");
        var executions = 0;
        async Task<ApplicationResult<string>> Rename(CancellationToken ct)
        {
            executions++;
            hardware.UpdateConfiguration("Phase 16 renamed fixture", hardware.ConnectionType, hardware.ConfigJson,
                "ACTIVE", hardware.ConfigurationVersion, userId, now);
            await db.SaveChangesAsync(ct);
            return ApplicationResult<string>.Success("updated");
        }
        var mutationResult = await mutation.ExecuteAsync(context, "update", hardware.Id, key, new { name = "fixture" }, Rename, default);
        var mutationReplay = await mutation.ExecuteAsync(context, "update", hardware.Id, key, new { name = "fixture" }, Rename, default);
        var mutationConflict = await mutation.ExecuteAsync(context, "update", hardware.Id, key, new { name = "different" }, Rename, default);
        if (!mutationResult.IsSuccess || !mutationReplay.IsSuccess || executions != 1 ||
            mutationConflict.Error.Code != "hardware.idempotency_conflict" ||
            await db.AuditLogs.CountAsync(x => x.TenantId == tenantId && x.EntityId == hardware.Id && x.EntityType == "HARDWARE") != 1)
            throw new InvalidOperationException("Mutation replay or audit failed");
        var oldTransition = HardwareTestLog.Create(Guid.NewGuid(), tenantId, outletId, hardware.Id, pos.Id,
            null, null, userId, Guid.NewGuid(), "old-fixture", 1, "RECEIPT_PRINTER", "TELEMETRY", "FAILED",
            null, null, null, now.AddDays(-120), now.AddDays(-120));
        var newestTransition = HardwareTestLog.Create(Guid.NewGuid(), tenantId, outletId, hardware.Id, pos.Id,
            null, null, userId, Guid.NewGuid(), "new-fixture", 1, "RECEIPT_PRINTER", "TELEMETRY", "SUCCESS",
            null, null, null, now.AddDays(-100), now.AddDays(-100));
        db.HardwareTestLogs.AddRange(oldTransition, newestTransition);
        await db.SaveChangesAsync();
        await HardwareTelemetryRetentionWorker.PruneAsync(db, default);
        if (await db.HardwareTestLogs.AsNoTracking().AnyAsync(x => x.Id == oldTransition.Id) ||
            !await db.HardwareTestLogs.AsNoTracking().AnyAsync(x => x.Id == newestTransition.Id) ||
            !await db.HardwareTestLogs.AsNoTracking().AnyAsync(x => x.Id == next.Operation.TestId))
            throw new InvalidOperationException("Retention evidence preservation failed");
        await transaction.RollbackAsync();
        Console.WriteLine("PostgreSQL registry/POS identity, version conflict, test replay and readiness projection: PASS (synthetic fixtures rolled back; not physical acceptance)");
        Console.WriteLine("PostgreSQL mutation replay/conflict, single durable audit and retention preservation: PASS (transaction rolled back)");
    }
}

catch (Exception ex)
{
    Console.WriteLine("Database verification: FAIL " + ex.GetType().Name + (ex is PostgresException pg ? " SQLSTATE=" + pg.SqlState : ""));
    Environment.ExitCode = 1;
}

sealed class VerificationClock : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
