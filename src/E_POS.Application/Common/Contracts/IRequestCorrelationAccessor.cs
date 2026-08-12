namespace E_POS.Application.Common.Contracts;

public interface IRequestCorrelationAccessor
{
    string CorrelationId { get; }
    void Set(string correlationId);
}
