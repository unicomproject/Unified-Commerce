namespace E_POS.Application.Modules.OutletTillDevice.Dtos;

public sealed record OutletSummaryResponse(Guid Id, string OutletCode, string Name, string Status, string OutletType, bool IsOnlineVisible, string? ContactPhone, string? ContactEmail, bool CollectionEnabled);