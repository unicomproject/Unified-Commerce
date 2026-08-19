namespace E_POS.LocalPrintAgent.Models;

public sealed record DrawerOpenRequest(
    string ApiVersion,
    Guid RequestId,
    Guid DrawerOperationId,
    string DrawerPurpose,
    string PrinterName,
    string DrawerPort,
    int PulseOnTime,
    int PulseOffTime,
    DateTimeOffset? RequestedAt = null,
    Guid? ConfigurationId = null,
    int? ConfigurationVersion = null,
    string? PosDeviceId = null);

public sealed record DrawerOpenApiResponse(
    bool Success,
    string Code,
    string Message,
    Guid RequestId,
    Guid DrawerOperationId,
    bool Duplicate,
    string PrinterName,
    int BytesWritten = 0,
    bool PhysicalOpenConfirmed = false);
