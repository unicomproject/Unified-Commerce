using E_POS.Application.Common.Contracts;
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
        var banner = new StorefrontBannerReadModel
        {
            Id = Guid.NewGuid(),
            BannerType = "HERO",
            Title = "Summer Sale",
            Subtitle = "Fresh deals",
            ImageUrl = "/images/hero.jpg",
            ActionText = "Shop now",
            ActionUrl = "/shop",
            SortOrder = 1
        };
        var repository = new FakeStorefrontRepository { Banners = [banner] };
        var service = new StorefrontService(
            new StorefrontBannerService(repository),
            new StorefrontCategoryService(repository),
            new StorefrontProductService(repository),
            new StorefrontFulfillmentService(repository),
            new StorefrontTenantService(repository));

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
    public async Task GetFeaturedCategoriesAsync_MapsImageUrlAndEmptyFallback()
    {
        var categoryWithImage = new StorefrontCategoryReadModel
        {
            Id = Guid.NewGuid(),
            Name = "Fruit",
            ImageUrl = "/images/fruit.jpg"
        };
        var categoryWithoutImage = new StorefrontCategoryReadModel
        {
            Id = Guid.NewGuid(),
            Name = "Bakery",
            ImageUrl = string.Empty
        };
        var repository = new FakeStorefrontRepository { Categories = [categoryWithImage, categoryWithoutImage] };
        var service = new StorefrontService(
            new StorefrontBannerService(repository),
            new StorefrontCategoryService(repository),
            new StorefrontProductService(repository),
            new StorefrontFulfillmentService(repository),
            new StorefrontTenantService(repository));

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
                Assert.Empty(second.ImageUrl);
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
        var service = new StorefrontService(
            new StorefrontBannerService(repository),
            new StorefrontCategoryService(repository),
            new StorefrontProductService(repository),
            new StorefrontFulfillmentService(repository),
            new StorefrontTenantService(repository));

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
        var service = new StorefrontService(
            new StorefrontBannerService(repository),
            new StorefrontCategoryService(repository),
            new StorefrontProductService(repository),
            new StorefrontFulfillmentService(repository),
            new StorefrontTenantService(repository));

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
        var service = new StorefrontService(
            new StorefrontBannerService(repository),
            new StorefrontCategoryService(repository),
            new StorefrontProductService(repository),
            new StorefrontFulfillmentService(repository),
            new StorefrontTenantService(repository));

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
        var service = new StorefrontService(
            new StorefrontBannerService(repository),
            new StorefrontCategoryService(repository),
            new StorefrontProductService(repository),
            new StorefrontFulfillmentService(repository),
            new StorefrontTenantService(repository));

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
                (productWithDetails, rating, 9.99m, "LKR", "/images/apple.jpg"),
                (productWithFallbacks, null, null, "LKR", null)
            ]
        };
        var service = new StorefrontService(
            new StorefrontBannerService(repository),
            new StorefrontCategoryService(repository),
            new StorefrontProductService(repository),
            new StorefrontFulfillmentService(repository),
            new StorefrontTenantService(repository));

        var result = (await service.GetBestSellersAsync(TenantId, CancellationToken.None)).ToList();

        Assert.Collection(
            result,
            first =>
            {
                Assert.Equal(productWithDetails.Id, first.Id);
                Assert.Equal("Apple", first.Name);
                Assert.Equal(9.99m, first.Price);
                Assert.Equal("LKR", first.CurrencyCode);
                Assert.Equal("/images/apple.jpg", first.ImageUrl);
                Assert.Equal(4.5m, first.Rating);
                Assert.Equal(12, first.ReviewCount);
            },
            second =>
            {
                Assert.Equal(productWithFallbacks.Id, second.Id);
                Assert.Equal("Bread", second.Name);
                Assert.Equal(0m, second.Price);
                Assert.Equal("LKR", second.CurrencyCode);
                Assert.Empty(second.ImageUrl);
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
        var service = new StorefrontService(
            new StorefrontBannerService(repository),
            new StorefrontCategoryService(repository),
            new StorefrontProductService(repository),
            new StorefrontFulfillmentService(repository),
            new StorefrontTenantService(repository));

        var result = await service.GetAvailableStoresAsync(TenantId, CancellationToken.None);

        Assert.Same(stores, result);
        Assert.Equal(TenantId, repository.StoresTenantId);
    }

    [Fact]
    public async Task GetCollectionOptionsAsync_AppliesLeadTimeWindowsAndClosedDays()
    {
        var outletId = Guid.NewGuid();
        var repository = new FakeStorefrontRepository
        {
            CollectionConfiguration = new StorefrontCollectionConfigurationReadModel
            {
                OutletId = outletId,
                OutletName = "Main Store",
                Timezone = "UTC",
                PreparationLeadMinutes = 90,
                PickupWindowMinutes = 30,
                BusinessHours =
                [
                    new() { DayOfWeek = 1, OpeningTime = new TimeOnly(8, 0), ClosingTime = new TimeOnly(12, 0) },
                    new() { DayOfWeek = 2, IsClosed = true },
                    new() { DayOfWeek = 3, OpeningTime = new TimeOnly(9, 0), ClosingTime = new TimeOnly(10, 0) }
                ]
            }
        };
        var service = new StorefrontFulfillmentService(repository, new FakeClock(Now));

        var result = await service.GetCollectionOptionsAsync(TenantId, outletId, 3, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var options = Assert.IsType<StorefrontCollectionOptionsReadModel>(result.Value);
        Assert.Equal(Now.AddMinutes(90), options.EarliestCollectionAt);
        Assert.Collection(
            options.Dates,
            monday =>
            {
                Assert.Equal(new DateOnly(2026, 7, 13), monday.Date);
                Assert.Equal(new DateTimeOffset(2026, 7, 13, 9, 30, 0, TimeSpan.Zero), monday.Windows[0].StartAt);
                Assert.Equal(5, monday.Windows.Count);
            },
            wednesday =>
            {
                Assert.Equal(new DateOnly(2026, 7, 15), wednesday.Date);
                Assert.Equal(2, wednesday.Windows.Count);
            });
        Assert.Equal(TenantId, repository.CollectionConfigurationTenantId);
        Assert.Equal(outletId, repository.CollectionConfigurationOutletId);
    }

    [Fact]
    public async Task GetCollectionOptionsAsync_WhenTodayCutoffReached_OmitsToday()
    {
        var outletId = Guid.NewGuid();
        var repository = new FakeStorefrontRepository
        {
            CollectionConfiguration = new StorefrontCollectionConfigurationReadModel
            {
                OutletId = outletId,
                OutletName = "Main Store",
                Timezone = "UTC",
                PreparationLeadMinutes = 0,
                PickupWindowMinutes = 30,
                CutoffTime = new TimeOnly(8, 0),
                BusinessHours =
                [
                    new() { DayOfWeek = 1, OpeningTime = new TimeOnly(8, 0), ClosingTime = new TimeOnly(9, 0) },
                    new() { DayOfWeek = 2, OpeningTime = new TimeOnly(8, 0), ClosingTime = new TimeOnly(9, 0) }
                ]
            }
        };
        var service = new StorefrontFulfillmentService(repository, new FakeClock(Now));

        var result = await service.GetCollectionOptionsAsync(TenantId, outletId, 2, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var date = Assert.Single(result.Value!.Dates);
        Assert.Equal(new DateOnly(2026, 7, 14), date.Date);
    }

    [Fact]
    public async Task GetCollectionOptionsAsync_DateSpecificClosureOverridesWeeklyHours()
    {
        var outletId = Guid.NewGuid();
        var repository = new FakeStorefrontRepository
        {
            CollectionConfiguration = new StorefrontCollectionConfigurationReadModel
            {
                OutletId = outletId,
                OutletName = "Main Store",
                Timezone = "UTC",
                PreparationLeadMinutes = 0,
                PickupWindowMinutes = 30,
                BusinessHours =
                [
                    new() { DayOfWeek = 1, OpeningTime = new TimeOnly(8, 0), ClosingTime = new TimeOnly(10, 0) },
                    new()
                    {
                        DayOfWeek = 1,
                        IsClosed = true,
                        ValidFrom = new DateOnly(2026, 7, 13),
                        ValidUntil = new DateOnly(2026, 7, 13)
                    },
                    new() { DayOfWeek = 2, OpeningTime = new TimeOnly(8, 0), ClosingTime = new TimeOnly(9, 0) }
                ]
            }
        };
        var service = new StorefrontFulfillmentService(repository, new FakeClock(Now));

        var result = await service.GetCollectionOptionsAsync(
            TenantId,
            outletId,
            2,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var date = Assert.Single(result.Value!.Dates);
        Assert.Equal(new DateOnly(2026, 7, 14), date.Date);
    }

    [Fact]
    public async Task GetCollectionOptionsAsync_DstAmbiguousWindows_AreNotOffered()
    {
        var outletId = Guid.NewGuid();
        var repository = new FakeStorefrontRepository
        {
            CollectionConfiguration = new StorefrontCollectionConfigurationReadModel
            {
                OutletId = outletId,
                OutletName = "New York Store",
                Timezone = "America/New_York",
                PreparationLeadMinutes = 0,
                PickupWindowMinutes = 30,
                BusinessHours =
                [
                    new()
                    {
                        DayOfWeek = 0,
                        OpeningTime = new TimeOnly(1, 0),
                        ClosingTime = new TimeOnly(2, 0)
                    }
                ]
            }
        };
        var beforeFallback = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);
        var service = new StorefrontFulfillmentService(repository, new FakeClock(beforeFallback));

        var result = await service.GetCollectionOptionsAsync(
            TenantId,
            outletId,
            2,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Dates);
    }

    [Fact]
    public async Task ResolveTenantAsync_ReturnsRepositoryTenantId()
    {
        var resolvedTenantId = Guid.NewGuid();
        var repository = new FakeStorefrontRepository { ResolvedTenantId = resolvedTenantId };
        var service = new StorefrontService(
            new StorefrontBannerService(repository),
            new StorefrontCategoryService(repository),
            new StorefrontProductService(repository),
            new StorefrontFulfillmentService(repository),
            new StorefrontTenantService(repository));

        var result = await service.ResolveTenantAsync("demo-store", CancellationToken.None);

        Assert.Equal(resolvedTenantId, result.TenantId);
        Assert.Equal("USD", result.BaseCurrencyCode);
        Assert.Equal("demo-store", repository.ResolvedSlug);
    }

    private static void Set<T>(object entity, string propertyName, T value)
    {
        var property = entity.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        property?.SetValue(entity, value);
    }

    private sealed class FakeClock(DateTimeOffset utcNow) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class FakeStorefrontRepository : IStorefrontRepository
    {
        public IEnumerable<StorefrontBannerReadModel> Banners { get; init; } = [];
        public IEnumerable<StorefrontCategoryReadModel> Categories { get; init; } = [];
        public IEnumerable<StorefrontCategoryListReadModel> RootCategories { get; init; } = [];
        public IEnumerable<StorefrontCategoryListReadModel> ChildCategories { get; init; } = [];
        public StorefrontPagedReadModel<StorefrontProductListReadModel> ProductListingPage { get; init; } = new();
        public StorefrontProductDetailReadModel? ProductDetail { get; init; }
        public IEnumerable<(Product Product, ProductRatingSummary? Rating, decimal? SellingPrice, string CurrencyCode, string? PrimaryImageUrl)> BestSellers { get; init; } = [];
        public IEnumerable<StorefrontStoreReadModel> Stores { get; init; } = [];
        public StorefrontCollectionConfigurationReadModel? CollectionConfiguration { get; init; }
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
        public Guid? CollectionConfigurationTenantId { get; private set; }
        public Guid? CollectionConfigurationOutletId { get; private set; }
        public string? ResolvedSlug { get; private set; }

        public Task<IEnumerable<StorefrontBannerReadModel>> GetActiveBannersAsync(Guid tenantId, string bannerType, CancellationToken cancellationToken = default)
        {
            BannersTenantId = tenantId;
            BannerType = bannerType;
            return Task.FromResult(Banners);
        }

        public Task<IEnumerable<StorefrontCategoryReadModel>> GetFeaturedCategoriesAsync(Guid tenantId, CancellationToken cancellationToken = default)
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

        public Task<StorefrontCategoryListReadModel?> GetCategoryBySlugAsync(Guid tenantId, string slug, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<StorefrontCategoryListReadModel?>(null);
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
        }        public Task<IEnumerable<(Product Product, ProductRatingSummary? Rating, decimal? SellingPrice, string CurrencyCode, string? PrimaryImageUrl)>> GetBestSellersAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            BestSellersTenantId = tenantId;
            return Task.FromResult(BestSellers);
        }

        public Task<StorefrontSearchReadModel> SearchAsync(Guid tenantId, StorefrontSearchRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new StorefrontSearchReadModel());
        }

        public Task<IEnumerable<StorefrontStoreReadModel>> GetAvailableStoresAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            StoresTenantId = tenantId;
            return Task.FromResult(Stores);
        }

        public Task<StorefrontCollectionConfigurationReadModel?> GetCollectionConfigurationAsync(
            Guid tenantId,
            Guid outletId,
            DateTimeOffset now,
            CancellationToken cancellationToken = default)
        {
            CollectionConfigurationTenantId = tenantId;
            CollectionConfigurationOutletId = outletId;
            return Task.FromResult(CollectionConfiguration);
        }

        public Task<(Guid? TenantId, string? BaseCurrencyCode)> GetTenantIdBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            ResolvedSlug = slug;
            return Task.FromResult<(Guid?, string?)>((ResolvedTenantId, "USD"));
        }
    }
}
