using System.Net.Mail;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.Customer.Contracts;
using E_POS.Application.Modules.ECommerce.Customer.Dtos;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
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

        var tillContext = await _tillSessionRepository.ResolveCurrentSessionAsync(
            context.TenantId,
            deviceId.Value,
            cancellationToken);

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
                customer.Status));
    }

    public async Task<ApplicationResult<PosCustomerListResponseDto>> ListAsync(
        TenantRequestContext context,
        Guid? deviceId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(CustomerPermissions.View))
        {
            return ApplicationResult<PosCustomerListResponseDto>.Failure(PermissionDenied);
        }

        if (!deviceId.HasValue || deviceId.Value == Guid.Empty)
        {
            return ApplicationResult<PosCustomerListResponseDto>.Failure(InvalidDeviceId);
        }

        var tillContext = await _tillSessionRepository.ResolveCurrentSessionAsync(
            context.TenantId,
            deviceId.Value,
            cancellationToken);

        if (!tillContext.IsSuccess)
        {
            return ApplicationResult<PosCustomerListResponseDto>.Failure(
                MapTillContextError(tillContext.ErrorCode));
        }

        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = pageSize < 1
            ? DefaultPageSize
            : Math.Min(pageSize, MaxPageSize);

        var response = await _repository.ListAsync(
            context.TenantId,
            search,
            normalizedPage,
            normalizedPageSize,
            cancellationToken);

        return ApplicationResult<PosCustomerListResponseDto>.Success(response);
    }

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

    private static ApplicationError? ValidateCreateRequest(PosCustomerCreateRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return new ApplicationError("pos_customers.name_required", "Customer name is required.");
        }

        if (request.FullName.Trim().Length > 150)
        {
            return new ApplicationError("pos_customers.name_too_long", "Customer name cannot exceed 150 characters.");
        }

        if (string.IsNullOrWhiteSpace(request.Phone))
        {
            return new ApplicationError("pos_customers.phone_required", "Customer phone number is required.");
        }

        var normalizedPhone = CustomerEntity.NormalizePhone(request.Phone);
        if (request.Phone.Trim().Length > 50 || normalizedPhone.Count(char.IsDigit) < 7)
        {
            return new ApplicationError("pos_customers.invalid_phone", "Enter a valid customer phone number.");
        }

        if (!string.IsNullOrWhiteSpace(request.Email) &&
            (request.Email.Trim().Length > 150 || !MailAddress.TryCreate(request.Email.Trim(), out _)))
        {
            return new ApplicationError("pos_customers.invalid_email", "Enter a valid customer email address.");
        }

        return null;
    }

    private static ApplicationResult<PosCustomerListItemResponseDto> Failure(string code, string message) =>
        ApplicationResult<PosCustomerListItemResponseDto>.Failure(new ApplicationError(code, message));
}
