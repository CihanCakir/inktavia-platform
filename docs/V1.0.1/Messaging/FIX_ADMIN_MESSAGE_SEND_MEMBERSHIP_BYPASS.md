# FIX — admin can't post into conversations they don't participate in (internal-note intervention blocked)

> **Repo:** `addesso-project` — **Messaging module only** (`SendMessageCommandHandler`). Completes the Wave-2
> intervention capability: an admin must be able to post (especially **internal notes**) into **any** conversation
> without being a pre-existing participant.

## Root cause
`SendMessageCommandHandler` resolves the sender strictly from conversation membership:
```csharp
var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
var participant = conversation.Participants.FirstOrDefault(p => p.UserId == currentUserId)
    ?? throw new UnauthorizedAccessException("Sender is not a participant of this conversation.");
// … uses participant.DisplayName, participant.Role for the created message
```
An admin observing a user↔provider conversation is **not** a participant, so every admin send throws. It fails even on
seeded conv #9001 because the seed's `Admin` participant has placeholder `UserId=1`, which never equals the real Keycloak
admin's user id. Net: **admins cannot post internal notes on real conversations** — half of "intervene" is dead.
(Flag/moderate are unaffected: `/moderation` is admin-only and checks no membership.)

## Fix — admin-role bypass with a synthesized admin sender
When the caller carries the **Admin** role, skip the membership requirement and synthesize an admin sender identity;
otherwise keep the existing participant rule unchanged.

```csharp
var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
var isAdmin = _info.UserInfoAccessor.UserInfo.Roles?.Contains("Admin") == true; // match how the rest of the module reads roles

var participant = conversation.Participants.FirstOrDefault(p => p.UserId == currentUserId);
if (participant is null && !isAdmin)
    throw new UnauthorizedAccessException("Sender is not a participant of this conversation.");

// Non-participant admin → synthesized sender (NOT persisted as a participant)
var senderDisplayName = participant?.DisplayName
    ?? _info.UserInfoAccessor.UserInfo.DisplayName /* or FullName/UserName */ ?? "Admin";
var senderRole = participant?.Role ?? MessagingParticipantRole.Admin;
```
Then use `currentUserId`, `senderDisplayName`, `senderRole` where the handler currently uses
`participant.DisplayName` / `participant.Role` (message creation + the `MessagingMessageSentMessage` publish).

Rules:
- **Do NOT auto-add the admin as a participant.** The admin isn't a party to the conversation — synthesize the sender on
  the message only (messages store `SenderUserId` / `SenderName` / `SenderRole` independently of participant rows). This
  keeps participant lists + unread counts correct.
- **Non-admin non-participant is still rejected** (unchanged).
- The existing **internal-note gating stays** (`SendMessageCommandHandler` already skips LLM + the participant
  integration-event when `IsInternalNote`) — so an admin internal note remains admin-only. A **non-internal** admin
  message posts visibly as `Admin` (legitimate intervention).
- Use the module's existing role-source (`UserInfo.Roles` / whatever `[Authorize(Roles="Admin")]` reads) for `isAdmin`;
  do not invent a new claim.

## Also (cleanup, optional)
The seed's `Admin` participant on conv #9001 with placeholder `UserId=1` is now unnecessary and misleading — it can be
dropped from `MessagingMockDataSeeder` (the admin-role bypass makes it moot). Optional; not required for the fix.

## Don't-break / QA
- Only `SendMessageCommandHandler` (and optionally the seeder) change. No controller/BFF/FE change. Flag/moderate/list/
  detail untouched.
- Participant-based sends (real owner/provider) behave exactly as before. Backend builds clean.

## Verification (on-screen)
Fresh admin login on `/app/messages`:
1. Open a conversation the admin is **not** a participant of → toggle **INTERNAL NOTE**, send → it posts, appears in the
   **admin** view marked internal, and is **NOT** delivered to the participant (check a provider/user client) — the
   safety-critical W2 check now actually exercisable.
2. A **non-internal** admin message posts and shows as `Admin` to participants (intervention).
3. A non-admin, non-participant still gets rejected. Flag/moderate still work. No regression to normal participant sends.

## Report
`docs/V1.0.1/Messaging/REPORT_FIX_ADMIN_MESSAGE_SEND_MEMBERSHIP_BYPASS.md`: the handler change, the role-source used, the
synthesized-sender decision (not persisted as participant), the seed cleanup if done, and the on-screen internal-note +
participant-non-leak verification.
