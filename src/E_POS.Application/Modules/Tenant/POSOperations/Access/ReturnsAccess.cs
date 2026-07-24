using E_POS.Application.Common.Models;
using E_POS.Domain.Modules.Tenant.POSOperations.Constants;

namespace E_POS.Application.Modules.Tenant.POSOperations.Access;

/// <summary>
/// Permission evaluation helpers for the POS Returns / Refunds / Exchanges flow.
/// </summary>
public static class ReturnsAccess
{
    /// <summary>
    /// Shared Returns workflow view (Steps 1–4 entry). Does not accept branch-only view codes.
    /// </summary>
    public static bool CanViewReturns(TenantRequestContext context)
    {
        return context.HasPermission(ReturnsPermissions.ViewReturns);
    }

    public static bool CanViewFlow(TenantRequestContext context)
    {
        return CanViewReturns(context)
            || context.HasPermission(ReturnsPermissions.ViewRefunds)
            || context.HasPermission(ReturnsPermissions.ViewExchanges);
    }

    public static bool CanCreateReturn(TenantRequestContext context)
    {
        return context.HasPermission(ReturnsPermissions.CreateReturn);
    }

    public static bool CanCreateRefund(TenantRequestContext context)
    {
        return context.HasPermission(ReturnsPermissions.CreateRefund)
            || context.HasPermission(ReturnsPermissions.CreateReturn);
    }

    public static bool CanCreateExchange(TenantRequestContext context)
    {
        return context.HasPermission(ReturnsPermissions.CreateExchange)
            || context.HasPermission(ReturnsPermissions.CreateReturn);
    }

    /// <summary>
    /// True when the caller may run mutating steps (preview/complete) for any branch.
    /// </summary>
    public static bool CanCompleteReturnOrExchange(TenantRequestContext context)
    {
        return context.HasPermission(ReturnsPermissions.CreateRefund)
            || context.HasPermission(ReturnsPermissions.CreateExchange)
            || context.HasPermission(ReturnsPermissions.CreateReturn);
    }

    public static bool CanApproveRefund(TenantRequestContext context)
    {
        return context.HasPermission(ReturnsPermissions.ApproveRefund);
    }

    /// <summary>
    /// Strict branch permission for saving a Refund resolution (no createReturn alias).
    /// </summary>
    public static bool CanSaveRefundResolution(TenantRequestContext context)
    {
        return CanViewReturns(context)
            && CanCreateReturn(context)
            && context.HasPermission(ReturnsPermissions.CreateRefund);
    }

    /// <summary>
    /// Strict branch permission for saving an Exchange resolution (no createReturn alias).
    /// </summary>
    public static bool CanSaveExchangeResolution(TenantRequestContext context)
    {
        return CanViewReturns(context)
            && CanCreateReturn(context)
            && context.HasPermission(ReturnsPermissions.CreateExchange);
    }

    /// <summary>
    /// Strict Refund branch processing (preview, methods, method save, refund details).
    /// Does not accept <see cref="CreateReturn"/> as an alias for <see cref="CreateRefund"/>.
    /// </summary>
    public static bool CanProcessRefund(TenantRequestContext context)
    {
        return CanViewReturns(context)
            && CanCreateReturn(context)
            && context.HasPermission(ReturnsPermissions.CreateRefund);
    }

    /// <summary>
    /// Strict Exchange branch processing (products, replacement, preview, exchange flow).
    /// Does not accept <see cref="CreateReturn"/> as an alias for <see cref="CreateExchange"/>.
    /// </summary>
    public static bool CanProcessExchange(TenantRequestContext context)
    {
        return CanViewReturns(context)
            && CanCreateReturn(context)
            && context.HasPermission(ReturnsPermissions.CreateExchange);
    }

    /// <summary>
    /// Immediate Step 10 Refund success requires the strict Refund processing triple.
    /// Historical reload may also use <c>receipts.view</c> with <see cref="CanViewReturns"/>.
    /// </summary>
    public static bool CanViewRefundSuccess(TenantRequestContext context)
    {
        return CanProcessRefund(context)
            || (CanViewReturns(context) && context.HasPermission(ReceiptPermissions.View));
    }

    /// <summary>
    /// Immediate Step 10 Exchange success requires the strict Exchange processing triple.
    /// Historical reload may also use <c>receipts.view</c> with <see cref="CanViewReturns"/>.
    /// </summary>
    public static bool CanViewExchangeSuccess(TenantRequestContext context)
    {
        return CanProcessExchange(context)
            || (CanViewReturns(context) && context.HasPermission(ReceiptPermissions.View));
    }
}
