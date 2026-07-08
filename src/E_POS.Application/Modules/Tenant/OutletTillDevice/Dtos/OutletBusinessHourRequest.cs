namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;

public sealed record OutletBusinessHourRequest(int DayOfWeek, TimeOnly OpenTime, TimeOnly CloseTime);
