using System.Data;
using Aizen.Modules.Messaging.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Messaging.Repository.Services;

/// <summary>
/// Resolves userId → display name from Identity's <c>public."UserProfiles"</c> over the shared inktavia_store DB
/// (same transitional raw-SQL approach as the SR chat backfill — no new Identity remote-call endpoint). Used by the
/// live-sync consumer and the one-off name-fix pass so migrated + live SR conversations show real names in the admin
/// audit. Display name = CompanyName (providers) else "FirstName LastName".
/// </summary>
public sealed class MessagingUserNameResolver
{
    private readonly MessagingDbContext _db;

    public MessagingUserNameResolver(MessagingDbContext db) => _db = db;

    public async Task<IReadOnlyDictionary<long, string>> ResolveAsync(IEnumerable<long> userIds, CancellationToken ct = default)
    {
        var ids = userIds.Where(id => id > 0).Distinct().ToList();
        var result = new Dictionary<long, string>();
        if (ids.Count == 0) return result;

        var conn = _db.Database.GetDbConnection();
        var wasClosed = conn.State != ConnectionState.Open;
        if (wasClosed) await conn.OpenAsync(ct);
        try
        {
            await using var cmd = conn.CreateCommand();
            // ids are longs (no injection surface); inline for a small per-conversation set.
            cmd.CommandText =
                "SELECT \"UserId\", \"FirstName\", \"LastName\", \"CompanyName\" " +
                $"FROM public.\"UserProfiles\" WHERE \"UserId\" IN ({string.Join(",", ids)})";
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                var uid = r.GetInt64(0);
                var first = r.IsDBNull(1) ? "" : r.GetString(1);
                var last = r.IsDBNull(2) ? "" : r.GetString(2);
                var company = r.IsDBNull(3) ? "" : r.GetString(3);
                var name = !string.IsNullOrWhiteSpace(company) ? company.Trim() : $"{first} {last}".Trim();
                if (!string.IsNullOrWhiteSpace(name) && !result.ContainsKey(uid))
                    result[uid] = name;
            }
        }
        finally
        {
            if (wasClosed) await conn.CloseAsync();
        }
        return result;
    }

    public async Task<string?> ResolveOneAsync(long userId, CancellationToken ct = default)
    {
        if (userId <= 0) return null;
        var map = await ResolveAsync(new[] { userId }, ct);
        return map.TryGetValue(userId, out var name) ? name : null;
    }
}
