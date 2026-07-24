using E_POS.Application.Modules.Tenant.POSOperations.Dtos;

namespace E_POS.Application.Modules.Tenant.POSOperations.Contracts;

public interface IPosReturnRepository
{
    bool SupportsInspectionDrafts => false;
    Task<PosReturnInspectionDraftRecord?> GetInspectionDraftBySaleAsync(
        Guid tenantId, Guid outletId, Guid saleId, CancellationToken cancellationToken) =>
        Task.FromResult<PosReturnInspectionDraftRecord?>(null);

    Task<PosReturnInspectionDraftSaveResult> SaveInspectionDraftAsync(
        Guid tenantId, Guid outletId, Guid saleId, Guid userId,
        IReadOnlyList<PosReturnInspectionDraftLineDto> lines, DateTimeOffset now,
        int? expectedVersion,
        CancellationToken cancellationToken) =>
        Task.FromException<PosReturnInspectionDraftSaveResult>(new NotSupportedException());

    Task<bool> MarkInspectionDraftValidatedAsync(
        Guid tenantId, Guid draftId, Guid userId, DateTimeOffset now,
        bool requiresInspection, bool requiresManagerApproval,
        CancellationToken cancellationToken) =>
        Task.FromResult(false);

    Task<PosReturnInspectionDraftRecord?> GetValidatedInspectionDraftForCompletionAsync(
        Guid tenantId, Guid outletId, Guid saleId, CancellationToken cancellationToken) =>
        Task.FromResult<PosReturnInspectionDraftRecord?>(null);

