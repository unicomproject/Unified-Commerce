using System.Net;
using System.Security.Cryptography;
using System.Text;
using E_POS.Application.Modules.Tenant.OnlineStoreSetup.Contracts;
using E_POS.Infrastructure.Modules.Tenant.OnlineStoreSetup.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace E_POS.UnitTests.OnlineStore;

public sealed class OnlineStoreProviderTests
{
    [Fact]
    public async Task DnsProvider_ReturnsVerified_WhenExpectedTxtTokenExists()
    {
        const string token = "oneverz-test-token";
        var provider = CreateDnsProvider(HttpStatusCode.OK, """{"Answer":[{"data":"\"oneverz-test-token\""}]}""");

        var result = await provider.VerifyTxtRecordAsync("store.example.com", Hash(token), CancellationToken.None);

        Assert.Equal(DomainVerificationProviderStatus.Verified, result.Status);
    }

    [Fact]
    public async Task DnsProvider_ReturnsNotFound_WhenTxtTokenDoesNotMatch()
    {
        var provider = CreateDnsProvider(HttpStatusCode.OK, """{"Answer":[{"data":"\"wrong-token\""}]}""");

        var result = await provider.VerifyTxtRecordAsync("store.example.com", Hash("expected-token"), CancellationToken.None);

        Assert.Equal(DomainVerificationProviderStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task DnsProvider_ReturnsUnavailable_WhenProviderIsDisabled()
    {
        var options = Options.Create(new DomainVerificationOptions { Enabled = false });
        var provider = new DnsOverHttpsDomainVerificationProvider(new HttpClient(new StubHandler(HttpStatusCode.OK, "{}")), options);

        var result = await provider.VerifyTxtRecordAsync("store.example.com", Hash("token"), CancellationToken.None);

        Assert.Equal(DomainVerificationProviderStatus.Unavailable, result.Status);
    }

    [Fact]
    public async Task DnsProvider_ReturnsTimeout_WhenProviderTimesOut()
    {
        var provider = CreateDnsProvider(new ThrowingHandler(new TaskCanceledException()));

        var result = await provider.VerifyTxtRecordAsync("store.example.com", Hash("token"), CancellationToken.None);

        Assert.Equal(DomainVerificationProviderStatus.Timeout, result.Status);
    }

    [Fact]
    public async Task DnsProvider_RetryCanVerifyAfterRecordPropagation()
    {
        const string token = "propagated-token";
        var provider = CreateDnsProvider(new SequenceHandler(
            Response(HttpStatusCode.OK, "{}"),
            Response(HttpStatusCode.OK, """{"Answer":[{"data":"\"propagated-token\""}]}""")));

        var first = await provider.VerifyTxtRecordAsync("store.example.com", Hash(token), CancellationToken.None);
        var second = await provider.VerifyTxtRecordAsync("store.example.com", Hash(token), CancellationToken.None);

        Assert.Equal(DomainVerificationProviderStatus.NotFound, first.Status);
        Assert.Equal(DomainVerificationProviderStatus.Verified, second.Status);
    }

    [Fact]
    public async Task DnsProvider_RotatedTokenInvalidatesOldHashAndVerifiesNewHash()
    {
        const string rotatedToken = "rotated-token";
        var provider = CreateDnsProvider(HttpStatusCode.OK, """{"Answer":[{"data":"\"rotated-token\""}]}""");

        var oldResult = await provider.VerifyTxtRecordAsync("store.example.com", Hash("old-token"), CancellationToken.None);
        var rotatedResult = await provider.VerifyTxtRecordAsync("store.example.com", Hash(rotatedToken), CancellationToken.None);

        Assert.Equal(DomainVerificationProviderStatus.NotFound, oldResult.Status);
        Assert.Equal(DomainVerificationProviderStatus.Verified, rotatedResult.Status);
    }

    [Fact]
    public async Task CertificateProvider_MapsActiveProviderState()
    {
        var issuedAt = DateTimeOffset.Parse("2026-08-27T10:00:00Z");
        var options = Options.Create(new CertificateProvisioningOptions
        {
            Enabled = true,
            ProvisionEndpoint = "https://certificates.example.test/provision",
            StatusEndpoint = "https://certificates.example.test/domains/{domainId}",
            BearerToken = "test-token"
        });
        var provider = new HttpCertificateProvisioningProvider(
            new HttpClient(new StubHandler(HttpStatusCode.OK, $$"""{"status":"ACTIVE","issuedAt":"{{issuedAt:O}}","expiresAt":"2027-08-27T10:00:00Z"}""")),
            options);

        var result = await provider.RequestAsync(Guid.NewGuid(), Guid.NewGuid(), "store.example.com", CancellationToken.None);

        Assert.Equal(CertificateProvisioningProviderStatus.Active, result.Status);
        Assert.Equal(issuedAt, result.IssuedAt);
    }

    [Fact]
    public async Task CertificateProvider_ReturnsUnavailable_WhenDisabled()
    {
        var provider = new HttpCertificateProvisioningProvider(
            new HttpClient(new StubHandler(HttpStatusCode.OK, "{}")),
            Options.Create(new CertificateProvisioningOptions { Enabled = false }));

        var result = await provider.RequestAsync(Guid.NewGuid(), Guid.NewGuid(), "store.example.com", CancellationToken.None);

        Assert.Equal(CertificateProvisioningProviderStatus.Unavailable, result.Status);
    }

    [Fact]
    public async Task CertificateProvider_ReturnsFailed_WhenProviderThrowsUnexpectedException()
    {
        var provider = CreateCertificateProvider(new ThrowingHandler(new InvalidOperationException("provider failure")));

        var result = await provider.RequestAsync(Guid.NewGuid(), Guid.NewGuid(), "store.example.com", CancellationToken.None);

        Assert.Equal(CertificateProvisioningProviderStatus.Failed, result.Status);
        Assert.Equal("certificate_provider_failed", result.FailureCode);
    }

    private static DnsOverHttpsDomainVerificationProvider CreateDnsProvider(HttpStatusCode statusCode, string body) =>
        CreateDnsProvider(new StubHandler(statusCode, body));

    private static DnsOverHttpsDomainVerificationProvider CreateDnsProvider(HttpMessageHandler handler) =>
        new(
            new HttpClient(handler),
            Options.Create(new DomainVerificationOptions
            {
                Enabled = true,
                QueryEndpoint = "https://dns.example.test/resolve",
                RecordNamePrefix = "_oneverz-verification"
            }));

    private static HttpCertificateProvisioningProvider CreateCertificateProvider(HttpMessageHandler handler) =>
        new(
            new HttpClient(handler),
            Options.Create(new CertificateProvisioningOptions
            {
                Enabled = true,
                ProvisionEndpoint = "https://certificates.example.test/provision",
                StatusEndpoint = "https://certificates.example.test/domains/{domainId}",
                BearerToken = "test-token"
            }));

    private static HttpResponseMessage Response(HttpStatusCode statusCode, string body) =>
        new(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed class StubHandler(HttpStatusCode statusCode, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
    }

    private sealed class ThrowingHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromException<HttpResponseMessage>(exception);
    }

    private sealed class SequenceHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private int _index;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responses[_index++]);
    }
}
