$body = @{
    productName = "feegvewb"
    shortName = "gmer545"
    categoryId = "4c424a1f-dc4a-4e2b-bbd8-3ab985d7b561" # random guid
    brandId = "4c424a1f-dc4a-4e2b-bbd8-3ab985d7b562" # random guid
    status = $true
    posSellable = $true
    trackInventory = $true
    allowOnlineSale = $true
    shortDescription = "mnvckuyguj,hi.kl"
    wizardAction = "SAVE_AND_CONTINUE"
    currentSetupStep = 1
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5150/api/v1/tenant-admin/products/draft" -Method Post -Body $body -ContentType "application/json" -Headers @{ "Tenant-ID" = "00000000-0000-0000-0000-000000000000" }
