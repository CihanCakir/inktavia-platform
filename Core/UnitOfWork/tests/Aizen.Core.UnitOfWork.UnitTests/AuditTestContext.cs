using Aizen.Core.Domain;
using Aizen.Core.InfoAccessor.Abstraction;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Aizen.Core.UnitOfWork.UnitTests;

/// <summary>A minimal audited entity for exercising the audit interceptor.</summary>
public sealed class AuditTestEntity : AizenEntityWithAudit
{
    public string Name { get; set; } = default!;
}

/// <summary>Bare EF context — no UnitOfWork — used to prove the interceptor stamps regardless of save path.</summary>
public sealed class AuditTestDbContext : DbContext
{
    public AuditTestDbContext(DbContextOptions<AuditTestDbContext> options) : base(options) { }

    public DbSet<AuditTestEntity> Items => Set<AuditTestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // PublicId is [DatabaseGenerated(Computed)] on the base; the InMemory provider can't compute it — ignore it.
        modelBuilder.Entity<AuditTestEntity>().Ignore(x => x.PublicId);
    }
}

/// <summary>Test-double factory for <see cref="IAizenInfoAccessor"/>: only the members the interceptor reads.</summary>
public static class FakeInfo
{
    public static IAizenInfoAccessor With(long? userId, string host = "test-host")
    {
        var info = Substitute.For<IAizenInfoAccessor>();

        var userAccessor = Substitute.For<IAizenUserInfoAccessor>();
        userAccessor.UserInfo.Returns(userId is null ? null! : new AizenUserInfo { UserId = userId.Value });
        info.UserInfoAccessor.Returns(userAccessor);

        var serverAccessor = Substitute.For<IAizenServerInfoAccessor>();
        serverAccessor.ServerInfo.Returns(new AizenServerInfo { MachineName = host });
        info.ServerInfoAccessor.Returns(serverAccessor);

        return info;
    }
}
