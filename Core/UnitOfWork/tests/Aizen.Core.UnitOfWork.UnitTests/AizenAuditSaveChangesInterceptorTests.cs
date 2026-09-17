using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.UnitOfWork.Interceptors;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiniUow;
using MiniUow.DependencyInjection;

namespace Aizen.Core.UnitOfWork.UnitTests;

/// <summary>
/// Proves the systemic fix for the NULL-CreateDate bypass class: audit stamps are applied at the DbContext layer, so a
/// save through a BARE DbContext (no UnitOfWork — the pattern of 306 direct-save call sites) is stamped, and a save
/// through AizenUnitOfWork is stamped exactly once (no double-stamp, ModifyDate not set on insert). Delete semantics
/// are also pinned: bare-context deletes stay PHYSICAL (bypass sites depend on it), UnitOfWork deletes stay SOFT.
/// </summary>
public sealed class AizenAuditSaveChangesInterceptorTests
{
    private static AuditTestDbContext NewBareContext(IAizenInfoAccessor info, string db) =>
        new(new DbContextOptionsBuilder<AuditTestDbContext>()
            .UseInMemoryDatabase(db)
            .AddInterceptors(new AizenAuditSaveChangesInterceptor(info))
            .Options);

    [Fact]
    public async Task Added_entity_saved_through_bare_context_is_stamped()
    {
        await using var ctx = NewBareContext(FakeInfo.With(userId: 42, host: "host-A"), nameof(Added_entity_saved_through_bare_context_is_stamped));
        var e = new AuditTestEntity { Name = "insert" };
        ctx.Items.Add(e);

        await ctx.SaveChangesAsync();

        e.CreateDate.Should().NotBeNull();
        e.CreateUserId.Should().Be(42);
        e.CreateHost.Should().Be("host-A");
        e.ModifyDate.Should().BeNull("ModifyDate must not be set on insert");
        e.ModifyUserId.Should().BeNull();
    }

    [Fact]
    public async Task Modified_entity_saved_through_bare_context_gets_modify_stamps_only()
    {
        await using var ctx = NewBareContext(FakeInfo.With(userId: 7), nameof(Modified_entity_saved_through_bare_context_gets_modify_stamps_only));
        var e = new AuditTestEntity { Name = "v1" };
        ctx.Items.Add(e);
        await ctx.SaveChangesAsync();
        var createdAt = e.CreateDate;

        e.Name = "v2";
        await ctx.SaveChangesAsync();

        e.ModifyDate.Should().NotBeNull();
        e.ModifyUserId.Should().Be(7);
        e.ModifyHost.Should().NotBeNullOrEmpty();
        e.CreateDate.Should().Be(createdAt, "CreateDate is immutable after insert");
        e.CreateUserId.Should().Be(7);
    }

    // Delete semantics are deliberately preserved: only the UnitOfWork path soft-deletes (see next test). Direct-save
    // call sites rely on PHYSICAL deletes (delete-all+reinsert under unique indexes, consumed-token cleanup), so the
    // interceptor must not convert a bare-context delete.
    [Fact]
    public async Task Deleted_entity_through_bare_context_remains_a_physical_delete()
    {
        var db = nameof(Deleted_entity_through_bare_context_remains_a_physical_delete);
        long id;
        await using (var ctx = NewBareContext(FakeInfo.With(userId: 5), db))
        {
            var e = new AuditTestEntity { Name = "to-delete" };
            ctx.Items.Add(e);
            await ctx.SaveChangesAsync();
            id = e.Id;

            ctx.Items.Remove(e);
            await ctx.SaveChangesAsync();
        }

        // Fresh context on the same in-memory store: the row must be GONE — the interceptor does not soft-delete.
        await using var verify = NewBareContext(FakeInfo.With(userId: 5), db);
        var row = await verify.Items.SingleOrDefaultAsync(x => x.Id == id);
        row.Should().BeNull("direct-save deletes stay physical; soft-delete is a UnitOfWork-path behavior");
    }

