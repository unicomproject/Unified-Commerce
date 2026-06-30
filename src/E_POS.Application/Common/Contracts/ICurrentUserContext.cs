namespace E_POS.Application.Common.Contracts;

public interface ICurrentUserContext
{
    Guid? UserId { get; }

    string? UserType { get; }

    bool IsAuthenticated { get; }
}