namespace Cross.Identity.Tests.Entities;

[TestFixture]
[Category(TestCategory.UNIT)]
public sealed class UserCommunicationEndpointEntityConfigurationTests
{
    [Test]
    public void GivenModel_WhenBuilt_ThenHasPreferredUniqueIndexPerUser()
    {
        var options = new DbContextOptionsBuilder<IdentityContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new IdentityContext(options);
        var entityType = context.Model.FindEntityType(typeof(UserCommunicationEndpointEntity));
        entityType.Should().NotBeNull();

        var index = entityType!.GetIndexes()
            .Single(i => i.GetDatabaseName() == "UX_auth_UsersCommunicationEndpoints_User_Preferred");

        index.IsUnique.Should().BeTrue();
        index.GetFilter().Should().Contain("IsPreferred");
    }
}
