using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.Discount.Contracts;
using E_POS.Application.Modules.Tenant.Discount.Dtos;
using E_POS.Application.Modules.Tenant.Discount.Services;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using E_POS.Domain.Modules.Tenant.Orders.Constants;
using Xunit;

namespace E_POS.UnitTests.Discount;

public sealed class PosDiscountServiceTests
{
    [Fact]
    public async Task Validate_WhenRequestedValueWithinCashierLimit_ReturnsDirectApply()
    {
        var service = CreateService(maxPercentage: 10, absoluteLimit: 50);
        var result = await service.ValidateAsync(Context(SalesPermissions.Discount.Apply), Request(5), default);

        Assert.True(result.Value!.IsValid);
        Assert.Equal("direct_apply", result.Value.Outcome);
        Assert.False(result.Value.RequiresManagerApproval);
    }

    [Fact]
    public async Task Apply_WhenRequestedValueAboveCashierWithinPolicy_CreatesPendingApplication()
    {
        var repository = new FakeDiscountRepository(maxPercentage: 10, absoluteLimit: 50);
        var service = CreateService(repository);
        var result = await service.ApplyAsync(Context(SalesPermissions.Discount.Apply), Request(20, "apply-key"), default);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.Applied);
        Assert.Equal("pending_approval", result.Value.Status);
        Assert.NotNull(repository.CreatedCommand);
        Assert.True(repository.CreatedCommand!.RequiresManagerApproval);
    }

    [Fact]
    public async Task Apply_WhenRequestedValueExceedsAbsolutePolicyLimit_RejectsWithoutPersistence()
    {
        var repository = new FakeDiscountRepository(maxPercentage: 10, absoluteLimit: 30);
        var service = CreateService(repository);
        var result = await service.ApplyAsync(Context(SalesPermissions.Discount.Apply), Request(40, "apply-key"), default);

        Assert.False(result.Value!.Applied);
        Assert.Equal("rejected", result.Value.Status);
        Assert.Null(repository.CreatedCommand);
    }

    [Fact]
    public async Task Apply_ManualLineDiscount_UsesLineScopeAndTargetVariant()
    {
        var repository = new FakeDiscountRepository(
            maxPercentage: 10,
            absoluteLimit: 50,
            policyScope: "LINE");
        var service = CreateService(repository);
        var variantId = Guid.NewGuid();

        var result = await service.ApplyAsync(
            Context(SalesPermissions.Discount.Apply),
            ManualRequest(
                requested: 5,
                scope: "LINE",
                calculationMethod: "PERCENTAGE",
                targetVariantId: variantId,
                lines: [new PosCheckoutLineRequestDto(variantId, 1)],
                idempotency: "line-key"),
            default);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.CreatedCommand);
        Assert.Equal("LINE", repository.CreatedCommand!.DiscountScope);
        Assert.Equal(variantId, repository.CreatedCommand.TargetVariantId);
        Assert.Equal("LINE", repository.LastRequestedManualScope);
    }

    [Fact]
    public async Task Validate_ManualLineDiscount_WhenTargetMissing_ReturnsControlledError()
    {
        var repository = new FakeDiscountRepository(
            maxPercentage: 10,
            absoluteLimit: 50,
            policyScope: "LINE");
        var service = CreateService(repository);

        var result = await service.ValidateAsync(
            Context(SalesPermissions.Discount.Apply),
            ManualRequest(
                requested: 5,
                scope: "LINE",
                calculationMethod: "PERCENTAGE",
                targetVariantId: null,
                lines: [new PosCheckoutLineRequestDto(Guid.NewGuid(), 1)]),
            default);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_discounts.target_required", result.Error.Code);
    }

    [Fact]
    public async Task Validate_ManualLineDiscount_WhenRepositoryResolvesOrderPolicy_ReturnsScopeMismatch()
    {
        var repository = new FakeDiscountRepository(
            maxPercentage: 10,
            absoluteLimit: 50,
            policyScope: "ORDER");
        var service = CreateService(repository);
        var variantId = Guid.NewGuid();

        var result = await service.ValidateAsync(
            Context(SalesPermissions.Discount.Apply),
            ManualRequest(
                requested: 5,
                scope: "LINE",
                calculationMethod: "PERCENTAGE",
                targetVariantId: variantId,
                lines: [new PosCheckoutLineRequestDto(variantId, 1)]),
            default);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_discounts.scope_mismatch", result.Error.Code);
    }

    [Fact]
    public async Task Validate_PolicySource_ReloadsPolicyValueAndCalculationMethod()
    {
        var service = CreateService(maxPercentage: 10, absoluteLimit: 50);
        var result = await service.ValidateAsync(
            Context(SalesPermissions.Discount.Apply),
            PolicyRequest(requestedValue: 99, calculationMethod: "FIXED_AMOUNT", scope: "ORDER"),
            default);

        Assert.True(result.IsSuccess);
        Assert.Equal("PERCENTAGE", result.Value!.CalculationMethod);
        Assert.Equal(10, result.Value.RequestedValue);
    }

    [Fact]
    public async Task Validate_PolicySource_WhenClientChangesPolicyScope_Rejects()
    {
        var repository = new FakeDiscountRepository(maxPercentage: 10, absoluteLimit: 50, policyScope: "LINE");
        var service = CreateService(repository);
        var result = await service.ValidateAsync(
            Context(SalesPermissions.Discount.Apply),
            PolicyRequest(requestedValue: 10, calculationMethod: "PERCENTAGE", scope: "ORDER"),
            default);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_discounts.scope_mismatch", result.Error.Code);
    }

    [Fact]
    public async Task Decide_WithoutApprovalPermission_ReturnsForbidden()
    {
        var service = CreateService(maxPercentage: 10, absoluteLimit: 50);
        var result = await service.DecideAsync(
            Context(SalesPermissions.Discount.Apply), Guid.NewGuid(),
            new PosDiscountDecisionRequestDto("APPROVE", null), default);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_discounts.approval_permission_denied", result.Error.Code);
    }

    private static PosDiscountService CreateService(decimal maxPercentage, decimal absoluteLimit) =>
        CreateService(new FakeDiscountRepository(maxPercentage, absoluteLimit));

    private static PosDiscountService CreateService(FakeDiscountRepository repository) =>
        new(repository, new FakeCheckoutRepository(), new Clock());

    private static TenantRequestContext Context(string permission) =>
        new(Guid.NewGuid(), Guid.NewGuid(), [permission]);

    private static PosDiscountValidationRequestDto Request(decimal requested, string? idempotency = null) =>
        ManualRequest(requested, "ORDER", "PERCENTAGE", null,
            [new PosCheckoutLineRequestDto(Guid.NewGuid(), 1)], idempotency);

    private static PosDiscountValidationRequestDto ManualRequest(
        decimal requested,
        string scope,
        string calculationMethod,
        Guid? targetVariantId,
        IReadOnlyList<PosCheckoutLineRequestDto> lines,
        string? idempotency = null) =>
        new(Guid.NewGuid(), null, "MANUAL", requested, scope, targetVariantId,
            "Reason", "NewSale", null, lines, idempotency, calculationMethod);

    private static PosDiscountValidationRequestDto PolicyRequest(
        decimal requestedValue, string calculationMethod, string scope)
    {
        var variantId = Guid.NewGuid();
        return new(Guid.NewGuid(), FakeDiscountRepository.PolicyId, "POLICY", requestedValue,
            scope, scope == "LINE" ? variantId : null, "Reason", "NewSale", null,
            [new PosCheckoutLineRequestDto(variantId, 1)], null, calculationMethod);
    }

    private sealed class FakeDiscountRepository : IPosDiscountRepository
    {
        public static readonly Guid PolicyId = Guid.Parse("aaaaaaaa-0000-4000-8000-000000000001");
        private readonly PosDiscountAuthorityDto _authority;
        private readonly PosDiscountPolicySnapshot _policy;
        public PosDiscountApplicationCommand? CreatedCommand { get; private set; }
        public string? LastRequestedManualScope { get; private set; }

        public FakeDiscountRepository(decimal maxPercentage, decimal absoluteLimit, string policyScope = "ORDER")
        {
            _authority = new(maxPercentage, 1000, "LKR");
            _policy = new(PolicyId, Guid.NewGuid(), "MANUAL", "Manual", null, policyScope,
                "PERCENTAGE", 10, absoluteLimit, null, null, null, null,
                false, false, null, 1, null, null);
        }

        public Task<PosDiscountCatalogRepositoryResult> ListAvailableAsync(
            Guid tenantId, Guid tenantUserId, PosDiscountCatalogQueryDto query, DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.FromResult(new PosDiscountCatalogRepositoryResult(null,
                new PosDiscountCatalogResponseDto(_authority, [])));

        public Task<PosDiscountContextRepositoryResult> ResolveContextAsync(
            Guid tenantId, Guid tenantUserId, Guid deviceId, Guid discountPolicyId,
            PosDiscountApplicabilityContext applicability, DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.FromResult(new PosDiscountContextRepositoryResult(
                null, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), _authority, _policy));

        public Task<PosDiscountContextRepositoryResult> ResolveManualContextAsync(
            Guid tenantId, Guid tenantUserId, Guid deviceId, string calculationMethod,
            string requestedScope,
            DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.FromResult(ResolveManual(requestedScope));

        private PosDiscountContextRepositoryResult ResolveManual(string requestedScope)
        {
            LastRequestedManualScope = requestedScope;
            return new(null, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), _authority, _policy);
        }

        public Task<PosDiscountApplicationRepositoryResult> CreateApplicationAsync(
            PosDiscountApplicationCommand command, CancellationToken cancellationToken)
        {
            CreatedCommand = command;
            return Task.FromResult(new PosDiscountApplicationRepositoryResult(
                null, command.ApplicationId,
                command.RequiresManagerApproval ? "PENDING_APPROVAL" : "APPROVED",
                command.ExpiresAt, false));
        }

        public Task<PosDiscountDecisionRepositoryResult> DecideAsync(
            Guid tenantId, Guid managerUserId, Guid applicationId, string decision,
            string? note, DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PosDiscountCancelRepositoryResult> CancelAsync(
            Guid tenantId, Guid tenantUserId, Guid applicationId, Guid deviceId,
            string? reason, DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeCheckoutRepository : IPosCheckoutRepository
    {
        public Task<PosCheckoutCalculationResult> CalculateSummaryAsync(
            Guid tenantId, Guid tenantUserId, IReadOnlyCollection<string> permissions,
            PosCheckoutSummaryRequestDto request, DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.FromResult(new PosCheckoutCalculationResult(null,
                new PosCheckoutSummaryResponseDto(
                    new PosCheckoutBillingSummaryDto(1, 5000, 0, 0, 5000, "LKR"),
                    new PosCheckoutSaleDetailsDto("New Sale", 1, now, "Cashier"), [], [])));
        public Task<PosCheckoutStartPaymentResult> StartPaymentAsync(
            Guid tenantId, Guid tenantUserId, IReadOnlyCollection<string> permissions,
            PosCheckoutStartPaymentRequestDto request, DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class Clock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => new(2026, 7, 12, 10, 0, 0, TimeSpan.Zero);
    }
}
