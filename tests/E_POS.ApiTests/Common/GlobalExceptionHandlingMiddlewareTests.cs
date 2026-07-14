using System.Text.Json;
using E_POS.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

namespace E_POS.ApiTests.Common;

public sealed class GlobalExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenUnhandledException_WritesSafeStandardError()
    {
        var context = CreateContext();
        var middleware = new GlobalExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("database connection failed"),
            NullLogger<GlobalExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        var root = await ReadResponseAsync(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("internal_server_error", root.GetProperty("code").GetString());
        Assert.Equal("An unexpected error occurred.", root.GetProperty("message").GetString());
        Assert.False(root.GetRawText().Contains("database connection failed", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task InvokeAsync_WhenUniqueViolation_ReturnsConflict()
    {
        var context = CreateContext();
        var middleware = new GlobalExceptionHandlingMiddleware(
            _ => throw CreateDbUpdateException(
                new PostgresException(
                    "duplicate key value violates unique constraint",
                    severity: "ERROR",
                    invariantSeverity: "ERROR",
                    sqlState: "23505")),
            NullLogger<GlobalExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        var root = await ReadResponseAsync(context);

        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        Assert.Equal("data_conflict", root.GetProperty("code").GetString());
    }

    [Fact]
    public async Task InvokeAsync_WhenForeignKeyViolation_ReturnsBadRequest()
    {
        var context = CreateContext();
        var middleware = new GlobalExceptionHandlingMiddleware(
            _ => throw CreateDbUpdateException(
                new PostgresException(
                    "insert or update on table violates foreign key constraint",
                    severity: "ERROR",
                    invariantSeverity: "ERROR",
                    sqlState: "23503")),
            NullLogger<GlobalExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        var root = await ReadResponseAsync(context);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal("data_constraint_violation", root.GetProperty("code").GetString());
    }

    [Fact]
    public async Task InvokeAsync_WhenDbUpdateExceptionWithoutPostgresCause_ReturnsDataUpdateFailed()
    {
        var context = CreateContext();
        var middleware = new GlobalExceptionHandlingMiddleware(
            _ => throw new DbUpdateException("save failed"),
            NullLogger<GlobalExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        var root = await ReadResponseAsync(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("data_update_failed", root.GetProperty("code").GetString());
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<JsonElement> ReadResponseAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        return document.RootElement.Clone();
    }

    private static DbUpdateException CreateDbUpdateException(Exception innerException)
    {
        return new DbUpdateException("update failed", innerException);
    }
}