using System.Text.Json;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Repositories;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class PlatformTenantRepositoryWizardTests
{
    [Fact]
    public void BuildOnboardingChangeData_ReturnsValidJsonWithCorrelationIdentifiers()
    {
        var draftId = Guid.NewGuid();
        var operationId = Guid.NewGuid();

        var value = PlatformTenantRepository.BuildOnboardingChangeData(draftId, operationId);

        using var json = JsonDocument.Parse(value);
        Assert.Equal(draftId, json.RootElement.GetProperty("draftId").GetGuid());
        Assert.Equal(operationId, json.RootElement.GetProperty("operationId").GetGuid());
    }
}
