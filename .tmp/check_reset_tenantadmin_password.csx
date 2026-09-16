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
await using var tx=await connection.BeginTransactionAsync();
await using var find=new NpgsqlCommand("SELECT id, encrypted_password, account_status FROM tenant_users WHERE lower(email)=@email FOR UPDATE",connection,tx);
find.Parameters.AddWithValue("email","tenantadmin001@gmail.com");
Guid id; string stored; string status;
await using(var reader=await find.ExecuteReaderAsync()) {
if(!await reader.ReadAsync()) throw new Exception("Account not found; no changes.");
id=reader.GetGuid(0); stored=reader.IsDBNull(1)?"":reader.GetString(1); status=reader.GetString(2);
if(await reader.ReadAsync()) throw new Exception("Multiple matches; no changes.");
}
bool Matches(string encoded) {
var parts=encoded.Split(':');
if(parts.Length!=4 || parts[0]!="PBKDF2-SHA256" || !int.TryParse(parts[1],out var iterations)) return false;
try {
var expected=Convert.FromBase64String(parts[3]);
return CryptographicOperations.FixedTimeEquals(Rfc2898DeriveBytes.Pbkdf2(Args[0],Convert.FromBase64String(parts[2]),iterations,HashAlgorithmName.SHA256,expected.Length),expected);
} catch(FormatException) { return false; }
}
var matched=Matches(stored);
Console.WriteLine($"BeforePasswordMatches={matched}; AccountStatus={status}");
if(!matched) {
var salt=RandomNumberGenerator.GetBytes(16);
var hash=Rfc2898DeriveBytes.Pbkdf2(Args[0],salt,100000,HashAlgorithmName.SHA256,32);
var encoded=$"PBKDF2-SHA256:100000:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
await using var update=new NpgsqlCommand("UPDATE tenant_users SET encrypted_password=@hash, updated_at=now() WHERE id=@id",connection,tx);
update.Parameters.AddWithValue("hash",encoded); update.Parameters.AddWithValue("id",id);
if(await update.ExecuteNonQueryAsync()!=1) throw new Exception("Unexpected update count.");
}
await tx.CommitAsync();
await using var verify=new NpgsqlCommand("SELECT encrypted_password FROM tenant_users WHERE id=@id",connection);
verify.Parameters.AddWithValue("id",id);
Console.WriteLine($"VerifiedPasswordMatches={Matches((string)await verify.ExecuteScalarAsync())}; PasswordChanged={!matched}; Database={connection.Database}");
}
