using E_POS.Domain.Modules.Tenant.Payment.Entities;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Persistence.Seed;

public static class TenantPaymentMethodSeedApplicator
{
    public static async Task ApplyAsync(
        EPosDbContext dbContext,
        Guid tenantId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        await UpsertMethodAsync(
            dbContext,
            tenantId,
            TenantPaymentMethodSeedConstants.CashMethodCode,
            TenantPaymentMethodSeedConstants.CashMethodName,
            TenantPaymentMethodSeedConstants.MethodTypeCash,
            isActiveForPos: true,
            allowsChange: true,
            sortOrder: 1,
            now,
            cancellationToken);

        await UpsertMethodAsync(
            dbContext,
            tenantId,
            TenantPaymentMethodSeedConstants.CardMethodCode,
            TenantPaymentMethodSeedConstants.CardMethodName,
            TenantPaymentMethodSeedConstants.MethodTypeCard,
            isActiveForPos: true,
            allowsChange: false,
            sortOrder: 2,
            now,
            cancellationToken);

        await UpsertMethodAsync(
            dbContext,
            tenantId,
            TenantPaymentMethodSeedConstants.LankaQrMethodCode,
            TenantPaymentMethodSeedConstants.LankaQrMethodName,
            TenantPaymentMethodSeedConstants.MethodTypeQr,
            isActiveForPos: true,
            allowsChange: false,
            sortOrder: 3,
            now,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task UpsertMethodAsync(
        EPosDbContext dbContext,
        Guid tenantId,
        string methodCode,
        string methodName,
        string methodType,
        bool isActiveForPos,
        bool allowsChange,
        int sortOrder,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var method = await dbContext.PaymentMethods
            .SingleOrDefaultAsync(m => m.TenantId == tenantId && m.MethodCode == methodCode, cancellationToken);

        if (method is null)
        {
            dbContext.PaymentMethods.Add(PaymentMethod.Create(
                id: Guid.NewGuid(),
                tenantId: tenantId,
                methodCode: methodCode,
                methodName: methodName,
                methodType: methodType,
                isActiveForPos: isActiveForPos,
                allowsChange: allowsChange,
                sortOrder: sortOrder,
                status: TenantPaymentMethodSeedConstants.ActiveStatus,
                createdByTenantUserId: null,
                now: now));
            return;
        }

        dbContext.Entry(method).Property(nameof(PaymentMethod.MethodName)).CurrentValue = methodName;
        dbContext.Entry(method).Property(nameof(PaymentMethod.MethodType)).CurrentValue = methodType;
        dbContext.Entry(method).Property(nameof(PaymentMethod.IsActiveForPos)).CurrentValue = isActiveForPos;
        dbContext.Entry(method).Property(nameof(PaymentMethod.AllowsChange)).CurrentValue = allowsChange;
        dbContext.Entry(method).Property(nameof(PaymentMethod.SortOrder)).CurrentValue = sortOrder;
        dbContext.Entry(method).Property(nameof(PaymentMethod.Status)).CurrentValue = TenantPaymentMethodSeedConstants.ActiveStatus;
        dbContext.Entry(method).Property(nameof(PaymentMethod.UpdatedAt)).CurrentValue = now;
    }
}
