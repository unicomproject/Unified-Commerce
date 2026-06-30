namespace E_POS.Application.Common.Contracts;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}