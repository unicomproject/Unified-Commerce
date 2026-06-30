namespace E_POS.Application.Common.Models;

public class ApplicationResult
{
    protected ApplicationResult(bool isSuccess, ApplicationError error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public ApplicationError Error { get; }

    public static ApplicationResult Success()
    {
        return new ApplicationResult(true, ApplicationError.None);
    }

    public static ApplicationResult Failure(ApplicationError error)
    {
        return new ApplicationResult(false, error);
    }
}