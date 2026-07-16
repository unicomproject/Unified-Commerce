using System.Text;
using E_POS.Api.Common;
using E_POS.Api.Extensions;
using E_POS.Api.Middleware;
using E_POS.Application;
using E_POS.Application.Common.Security;
using E_POS.Infrastructure;
using E_POS.Infrastructure.Modules.Tenant.TenantAuth.Options;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Options;
using E_POS.Infrastructure.Modules.ECommerce.CustomerAuth.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

const string PlatformIdentityType = "platform_user";
const string TenantIdentityType = "tenant_user";
const string CustomerIdentityType = "customer";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<ITenantRequestContextFactory, TenantRequestContextFactory>();

var platformJwtOptions = builder.Configuration
    .GetSection(PlatformJwtOptions.SectionName)
    .Get<PlatformJwtOptions>() ?? new PlatformJwtOptions();
var tenantJwtOptions = builder.Configuration
    .GetSection(TenantJwtOptions.SectionName)
    .Get<TenantJwtOptions>() ?? new TenantJwtOptions();
var customerJwtOptions = builder.Configuration
    .GetSection(CustomerJwtOptions.SectionName)
    .Get<CustomerJwtOptions>() ?? new CustomerJwtOptions();

var signingKeys = new[]
    {
        platformJwtOptions.SigningKey,
        tenantJwtOptions.SigningKey,
        customerJwtOptions.SigningKey
    }
    .Where(key => !string.IsNullOrWhiteSpace(key))
    .Distinct(StringComparer.Ordinal)
    .Select(key => new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)))
    .ToArray();

builder.Services.AddApiRateLimiting();

// Validates JWT access tokens issued by platform and tenant login endpoints.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuers = new[] { platformJwtOptions.Issuer, tenantJwtOptions.Issuer, customerJwtOptions.Issuer }
                .Where(issuer => !string.IsNullOrWhiteSpace(issuer)),
            ValidateAudience = true,
            ValidAudiences = new[] { platformJwtOptions.Audience, tenantJwtOptions.Audience, customerJwtOptions.Audience }
                .Where(audience => !string.IsNullOrWhiteSpace(audience)),
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = signingKeys,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                if (context.Principal is null)
                {
                    context.Fail("Invalid authenticated principal.");
                    return;
                }

                var validator = context.HttpContext.RequestServices.GetRequiredService<IAuthSessionValidator>();
                if (!await validator.IsCurrentSessionActiveAsync(context.Principal, context.HttpContext.RequestAborted))
                {
                    context.Fail("Invalid or revoked auth session.");
                }
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Keep platform and tenant JWTs from being accepted interchangeably by future APIs.
    options.AddPolicy("PlatformOnly", policy => policy
        .RequireAuthenticatedUser()
        .RequireClaim("identity_type", PlatformIdentityType)
        .RequireClaim("aud", platformJwtOptions.Audience));

    options.AddPolicy("TenantOnly", policy => policy
        .RequireAuthenticatedUser()
        .RequireClaim("identity_type", TenantIdentityType)
        .RequireClaim("aud", tenantJwtOptions.Audience));

    options.AddPolicy("CustomerOnly", policy => policy
        .RequireAuthenticatedUser()
        .RequireClaim("identity_type", CustomerIdentityType)
        .RequireClaim("aud", customerJwtOptions.Audience));
});

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TM-EPOS Backend API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter a valid JWT access token."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Convert unexpected runtime failures into safe standard API errors.
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/api/v1/health", () =>
{
    return Results.Ok(new
    {
        service = "E_POS Backend API",
        status = "Healthy",
        architecture = "Clean Architecture + Repository/Service Pattern"
    });
})
.WithName("HealthCheck");

app.Run();

public partial class Program;
