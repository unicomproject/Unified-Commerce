namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;

public sealed record OutletBusinessHourResponse(Guid Id, int DayOfWeek, TimeOnly OpenTime, TimeOnly CloseTime);
