namespace Cross.Identity.Tests.Helpers;

[TestFixture]
public class EmailOrPhoneBagTests
{
    [Test]
    public void GivenEmailPhoneAndUserName_WhenResolve_ThenPrefersEmail()
    {
        var bag = new Bag()
            .Set("collectForm.Email", "a@b.co")
            .Set("collectForm.PhoneNumber", "+79161234567")
            .Set("collectForm.UserName", "alice");

        var (field, value, channel) = EmailOrPhoneBag.Resolve(
            bag, "token", "collectForm.Email", "collectForm.PhoneNumber", "collectForm.UserName");

        field.Should().Be("Email");
        value.Should().Be("a@b.co");
        channel.Should().Be(ChannelEnum.Email);
    }

    [Test]
    public void GivenOnlyPhone_WhenResolve_ThenUsesPhoneAndSms()
    {
        var bag = new Bag().Set("collectForm.PhoneNumber", "+79161234567");

        var (field, value, channel) = EmailOrPhoneBag.Resolve(
            bag, "token", "collectForm.Email", "collectForm.PhoneNumber", "collectForm.UserName");

        field.Should().Be("PhoneNumber");
        value.Should().Be("+79161234567");
        channel.Should().Be(ChannelEnum.Sms);
    }

    [Test]
    public void GivenOnlyUserName_WhenResolve_ThenUsesUserNameWithoutChannel()
    {
        var bag = new Bag().Set("collectForm.UserName", "alice");

        var (field, value, channel) = EmailOrPhoneBag.Resolve(
            bag, "token", "collectForm.Email", "collectForm.PhoneNumber", "collectForm.UserName");

        field.Should().Be("UserName");
        value.Should().Be("alice");
        channel.Should().BeNull();
    }

    [Test]
    public void GivenNeither_WhenResolve_ThenThrowsValidationException()
    {
        FluentActions.Invoking(() => EmailOrPhoneBag.Resolve(
                new Bag(), "token", "collectForm.Email", "collectForm.PhoneNumber", "collectForm.UserName"))
            .Should().Throw<ValidationException>()
            .WithMessage("*email, phone, or user name*");
    }

    [Test]
    public void GivenPhoneNumberKeyOnly_WhenIsMultiSelector_ThenTrue()
    {
        EmailOrPhoneBag.IsMultiSelector("collectForm.PhoneNumber", null).Should().BeTrue();
        EmailOrPhoneBag.IsMultiSelector(null, "collectForm.UserName").Should().BeTrue();
        EmailOrPhoneBag.IsMultiSelector(null, null).Should().BeFalse();
    }
}
