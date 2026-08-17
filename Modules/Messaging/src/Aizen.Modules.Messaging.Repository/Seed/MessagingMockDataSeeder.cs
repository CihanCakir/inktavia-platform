namespace Aizen.Modules.Messaging.Repository.Seed;

[DocumentationInfo("Messaging mock data seeder",
    "Dev/test seed hook. As of Phase 2 it seeds NO ServiceRequest-context demo conversations — real provider↔owner " +
    "ServiceRequest chat is populated by ServiceRequestChatBackfiller (one-time) + the live-sync consumer, so demo " +
    "SR conversations (#9001/#9004/#9010) would overlap the real data. Kept as the extension point for any future " +
    "non-ServiceRequest demo seed (e.g. a DirectMessage sample).")]
public sealed class MessagingMockDataSeeder
{
    public MessagingMockDataSeeder()
    {
    }

    public Task SeedAsync(CancellationToken ct = default) => Task.CompletedTask;
}
