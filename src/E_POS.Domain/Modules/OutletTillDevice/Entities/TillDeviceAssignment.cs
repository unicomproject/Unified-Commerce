using System.Globalization;
using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.OutletTillDevice.Constants;

namespace E_POS.Domain.Modules.OutletTillDevice.Entities;

public class TillDeviceAssignment : AuditableEntity
{
    public Guid? TillId { get; protected set; }
    public Guid? PosDeviceId { get; protected set; }
    public string EffectiveFrom { get; protected set; } = string.Empty;
    public DateTimeOffset? EffectiveTo { get; protected set; }
    public string Status { get; protected set; } = string.Empty;

    public static TillDeviceAssignment Create(Guid id, Guid tillId, Guid posDeviceId, DateTimeOffset now)
    {
        return new TillDeviceAssignment
        {
            Id = id,
            TillId = tillId,
            PosDeviceId = posDeviceId,
            EffectiveFrom = now.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
            Status = TillDeviceAssignmentConstants.ActiveStatus,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Revoke(DateTimeOffset now)
    {
        Status = TillDeviceAssignmentConstants.RevokedStatus;
        EffectiveTo = now;
        UpdatedAt = now;
    }
}