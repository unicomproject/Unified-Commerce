using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;

namespace E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;

public class Till : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid OutletId { get; protected set; }
    public string TillCode { get; protected set; } = string.Empty;
    public string TillName { get; protected set; } = string.Empty;
    public string TillAreaName { get; protected set; } = string.Empty;
    public int TillNumber { get; protected set; }
    public string TillType { get; protected set; } = string.Empty;
    public decimal DefaultOpeningFloatAmount { get; protected set; }
    public string CurrencyCode { get; protected set; } = string.Empty;
    public bool IsCashManaged { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public string? DeviceName { get; protected set; }
    public string? PrinterName { get; protected set; }
    public string? ScannerName { get; protected set; }
    public string? CashDrawerName { get; protected set; }
    public string? CardReaderName { get; protected set; }
    public string? InternalNote { get; protected set; }
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static Till Create(
        Guid id,
        Guid tenantId,
        Guid outletId,
        string tillName,
        string tillAreaName,
        int tillNumber,
        string tillCode,
        string tillType,
        decimal defaultOpeningFloatAmount,
        string currencyCode,
        bool isCashManaged,
        string status,
        Guid? createdByTenantUserId,
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
            TillName = tillName.Trim(),
            TillAreaName = TillConstants.NormalizeAreaName(tillAreaName),
            TillNumber = tillNumber,
            TillCode = TillConstants.NormalizeTillCode(tillCode),
            TillType = TillConstants.NormalizeTillType(tillType),
            DefaultOpeningFloatAmount = defaultOpeningFloatAmount,
            CurrencyCode = TillConstants.NormalizeCurrencyCode(currencyCode),
            IsCashManaged = isCashManaged,
            Status = TillConstants.NormalizeStatus(status),
            DeviceName = NormalizeOptional(deviceName),
            PrinterName = NormalizeOptional(printerName),
            ScannerName = NormalizeOptional(scannerName),
            CashDrawerName = NormalizeOptional(cashDrawerName),
            CardReaderName = NormalizeOptional(cardReaderName),
            InternalNote = NormalizeOptional(internalNote),
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateProfile(
        Guid outletId,
        string tillName,
        string tillAreaName,
        int tillNumber,
        string tillCode,
        string tillType,
        decimal defaultOpeningFloatAmount,
        string currencyCode,
        bool isCashManaged,
        string status,
        Guid? updatedByTenantUserId,
        DateTimeOffset now,
        string? deviceName = null,
        string? printerName = null,
        string? scannerName = null,
        string? cashDrawerName = null,
        string? cardReaderName = null,
        string? internalNote = null)
    {
        OutletId = outletId;
        TillName = tillName.Trim();
        TillAreaName = TillConstants.NormalizeAreaName(tillAreaName);
        TillNumber = tillNumber;
        TillCode = TillConstants.NormalizeTillCode(tillCode);
        TillType = TillConstants.NormalizeTillType(tillType);
        DefaultOpeningFloatAmount = defaultOpeningFloatAmount;
        CurrencyCode = TillConstants.NormalizeCurrencyCode(currencyCode);
        IsCashManaged = isCashManaged;
        Status = TillConstants.NormalizeStatus(status);
        DeviceName = NormalizeOptional(deviceName);
        PrinterName = NormalizeOptional(printerName);
        ScannerName = NormalizeOptional(scannerName);
        CashDrawerName = NormalizeOptional(cashDrawerName);
        CardReaderName = NormalizeOptional(cardReaderName);
        InternalNote = NormalizeOptional(internalNote);
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    public void SoftDelete(Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        Status = TillConstants.DeletedStatus;
        UpdatedByTenantUserId = updatedByTenantUserId;
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
}
