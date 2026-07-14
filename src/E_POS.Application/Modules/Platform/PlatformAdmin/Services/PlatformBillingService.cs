using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Services;

public sealed class PlatformBillingService : IPlatformBillingService
{
    private readonly IPlatformBillingRepository _repository;
    private readonly IPlatformPermissionChecker _permissions;
    private readonly IDateTimeProvider _clock;

    public PlatformBillingService(IPlatformBillingRepository repository, IPlatformPermissionChecker permissions, IDateTimeProvider clock)
    {
        _repository = repository;
        _permissions = permissions;
        _clock = clock;
    }

    public async Task<ApplicationResult<PlatformBillingSummaryResponse>> GetSummaryAsync(PlatformBillingQuery query, Guid userId, CancellationToken ct)
    {
        var validation = Validate(query);
        return validation is not null
            ? ApplicationResult<PlatformBillingSummaryResponse>.Failure(validation)
            : await ReadAsync(userId, () => _repository.GetSummaryAsync(Normalize(query), _clock.UtcNow, ct), ct);
    }

    public async Task<ApplicationResult<PlatformBillingInvoiceListResponse>> GetInvoicesAsync(PlatformBillingQuery query, Guid userId, CancellationToken ct)
    {
        var validation = Validate(query);
        return validation is not null
            ? ApplicationResult<PlatformBillingInvoiceListResponse>.Failure(validation)
            : await ReadAsync(userId, () => _repository.GetInvoicesAsync(Normalize(query), _clock.UtcNow, ct), ct);
    }

    public async Task<ApplicationResult<PlatformBillingInvoiceDetailResponse>> GetInvoiceAsync(Guid id, Guid userId, CancellationToken ct)
    {
        if (!await CanView(userId, ct)) return Failure<PlatformBillingInvoiceDetailResponse>("access_denied", "Platform billing access denied.");
        var value = await _repository.GetInvoiceAsync(id, _clock.UtcNow, ct);
        return value is null ? Failure<PlatformBillingInvoiceDetailResponse>("invoice_not_found", "Invoice was not found.") : ApplicationResult<PlatformBillingInvoiceDetailResponse>.Success(value);
    }

    public async Task<ApplicationResult<IReadOnlyList<PlatformBillingPaymentDto>>> GetPaymentsAsync(Guid id, Guid userId, CancellationToken ct)
    {
        if (!await CanView(userId, ct)) return Failure<IReadOnlyList<PlatformBillingPaymentDto>>("access_denied", "Platform billing access denied.");
        var value = await _repository.GetPaymentsAsync(id, ct);
        return value is null ? Failure<IReadOnlyList<PlatformBillingPaymentDto>>("invoice_not_found", "Invoice was not found.") : ApplicationResult<IReadOnlyList<PlatformBillingPaymentDto>>.Success(value);
    }

    public async Task<ApplicationResult<PlatformBillingFilterOptionsResponse>> GetFilterOptionsAsync(Guid userId, CancellationToken ct)
        => await ReadAsync(userId, () => _repository.GetFilterOptionsAsync(ct), ct);

    public Task<ApplicationResult<PlatformBillingInvoiceDto>> IssueAsync(Guid id, PlatformBillingTransitionRequest request, Guid userId, CancellationToken ct)
        => MutateAsync(userId, () => _repository.IssueAsync(id, request.ExpectedUpdatedAt, _clock.UtcNow, ct), ct);

    public Task<ApplicationResult<PlatformBillingInvoiceDto>> MarkPaidAsync(Guid id, PlatformBillingMarkPaidRequest request, Guid userId, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        return MutateAsync(userId, () => _repository.MarkPaidAsync(id, request.ExpectedUpdatedAt, request.PaidAt ?? now, now, ct), ct);
    }

    private async Task<ApplicationResult<T>> ReadAsync<T>(Guid userId, Func<Task<T>> read, CancellationToken ct)
    {
        if (!await CanView(userId, ct)) return Failure<T>("access_denied", "Platform billing access denied.");
        return ApplicationResult<T>.Success(await read());
    }

    private async Task<ApplicationResult<PlatformBillingInvoiceDto>> MutateAsync(Guid userId, Func<Task<PlatformBillingMutationResult>> mutate, CancellationToken ct)
    {
        if (!await _permissions.HasPermissionAsync(userId, PlatformPermissionCodes.BillingManage, ct))
            return Failure<PlatformBillingInvoiceDto>("access_denied", "Platform billing management access denied.");

        var result = await mutate();
        return result.Outcome switch
        {
            PlatformBillingMutationOutcome.Success when result.Invoice is not null => ApplicationResult<PlatformBillingInvoiceDto>.Success(result.Invoice),
            PlatformBillingMutationOutcome.NotFound => Failure<PlatformBillingInvoiceDto>("invoice_not_found", "Invoice was not found."),
            PlatformBillingMutationOutcome.InvalidTransition => Failure<PlatformBillingInvoiceDto>("invalid_transition", "The invoice is not in a valid state for this action."),
            _ => Failure<PlatformBillingInvoiceDto>("concurrency_conflict", "The invoice changed. Reload it before trying again.")
        };
    }

    private Task<bool> CanView(Guid userId, CancellationToken ct) => _permissions.HasPermissionAsync(userId, PlatformPermissionCodes.BillingView, ct);
    private static ApplicationResult<T> Failure<T>(string code, string message) => ApplicationResult<T>.Failure(new ApplicationError($"platform_billing.{code}", message));
    private static PlatformBillingQuery Normalize(PlatformBillingQuery q) => q with { PageNumber = Math.Max(1, q.PageNumber), PageSize = Math.Clamp(q.PageSize, 1, 100) };

    private static ApplicationError? Validate(PlatformBillingQuery query)
    {
        if (query.DateFrom.HasValue && query.DateTo.HasValue && query.DateFrom > query.DateTo)
            return new("platform_billing.validation_failed", "Date from must be before or equal to date to.");

        var sort = query.SortBy.Trim().ToLowerInvariant();
        if (sort is not ("createdat" or "invoicenumber" or "tenant" or "amount" or "status" or "issuedat" or "dueat"))
            return new("platform_billing.validation_failed", "Unsupported billing sort field.");

        if (!query.SortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase) &&
            !query.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase))
            return new("platform_billing.validation_failed", "Sort direction must be asc or desc.");

        if (!query.DateField.Equals("issuedAt", StringComparison.OrdinalIgnoreCase) &&
            !query.DateField.Equals("dueAt", StringComparison.OrdinalIgnoreCase))
            return new("platform_billing.validation_failed", "Date field must be issuedAt or dueAt.");

        return null;
    }
}
