#r "nuget: Npgsql, 8.0.3"
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Security.Cryptography;
using Npgsql;
{
using var settings=JsonDocument.Parse(File.ReadAllText("src/E_POS.Api/appsettings.Development.json"));
var cs=Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ?? settings.RootElement.GetProperty("ConnectionStrings").EnumerateObject().First().Value.GetString();
await using var connection=new NpgsqlConnection(cs);
await connection.OpenAsync();
await using var cmd=new NpgsqlCommand("SELECT encrypted_password, account_status FROM tenant_users WHERE lower(email)=@email",connection);
cmd.Parameters.AddWithValue("email","cashier001@gmail.com");
await using var reader=await cmd.ExecuteReaderAsync();
if(!await reader.ReadAsync()) throw new Exception("Account not found");
var hash=reader.IsDBNull(0)?"":reader.GetString(0);
var status=reader.GetString(1);
if(await reader.ReadAsync()) throw new Exception("Multiple matches");
var parts=hash.Split(':');
bool match=false;
if(parts.Length==4 && parts[0]=="PBKDF2-SHA256" && int.TryParse(parts[1],out var iterations)) {
var expected=Convert.FromBase64String(parts[3]);
var actual=Rfc2898DeriveBytes.Pbkdf2(Args[0],Convert.FromBase64String(parts[2]),iterations,HashAlgorithmName.SHA256,expected.Length);
match=CryptographicOperations.FixedTimeEquals(actual,expected);
}
Console.WriteLine($"PasswordMatches={match}; AccountStatus={status}; Database={connection.Database}");
}