    Task<PosReturnResolutionSaveRepositoryResult> SaveResolutionAsync(
        Guid tenantId,
        Guid outletId,
        Guid saleId,
        Guid userId,
        string resolutionType,
        int expectedVersion,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        Task.FromResult(new PosReturnResolutionSaveRepositoryResult(null, "not_supported"));

    Task<PosReturnResolutionRecord?> GetResolutionAsync(
        Guid tenantId,
        Guid outletId,
        Guid saleId,
        CancellationToken cancellationToken) =>
        Task.FromResult<PosReturnResolutionRecord?>(null);

    Task<PosReturnRefundMethodsRepositoryResult> GetRefundMethodsAsync(
        Guid tenantId,
        Guid outletId,
        Guid saleId,
        bool hasOpenTillSession,
        CancellationToken cancellationToken) =>
        Task.FromResult(new PosReturnRefundMethodsRepositoryResult(null, "not_supported"));

    Task<PosReturnRefundMethodSaveRepositoryResult> SaveRefundMethodAsync(
        Guid tenantId,
        Guid outletId,
        Guid saleId,
        Guid userId,
        string methodCode,
        bool hasOpenTillSession,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        Task.FromResult(new PosReturnRefundMethodSaveRepositoryResult(null, "not_supported"));

    Task<PosReturnRefundMethodRecord?> GetSavedRefundMethodAsync(
        Guid tenantId,
        Guid outletId,
        Guid saleId,
        CancellationToken cancellationToken) =>
        Task.FromResult<PosReturnRefundMethodRecord?>(null);

    Task<PosExchangeReplacementSaveRepositoryResult> SaveExchangeReplacementAsync(
        Guid tenantId,
        Guid outletId,
        Guid saleId,
        Guid userId,
        IReadOnlyList<PosExchangeReplacementItemRequestDto> items,
        int expectedVersion,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        Task.FromResult(new PosExchangeReplacementSaveRepositoryResult(null, "not_supported"));

    Task<PosExchangeReplacementRepositoryResult> GetExchangeReplacementAsync(
        Guid tenantId,
        Guid outletId,
        Guid saleId,
        CancellationToken cancellationToken) =>
        Task.FromResult(new PosExchangeReplacementRepositoryResult(null, "not_supported"));

    Task<PosExchangePreviewRepositoryResult> PreviewExchangeAsync(
        Guid tenantId,
        Guid outletId,
        Guid saleId,
        string reasonCode,
        IReadOnlyList<PosReturnCreditPreviewLineRequestDto> lines,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        Task.FromResult(new PosExchangePreviewRepositoryResult(null, "not_supported"));

    Task<string?> GetSaleCurrencyCodeAsync(
        Guid tenantId,
        Guid saleId,
        CancellationToken cancellationToken) =>
        Task.FromResult<string?>(null);

    Task<PosReturnCompleteRepositoryResult> CompleteReturnAsync(
        Guid tenantId,
        Guid tenantUserId,
        PosReturnCompleteCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<PosReturnCompleteRepositoryResult> GetCompletionAsync(
        Guid tenantId,
        Guid outletId,
        Guid returnId,
        CancellationToken cancellationToken) =>
        Task.FromResult(new PosReturnCompleteRepositoryResult(null, "not_supported"));

    Task<PosReturnCreditPreviewRepositoryResult> PreviewCreditAsync(
        Guid tenantId,
        Guid saleId,
        string reasonCode,
        IReadOnlyList<PosReturnCreditPreviewLineRequestDto> lines,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<PosReturnSaleEligibilityDto?> GetSaleEligibilityAsync(
        Guid tenantId,
        Guid outletId,
        Guid saleId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<PosReturnSaleEligibilityCheckResult> CheckSelectedSaleEligibilityAsync(
        Guid tenantId,
        Guid outletId,
        Guid saleId,
        IReadOnlyList<PosReturnCreditPreviewLineRequestDto> lines,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PosReturnReasonOptionDto>> GetActiveReturnReasonsAsync(
        Guid tenantId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PosReturnInspectionConditionDto>> GetActiveInspectionConditionsAsync(
        Guid tenantId,
        CancellationToken cancellationToken);

    Task<bool> SaleLineBelongsToSaleAsync(
        Guid tenantId,
        Guid saleId,
        Guid saleLineId,
        CancellationToken cancellationToken);

    Task<bool> SaleBelongsToOutletAsync(
        Guid tenantId,
        Guid outletId,
        Guid saleId,
        CancellationToken cancellationToken) =>
        Task.FromResult(false);

    Task<PosReturnInspectionMediaStagingResult> SaveInspectionMediaStagingAsync(
        Guid tenantId,
        Guid outletId,
        Guid tenantUserId,
        Guid saleId,
        Guid saleLineId,
        Guid mediaId,
        string storageKey,
        string fileName,
        string contentType,
        long sizeBytes,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        Task.FromResult(new PosReturnInspectionMediaStagingResult(null, "not_supported"));

    Task<PosReturnInspectionMediaStagingRecord?> GetInspectionMediaStagingAsync(
        Guid tenantId,
        Guid outletId,
        Guid mediaId,
        CancellationToken cancellationToken) =>
        Task.FromResult<PosReturnInspectionMediaStagingRecord?>(null);

    Task<IReadOnlyList<PosReturnInspectionMediaStagingRecord>> GetInspectionMediaForSaleLineAsync(
        Guid tenantId,
        Guid outletId,
        Guid saleId,
        Guid saleLineId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<PosReturnInspectionMediaStagingRecord>>(Array.Empty<PosReturnInspectionMediaStagingRecord>());

    Task<PosReturnInspectionMediaDeleteResult> DeleteInspectionMediaStagingAsync(
        Guid tenantId,
        Guid outletId,
        Guid mediaId,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        Task.FromResult(new PosReturnInspectionMediaDeleteResult(false, null, "not_supported"));


    Task<PosReturnSaleSearchPageDto> SearchOriginalSalesAsync(
        Guid tenantId,
        Guid outletId,
        string searchType,
        string? search,
        PosReturnSaleSearchFilterDto filters,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<bool> IsActivePaymentMethodAsync(
        Guid tenantId,
        string paymentMethodCode,
        CancellationToken cancellationToken);
}

public sealed record PosReturnCreditPreviewRepositoryResult(
    PosReturnCreditPreviewDto? Preview,
    string? ErrorCode);

public sealed record PosReturnCompleteCommand(
    Guid SaleId,
    Guid DeviceId,
    Guid TillSessionId,
    Guid OutletId,
    Guid TillId,
    string ResolutionType,
    string ReasonCode,
    string SettlementMethodCode,
    string? Notes,
    IReadOnlyList<PosReturnCreditPreviewLineRequestDto> Lines,
    int ExpectedVersion,
    string IdempotencyKey);

public sealed record PosReturnCompleteRepositoryResult(
    PosReturnReceiptDto? Receipt,
    string? ErrorCode);

public sealed record PosReturnSaleEligibilityCheckResult(
    PosReturnSaleEligibilityDto? Eligibility,
    string? ErrorCode);

public sealed record PosReturnInspectionMediaStagingResult(
    PosReturnInspectionMediaStagingRecord? Media,
    string? ErrorCode);

public sealed record PosReturnInspectionMediaStagingRecord(
    Guid MediaId,
    Guid SaleId,
    Guid SaleLineId,
    string StorageKey,
    string FileName,
    string ContentType,
    long SizeBytes,
    string Status = "STAGED",
    Guid? DraftId = null,
    Guid? DraftLineId = null,
    DateTimeOffset? ExpiresAt = null,
    Guid? OutletId = null);

public sealed record PosReturnInspectionDraftLineRecord(
    Guid DraftLineId, Guid SaleLineId, Guid? ConditionId, string ConditionCode,
    string? Notes, string InspectionStatus, IReadOnlyList<Guid> MediaIds);

public sealed record PosReturnInspectionDraftRecord(
    Guid DraftId, string Status, DateTimeOffset? ValidatedAt,
    IReadOnlyList<PosReturnInspectionDraftLineRecord> Lines,
    string? ResolutionType = null,
    DateTimeOffset? ResolutionSelectedAt = null,
    Guid? ResolutionSelectedByTenantUserId = null,
    int Version = 1,
    DateTimeOffset? ExpiresAt = null,
    bool RequiresInspection = false,
    bool RequiresManagerApproval = false);

public sealed record PosReturnInspectionDraftSaveResult(
    PosReturnInspectionDraftRecord? Draft,
    string? ErrorCode);

public sealed record PosReturnInspectionMediaDeleteResult(
    bool Deleted,
    string? StorageKey,
    string? ErrorCode);

public sealed record PosReturnResolutionRecord(
    Guid SaleId,
    Guid DraftId,
    string? ResolutionType,
    DateTimeOffset? ResolutionSelectedAt,
    Guid? ResolutionSelectedByTenantUserId,
    int Version,
    string DraftStatus,
    DateTimeOffset ExpiresAt,
    bool RequiresInspection,
    bool RequiresManagerApproval,
    bool CanChange);

public sealed record PosReturnResolutionSaveRepositoryResult(
    PosReturnResolutionRecord? Resolution,
    string? ErrorCode);

public sealed record PosReturnRefundMethodRecord(
    Guid SaleId,
    string MethodCode,
    DateTimeOffset SelectedAt);

public sealed record PosReturnRefundMethodsRepositoryResult(
    PosReturnRefundMethodsResponseDto? Methods,
    string? ErrorCode);

public sealed record PosReturnRefundMethodSaveRepositoryResult(
    PosReturnRefundMethodRecord? Method,
    string? ErrorCode);

public sealed record PosExchangeReplacementSaveRepositoryResult(
    PosExchangeReplacementSaveResponseDto? Replacement,
    string? ErrorCode);

public sealed record PosExchangeReplacementRepositoryResult(
    PosExchangeReplacementSaveResponseDto? Replacement,
    string? ErrorCode);

public sealed record PosExchangePreviewRepositoryResult(
    PosExchangePreviewDto? Preview,
    string? ErrorCode);
