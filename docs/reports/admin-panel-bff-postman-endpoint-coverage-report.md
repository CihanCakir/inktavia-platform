# AdminPanel BFF — Postman Endpoint Coverage Report

**Generated:** 2026-06-11  
**Scope:** Endpoint coverage between catalog, controllers, and generated Postman collection  
**Repository:** Inktavia Marine OS (CihanCakir/inktavia-platform)

---

## Coverage Summary

| Domain | Catalog Endpoints | Controller Endpoints | Postman Requests | Coverage |
|--------|-------------------|---------------------|------------------|----------|
| Auth | 7 | 7 | 7 | ✅ 100% |
| Dashboard | 1 | 1 | 1 | ✅ 100% |
| Identity - General Profiles | 3 | 3 | 3 | ✅ 100% |
| Identity - Organizer Profiles | 5 | 5* | 5 | ✅ 100% |
| Identity - Venue Profiles | 4 | 4* | 4 | ✅ 100% |
| Identity - Participant Profiles | 2 | 2 | 2 | ✅ 100% |
| Files | 5 | 5 | 5 | ✅ 100% |
| Vessels | 9 | 9 | 9 | ✅ 100% |
| Service Requests | 10 | 10 | 10 | ✅ 100% |
| Reference Data | 8 | 8 | 8 | ✅ 100% |
| **Total** | **54** | **54** | **54** | ✅ **100%** |

> *Organizer approve/reject (2) and Venue approve/reject (2) are physically in `AdminIdentityController.cs` but logically belong to Organizer/Venue folders in the catalog. All 4 are correctly included.

---

## Detailed Endpoint Coverage

### Auth (7/7 ✅)

| Method | Path | Postman Name | Status |
|--------|------|--------------|--------|
| POST | /auth/login/username | Login with Username | ✅ |
| POST | /auth/login/phone | Login with Phone | ✅ |
| POST | /auth/login/otp | Login with OTP | ✅ |
| POST | /auth/otp/send | Send OTP | ✅ |
| POST | /auth/otp/check | Check OTP | ✅ |
| POST | /auth/refresh | Refresh Token | ✅ |
| POST | /auth/password/change | Change Password | ✅ |

### Dashboard (1/1 ✅)

| Method | Path | Postman Name | Status |
|--------|------|--------------|--------|
| GET | /dashboard/overview | Dashboard Overview | ✅ |

### Identity - General Profiles (3/3 ✅)

| Method | Path | Postman Name | Status |
|--------|------|--------------|--------|
| GET | /identity/profiles | List Profiles | ✅ |
| GET | /identity/profiles/{profileId:guid} | Profile Detail | ✅ |
| GET | /identity/profiles/{profileId:guid}/with-roles | Profile with Roles | ✅ |

### Identity - Organizer Profiles (5/5 ✅)

| Method | Path | Postman Name | Status |
|--------|------|--------------|--------|
| GET | /identity/organizers/profiles | List Organizer Profiles | ✅ |
| GET | /identity/organizers/profiles/{profileId:guid} | Organizer Profile by ID | ✅ |
| GET | /identity/organizers/profiles/{profileId:guid}/with-user | Organizer Profile with User | ✅ |
| POST | /identity/organizers/{userId:long}/profiles/{profileId:guid}/approve | Approve Organizer Profile | ✅ |
| POST | /identity/organizers/{userId:long}/profiles/{profileId:guid}/reject | Reject Organizer Profile | ✅ |

### Identity - Venue Profiles (4/4 ✅)

| Method | Path | Postman Name | Status |
|--------|------|--------------|--------|
| GET | /identity/venues/profiles | List Venue Profiles | ✅ |
| GET | /identity/venues/profiles/{profileId:guid} | Venue Profile by ID | ✅ |
| POST | /identity/venues/{userId:long}/profiles/{profileId:guid}/approve | Approve Venue Profile | ✅ |
| POST | /identity/venues/{userId:long}/profiles/{profileId:guid}/reject | Reject Venue Profile | ✅ |

### Identity - Participant Profiles (2/2 ✅)

| Method | Path | Postman Name | Status |
|--------|------|--------------|--------|
| GET | /identity/participant/profiles | List Participant Profiles | ✅ |
| GET | /identity/participant/profiles/{profileId:guid} | Participant Profile by ID | ✅ |

### Files (5/5 ✅)

