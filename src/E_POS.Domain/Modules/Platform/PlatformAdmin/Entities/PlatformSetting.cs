using System.Text.Json;
using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;

public class PlatformSetting : BaseEntity
{
    public string SettingKey { get; protected set; } = string.Empty;

    public string SettingValue { get; protected set; } = "null";

    public bool IsSecret { get; protected set; }

    public string? Description { get; protected set; }

    public Guid? UpdatedByPlatformUserId { get; protected set; }

    public DateTimeOffset CreatedAt { get; protected set; }

    public DateTimeOffset UpdatedAt { get; protected set; }

    public static PlatformSetting Create(
        Guid id,
        string settingKey,
        string? value,
        bool isSecret,
        string? description,
        DateTimeOffset now)
    {
        var setting = new PlatformSetting
        {
            Id = id,
            SettingKey = settingKey,
            IsSecret = isSecret,
            Description = description,
            CreatedAt = now,
            UpdatedAt = now
        };

        setting.SetStringValue(value);
        return setting;
    }

    public string? GetStringValue()
    {
        if (string.IsNullOrWhiteSpace(SettingValue) ||
            string.Equals(SettingValue, "null", StringComparison.Ordinal))
        {
            return null;
        }

        return JsonSerializer.Deserialize<string>(SettingValue);
    }

    public void SetStringValue(string? value)
    {
        SettingValue = value is null
            ? "null"
            : JsonSerializer.Serialize(value);
    }

    public void UpdateValue(string? value, Guid updatedByPlatformUserId, DateTimeOffset now)
    {
        SetStringValue(value);
        UpdatedByPlatformUserId = updatedByPlatformUserId;
        UpdatedAt = now;
    }
}

