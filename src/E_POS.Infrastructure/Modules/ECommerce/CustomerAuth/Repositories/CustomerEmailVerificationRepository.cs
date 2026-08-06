using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Interfaces;
using E_POS.Domain.Modules.ECommerce.Customer.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.ECommerce.CustomerAuth.Repositories;

public sealed class CustomerEmailVerificationRepository : CustomerAuthRepositoryBase, ICustomerEmailVerificationRepository
{
    public CustomerEmailVerificationRepository(EPosDbContext dbContext) : base(dbContext)
    {
    }

    public Task<bool> TenantIsActiveAsync(Guid tenantId, CancellationToken cancellationToken) =>
        TenantIsActiveCoreAsync(tenantId, cancellationToken);

    public Task<CustomerLoginAccount?> FindAccountByEmailAsync(
        Guid tenantId,
        string normalizedEmail,
        bool trackAccount,
        CancellationToken cancellationToken) =>
        FindAccountByEmailCoreAsync(tenantId, normalizedEmail, trackAccount, cancellationToken);

    public async Task SaveEmailVerificationOtpAsync(
        CustomerVerificationOtp verificationOtp,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var transaction = await BeginSerializableTransactionAsync(cancellationToken);

        var pending = await DbContext.CustomerVerificationOtps
            .Where(x => x.TenantId == verificationOtp.TenantId &&
                        x.VerificationPurpose == verificationOtp.VerificationPurpose &&
                        x.NormalizedRecipientValue == verificationOtp.NormalizedRecipientValue &&
                        x.Status == "PENDING")
            .ToListAsync(cancellationToken);
        foreach (var existing in pending)
            existing.Invalidate(now);

        DbContext.CustomerVerificationOtps.Add(verificationOtp);
        await DbContext.SaveChangesAsync(cancellationToken);
        await CommitAsync(transaction, cancellationToken);
    }

    public async Task<CustomerEmailVerificationContext?> FindPendingEmailVerificationAsync(
        Guid tenantId,
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        return await (
            from otp in DbContext.CustomerVerificationOtps
            join customer in DbContext.Customers
                on new { otp.TenantId, CustomerId = otp.CustomerId }
                equals new { customer.TenantId, CustomerId = (Guid?)customer.Id }
            join authAccount in DbContext.CustomerAuthAccounts
                on new { customer.TenantId, CustomerId = customer.Id }
                equals new { authAccount.TenantId, authAccount.CustomerId }
            join tenant in DbContext.Tenants.AsNoTracking()
                on customer.TenantId equals tenant.Id
            where otp.TenantId == tenantId &&
                  otp.VerificationPurpose == "EMAIL_VERIFY" &&
                  otp.DeliveryChannel == "EMAIL" &&
                  otp.NormalizedRecipientValue == normalizedEmail &&
                  otp.Status == "PENDING" &&
                  customer.NormalizedEmail == normalizedEmail &&
                  customer.Status != "DELETED"
            orderby otp.SentAt descending
            select new CustomerEmailVerificationContext(
                otp,
                authAccount,
                customer.Email,
                customer.Name,
                customer.Status,
                tenant.Status))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task SaveEmailVerificationAsync(
        CustomerVerificationOtp verificationOtp,
        CustomerAuthAccount account,
        CancellationToken cancellationToken)
    {
        DbContext.CustomerVerificationOtps.Update(verificationOtp);
        DbContext.CustomerAuthAccounts.Update(account);
        await DbContext.SaveChangesAsync(cancellationToken);
    }
}