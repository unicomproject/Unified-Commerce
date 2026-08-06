using E_POS.LocalPrintAgent.Configuration;
using E_POS.LocalPrintAgent.Idempotency;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace E_POS.LocalPrintAgent.Tests;

public sealed class FilePrintRequestStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "e-pos-agent-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Status_survives_store_restart_and_reports_completion()
    {
        Directory.CreateDirectory(_root);
        var requestId = Guid.NewGuid();
        var first = CreateStore();

        var claim = await first.TryClaimAsync(requestId, "HASH", CancellationToken.None);
        var accepted = await first.GetStatusAsync(requestId, CancellationToken.None);
        await first.CompleteAsync(requestId, true, "printed", CancellationToken.None);

        var restarted = CreateStore();
        var completed = await restarted.GetStatusAsync(requestId, CancellationToken.None);

        Assert.True(claim.Acquired);
        Assert.Equal("accepted", accepted?.State);
        Assert.Equal("completed", completed?.State);
        Assert.True(completed?.Success);
        Assert.Equal("printed", completed?.ResultCode);
    }

    [Fact]
    public async Task Unknown_request_returns_null()
    {
        Directory.CreateDirectory(_root);
        Assert.Null(await CreateStore().GetStatusAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task Corrupted_record_is_quarantined_and_never_treated_as_missing()
    {
        Directory.CreateDirectory(Path.Combine(_root, "operations"));
        var requestId = Guid.NewGuid();
        await File.WriteAllTextAsync(
            Path.Combine(_root, "operations", $"{requestId:N}.json"),
            "{not-json");

        var exception = await Assert.ThrowsAsync<IdempotencyRecordCorruptedException>(
            () => CreateStore().GetStatusAsync(requestId, CancellationToken.None));

        Assert.Equal(requestId, exception.RequestId);
        Assert.NotEmpty(Directory.GetFiles(
            Path.Combine(_root, "operations", "quarantine"), "*.corrupt"));
    }

    [Fact]
    public async Task Concurrent_duplicate_claim_is_acquired_only_once()
    {
        Directory.CreateDirectory(_root);
        var store = CreateStore();
        var requestId = Guid.NewGuid();
        var claims = await Task.WhenAll(
            Enumerable.Range(0, 12).Select(_ =>
                store.TryClaimAsync(requestId, "HASH", CancellationToken.None)));

        Assert.Single(claims, x => x.Acquired);
        Assert.Equal(11, claims.Count(x => !x.Acquired && !x.PayloadConflict));
    }

    [Fact]
    public async Task Retention_removes_old_completed_but_preserves_old_unresolved_operations()
    {
        Directory.CreateDirectory(_root);
        var completedId = Guid.NewGuid();
        var unresolvedId = Guid.NewGuid();
        var store = CreateStore();
        await store.TryClaimAsync(completedId, "COMPLETED", CancellationToken.None);
        await store.CompleteAsync(completedId, true, "printed", CancellationToken.None);
        await store.TryClaimAsync(unresolvedId, "UNRESOLVED", CancellationToken.None);

        var operations = Path.Combine(_root, "operations");
        File.SetLastWriteTimeUtc(
            Path.Combine(operations, $"{completedId:N}.json"),
            DateTime.UtcNow.AddDays(-31));
        File.SetLastWriteTimeUtc(
            Path.Combine(operations, $"{unresolvedId:N}.json"),
            DateTime.UtcNow.AddDays(-31));

        var restarted = CreateStore();

        Assert.Null(await restarted.GetStatusAsync(completedId, CancellationToken.None));
        Assert.Equal(
            "accepted",
            (await restarted.GetStatusAsync(unresolvedId, CancellationToken.None))?.State);
    }

    [Fact]
    public void Agent_start_count_survives_store_restart()
    {
        Directory.CreateDirectory(_root);

        Assert.Equal(1, CreateStore().RecordAgentStart());
        Assert.Equal(2, CreateStore().RecordAgentStart());
    }

    private FilePrintRequestStore CreateStore() =>
        new(Options.Create(new PrintAgentOptions
        {
            PrinterName = "Test Printer",
            LocalApiKey = "123456789012345678901234",
            IdempotencyDirectory = "operations",
            OperationRetentionDays = 30
        }), new TestEnvironment(_root));

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    private sealed class TestEnvironment(string contentRoot) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "E_POS.LocalPrintAgent.Tests";
        public string ContentRootPath { get; set; } = contentRoot;
        public IFileProvider ContentRootFileProvider { get; set; } =
            new NullFileProvider();
    }
}
