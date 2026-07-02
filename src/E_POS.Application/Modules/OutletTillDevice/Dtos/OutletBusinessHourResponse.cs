namespace E_POS.Application.Modules.OutletTillDevice.Dtos;

public sealed record OutletBusinessHourResponse(Guid Id, int DayOfWeek, TimeOnly OpenTime, TimeOnly CloseTime);