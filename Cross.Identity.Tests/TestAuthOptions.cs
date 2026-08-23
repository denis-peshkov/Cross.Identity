namespace Cross.Identity.Tests;

internal static class TestAuthOptions
{
    public static IOptionsSnapshot<AuthenticationOptions> Snapshot(AuthenticationOptions? value = null)
    {
        var mock = new Mock<IOptionsSnapshot<AuthenticationOptions>>();
        mock.Setup(o => o.Value).Returns(value ?? new AuthenticationOptions());
        return mock.Object;
    }
}
