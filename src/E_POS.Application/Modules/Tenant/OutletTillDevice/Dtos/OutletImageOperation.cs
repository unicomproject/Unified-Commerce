using System.Text.Json.Serialization;

namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OutletImageOperation
{
    KEEP,
    REPLACE,
    REMOVE
}
