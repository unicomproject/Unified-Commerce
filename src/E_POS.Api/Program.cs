using E_POS.Application;
using E_POS.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseHttpsRedirection();

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
