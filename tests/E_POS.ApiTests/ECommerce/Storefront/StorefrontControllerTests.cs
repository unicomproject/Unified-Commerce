using E_POS.Api.Controllers.V1.ECommerce.Storefront;
using E_POS.Application.Modules.ECommerce.Storefront.Contracts;
using E_POS.Application.Modules.ECommerce.Storefront.Dtos;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.ECommerce.Storefront;

public sealed class StorefrontControllerTests
{
    [Fact]
    public async Task ResolveTenant_WithBlankSlug_ReturnsBadRequest()
    {
        var service = new FakeStorefrontService();
        var controller = new StorefrontTenantController(service);

        var result = await controller.ResolveTenant(" ", CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Null(service.ResolvedSlug);
    }

    [Fact]
    public async Task ResolveTenant_WhenTenantMissing_ReturnsNotFound()
    {
        var service = new FakeStorefrontService { ResolvedTenantId = null };
        var controller = new StorefrontTenantController(service);

        var result = await controller.ResolveTenant("demo-store", CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("demo-store", service.ResolvedSlug);
    }

    [Fact]
    public async Task ResolveTenant_WhenTenantExists_ReturnsTenantId()
    {
        var tenantId = Guid.NewGuid();
        var service = new FakeStorefrontService { ResolvedTenantId = tenantId };
        var controller = new StorefrontTenantController(service);

        var result = await controller.ResolveTenant("demo-store", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        var returnedTenantId = ok.Value.GetType().GetProperty("tenantId")?.GetValue(ok.Value);
        Assert.Equal(tenantId, returnedTenantId);
    }

    [Fact]
    public async Task GetBanners_WithMissingTenantHeader_ReturnsBadRequest()
    {
        var service = new FakeStorefrontService();
        var controller = new StorefrontBannersController(service);

        var result = await controller.GetBanners(Guid.Empty, "HERO", CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Null(service.BannersTenantId);
    }

    [Fact]
    public async Task GetBanners_WithMissingBannerType_ReturnsBadRequest()
    {
        var service = new FakeStorefrontService();
        var controller = new StorefrontBannersController(service);

        var result = await controller.GetBanners(Guid.NewGuid(), " ", CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Null(service.BannersTenantId);
    }

    [Fact]
    public async Task GetBanners_WithValidRequest_ReturnsBannersAndPassesParameters()
    {
        var tenantId = Guid.NewGuid();
        var service = new FakeStorefrontService
        {
            Banners = [new StorefrontBannerReadModel { Id = Guid.NewGuid(), BannerType = "HERO", Title = "Hero", ImageUrl = "/hero.jpg" }]
        };
        var controller = new StorefrontBannersController(service);

        var result = await controller.GetBanners(tenantId, "HERO", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(service.Banners, ok.Value);
        Assert.Equal(tenantId, service.BannersTenantId);
        Assert.Equal("HERO", service.BannerType);
    }

    [Fact]
    public async Task GetCategories_WithMissingTenantHeader_ReturnsBadRequest()
    {
        var service = new FakeStorefrontService();
        var controller = new StorefrontCategoriesController(service);

        var result = await controller.GetCategories(Guid.Empty, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Null(service.RootCategoriesTenantId);
    }

    [Fact]
    public async Task GetCategories_WithValidRequest_ReturnsRootCategories()
    {
        var tenantId = Guid.NewGuid();
        var service = new FakeStorefrontService
        {
            RootCategories = [new StorefrontCategoryListReadModel { Id = Guid.NewGuid(), Name = "Clothing", Slug = "clothing", ItemCount = 124, SortOrder = 1 }]
        };
        var controller = new StorefrontCategoriesController(service);

        var result = await controller.GetCategories(tenantId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(service.RootCategories, ok.Value);
        Assert.Equal(tenantId, service.RootCategoriesTenantId);
    }

    [Fact]
    public async Task GetChildCategories_WithMissingTenantHeader_ReturnsBadRequest()
    {
        var service = new FakeStorefrontService();
        var controller = new StorefrontCategoriesController(service);

        var result = await controller.GetChildCategories(Guid.Empty, Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Null(service.ChildCategoriesTenantId);
    }

    [Fact]
    public async Task GetChildCategories_WithMissingCategoryId_ReturnsBadRequest()
    {
        var service = new FakeStorefrontService();
        var controller = new StorefrontCategoriesController(service);

        var result = await controller.GetChildCategories(Guid.NewGuid(), Guid.Empty, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Null(service.ChildCategoriesTenantId);
    }

    [Fact]
    public async Task GetChildCategories_WithValidRequest_ReturnsChildCategories()
    {
        var tenantId = Guid.NewGuid();
        var parentCategoryId = Guid.NewGuid();
        var service = new FakeStorefrontService
        {
            ChildCategories = [new StorefrontCategoryListReadModel { Id = Guid.NewGuid(), Name = "Jerseys", Slug = "jerseys", ItemCount = 35, SortOrder = 1 }]
        };
        var controller = new StorefrontCategoriesController(service);

        var result = await controller.GetChildCategories(tenantId, parentCategoryId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(service.ChildCategories, ok.Value);
        Assert.Equal(tenantId, service.ChildCategoriesTenantId);
        Assert.Equal(parentCategoryId, service.ChildCategoriesParentCategoryId);
    }

    [Fact]
    public async Task GetFeaturedCategories_WithMissingTenantHeader_ReturnsBadRequest()
    {
        var service = new FakeStorefrontService();
        var controller = new StorefrontCategoriesController(service);

        var result = await controller.GetFeaturedCategories(Guid.Empty, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Null(service.FeaturedCategoriesTenantId);
    }

    [Fact]
    public async Task GetFeaturedCategories_WithValidRequest_ReturnsCategories()
    {
        var tenantId = Guid.NewGuid();
        var service = new FakeStorefrontService
        {
            Categories = [new StorefrontCategoryReadModel { Id = Guid.NewGuid(), Name = "Fruit", ImageUrl = "/fruit.jpg" }]
        };
        var controller = new StorefrontCategoriesController(service);

        var result = await controller.GetFeaturedCategories(tenantId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(service.Categories, ok.Value);
        Assert.Equal(tenantId, service.FeaturedCategoriesTenantId);
    }

    [Fact]
    public async Task GetProducts_WithMissingTenantHeader_ReturnsBadRequest()
    {
        var service = new FakeStorefrontService();
        var controller = new StorefrontProductsController(service);

        var result = await controller.GetProducts(Guid.Empty, Guid.NewGuid(), "popular", 1, 20, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Null(service.ProductsTenantId);
    }

    [Fact]
    public async Task GetProducts_WithMissingCategoryId_ReturnsBadRequest()
    {
        var service = new FakeStorefrontService();
        var controller = new StorefrontProductsController(service);

        var result = await controller.GetProducts(Guid.NewGuid(), Guid.Empty, "popular", 1, 20, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Null(service.ProductsTenantId);
    }

    [Fact]
    public async Task GetProducts_WithValidRequest_ReturnsPagedProducts()
    {
        var tenantId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var service = new FakeStorefrontService
        {
            ProductListingPage = new StorefrontPagedReadModel<StorefrontProductListReadModel>
            {
                Items = [new StorefrontProductListReadModel { Id = Guid.NewGuid(), Name = "Man City Home Jersey", Slug = "man-city-home-jersey", Price = 74.99m, IsInStock = true }],
                TotalCount = 1,
                Page = 1,
                PageSize = 20
            }
        };
        var controller = new StorefrontProductsController(service);

        var result = await controller.GetProducts(tenantId, categoryId, "price_desc", 0, 100, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(service.ProductListingPage, ok.Value);
        Assert.Equal(tenantId, service.ProductsTenantId);
        Assert.Equal(categoryId, service.ProductsCategoryId);
        Assert.Equal("price_desc", service.ProductsSort);
        Assert.Equal(1, service.ProductsPage);
        Assert.Equal(50, service.ProductsPageSize);
    }

    [Fact]
    public async Task GetProductDetail_WithMissingTenantHeader_ReturnsBadRequest()
    {
        var service = new FakeStorefrontService();
        var controller = new StorefrontProductsController(service);

        var result = await controller.GetProductDetail(Guid.Empty, "man-city-home-jersey", CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Null(service.ProductDetailTenantId);
    }

    [Fact]
    public async Task GetProductDetail_WithMissingSlug_ReturnsBadRequest()
    {
        var service = new FakeStorefrontService();
        var controller = new StorefrontProductsController(service);

        var result = await controller.GetProductDetail(Guid.NewGuid(), " ", CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Null(service.ProductDetailTenantId);
    }

    [Fact]
    public async Task GetProductDetail_WhenProductMissing_ReturnsNotFound()
    {
        var tenantId = Guid.NewGuid();
        var service = new FakeStorefrontService { ProductDetail = null };
        var controller = new StorefrontProductsController(service);

        var result = await controller.GetProductDetail(tenantId, "missing-product", CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(tenantId, service.ProductDetailTenantId);
        Assert.Equal("missing-product", service.ProductDetailSlug);
    }

    [Fact]
    public async Task GetProductDetail_WithValidRequest_ReturnsProductDetail()
    {
        var tenantId = Guid.NewGuid();
        var service = new FakeStorefrontService
        {
            ProductDetail = new StorefrontProductDetailReadModel
            {
                Id = Guid.NewGuid(),
                Name = "Man City Home Jersey 2024/25",
                Slug = "man-city-home-jersey",
                Price = 74.99m,
                IsInStock = true
            }
        };
        var controller = new StorefrontProductsController(service);

        var result = await controller.GetProductDetail(tenantId, "man-city-home-jersey", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(service.ProductDetail, ok.Value);
        Assert.Equal(tenantId, service.ProductDetailTenantId);
        Assert.Equal("man-city-home-jersey", service.ProductDetailSlug);
    }
    [Fact]
    public async Task GetBestSellers_WithMissingTenantHeader_ReturnsBadRequest()
    {
        var service = new FakeStorefrontService();
        var controller = new StorefrontProductsController(service);

        var result = await controller.GetBestSellers(Guid.Empty, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Null(service.BestSellersTenantId);
    }

    [Fact]
    public async Task GetBestSellers_WithValidRequest_ReturnsProducts()
    {
        var tenantId = Guid.NewGuid();
        var service = new FakeStorefrontService
        {
            Products = [new StorefrontProductReadModel { Id = Guid.NewGuid(), Name = "Apple", Price = 12.50m, ImageUrl = "/apple.jpg" }]
        };
        var controller = new StorefrontProductsController(service);

        var result = await controller.GetBestSellers(tenantId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(service.Products, ok.Value);
        Assert.Equal(tenantId, service.BestSellersTenantId);
    }

    [Fact]
    public async Task GetStores_WithMissingTenantHeader_ReturnsBadRequest()
    {
        var service = new FakeStorefrontService();
        var controller = new StorefrontFulfillmentController(service);

        var result = await controller.GetStores(Guid.Empty, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Null(service.StoresTenantId);
    }

    [Fact]
    public async Task GetStores_WithValidRequest_ReturnsStores()
    {
        var tenantId = Guid.NewGuid();
        var service = new FakeStorefrontService
        {
            Stores = [new StorefrontStoreReadModel { Id = Guid.NewGuid(), Name = "Main Store", Address = "Main Road", IsAvailable = true }]
        };
        var controller = new StorefrontFulfillmentController(service);

        var result = await controller.GetStores(tenantId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(service.Stores, ok.Value);
        Assert.Equal(tenantId, service.StoresTenantId);
    }

    private sealed class FakeStorefrontService : IStorefrontService
    {
        public Guid? ResolvedTenantId { get; init; }
        public string? ResolvedSlug { get; private set; }
        public Guid? BannersTenantId { get; private set; }
        public string? BannerType { get; private set; }
        public Guid? RootCategoriesTenantId { get; private set; }
        public Guid? ChildCategoriesTenantId { get; private set; }
        public Guid? ChildCategoriesParentCategoryId { get; private set; }
        public Guid? FeaturedCategoriesTenantId { get; private set; }
        public Guid? ProductsTenantId { get; private set; }
        public Guid? ProductsCategoryId { get; private set; }
        public string? ProductsSort { get; private set; }
        public int? ProductsPage { get; private set; }
        public int? ProductsPageSize { get; private set; }
        public Guid? ProductDetailTenantId { get; private set; }
        public string? ProductDetailSlug { get; private set; }
        public Guid? BestSellersTenantId { get; private set; }
        public Guid? StoresTenantId { get; private set; }
        public IEnumerable<StorefrontBannerReadModel> Banners { get; init; } = [];
        public IEnumerable<StorefrontCategoryListReadModel> RootCategories { get; init; } = [];
        public IEnumerable<StorefrontCategoryListReadModel> ChildCategories { get; init; } = [];
        public IEnumerable<StorefrontCategoryReadModel> Categories { get; init; } = [];
        public StorefrontPagedReadModel<StorefrontProductListReadModel> ProductListingPage { get; init; } = new();
        public StorefrontProductDetailReadModel? ProductDetail { get; init; }
        public IEnumerable<StorefrontProductReadModel> Products { get; init; } = [];
        public IEnumerable<StorefrontStoreReadModel> Stores { get; init; } = [];

        public Task<IEnumerable<StorefrontBannerReadModel>> GetActiveBannersAsync(Guid tenantId, string bannerType, CancellationToken cancellationToken = default)
        {
            BannersTenantId = tenantId;
            BannerType = bannerType;
            return Task.FromResult(Banners);
        }

        public Task<IEnumerable<StorefrontCategoryListReadModel>> GetRootCategoriesAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            RootCategoriesTenantId = tenantId;
            return Task.FromResult(RootCategories);
        }

        public Task<IEnumerable<StorefrontCategoryListReadModel>> GetChildCategoriesAsync(Guid tenantId, Guid parentCategoryId, CancellationToken cancellationToken = default)
        {
            ChildCategoriesTenantId = tenantId;
            ChildCategoriesParentCategoryId = parentCategoryId;
            return Task.FromResult(ChildCategories);
        }

        public Task<IEnumerable<StorefrontCategoryReadModel>> GetFeaturedCategoriesAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            FeaturedCategoriesTenantId = tenantId;
            return Task.FromResult(Categories);
        }

        public Task<StorefrontPagedReadModel<StorefrontProductListReadModel>> GetProductsAsync(Guid tenantId, Guid categoryId, string? sort, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            ProductsTenantId = tenantId;
            ProductsCategoryId = categoryId;
            ProductsSort = sort;
            ProductsPage = page;
            ProductsPageSize = pageSize;
            return Task.FromResult(ProductListingPage);
        }


        public Task<StorefrontProductDetailReadModel?> GetProductDetailAsync(Guid tenantId, string slug, CancellationToken cancellationToken = default)
        {
            ProductDetailTenantId = tenantId;
            ProductDetailSlug = slug;
            return Task.FromResult(ProductDetail);
        }

        public Task<IEnumerable<StorefrontProductReadModel>> GetBestSellersAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            BestSellersTenantId = tenantId;
            return Task.FromResult(Products);
        }

        public Task<IEnumerable<StorefrontStoreReadModel>> GetAvailableStoresAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            StoresTenantId = tenantId;
            return Task.FromResult(Stores);
        }

        public Task<Guid?> ResolveTenantIdAsync(string slug, CancellationToken cancellationToken = default)
        {
            ResolvedSlug = slug;
            return Task.FromResult(ResolvedTenantId);
        }
    }
}