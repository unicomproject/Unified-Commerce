using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Idempotency;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Tenant.TenantAuth.Contracts;
using E_POS.Application.Modules.Tenant.TenantAuth.Dtos;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.Inventory.Shared.Contracts;
using E_POS.Application.Modules.Tenant.Inventory.CurrentStock.Contracts.Repositories;
using E_POS.Application.Modules.Tenant.Inventory.CurrentStock.Contracts.Services;
using E_POS.Application.Modules.Tenant.Inventory.Dashboard.Contracts.Repositories;
using E_POS.Application.Modules.Tenant.Inventory.Dashboard.Contracts.Services;
using E_POS.Infrastructure.Modules.Tenant.Inventory.Repositories.CurrentStock;
using E_POS.Infrastructure.Modules.Tenant.Inventory.Repositories.Dashboard;
using E_POS.Infrastructure.Modules.Tenant.Inventory.Services;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.HardwareCash.Contracts;
using E_POS.Application.Modules.Tenant.AccessControl.Contracts;
using E_POS.Infrastructure.Modules.Tenant.AccessControl.Repositories;
using E_POS.Infrastructure.Modules.Tenant.AccessControl.Options;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Tenant.TenantFoundation.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.Payment.Contracts;
using E_POS.Application.Modules.Shared.Media.Contracts;
using E_POS.Application.Modules.Shared.Notification.Contracts.Repositories;
using E_POS.Infrastructure.Modules.Tenant.TenantFoundation.Repositories;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Infrastructure.Common;
using E_POS.Infrastructure.Common.Security;
using E_POS.Infrastructure.Integrations.Google;
using E_POS.Infrastructure.Modules.Tenant.TenantAuth.Options;
using E_POS.Infrastructure.Modules.Tenant.TenantAuth.Repositories;
using E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;
using E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Services;
using E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Repositories;
using E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Services;
using E_POS.Infrastructure.Modules.Tenant.HardwareCash.Repositories;
using E_POS.Infrastructure.Modules.Tenant.HardwareCash.Services;
using E_POS.Infrastructure.Modules.Tenant.POSOperations.Repositories;
using E_POS.Infrastructure.Modules.Tenant.Payment;
using E_POS.Application.Common.Email;
using E_POS.Infrastructure.Integrations.Email;
using E_POS.Infrastructure.Modules.Tenant.POSOperations.Services;
using E_POS.Infrastructure.Modules.Tenant.AccessControl.Services;
using E_POS.Infrastructure.Modules.Tenant.TenantAuth.Services;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Options;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Repositories;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Services;
using E_POS.Infrastructure.Modules.Shared.Integration;
using E_POS.Infrastructure.Modules.Shared.Integration.Services;
using E_POS.Infrastructure.Modules.Shared.Idempotency.Services;
using E_POS.Infrastructure.Modules.Platform.Subscription.Repositories;
using E_POS.Infrastructure.Modules.Platform.Subscription.Services;
using E_POS.Application.Modules.ECommerce.Storefront.Contracts;
using E_POS.Application.Modules.ECommerce.FulfilmentPickup.Contracts;
using E_POS.Application.Modules.Tenant.OnlineStoreSetup.Contracts;
using E_POS.Infrastructure.Modules.ECommerce.Storefront.Repositories;
using E_POS.Infrastructure.Modules.ECommerce.FulfilmentPickup.Repositories;
using E_POS.Infrastructure.Modules.Tenant.OnlineStoreSetup.Services;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using E_POS.Application.Modules.Tenant.PricingTax.Contracts;
using E_POS.Infrastructure.Modules.Tenant.PricingTax.Repositories;
using E_POS.Application.Modules.Tenant.Discount.Contracts;
using E_POS.Infrastructure.Modules.Tenant.Discount.Repositories;
using E_POS.Application.Modules.ECommerce.Customer.Contracts;
using E_POS.Infrastructure.Modules.ECommerce.Customer.Repositories;
using E_POS.Application.Modules.ECommerce.CartCheckout.Contracts;
using E_POS.Infrastructure.Modules.ECommerce.CartCheckout.Repositories;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Interfaces;
using E_POS.Application.Modules.ECommerce.Customer.Contracts.Interfaces;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Services;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;
using E_POS.Infrastructure.Modules.ECommerce.CustomerAuth.Options;
using E_POS.Infrastructure.Modules.ECommerce.CustomerAuth.Repositories;
using E_POS.Infrastructure.Modules.ECommerce.CustomerAuth.Services;
using E_POS.Infrastructure.Modules.ECommerce.Storefront.Services.Autocomplete;
using E_POS.Application.Modules.ECommerce.CustomerWishlist.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;
using E_POS.Infrastructure.Modules.ECommerce.CustomerOrders.Repositories;
using E_POS.Application.Modules.ECommerce.ProductReviews.Contracts;
using E_POS.Infrastructure.Modules.ECommerce.ProductReviews.Repositories;
using E_POS.Infrastructure.Modules.Shared.ReturnExchange.Repositories;
using E_POS.Application.Modules.Tenant.Reports.Contracts;
using E_POS.Infrastructure.Modules.Tenant.Reports.Repositories;
using E_POS.Infrastructure.Modules.Shared.ReturnExchange.Services;
using E_POS.Application.Modules.Shared.Storage.Contracts;
using E_POS.Infrastructure.Modules.Shared.Storage.Services;
using E_POS.Infrastructure.Modules.Shared.Media.Options;
using E_POS.Infrastructure.Modules.Shared.Media.Services;
using E_POS.Infrastructure.Modules.Shared.Notification.Repositories;


