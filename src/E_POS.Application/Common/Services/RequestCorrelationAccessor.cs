using E_POS.Application.Common.Contracts;

namespace E_POS.Application.Common.Services;

public sealed class RequestCorrelationAccessor : IRequestCorrelationAccessor
{
    private static readonly AsyncLocal<string?> Current = new();

    public string CorrelationId => Current.Value ?? string.Empty;

    public void Set(string correlationId)
    {
        Current.Value = string.IsNullOrWhiteSpace(correlationId)
            ? string.Empty
            : correlationId.Trim();
    }
}
