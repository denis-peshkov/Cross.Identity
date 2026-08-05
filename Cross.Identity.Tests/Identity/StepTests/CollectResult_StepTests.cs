namespace Cross.Identity.Tests.Identity.StepTests;

[Category(TestCategory.UNIT)]
[TestFixture]
public class CollectResult_StepTests
{
    [Test]
    public async Task GivenMappedBagValues_WhenExecuteAsync_ThenCollectsValuesAsync()
    {
        // Arrange
        var step = new CollectResultStep
        {
            Kind = "collectResult",
            Map = new Dictionary<string, string>
            {
                ["UserId"] = "createUser.UserId",
                ["LastCode"] = "sendCode.LastCode",
                ["Token"] = "token.AccessToken"
            },
            Next = "done"
        };

        var bag = new Bag();
        bag.Set("createUser.UserId", "user123");
        bag.Set("sendCode.LastCode", "ABC123");
        bag.Set("token.AccessToken", "jwt.token.here");

        // Act
        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StepStatusEnum.Ok);
        result.Next.Should().Be("done");
        bag.Get<string>("collectResult.UserId").Should().Be("user123");
        bag.Get<string>("collectResult.LastCode").Should().Be("ABC123");
        bag.Get<string>("collectResult.Token").Should().Be("jwt.token.here");
    }

    [Test]
    public async Task GivenMissingMappedValues_WhenExecuteAsync_ThenSkipsMissingKeysAsync()
    {
        // Arrange
        var step = new CollectResultStep
        {
            Kind = "collectResult",
            Map = new Dictionary<string, string>
            {
                ["UserId"] = "createUser.UserId",
                ["LastCode"] = "sendCode.LastCode"
            },
            Next = null
        };

        var bag = new Bag();
        bag.Set("createUser.UserId", "user123");
        // LastCode is missing

        // Act
        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StepStatusEnum.Ok);
        bag.Get<string>("collectResult.UserId").Should().Be("user123");
        bag.TryGet<string>("collectResult.LastCode", out _).Should().BeFalse();
    }

    [Test]
    public async Task GivenRelativeKeys_WhenExecuteAsync_ThenCollectsValuesAsync()
    {
        // Arrange
        var step = new CollectResultStep
        {
            Kind = "collectResult",
            Map = new Dictionary<string, string>
            {
                ["UserId"] = "UserId" // relative key
            },
            Next = null
        };

        var bag = new Bag();
        bag.Set("collectResult.UserId", "user123");

        // Act
        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StepStatusEnum.Ok);
        bag.Get<string>("collectResult.UserId").Should().Be("user123");
    }

    [Test]
    public async Task GivenReturnEmptyFlag_WhenExecuteAsync_ThenSetsEmptyFlagAsync()
    {
        // Arrange
        var step = new CollectResultStep
        {
            Kind = "collectResult",
            Map = new Dictionary<string, string>
            {
                ["UserId"] = "createUser.UserId"
            },
            ReturnEmpty = true,
            Next = null
        };

        var bag = new Bag();
        bag.Set("createUser.UserId", "user123");

        // Act
        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StepStatusEnum.Ok);
        bag.Get<bool>("collectResult._empty").Should().BeTrue();
        bag.TryGet<string>("collectResult.UserId", out _).Should().BeFalse();
    }
}
