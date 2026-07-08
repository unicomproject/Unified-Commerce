using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;

namespace E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;

public class Till : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid? OutletId { get; protected set; }
    public string Name { get; protected set; } = string.Empty;
    public string Status { get; protected set; } = string.Empty;
    public string TillCode { get; protected set; } = string.Empty;
    public string? DeviceName { get; protected set; }
    public string? PrinterName { get; protected set; }
    public string? ScannerName { get; protected set; }
    public string? CashDrawerName { get; protected set; }
    public string? CardReaderName { get; protected set; }
    public string? InternalNote { get; protected set; }

    public static Till Create(
        Guid id,
        Guid tenantId,
        Guid outletId,
        string name,
        string tillCode,
        string status,
        DateTimeOffset now,
        string? deviceName = null,
        string? printerName = null,
        string? scannerName = null,
        string? cashDrawerName = null,
        string? cardReaderName = null,
        string? internalNote = null)
    {
        return new Till
        {
            Id = id,
            TenantId = tenantId,
            OutletId = outletId,
            Name = name.Trim(),
            TillCode = TillConstants.NormalizeTillCode(tillCode),
            Status = TillConstants.NormalizeStatus(status),
            DeviceName = NormalizeOptional(deviceName),
            PrinterName = NormalizeOptional(printerName),
            ScannerName = NormalizeOptional(scannerName),
            CashDrawerName = NormalizeOptional(cashDrawerName),
            CardReaderName = NormalizeOptional(cardReaderName),
            InternalNote = NormalizeOptional(internalNote),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateProfile(
        Guid outletId,
        string name,
        string tillCode,
        string status,
        DateTimeOffset now,
        string? deviceName = null,
        string? printerName = null,
        string? scannerName = null,
        string? cashDrawerName = null,
        string? cardReaderName = null,
        string? internalNote = null)
    {
        OutletId = outletId;
        Name = name.Trim();
        TillCode = TillConstants.NormalizeTillCode(tillCode);
        Status = TillConstants.NormalizeStatus(status);
        DeviceName = NormalizeOptional(deviceName);
        PrinterName = NormalizeOptional(printerName);
        ScannerName = NormalizeOptional(scannerName);
        CashDrawerName = NormalizeOptional(cashDrawerName);
        CardReaderName = NormalizeOptional(cardReaderName);
        InternalNote = NormalizeOptional(internalNote);
        UpdatedAt = now;
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    public void SoftDelete(DateTimeOffset now)
    {
        Status = TillConstants.DeletedStatus;
        UpdatedAt = now;
    }
}

