using System.Reflection;
using System.Net;
using System.Text.RegularExpressions;
using E_POS.Application.Common.Email;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using E_POS.Domain.Modules.Shared.Integration.Entities;
using E_POS.Domain.Modules.Tenant.TenantAuth.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Common.Security;
using E_POS.Infrastructure.Integrations.Email;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Services;
using E_POS.Infrastructure.Modules.Shared.Integration.Services;
using E_POS.Infrastructure.Modules.Tenant.TenantAuth.Options;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;

// Explicit live opt-in, one recipient, isolated database, no raw exception/configuration output.
try { return await Run(args.Contains("--send-one")); }
catch { Console.WriteLine("Probe failed; sensitive exception details suppressed. No automatic resend."); return 1; }

static async Task<int> Run(bool live)
{
    var root = Path.GetFullPath("."); // Run from Unified-Commerce.
    var config = new ConfigurationBuilder().SetBasePath(root)
        .AddJsonFile("src/E_POS.Api/appsettings.json")
        .AddJsonFile("src/E_POS.Api/appsettings.Development.json", optional:true)
        .AddUserSecrets("epos-api-development-secrets").AddEnvironmentVariables().Build();
    var connection = new NpgsqlConnectionStringBuilder(config.GetConnectionString("DefaultConnection"));
    if (connection.Host is not ("localhost" or "127.0.0.1")) throw new InvalidOperationException();
    connection.Database = "oneverz_phase_b_test_20260907";
    var dbOptions = new DbContextOptionsBuilder<EPosDbContext>().UseNpgsql(connection.ConnectionString).Options;
    await using var db = new EPosDbContext(dbOptions);
    if (!await db.Database.CanConnectAsync()) throw new InvalidOperationException();
    const string dedupe = "phase-b-authorized-email-english-v3-jevamathu0826";
    // Login resolves email globally. Never create another fixture for an existing recipient.
    if (await db.TenantUsers.AnyAsync(x => x.Email == GuardedSender.Recipient.ToUpperInvariant()))
    { Console.WriteLine("Recipient already exists. Use its normal invitation lifecycle; refusing a duplicate fixture."); return 2; }
    if (live && await db.IntegrationOutboxMessages.AnyAsync(x => x.DeduplicationKey == dedupe))
    { Console.WriteLine("Live attempt already recorded; refusing duplicate send."); return 2; }
    var now = DateTimeOffset.UtcNow;
    var tenantId = Guid.NewGuid(); var userId = Guid.NewGuid(); var adminId = Guid.NewGuid();
    var draftId = Guid.NewGuid(); var operationId = Guid.NewGuid(); var outboxId = Guid.NewGuid();
    var suffix = Guid.NewGuid().ToString("N")[..8];
    db.PlatformUsers.Add(PlatformUser.Create(adminId, $"probe-{suffix}@example.invalid", "not-a-login-hash", "ACTIVE", now));
    db.Tenants.Add(Tenant.Create(tenantId, $"PROBE-{suffix}", $"probe-{suffix}", "Phase B email test", "ACTIVE", "LKR", "Asia/Colombo", null, null, now));
    db.TenantUsers.Add(TenantUser.Create(userId, tenantId, GuardedSender.Recipient, "Invitation test admin", null, null,
        "not-a-login-hash", "not-a-login-salt", "INVITED", "TENANT_ADMIN", "admin", "MAIN", now, staffCode:$"P-{suffix}"));
    db.PlatformTenantOnboardingDrafts.Add(PlatformTenantOnboardingDraft.Create(draftId, adminId, "{}", 7, 127, 100, now, now.AddDays(1)));
    db.PlatformTenantOnboardingOperations.Add(PlatformTenantOnboardingOperation.CreateCompleted(operationId, draftId, tenantId,
        suffix, suffix, "NOT_REQUIRED", "PENDING", now));
    var message = IntegrationOutboxMessage.Create(outboxId, "tenant_admin.invitation_requested", "tenant_onboarding", operationId,
        1, tenantId, Guid.NewGuid(), null, "{}", live ? dedupe : $"probe-dry-{suffix}", now);
    db.IntegrationOutboxMessages.Add(message);
    await db.SaveChangesAsync();
    Console.WriteLine("Outbox created: True");
    var tokenService = new InvitationTokenService(new TokenHashService(), Options.Create(new TenantJwtOptions { SigningKey="012345678901234567890123456789012" }));
    var emailOptions = config.GetSection("AzureCommunicationEmail").Get<AzureCommunicationEmailOptions>() ?? new();
    var actual = new AzureCommunicationEmailSender(Options.Create(emailOptions), NullLogger<AzureCommunicationEmailSender>.Instance);
    var guard = new GuardedSender(actual, dbOptions, tokenService, userId, live);
    var services = new ServiceCollection();
    services.AddDbContext<EPosDbContext>(o => o.UseNpgsql(connection.ConnectionString));
    services.AddSingleton<IApplicationEmailSender>(guard);
    services.AddSingleton<IInvitationTokenService>(tokenService);
    await using var provider = services.BuildServiceProvider();
    var worker = new TenantOnboardingOutboxWorker(provider.GetRequiredService<IServiceScopeFactory>(),
        Options.Create(new TenantOnboardingOutboxOptions { Enabled=true, MaximumAttempts=1, TenantAdminAppBaseUrl="http://localhost:4200" }),
        NullLogger<TenantOnboardingOutboxWorker>.Instance);
    // Acquire ONLY this message. Never run the background worker or claim unrelated pending emails.
    const string owner = "phase-b-single-email-probe";
    typeof(TenantOnboardingOutboxWorker).GetField("_workerId", BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(worker, owner);
    message.TryAcquire(owner, now, TimeSpan.FromMinutes(10));
    await db.SaveChangesAsync();
    using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
    await (Task)typeof(TenantOnboardingOutboxWorker).GetMethod("ProcessAsync", BindingFlags.NonPublic|BindingFlags.Instance)!
        .Invoke(worker, new object[]{outboxId, timeout.Token})!;
    await db.Entry(message).ReloadAsync();
    var invite = await db.UserInvites.AsNoTracking().SingleOrDefaultAsync(x=>x.TenantId==tenantId);
    var op = await db.PlatformTenantOnboardingOperations.AsNoTracking().SingleAsync(x=>x.Id==operationId);
    Console.WriteLine($"Mode: {(live ? "LIVE" : "DRY RUN - NO EMAIL")}");
    Console.WriteLine($"URL/token binding verified: {guard.Validated}");
    Console.WriteLine($"Provider call count: {guard.Calls}");
    Console.WriteLine($"ACS completed successfully: {guard.Accepted}");
    Console.WriteLine($"Outbox delivered: {message.Status=="DELIVERED"}");
    Console.WriteLine($"Invitation marked sent: {invite?.InviteStatus=="SENT"}");
    Console.WriteLine($"Onboarding operation marked sent: {op.InvitationStatus=="SENT"}");
    if (message.Status!="DELIVERED") Console.WriteLine($"Failure code: {message.LastErrorCode}");
    return message.Status=="DELIVERED" && guard.Validated ? 0 : 1;
}

sealed class GuardedSender(IApplicationEmailSender actual, DbContextOptions<EPosDbContext> dbOptions,
    IInvitationTokenService tokens, Guid expectedUser, bool live) : IApplicationEmailSender
{
    public const string Recipient="jevamathu0826@gmail.com";
    public bool IsConfigured => !live || actual.IsConfigured;
    public bool Validated {get;private set;}
    public int Calls {get;private set;}
    public bool Accepted {get;private set;}
    public async Task<ApplicationResult<ApplicationEmailSendResult>> SendAsync(ApplicationEmailMessage message, CancellationToken ct)
    {
        Console.WriteLine("Preflight: sender guard entered");
        if (!message.HtmlBody.Contains("Confirm Password") || !message.HtmlBody.Contains("Login email:") ||
            message.PlainTextBody is null || !message.Subject.Contains("Activate your Tenant Admin account"))
            throw new InvalidOperationException();
        if(!string.Equals(message.ToAddress,Recipient,StringComparison.OrdinalIgnoreCase) || Calls!=0) throw new InvalidOperationException();
        var match=Regex.Match(message.HtmlBody,"href=\"([^\"]+)\"");
        var url=new Uri(WebUtility.HtmlDecode(match.Groups[1].Value));
        if(url.GetLeftPart(UriPartial.Authority)!="http://localhost:4200" || !url.AbsolutePath.StartsWith("/tenant-admin/setup/")) throw new InvalidOperationException();
        var token=Uri.UnescapeDataString(url.Segments.Last());
        await using var db=new EPosDbContext(dbOptions);
        var hash=tokens.HashToken(token);
        var invite=await db.UserInvites.AsNoTracking().SingleAsync(x=>x.InviteTokenHash==hash,ct);
        Console.WriteLine($"Preflight: user binding matches: {invite.TenantUserId==expectedUser}; invitation usable: {invite.IsUsableAt(DateTimeOffset.UtcNow)}");
        if(invite.TenantUserId!=expectedUser || !invite.IsUsableAt(DateTimeOffset.UtcNow)) throw new InvalidOperationException();
        using var http=new HttpClient {BaseAddress=new Uri("http://127.0.0.1:5151")};
        using var response=await http.GetAsync($"/api/tenant-admin/onboarding/setup-token/{Uri.EscapeDataString(token)}/validate",ct);
        Console.WriteLine($"Preflight: validation HTTP status: {(int)response.StatusCode}");
        using var result=System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        if(!response.IsSuccessStatusCode || !result.RootElement.GetProperty("valid").GetBoolean()) throw new InvalidOperationException();
        Validated=true;
        if(!live) return ApplicationResult<ApplicationEmailSendResult>.Success(new("dry-run","Succeeded","dry-run"));
        Calls++;
        var sent=await actual.SendAsync(message,ct);
        Accepted=sent.IsSuccess;
        return sent;
    }
}
