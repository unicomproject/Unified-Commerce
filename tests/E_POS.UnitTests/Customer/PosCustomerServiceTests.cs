using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.Customer.Contracts;
using E_POS.Application.Modules.ECommerce.Customer.Dtos;
using E_POS.Application.Modules.ECommerce.Customer.Services;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using CustomerEntity = E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
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
            1,
            20,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_customers.open_till_required", result.Error.Code);
        Assert.Equal(0, repository.ListCallCount);
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

        public Task<bool> NormalizedEmailExistsAsync(
            Guid tenantId,
            string normalizedEmail,
            CancellationToken cancellationToken) => Task.FromResult(EmailExists);

        public Task<bool> AddAsync(CustomerEntity customer, CancellationToken cancellationToken)
        {
            AddedCustomer = customer;
            return Task.FromResult(AddResult);
        }

        public Task<PosCustomerListResponseDto> ListAsync(
            Guid tenantId,
            string? search,
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
