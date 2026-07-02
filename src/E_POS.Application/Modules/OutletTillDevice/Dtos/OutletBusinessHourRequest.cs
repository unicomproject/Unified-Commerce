namespace E_POS.Application.Modules.OutletTillDevice.Dtos;

public sealed record OutletBusinessHourRequest(int DayOfWeek, TimeOnly OpenTime, TimeOnly CloseTime);