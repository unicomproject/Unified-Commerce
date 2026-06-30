namespace E_POS.Application.Common.Models;

public sealed record ApplicationError(string Code, string Message)
{
    public static ApplicationError None { get; } = new(string.Empty, string.Empty);
}