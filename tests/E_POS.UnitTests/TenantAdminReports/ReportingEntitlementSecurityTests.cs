using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using E_POS.Application.Modules.Tenant.Reports.Services;
using E_POS.Domain.Modules.Tenant.Reports.Constants;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using System;

namespace E_POS.UnitTests.TenantAdminReports
{
    public class ReportingEntitlementSecurityTests
    {
        [Fact]
        public async Task EntitlementMatrix_SalesReports_NoEntitlement_FailsClosed()
        {
            var evaluator = new Mock<ITenantFeatureEntitlementEvaluator>();
            evaluator.Setup(e => e.IsEnabledAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(false);

            var tenantId = Guid.NewGuid();
            var result = await ReportFeaturePolicy.IsSectionEnabledAsync("transactions", evaluator.Object, tenantId, DateTimeOffset.UtcNow, CancellationToken.None);
            
            Assert.False(result);
        }

        [Fact]
        public async Task EntitlementMatrix_InventoryReports_NoEntitlement_FailsClosed()
        {
            var evaluator = new Mock<ITenantFeatureEntitlementEvaluator>();
            evaluator.Setup(e => e.IsEnabledAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(false);

            var tenantId = Guid.NewGuid();
            var result = await ReportFeaturePolicy.IsSectionEnabledAsync("current", evaluator.Object, tenantId, DateTimeOffset.UtcNow, CancellationToken.None);
            
            Assert.False(result);
        }

        [Fact]
        public async Task EntitlementMatrix_Export_NoEntitlement_FailsClosed()
        {
            var evaluator = new Mock<ITenantFeatureEntitlementEvaluator>();
            evaluator.Setup(e => e.IsEnabledAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(false);

            var tenantId = Guid.NewGuid();
            var result = await ReportFeaturePolicy.IsExportEnabledAsync(evaluator.Object, tenantId, DateTimeOffset.UtcNow, CancellationToken.None);
            
            Assert.False(result);
        }
    }
}
