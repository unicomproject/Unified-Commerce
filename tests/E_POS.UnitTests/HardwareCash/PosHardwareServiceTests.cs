using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.HardwareCash.Contracts;
using E_POS.Application.Modules.Tenant.HardwareCash.Dtos;
using E_POS.Application.Modules.Tenant.HardwareCash.Services;
using E_POS.Domain.Modules.Tenant.POSOperations.Constants;
using Xunit;

namespace E_POS.UnitTests.HardwareCash;

public sealed class PosHardwareServiceTests
{
    [Fact]
    public async Task SaveConfiguration_WithoutPermission_IsDenied()
    {
        var service = new PosHardwareService(new FakeRepository(), new Clock());
        var result = await service.SaveConfigurationAsync(
            Context([]), ValidPrinter(), CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Equal("pos_hardware.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task SaveConfiguration_LoopbackAgentUrl_IsRejected()
    {
        var service = new PosHardwareService(new FakeRepository(), new Clock());
        var request = ValidPrinter() with
        {
            ReceiptPrinter = ValidPrinter().ReceiptPrinter! with
            {
                AgentBaseUrl = "http://127.0.0.1:9101"
            }
        };
        var result = await service.SaveConfigurationAsync(
            Context([PosPermissions.Hardware.Settings]), request, CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Equal("pos_hardware.invalid_configuration", result.Error.Code);
    }

    [Fact]
    public async Task SaveConfiguration_DoesNotSerializeApiKey()
    {
        var repository = new FakeRepository();
        var service = new PosHardwareService(repository, new Clock());
        await service.SaveConfigurationAsync(
            Context([PosPermissions.Hardware.Settings]), ValidPrinter(), CancellationToken.None);
        Assert.DoesNotContain("secret", repository.SafeSettingsJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"localApiKeyPresent\":true", repository.SafeSettingsJson);
    }

    [Fact]
    public async Task CreateTest_UsesStableRequestHash()
    {
        var repository = new FakeRepository();
        var service = new PosHardwareService(repository, new Clock());
        var request = new CreateHardwareTestRequest(
            Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(),
            "receiptPrinter", "testPrint", 3);
        await service.CreateTestAsync(
            Context([PosPermissions.Hardware.Settings]), request, CancellationToken.None);
        var first = repository.RequestHash;
        await service.CreateTestAsync(
            Context([PosPermissions.Hardware.Settings]), request, CancellationToken.None);
        Assert.Equal(first, repository.RequestHash);
        Assert.Equal(64, repository.RequestHash.Length);
    }

    [Fact]
    public async Task CompleteTest_FreeTextCategory_IsRejected()
    {
        var service = new PosHardwareService(new FakeRepository(), new Clock());
        var result = await service.CompleteTestAsync(
            Context([PosPermissions.Hardware.Settings]), Guid.NewGuid(),
            new CompleteHardwareTestRequest("Passed", "made_up", null, true),
            CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Equal("pos_hardware.invalid_test_result", result.Error.Code);
    }

    [Fact]
    public async Task SaveScannerConfiguration_PreservesProductionSettings()
    {
        var repository = new FakeRepository();
        var service = new PosHardwareService(repository, new Clock());

        var result = await service.SaveConfigurationAsync(
            Context([PosPermissions.Hardware.Settings]),
            ValidScanner(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains("\"mode\":\"usbHid\"", repository.SafeSettingsJson);
        Assert.Contains("\"scanTimeout\":120", repository.SafeSettingsJson);
        Assert.Contains("\"minimumBarcodeLength\":4", repository.SafeSettingsJson);
        Assert.DoesNotContain("TB-00D", repository.SafeSettingsJson);
    }

    [Fact]
    public async Task SaveScannerConfiguration_InvalidLengthRange_IsRejected()
    {
        var service = new PosHardwareService(new FakeRepository(), new Clock());
        var request = ValidScanner() with
        {
            BarcodeScanner = ValidScanner().BarcodeScanner! with
            {
                MinimumBarcodeLength = 20,
                MaximumBarcodeLength = 10
            }
        };

        var result = await service.SaveConfigurationAsync(
            Context([PosPermissions.Hardware.Settings]),
            request,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("pos_hardware.invalid_configuration", result.Error.Code);
    }

    [Fact]
    public async Task CreateScannerTest_UnsupportedType_IsRejected()
    {
        var service = new PosHardwareService(new FakeRepository(), new Clock());
        var request = new CreateHardwareTestRequest(
            Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(),
            "barcodeScanner", "inventedTest", 1);

        var result = await service.CreateTestAsync(
            Context([PosPermissions.Hardware.Settings]),
            request,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("pos_hardware.invalid_test", result.Error.Code);
    }

    [Fact]
    public async Task CompleteScannerTest_SerializesOnlyPrivacySafeEvidence()
    {
        var repository = new FakeRepository();
        var service = new PosHardwareService(repository, new Clock());

        await service.CompleteTestAsync(
            Context([PosPermissions.Hardware.Settings]),
            Guid.NewGuid(),
            new CompleteHardwareTestRequest(
                "Passed", "scanner_input_detected", "Scanner input detected.",
                true, new DateTimeOffset(2026, 7, 29, 9, 0, 1, TimeSpan.Zero),
                "inputDetected",
                new ScannerTestEvidenceDto(
                    "usbHid", 13, new string('A', 64),
                    1, 1, 0, 0, 25, 25)),
            CancellationToken.None);

        Assert.Contains("\"barcodeLength\":13", repository.SafeResultPayloadJson);
        Assert.Contains(new string('A', 64), repository.SafeResultPayloadJson);
        Assert.DoesNotContain("0012345678905", repository.SafeResultPayloadJson);
    }

    [Fact]
    public async Task SaveCashDrawerConfiguration_PreservesTypedSafeSettings()
    {
        var repository = new FakeRepository();
        var service = new PosHardwareService(repository, new Clock());

        var result = await service.SaveConfigurationAsync(
            Context([PosPermissions.Hardware.Settings]), ValidDrawer(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains("\"drawerPort\":\"drawerPin2\"", repository.SafeSettingsJson);
        Assert.Contains("\"pulseOnMilliseconds\":100", repository.SafeSettingsJson);
        Assert.DoesNotContain("apiKey", repository.SafeSettingsJson,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveCashDrawerConfiguration_UnsafeTiming_IsRejected()
    {
        var service = new PosHardwareService(new FakeRepository(), new Clock());
        var request = ValidDrawer() with
        {
            CashDrawer = ValidDrawer().CashDrawer! with
            {
                PulseOnMilliseconds = 511
            }
        };

        var result = await service.SaveConfigurationAsync(
            Context([PosPermissions.Hardware.Settings]), request,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("pos_hardware.invalid_configuration", result.Error.Code);
    }

    [Fact]
    public async Task CreateCashDrawerTest_RequiresAuthoritativeConfiguration()
    {
        var service = new PosHardwareService(new FakeRepository(), new Clock());
        var request = new CreateHardwareTestRequest(
            Guid.NewGuid(), Guid.NewGuid(), null, null,
            "cashDrawer", "drawerPulse", 1);

        var result = await service.CreateTestAsync(
            Context([PosPermissions.Hardware.Settings]), request,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("pos_hardware.configuration_not_found", result.Error.Code);
    }

    [Fact]
    public async Task SaveDisabledCardTerminalFoundation_WithoutProvider_IsSafe()
    {
        var repository = new FakeRepository();
        var service = new PosHardwareService(repository, new Clock());

        var result = await service.SaveConfigurationAsync(
            Context([PosPermissions.Hardware.Settings]),
            new SavePosHardwareConfigurationRequest(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "cardTerminal",
                "providerManaged", "Card terminal", false, 0, "Foundation",
                null, null, null,
                new CardTerminalSettingsDto(
                    null, null, ConnectionMode: "providerManaged",
                    PairingStatus: "notConfigured", Currency: "LKR")),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains("\"pairingStatus\":\"notConfigured\"",
            repository.SafeSettingsJson);
        Assert.DoesNotContain("apiKey", repository.SafeSettingsJson,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveEnabledCardTerminal_WithoutRealProvider_IsBlocked()
    {
        var service = new PosHardwareService(new FakeRepository(), new Clock());
        var result = await service.SaveConfigurationAsync(
            Context([PosPermissions.Hardware.Settings]),
            new SavePosHardwareConfigurationRequest(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "cardTerminal",
                "providerManaged", "Card terminal", true, 0, null,
                null, null, null,
                new CardTerminalSettingsDto(
                    "uninstalled-provider", "terminal-1",
                    PairingStatus: "paired")),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("pos_hardware.external_dependency", result.Error.Code);
    }

    private static TenantRequestContext Context(IReadOnlyCollection<string> permissions) =>
        new(Guid.NewGuid(), Guid.NewGuid(), permissions);

    private static SavePosHardwareConfigurationRequest ValidPrinter() =>
        new(Guid.NewGuid(), Guid.NewGuid(), null, "receiptPrinter",
            "localPrintAgent", "POSPrinter POS80", true, 0, null,
            new ReceiptPrinterSettingsDto(
                "http://192.168.18.8:9101", "POSPrinter POS80",
                "80mm", true, 5000, 5, true),
            null, null, null);

    private static SavePosHardwareConfigurationRequest ValidScanner() =>
        new(Guid.NewGuid(), Guid.NewGuid(), null, "barcodeScanner",
            "usbHid", "Checkout Scanner", true, 0, null,
            null,
            new BarcodeScannerSettingsDto(
                "usbHid",
                ["ean13", "ean8", "upcA", "code128", "code39"],
                true),
            null, null);

    private static SavePosHardwareConfigurationRequest ValidDrawer() =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "cashDrawer",
            "localPrintAgent", "Till cash drawer", true, 0, "Initial setup",
            null, null,
            new CashDrawerSettingsDto(
                Guid.NewGuid(), "drawerPin2", 100, 200, "cashOnly",
                OpenOnCashSale: true,
                OpenOnCashRefund: true,
                OpenOnCashSplit: true,
                ManualOpenEnabled: false),
            null);

    private sealed class Clock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => new(2026, 7, 29, 9, 0, 0, TimeSpan.Zero);
    }

    private sealed class FakeRepository : IPosHardwareRepository
    {
        public string SafeSettingsJson { get; private set; } = string.Empty;
        public string RequestHash { get; private set; } = string.Empty;
        public string SafeResultPayloadJson { get; private set; } = string.Empty;

        public Task<IReadOnlyList<PosHardwareConfigurationDto>> GetConfigurationsAsync(
            Guid tenantId, Guid posDeviceId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<PosHardwareConfigurationDto>>([]);

        public Task<(string? ErrorCode, PosHardwareConfigurationDto? Configuration)> SaveConfigurationAsync(
            Guid tenantId, Guid userId, SavePosHardwareConfigurationRequest request,
            string safeSettingsJson, DateTimeOffset now, CancellationToken cancellationToken)
        {
            SafeSettingsJson = safeSettingsJson;
            return Task.FromResult<(string?, PosHardwareConfigurationDto?)>((null,
                new PosHardwareConfigurationDto(
                    Guid.NewGuid(), tenantId, request.OutletId, request.PosDeviceId,
                    request.TillId, request.HardwareType, request.TransportType,
                    request.DisplayName, request.Enabled, 1, false, null,
                    new { }, now)));
        }

        public Task<(string? ErrorCode, HardwareTestOperationDto? Operation)> CreateTestAsync(
            Guid tenantId, Guid userId, CreateHardwareTestRequest request,
            string requestPayloadHash, DateTimeOffset now, CancellationToken cancellationToken)
        {
            RequestHash = requestPayloadHash;
            return Task.FromResult<(string?, HardwareTestOperationDto?)>((null,
                Operation(request, now)));
        }

        public Task<(string? ErrorCode, HardwareTestOperationDto? Operation)> CompleteTestAsync(
            Guid tenantId, Guid userId, Guid testId, CompleteHardwareTestRequest request,
            string? safeResultPayloadJson, DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            SafeResultPayloadJson = safeResultPayloadJson ?? string.Empty;
            return Task.FromResult<(string?, HardwareTestOperationDto?)>((null, null));
        }

        public Task<IReadOnlyList<HardwareTestOperationDto>> GetTestHistoryAsync(
            Guid tenantId, Guid posDeviceId, int take, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<HardwareTestOperationDto>>([]);

        private static HardwareTestOperationDto Operation(
            CreateHardwareTestRequest request, DateTimeOffset now) =>
            new(Guid.NewGuid(), request.RequestId, request.PosDeviceId,
                request.TillId, null, request.HardwareConfigurationId,
                request.HardwareType, request.TestType, request.ConfigurationVersion,
                "Pending", null, null, null, now, null);
    }
}
