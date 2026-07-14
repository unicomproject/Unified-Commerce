using System.Reflection;
using E_POS.Application.Modules.ECommerce.Storefront.Contracts;
using E_POS.Application.Modules.ECommerce.Storefront.Dtos;
using E_POS.Application.Modules.ECommerce.Storefront.Services;
using E_POS.Domain.Modules.ECommerce.Storefront.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using Xunit;

namespace E_POS.UnitTests.ECommerce.Storefront;

public sealed class StorefrontServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 7, 13, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetActiveBannersAsync_MapsBannersToReadModels()
    {
        var banner = StorefrontBanner.Create(
            TenantId,
            null,
            "HERO",
            "Summer Sale",
            "Fresh deals",
            "/images/hero.jpg",
            "Shop now",
            "/shop",
            1,
            "ACTIVE");
        var repository = new FakeStorefrontRepository { Banners = [banner] };
        var service = new StorefrontService(repository);

        var result = (await service.GetActiveBannersAsync(TenantId, "HERO", CancellationToken.None)).ToList();

        var readModel = Assert.Single(result);
        Assert.Equal(banner.Id, readModel.Id);
        Assert.Equal("HERO", readModel.BannerType);
        Assert.Equal("Summer Sale", readModel.Title);
        Assert.Equal("/images/hero.jpg", readModel.ImageUrl);
        Assert.Equal(TenantId, repository.BannersTenantId);
        Assert.Equal("HERO", repository.BannerType);
    }

    [Fact]
    public async Task GetFeaturedCategoriesAsync_MapsImageUrlAndPlaceholder()
    {
        var categoryWithImage = Category.Create(
            Guid.NewGuid(),
            TenantId,
            Guid.NewGuid(),
            null,
            "FRUIT",
            "Fruit",
            "fruit",
            null,
            "/images/fruit.jpg",
            1,
            CategoryConstants.ActiveStatus,
            null,
            Now);
        var categoryWithoutImage = Category.Create(
            Guid.NewGuid(),
            TenantId,
            Guid.NewGuid(),
            null,
            "BAKERY",
            "Bakery",
            "bakery",
            null,
            null,
            2,
            CategoryConstants.ActiveStatus,
            null,
            Now);
        var repository = new FakeStorefrontRepository { Categories = [categoryWithImage, categoryWithoutImage] };
        var service = new StorefrontService(repository);

        var result = (await service.GetFeaturedCategoriesAsync(TenantId, CancellationToken.None)).ToList();

        Assert.Collection(
            result,
            first =>
            {
                Assert.Equal(categoryWithImage.Id, first.Id);
                Assert.Equal("Fruit", first.Name);
                Assert.Equal("/images/fruit.jpg", first.ImageUrl);
            },
            second =>
            {
                Assert.Equal(categoryWithoutImage.Id, second.Id);
                Assert.Equal("Bakery", second.Name);
                Assert.Equal("https://via.placeholder.com/150", second.ImageUrl);
            });
        Assert.Equal(TenantId, repository.FeaturedCategoriesTenantId);
    }

    [Fact]
    public async Task GetRootCategoriesAsync_ReturnsRepositoryCategories()
    {
        var rootCategories = new[]
        {
            new StorefrontCategoryListReadModel
            {
                Id = Guid.NewGuid(),
                Name = "Clothing",
                Slug = "clothing",
                Description = "Jerseys, T-Shirts, Hoodies & more",
                ImageUrl = "/images/clothing.jpg",
                ItemCount = 124,
                SortOrder = 1
            }
        };
        var repository = new FakeStorefrontRepository { RootCategories = rootCategories };
        var service = new StorefrontService(repository);

        var result = await service.GetRootCategoriesAsync(TenantId, CancellationToken.None);

        Assert.Same(rootCategories, result);
        Assert.Equal(TenantId, repository.RootCategoriesTenantId);
    }

    [Fact]
    public async Task GetChildCategoriesAsync_ReturnsRepositoryCategories()
    {
        var parentCategoryId = Guid.NewGuid();
        var childCategories = new[]
        {
            new StorefrontCategoryListReadModel
            {
                Id = Guid.NewGuid(),
                Name = "Jerseys",
                Slug = "jerseys",
                Description = "Official jerseys",
                ImageUrl = "/images/jerseys.jpg",
                ItemCount = 35,
                SortOrder = 1
            }
        };
        var repository = new FakeStorefrontRepository { ChildCategories = childCategories };
        var service = new StorefrontService(repository);

        var result = await service.GetChildCategoriesAsync(TenantId, parentCategoryId, CancellationToken.None);

        Assert.Same(childCategories, result);
        Assert.Equal(TenantId, repository.ChildCategoriesTenantId);
        Assert.Equal(parentCategoryId, repository.ChildCategoriesParentCategoryId);
    }

    [Fact]
    public async Task GetProductsAsync_ReturnsRepositoryProducts()
    {
        var categoryId = Guid.NewGuid();
        var productPage = new StorefrontPagedReadModel<StorefrontProductListReadModel>
        {
            Items = [new StorefrontProductListReadModel { Id = Guid.NewGuid(), Name = "Man City Home Jersey", Slug = "man-city-home-jersey", Price = 74.99m, IsInStock = true }],
            TotalCount = 1,
            Page = 1,
            PageSize = 20
        };
        var repository = new FakeStorefrontRepository { ProductListingPage = productPage };
        var service = new StorefrontService(repository);

        var result = await service.GetProductsAsync(TenantId, categoryId, "newest", 1, 20, CancellationToken.None);

        Assert.Same(productPage, result);
        Assert.Equal(TenantId, repository.ProductsTenantId);
        Assert.Equal(categoryId, repository.ProductsCategoryId);
        Assert.Equal("newest", repository.ProductsSort);
        Assert.Equal(1, repository.ProductsPage);
        Assert.Equal(20, repository.ProductsPageSize);
    }

    [Fact]
    public async Task GetProductDetailAsync_ReturnsRepositoryProductDetail()
    {
        var productDetail = new StorefrontProductDetailReadModel
        {
            Id = Guid.NewGuid(),
            Name = "Man City Home Jersey 2024/25",
            Slug = "man-city-home-jersey",
            Price = 74.99m,
            IsInStock = true
        };
        var repository = new FakeStorefrontRepository { ProductDetail = productDetail };
        var service = new StorefrontService(repository);

        var result = await service.GetProductDetailAsync(TenantId, "man-city-home-jersey", CancellationToken.None);

        Assert.Same(productDetail, result);
        Assert.Equal(TenantId, repository.ProductDetailTenantId);
        Assert.Equal("man-city-home-jersey", repository.ProductDetailSlug);
    }
    [Fact]
    public async Task GetBestSellersAsync_MapsPriceImageAndRatingWithFallbacks()
    {
        var productWithDetails = Product.Create(
            Guid.NewGuid(),
            TenantId,
            "APPLE",
            "Apple",
            "apple",
            "STANDARD",
            "SIMPLE",
            null,
            null,
            null,
            null,
            null,
            true,
            true,
            ProductConstants.ActiveStatus,
            null,
            Now);
        var rating = ProductRatingSummary.Create(TenantId, productWithDetails.Id);
        Set(rating, "AverageRating", 4.5m);
        Set(rating, "TotalReviews", 12);

        var productWithFallbacks = Product.Create(
            Guid.NewGuid(),
            TenantId,
            "BREAD",
            "Bread",
            "bread",
            "STANDARD",
            "SIMPLE",
            null,
            null,
            null,
            null,
            null,
            true,
            true,
            ProductConstants.ActiveStatus,
            null,
            Now);

        var repository = new FakeStorefrontRepository
        {
            BestSellers =
            [
                (productWithDetails, rating, 9.99m, "/images/apple.jpg"),
                (productWithFallbacks, null, null, null)
            ]
        };
        var service = new StorefrontService(repository);

        var result = (await service.GetBestSellersAsync(TenantId, CancellationToken.None)).ToList();

        Assert.Collection(
            result,
            first =>
            {
                Assert.Equal(productWithDetails.Id, first.Id);
                Assert.Equal("Apple", first.Name);
                Assert.Equal(9.99m, first.Price);
                Assert.Equal("/images/apple.jpg", first.ImageUrl);
                Assert.Equal(4.5m, first.Rating);
                Assert.Equal(12, first.ReviewCount);
            },
            second =>
            {
                Assert.Equal(productWithFallbacks.Id, second.Id);
                Assert.Equal("Bread", second.Name);
                Assert.Equal(0m, second.Price);
                Assert.Equal("https://via.placeholder.com/300", second.ImageUrl);
                Assert.Equal(0m, second.Rating);
                Assert.Equal(0, second.ReviewCount);
            });
        Assert.Equal(TenantId, repository.BestSellersTenantId);
    }

    [Fact]
    public async Task GetAvailableStoresAsync_ReturnsRepositoryStores()
    {
        var stores = new[] { new StorefrontStoreReadModel { Id = Guid.NewGuid(), Name = "Main Store", Address = "Main Road", IsAvailable = true } };
        var repository = new FakeStorefrontRepository { Stores = stores };
        var service = new StorefrontService(repository);

        var result = await service.GetAvailableStoresAsync(TenantId, CancellationToken.None);

        Assert.Same(stores, result);
        Assert.Equal(TenantId, repository.StoresTenantId);
    }

    [Fact]
    public async Task ResolveTenantIdAsync_ReturnsRepositoryTenantId()
    {
        var resolvedTenantId = Guid.NewGuid();
        var repository = new FakeStorefrontRepository { ResolvedTenantId = resolvedTenantId };
        var service = new StorefrontService(repository);

        var result = await service.ResolveTenantIdAsync("demo-store", CancellationToken.None);

        Assert.Equal(resolvedTenantId, result);
        Assert.Equal("demo-store", repository.ResolvedSlug);
    }

    private static void Set<T>(object entity, string propertyName, T value)
    {
        var property = entity.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        property?.SetValue(entity, value);
    }

    private sealed class FakeStorefrontRepository : IStorefrontRepository
    {
        public IEnumerable<StorefrontBanner> Banners { get; init; } = [];
        public IEnumerable<Category> Categories { get; init; } = [];
        public IEnumerable<StorefrontCategoryListReadModel> RootCategories { get; init; } = [];
        public IEnumerable<StorefrontCategoryListReadModel> ChildCategories { get; init; } = [];
        public StorefrontPagedReadModel<StorefrontProductListReadModel> ProductListingPage { get; init; } = new();
        public StorefrontProductDetailReadModel? ProductDetail { get; init; }
        public IEnumerable<(Product Product, ProductRatingSummary? Rating, decimal? SellingPrice, string? PrimaryImageUrl)> BestSellers { get; init; } = [];
        public IEnumerable<StorefrontStoreReadModel> Stores { get; init; } = [];
        public Guid? ResolvedTenantId { get; init; }
        public Guid? BannersTenantId { get; private set; }
        public string? BannerType { get; private set; }
        public Guid? FeaturedCategoriesTenantId { get; private set; }
        public Guid? RootCategoriesTenantId { get; private set; }
        public Guid? ChildCategoriesTenantId { get; private set; }
        public Guid? ChildCategoriesParentCategoryId { get; private set; }
        public Guid? ProductsTenantId { get; private set; }
        public Guid? ProductsCategoryId { get; private set; }
        public string? ProductsSort { get; private set; }
        public int? ProductsPage { get; private set; }
        public int? ProductsPageSize { get; private set; }
        public Guid? ProductDetailTenantId { get; private set; }
        public string? ProductDetailSlug { get; private set; }
        public Guid? BestSellersTenantId { get; private set; }
        public Guid? StoresTenantId { get; private set; }
        public string? ResolvedSlug { get; private set; }

        public Task<IEnumerable<StorefrontBanner>> GetActiveBannersAsync(Guid tenantId, string bannerType, CancellationToken cancellationToken = default)
        {
            BannersTenantId = tenantId;
            BannerType = bannerType;
            return Task.FromResult(Banners);
        }

        public Task<IEnumerable<Category>> GetFeaturedCategoriesAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            FeaturedCategoriesTenantId = tenantId;
            return Task.FromResult(Categories);
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
        }        public Task<IEnumerable<(Product Product, ProductRatingSummary? Rating, decimal? SellingPrice, string? PrimaryImageUrl)>> GetBestSellersAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            BestSellersTenantId = tenantId;
            return Task.FromResult(BestSellers);
        }

        public Task<IEnumerable<StorefrontStoreReadModel>> GetAvailableStoresAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            StoresTenantId = tenantId;
            return Task.FromResult(Stores);
        }

        public Task<Guid?> GetTenantIdBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            ResolvedSlug = slug;
            return Task.FromResult(ResolvedTenantId);
        }
    }
}