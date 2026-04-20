namespace Cross.Identity.UnitTests.Identity.FlowTests;

[TestFixture]
internal class Nonexistent_FlowTests : RunFlowCommandHandlerTestsBase
{
    [Test]
    public async Task Handle_NoneExistFlow_ThrowsException()
    {
        base.Setup();

        // Arrange
        Initialize();

        var input = new Dictionary<string, object?>
        {
            ["Email"] = "test@example.com",
            ["Password"] = "P@ssw0rd!",
        };

        // Act & Assert
        await FluentActions.Invoking(() =>
                _flowExecutor.ExecuteAsync(input, "nonexistent", FlowOperationEnum.Register, CancellationToken.None))
            .Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage("*nonexistent.register*");
    }
}
