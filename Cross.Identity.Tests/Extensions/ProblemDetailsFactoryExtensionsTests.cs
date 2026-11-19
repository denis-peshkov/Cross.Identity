namespace Cross.Identity.Tests.Extensions;

[TestFixture]
public class ProblemDetailsFactoryExtensionsTests
{
    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenValidationException_WhenToValidationProblemDetails_ThenMapsErrors()
    {
        var ex = new ValidationException(new[]
        {
            new ValidationFailure("Email", "Required"),
            new ValidationFailure("Email", "Invalid format"),
            new ValidationFailure("Password", "Too short"),
        });

        var result = ex.ToValidationProblemDetails();

        result.Status.Should().Be(406);
        result.Title.Should().Be("Validation Failed");
        result.Errors.Should().ContainKey("Email").WhoseValue.Should().Equal("Required", "Invalid format");
        result.Errors.Should().ContainKey("Password").WhoseValue.Should().Equal("Too short");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenErrorDictionary_WhenToValidationProblemDetails_ThenSetsStatusAndTitle()
    {
        var errors = new Dictionary<string, string[]>
        {
            ["Field"] = new[] { "Error1" },
        };

        var result = errors.ToValidationProblemDetails();

        result.Status.Should().Be(406);
        result.Title.Should().Be("Validation Failed");
        result.Errors.Should().BeEquivalentTo(errors);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenAccountMergeConflict_WhenToConflictProblemDetails_ThenMaps409AndFields()
    {
        var absorbed = Guid.NewGuid();
        var survivor = Guid.NewGuid();
        var ex = AccountMergeRequired.Create(
            "This external account is already linked to another user.",
            "AABBCCDDEEFF00112233445566778899",
            absorbed,
            survivor,
            "Microsoft");

        var result = ex.ToConflictProblemDetails();

        result.Status.Should().Be(409);
        result.Title.Should().Be("Conflict");
        result.Errors[string.Empty].Should().Equal(ex.Message);
        result.Errors["code"].Should().Equal(AccountMergeRequired.ErrorCode);
        result.Errors["mergeToken"].Should().Equal("AABBCCDDEEFF00112233445566778899");
        result.Errors["absorbedUserAccountId"].Should().Equal(absorbed.ToString("D"));
        result.Errors["survivorUserAccountId"].Should().Equal(survivor.ToString("D"));
        result.Errors["provider"].Should().Equal("Microsoft");
    }
}
