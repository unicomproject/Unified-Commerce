using E_POS.Infrastructure.Common.Security;
using E_POS.Infrastructure.Persistence.Seed;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class OneverceAdminPasswordSeedTests
{
    [Fact]
    public void DocumentedOneverceAdminPasswordHash_VerifiesWithPasswordHashService()
    {
        var service = new PasswordHashService();

        Assert.True(service.VerifyPassword(
            OneverceAdminAndTillSeedData.AdminPassword,
            OneverceAdminAndTillSeedData.AdminPasswordHash));
    }
}
