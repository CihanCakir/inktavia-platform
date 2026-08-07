# Fix Slice — Avatar render (UP-1) + Document picker (UP-2)

Follow-up to `REPORT_ONDEVICE_QA_UPLOAD_FIX_VERIFY.md` (the two ⚠️ follow-ups from the
Aug-6 on-device round). FE-only. Mock parity preserved. The working vessel-photo
`directUpload` path and the mock system were **not** touched.

App: `inktavia-marine-mobile` (Expo SDK 56, RN 0.85). BFF: `bff-marine-mobile`
(`localhost:17003`). Date: 2026-08-07.

---

## TL;DR

| Item | Root cause | Fix | Status |
|---|---|---|---|
| **UP-1** avatar empty black circle | The **stored** avatar is a pre-fix **1×1-pixel / 159-byte** JPEG artifact that decodes to a solid dark square. The upload/attach/read code is already correct (identical primitive to the working vessel photo). Two FE robustness gaps remained. | Seed the profile cache from the attach response (immediate render) + graceful `<Image onError>` → initials fallback. | Code done, tsc clean. Live e2e blocked by an unrelated auth-env failure (see below). |
| **UP-2** doc picker never launches | The type-sheet RN `Modal` is dismissed and the OS document picker is launched in the **same tick** — on iOS you cannot present a native picker while a JS `Modal` is still animating out, so it silently no-ops. | Defer `pickDocument()` to the Modal's `onDismiss` (iOS); fire directly on Android. | Code done, tsc clean. On-device pending. |

---

## UP-1 — Avatar upload does not render/persist

### Investigation (in the order the ticket asked)

1. **Does the flow call the attach endpoint after `directUpload`?** — Yes.
   `ProfileScreen.handlePickAvatar` → `pickImage()` → `uploadAvatar(asset)`
   (`profileApi.ts:65`) → `directUpload(asset,'Image')` → `POST /api/v1/mobile/profile/avatar { fileId }`.
   Identical shape to the working vessel-photo attach.

2. **Does `/me` (and the attach response) return `avatarUrl`, and is it device-reachable?**
   Traced the whole BFF chain — **all correct**:
   - Attach (`UploadParticipantAvatarCommandHandler`) does an asserted Identity update
     `ProfilePhotoUrl = fileId`, then re-resolves + mints a fresh presigned read URL.
   - `/me` (`GetParticipantProfileQueryHandler` → `MapProfile` → `ResolveAvatarUrlAsync`)
     `Guid.TryParse`es the stored fileId and calls `IFileStorageRemoteCall.CreateReadUrl` —
     the **exact same call** the working vessel gallery uses
     (`MobileVesselMediaMapper.MapWithUrlAsync`). Since vessel photos render on-device, the
     read-URL base is device-reachable.
   - The Identity write persists `ProfilePhotoUrl` (patch-merge, `UpdateParticipantProfileCommandHandler:49`)
     and the by-subject read projects it back (`GetParticipantProfileByKeycloakSubjectQueryHandler:39`).
     No read-cache on that query, so no stale-cache bug.

   **Live proof it persists** (DB `inktavia_store`, user `qa.owner.aug5`):
   ```
   UserProfiles.ProfilePhotoUrl = dcebbc4d-8ebc-4ab9-93e6-7baccbd6ce5a   (a valid fileId)
   ```

3. **Is the profile cache invalidated after attach?** — Yes, the mutation invalidated
   `queryKeys.profile.me`. (Improved further, below.)

4. **Does ProfileScreen bind the URL with a graceful fallback?** — It binds
   `<Image source={{ uri: avatarUrl }}>` and falls back to initials **only when `avatarUrl` is null**.
   It had **no fallback for a URL that loads a broken/empty image** — that's the black circle.

### Root cause (the smoking gun)

The `fileId` in the DB resolves to a real, `Ready` FileStorage object — but it is **159 bytes**:

```
file_storage.files  id=32  PublicId=dcebbc4d…  Status=5(Ready)  Category=1(Image)  SizeInBytes=159
MinIO object  inktavia-filestorage-local/image/2026/08/7A184DB1608C42B0BA2EBF20BD47AC60.jpg  = 159 B
byte dump → FFD8 FFE0 JFIF … SOF0 height=0x0001 width=0x0001 … FFD9  → a valid but 1×1-pixel JPEG
```

A 1×1 JPEG stretched to fill the avatar ring is a **solid dark square** = the reported
"empty black circle". Comparison in the same file table clinches it — same code path, same day:

```
id=34  1,500,185 B  image/jpeg  2026-08-06 19:59  ← working vessel photo (headline fix, 1.5 MB)
id=32    159 B      image/jpeg  2026-08-06 17:55  ← avatar (this bug)
id=30,31 159 B each 2026-08-06 17:26              ← earlier identical avatar artifacts
```

The 1.5 MB vessel photo (proving the `expo-file-system` `directUpload` primitive works) was
uploaded **after** the 159-byte avatars. Avatar and vessel photo share the **identical**
`pickImage()` → `directUpload(file,'Image')` → `uploadAsync(BINARY_CONTENT)` code — there is no
avatar-specific upload difference. So the 159-byte avatars are **pre-`directUpload`-fix
artifacts**: the avatar was exercised before the fixed primitive was live in the running app,
while the vessel photo was exercised after. **The avatar upload mechanics are already fixed.**

