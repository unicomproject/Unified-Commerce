namespace E_POS.Application.Modules.Tenant.TenantFoundation.Exceptions;

public sealed class MissingMandatoryTenantSettingDefinitionException : Exception
{
    public MissingMandatoryTenantSettingDefinitionException(string settingKey)
        : base($"Mandatory tenant setting definition '{settingKey}' is missing or inactive.")
    {
        SettingKey = settingKey;
    }

    public string SettingKey { get; }
}

public sealed class MissingPlatformGeneralDefaultException : Exception
{
    public MissingPlatformGeneralDefaultException(string settingKey)
        : base($"Required platform general default '{settingKey}' is missing.")
    {
        SettingKey = settingKey;
    }

    public string SettingKey { get; }
}

public sealed class InvalidTenantSettingDefaultValueException : Exception
{
    public InvalidTenantSettingDefaultValueException(string settingKey, string reason)
        : base($"Invalid default value for tenant setting '{settingKey}': {reason}")
    {
        SettingKey = settingKey;
    }

    public string SettingKey { get; }
}
