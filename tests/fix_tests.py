import os
import re

tests_dir = r"c:\POS_PROPJECT\BACKEND\tests"

def replace_in_file(path, replacements):
    with open(path, "r", encoding="utf-8") as f:
        content = f.read()
    
    original = content
    for old, new in replacements:
        content = content.replace(old, new)
        # Also try regex
        if content == original:
            try:
                content = re.sub(old, new, content)
            except:
                pass
            
    if content != original:
        with open(path, "w", encoding="utf-8") as f:
            f.write(content)
        print(f"Updated {path}")

# Fix TenantSecurityServiceTests
replace_in_file(os.path.join(tests_dir, r"E_POS.IntegrationTests\AuthSecurity\TenantSecurityServiceTests.cs"), [
    ("Name = \"Tenant 1\"", "DisplayName = \"Tenant 1\""),
    ("BaseCurrency = \"LKR\"", "BaseCurrencyCode = \"LKR\""),
    ("BillingStatus = \"ACTIVE\",", ""),
    ("BusinessTypeId = Guid.NewGuid(),", ""),
    ("DefaultLocale = \"en-US\",", ""),
    ("OperatingMode = \"STANDARD\",", ""),
    ("PrimaryDomain = \"example.com\",", "")
])

# Fix PlatformTenantRepositoryTests
replace_in_file(os.path.join(tests_dir, r"E_POS.IntegrationTests\PlatformAdministration\PlatformTenantRepositoryTests.cs"), [
    ("Tenant.Create(\n            tenantId,\n            code,\n            name,\n            status,\n            billingStatus,\n            now)", 
     "Tenant.Create(tenantId, code, code.ToLower(), name, status, \"LKR\", \"Asia/Colombo\", null, null, now)"),
    ("Tenant.Create(tenantId, \"TEN-1\", \"Tenant 1\", TenantStatusConstants.Active, TenantBillingStatusConstants.Paid, Now)",
     "Tenant.Create(tenantId, \"TEN-1\", \"ten-1\", \"Tenant 1\", TenantStatusConstants.Active, \"LKR\", \"Asia/Colombo\", null, null, Now)"),
    ("Tenant.Create(tenantId, \"TEN-2\", \"Tenant 2\", TenantStatusConstants.Suspended, TenantBillingStatusConstants.Overdue, Now)",
     "Tenant.Create(tenantId, \"TEN-2\", \"ten-2\", \"Tenant 2\", TenantStatusConstants.Suspended, \"LKR\", \"Asia/Colombo\", null, null, Now)"),
    ("Tenant.Create(tenantId, \"TEN-3\", \"Tenant 3\", TenantStatusConstants.Active, TenantBillingStatusConstants.Paid, Now)",
     "Tenant.Create(tenantId, \"TEN-3\", \"ten-3\", \"Tenant 3\", TenantStatusConstants.Active, \"LKR\", \"Asia/Colombo\", null, null, Now)"),
    ("passwordHash,\n            \"ACTIVE\",", "passwordHash, \"ACTIVE\", \"\",")
])

# Fix PlatformTenantLifecycleRepositoryTests
replace_in_file(os.path.join(tests_dir, r"E_POS.IntegrationTests\PlatformAdministration\PlatformTenantLifecycleRepositoryTests.cs"), [
    ("Tenant.CreateDraft(\n            tenantId,\n            \"TEN-LIFECYCLE\",\n            \"Lifecycle Tenant\",\n            TenantBillingStatusConstants.Pending,\n            \"LKR\",\n            \"Asia/Colombo\",\n            \"en-LK\",\n            \"unified_epos\",\n            \"Retail\",\n            null,\n            Now)",
     "Tenant.Create(tenantId, \"TEN-LIFECYCLE\", \"ten-lifecycle\", \"Lifecycle Tenant\", TenantStatusConstants.Draft, \"LKR\", \"Asia/Colombo\", null, null, Now)"),
    ("Activate(Now)", "Activate(null, Now)"),
    ("Suspend(Now)", "Suspend(null, Now)"),
    ("Tenant.Create(\n            tenantId,\n            \"TEN-ENT\",\n            \"Entitlement Tenant\",\n            TenantStatusConstants.Active,\n            TenantBillingStatusConstants.Paid,\n            Now)",
     "Tenant.Create(tenantId, \"TEN-ENT\", \"ten-ent\", \"Entitlement Tenant\", TenantStatusConstants.Active, \"LKR\", \"Asia/Colombo\", null, null, Now)")
])

# Fix PlatformTenantLifecycleServiceTests
replace_in_file(os.path.join(tests_dir, r"E_POS.UnitTests\PlatformAdministration\PlatformTenantLifecycleServiceTests.cs"), [
    ("Tenant.CreateDraft", "Tenant.Create"),
    ("It.IsAny<string>(),\n                    It.IsAny<string>(),\n                    It.IsAny<string>(),\n                    It.IsAny<string>(),\n                    It.IsAny<string>(),\n                    It.IsAny<string>(),\n                    It.IsAny<string>(),\n                    It.IsAny<string>(),\n                    It.IsAny<string>(),\n                    It.IsAny<Guid?>(),\n                    It.IsAny<DateTimeOffset>()", 
     "It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<DateTimeOffset>()"),
    ("tenant.Name", "tenant.DisplayName"),
    ("Tenant.Create(\n            tenantId,\n            \"TEN-X\",\n            \"Tenant X\",\n            TenantStatusConstants.Active,\n            TenantBillingStatusConstants.Paid,\n            _now)",
     "Tenant.Create(tenantId, \"TEN-X\", \"ten-x\", \"Tenant X\", TenantStatusConstants.Active, \"LKR\", \"Asia/Colombo\", null, null, _now)"),
    ("Activate(_now)", "Activate(null, _now)"),
    ("Suspend(_now)", "Suspend(null, _now)"),
    ("Tenant.Create(\n            tenantId,\n            \"TEN-SUB\",\n            \"Tenant Sub\",\n            TenantStatusConstants.Active,\n            TenantBillingStatusConstants.Paid,\n            _now)",
     "Tenant.Create(tenantId, \"TEN-SUB\", \"ten-sub\", \"Tenant Sub\", TenantStatusConstants.Active, \"LKR\", \"Asia/Colombo\", null, null, _now)")
])

# Fix PlatformTenantEntitlementOptionsServiceTests
replace_in_file(os.path.join(tests_dir, r"E_POS.UnitTests\PlatformAdministration\PlatformTenantEntitlementOptionsServiceTests.cs"), [
    ("Tenant.Create(\n            tenantId,\n            \"TEN-OPTS\",\n            \"Tenant Options\",\n            TenantStatusConstants.Active,\n            TenantBillingStatusConstants.Paid,\n            _now)",
     "Tenant.Create(tenantId, \"TEN-OPTS\", \"ten-opts\", \"Tenant Options\", TenantStatusConstants.Active, \"LKR\", \"Asia/Colombo\", null, null, _now)")
])
