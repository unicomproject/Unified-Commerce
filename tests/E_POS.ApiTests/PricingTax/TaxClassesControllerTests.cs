using System.Security.Claims;
using E_POS.Api.Common;
using E_POS.Api.Controllers.PricingTax;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.PricingTax.Contracts;
using E_POS.Application.Modules.PricingTax.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.PricingTax;

public sealed class TaxClassesControllerTests
{
    [Fact]
    public async Task Create_Returns201Created()
    {
        var service = new FakeTaxSetupService { CreateResult = ApplicationResult<Guid>.Success(Guid.NewGuid()) };
        var factory = new FakeTenantRequestContextFactory();
        var controller = new TaxClassesController(service, factory)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            }
        };

        var request = new TaxClassCreateRequest { TaxClassCode = "TC-1", TaxClassName = "Standard" };

        var result = await controller.CreateTaxClass(request, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
        Assert.Equal("GetTaxClass", createdResult.ActionName);
    }

    [Fact]
    public async Task Create_Returns403Forbidden_WhenPermissionDenied()
    {
        var error = new ApplicationError("pricing.tax_setup.permission_denied", "Denied");
        var service = new FakeTaxSetupService { CreateResult = ApplicationResult<Guid>.Failure(error) };
        var factory = new FakeTenantRequestContextFactory();
        var controller = new TaxClassesController(service, factory)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            }
        };

        var request = new TaxClassCreateRequest { TaxClassCode = "TC-1", TaxClassName = "Standard" };

        var result = await controller.CreateTaxClass(request, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, objectResult.StatusCode);
    }

    private sealed class FakeTaxSetupService : ITaxSetupService
    {
        public ApplicationResult<Guid>? CreateResult { get; set; }

        public Task<ApplicationResult<Guid>> CreateTaxClassAsync(TenantRequestContext context, TaxClassCreateRequest request, CancellationToken cancellationToken) => Task.FromResult(CreateResult!);
        public Task<ApplicationResult<bool>> UpdateTaxClassAsync(TenantRequestContext context, Guid id, TaxClassUpdateRequest request, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<ApplicationResult<TaxClassResponse>> GetTaxClassAsync(TenantRequestContext context, Guid id, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<ApplicationResult<TaxClassListResponse>> GetTaxClassesAsync(TenantRequestContext context, int pageNumber, int pageSize, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<ApplicationResult<bool>> DeleteTaxClassAsync(TenantRequestContext context, Guid id, CancellationToken cancellationToken) => throw new NotImplementedException();
        
        public Task<ApplicationResult<Guid>> CreateTaxRateAsync(TenantRequestContext context, TaxRateCreateRequest request, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<ApplicationResult<bool>> UpdateTaxRateAsync(TenantRequestContext context, Guid id, TaxRateUpdateRequest request, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<ApplicationResult<TaxRateResponse>> GetTaxRateAsync(TenantRequestContext context, Guid id, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<ApplicationResult<TaxRateListResponse>> GetTaxRatesAsync(TenantRequestContext context, int pageNumber, int pageSize, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<ApplicationResult<bool>> DeleteTaxRateAsync(TenantRequestContext context, Guid id, CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    private sealed class FakeTenantRequestContextFactory : ITenantRequestContextFactory
    {
        public bool TryCreate(ClaimsPrincipal principal, out TenantRequestContext context)
        {
            context = new TenantRequestContext(Guid.NewGuid(), Guid.NewGuid(), []);
            return true;
        }
    }
}
