# PROMPT E — Messaging Module: Seed Data
# `Aizen.Modules.Messaging.Repository` — Seed

## Context

Seed real conversations linking to existing ServiceRequest test data (SR IDs 9001, 9004, 9010). Seeds only run when the table is empty — idempotent. Seed class implements the `SeedMessagingAsync` hook already wired in `DependencyInjection.cs`.

**Prerequisite:** PROMPT_C migration must have run successfully and tables exist in `messaging` schema.

---

## Step 1 — Seed Data Class (`Seed/MessagingMockDataSeeder.cs`)

```csharp
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Domain.Entities.Conversation;
using Aizen.Modules.Messaging.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Messaging.Repository.Seed;

[DocumentationInfo("Messaging mock data seeder",
    "Seeds conversation and message test data for development and testing environments only.")]
public sealed class MessagingMockDataSeeder
{
    private readonly MessagingDbContext _db;

    public MessagingMockDataSeeder(MessagingDbContext db)
    {
        _db = db;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        // Idempotency check — skip if any conversations exist
        if (await _db.Conversations.AnyAsync(ct))
            return;

        var now = DateTimeOffset.UtcNow;

        // ── Conversation 1: SR 9001 — Hull Cleaning & Inspection ─────
        var conv1 = ConversationEntity.Create(
            MessagingContextType.ServiceRequest, 9001,
            "Hull Cleaning & Inspection — SR #9001");

        var conv1p1 = ConversationParticipantEntity.Create(0, 10001, "Julian Vane", MessagingParticipantRole.Owner);
        var conv1p2 = ConversationParticipantEntity.Create(0, 10011, "Aria Voss", MessagingParticipantRole.Provider);
        var conv1p3 = ConversationParticipantEntity.Create(0, 1, "Admin", MessagingParticipantRole.Admin);

        await _db.Conversations.AddAsync(conv1, ct);
        await _db.SaveChangesAsync(ct);

        conv1p1 = ConversationParticipantEntity.Create(conv1.Id, 10001, "Julian Vane", MessagingParticipantRole.Owner);
        conv1p2 = ConversationParticipantEntity.Create(conv1.Id, 10011, "Aria Voss", MessagingParticipantRole.Provider);
        conv1p3 = ConversationParticipantEntity.Create(conv1.Id, 1, "Admin", MessagingParticipantRole.Admin);

        await _db.ConversationParticipants.AddRangeAsync([conv1p1, conv1p2, conv1p3], ct);

        var msg1_1 = ConversationMessageEntity.Create(conv1.Id, 10001, "Julian Vane",
            MessagingParticipantRole.Owner,
            "Hello, I need a thorough hull inspection before the Mediterranean season. Please pay special attention to the port-side thruster.");
        msg1_1.SetModerationStatus(MessageModerationStatus.Allowed);

        var msg1_2 = ConversationMessageEntity.Create(conv1.Id, 10011, "Aria Voss",
            MessagingParticipantRole.Provider,
            "Understood, Julian. We'll schedule a full sonic scan of the hull in addition to the standard cleaning. When is the vessel available?");
        msg1_2.SetModerationStatus(MessageModerationStatus.Allowed);

        var msg1_3 = ConversationMessageEntity.Create(conv1.Id, 10001, "Julian Vane",
            MessagingParticipantRole.Owner,
            "She'll be at berth 14-C from Monday morning. Access code for the marina gate is 4812.");
        msg1_3.SetModerationStatus(MessageModerationStatus.PendingReview); // Contains numeric code — triggers review

        var msg1_4 = ConversationMessageEntity.Create(conv1.Id, 1, "Admin",
            MessagingParticipantRole.Admin,
            "SR #9001 has been assigned to Aria Voss (Poseidon Marine Services). Expected completion: 3 days.",
            MessageType.SystemNotification);
        msg1_4.SetModerationStatus(MessageModerationStatus.AutoApproved);

        await _db.ConversationMessages.AddRangeAsync([msg1_1, msg1_2, msg1_3, msg1_4], ct);

        // ── Conversation 2: SR 9004 — Engine Overhaul ────────────────
        var conv2 = ConversationEntity.Create(
            MessagingContextType.ServiceRequest, 9004,
            "Engine Overhaul & Fuel System — SR #9004");

        await _db.Conversations.AddAsync(conv2, ct);
        await _db.SaveChangesAsync(ct);

        var conv2p1 = ConversationParticipantEntity.Create(conv2.Id, 10002, "Marco Russo", MessagingParticipantRole.Owner);
        var conv2p2 = ConversationParticipantEntity.Create(conv2.Id, 10012, "Nico Hartmann", MessagingParticipantRole.Provider);
        var conv2p3 = ConversationParticipantEntity.Create(conv2.Id, 1, "Admin", MessagingParticipantRole.Admin);

        await _db.ConversationParticipants.AddRangeAsync([conv2p1, conv2p2, conv2p3], ct);

        var msg2_1 = ConversationMessageEntity.Create(conv2.Id, 10012, "Nico Hartmann",
            MessagingParticipantRole.Provider,
            "Good morning, Mr. Russo. I have completed the initial diagnostic. The secondary fuel injectors need replacement — estimated part lead time is 5 business days.");
        msg2_1.SetModerationStatus(MessageModerationStatus.Allowed);

        var msg2_2 = ConversationMessageEntity.Create(conv2.Id, 10002, "Marco Russo",
            MessagingParticipantRole.Owner,
            "Can we use OEM parts only? No aftermarket.");
        msg2_2.SetModerationStatus(MessageModerationStatus.Allowed);

        var msg2_3 = ConversationMessageEntity.Create(conv2.Id, 10012, "Nico Hartmann",
            MessagingParticipantRole.Provider,
            "Of course. I'll source directly from Volvo Penta. I'll update the quote once confirmed.");
        msg2_3.SetModerationStatus(MessageModerationStatus.Allowed);

        var msg2_internal = ConversationMessageEntity.Create(conv2.Id, 1, "Admin",
            MessagingParticipantRole.Admin,
            "INTERNAL: Parts sourcing approved via procurement channel. Budget ceiling €3,200.",
            MessageType.InternalNote, isInternalNote: true);
        msg2_internal.SetModerationStatus(MessageModerationStatus.AutoApproved);

        await _db.ConversationMessages.AddRangeAsync([msg2_1, msg2_2, msg2_3, msg2_internal], ct);

        // ── Conversation 3: SR 9010 — Navigation Electronics ─────────
        var conv3 = ConversationEntity.Create(
            MessagingContextType.ServiceRequest, 9010,
            "Navigation Electronics Calibration — SR #9010");

        await _db.Conversations.AddAsync(conv3, ct);
        await _db.SaveChangesAsync(ct);

        var conv3p1 = ConversationParticipantEntity.Create(conv3.Id, 10005, "Elara Kane", MessagingParticipantRole.Owner);
        var conv3p2 = ConversationParticipantEntity.Create(conv3.Id, 10015, "Daria Solano", MessagingParticipantRole.Provider);

        await _db.ConversationParticipants.AddRangeAsync([conv3p1, conv3p2], ct);

        var msg3_1 = ConversationMessageEntity.Create(conv3.Id, 10005, "Elara Kane",
            MessagingParticipantRole.Owner,
            "The Furuno GP-1870F chartplotter is showing a consistent 0.3nm offset from actual position. It started after the firmware update last month.");
        msg3_1.SetModerationStatus(MessageModerationStatus.Allowed);

        var msg3_2 = ConversationMessageEntity.Create(conv3.Id, 10015, "Daria Solano",
            MessagingParticipantRole.Provider,
            "This is a known issue with Furuno firmware 3.42. I have the patch and calibration tools on hand. Can you share the vessel's current position coordinates so I can pre-load the offset correction?");
        msg3_2.SetModerationStatus(MessageModerationStatus.Allowed);

        var msg3_3 = ConversationMessageEntity.Create(conv3.Id, 10005, "Elara Kane",
            MessagingParticipantRole.Owner,
            "She's at 43°41′24″N, 7°15′58″E — Antibes Port Vauban, berth J12.");
        msg3_3.SetModerationStatus(MessageModerationStatus.Allowed);

        var msg3_4 = ConversationMessageEntity.Create(conv3.Id, 1, "Admin",
            MessagingParticipantRole.Admin,
            "SR #9010 status changed to InProgress.",
            MessageType.StatusChange);
        msg3_4.SetModerationStatus(MessageModerationStatus.AutoApproved);

        await _db.ConversationMessages.AddRangeAsync([msg3_1, msg3_2, msg3_3, msg3_4], ct);

        await _db.SaveChangesAsync(ct);
    }
}
```

