# Unit Test Progress — Identity Module

## Summary

| Project | Test Files | Tests | Status |
|---------|-----------|-------|--------|
| `Aizen.Modules.Identity.Domain.UnitTests` | 3 | 44 | ✅ All Pass |
| `Aizen.Modules.Identity.Application.UnitTests` | 3 | 23 | ✅ All Pass |
| `Aizen.Modules.Identity.Api.UnitTests` | 2 | 2 | ✅ All Pass |
| **Total** | **8** | **69** | ✅ **69/69 Passed** |

---

## Projects Created

```
Modules/Identity/tests/
├── Aizen.Modules.Identity.Domain.UnitTests/
│   └── Entities/
│       ├── UserProfileEntityTests.cs       (19 tests)
│       ├── UserValidationEntityTests.cs    (13 tests)
│       └── AgreementEntityTests.cs         (12 tests)
├── Aizen.Modules.Identity.Application.UnitTests/
│   └── Validators/
│       ├── CheckOtpCommandValidatorTests.cs         (9 tests)
│       ├── LoginWithPhoneNumberValidatorTests.cs    (6 tests)
│       └── RegisterOrganizerCommandValidatorTests.cs (8 tests)
└── Aizen.Modules.Identity.Api.UnitTests/
    ├── Fakes/
    │   └── FakeCQRSProcessor.cs
    └── Controllers/
        └── FakeCQRSProcessorTests.cs               (2 tests)
```

---

## Domain Tests (`Aizen.Modules.Identity.Domain.UnitTests`)

### `UserProfileEntityTests` — 19 tests
- `Create` factory: validates all properties are set, default optionals are null
- `Approve`: pending→approved, idempotent, throws when already rejected
- `Reject`: pending→rejected, idempotent, throws when already approved, throws on empty/whitespace reason
- `ChangeName`, `UpdateBio`, `UpdateProfilePhoto`, `UpdateGender`, `UpdateBirthDate`, `UpdateTaxpayerType`

### `UserValidationEntityTests` — 13 tests
- `Create`: all fields set correctly, initial state = 1, optional `RelationCode` = null
- `MarkAsUsed`: state → 2
- `Expire`: state → 3
- `IsExpired`: future → false, past → true
- `IsValid`: correct code+guid → true; wrong code/guid/expired/used/expired state → false

### `AgreementEntityTests` — 12 tests
- Constructor: sets name, version, type, optional flag, timestamps
- `UpdateMeta`, `BumpVersionIfHigher`, `IncrementVersion`, `SetOptional`, `Rename`

---

## Application Tests (`Aizen.Modules.Identity.Application.UnitTests`)

### `CheckOtpCommandValidatorTests` — 9 tests
- Valid command passes
- PhoneNumber: null, empty, too short (< 10), too long (> 10) fail
- Otp: < 100001 or > 999998 fail
- ValidationGuid: null, empty fail

### `LoginWithPhoneNumberValidatorTests` — 6 tests
- Valid command passes
- PhoneNumber: empty, not starting with `+90`, too short fail
- Password: empty fails
- DeviceId: empty fails

### `RegisterOrganizerCommandValidatorTests` — 8 tests
- Valid command passes
- Email: empty, invalid format fail
- Password: too short (< 6) fails
- OwnerFirstName / OwnerLastName: empty fail
- KvkkAccepted: false fails
- DeviceId: > 200 chars fails

---

## API Tests (`Aizen.Modules.Identity.Api.UnitTests`)

### `FakeCQRSProcessor`
- Implements `IAizenCQRSProcessor` for use in controller tests
- Tracks all dispatched commands via `ProcessedCommands`
- Supports `SetupResult<TResult>` for mock return values

### `FakeCQRSProcessorTests` — 2 tests
- Verifies initial state is empty
- Verifies `SetupResult` stores without error

---

## Dependencies Added
- `FluentAssertions 8.10.0` — all three test projects

## Pending / Future Work
- Handler tests (requires mocking `IUserRepository`, `IAuthorizationService`, etc.)
- Controller integration tests (requires `WebApplicationFactory`)
- Query handler tests (none exist yet in source)
- ApproveOrganizerProfile / RejectOrganizerProfile validator tests
- Venue & Participant command validator tests
