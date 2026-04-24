namespace Cross.Identity.UnitTests.Core;

[TestFixture]
public sealed class JsonHelpersAndOptionsTests
{
    [Test]
    public void JsonHelpers_Methods_ShouldHandleCommonCases()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "name":"abc",
              "enabled":true,
              "disabled":false,
              "seconds":12.5,
              "obj":{"x":1},
              "enumValue":"RefreshToken"
            }
            """);
        var root = json.RootElement;

        root.Str("name").Should().Be("abc");
        root.StrOpt("missing").Should().BeNull();
        root.BoolOpt("enabled").Should().BeTrue();
        root.BoolOpt("disabled").Should().BeFalse();
        root.BoolOpt("missing").Should().BeNull();
        root.TimeSpanSecondsOpt("seconds").Should().Be(TimeSpan.FromSeconds(12.5));
        root.TimeSpanSecondsOpt("missing").Should().BeNull();
        root.EnumOpt<FlowOperationEnum>("enumValue").Should().Be(FlowOperationEnum.RefreshToken);
        root.EnumOpt<FlowOperationEnum>("missing").Should().BeNull();
        root.Obj("obj").ValueKind.Should().Be(JsonValueKind.Object);

        Action badObj = () => root.Obj("name");
        badObj.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void StepFactoryJsonGuards_ShouldValidateMismatch()
    {
        using var okJson = JsonDocument.Parse("""{"kind":"collectForm"}""");
        var actOk = () => StepFactoryJsonGuards.ValidateOptionalKind(okJson.RootElement, "collectForm");
        actOk.Should().NotThrow();

        using var badJson = JsonDocument.Parse("""{"kind":"other"}""");
        var actBad = () => StepFactoryJsonGuards.ValidateOptionalKind(badJson.RootElement, "collectForm");
        actBad.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void EmbeddedProcessDefinitionOptions_AssemblyName_Property_ShouldSetAssembly()
    {
        var opt = new EmbeddedProcessDefinitionOptions();
        opt.AssemblyName = typeof(EmbeddedProcessDefinitionOptions).Assembly.GetName().Name!;

        opt.Assembly.Should().NotBeNull();
        opt.Assembly.GetName().Name.Should().NotBeNullOrWhiteSpace();
    }
}