---

## Step 2 — Register Seeder in DI

**Update `DependencyInjection.cs`** — add seeder registration to `AddMessagingRepository()`:

```csharp
public static IServiceCollection AddMessagingRepository(this IServiceCollection services)
{
    services.AddScoped<IConversationRepository, ConversationRepository>();
    services.AddScoped<IConversationMessageRepository, ConversationMessageRepository>();
    services.AddScoped<IMessageContentPolicy, MessageContentPolicyService>();

    // Seed
    services.AddScoped<MessagingMockDataSeeder>();   // ← ADD THIS

    return services;
}
```

**Update `SeedMessagingAsync`** in the same file:

```csharp
public static async Task SeedMessagingAsync(this IHost host, CancellationToken ct = default)
{
    using var scope = host.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<MessagingDbContext>();

    var pending = await db.Database.GetPendingMigrationsAsync(ct);
    if (pending.Any())
        await db.Database.MigrateAsync(ct);

    // Seed only in Development
    var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
    if (env.IsDevelopment())
    {
        var seeder = scope.ServiceProvider.GetRequiredService<MessagingMockDataSeeder>();
        await seeder.SeedAsync(ct);
    }
}
```

---

## Step 3 — SQL Verification Queries

Run after startup to confirm seed:

```sql
-- Count conversations
SELECT COUNT(*) as conversation_count FROM messaging.conversations;
-- Expected: 3

-- List conversations with context binding
SELECT id, context_type, context_id, title, status, last_message_preview
FROM messaging.conversations
ORDER BY id;

-- Count messages per conversation
SELECT c.title, COUNT(m.id) as message_count,
       SUM(CASE WHEN m.is_internal_note THEN 1 ELSE 0 END) as internal_notes
FROM messaging.conversations c
LEFT JOIN messaging.conversation_messages m ON m.conversation_id = c.id
GROUP BY c.id, c.title
ORDER BY c.id;
-- Expected: conv1=4 msgs, conv2=4 msgs (1 internal), conv3=4 msgs

-- Moderation queue — pending review
SELECT id, conversation_id, sender_name, left(content, 50), moderation_status
FROM messaging.conversation_messages
WHERE moderation_status IN (2, 3)  -- PendingReview=2, Flagged=3
ORDER BY sent_at;
-- Expected: 1 message from Julian Vane (berth access code)

-- Participants per conversation
SELECT c.title, p.display_name, p.role
FROM messaging.conversations c
JOIN messaging.conversation_participants p ON p.conversation_id = c.id
ORDER BY c.id, p.role;

-- Verify UNIQUE index works (should fail with conflict):
-- INSERT INTO messaging.conversations (context_type, context_id, ...)
-- VALUES (1, 9001, ...) -- same as SR 9001 — must throw duplicate key error
```

---

## Quality Gates

- [ ] `MessagingMockDataSeeder.SeedAsync` is idempotent — running twice produces no duplicates
- [ ] Seed only runs in Development environment
- [ ] All 3 conversations created with correct `ContextType=1` (ServiceRequest)
- [ ] Conversation 2 has 1 `InternalNote` message — admin panels must filter this for non-admin callers
- [ ] Conversation 1 has 1 message with `ModerationStatus=PendingReview` — visible in moderation queue
- [ ] SQL verification: `conversation_count = 3`, `message_count >= 4` per conversation
- [ ] `IWebHostEnvironment` is available in the seeder scope (registered via `AizenApplicationBuilder`)
