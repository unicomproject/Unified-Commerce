using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Options;
using Microsoft.Extensions.Options;

namespace E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Services;

public sealed class ClamAvManualPaymentEvidenceScanner : IManualPaymentEvidenceScanner
{
    private readonly ManualPaymentEvidenceScannerOptions _options;
    public ClamAvManualPaymentEvidenceScanner(IOptions<ManualPaymentEvidenceScannerOptions> options) => _options = options.Value;

    public async Task<string> ScanAsync(Stream content, string contentType, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.Host)) return ManualPaymentConstants.ScanUnavailable;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(_options.TimeoutSeconds, 1, 120)));
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(_options.Host.Trim(), Math.Clamp(_options.Port, 1, 65535), timeout.Token);
            await using var network = client.GetStream();
            await network.WriteAsync("zINSTREAM\0"u8.ToArray(), timeout.Token);
            var buffer = new byte[8192];
            var lengthPrefix = new byte[4];
            int read;
            while ((read = await content.ReadAsync(buffer, timeout.Token)) > 0)
            {
                BinaryPrimitives.WriteInt32BigEndian(lengthPrefix, read);
                await network.WriteAsync(lengthPrefix, timeout.Token);
                await network.WriteAsync(buffer.AsMemory(0, read), timeout.Token);
            }
            Array.Clear(lengthPrefix);
            await network.WriteAsync(lengthPrefix, timeout.Token);
            await network.FlushAsync(timeout.Token);
            var responseBuffer = new byte[1024];
            var responseLength = await network.ReadAsync(responseBuffer, timeout.Token);
            var response = Encoding.UTF8.GetString(responseBuffer, 0, responseLength);
            if (response.Contains("FOUND", StringComparison.OrdinalIgnoreCase)) return ManualPaymentConstants.ScanRejected;
            if (response.Contains("OK", StringComparison.OrdinalIgnoreCase)) return ManualPaymentConstants.ScanClean;
            return ManualPaymentConstants.ScanUnavailable;
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            return ManualPaymentConstants.ScanUnavailable;
        }
    }
}
