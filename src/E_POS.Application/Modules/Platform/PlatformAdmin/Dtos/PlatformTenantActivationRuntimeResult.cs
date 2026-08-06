namespace E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

public enum PlatformTenantActivationRuntimeOutcome
{
    Success,
    Replay,
    NotFound,
    InvalidTransition,
    PaymentNotVerified,
    SubscriptionMissing,
    MembershipMissing,
    EntitlementsNotReady,
    ConcurrencyConflict
}

public sealed record PlatformTenantActivationRuntimeResult(PlatformTenantActivationRuntimeOutcome Outcome);

public enum TenantInvitationResendOutcome
{
    Success,
    Replay,
    NotFound,
    InvalidTransition,
    IdempotencyConflict,
    RateLimited
}

public sealed record TenantInvitationResendResult(TenantInvitationResendOutcome Outcome);