namespace E_POS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.Configure<PlatformJwtOptions>(configuration.GetSection(PlatformJwtOptions.SectionName));
        services.Configure<TenantJwtOptions>(configuration.GetSection(TenantJwtOptions.SectionName));
        services.Configure<InvitationDeliverySecretOptions>(configuration.GetSection(InvitationDeliverySecretOptions.SectionName));
        services.Configure<CustomerJwtOptions>(configuration.GetSection(CustomerJwtOptions.SectionName));
        services.Configure<GoogleAuthOptions>(configuration.GetSection(GoogleAuthOptions.SectionName));
        services.AddOptions<AzureBlobStorageOptions>()
            .Bind(configuration.GetSection(AzureBlobStorageOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<AzureBlobStorageOptions>, AzureBlobStorageOptionsValidator>();
        services.Configure<ManualPaymentEvidenceScannerOptions>(configuration.GetSection(ManualPaymentEvidenceScannerOptions.SectionName));
        services.Configure<DevelopmentPlatformAdminSeedOptions>(
            configuration.GetSection(DevelopmentPlatformAdminSeedOptions.SectionName));
        services.Configure<DevelopmentTenantRoleAccessSeedOptions>(
            configuration.GetSection(DevelopmentTenantRoleAccessSeedOptions.SectionName));
        services.Configure<E_POS.Application.Modules.Tenant.OutletTillDevice.Options.TillMonitoringOptions>(
            configuration.GetSection(E_POS.Application.Modules.Tenant.OutletTillDevice.Options.TillMonitoringOptions.SectionName));
        services.AddScoped<IDevelopmentPlatformAdminTestAccountSeeder, DevelopmentPlatformAdminTestAccountSeeder>();
        services.AddScoped<IDevelopmentTenantRoleAccessTestAccountSeeder, DevelopmentTenantRoleAccessTestAccountSeeder>();

        var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();

        services.AddDbContext<EPosDbContext>(options =>
        {
            options.UseNpgsql(dataSource);
            // Hand-written SQL migrations (inspection drafts/media) intentionally skip
            // regenerating the EF model snapshot, matching prior POS return migrations.
            options.ConfigureWarnings(warnings =>
                warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IPasswordHashService, PasswordHashService>();
        services.AddScoped<IJwtTokenFactory, JwtTokenFactory>();
        services.AddScoped<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddScoped<ITokenHashService, TokenHashService>();
        services.AddScoped<IGoogleIdentityVerifier, GoogleIdentityVerifier>();
        services.AddScoped<IAuthSessionValidator, AuthSessionValidator>();
        services.AddScoped<IPlatformPermissionRepository, PlatformPermissionRepository>();
        services.AddScoped<IPlatformAuthRepository, PlatformAuthRepository>();
        services.AddScoped<IPlatformDashboardRepository, PlatformDashboardRepository>();
        services.AddScoped<IPlatformDashboardHealthProbe, PlatformDashboardHealthProbe>();
        services.AddScoped<IPlatformTenantRepository, PlatformTenantRepository>();
        services.AddScoped<IPlatformTenantBootstrapRepository, PlatformTenantBootstrapRepository>();
        services.AddScoped<IPlatformTenantOnboardingRepository, PlatformTenantOnboardingRepository>();
        services.AddSingleton<IValidateOptions<TenantOnboardingOutboxOptions>, TenantOnboardingOutboxOptionsValidator>();
        services.AddSingleton<IValidateOptions<AzureCommunicationEmailOptions>, ProductionAzureCommunicationEmailOptionsValidator>();
        services.AddOptions<TenantOnboardingOutboxOptions>()
            .Bind(configuration.GetSection(TenantOnboardingOutboxOptions.SectionName))
            .ValidateOnStart();
        services.AddHostedService<TenantOnboardingOutboxWorker>();
        services.AddScoped<ITenantAdminInvitationAcceptanceRepository, TenantAdminInvitationAcceptanceRepository>();
        services.AddScoped<IPlatformPermissionCatalogRepository, PlatformPermissionCatalogRepository>();
        services.AddScoped<IPlatformModulesCatalogRepository, PlatformModulesCatalogRepository>();
        services.AddScoped<IPlatformSettingsRepository, PlatformSettingsRepository>();
        services.AddScoped<ISettingDefinitionRepository, SettingDefinitionRepository>();
        services.AddScoped<IPosLoginBrandingRepository, PosLoginBrandingRepository>();
        services.AddScoped<IPosLoginBrandingMediaRepository, PosLoginBrandingMediaRepository>();
        services.AddScoped<IPlatformBillingRepository, PlatformBillingRepository>();
        services.AddScoped<IManualPaymentRepository, ManualPaymentRepository>();
        services.AddScoped<IManualPaymentAccessTokenService, ManualPaymentAccessTokenService>();
        services.AddScoped<IInvitationTokenService, InvitationTokenService>();
        services.AddSingleton<IInvitationDeliverySecretProtector, AesGcmInvitationDeliverySecretProtector>();
        services.AddScoped(provider => new Lazy<IInvitationDeliverySecretProtector>(
            provider.GetRequiredService<IInvitationDeliverySecretProtector>));
        services.AddScoped<ITenantUserInviteDeliverySecretCleanupService, TenantUserInviteDeliverySecretCleanupService>();
        services.AddHostedService<TenantUserInviteDeliverySecretCleanupHostedService>();
        services.AddScoped<IManualPaymentEvidenceStorage, AzureManualPaymentEvidenceStorage>();
        services.AddScoped<IManualPaymentEvidenceScanner, ClamAvManualPaymentEvidenceScanner>();
        services.AddScoped<IPaymentProvider, ManualPaymentProvider>();
        services.AddScoped<IPlatformRoleRepository, PlatformRoleRepository>();
        services.AddScoped<IPlatformUserRepository, PlatformUserRepository>();
        services.AddScoped<IPlatformAuditLogRepository, PlatformAuditLogRepository>();
        services.AddScoped<IPlatformPasswordResetRepository, PlatformPasswordResetRepository>();
        services.AddScoped<IPlatformPasswordResetLinkBuilder, PlatformPasswordResetLinkBuilder>();
        services.AddSingleton<IValidateOptions<AzureCommunicationEmailOptions>, AzureCommunicationEmailOptionsValidator>();
        services.AddOptions<AzureCommunicationEmailOptions>()
            .Bind(configuration.GetSection(AzureCommunicationEmailOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IApplicationEmailSender, AzureCommunicationEmailSender>();
        services.AddScoped<ICustomerPasswordResetLinkBuilder, CustomerPasswordResetLinkBuilder>();
        services.AddScoped(static provider =>
        {
            var configuration = provider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
            var section = configuration.GetSection("CustomerPasswordReset");
            return new CustomerPasswordResetSettings(
                section["PublicStorefrontBaseUrl"] ?? "http://localhost:4200",
                section["ResetPath"] ?? "/reset-password");
        });
        services.AddScoped<IPlatformPasswordResetDeliveryService, AcsPlatformPasswordResetDeliveryService>();
        services.AddScoped(static provider =>
        {
            var configuration = provider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
            var section = configuration.GetSection("PlatformPasswordReset");
            return new PlatformPasswordResetSettings(
                section["PublicAppBaseUrl"] ?? "http://localhost:4200",
                section["ResetPath"] ?? "/reset-password");
        });
        services.AddScoped<IPlatformSubscriptionPlanRepository, PlatformSubscriptionPlanRepository>();
        services.AddScoped<ITenantFeatureEntitlementEvaluator, TenantFeatureEntitlementEvaluator>();
        services.AddScoped<ITenantSubscriptionLimitResolver, TenantSubscriptionLimitResolver>();
        services.AddScoped<ITenantResourceLimitGuard, TenantResourceLimitGuard>();
        services.AddScoped<IIdempotencyService, IdempotencyService>();
        services.AddScoped<ITenantUsageCounterRepository, TenantUsageCounterRepository>();
        services.AddScoped<ITenantAuthRepository, TenantAuthRepository>();
        services.AddScoped<ITenantAdminContextRepository, TenantAdminContextRepository>();
        services.AddScoped<IUnitOfMeasureRepository, UnitOfMeasureRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IBrandRepository, BrandRepository>();
        services.AddScoped<ICollectionRepository, CollectionRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ITenantAdminProductRepository, TenantAdminProductRepository>();
        services.AddScoped<ITenantAdminProductAuditLogger, TenantAdminProductAuditLogger>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<ICurrentStockRepository, CurrentStockRepository>();
        services.AddScoped<IInventoryAuditLogger, InventoryAuditLogger>();
        services.AddScoped<ICatalogMediaRepository, CatalogMediaRepository>();
        services.AddScoped<IPosProductCatalogRepository, PosProductCatalogRepository>();
        services.AddScoped<IPosCustomerRepository, PosCustomerRepository>();
        services.AddScoped<IReturnPolicyTemplateRepository, ReturnPolicyTemplateRepository>();
        services.AddScoped<IReturnPolicyRepository, ReturnPolicyRepository>();
        services.AddScoped<ICodeSequenceRepository, CodeSequenceRepository>();
        services.AddScoped<IOutletRepository, OutletRepository>();
        services.AddScoped<IOutletImageRepository, OutletImageRepository>();
        services.AddScoped<IOutletAuditLogger, OutletAuditLogger>();
        services.AddScoped<ITenantAdminOutletRepository, TenantAdminOutletRepository>();
        services.AddScoped<ITenantAdminTillRepository, TenantAdminTillRepository>();
        services.AddScoped<ITenantAdminHardwareRepository, TenantAdminHardwareRepository>();
        services.AddScoped<ITenantAdminHardwareAuditLogger, TenantAdminHardwareAuditLogger>();
        services.AddScoped<ITenantAdminUserRepository, TenantAdminUserRepository>();
        services.AddScoped<ITenantAdminRoleRepository, TenantAdminRoleRepository>();
        services.AddScoped<ITenantUserStaffCodeService, TenantUserStaffCodeService>();
        services.AddScoped<ITillRepository, TillRepository>();
        services.AddScoped<IPosDeviceRepository, PosDeviceRepository>();
        services.AddScoped<ITillDeviceAssignmentRepository, TillDeviceAssignmentRepository>();
        services.AddScoped<IDeviceContextRepository, DeviceContextRepository>();
        services.AddScoped<IPriceListRepository, PriceListRepository>();
        services.AddScoped<IPriceListItemsRepository, PriceListItemsRepository>();
        services.AddScoped<ITaxSetupRepository, TaxSetupRepository>();
        services.AddScoped<IProductTaxAssignmentRepository, ProductTaxAssignmentRepository>();
        services.AddScoped<ITenantLookupRepository, TenantLookupRepository>();
        services.AddScoped<IPosHomeDashboardRepository, PosHomeDashboardRepository>();
        services.AddScoped<IPosTillSessionRepository, PosTillSessionRepository>();
        services.AddScoped<IPosCheckoutRepository, PosCheckoutRepository>();
        services.AddScoped<IReceiptTemplateResolutionService, ReceiptTemplateResolutionService>();
        services.AddScoped<ICardPaymentGateway, UnavailableCardPaymentGateway>();
        services.AddScoped<IPosSaleLinePricingCalculator, PosSaleLinePricingCalculator>();
        services.AddScoped<IPosReceiptRepository, PosReceiptRepository>();
        services.AddScoped<IPosReturnRepository, PosReturnRepository>();
        services.AddScoped<IMediaObjectStorage, AzureBlobMediaObjectStorage>();
        services.AddScoped<IAzureSasTokenProvider, AzureBlobSasTokenProvider>();
        services.AddScoped<IMediaReadUrlResolver, AzureBlobMediaReadUrlResolver>();
        services.AddScoped<IReturnInspectionMediaStorage, LocalReturnInspectionMediaStorage>();
        services.AddHostedService<ReturnInspectionMediaStagingCleanupService>();
        services.AddHostedService<OutletMediaStagingCleanupService>();
        services.AddHostedService<ProductMediaStagingCleanupService>();
        services.AddScoped<IPosHoldRepository, PosHoldRepository>();
        services.AddScoped<IPosDiscountRepository, PosDiscountRepository>();
        services.AddScoped<IPosHardwareRepository, PosHardwareRepository>();
        services.AddScoped<IPosDrawerRepository, PosDrawerRepository>();
        services.AddScoped<IDiscountPolicyAdminRepository, DiscountPolicyAdminRepository>();
        services.AddScoped<ITenantAdminReportsRepository, TenantAdminReportsRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped(static provider =>
        {
            var options = provider.GetRequiredService<IOptions<PlatformJwtOptions>>().Value;
            return new PlatformJwtSettings(options.Issuer, options.Audience, options.SigningKey, options.AccessTokenMinutes, options.RefreshTokenDays);
        });
        services.AddScoped(static provider =>
        {
            var options = provider.GetRequiredService<IOptions<TenantJwtOptions>>().Value;
            return new TenantJwtSettings(options.Issuer, options.Audience, options.SigningKey, options.AccessTokenMinutes, options.RefreshTokenDays);
        });
        services.AddScoped(static provider =>
        {
            var options = provider.GetRequiredService<IOptions<CustomerJwtOptions>>().Value;
            return new CustomerJwtSettings(
                options.Issuer,
                options.Audience,
                options.SigningKey,
                options.AccessTokenMinutes,
                options.RefreshTokenDays);
        });

        // ECommerce Storefront
        services.AddScoped<IStorefrontBannerRepository, StorefrontBannerRepository>();
        services.AddScoped<IStorefrontCategoryRepository, StorefrontCategoryRepository>();
        services.AddScoped<IStorefrontProductListingRepository, StorefrontProductListingRepository>();
        services.AddScoped<IStorefrontProductDetailRepository, StorefrontProductDetailRepository>();
        services.AddScoped<IStorefrontProductSearchRepository, StorefrontProductSearchRepository>();
        services.AddScoped<IStorefrontProductBestSellerRepository, StorefrontProductBestSellerRepository>();
        services.AddScoped<IStorefrontProductRepository>(provider => new StorefrontProductRepository(
            provider.GetRequiredService<IStorefrontProductListingRepository>(),
            provider.GetRequiredService<IStorefrontProductDetailRepository>(),
            provider.GetRequiredService<IStorefrontProductSearchRepository>(),
            provider.GetRequiredService<IStorefrontProductBestSellerRepository>()));
        services.AddScoped<IStorefrontFulfilmentRepository, StorefrontFulfilmentRepository>();
        services.AddScoped<IStorefrontTenantRepository, StorefrontTenantRepository>();
        services.AddScoped<IStorefrontRepository, StorefrontRepository>();
        services.AddScoped<ITenantAdminOnlineStoreService, TenantAdminOnlineStoreService>();
        services.AddScoped<IStorefrontCartRepository, StorefrontCartRepository>();
        services.AddScoped<IStorefrontCheckoutSessionRepository, StorefrontCheckoutSessionRepository>();
        services.AddScoped<IStorefrontCheckoutConfirmationRepository, StorefrontCheckoutConfirmationRepository>();
        services.AddScoped<IStorefrontCheckoutRepository>(provider => new StorefrontCheckoutRepository(
            provider.GetRequiredService<IStorefrontCheckoutSessionRepository>(),
            provider.GetRequiredService<IStorefrontCheckoutConfirmationRepository>()));
        services.AddSingleton<IStorefrontAutocompleteService, StorefrontAutocompleteService>();
        services.AddHostedService<AutocompleteInitializationHostedService>();
        services.AddScoped<ICustomerRegistrationRepository, CustomerRegistrationRepository>();
        services.AddScoped<ICustomerEmailVerificationRepository, CustomerEmailVerificationRepository>();
        services.AddScoped<ICustomerPasswordResetRepository, CustomerPasswordResetRepository>();
        services.AddScoped<ICustomerLoginRepository, CustomerLoginRepository>();
        services.AddScoped<ICustomerExternalAuthRepository, CustomerExternalAuthRepository>();
        services.AddScoped<ICustomerSessionRepository, CustomerSessionRepository>();
        services.AddScoped<ICustomerProfileRepository, CustomerProfileRepository>();
        services.AddScoped<ICustomerAddressRepository, CustomerAddressRepository>();
        services.AddScoped<ICustomerWishlistRepository, CustomerWishlistRepository>();
        services.AddScoped<ICustomerOrderReadRepository, CustomerOrderReadRepository>();
        services.AddScoped<ICustomerOrderCancelRepository, CustomerOrderCancelRepository>();
        services.AddScoped<ICustomerOrderRepository>(provider => new CustomerOrderRepository(
            provider.GetRequiredService<ICustomerOrderReadRepository>(),
            provider.GetRequiredService<ICustomerOrderCancelRepository>()));
        services.AddScoped<IClickCollectOrderStatusRepository, ClickCollectOrderStatusRepository>();
        services.AddScoped<IPosOnlineOrderDetailRepository, PosOnlineOrderDetailRepository>();
        services.AddScoped<IPosOnlineOrderStartFulfillmentRepository, PosOnlineOrderStartFulfillmentRepository>();
        services.AddScoped<IProductReviewRepository, ProductReviewRepository>();

        return services;
    }
}

