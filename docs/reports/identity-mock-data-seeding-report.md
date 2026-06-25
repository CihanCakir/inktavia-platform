# Identity Mock Data Seeding Report

**Date**: 2026-06-14  
**Module**: Identity (`Aizen.Modules.Identity`)

---

## 1. Scope

Creates 11 demo users (2 admins + 6 boat-owner participants + 3 organizer/providers) with matching user profiles in the Identity module for admin panel demo purposes.

---

## 2. Files Created / Modified

| File | Action | Description |
|------|--------|-------------|
| `Modules/Identity/src/Aizen.Modules.Identity.Repository/Context/Seed/MockData/IdentityMockDataSeeder.cs` | Created | Main seeder class |
| `Modules/Identity/src/Aizen.Modules.Identity.Repository/Seed/MockData/MockDataSeedOptions.cs` | Created | Options class for `IOptions<T>` binding |
| `Modules/Identity/src/Aizen.Modules.Identity.Repository/Seed/Json/MockData/admin-demo/identity-users.json` | Created | 11 user definitions |
| `Modules/Identity/src/Aizen.Modules.Identity.Repository/Seed/Json/MockData/admin-demo/identity-profiles.json` | Created | 11 profile definitions |
| `Modules/Identity/src/Aizen.Modules.Identity/Program.cs` | Modified | Calls `AddIdentityMockData()` |
| `Modules/Identity/src/Aizen.Modules.Identity/configuration/appsettings.json` | Modified | Added `MockData` section (disabled) |
| `Modules/Identity/src/Aizen.Modules.Identity/configuration/appsettings.Local.json` | Modified | Added `MockData` (enabled) |
| `Modules/Identity/src/Aizen.Modules.Identity.Repository/DependencyInjection.cs` | Modified | `AddIdentityMockData()` extension, calls seeder |

---

## 3. Architecture Decisions

### 3.1 IdentityDbContext Direct Seeding (Not UserManager)
`UserManager<UserEntity>.CreateAsync()` does not permit stable explicit IDs reliably. The seeder uses `IdentityDbContext` directly with `IPasswordHasher<UserEntity>` to:
1. Hash passwords without UserManager
2. Set explicit `Id` values before insert
3. Bypass UserManager's internal ID assignment

### 3.2 Reflection for Private Setters
`UserProfileEntity` status fields (`ApprovalStatus`, `Status`) have private setters enforced by the domain. The seeder uses reflection (`BindingFlags.NonPublic | BindingFlags.Instance`) to set these approval states for demo data. This is acceptable for dev-only infrastructure code.

### 3.3 Options-Bound Configuration
`MockDataSeedOptions` is bound via `IOptions<MockDataSeedOptions>`:
```json
{
  "MockData": {
    "Enabled": true,
    "RunOnStartup": true,
    "DataSet": "admin-demo",
    "EnvironmentGuard": ["Local", "Development"]
  }
}
```

---

## 4. Demo Users

| ID | Email | Role | Approval | Password |
|----|-------|------|---------|---------|
| 10001 | admin@inktavia.local | Admin (4) | Approved | Admin!123 |
| 10002 | operations.admin@inktavia.local | Admin (4) | Approved | Admin!123 |
| 10003 | ayse.demir@inktavia.local | Participant (1) | Approved | Test!123 |
| 10004 | mehmet.kaya@inktavia.local | Participant (1) | Approved | Test!123 |
| 10005 | deniz.yilmaz@inktavia.local | Participant (1) | Pending | Test!123 |
| 10006 | selin.uzun@inktavia.local | Participant (1) | Rejected | Test!123 |
| 10007 | burak.arslan@inktavia.local | Participant (1) | Approved | Test!123 |
| 10008 | fatma.celik@inktavia.local | Participant (1) | Approved | Test!123 |
| 10011 | marina.ops@inktavia.local | Organizer (3) | Approved | Test!123 |
| 10012 | teknik.servis@inktavia.local | Organizer (3) | Approved | Test!123 |
| 10013 | cargodry.team@inktavia.local | Organizer (3) | Approved | Test!123 |

---

## 5. Idempotency Strategy

```csharp
var exists = await dbContext.Users.AnyAsync(u => u.Id == user.Id, ct);
if (!exists) await dbContext.Users.AddAsync(user, ct);
```

Profile seeding uses same pattern on `UserProfiles` DbSet.

---

## 6. JSON File Structure

### identity-users.json
```json
[
  {
    "id": 10001,
    "email": "admin@inktavia.local",
    "userName": "admin",
    "loginType": 1,
    "emailConfirmed": true
  },
  ...
]
```

### identity-profiles.json
```json
[
  {
    "id": 11001,
    "userId": 10001,
    "firstName": "Super",
    "lastName": "Admin",
    "roleContext": 4,
    "approvalStatus": 1,
    "status": 2
  },
  ...
]
```

---

## 7. Environment Guard

The seeder only runs if:
1. `MockData.Enabled = true`
2. `MockData.RunOnStartup = true`
3. `ASPNETCORE_ENVIRONMENT` is in `MockData.EnvironmentGuard` array (e.g. `["Local", "Development"]`)

Default `appsettings.json` has `Enabled: false` — safe for all non-local environments.

---

## 8. Validation Results

- **Build**: ✅ 0 errors
- **JSON files**: ✅ Located at correct path (`Seed/Json/MockData/admin-demo/`)
- **DI registration**: ✅ `AddIdentityMockData()` registered in `DependencyInjection.cs`
- **Program.cs**: ✅ `AddIdentityMockData()` called before `Run()`

---

## 9. Remaining Gaps / Follow-ups

- Admin user IDs 10001-10002 may conflict with the existing `SeedIdentityBase` seeded admin if that admin was inserted with ID=1. The existing seeder uses `UserManager` which assigns auto-generated IDs — there is no collision risk since auto-generated IDs are from the sequence (which starts at 1 and increments), while mock IDs start at 10001.
- Phone number fields are not populated. Future iteration can add phone mock data.
- `KeycloakSubjectId` is not set. Keycloak integration for mock users can be set up in a separate Keycloak import step.
