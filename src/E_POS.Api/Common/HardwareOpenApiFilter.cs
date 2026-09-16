using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace E_POS.Api.Common;

public sealed class HardwareOpenApiFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var controller = context.MethodInfo.DeclaringType?.Name;
        var native = controller is "PosHardwareController" or "PosHardwareTelemetryController";
        var registry = controller == "TenantAdminHardwareDevicesController";
        if (!native && !registry) return;
        operation.Parameters ??= new List<OpenApiParameter>();
        if (native)
            operation.Parameters.Add(new OpenApiParameter {
                Name = PosDeviceProofFilter.HeaderName, In = ParameterLocation.Header, Required = true,
                Description = "Native device activation proof from secure storage. Required in addition to the tenant JWT. Never put this value in URLs or logs.",
                Schema = new OpenApiSchema { Type = "string", Format = "password", MinLength = 78, MaxLength = 78 }
            });
        if (registry && context.ApiDescription.HttpMethod is "POST" or "PUT" or "DELETE")
            operation.Parameters.Add(new OpenApiParameter {
                Name = "Idempotency-Key", In = ParameterLocation.Header, Required = true,
                Description = "Reuse the same key and payload when retrying a registry mutation.",
                Schema = new OpenApiSchema { Type = "string", MinLength = 1, MaxLength = 100 }
            });
        operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Permission, hardware entitlement or native device proof denied." });
        operation.Responses.TryAdd("409", new OpenApiResponse { Description = "Configuration version, assignment or idempotency conflict." });
        if (native)
        {
            operation.Responses.TryAdd("413", new OpenApiResponse { Description = "Request body exceeds 32768 bytes." });
            operation.Responses.TryAdd("429", new OpenApiResponse { Description = "Hardware request rate exceeded." });
        }
    }
}
