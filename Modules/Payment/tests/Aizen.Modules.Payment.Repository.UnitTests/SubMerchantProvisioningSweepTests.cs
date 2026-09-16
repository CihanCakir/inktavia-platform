using System.Reflection;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.PaymentProfile;
using Aizen.Modules.Payment.Repository.Persistence;
using Aizen.Modules.Payment.Repository.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Payment.Repository.UnitTests;

/// <summary>
/// Selection logic behind the hourly retry sweep (<see cref="ProviderPaymentProfileRepository.GetProvisioningRetryDueIdsAsync"/>):
/// only DataSubmitted profiles under the attempt cap whose last attempt is null or older than the retry window, ordered
/// nulls-first (never-attempted) then oldest. Pins the four exclusion reasons + the ordering the sweep relies on.
/// </summary>
public sealed class SubMerchantProvisioningSweepTests
{
    private static PaymentDbContext NewDb()
        => new(new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase($"submerchant-sweep-{Guid.NewGuid():N}").Options);

    private static readonly PropertyInfo LastAttempt = typeof(ProviderPaymentProfileEntity).GetProperty(nameof(ProviderPaymentProfileEntity.LastAttemptAtUtc))!;
    private static readonly PropertyInfo Attempts    = typeof(ProviderPaymentProfileEntity).GetProperty(nameof(ProviderPaymentProfileEntity.AttemptCount))!;

    // Builds a DataSubmitted profile with a controlled attempt count + timestamp (private setters set via reflection so the
    // window/cap edges can be pinned deterministically — RecordProvisioningFailure can only stamp "now").
    private static ProviderPaymentProfileEntity DataSubmitted(long id, DateTime? lastAttempt, int attempts)
    {
        var p = ProviderPaymentProfileEntity.Create(id, "iyzico", "P" + id, "1234567890");
        p.UpdateProfileAndResetVerification("enc", "7890", "P" + id, "1234567890"); // → DataSubmitted
        LastAttempt.SetValue(p, lastAttempt);
        Attempts.SetValue(p, attempts);
        return p;
    }

    [Fact]
    public async Task Selects_only_under_cap_DataSubmitted_profiles_outside_the_retry_window_nulls_first()
    {
        var now = DateTime.UtcNow;
        var retryBefore = now.AddMinutes(-60);
        const int maxAttempts = 10;

        await using var db = NewDb();
        db.PaymentProfiles.Add(DataSubmitted(101, lastAttempt: null,                      attempts: 0)); // never attempted → SELECTED
        db.PaymentProfiles.Add(DataSubmitted(102, lastAttempt: retryBefore.AddMinutes(-5), attempts: 3)); // window elapsed → SELECTED
        db.PaymentProfiles.Add(DataSubmitted(103, lastAttempt: retryBefore.AddMinutes(+5), attempts: 1)); // too recent → excluded
        db.PaymentProfiles.Add(DataSubmitted(104, lastAttempt: null,                      attempts: maxAttempts)); // capped → excluded

        var keyed = DataSubmitted(105, lastAttempt: null, attempts: 0);
        keyed.MarkSubMerchantCreated("sm-key", "acc"); // → SubMerchantCreated, no longer provisionable → excluded
        db.PaymentProfiles.Add(keyed);
        await db.SaveChangesAsync();

        var repo = new ProviderPaymentProfileRepository(db);
        var due = await repo.GetProvisioningRetryDueIdsAsync(retryBefore, maxAttempts, take: 50);

        due.Should().Equal(101, 102); // nulls-first ordering, everything else excluded
    }

    [Fact]
    public async Task Respects_take_limit()
    {
        var now = DateTime.UtcNow;
        var retryBefore = now.AddMinutes(-60);

        await using var db = NewDb();
        for (long id = 201; id <= 205; id++)
            db.PaymentProfiles.Add(DataSubmitted(id, lastAttempt: null, attempts: 0));
        await db.SaveChangesAsync();

        var repo = new ProviderPaymentProfileRepository(db);
        var due = await repo.GetProvisioningRetryDueIdsAsync(retryBefore, maxAttempts: 10, take: 2);

        due.Should().HaveCount(2);
    }
}
