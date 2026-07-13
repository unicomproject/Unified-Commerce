namespace E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;

/// <summary>
/// Optional request metadata captured from the HTTP layer for platform auth auditing.
/// Not part of any public API contract.
/// </summary>
public sealed record PlatformAuthClientContext(
    string? IpAddress,
    string? UserAgent,
    string? DeviceName);
