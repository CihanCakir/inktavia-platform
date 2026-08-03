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

    public MessagingMockDataSeeder(MessagingDbContext db) => _db = db;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await _db.Conversations.AnyAsync(ct))
            return;

        // ── Conversation 1: SR 9001 ─────────────────────────────────
        var conv1 = ConversationEntity.Create(
            MessagingContextType.ServiceRequest, 9001,
            "Hull Cleaning & Inspection — SR #9001");

        await _db.Conversations.AddAsync(conv1, ct);
        await _db.SaveChangesAsync(ct);

        // No Admin participant is seeded: admins aren't parties to a conversation — the admin-role bypass in
        // SendMessageCommandHandler lets them intervene without a participant row.
        var conv1p1 = ConversationParticipantEntity.Create(conv1.Id, 10001, "Julian Vane",  MessagingParticipantRole.Owner);
        var conv1p2 = ConversationParticipantEntity.Create(conv1.Id, 10011, "Aria Voss",    MessagingParticipantRole.Provider);
        await _db.ConversationParticipants.AddRangeAsync([conv1p1, conv1p2], ct);

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
        msg1_3.SetModerationStatus(MessageModerationStatus.PendingReview);

        var msg1_4 = ConversationMessageEntity.Create(conv1.Id, 1, "Admin",
            MessagingParticipantRole.Admin,
            "SR #9001 has been assigned to Aria Voss (Poseidon Marine Services). Expected completion: 3 days.",
            MessageType.SystemNotification);
        msg1_4.SetModerationStatus(MessageModerationStatus.AutoApproved);

        await _db.ConversationMessages.AddRangeAsync([msg1_1, msg1_2, msg1_3, msg1_4], ct);

        // ── Conversation 2: SR 9004 ─────────────────────────────────
        var conv2 = ConversationEntity.Create(
            MessagingContextType.ServiceRequest, 9004,
            "Engine Overhaul & Fuel System — SR #9004");

        await _db.Conversations.AddAsync(conv2, ct);
        await _db.SaveChangesAsync(ct);

        var conv2p1 = ConversationParticipantEntity.Create(conv2.Id, 10002, "Marco Russo",   MessagingParticipantRole.Owner);
        var conv2p2 = ConversationParticipantEntity.Create(conv2.Id, 10012, "Nico Hartmann", MessagingParticipantRole.Provider);
        await _db.ConversationParticipants.AddRangeAsync([conv2p1, conv2p2], ct);

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

        // ── Conversation 3: SR 9010 ─────────────────────────────────
        var conv3 = ConversationEntity.Create(
            MessagingContextType.ServiceRequest, 9010,
            "Navigation Electronics Calibration — SR #9010");

        await _db.Conversations.AddAsync(conv3, ct);
        await _db.SaveChangesAsync(ct);

        var conv3p1 = ConversationParticipantEntity.Create(conv3.Id, 10005, "Elara Kane",   MessagingParticipantRole.Owner);
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
