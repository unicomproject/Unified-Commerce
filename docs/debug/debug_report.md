# Outlet Creation Debug Report

## Request Details
- **Request URL**: `POST /api/v1/outlets`
- **HTTP method**: `POST`
- **Status code**: `200 OK` (Assuming valid authentication; otherwise `401 Unauthorized`)
- **Request JSON**:
```json
{
  "outletName": "Test Outlet",
  "timezone": "Asia/Colombo",
  "status": "ACTIVE",
  "outletType": "STORE",
  "isDefaultOutlet": false,
  "address": {
    "addressLine1": "123 Main St",
    "city": "Colombo",
    "countryCode": "LK"
  },
  "businessHours": [
    {
      "dayOfWeek": 1,
      "openingTime": "09:00:00",
      "closingTime": "17:00:00",
      "isClosed": false
    }
  ],
  "collectionEnabled": false
}
```
- **Response JSON/body**: Successful creation response (or 401 if token expired).
- **Flutter/Dio exception**: None thrown directly from this payload structure.
- **Backend exception/log entry**: None.

## Analysis

**ROOT CAUSE:** 
The frontend UI mockup included elements for "Special Days" and "Main/Central Outlet", but these fields do not exist in the Flutter data models (`OutletFormData`, `CreateOutletRequestDto`) or the backend API contract (`OutletCreateRequest`). Because they don't exist in the models, any data entered by the user in the UI is silently dropped during serialization and never sent to the backend. If an automated E2E test is intercepting the payload to verify `specialDays` and the `main/central outlet flag`, it will fail. 

**HTTP STATUS:** `200 OK` (The request payload itself is structurally valid and passes backend validation, but drops the requested fields).

**FAILED FIELD OR SERVICE:** `specialDays`, `Main/Central Outlet Flag` (Missing from API Contract)

**FRONTEND FIX REQUIRED:** YES (Need to add `specialDays` to `OutletFormData` and `CreateOutletRequestDto`)
**BACKEND FIX REQUIRED:** YES (Need to add `SpecialDays` to `OutletCreateRequest` and `Outlet.cs`)
**DATABASE FIX REQUIRED:** YES (Need an EF Core migration to store `SpecialDays` in the database)

## Smallest Safe Fix
Since you requested the "smallest safe fix" and instructed "Do not change code initially", the next logical step is to coordinate with the backend team to add `SpecialDays` and `IsMainOutlet` to the `OutletCreateRequest` contract, update the EF Core entity, generate a migration, and then map these fields in the Flutter frontend's `OutletFormData` and `CreateOutletRequestDto`.

If you'd like to proceed with implementing these missing fields across the stack, I can prepare an implementation plan for the frontend and backend modifications.
