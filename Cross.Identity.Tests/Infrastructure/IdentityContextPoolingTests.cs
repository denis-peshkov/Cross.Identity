namespace Cross.Identity.Tests.Infrastructure;

[TestFixture]
[Category(TestCategory.UNIT)]
public sealed class IdentityContextPoolingTests
{
    [Test]
    public void GivenAddDbContextPool_WhenCreateAndSave_ThenDoesNotThrowOptionsMutation()
    {
        var dbName = $"pool-{Guid.NewGuid():N}";
        var services = new ServiceCollection();
        services.AddDbContextPool<IdentityContext>(options =>
            options.UseInMemoryDatabase(dbName));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<IdentityContext>();

        var id = Guid.NewGuid();
        ctx.UsersAccounts.Add(new UserAccountEntity
        {
            Id = id,
            Email = "pool@example.com",
            CreatedAt = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid(),
            ConcurrencyStamp = Guid.Empty,
        });

        FluentActions.Invoking(() => ctx.SaveChanges()).Should().NotThrow();

        var user = ctx.UsersAccounts.Single(x => x.Id == id);
        user.ConcurrencyStamp.Should().NotBe(Guid.Empty);
    }

    [Test]
    public void GivenAddPooledDbContextFactory_WhenCreateAndSave_ThenDoesNotThrowOptionsMutation()
    {
        var dbName = $"factory-{Guid.NewGuid():N}";
        var services = new ServiceCollection();
        services.AddPooledDbContextFactory<IdentityContext>(options =>
            options.UseInMemoryDatabase(dbName));

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IDbContextFactory<IdentityContext>>();

        using var ctx = factory.CreateDbContext();
        var id = Guid.NewGuid();
        ctx.UsersAccounts.Add(new UserAccountEntity
        {
            Id = id,
            Email = "factory@example.com",
            CreatedAt = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid(),
            ConcurrencyStamp = Guid.Empty,
        });

        FluentActions.Invoking(() => ctx.SaveChanges()).Should().NotThrow();

        var user = ctx.UsersAccounts.Single(x => x.Id == id);
        user.ConcurrencyStamp.Should().NotBe(Guid.Empty);
    }

    [Test]
    public void GivenPooledContext_WhenEntityUpdatedTwice_ThenConcurrencyStampRotatesEachSave()
    {
        var dbName = $"rotate-{Guid.NewGuid():N}";
        var services = new ServiceCollection();
        services.AddDbContextPool<IdentityContext>(options =>
            options.UseInMemoryDatabase(dbName));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<IdentityContext>();

        var id = Guid.NewGuid();
        ctx.UsersAccounts.Add(new UserAccountEntity
        {
            Id = id,
            Email = "rotate@example.com",
            CreatedAt = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
        });
        ctx.SaveChanges();

        var user = ctx.UsersAccounts.Single(x => x.Id == id);
        var first = user.ConcurrencyStamp;

        user.EmailVerified = true;
        ctx.SaveChanges();
        var second = user.ConcurrencyStamp;

        second.Should().NotBe(first);

        user.PhoneNumberVerified = true;
        ctx.SaveChanges();
        user.ConcurrencyStamp.Should().NotBe(second);
    }
}
