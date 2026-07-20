using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;

namespace E_POS.Application.Modules.ECommerce.CustomerOrders.Services;

public sealed class CustomerOrderService : ICustomerOrderService
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 50;
    private const int MaxCancelReasonLength = 500;

    private readonly ICustomerOrderRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CustomerOrderService(
        ICustomerOrderRepository repository,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationResult<CustomerOrderListReadModel>> GetAsync(
        Guid tenantId,
        Guid customerId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty || customerId == Guid.Empty)
        {
            return ApplicationResult<CustomerOrderListReadModel>.Failure(
                Error("customer_orders.invalid_customer_context", "A valid customer session is required."));
        }

        if (!TryNormalizeStatus(status, out var normalizedStatus))
        {
            return ApplicationResult<CustomerOrderListReadModel>.Failure(
                Error("customer_orders.invalid_status", "Invalid order status filter."));
        }

        var safePage = page <= 0 ? DefaultPage : page;
        var safePageSize = pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

        var orders = await _repository.GetAsync(
            tenantId,
            customerId,
            normalizedStatus,
            safePage,
            safePageSize,
            cancellationToken);

        return ApplicationResult<CustomerOrderListReadModel>.Success(orders);
    }

    public async Task<ApplicationResult<CustomerOrderDetailReadModel>> GetDetailAsync(
        Guid tenantId,
        Guid customerId,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty || customerId == Guid.Empty)
        {
            return ApplicationResult<CustomerOrderDetailReadModel>.Failure(
                Error("customer_orders.invalid_customer_context", "A valid customer session is required."));
        }

        if (orderId == Guid.Empty)
        {
            return ApplicationResult<CustomerOrderDetailReadModel>.Failure(
                Error("customer_orders.invalid_order_id", "A valid order id is required."));
        }

        var order = await _repository.GetDetailAsync(
            tenantId,
            customerId,
            orderId,
            cancellationToken);

        return order is null
            ? ApplicationResult<CustomerOrderDetailReadModel>.Failure(
                Error("customer_orders.not_found", "Order was not found."))
            : ApplicationResult<CustomerOrderDetailReadModel>.Success(order);
    }

    public async Task<ApplicationResult<CustomerOrderCancelResponse>> CancelAsync(
        Guid tenantId,
        Guid customerId,
        Guid orderId,
        CustomerOrderCancelRequest request,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty || customerId == Guid.Empty)
        {
            return ApplicationResult<CustomerOrderCancelResponse>.Failure(
                Error("customer_orders.invalid_customer_context", "A valid customer session is required."));
        }

        if (orderId == Guid.Empty)
        {
            return ApplicationResult<CustomerOrderCancelResponse>.Failure(
                Error("customer_orders.invalid_order_id", "A valid order id is required."));
        }

        var reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
        if (reason?.Length > MaxCancelReasonLength)
        {
            return ApplicationResult<CustomerOrderCancelResponse>.Failure(
                Error("customer_orders.invalid_cancel_reason", $"Cancel reason cannot exceed {MaxCancelReasonLength} characters."));
        }

        var result = await _repository.CancelAsync(
            tenantId,
            customerId,
            orderId,
            reason,
            _dateTimeProvider.UtcNow,
            cancellationToken);

        return result.IsSuccess
            ? ApplicationResult<CustomerOrderCancelResponse>.Success(result.Response!)
            : ApplicationResult<CustomerOrderCancelResponse>.Failure(
                Error(result.ErrorCode!, result.ErrorMessage ?? MapCancelErrorMessage(result.ErrorCode!)));
    }

    private static bool TryNormalizeStatus(string? status, out string? normalizedStatus)
    {
        normalizedStatus = null;

        if (string.IsNullOrWhiteSpace(status) ||
            string.Equals(status.Trim(), "all", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        normalizedStatus = status.Trim().Replace('-', '_').ToUpperInvariant() switch
        {
            "PENDING" or "PENDING_CONFIRMATION" => "PENDING_CONFIRMATION",
            "ACCEPTED" => "ACCEPTED",
            "PREPARING" => "PREPARING",
            "READY" or "READY_FOR_COLLECTION" => "READY_FOR_COLLECTION",
            "COMPLETED" => "COMPLETED",
            "CANCELLED" or "CANCELED" => "CANCELLED",
            _ => null
        };

        return normalizedStatus is not null;
    }

    private static string MapCancelErrorMessage(string code) => code switch
    {
        "customer_orders.not_found" => "Order was not found.",
        "customer_orders.invalid_transition" => "Order cannot be cancelled in its current status.",
        _ => "Order could not be cancelled."
    };

    private static ApplicationError Error(string code, string message) => new(code, message);
}
