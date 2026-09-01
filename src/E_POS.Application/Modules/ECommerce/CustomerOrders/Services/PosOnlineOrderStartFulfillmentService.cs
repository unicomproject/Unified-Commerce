using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using Microsoft.Extensions.Logging;

namespace E_POS.Application.Modules.ECommerce.CustomerOrders.Services;

public sealed class PosOnlineOrderStartFulfillmentService : IPosOnlineOrderStartFulfillmentService
{
    public const string StartPermission = "commerce.online_order.fulfilment.start";

    private readonly IPosOnlineOrderStartFulfillmentRepository _repository;
    private readonly ITenantFeatureEntitlementEvaluator _entitlements;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<PosOnlineOrderStartFulfillmentService>? _logger;

    public PosOnlineOrderStartFulfillmentService(
        IPosOnlineOrderStartFulfillmentRepository repository,
        ITenantFeatureEntitlementEvaluator entitlements,
        IDateTimeProvider clock,
        ILogger<PosOnlineOrderStartFulfillmentService>? logger = null)
    {
        _repository = repository;
        _entitlements = entitlements;
        _clock = clock;
        _logger = logger;
    }

    public async Task<ApplicationResult<PosOnlineOrderStartFulfillmentResponse>> StartAsync(
        TenantRequestContext context,
        Guid outletId,
        Guid orderId,
        PosOnlineOrderStartFulfillmentRequest request,
        CancellationToken cancellationToken)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
            return Failure("online_orders.invalid_tenant_context", "Invalid tenant context.");
        if (!context.HasPermission(StartPermission))
            return Failure("online_orders.permission_denied", "Permission denied for starting fulfilment.");
        if (outletId == Guid.Empty)
            return Failure("online_orders.invalid_outlet", "A valid outlet id is required.");
        if (orderId == Guid.Empty)
            return Failure("online_orders.invalid_order_id", "A valid order id is required.");
        if (request.ExpectedVersion <= 0)
            return Failure("online_orders.invalid_expected_version", "A valid expected version is required.");

        var now = _clock.UtcNow;
        var entitlement = await _entitlements.EvaluateAsync(
            context.TenantId,
            PlatformTenantFeatureCodes.ClickCollect,
            now,
            cancellationToken);
        if (!entitlement.IsAllowed)
            return Failure("online_orders.feature_not_entitled", "Click & collect is not enabled for this tenant.");

        var result = await _repository.StartAsync(
            context.TenantId,
            context.UserId,
            outletId,
            orderId,
            request.ExpectedVersion,
            now,
            cancellationToken);

        _logger?.LogInformation(
            "POS StartFulfilment completed for TenantId {TenantId}, OutletId {OutletId}, OrderId {OrderId}, ActorId {ActorId}, Result {ResultCode}",
            context.TenantId,
            outletId,
            orderId,
            context.UserId,
            result.IsSuccess ? "SUCCESS" : result.ErrorCode);

        return result.IsSuccess
            ? ApplicationResult<PosOnlineOrderStartFulfillmentResponse>.Success(result.Response!)
            : Failure(result.ErrorCode!, MapError(result.ErrorCode!));
    }

    private static string MapError(string code) => code switch
    {
        "online_orders.outlet_access_denied" => "You do not have access to this outlet.",
        "online_orders.not_found" => "Online order was not found.",
        "online_orders.concurrency_conflict" => "The fulfilment changed after it was loaded. Refresh and try again.",
        "online_orders.invalid_state" => "The fulfilment cannot be started from its current state.",
        "online_orders.invalid_reservation" => "The order reservation is not valid for fulfilment.",
        _ => "Fulfilment could not be started."
    };

    private static ApplicationResult<PosOnlineOrderStartFulfillmentResponse> Failure(string code, string message) =>
        ApplicationResult<PosOnlineOrderStartFulfillmentResponse>.Failure(new ApplicationError(code, message));
}
