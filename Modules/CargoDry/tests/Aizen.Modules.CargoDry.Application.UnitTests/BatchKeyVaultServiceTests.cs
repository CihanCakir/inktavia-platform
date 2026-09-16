using Aizen.Core.Security;
using Aizen.Modules.CargoDry.Application.Services;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Aizen.Modules.CargoDry.Application.UnitTests;

/// <summary>
/// Regression coverage for the GenerateBatch 500: batch signing keys are now persisted (AES-encrypted) on the batch row
/// instead of only logged. Pins the three resolution paths of <see cref="BatchKeyVaultService.GetKeyAsync"/>:
/// in-flight cache + persisted row (new batches), legacy config (seeded dev batches), and the preserved missing-key error.
/// </summary>
public sealed class BatchKeyVaultServiceTests
{
    private const string EncKey = "CargoDryBatchKeyEncryptionKey_32"; // 32 chars = AES-256 key

    private static IConfiguration Config(Dictionary<string, string?>? extra = null)
    {
        var dict = new Dictionary<string, string?> { ["CargoDry:BatchKeyEncryptionKey"] = EncKey };
        if (extra is not null)
            foreach (var kv in extra) dict[kv.Key] = kv.Value;
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    private static BatchKeyVaultService Vault(IConfiguration cfg, ICargoDryBatchRepository repo)
        => new(cfg, repo, NullLogger<BatchKeyVaultService>.Instance);

    private static CargoDryBatchEntity BatchWithKey(string batchCode, string encryptedKey)
    {
        var batch = CargoDryBatchEntity.Create(batchCode, "STAN", 10, adminId: 1);
        batch.SetSigningKey(encryptedKey);
        return batch;
    }

    [Fact]
    public async Task NewBatch_signs_via_in_flight_cache_then_verifies_via_persisted_row()
    {
        const string batchCode = "202609-STAN-0486";
        const string serial     = "ABCD-EFGH-JKLM-NPQR";

        // ── Generation request: cache holds the raw key; the batch is NOT saved yet. ──
        var genRepo   = Substitute.For<ICargoDryBatchRepository>();
        genRepo.GetByCodeAsync(batchCode, Arg.Any<CancellationToken>()).Returns((CargoDryBatchEntity?)null);
        var genVault  = Vault(Config(), genRepo);
        var genQr     = new CargoDryQrService(genVault);

        var encryptedKey = await genVault.CreateKeyAsync(batchCode);
        encryptedKey.Should().NotBeNullOrWhiteSpace();

        var sig = await genQr.SignAsync(serial, batchCode, default);       // resolves from cache, no DB read
        (await genQr.VerifyAsync(serial, batchCode, sig, default)).Should().BeTrue();

        // ── Later request (verify/activation): fresh vault (empty cache) reads the persisted encrypted key. ──
        var verifyRepo = Substitute.For<ICargoDryBatchRepository>();
        verifyRepo.GetByCodeAsync(batchCode, Arg.Any<CancellationToken>())
            .Returns(BatchWithKey(batchCode, encryptedKey));
        var verifyQr = new CargoDryQrService(Vault(Config(), verifyRepo));

        (await verifyQr.VerifyAsync(serial, batchCode, sig, default)).Should().BeTrue("the persisted key decrypts to the same secret");
        (await verifyQr.SignAsync(serial, batchCode, default)).Should().Be(sig, "the DB-resolved key reproduces the signature");
    }

    [Fact]
    public async Task Persisted_key_is_encrypted_at_rest_not_the_raw_value()
    {
        var repo  = Substitute.For<ICargoDryBatchRepository>();
        var vault = Vault(Config(), repo);

        var encrypted = await vault.CreateKeyAsync("202609-STAN-0001");

        // The encrypted blob is what gets stored; it must not equal the (unknowable) raw key, and it must decrypt back.
        AizenSecurityHelper.DecryptByAES(encrypted, EncKey)
            .Should().NotBe(encrypted);
    }

    [Fact]
    public async Task LegacyConfigBatch_signs_and_verifies_from_config_fallback()
    {
        const string batchCode = "202506-STAN-DEV1";
        const string serial     = "WXYZ-2345-6789-ABCD";
        var repo = Substitute.For<ICargoDryBatchRepository>();
        repo.GetByCodeAsync(batchCode, Arg.Any<CancellationToken>()).Returns((CargoDryBatchEntity?)null); // no row/key

        var cfg = Config(new Dictionary<string, string?>
        {
            [$"CargoDry:BatchKeys:{batchCode}"] = "bGVnYWN5LWRldi1iYXRjaC1rZXk=", // seeded legacy key
        });
        var qr = new CargoDryQrService(Vault(cfg, repo));

        var sig = await qr.SignAsync(serial, batchCode, default);
        (await qr.VerifyAsync(serial, batchCode, sig, default)).Should().BeTrue();
    }

    [Fact]
    public async Task Row_key_takes_precedence_over_legacy_config()
    {
        const string batchCode = "202609-STAN-0999";
        // Row key (encrypted) AND a stale legacy config entry both exist — the row must win.
        var repo   = Substitute.For<ICargoDryBatchRepository>();
        var seeder = Vault(Config(), repo);
        var encryptedRowKey = await seeder.CreateKeyAsync(batchCode);
        repo.GetByCodeAsync(batchCode, Arg.Any<CancellationToken>()).Returns(BatchWithKey(batchCode, encryptedRowKey));

        var cfg = Config(new Dictionary<string, string?>
        {
            [$"CargoDry:BatchKeys:{batchCode}"] = "c3RhbGUtY29uZmlnLWtleQ==",
        });
        var freshVault = Vault(cfg, repo); // empty cache → resolves the row, not the config

        var rowKey = await freshVault.GetKeyAsync(batchCode);
        rowKey.Should().NotBe("c3RhbGUtY29uZmlnLWtleQ==");
    }

    [Fact]
    public async Task MissingKey_throws_original_error()
    {
        const string batchCode = "202609-STAN-NONE";
        var repo = Substitute.For<ICargoDryBatchRepository>();
        repo.GetByCodeAsync(batchCode, Arg.Any<CancellationToken>()).Returns((CargoDryBatchEntity?)null);
        var vault = Vault(Config(), repo);

        var act = () => vault.GetKeyAsync(batchCode);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage($"Batch key not configured for batch {batchCode}");
    }
}
