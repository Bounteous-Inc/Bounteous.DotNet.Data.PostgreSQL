using Bounteous.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Bounteous.Data.PostgreSQL.Tests;

public class PostgresDbContextFactoryTests
{
    private class TestDbContext(
        DbContextOptions options,
        IDbContextObserver observer,
        IIdentityProvider<Guid> identityProvider)
        : DbContextBase<Guid>(options, observer, identityProvider)
    {
        protected override void RegisterModels(ModelBuilder modelBuilder)
        {
        }
    }

    private class TestPostgresDbContextFactory(
        IConnectionBuilder connectionBuilder,
        IDbContextObserver observer,
        IIdentityProvider<Guid> identityProvider)
        : PostgresDbContextFactory<TestDbContext, Guid>(connectionBuilder, observer, identityProvider)
    {
        public DbContextOptions TestApplyOptions(bool sensitiveDataLoggingEnabled = false)
            => ApplyOptions(sensitiveDataLoggingEnabled);

        protected override TestDbContext Create(DbContextOptions options, IDbContextObserver observer,
            IIdentityProvider<Guid> identityProvider)
            => new(options, observer, identityProvider);
    }

    private static TestPostgresDbContextFactory CreateFactory(string connectionString,
        out Mock<IConnectionBuilder> mockConnectionBuilder)
    {
        mockConnectionBuilder = new Mock<IConnectionBuilder>();
        mockConnectionBuilder.Setup(x => x.AdminConnectionString).Returns(connectionString);
        var mockObserver = new Mock<IDbContextObserver>();
        var mockIdentityProvider = new Mock<IIdentityProvider<Guid>>();
        return new TestPostgresDbContextFactory(mockConnectionBuilder.Object, mockObserver.Object,
            mockIdentityProvider.Object);
    }

    private static TestPostgresDbContextFactory CreateFactory(Mock<IConnectionBuilder> mockConnectionBuilder)
    {
        var mockObserver = new Mock<IDbContextObserver>();
        var mockIdentityProvider = new Mock<IIdentityProvider<Guid>>();
        return new TestPostgresDbContextFactory(mockConnectionBuilder.Object, mockObserver.Object,
            mockIdentityProvider.Object);
    }

    private const string ValidConnectionString = "Host=localhost;Database=test;Username=test;Password=test";

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        var factory = CreateFactory(ValidConnectionString, out _);

        Assert.NotNull(factory);
    }

    [Fact]
    public void ApplyOptions_WithDefaultParameters_ReturnsConfiguredOptions()
    {
        var factory = CreateFactory(ValidConnectionString, out _);
        var options = factory.TestApplyOptions();

        Assert.NotNull(options);
    }

    [Fact]
    public void ApplyOptions_WithSensitiveDataLoggingEnabled_ReturnsConfiguredOptions()
    {
        var factory = CreateFactory(ValidConnectionString, out _);
        var options = factory.TestApplyOptions(sensitiveDataLoggingEnabled: true);

        Assert.NotNull(options);
    }

    [Fact]
    public void ApplyOptions_WithSensitiveDataLoggingDisabled_ReturnsConfiguredOptions()
    {
        var factory = CreateFactory(ValidConnectionString, out _);
        var options = factory.TestApplyOptions(sensitiveDataLoggingEnabled: false);

        Assert.NotNull(options);
    }

    [Fact]
    public void ApplyOptions_SetsNpgsqlLegacyTimestampBehavior()
    {
        var factory = CreateFactory(ValidConnectionString, out _);
        factory.TestApplyOptions();

        var switchValue = AppContext.TryGetSwitch("Npgsql.EnableLegacyTimestampBehavior", out var isEnabled);
        Assert.True(switchValue);
        Assert.True(isEnabled);
    }

    [Fact]
    public void ApplyOptions_UsesConnectionStringFromConnectionBuilder()
    {
        const string expectedConnectionString = "Host=testhost;Database=testdb;Username=testuser;Password=testpass";
        var factory = CreateFactory(expectedConnectionString, out var mockConnectionBuilder);
        var options = factory.TestApplyOptions();

        Assert.NotNull(options);
        mockConnectionBuilder.Verify(x => x.AdminConnectionString, Times.AtLeastOnce);
    }

    [Fact]
    public void ApplyOptions_CalledMultipleTimes_ReturnsNewOptionsEachTime()
    {
        var factory = CreateFactory(ValidConnectionString, out _);
        var options1 = factory.TestApplyOptions();
        var options2 = factory.TestApplyOptions();

        Assert.NotNull(options1);
        Assert.NotNull(options2);
        Assert.NotSame(options1, options2);
    }

    [Fact]
    public void ApplyOptions_WithDifferentConnectionStrings_UsesCorrectConnectionString()
    {
        const string connectionString1 = "Host=server1;Database=db1;Username=user1;Password=pass1";
        const string connectionString2 = "Host=server2;Database=db2;Username=user2;Password=pass2";

        var mockConnectionBuilder = new Mock<IConnectionBuilder>();
        mockConnectionBuilder.SetupSequence(x => x.AdminConnectionString)
            .Returns(connectionString1)
            .Returns(connectionString2);

        var factory = CreateFactory(mockConnectionBuilder);

        var options1 = factory.TestApplyOptions();
        var options2 = factory.TestApplyOptions();

        Assert.NotNull(options1);
        Assert.NotNull(options2);
        mockConnectionBuilder.Verify(x => x.AdminConnectionString, Times.Exactly(2));
    }

    [Fact]
    public void Constructor_InheritsFromDbContextFactory()
    {
        var factory = CreateFactory(ValidConnectionString, out _);

        Assert.IsAssignableFrom<DbContextFactory<TestDbContext, Guid>>(factory);
    }

    [Fact]
    public void ApplyOptions_OptionsHaveCorrectType()
    {
        var factory = CreateFactory(ValidConnectionString, out _);
        var options = factory.TestApplyOptions();

        Assert.IsType<DbContextOptions<DbContextBase<Guid>>>(options);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ApplyOptions_WithBothSensitiveDataLoggingValues_ReturnsValidOptions(bool sensitiveDataLogging)
    {
        var factory = CreateFactory(ValidConnectionString, out _);
        var options = factory.TestApplyOptions(sensitiveDataLogging);

        Assert.NotNull(options);
        Assert.IsType<DbContextOptions<DbContextBase<Guid>>>(options);
    }

    [Fact]
    public void ApplyOptions_WithEmptyConnectionString_StillReturnsOptions()
    {
        var factory = CreateFactory(string.Empty, out _);
        var options = factory.TestApplyOptions();

        Assert.NotNull(options);
    }

    [Fact]
    public void ApplyOptions_WithComplexConnectionString_ReturnsValidOptions()
    {
        const string complexConnectionString = "Host=localhost;Port=5432;Database=testdb;Username=testuser;Password=testpass;Pooling=true;MinPoolSize=1;MaxPoolSize=20;ConnectionLifetime=15;";
        var factory = CreateFactory(complexConnectionString, out var mockConnectionBuilder);
        var options = factory.TestApplyOptions();

        Assert.NotNull(options);
        mockConnectionBuilder.Verify(x => x.AdminConnectionString, Times.AtLeastOnce);
    }
}
