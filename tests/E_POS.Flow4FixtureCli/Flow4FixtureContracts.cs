using System.Text.Json.Serialization;

namespace E_POS.Flow4FixtureCli;

[JsonConverter(typeof(JsonStringEnumConverter<Flow4FixtureScenario>))]
public enum Flow4FixtureScenario
{
    AWAITING_PAYMENT,
    PAYMENT_SUBMITTED,
    ACTION_REQUIRED,
    REJECTED,
    APPROVABLE_PAYMENT,
    REJECTABLE_PAYMENT,
    REQUEST_INFORMATION_ELIGIBLE,
    CONCURRENT_REVIEW,
    UNCLEAN_EVIDENCE,
    NOTIFICATION_FAILED,
    PAID_PENDING_ACTIVATION,
    ACTIVE_INVITATION_READY,
    RETRYABLE_OPERATION,
    EXPIRED_PAYMENT_ACCESS,
    REVOKED_PAYMENT_ACCESS,
    CROSS_TENANT_PROOF,
    COMPLETE_HAPPY_PATH
}

public sealed record Flow4FixtureOptions(
    string Environment,
    bool Enabled,
    string ConnectionString,
    string ExpectedBootstrapCredential,
    string ExpectedMarkerNonce,
    string EmailMode,
    string TenantSigningKey,
    int TokenTtlMinutes,
    IReadOnlySet<string> AllowedHosts,
    IReadOnlySet<string> DeniedHosts,
    IReadOnlySet<string> DeniedDatabases,
    string RequiredDatabaseRole)
{
    public const string DatabaseNamePattern = "^oneverz_flow4_e2e_[a-z0-9_]{8,64}$";

    public static Flow4FixtureOptions FromEnvironment()
    {
        static string Get(string name) => System.Environment.GetEnvironmentVariable(name)?.Trim() ?? string.Empty;
        static HashSet<string> Set(string name, params string[] defaults) =>
            new(defaults.Concat(Get(name).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)),
                StringComparer.OrdinalIgnoreCase);

        _ = int.TryParse(Get("Flow4TestHost__TokenTtlMinutes"), out var ttl);
        return new(
            Get("DOTNET_ENVIRONMENT"),
            bool.TryParse(Get("Flow4TestHost__Enabled"), out var enabled) && enabled,
            Get("ConnectionStrings__DefaultConnection"),
            Get("Flow4TestHost__BootstrapCredential"),
            Get("Flow4TestHost__MarkerNonce"),
            Get("Flow4TestHost__EmailMode"),
            Get("TenantJwt__SigningKey"),
            ttl == 0 ? 30 : ttl,
            Set("Flow4TestHost__AllowedHosts", "localhost", "127.0.0.1", "::1"),
            Set("Flow4TestHost__DeniedHosts", "production", "prod", "staging", "stage", "shared"),
            Set("Flow4TestHost__DeniedDatabases", "UnifiedCommerceDb", "postgres", "production", "staging"),
            Get("Flow4TestHost__RequiredDatabaseRole"));
    }
}

public sealed record Flow4BootstrapInput(string BootstrapCredential, string? CleanupHandle = null);

public sealed record Flow4FixtureMetadata(
    string SchemaVersion,
    string FixtureSetVersion,
    Guid TestRunId,
    string Environment,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    string? BackendCommit);

public sealed record Flow4FixtureCleanup(
    string Handle,
    string Version,
    IReadOnlyList<string> Scenarios,
    IReadOnlyDictionary<string, int> ResourceCounts);

public sealed record Flow4FixtureManifest(
    Flow4FixtureMetadata Metadata,
    IReadOnlyDictionary<string, string> Identifiers,
    IReadOnlyDictionary<string, string> Secrets,
    Flow4FixtureCleanup Cleanup)
{
    public const string CurrentSchemaVersion = "1.0";
    public const string CurrentFixtureSetVersion = "canonical-v1";

    public void Validate(Guid expectedRunId, string expectedEnvironment)
    {
        if (Metadata.SchemaVersion != CurrentSchemaVersion || Metadata.FixtureSetVersion != CurrentFixtureSetVersion)
            throw new InvalidOperationException("Unsupported fixture manifest version.");
        if (Metadata.TestRunId != expectedRunId || !string.Equals(Metadata.Environment, expectedEnvironment, StringComparison.Ordinal))
            throw new InvalidOperationException("Fixture manifest boundary mismatch.");
        if (Metadata.ExpiresAt <= DateTimeOffset.UtcNow || Metadata.ExpiresAt > Metadata.CreatedAt.AddMinutes(60))
            throw new InvalidOperationException("Fixture manifest expiry is invalid.");
        if (Secrets.Any(x => string.IsNullOrWhiteSpace(x.Value)))
            throw new InvalidOperationException("Fixture manifest contains an empty secret.");
    }

    public string ToRedactedDiagnostic() =>
        $"Flow4FixtureManifest {{ SchemaVersion = {Metadata.SchemaVersion}, RunId = {Metadata.TestRunId:D}, " +
        $"Environment = {Metadata.Environment}, Scenarios = {Cleanup.Scenarios.Count}, " +
        $"Identifiers = {Identifiers.Count}, Secrets = [REDACTED:{Secrets.Count}], Resources = {Cleanup.ResourceCounts.Values.Sum()} }}";

    public override string ToString() => ToRedactedDiagnostic();
}

public sealed record Flow4FixtureCleanupResult(Guid RunId, bool AlreadyClean, IReadOnlyDictionary<string, int> Removed)
{
    public override string ToString() =>
        $"Flow4FixtureCleanupResult {{ RunId = {RunId:D}, AlreadyClean = {AlreadyClean}, Removed = {Removed.Values.Sum()} }}";
}

public sealed record Flow4FixtureScenarioDescription(string Name, bool ReturnsPaymentToken, bool ReturnsInvitationToken);

public sealed record Flow4ScenarioResources(
    Flow4FixtureScenario Scenario,
    Guid TenantId,
    Guid PlanId,
    Guid SubscriptionId,
    Guid InvoiceId,
    Guid PaymentId,
    Guid DraftId,
    Guid OperationId,
    Guid AccessId,
    Guid AdminUserId,
    Guid RoleId,
    Guid UserRoleId,
    Guid? EvidenceId,
    Guid? InvitationId,
    Guid? SecondaryTenantId,
    Guid? SecondarySubscriptionId,
    Guid? SecondaryInvoiceId,
    Guid? SecondaryPaymentId,
    string? RawPaymentToken,
    string? RawInvitationToken,
    DateTimeOffset ExpiresAt);
