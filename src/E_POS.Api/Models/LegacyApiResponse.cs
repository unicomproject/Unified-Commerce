namespace E_POS.Api.Models;

public sealed record LegacyApiResponse<T>(bool Success, string Message, T Data)
{
    public static LegacyApiResponse<T> Ok(string message, T data)
    {
        return new LegacyApiResponse<T>(true, message, data);
    }
}
