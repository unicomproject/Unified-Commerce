using E_POS.Application.Modules.Tenant.Reports.Dtos;

namespace E_POS.Application.Modules.Tenant.Reports.Validators;

public sealed class ReportQueryValidator
{
    private static readonly int[] AllowedPageSizes = [25, 50, 100];
    private readonly IReadOnlyCollection<string> _allowedSections;
    private readonly IReadOnlyDictionary<string, IReadOnlyCollection<string>> _sortableFieldsBySection;
    private readonly int _maximumDateRangeDays;

    public ReportQueryValidator(
        IReadOnlyCollection<string> allowedSections,
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> sortableFieldsBySection,
        int maximumDateRangeDays)
    {
        _allowedSections = allowedSections;
        _sortableFieldsBySection = sortableFieldsBySection;
        _maximumDateRangeDays = maximumDateRangeDays;
    }

    public IReadOnlyList<string> Validate(ReportQueryDto query)
    {
        var errors = new List<string>();

        if (query.Page < 1)
        {
            errors.Add("Page must be greater than or equal to 1.");
        }

        if (!AllowedPageSizes.Contains(query.PageSize))
        {
            errors.Add("PageSize must be one of 25, 50, or 100.");
        }

        if (!string.IsNullOrWhiteSpace(query.SortDirection) &&
            !string.Equals(query.SortDirection, "asc", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(query.SortDirection, "desc", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("SortDirection must be asc or desc.");
        }

        if (query.From.HasValue && query.To.HasValue)
        {
            if (query.From.Value > query.To.Value)
            {
                errors.Add("From must be before or equal to To.");
            }

            if (query.To.Value.DayNumber - query.From.Value.DayNumber > _maximumDateRangeDays)
            {
                errors.Add($"Report date range cannot exceed {_maximumDateRangeDays} days.");
            }
        }

        if (!string.IsNullOrWhiteSpace(query.Section) && !_allowedSections.Contains(query.Section))
        {
            errors.Add("Section is not supported.");
        }

        if (!IsSortAllowed(query))
        {
            errors.Add("SortBy is not supported for this section.");
        }

        return errors;
    }

    private bool IsSortAllowed(ReportQueryDto query)
    {
        if (string.IsNullOrWhiteSpace(query.SortBy))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(query.Section))
        {
            return false;
        }

        return _sortableFieldsBySection.TryGetValue(query.Section, out var fields) &&
               fields.Contains(query.SortBy, StringComparer.Ordinal);
    }
}
