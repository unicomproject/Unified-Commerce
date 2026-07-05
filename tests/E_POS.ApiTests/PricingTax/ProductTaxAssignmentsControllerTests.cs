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

public sealed class ProductTaxAssignmentsControllerTests
{
    [Fact]
    public async Task Create_Returns201Created()
    {
        var service = new FakeProductTaxAssignmentService { CreateResult = ApplicationResult<Guid>.Success(Guid.NewGuid()) };
        var factory = new FakeTenantRequestContextFactory();
        var controller = new ProductTaxAssignmentsController(service, factory)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            }
        };

        var request = new ProductTaxAssignmentCreateRequest { ProductId = Guid.NewGuid(), TaxClassId = Guid.NewGuid() };

        var result = await controller.Create(request, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
        Assert.Equal("Get", createdResult.ActionName);
    }

    [Fact]
    public async Task Create_Returns409Conflict_WhenOverlap()
    {
        var error = new ApplicationError("pricing.product_tax_assignment.overlap", "Overlap");
        var service = new FakeProductTaxAssignmentService { CreateResult = ApplicationResult<Guid>.Failure(error) };
        var factory = new FakeTenantRequestContextFactory();
        var controller = new ProductTaxAssignmentsController(service, factory)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            }
        };

        var request = new ProductTaxAssignmentCreateRequest { ProductId = Guid.NewGuid(), TaxClassId = Guid.NewGuid() };

        var result = await controller.Create(request, CancellationToken.None);

        var objectResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(409, objectResult.StatusCode);
    }

    private sealed class FakeProductTaxAssignmentService : IProductTaxAssignmentService
    {
        public ApplicationResult<Guid>? CreateResult { get; set; }

        public Task<ApplicationResult<Guid>> CreateAsync(TenantRequestContext context, ProductTaxAssignmentCreateRequest request, CancellationToken cancellationToken) => Task.FromResult(CreateResult!);
        public Task<ApplicationResult<bool>> UpdateAsync(TenantRequestContext context, Guid id, ProductTaxAssignmentUpdateRequest request, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<ApplicationResult<ProductTaxAssignmentResponse>> GetAsync(TenantRequestContext context, Guid id, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<ApplicationResult<ProductTaxAssignmentListResponse>> GetByProductAsync(TenantRequestContext context, Guid productId, int pageNumber, int pageSize, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<ApplicationResult<bool>> DeleteAsync(TenantRequestContext context, Guid id, CancellationToken cancellationToken) => throw new NotImplementedException();
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
