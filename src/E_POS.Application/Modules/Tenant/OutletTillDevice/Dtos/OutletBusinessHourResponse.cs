namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;

public sealed record OutletBusinessHourResponse(
    Guid Id,
    int DayOfWeek,
    TimeOnly? OpeningTime,
    TimeOnly? ClosingTime,
    bool IsClosed,
    DateOnly? ValidFrom,
    DateOnly? ValidUntil);
