using Aizen.Modules.Messaging.Repository.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Aizen.Modules.Messaging.Repository.DesignTime;

public sealed class MessagingDesignTimeFactory : IDesignTimeDbContextFactory<MessagingDbContext>
{
    public MessagingDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MessagingDbContext>()
            .UseNpgsql("Host=localhost;Database=aizen;Username=postgres;Password=postgres;Search Path=messaging")
            .Options;
        return new MessagingDbContext(options);
    }
}
