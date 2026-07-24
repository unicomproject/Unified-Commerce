using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.Storefront.Contracts;
using E_POS.Application.Modules.ECommerce.Storefront.Dtos;

namespace E_POS.Application.Modules.ECommerce.Storefront.Services;

public sealed class StorefrontFulfillmentService : IStorefrontFulfillmentService
{
    public const int DefaultCollectionDays = 5;
    public const int MaximumCollectionDays = 14;

    private readonly IStorefrontFulfillmentRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public StorefrontFulfillmentService(IStorefrontFulfillmentRepository repository)
        : this(repository, new SystemClock())
    {
    }

    public StorefrontFulfillmentService(
        IStorefrontFulfillmentRepository repository,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<IEnumerable<StorefrontStoreReadModel>> GetAvailableStoresAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _repository.GetAvailableStoresAsync(tenantId, cancellationToken);
    }

    public async Task<ApplicationResult<StorefrontCollectionOptionsReadModel>> GetCollectionOptionsAsync(
        Guid tenantId,
        Guid outletId,
        int days,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
            return Failure("storefront_fulfillment.invalid_tenant", "A valid tenant is required.");
        if (outletId == Guid.Empty)
            return Failure("storefront_fulfillment.invalid_outlet_id", "A valid outlet is required.");
        if (days < 1 || days > MaximumCollectionDays)
            return Failure("storefront_fulfillment.invalid_days", $"Days must be between 1 and {MaximumCollectionDays}.");

        var generatedAt = _dateTimeProvider.UtcNow.ToUniversalTime();
        var configuration = await _repository.GetCollectionConfigurationAsync(
            tenantId,
            outletId,
            generatedAt,
            cancellationToken);
        if (configuration is null)
            return Failure("storefront_fulfillment.collection_unavailable", "Collection is not available for this outlet.");
        if (configuration.PreparationLeadMinutes is null or < 0 ||
            configuration.PickupWindowMinutes is null or <= 0 ||
            configuration.BusinessHours.Count == 0)
        {
            return Failure(
                "storefront_fulfillment.collection_configuration_missing",
                "Collection hours, preparation time, or pickup window are not configured for this outlet.");
        }

        TimeZoneInfo timezone;
        try
        {
            timezone = TimeZoneInfo.FindSystemTimeZoneById(configuration.Timezone);
        }
        catch (TimeZoneNotFoundException)
        {
            return Failure("storefront_fulfillment.invalid_timezone", "The outlet timezone is not supported.");
        }
        catch (InvalidTimeZoneException)
        {
            return Failure("storefront_fulfillment.invalid_timezone", "The outlet timezone is not supported.");
        }

        var earliestCollectionAt = generatedAt.AddMinutes(configuration.PreparationLeadMinutes.Value);
        var localNow = TimeZoneInfo.ConvertTime(generatedAt, timezone);
        var localToday = DateOnly.FromDateTime(localNow.DateTime);
        var dates = new List<StorefrontCollectionDateReadModel>();

        for (var dayOffset = 0; dayOffset < days; dayOffset++)
        {
            var date = localToday.AddDays(dayOffset);
            var hours = SelectBusinessHours(configuration.BusinessHours, date);

            if (hours is null || hours.IsClosed || !hours.OpeningTime.HasValue || !hours.ClosingTime.HasValue)
                continue;
            if (dayOffset == 0 && configuration.CutoffTime.HasValue &&
                TimeOnly.FromDateTime(localNow.DateTime) >= configuration.CutoffTime.Value)
                continue;

            var windows = CreateWindows(
                date,
                hours.OpeningTime.Value,
                hours.ClosingTime.Value,
                configuration.PickupWindowMinutes.Value,
                earliestCollectionAt,
                timezone);
            if (windows.Count == 0)
                continue;

            dates.Add(new StorefrontCollectionDateReadModel
            {
                Date = date,
                DayOfWeek = date.DayOfWeek.ToString(),
                OpeningTime = hours.OpeningTime.Value,
                ClosingTime = hours.ClosingTime.Value,
                Windows = windows
            });
        }

        return ApplicationResult<StorefrontCollectionOptionsReadModel>.Success(new StorefrontCollectionOptionsReadModel
        {
            OutletId = configuration.OutletId,
            OutletName = configuration.OutletName,
            Timezone = configuration.Timezone,
            PreparationLeadMinutes = configuration.PreparationLeadMinutes.Value,
            PickupWindowMinutes = configuration.PickupWindowMinutes.Value,
            CutoffTime = configuration.CutoffTime,
            GeneratedAt = generatedAt,
            EarliestCollectionAt = earliestCollectionAt,
            Dates = dates
        });
    }

    private static StorefrontCollectionBusinessHourReadModel? SelectBusinessHours(
        IReadOnlyList<StorefrontCollectionBusinessHourReadModel> businessHours,
        DateOnly date)
    {
        return businessHours
            .Where(x =>
                x.DayOfWeek == (short)date.DayOfWeek &&
                (!x.ValidFrom.HasValue || x.ValidFrom.Value <= date) &&
                (!x.ValidUntil.HasValue || x.ValidUntil.Value >= date))
            .OrderByDescending(x => x.ValidFrom.HasValue || x.ValidUntil.HasValue)
            .ThenByDescending(x => x.IsClosed)
            .ThenByDescending(x => x.ValidFrom ?? DateOnly.MinValue)
            .ThenBy(x => x.ValidUntil ?? DateOnly.MaxValue)
            .FirstOrDefault();
    }

    private static IReadOnlyList<StorefrontCollectionWindowReadModel> CreateWindows(
        DateOnly date,
        TimeOnly openingTime,
        TimeOnly closingTime,
        int pickupWindowMinutes,
        DateTimeOffset earliestCollectionAt,
        TimeZoneInfo timezone)
    {
        var windows = new List<StorefrontCollectionWindowReadModel>();
        var cursor = date.ToDateTime(openingTime, DateTimeKind.Unspecified);
        var closing = date.ToDateTime(closingTime, DateTimeKind.Unspecified);

        while (cursor.AddMinutes(pickupWindowMinutes) <= closing)
        {
            var localEnd = cursor.AddMinutes(pickupWindowMinutes);
            if (!timezone.IsInvalidTime(cursor) &&
                !timezone.IsInvalidTime(localEnd) &&
                !timezone.IsAmbiguousTime(cursor) &&
                !timezone.IsAmbiguousTime(localEnd))
            {
                var start = new DateTimeOffset(cursor, timezone.GetUtcOffset(cursor));
                var end = new DateTimeOffset(localEnd, timezone.GetUtcOffset(localEnd));
                if (start.ToUniversalTime() >= earliestCollectionAt)
                {
                    windows.Add(new StorefrontCollectionWindowReadModel
                    {
                        StartAt = start,
                        EndAt = end
                    });
                }
            }

            cursor = localEnd;
        }

        return windows;
    }

    private static ApplicationResult<StorefrontCollectionOptionsReadModel> Failure(string code, string message) =>
        ApplicationResult<StorefrontCollectionOptionsReadModel>.Failure(new ApplicationError(code, message));

    private sealed class SystemClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
