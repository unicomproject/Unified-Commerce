namespace E_POS.Application.Common.Models;

public sealed class ApplicationResult<T> : ApplicationResult
{
    private ApplicationResult(T value)
        : base(true, ApplicationError.None)
    {
        Value = value;
    }

    private ApplicationResult(ApplicationError error)
        : base(false, error)
    {
    }

    public T? Value { get; }

    public static ApplicationResult<T> Success(T value)
    {
        return new ApplicationResult<T>(value);
    }

    public static new ApplicationResult<T> Failure(ApplicationError error)
    {
        return new ApplicationResult<T>(error);
    }
}