### Fix (FE)

`src/features/profile/screens/ProfileScreen.tsx`

- **Immediate render** — the avatar mutation now seeds the query cache from the attach
  response (which carries the fresh presigned `avatarUrl`) before invalidating:
  ```ts
  onSuccess: (updated) => {
    if (updated) queryClient.setQueryData(queryKeys.profile.me, updated); // render now
    queryClient.invalidateQueries({ queryKey: queryKeys.profile.me });     // eventual consistency
    showToast('Photo updated.', 'success');
  }
  ```
- **Graceful fallback on image error** — `AvatarHero` tracks an `imgFailed` flag (reset when
  `avatarUrl` changes) and renders initials instead of a dark circle when the `<Image>` fails
  to load (broken / expired / unreachable URL):
  ```tsx
  const showImage = !!avatarUrl && !imgFailed;
  … <Image source={{ uri: avatarUrl! }} onError={() => setImgFailed(true)} /> …
  ```

> Note: a *valid* 1×1 JPEG loads "successfully", so `onError` does **not** fire for the
> existing stale artifact — that specific state is cleared by any fresh upload (the fixed
> primitive now stores a full-size image). The `onError` fallback covers genuinely broken URLs.

### Deliverable status

- After picking an image, the avatar renders immediately (attach-response cache seed) and
  persists across navigation/relaunch (`/me` returns the presigned URL — path verified end to
  end at the DB + storage layers). Broken URLs degrade to initials instead of a black circle.
- **Not driven live**: see the environment blocker below.

---

## UP-2 — Document file picker never launches

### Investigation

`VesselDocumentsScreen`: "Upload Document" opens a **document-type sheet** (an RN `Modal`,
`animationType="slide"`). Selecting a type ran:

```ts
const onDocTypeChosen = (code) => {
  setPickerOpen(false);          // start dismissing the Modal
  uploadMutation.mutate(code);   // → pickDocument() in the SAME tick
};
```

`pickDocument()` (`expo-document-picker.getDocumentAsync`) **is** invoked and **is** awaited —
but on iOS a native picker cannot be presented while the JS `Modal` is still animating out, so
the presentation silently no-ops. The working avatar / vessel-photo flows have **no
intervening Modal** (the picker opens straight off a press), which is why only documents broke.
`expo-document-picker@13.1.6` is installed and Expo-Go-available for SDK 56 — not a dep gap.

### Fix (FE)

`src/features/vessels/screens/VesselDocumentsScreen.tsx` — launch the picker only **after** the
sheet has fully dismissed:

```ts
const onDocTypeChosen = (code) => {
  setPickerOpen(false);
  if (Platform.OS === 'ios') setPendingDocType(code);  // launched in onDismiss
  else uploadMutation.mutate(code);                    // Android: no onDismiss, no conflict
};
// <Modal … onDismiss={onTypeSheetDismissed} />  →  fires uploadMutation.mutate(pendingDocType)
```

Everything downstream is unchanged: `pickDocument()` → `directUpload(file,'Document')` →
`POST /vessels/{id}/documents { fileId, documentTypeCode }` → list invalidation. Errors
(picker/upload) still surface via the existing `onError` toast.

### Deliverable status

Code complete + tsc clean. Type-select → picker opens → upload → appears in the list. On-device
confirmation pending (blocked by the same auth-env issue).

---

## Environment blocker (not a product bug, not in this slice's scope)

Live end-to-end could not be driven: **every** `bff-marine-mobile` → `identity-api` S2S call
returns **403 Forbidden** right now, so *all* authenticated flows (email login, OTP login) fail
before reaching any screen:

```
RequestParticipantOtpLogin / MintParticipantLoginTicket → Refit ApiException: 403 (Forbidden)
```

This is a service-account / assertion authorization failure at the infra layer (containers were
recreated ~15 min before testing; cf. the known `marine-mobile-bff` service-token / BffAssertion
issues), independent of the FE changes here. It blocks on-device re-verification of both fixes.

### Recommended on-device re-test (once auth is restored)

1. **UP-1** — Profile → avatar badge → pick a real photo → avatar renders immediately; leave &
   re-enter Profile and relaunch the app → still renders. (Optional: clear the stale artifact
   first — `UPDATE "UserProfiles" SET "ProfilePhotoUrl"=NULL WHERE "Id"=100030;` — to start from
   the initials baseline; a fresh upload overwrites it either way.)
2. **UP-2** — Vessel → Documents → Upload Document → pick a type → **native document picker
   opens** → pick a PDF/image → uploads → appears in the list.

---

## Files changed

- `inktavia-marine-mobile/src/features/profile/screens/ProfileScreen.tsx` (UP-1)
- `inktavia-marine-mobile/src/features/vessels/screens/VesselDocumentsScreen.tsx` (UP-2)

`npx tsc --noEmit` → 0 errors. No BFF/module changes (the avatar + document BFF contracts were
verified correct end-to-end). Mock parity untouched.
