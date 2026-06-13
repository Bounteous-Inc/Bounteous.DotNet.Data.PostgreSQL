using Microsoft.EntityFrameworkCore;

namespace Bounteous.Data.PostgreSQL;

public abstract class PostgresDbContextFactory<T, TUserId>(
    IConnectionBuilder connectionBuilder,
    IDbContextObserver observer,
    IIdentityProvider<TUserId> identityProvider)
    : DbContextFactory<T, TUserId>(connectionBuilder, observer, identityProvider)
    where T : IDbContext<TUserId>
    where TUserId : struct
{
    protected override DbContextOptions ApplyOptions(bool sensitiveDataLoggingEnabled = false)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        return new DbContextOptionsBuilder<DbContextBase<TUserId>>().UseNpgsql(ConnectionBuilder.AdminConnectionString,
                sqlOptions => { sqlOptions.EnableRetryOnFailure(); })
            .UseSnakeCaseNamingConvention()
            .EnableSensitiveDataLogging(sensitiveDataLoggingEnabled: sensitiveDataLoggingEnabled)
            .EnableDetailedErrors()
            .Options;
    }
}
