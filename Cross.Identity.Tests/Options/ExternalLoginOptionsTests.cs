namespace Cross.Identity.Tests.Options;

[TestFixture]
public class ExternalLoginOptionsTests
{
    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenMissingCredentials_WhenIsConfiguredChecked_ThenReturnsFalse()
    {
        var options = new ExternalLoginProviderOptions
        {
            ClientId = "id",
            ClientSecret = string.Empty,
        };

        options.IsConfigured.Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenDisabledProvider_WhenIsConfiguredChecked_ThenReturnsFalse()
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
    [Category(TestCategory.UNIT)]
    public void GivenEnabledConfiguredProvider_WhenIsConfiguredChecked_ThenReturnsTrue()
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