    [Fact]
    public async Task Deleted_entity_through_AizenUnitOfWork_is_converted_to_soft_delete_with_modify_stamps()
    {
        var info = FakeInfo.With(userId: 5, host: "uow-host");
        var services = new ServiceCollection();
        services.AddSingleton(info);
        services.AddDbContext<AuditTestDbContext>(o => o
            .UseInMemoryDatabase("uow-del-" + Guid.NewGuid().ToString("N"))
            .AddInterceptors(new AizenAuditSaveChangesInterceptor(info)));
        services.AddUnitOfWork<AuditTestDbContext>();

        using var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var uow = new AizenUnitOfWork<AuditTestDbContext>(
            scope.ServiceProvider.GetRequiredService<IUnitOfWork<AuditTestDbContext>>(), info);

        var e = new AuditTestEntity { Name = "to-soft-delete" };
        uow.Context.Items.Add(e);
        await uow.SaveChangesAsync();

        uow.Context.Items.Remove(e);
        await uow.SaveChangesAsync();

        // UnitOfWork flips Deleted -> Modified + IsDeleted, then the interceptor stamps Modify* on that update.
        var row = await uow.Context.Items.SingleOrDefaultAsync(x => x.Id == e.Id);
        row.Should().NotBeNull("UnitOfWork-path deletes are soft — the row survives");
        row!.IsDeleted.Should().BeTrue();
        row.ModifyDate.Should().NotBeNull();
        row.ModifyUserId.Should().Be(5);
    }

    [Fact]
    public async Task No_user_context_falls_back_to_user_id_1()
    {
        await using var ctx = NewBareContext(FakeInfo.With(userId: null), nameof(No_user_context_falls_back_to_user_id_1));
        var e = new AuditTestEntity { Name = "job" };
        ctx.Items.Add(e);

        await ctx.SaveChangesAsync();

        e.CreateUserId.Should().Be(1, "background paths with no authenticated user fall back to userId 1");
        e.CreateDate.Should().NotBeNull();
    }

    [Fact]
    public async Task Saving_through_AizenUnitOfWork_yields_a_single_consistent_stamp()
    {
        var info = FakeInfo.With(userId: 99, host: "uow-host");
        var services = new ServiceCollection();
        services.AddSingleton(info);
        services.AddDbContext<AuditTestDbContext>(o => o
            .UseInMemoryDatabase("uow-" + Guid.NewGuid().ToString("N"))
            .AddInterceptors(new AizenAuditSaveChangesInterceptor(info)));
        services.AddUnitOfWork<AuditTestDbContext>();

        using var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var uow = new AizenUnitOfWork<AuditTestDbContext>(
            scope.ServiceProvider.GetRequiredService<IUnitOfWork<AuditTestDbContext>>(), info);

        var e = new AuditTestEntity { Name = "via-uow" };
        uow.Context.Items.Add(e);
        await uow.SaveChangesAsync();

        // Interceptor is the sole stamping site — UnitOfWork no longer stamps, so no double-stamp is possible.
        e.CreateDate.Should().NotBeNull();
        e.CreateUserId.Should().Be(99);
        e.CreateHost.Should().Be("uow-host");
        e.ModifyDate.Should().BeNull("insert through UnitOfWork must not set ModifyDate");
    }

    // Mirrors the production wiring in AddAizenUnitOfWork: interceptor registered SCOPED and attached via the
    // (sp, options) overload, resolved from the same scope as the context. Uses a fresh DI scope with NO HTTP context
    // (the Hangfire-job / MassTransit-consumer shape) and scope validation ON, to prove the scoped-service-in-options
    // resolution is legal and stamps correctly on background save paths.
    [Fact]
    public async Task Scoped_interceptor_resolved_via_options_overload_stamps_in_a_background_scope()
    {
        var services = new ServiceCollection();
        services.AddScoped<IAizenInfoAccessor>(_ => FakeInfo.With(userId: null)); // no authenticated user (job)
        services.AddScoped<AizenAuditSaveChangesInterceptor>();
        services.AddDbContext<AuditTestDbContext>((sp, o) => o
            .UseInMemoryDatabase("bg-" + Guid.NewGuid().ToString("N"))
            .AddInterceptors(sp.GetRequiredService<AizenAuditSaveChangesInterceptor>()));

        // ValidateScopes:true would throw if a scoped service were captured by a root/singleton — proves the wiring is sound.
        using var sp = services.BuildServiceProvider(validateScopes: true);
        using var scope = sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AuditTestDbContext>();

        var e = new AuditTestEntity { Name = "from-job" };
        ctx.Items.Add(e);
        await ctx.SaveChangesAsync();

        e.CreateDate.Should().NotBeNull();
        e.CreateUserId.Should().Be(1, "no user context in a background scope → userId 1");
    }
}
