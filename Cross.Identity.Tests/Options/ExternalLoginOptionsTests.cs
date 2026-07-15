namespace Cross.Identity.Tests.Options;

[Category(TestCategory.UNIT)]
[TestFixture]
public class ExternalLoginOptionsTests
{
    [Test]
    public void ExternalLoginProviderOptions_IsConfigured_ShouldBeFalse_WhenCredentialsMissing()
    {
        var options = new ExternalLoginProviderOptions
        {
            ClientId = "id",
            ClientSecret = string.Empty,
        };

        options.IsConfigured.Should().BeFalse();
    }

    [Test]
    public void ExternalLoginProviderOptions_IsConfigured_ShouldBeFalse_WhenDisabled()
    {
        var options = new ExternalLoginProviderOptions
        {
            ClientId = "id",
            ClientSecret = "secret",
            IsEnabled = false,
        };

        options.IsConfigured.Should().BeFalse();
    }

    [Test]
    public void ExternalLoginProviderOptions_IsConfigured_ShouldBeTrue_WhenEnabledAndConfigured()
    {
        var options = new ExternalLoginProviderOptions
        {
            ClientId = "id",
            ClientSecret = "secret",
            IsEnabled = true,
        };

        options.IsConfigured.Should().BeTrue();
    }
}
