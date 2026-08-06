using E_POS.Application.Modules.ECommerce.CartCheckout.Contracts;
using E_POS.Application.Modules.ECommerce.CartCheckout.Dtos;
using E_POS.Infrastructure.Persistence;

namespace E_POS.Infrastructure.Modules.ECommerce.CartCheckout.Repositories;

public sealed class StorefrontCheckoutRepository : IStorefrontCheckoutRepository
{
    private readonly IStorefrontCheckoutSessionRepository _sessionRepository;
    private readonly IStorefrontCheckoutConfirmationRepository _confirmationRepository;

    public StorefrontCheckoutRepository(EPosDbContext dbContext)
        : this(
            new StorefrontCheckoutSessionRepository(dbContext),
            new StorefrontCheckoutConfirmationRepository(dbContext))
    {
    }

    public StorefrontCheckoutRepository(
        IStorefrontCheckoutSessionRepository sessionRepository,
        IStorefrontCheckoutConfirmationRepository confirmationRepository)
    {
        _sessionRepository = sessionRepository;
        _confirmationRepository = confirmationRepository;
    }

    public Task<StorefrontCheckoutRepositoryResult> CreateFromCartAsync(
        Guid tenantId,
        Guid customerId,
        string cartSessionId,
        CreateStorefrontCheckoutFromCartRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        _sessionRepository.CreateFromCartAsync(
            tenantId,
            customerId,
            cartSessionId,
            request,
            now,
            cancellationToken);

    public Task<StorefrontCheckoutRepositoryResult> GetAsync(
        Guid tenantId,
        Guid customerId,
        Guid checkoutSessionId,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        _sessionRepository.GetAsync(
            tenantId,
            customerId,
            checkoutSessionId,
            now,
            cancellationToken);

    public Task<StorefrontCheckoutRepositoryResult> UpdateCollectionAsync(
        Guid tenantId,
        Guid customerId,
        Guid checkoutSessionId,
        UpdateStorefrontCheckoutCollectionRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        _sessionRepository.UpdateCollectionAsync(
            tenantId,
            customerId,
            checkoutSessionId,
            request,
            now,
            cancellationToken);

    public Task<StorefrontCheckoutRepositoryResult> ConfirmAsync(
        Guid tenantId,
        Guid customerId,
        Guid checkoutSessionId,
        string idempotencyKey,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        _confirmationRepository.ConfirmAsync(
            tenantId,
            customerId,
            checkoutSessionId,
            idempotencyKey,
            now,
            cancellationToken);
}