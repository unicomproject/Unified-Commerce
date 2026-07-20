using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Options;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Services;

/// <summary>
/// Creates or reconciles Development Platform Admin test accounts for billing permission browser testing.
/// Credentials come only from configuration/secrets. Does not create roles or touch the seeded super admin.
/// </summary>
public sealed class DevelopmentPlatformAdminTestAccountSeeder : IDevelopmentPlatformAdminTestAccountSeeder
{
    private readonly DevelopmentPlatformAdminSeedOptions _options;
    private readonly IPlatformUserRepository _userRepository;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<DevelopmentPlatformAdminTestAccountSeeder> _logger;

    public DevelopmentPlatformAdminTestAccountSeeder(
        IOptions<DevelopmentPlatformAdminSeedOptions> options,
        IPlatformUserRepository userRepository,
        IPasswordHashService passwordHashService,
        IDateTimeProvider dateTimeProvider,
        ILogger<DevelopmentPlatformAdminTestAccountSeeder> logger)
    {
        _options = options.Value;
        _userRepository = userRepository;
        _passwordHashService = passwordHashService;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedProfileAsync(
            profileName: "BillingViewer",
            account: _options.BillingViewer,
            roleCode: PlatformRoleCodes.BillingViewerDev,
            defaultDisplayName: DevelopmentPlatformAdminSeedOptions.DefaultBillingViewerDisplayName,
            cancellationToken);

        await SeedProfileAsync(
            profileName: "NoBilling",
            account: _options.NoBilling,
            roleCode: PlatformRoleCodes.PlatformOpsNoBillingDev,
            defaultDisplayName: DevelopmentPlatformAdminSeedOptions.DefaultNoBillingDisplayName,
            cancellationToken);
    }

    private async Task SeedProfileAsync(
        string profileName,
        DevelopmentPlatformAdminAccountOptions account,
        string roleCode,
        string defaultDisplayName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(account.Email) || string.IsNullOrWhiteSpace(account.Password))
        {
            _logger.LogWarning(
                "Development Platform Admin seed profile {ProfileName} skipped: email and password must both be configured under {SectionName}.",
                profileName,
                DevelopmentPlatformAdminSeedOptions.SectionName);
            return;
        }

        var email = account.Email.Trim();
        var normalizedEmail = PlatformUser.NormalizeEmail(email);

        if (string.Equals(
                normalizedEmail,
                PlatformAdminSeedConstants.DevelopmentPlatformUserEmail,
                StringComparison.Ordinal))
        {
            _logger.LogError(
                "Development Platform Admin seed profile {ProfileName} skipped: configured email matches the protected development super administrator and must not be reused.",
                profileName);
            return;
        }

        var roles = await _userRepository.ResolveActiveRolesAsync(
            roleIds: null,
            roleCodes: [roleCode],
            cancellationToken);

        var role = roles.FirstOrDefault(item =>
            string.Equals(item.RoleCode, roleCode, StringComparison.Ordinal));

        if (role is null)
        {
            _logger.LogError(
                "Development Platform Admin seed profile {ProfileName} skipped: required role {RoleCode} was not found. Create the role with the expected permissions before seeding this account.",
                profileName,
                roleCode);
            return;
        }

        var displayName = string.IsNullOrWhiteSpace(account.DisplayName)
            ? defaultDisplayName
            : account.DisplayName.Trim();

        var now = _dateTimeProvider.UtcNow;
        var passwordHash = _passwordHashService.HashPassword(account.Password);
        var existing = await _userRepository.GetUserEntityByNormalizedEmailAsync(
            normalizedEmail,
            cancellationToken);

        if (existing is null)
        {
            var user = PlatformUser.Create(
                Guid.NewGuid(),
                email,
                passwordHash,
                PlatformAuthConstants.ActiveStatus,
                now);
            user.SetDisplayName(displayName, now);

            await _userRepository.AddUserWithRolesAsync(
                user,
                [role.Id],
                now,
                cancellationToken);

            _logger.LogInformation(
                "Development Platform Admin seed profile {ProfileName}: created login-capable account {Email} with role {RoleCode}.",
                profileName,
                email,
                roleCode);
            return;
        }

        if (existing.Id == PlatformAdminSeedConstants.DevelopmentPlatformUserId ||
            string.Equals(
                existing.NormalizedEmail,
                PlatformAdminSeedConstants.DevelopmentPlatformUserEmail,
                StringComparison.Ordinal))
        {
            _logger.LogError(
                "Development Platform Admin seed profile {ProfileName} skipped: matched the protected development super administrator account.",
                profileName);
            return;
        }

        var wasInvitePending = PlatformUserProtection.IsPendingInvite(existing);
        existing.SetPasswordHash(passwordHash, now);
        existing.SetStatus(PlatformAuthConstants.ActiveStatus, now);
        existing.SetDisplayName(displayName, now);
        await _userRepository.UpdateUserAsync(existing, cancellationToken);

        await _userRepository.ReplaceUserRolesAsync(
            existing.Id,
            [role.Id],
            now,
            actorPlatformUserId: null,
            cancellationToken);

        _logger.LogInformation(
            wasInvitePending
                ? "Development Platform Admin seed profile {ProfileName}: activated existing invite-pending account {Email} with role {RoleCode}."
                : "Development Platform Admin seed profile {ProfileName}: reconciled existing account {Email} with role {RoleCode}.",
            profileName,
            email,
            roleCode);
    }
}
