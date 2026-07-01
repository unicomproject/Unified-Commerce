namespace E_POS.Infrastructure.Common.Security;

internal static class Base64Url
{
    public static string Encode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public static string Encode(string value)
    {
        return Encode(System.Text.Encoding.UTF8.GetBytes(value));
    }
}