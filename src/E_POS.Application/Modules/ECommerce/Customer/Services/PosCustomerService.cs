using System.Net.Mail;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.Customer.Contracts;
using E_POS.Application.Modules.ECommerce.Customer.Dtos;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.Orders.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using CustomerEntity = E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer;

namespace E_POS.Application.Modules.ECommerce.Customer.Services;

public sealed class PosCustomerService : IPosCustomerService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;
    private const string CustomerCodeSequenceKey = "CUSTOMER_CODE";
    private const string CustomerCodePrefix = "CUS";
    private const int CustomerCodePaddingLength = 6;

    private static readonly ApplicationError PermissionDenied = new(
        "pos_customers.permission_denied",
        "You do not have permission to view customers.");

    private static readonly ApplicationError InvalidDeviceId = new(
        "pos_customers.invalid_device_id",
        "Device id is required.");

    private readonly IPosCustomerRepository _repository;
    private readonly IPosTillSessionRepository _tillSessionRepository;
    private readonly ICodeSequenceRepository _codeSequenceRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PosCustomerService(
        IPosCustomerRepository repository,
        IPosTillSessionRepository tillSessionRepository,
        ICodeSequenceRepository codeSequenceRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _tillSessionRepository = tillSessionRepository;
        _codeSequenceRepository = codeSequenceRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationResult<PosCustomerListItemResponseDto>> CreateAsync(
        TenantRequestContext context,
        Guid? deviceId,
        PosCustomerCreateRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(CustomerPermissions.Create))
        {
            return Failure("pos_customers.create_permission_denied", "You do not have permission to create customers.");
        }

        if (!deviceId.HasValue || deviceId.Value == Guid.Empty)
        {
            return ApplicationResult<PosCustomerListItemResponseDto>.Failure(InvalidDeviceId);
        }

        var validationError = ValidateCreateRequest(request);
        if (validationError is not null)
        {
            return ApplicationResult<PosCustomerListItemResponseDto>.Failure(validationError);
        }

        var tillContext = await ResolveTillAsync(context.TenantId, deviceId.Value, cancellationToken);
        if (!tillContext.IsSuccess)
        {
            return ApplicationResult<PosCustomerListItemResponseDto>.Failure(
                MapTillContextError(tillContext.ErrorCode));
        }

        var normalizedPhone = CustomerEntity.NormalizePhone(request.Phone);
        if (await _repository.NormalizedPhoneExistsAsync(
                context.TenantId,
                normalizedPhone,
                cancellationToken))
        {
            return Failure("pos_customers.duplicate_phone", "A customer with this phone number already exists.");
        }

        var normalizedEmail = CustomerEntity.NormalizeEmail(request.Email);
        if (normalizedEmail is not null && await _repository.NormalizedEmailExistsAsync(
                context.TenantId,
                normalizedEmail,
                cancellationToken))
        {
            return Failure("pos_customers.duplicate_email", "A customer with this email address already exists.");
        }

        var now = _dateTimeProvider.UtcNow;
        var customerCode = await _codeSequenceRepository.GetNextCodeAsync(
            context.TenantId,
            CustomerCodeSequenceKey,
            CustomerCodePrefix,
            CustomerCodePaddingLength,
            now,
            cancellationToken);
        var customer = CustomerEntity.CreatePosCustomer(
            Guid.NewGuid(),
            context.TenantId,
            customerCode,
            request.FullName,
            request.Phone,
            request.Email,
            context.UserId,
            now);

        if (!await _repository.AddAsync(customer, cancellationToken))
        {
            return Failure("pos_customers.duplicate_contact", "A customer with the same phone, email, or customer code already exists.");
        }

        return ApplicationResult<PosCustomerListItemResponseDto>.Success(
            new PosCustomerListItemResponseDto(
                customer.Id,
                customer.Name,
                customer.Phone,
                customer.Email,
                customer.Status,
                customer.CustomerCode,
                customer.SourceType,
                customer.CreatedAt));
    }

    public async Task<ApplicationResult<PosCustomerListItemResponseDto>> UpdateAsync(
        TenantRequestContext context,
        Guid? deviceId,
        Guid customerId,
        PosCustomerUpdateRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(CustomerPermissions.Update))
        {
            return Failure(
                "pos_customers.update_permission_denied",
                "You do not have permission to update customers.");
        }

        if (customerId == Guid.Empty)
        {
            return Failure("pos_customers.invalid_customer_id", "Customer id is required.");
        }

        if (!deviceId.HasValue || deviceId.Value == Guid.Empty)
        {
            return ApplicationResult<PosCustomerListItemResponseDto>.Failure(InvalidDeviceId);
        }

        var validationError = ValidateUpdateRequest(request);
        if (validationError is not null)
        {
            return ApplicationResult<PosCustomerListItemResponseDto>.Failure(validationError);
        }

        var tillContext = await ResolveTillAsync(context.TenantId, deviceId.Value, cancellationToken);
        if (!tillContext.IsSuccess)
        {
            return ApplicationResult<PosCustomerListItemResponseDto>.Failure(
                MapTillContextError(tillContext.ErrorCode));
        }

        var customer = await _repository.GetTrackedByIdAsync(
            context.TenantId,
            customerId,
            cancellationToken);
        if (customer is null)
        {
            return Failure("pos_customers.customer_not_found", "Customer could not be found.");
        }

        var normalizedPhone = CustomerEntity.NormalizePhone(request.Phone);
        if (await _repository.NormalizedPhoneExistsAsync(
                context.TenantId,
                normalizedPhone,
                customerId,
                cancellationToken))
        {
            return Failure("pos_customers.duplicate_phone", "A customer with this phone number already exists.");
        }

        var normalizedEmail = CustomerEntity.NormalizeEmail(request.Email);
        if (normalizedEmail is not null && await _repository.NormalizedEmailExistsAsync(
                context.TenantId,
                normalizedEmail,
                customerId,
                cancellationToken))
        {
            return Failure("pos_customers.duplicate_email", "A customer with this email address already exists.");
        }

        var status = request.Status.Trim().ToUpperInvariant();
        customer.UpdatePosProfile(
            request.FullName,
            request.Phone,
            request.Email,
            status,
            context.UserId,
            _dateTimeProvider.UtcNow);

        if (!await _repository.UpdateAsync(customer, cancellationToken))
        {
            return Failure(
                "pos_customers.duplicate_contact",
                "A customer with the same phone, email, or customer code already exists.");
        }

        var updated = await _repository.GetByIdAsync(context.TenantId, customerId, cancellationToken);
        return updated is null
            ? Failure("pos_customers.customer_not_found", "Customer could not be found.")
            : ApplicationResult<PosCustomerListItemResponseDto>.Success(updated);
    }

    public async Task<ApplicationResult<PosCustomerListResponseDto>> ListAsync(
        TenantRequestContext context,
        Guid? deviceId,
        string? search,
        string? status,
        string? source,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(CustomerPermissions.View))
        {
            return ApplicationResult<PosCustomerListResponseDto>.Failure(PermissionDenied);
        }

        var tillError = await EnsureDeviceTillAsync(context.TenantId, deviceId, cancellationToken);
        if (tillError is not null)
        {
            return ApplicationResult<PosCustomerListResponseDto>.Failure(tillError);
        }

        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = pageSize < 1
            ? DefaultPageSize
            : Math.Min(pageSize, MaxPageSize);

        var response = await _repository.ListAsync(
            context.TenantId,
            search,
            status,
            source,
            normalizedPage,
            normalizedPageSize,
            cancellationToken);

        return ApplicationResult<PosCustomerListResponseDto>.Success(response);
    }

    public async Task<ApplicationResult<PosCustomerSummaryResponseDto>> GetSummaryAsync(
        TenantRequestContext context,
        Guid? deviceId,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(CustomerPermissions.View))
        {
            return ApplicationResult<PosCustomerSummaryResponseDto>.Failure(PermissionDenied);
        }

        var tillError = await EnsureDeviceTillAsync(context.TenantId, deviceId, cancellationToken);
        if (tillError is not null)
        {
            return ApplicationResult<PosCustomerSummaryResponseDto>.Failure(tillError);
        }

        var tenantTimezone = await _repository.GetTenantDefaultTimezoneAsync(
            context.TenantId,
            cancellationToken);
        var (monthStartUtc, monthEndUtc, timeZoneId) = ResolveTenantLocalMonthBounds(
            _dateTimeProvider.UtcNow,
            tenantTimezone);

        var summary = await _repository.GetSummaryAsync(
            context.TenantId,
            monthStartUtc,
            monthEndUtc,
            timeZoneId,
            cancellationToken);

        return ApplicationResult<PosCustomerSummaryResponseDto>.Success(summary);
    }

    public async Task<ApplicationResult<PosCustomerListItemResponseDto>> GetByIdAsync(
        TenantRequestContext context,
        Guid? deviceId,
        Guid customerId,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(CustomerPermissions.View))
        {
            return ApplicationResult<PosCustomerListItemResponseDto>.Failure(PermissionDenied);
        }

        if (customerId == Guid.Empty)
        {
            return Failure("pos_customers.invalid_customer_id", "Customer id is required.");
        }

        var tillError = await EnsureDeviceTillAsync(context.TenantId, deviceId, cancellationToken);
        if (tillError is not null)
        {
            return ApplicationResult<PosCustomerListItemResponseDto>.Failure(tillError);
        }

        var customer = await _repository.GetByIdAsync(context.TenantId, customerId, cancellationToken);
        if (customer is null)
        {
            return Failure("pos_customers.customer_not_found", "Customer could not be found.");
        }

        return ApplicationResult<PosCustomerListItemResponseDto>.Success(customer);
    }

    public async Task<ApplicationResult<PosCustomerOrdersResponseDto>> GetOrdersAsync(
        TenantRequestContext context,
        Guid? deviceId,
        Guid customerId,
        int page,
        int pageSize,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        string? status,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(CustomerPermissions.View))
        {
            return ApplicationResult<PosCustomerOrdersResponseDto>.Failure(PermissionDenied);
        }

        if (customerId == Guid.Empty)
        {
            return ApplicationResult<PosCustomerOrdersResponseDto>.Failure(
                new ApplicationError("pos_customers.invalid_customer_id", "Customer id is required."));
        }

        var tillError = await EnsureDeviceTillAsync(context.TenantId, deviceId, cancellationToken);
        if (tillError is not null)
        {
            return ApplicationResult<PosCustomerOrdersResponseDto>.Failure(tillError);
        }

        var customer = await _repository.GetByIdAsync(context.TenantId, customerId, cancellationToken);
        if (customer is null)
        {
            return ApplicationResult<PosCustomerOrdersResponseDto>.Failure(
                new ApplicationError("pos_customers.customer_not_found", "Customer could not be found."));
        }

        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = pageSize < 1
            ? DefaultPageSize
            : Math.Min(pageSize, MaxPageSize);

        var response = await _repository.GetOrdersAsync(
            context.TenantId,
            customerId,
            normalizedPage,
            normalizedPageSize,
            fromDate,
            toDate,
            status,
            cancellationToken);

        return ApplicationResult<PosCustomerOrdersResponseDto>.Success(response);
    }

    public async Task<ApplicationResult<PosCustomerAttachToSaleResponseDto>> AttachToSaleAsync(
        TenantRequestContext context,
        Guid? deviceId,
        Guid customerId,
        PosCustomerAttachToSaleRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(CustomerPermissions.View) ||
            !context.HasPermission(SalesPermissions.Cart.Manage))
        {
            return ApplicationResult<PosCustomerAttachToSaleResponseDto>.Failure(
                new ApplicationError(
                    "pos_customers.attach_permission_denied",
                    "You do not have permission to attach a customer to a sale."));
        }

        if (customerId == Guid.Empty)
        {
            return ApplicationResult<PosCustomerAttachToSaleResponseDto>.Failure(
                new ApplicationError("pos_customers.invalid_customer_id", "Customer id is required."));
        }

        if (!deviceId.HasValue || deviceId.Value == Guid.Empty)
        {
            return ApplicationResult<PosCustomerAttachToSaleResponseDto>.Failure(InvalidDeviceId);
        }

        var tillContext = await ResolveTillAsync(context.TenantId, deviceId.Value, cancellationToken);
        if (!tillContext.IsSuccess || tillContext.Snapshot is null)
        {
            return ApplicationResult<PosCustomerAttachToSaleResponseDto>.Failure(
                MapTillContextError(tillContext.ErrorCode));
        }

        var status = await _repository.GetCustomerStatusAsync(
            context.TenantId,
            customerId,
            cancellationToken);
        if (status is null)
        {
            return ApplicationResult<PosCustomerAttachToSaleResponseDto>.Failure(
                new ApplicationError("pos_customers.customer_not_found", "Customer could not be found."));
        }

        var statusError = MapNonActiveCustomerError(status);
        if (statusError is not null)
        {
            return ApplicationResult<PosCustomerAttachToSaleResponseDto>.Failure(statusError);
        }

        var customer = await _repository.GetByIdAsync(context.TenantId, customerId, cancellationToken);
        if (customer is null)
        {
            return ApplicationResult<PosCustomerAttachToSaleResponseDto>.Failure(
                new ApplicationError("pos_customers.customer_not_found", "Customer could not be found."));
        }

        Guid? attachedSaleId = null;
        var attachmentMode = "CART_CONTEXT";
        if (request.SaleId is { } saleId && saleId != Guid.Empty)
        {
            var assigned = await _repository.TryAssignCustomerToEditableSaleAsync(
                context.TenantId,
                saleId,
                customer.CustomerId,
                customer.FullName,
                tillContext.Snapshot.SessionId,
                context.UserId,
                _dateTimeProvider.UtcNow,
                cancellationToken);
            if (!assigned)
            {
                return ApplicationResult<PosCustomerAttachToSaleResponseDto>.Failure(
                    new ApplicationError(
                        "pos_customers.sale_not_editable",
                        "The sale could not be updated with this customer."));
            }

            attachedSaleId = saleId;
            attachmentMode = "SALE_ASSIGNED";
        }

        return ApplicationResult<PosCustomerAttachToSaleResponseDto>.Success(
            new PosCustomerAttachToSaleResponseDto(
                customer.CustomerId,
                customer.FullName,
                customer.Phone,
                customer.Email,
                customer.Status,
                customer.CustomerCode,
                attachedSaleId,
                attachmentMode,
                true));
    }

    public static (DateTimeOffset MonthStartUtc, DateTimeOffset MonthEndUtc, string TimeZoneId)
        ResolveTenantLocalMonthBounds(DateTimeOffset utcNow, string? tenantTimezone)
    {
        var timeZoneId = string.IsNullOrWhiteSpace(tenantTimezone)
            ? OutletConstants.DefaultTimezone
            : tenantTimezone.Trim();

        TimeZoneInfo timeZone;
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            timeZone = TimeZoneInfo.Utc;
            timeZoneId = OutletConstants.DefaultTimezone;
        }
        catch (InvalidTimeZoneException)
        {
            timeZone = TimeZoneInfo.Utc;
            timeZoneId = OutletConstants.DefaultTimezone;
        }

        var localNow = TimeZoneInfo.ConvertTime(utcNow, timeZone);
        var localMonthStart = new DateTime(localNow.Year, localNow.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var localMonthEnd = localMonthStart.AddMonths(1);
        var monthStartUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localMonthStart, timeZone));
        var monthEndUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localMonthEnd, timeZone));
        return (monthStartUtc, monthEndUtc, timeZoneId);
    }

    private async Task<ApplicationError?> EnsureDeviceTillAsync(
        Guid tenantId,
        Guid? deviceId,
        CancellationToken cancellationToken)
    {
        if (!deviceId.HasValue || deviceId.Value == Guid.Empty)
        {
            return InvalidDeviceId;
        }

        var tillContext = await ResolveTillAsync(tenantId, deviceId.Value, cancellationToken);
        return tillContext.IsSuccess ? null : MapTillContextError(tillContext.ErrorCode);
    }

    private Task<CurrentTillSessionResolveResult> ResolveTillAsync(
        Guid tenantId,
        Guid deviceId,
        CancellationToken cancellationToken) =>
        _tillSessionRepository.ResolveCurrentSessionAsync(tenantId, deviceId, cancellationToken);

    private static ApplicationError MapTillContextError(string? errorCode) => errorCode switch
    {
        "till_session.device_not_found" => new ApplicationError(
            "pos_customers.device_not_found",
            "POS device could not be found."),
        "till_session.device_not_trusted" => new ApplicationError(
            "pos_customers.device_not_trusted",
            "This POS device is not trusted."),
        "till_session.till_not_assigned" => new ApplicationError(
            "pos_customers.till_not_assigned",
            "No till is assigned to this POS device."),
        _ => new ApplicationError(
            "pos_customers.open_till_required",
            "An open till session is required to load customers.")
    };

    private static ApplicationError? MapNonActiveCustomerError(string status)
    {
        var normalized = status.Trim().ToUpperInvariant();
        return normalized switch
        {
            "ACTIVE" => null,
            "INACTIVE" => new ApplicationError(
                "pos_customers.customer_inactive",
                "Only active customers can be attached to a sale."),
            "BLOCKED" => new ApplicationError(
                "pos_customers.customer_blocked",
                "Blocked customers cannot be attached to a sale."),
            "DELETED" => new ApplicationError(
                "pos_customers.customer_deleted",
                "Deleted customers cannot be attached to a sale."),
            _ => new ApplicationError(
                "pos_customers.customer_not_eligible",
                "This customer is not eligible for a sale.")
        };
    }

    private static ApplicationError? ValidateCreateRequest(PosCustomerCreateRequestDto request) =>
        ValidateProfileFields(request.FullName, request.Phone, request.Email, requireStatus: false, status: null);

    private static ApplicationError? ValidateUpdateRequest(PosCustomerUpdateRequestDto request) =>
        ValidateProfileFields(request.FullName, request.Phone, request.Email, requireStatus: true, status: request.Status);

    private static ApplicationError? ValidateProfileFields(
        string fullName,
        string phone,
        string? email,
        bool requireStatus,
        string? status)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return new ApplicationError("pos_customers.name_required", "Customer name is required.");
        }

        if (fullName.Trim().Length > 150)
        {
            return new ApplicationError("pos_customers.name_too_long", "Customer name cannot exceed 150 characters.");
        }

        if (string.IsNullOrWhiteSpace(phone))
        {
            return new ApplicationError("pos_customers.phone_required", "Customer phone number is required.");
        }

        var normalizedPhone = CustomerEntity.NormalizePhone(phone);
        if (phone.Trim().Length > 50 || normalizedPhone.Count(char.IsDigit) < 7)
        {
            return new ApplicationError("pos_customers.invalid_phone", "Enter a valid customer phone number.");
        }

        if (!string.IsNullOrWhiteSpace(email) &&
            (email.Trim().Length > 150 || !MailAddress.TryCreate(email.Trim(), out _)))
        {
            return new ApplicationError("pos_customers.invalid_email", "Enter a valid customer email address.");
        }

        if (requireStatus)
        {
            if (string.IsNullOrWhiteSpace(status) || !CustomerEntity.IsAllowedPosStatus(status))
            {
                return new ApplicationError(
                    "pos_customers.invalid_customer_status",
                    "Customer status must be ACTIVE, INACTIVE, or BLOCKED.");
            }
        }

        return null;
    }

    private static ApplicationResult<PosCustomerListItemResponseDto> Failure(string code, string message) =>
        ApplicationResult<PosCustomerListItemResponseDto>.Failure(new ApplicationError(code, message));
}