| Method | Path | Postman Name | Status |
|--------|------|--------------|--------|
| GET | /files/{fileId:long} | File Metadata | ✅ |
| POST | /files/{fileId:long}/read-url | Generate File Read URL | ✅ |
| POST | /files/bulk-read-urls | Bulk Generate Read URLs | ✅ |
| DELETE | /files/{fileId:long} | Delete File | ✅ |
| PATCH | /files/{fileId:long}/visibility | Update File Visibility | ✅ |

### Vessels (9/9 ✅)

| Method | Path | Postman Name | Status |
|--------|------|--------------|--------|
| GET | /vessels | List Vessels | ✅ |
| GET | /vessels/form-options | Vessel Form Options | ✅ |
| GET | /vessels/{vesselId:long} | Vessel by ID | ✅ |
| GET | /vessels/{vesselId:long}/detail | Vessel Detail (Documents + Owners) | ✅ |
| PUT | /vessels/{vesselId:long} | Update Vessel | ✅ |
| PATCH | /vessels/{vesselId:long}/archive | Archive Vessel | ✅ |
| PATCH | /vessels/{vesselId:long}/restore | Restore Vessel | ✅ |
| PATCH | /vessels/{vesselId:long}/status | Update Vessel Status | ✅ |
| DELETE | /vessels/{vesselId:long}/documents/{documentId:long} | Remove Vessel Document | ✅ |

### Service Requests (10/10 ✅)

| Method | Path | Postman Name | Status |
|--------|------|--------------|--------|
| GET | /service-requests | List Service Requests | ✅ |
| GET | /service-requests/filter-options | Filter Options | ✅ |
| GET | /service-requests/disputes | List Disputes | ✅ |
| GET | /service-requests/{serviceRequestId:long} | Service Request Detail | ✅ |
| GET | /service-requests/{serviceRequestId:long}/timeline | Service Request Timeline | ✅ |
| PATCH | /service-requests/{serviceRequestId:long}/cancel | Cancel Service Request | ✅ |
| PATCH | /service-requests/{serviceRequestId:long}/completion/approve | Approve Completion | ✅ |
| PATCH | /service-requests/{serviceRequestId:long}/completion/reject | Reject Completion | ✅ |
| PATCH | /service-requests/{serviceRequestId:long}/disputes/{disputeId:long}/status | Change Dispute Status | ✅ |
| PATCH | /service-requests/{serviceRequestId:long}/disputes/{disputeId:long}/resolve | Resolve Dispute | ✅ |

### Reference Data (8/8 ✅)

| Method | Path | Postman Name | Status |
|--------|------|--------------|--------|
| GET | /reference-data/lookup-groups | Lookup Groups | ✅ |
| GET | /reference-data/lookup-tree | Lookup Tree | ✅ |
| GET | /reference-data/lookup/{groupCode}/items | Lookup Items by Group Code | ✅ |
| GET | /reference-data/currencies | Currencies | ✅ |
| GET | /reference-data/locations/countries | Countries | ✅ |
| GET | /reference-data/locations/cities | Cities | ✅ |
| GET | /reference-data/measurement-units | Measurement Units | ✅ |
| GET | /reference-data/system-parameters | System Parameters | ✅ |

---

## Excluded Active Modules

The following active module domains are NOT represented as AdminPanel BFF endpoints because no corresponding BFF controller endpoints were found:

| Module | Status |
|--------|--------|
| ServiceRequest - Create/Update (owner flow) | Excluded — No admin BFF create/update endpoint |
| ServiceRequest - Offer management | Excluded — No admin BFF offer endpoint |
| Vessel - Create (admin flow) | Excluded — No admin BFF vessel create endpoint |

---

## Inactive / Future Modules (Not included)

| Module | Status |
|--------|--------|
| Payment | Inactive — no BFF endpoint exists |
| Profile (future) | Inactive — no BFF endpoint exists |
| Provider | Inactive — no BFF endpoint exists |
| Seller | Inactive — no BFF endpoint exists |

---

## Remaining Gaps

| Gap | Severity |
|-----|----------|
| `ChangePasswordRequest` DTO fields not verified against source | Low — inferred from standard pattern |
| Enum integer values for archive/status requests are placeholder values | Low — check Abstraction enums for real values |
| No `GET /identity/profiles` with free-text search capability documented | Informational |
