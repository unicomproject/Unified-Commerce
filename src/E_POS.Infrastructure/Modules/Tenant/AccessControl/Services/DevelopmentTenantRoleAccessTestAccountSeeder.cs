using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Tenant.AccessControl.Contracts;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Infrastructure.Modules.Tenant.AccessControl.Options;
using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace E_POS.Infrastructure.Modules.Tenant.AccessControl.Services;

/// <summary>
/// Activates only the fixed development tenant identities already created by migrations.
/// It never creates users, roles, or role assignments and reads passwords only from runtime configuration.
/// </summary>
public sealed class DevelopmentTenantRoleAccessTestAccountSeeder : IDevelopmentTenantRoleAccessTestAccountSeeder
{
    private readonly DevelopmentTenantRoleAccessSeedOptions _options;
    private readonly EPosDbContext _dbContext;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<DevelopmentTenantRoleAccessTestAccountSeeder> _logger;

    public DevelopmentTenantRoleAccessTestAccountSeeder(
        IOptions<DevelopmentTenantRoleAccessSeedOptions> options,
        EPosDbContext dbContext,
        IPasswordHashService passwordHashService,
        IDateTimeProvider dateTimeProvider,
        ILogger<DevelopmentTenantRoleAccessTestAccountSeeder> logger)
    {
        _options = options.Value;
        _dbContext = dbContext;
        _passwordHashService = passwordHashService;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedProfileAsync(
            "TenantAdmin",
            _options.TenantAdmin,
            DevelopmentTenantSeedConstants.TenantAdminUserId,
            DevelopmentTenantSeedConstants.TenantAdminEmail,
            DevelopmentTenantSeedConstants.TenantAdminRoleId,
            DevelopmentTenantSeedConstants.TenantAdminRoleCode,
            cancellationToken);

        await SeedProfileAsync(
            "Cashier",
            _options.Cashier,
            DevelopmentTenantSeedConstants.CashierUserId,
            DevelopmentTenantSeedConstants.CashierEmail,
            DevelopmentTenantSeedConstants.CashierRoleId,
            DevelopmentTenantSeedConstants.CashierRoleCode,
            cancellationToken);
    }

    private async Task SeedProfileAsync(
        string profileName,
        DevelopmentTenantRoleAccessAccountOptions account,
        Guid userId,
        string expectedEmail,
        Guid roleId,
        string roleCode,
        CancellationToken cancellationToken)
    {
        if (!account.HasPassword)
        {
            _logger.LogWarning(
                "Development Tenant Role Access profile {ProfileName} skipped: configure a password under {SectionName}.",
                profileName,
                DevelopmentTenantRoleAccessSeedOptions.SectionName);
            return;
        }

        var tenantId = DevelopmentTenantSeedConstants.DevelopmentTenantId;
        var user = await _dbContext.TenantUsers.SingleOrDefaultAsync(
            item => item.Id == userId && item.TenantId == tenantId && item.Email == expectedEmail,
            cancellationToken);
        var role = await _dbContext.TenantRoles.SingleOrDefaultAsync(
            item => item.Id == roleId && item.TenantId == tenantId && item.RoleCode == roleCode && item.IsActive,
            cancellationToken);
        var assignment = await _dbContext.TenantUserRoles.SingleOrDefaultAsync(
            item => item.TenantId == tenantId && item.TenantUserId == userId && item.TenantRoleId == roleId,
            cancellationToken);

        if (user is null || role is null || assignment is null)
        {
            _logger.LogError(
                "Development Tenant Role Access profile {ProfileName} skipped: expected seeded user, active role, and assignment are required.",
                profileName);
            return;
        }

        var now = _dateTimeProvider.UtcNow;
        user.ResetPasswordAndActivate(_passwordHashService.HashPassword(account.Password!), "development-runtime-seed", now);
        assignment.Reactivate(null, now);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Development Tenant Role Access profile {ProfileName}: reconciled fixed development account with role {RoleCode}.",
            profileName,
            roleCode);
    }
}
