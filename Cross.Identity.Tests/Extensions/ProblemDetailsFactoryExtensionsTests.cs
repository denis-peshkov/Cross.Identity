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
}
