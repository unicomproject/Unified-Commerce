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
await using var find=new NpgsqlCommand("SELECT id FROM tenant_users WHERE lower(email)=@email FOR UPDATE",connection,tx);
find.Parameters.AddWithValue("email","cashier001@gmail.com");
Guid id;
await using(var reader=await find.ExecuteReaderAsync()) {
if(!await reader.ReadAsync()) throw new Exception("Account not found; no changes.");
id=reader.GetGuid(0);
if(await reader.ReadAsync()) throw new Exception("Multiple matches; no changes.");
}
var salt=RandomNumberGenerator.GetBytes(16);
var hash=Rfc2898DeriveBytes.Pbkdf2(Args[0],salt,100000,HashAlgorithmName.SHA256,32);
var encoded=$"PBKDF2-SHA256:100000:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
await using var update=new NpgsqlCommand("UPDATE tenant_users SET encrypted_password=@hash, account_status='ACTIVE', locked_until=NULL, failed_login_attempts=0, updated_at=now() WHERE id=@id",connection,tx);
update.Parameters.AddWithValue("hash",encoded);
update.Parameters.AddWithValue("id",id);
if(await update.ExecuteNonQueryAsync()!=1) throw new Exception("Unexpected update count.");
await tx.CommitAsync();
Console.WriteLine("Password reset committed for cashier001@gmail.com.");
}
