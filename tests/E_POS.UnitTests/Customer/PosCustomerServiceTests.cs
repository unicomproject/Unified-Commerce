using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.Customer.Contracts;
using E_POS.Application.Modules.ECommerce.Customer.Dtos;
using E_POS.Application.Modules.ECommerce.Customer.Services;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using CustomerEntity = E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.Orders.Constants;
using Xunit;

namespace E_POS.UnitTests.Customer;

public sealed class PosCustomerServiceTests
{
    [Fact]
    public async Task ListAsync_WithoutViewPermission_ReturnsForbiddenBeforeResolvingDevice()
    {
        var repository = new FakeCustomerRepository();
        var tillRepository = new FakeTillSessionRepository();
        var service = CreateService(repository, tillRepository);
        var context = new TenantRequestContext(Guid.NewGuid(), Guid.NewGuid(), []);

        var result = await service.ListAsync(
            context,
            Guid.NewGuid(),
            null,
            null,
            null,
            1,
            20,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_customers.permission_denied", result.Error.Code);
        Assert.Equal(0, tillRepository.ResolveCallCount);
        Assert.Equal(0, repository.ListCallCount);
    }

    [Fact]
    public async Task ListAsync_WithOpenTill_NormalizesPaginationAndCallsRepository()
    {
        var repository = new FakeCustomerRepository();
        var tillRepository = new FakeTillSessionRepository
        {
            ResolveResult = OpenTillResult()
        };
        var service = CreateService(repository, tillRepository);
        var tenantId = Guid.NewGuid();
        var context = new TenantRequestContext(
            tenantId,
            Guid.NewGuid(),
            [CustomerPermissions.View]);

        var result = await service.ListAsync(
            context,
            Guid.NewGuid(),
            "kamal",
            null,
            null,
            0,
            500,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, repository.Page);
        Assert.Equal(100, repository.PageSize);
        Assert.Equal("kamal", repository.Search);
        Assert.Equal(tenantId, repository.TenantId);
    }

    [Fact]
    public async Task ListAsync_WithoutOpenTill_ReturnsOpenTillRequired()
    {
        var repository = new FakeCustomerRepository();
        var tillRepository = new FakeTillSessionRepository
        {
            ResolveResult = new CurrentTillSessionResolveResult(
                false,
                "till_session.not_found",
                null)
        };
        var service = CreateService(repository, tillRepository);
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [CustomerPermissions.View]);

        var result = await service.ListAsync(
            context,
            Guid.NewGuid(),
            null,
            null,
            null,
            1,
            20,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_customers.open_till_required", result.Error.Code);
        Assert.Equal(0, repository.ListCallCount);
    }

    [Fact]
    public async Task AttachToSaleAsync_WithoutCartManage_ReturnsPermissionDenied()
    {
        var service = CreateService(new FakeCustomerRepository(), new FakeTillSessionRepository());
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [CustomerPermissions.View]);

        var result = await service.AttachToSaleAsync(
            context,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new PosCustomerAttachToSaleRequestDto(null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_customers.attach_permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task AttachToSaleAsync_WhenBlocked_ReturnsCustomerBlocked()
    {
        var repository = new FakeCustomerRepository { CustomerStatus = "BLOCKED" };
        var tillRepository = new FakeTillSessionRepository { ResolveResult = OpenTillResult() };
        var service = CreateService(repository, tillRepository);
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [CustomerPermissions.View, SalesPermissions.Cart.Manage]);

        var result = await service.AttachToSaleAsync(
            context,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new PosCustomerAttachToSaleRequestDto(null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_customers.customer_blocked", result.Error.Code);
    }

    [Fact]
    public async Task UpdateAsync_WithValidRequest_UpdatesProfile()
    {
        var customerId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var tracked = CustomerEntity.CreatePosCustomer(
            customerId,
            tenantId,
            "CUS000001",
            "Old Name",
            "+94770000000",
            null,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);
        var repository = new FakeCustomerRepository
        {
            TrackedCustomer = tracked,
            GetByIdResult = new PosCustomerListItemResponseDto(
                customerId,
                "New Name",
                "+94771234567",
                "new@example.com",
                "INACTIVE",
                "CUS000001",
                "POS")
        };
        var tillRepository = new FakeTillSessionRepository { ResolveResult = OpenTillResult() };
        var service = CreateService(repository, tillRepository);
        var context = new TenantRequestContext(
            tenantId,
            Guid.NewGuid(),
            [CustomerPermissions.Update]);

        var result = await service.UpdateAsync(
            context,
            Guid.NewGuid(),
            customerId,
            new PosCustomerUpdateRequestDto(
                " New Name ",
                "+94 77-123-4567",
                " New@Example.com ",
                "INACTIVE"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.UpdatedCustomer);
        Assert.Equal("New Name", repository.UpdatedCustomer!.Name);
        Assert.Equal("+94771234567", repository.UpdatedCustomer.NormalizedPhone);
        Assert.Equal("NEW@EXAMPLE.COM", repository.UpdatedCustomer.NormalizedEmail);
        Assert.Equal("INACTIVE", repository.UpdatedCustomer.Status);
        Assert.Equal("CUS000001", repository.UpdatedCustomer.CustomerCode);
    }

    [Fact]
    public async Task UpdateAsync_WithoutUpdatePermission_ReturnsForbidden()
    {
        var service = CreateService(new FakeCustomerRepository(), new FakeTillSessionRepository());
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [CustomerPermissions.View, CustomerPermissions.Create]);

        var result = await service.UpdateAsync(
            context,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new PosCustomerUpdateRequestDto("Name", "+94771234567", null, "ACTIVE"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_customers.update_permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task GetSummaryAsync_UsesTenantTimezoneMonthBounds()
    {
        var repository = new FakeCustomerRepository
        {
            TenantTimezone = "Asia/Colombo"
        };
        var tillRepository = new FakeTillSessionRepository { ResolveResult = OpenTillResult() };
        var service = CreateService(repository, tillRepository);
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [CustomerPermissions.View]);

        var result = await service.GetSummaryAsync(context, Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Asia/Colombo", repository.SummaryTimeZoneId);
        Assert.NotNull(repository.SummaryMonthStartUtc);
        Assert.NotNull(repository.SummaryMonthEndUtc);
        Assert.True(repository.SummaryMonthEndUtc > repository.SummaryMonthStartUtc);
    }

    [Fact]
    public void ResolveTenantLocalMonthBounds_UsesUtcWhenConfigured()
    {
        var utcNow = new DateTimeOffset(2026, 7, 15, 10, 0, 0, TimeSpan.Zero);
        var (start, end, resolved) = PosCustomerService.ResolveTenantLocalMonthBounds(utcNow, "UTC");

        Assert.Equal("UTC", resolved);
        Assert.Equal(new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero), start);
        Assert.Equal(new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero), end);
    }

    [Fact]
    public void ResolveTenantLocalMonthBounds_InvalidTimezone_FallsBackToUtc()
    {
        var utcNow = new DateTimeOffset(2026, 7, 15, 10, 0, 0, TimeSpan.Zero);
        var (start, end, resolved) = PosCustomerService.ResolveTenantLocalMonthBounds(
            utcNow,
            "Not/ARealZone");

        Assert.Equal("UTC", resolved);
        Assert.Equal(new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero), start);
        Assert.Equal(new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero), end);
    }

    [Fact]
    public void ResolveTenantLocalMonthBounds_TenantOffset_ConvertsLocalMonthToUtc()
    {
        TimeZoneInfo tz;
        try
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                tz = TimeZoneInfo.FindSystemTimeZoneById("Sri Lanka Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                return;
            }
        }

        var utcNow = new DateTimeOffset(2026, 7, 15, 10, 0, 0, TimeSpan.Zero);
        var (start, end, resolved) = PosCustomerService.ResolveTenantLocalMonthBounds(
            utcNow,
            tz.Id);

        Assert.Equal(tz.Id, resolved);
        var localStart = TimeZoneInfo.ConvertTime(start, tz);
        Assert.Equal(2026, localStart.Year);
        Assert.Equal(7, localStart.Month);
        Assert.Equal(1, localStart.Day);
        Assert.Equal(0, localStart.Hour);
        var localEnd = TimeZoneInfo.ConvertTime(end, tz);
        Assert.Equal(8, localEnd.Month);
        Assert.Equal(1, localEnd.Day);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesActivePosCustomer()
    {
        var repository = new FakeCustomerRepository();
        var tillRepository = new FakeTillSessionRepository { ResolveResult = OpenTillResult() };
        var service = CreateService(repository, tillRepository);
        var tenantId = Guid.NewGuid();
        var context = new TenantRequestContext(
            tenantId,
            Guid.NewGuid(),
            [CustomerPermissions.Create]);

        var result = await service.CreateAsync(
            context,
            Guid.NewGuid(),
            new PosCustomerCreateRequestDto(
                " Kamal Perera ",
                "+94 77-123-4567",
                " Kamal@Example.com "),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Kamal Perera", result.Value?.FullName);
        Assert.NotNull(repository.AddedCustomer);
        Assert.Equal(tenantId, repository.AddedCustomer.TenantId);
        Assert.Equal("+94771234567", repository.AddedCustomer.NormalizedPhone);
        Assert.Equal("KAMAL@EXAMPLE.COM", repository.AddedCustomer.NormalizedEmail);
        Assert.Equal("POS", repository.AddedCustomer.SourceType);
        Assert.Equal("ACTIVE", repository.AddedCustomer.Status);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicatePhone_ReturnsConflictBeforeInsert()
    {
        var repository = new FakeCustomerRepository { PhoneExists = true };
        var tillRepository = new FakeTillSessionRepository { ResolveResult = OpenTillResult() };
        var service = CreateService(repository, tillRepository);
        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [CustomerPermissions.Create]);

        var result = await service.CreateAsync(
            context,
            Guid.NewGuid(),
            new PosCustomerCreateRequestDto("Kamal", "+94771234567", null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_customers.duplicate_phone", result.Error.Code);
        Assert.Null(repository.AddedCustomer);
    }

    [Fact]
    public async Task CreateAsync_WithoutCreatePermission_DoesNotResolveDevice()
    {
        var repository = new FakeCustomerRepository();
        var tillRepository = new FakeTillSessionRepository();
        var service = CreateService(repository, tillRepository);
        var context = new TenantRequestContext(Guid.NewGuid(), Guid.NewGuid(), []);

        var result = await service.CreateAsync(
            context,
            Guid.NewGuid(),
            new PosCustomerCreateRequestDto("Kamal", "+94771234567", null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_customers.create_permission_denied", result.Error.Code);
        Assert.Equal(0, tillRepository.ResolveCallCount);
    }

    private static PosCustomerService CreateService(
        FakeCustomerRepository repository,
        FakeTillSessionRepository tillRepository) =>
        new(repository, tillRepository, new FakeCodeSequenceRepository(), new FakeDateTimeProvider());

    private static CurrentTillSessionResolveResult OpenTillResult() => new(
        true,
        null,
        new CurrentTillSessionDbSnapshot(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1000m,
            "OPEN",
            DateTimeOffset.UtcNow,
            null));

    private sealed class FakeCustomerRepository : IPosCustomerRepository
    {
        public bool PhoneExists { get; init; }
        public bool EmailExists { get; init; }
        public bool AddResult { get; init; } = true;
        public CustomerEntity? AddedCustomer { get; private set; }
        public int ListCallCount { get; private set; }
        public Guid TenantId { get; private set; }
        public string? Search { get; private set; }
        public int Page { get; private set; }
        public int PageSize { get; private set; }

        public Task<bool> NormalizedPhoneExistsAsync(
            Guid tenantId,
            string normalizedPhone,
            CancellationToken cancellationToken) => Task.FromResult(PhoneExists);

        public Task<bool> NormalizedPhoneExistsAsync(
            Guid tenantId,
            string normalizedPhone,
            Guid excludingCustomerId,
            CancellationToken cancellationToken) => Task.FromResult(PhoneExists);

        public Task<bool> NormalizedEmailExistsAsync(
            Guid tenantId,
            string normalizedEmail,
            CancellationToken cancellationToken) => Task.FromResult(EmailExists);

        public Task<bool> NormalizedEmailExistsAsync(
            Guid tenantId,
            string normalizedEmail,
            Guid excludingCustomerId,
            CancellationToken cancellationToken) => Task.FromResult(EmailExists);

        public Task<bool> AddAsync(CustomerEntity customer, CancellationToken cancellationToken)
        {
            AddedCustomer = customer;
            return Task.FromResult(AddResult);
        }

        public Task<CustomerEntity?> GetTrackedByIdAsync(
            Guid tenantId,
            Guid customerId,
            CancellationToken cancellationToken) =>
            Task.FromResult(TrackedCustomer);

        public Task<string?> GetCustomerStatusAsync(
            Guid tenantId,
            Guid customerId,
            CancellationToken cancellationToken) =>
            Task.FromResult(CustomerStatus);

        public Task<bool> UpdateAsync(CustomerEntity customer, CancellationToken cancellationToken)
        {
            UpdatedCustomer = customer;
            return Task.FromResult(UpdateResult);
        }

        public Task<string?> GetTenantDefaultTimezoneAsync(
            Guid tenantId,
            CancellationToken cancellationToken) =>
            Task.FromResult(TenantTimezone);

        public Task<PosCustomerListResponseDto> ListAsync(
            Guid tenantId,
            string? search,
            string? status,
            string? source,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            ListCallCount++;
            TenantId = tenantId;
            Search = search;
            Page = page;
            PageSize = pageSize;
            return Task.FromResult(new PosCustomerListResponseDto([], page, pageSize, 0, 0));
        }

        public Task<PosCustomerSummaryResponseDto> GetSummaryAsync(
            Guid tenantId,
            DateTimeOffset monthStartUtc,
            DateTimeOffset monthEndUtc,
            string timeZoneId,
            CancellationToken cancellationToken)
        {
            SummaryMonthStartUtc = monthStartUtc;
            SummaryMonthEndUtc = monthEndUtc;
            SummaryTimeZoneId = timeZoneId;
            return Task.FromResult(new PosCustomerSummaryResponseDto(0, 0, 0, 0, timeZoneId));
        }

        public Task<PosCustomerListItemResponseDto?> GetByIdAsync(
            Guid tenantId,
            Guid customerId,
            CancellationToken cancellationToken) =>
            Task.FromResult(GetByIdResult);

        public Task<PosCustomerOrdersResponseDto> GetOrdersAsync(
            Guid tenantId,
            Guid customerId,
            int page,
            int pageSize,
            DateTimeOffset? fromDate,
            DateTimeOffset? toDate,
            string? status,
            CancellationToken cancellationToken) =>
            Task.FromResult(new PosCustomerOrdersResponseDto([], page, pageSize, 0, 0));

        public Task<bool> TryAssignCustomerToEditableSaleAsync(
            Guid tenantId,
            Guid saleId,
            Guid customerId,
            string? customerNameSnapshot,
            Guid? tillSessionId,
            Guid updatedByTenantUserId,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public CustomerEntity? TrackedCustomer { get; set; }
        public CustomerEntity? UpdatedCustomer { get; private set; }
        public bool UpdateResult { get; init; } = true;
        public string? CustomerStatus { get; set; }
        public string? TenantTimezone { get; set; } = "UTC";
        public PosCustomerListItemResponseDto? GetByIdResult { get; set; }
        public DateTimeOffset? SummaryMonthStartUtc { get; private set; }
        public DateTimeOffset? SummaryMonthEndUtc { get; private set; }
        public string? SummaryTimeZoneId { get; private set; }
    }

    private sealed class FakeCodeSequenceRepository : ICodeSequenceRepository
    {
        public Task<string> GetNextCodeAsync(
            Guid tenantId,
            string sequenceKey,
            string prefix,
            int paddingLength,
            DateTimeOffset now,
            CancellationToken cancellationToken) => Task.FromResult("CUS000001");
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 7, 14, 10, 0, 0, TimeSpan.Zero);
    }

    private sealed class FakeTillSessionRepository : IPosTillSessionRepository
    {
        public int ResolveCallCount { get; private set; }
        public CurrentTillSessionResolveResult ResolveResult { get; init; } =
            new(false, "till_session.not_found", null);

        public Task<CurrentTillSessionResolveResult> ResolveCurrentSessionAsync(
            Guid tenantId,
            Guid deviceId,
            CancellationToken cancellationToken)
        {
            ResolveCallCount++;
            return Task.FromResult(ResolveResult);
        }

        public Task<OpenTillRepositoryResult> OpenTillAsync(
            Guid tenantId,
            Guid tenantUserId,
            OpenTillCommand command,
            DateTimeOffset now,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<CloseTillRepositoryResult> CloseTillAsync(
            Guid tenantId,
            Guid tenantUserId,
            CloseTillCommand command,
            DateTimeOffset now,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
