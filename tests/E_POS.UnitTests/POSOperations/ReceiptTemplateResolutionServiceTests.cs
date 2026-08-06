using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Xunit;
using E_POS.Application.Common.Contracts;
using E_POS.Domain.Modules.Tenant.POSOperations.Entities;
using E_POS.Infrastructure.Modules.Tenant.POSOperations.Services;
using E_POS.Infrastructure.Persistence;

namespace E_POS.UnitTests.POSOperations;

public sealed class ReceiptTemplateResolutionServiceTests : IDisposable
{
    private readonly EPosDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ReceiptTemplateResolutionService _sut;
    private readonly DateTimeOffset _now = new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero);

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; set; }
    }

    public ReceiptTemplateResolutionServiceTests()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new EPosDbContext(options);

        var fakeProvider = new FakeDateTimeProvider { UtcNow = _now };
        _dateTimeProvider = fakeProvider;

        _sut = new ReceiptTemplateResolutionService(_dbContext, _dateTimeProvider);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    private static ReceiptTemplateVersion CreateVersion(Guid id, Guid tenantId, string templateData, bool isActive)
    {
        var version = new ReceiptTemplateVersion();
        SetProperty(version, "Id", id);
        SetProperty(version, "TenantId", tenantId);
        SetProperty(version, "TemplateData", templateData);
        SetProperty(version, "IsActive", isActive);
        SetProperty(version, "CreatedAt", DateTimeOffset.UtcNow);
        SetProperty(version, "UpdatedAt", DateTimeOffset.UtcNow);
        return version;
    }

    private static ReceiptTemplateAssignment CreateAssignment(Guid tenantId, Guid versionId, string status, bool isDefault, Guid? outletId = null, Guid? tillId = null, Guid? deviceId = null)
    {
        var assignment = new ReceiptTemplateAssignment();
        SetProperty(assignment, "Id", Guid.NewGuid());
        SetProperty(assignment, "TenantId", tenantId);
        SetProperty(assignment, "ReceiptTemplateVersionId", versionId);
        SetProperty(assignment, "Status", status);
        SetProperty(assignment, "IsDefault", isDefault);
        SetProperty(assignment, "OutletId", outletId);
        SetProperty(assignment, "TillId", tillId);
        SetProperty(assignment, "PosDeviceId", deviceId);
        SetProperty(assignment, "EffectiveFrom", DateTimeOffset.MinValue);
        SetProperty(assignment, "EffectiveTo", DateTimeOffset.MaxValue);
        SetProperty(assignment, "CreatedAt", DateTimeOffset.UtcNow);
        SetProperty(assignment, "UpdatedAt", DateTimeOffset.UtcNow);
        return assignment;
    }

    private static void SetProperty(object obj, string propertyName, object? value)
    {
        var prop = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        prop?.SetValue(obj, value);
    }

    [Fact]
    public async Task ResolveTemplateAsync_ReturnsSystemFallback_WhenNoAssignmentExists()
    {
        var tenantId = Guid.NewGuid();
        var result = await _sut.ResolveTemplateAsync(tenantId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(Guid.Empty, result.TemplateVersionId);
        Assert.Equal("{\"type\":\"system_fallback\",\"components\":[]}", result.TemplateData);
    }

    [Fact]
    public async Task ResolveTemplateAsync_ReturnsTemplate_WhenDefaultAssignmentExists()
    {
        var tenantId = Guid.NewGuid();
        var versionId = Guid.NewGuid();

        _dbContext.ReceiptTemplateVersions.Add(CreateVersion(versionId, tenantId, "{}", true));
        _dbContext.ReceiptTemplateAssignments.Add(CreateAssignment(tenantId, versionId, "ACTIVE", true));
        await _dbContext.SaveChangesAsync();

        var result = await _sut.ResolveTemplateAsync(tenantId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(versionId, result.TemplateVersionId);
        Assert.Equal("{}", result.TemplateData);
    }

    [Fact]
    public async Task ResolveTemplateAsync_ReturnsHighestPriority_WhenMultipleExist()
    {
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();

        var defaultVersionId = Guid.NewGuid();
        var outletVersionId = Guid.NewGuid();
        var tillVersionId = Guid.NewGuid();
        var deviceVersionId = Guid.NewGuid();

        _dbContext.ReceiptTemplateVersions.Add(CreateVersion(defaultVersionId, tenantId, "default", true));
        _dbContext.ReceiptTemplateVersions.Add(CreateVersion(outletVersionId, tenantId, "outlet", true));
        _dbContext.ReceiptTemplateVersions.Add(CreateVersion(tillVersionId, tenantId, "till", true));
        _dbContext.ReceiptTemplateVersions.Add(CreateVersion(deviceVersionId, tenantId, "device", true));

        _dbContext.ReceiptTemplateAssignments.Add(CreateAssignment(tenantId, defaultVersionId, "ACTIVE", true));
        _dbContext.ReceiptTemplateAssignments.Add(CreateAssignment(tenantId, outletVersionId, "ACTIVE", false, outletId: outletId));
        _dbContext.ReceiptTemplateAssignments.Add(CreateAssignment(tenantId, tillVersionId, "ACTIVE", false, tillId: tillId));
        
        await _dbContext.SaveChangesAsync();

        var result1 = await _sut.ResolveTemplateAsync(tenantId, outletId, tillId, deviceId, CancellationToken.None);

        _dbContext.ReceiptTemplateAssignments.Add(CreateAssignment(tenantId, deviceVersionId, "ACTIVE", false, deviceId: deviceId));
        await _dbContext.SaveChangesAsync();

        var result2 = await _sut.ResolveTemplateAsync(tenantId, outletId, tillId, deviceId, CancellationToken.None);

        Assert.Equal(tillVersionId, result1!.TemplateVersionId);
        Assert.Equal("till", result1.TemplateData);

        Assert.Equal(deviceVersionId, result2!.TemplateVersionId);
        Assert.Equal("device", result2.TemplateData);
    }
}
