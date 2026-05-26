namespace Cross.Identity.Tests.Core;

[Category(TestCategory.UNIT)]
[TestFixture]
public sealed class EmbeddedProviderTests
{
    [Test]
    public void EmbeddedProvider_ShouldLoadAndReturnKnownFlow()
    {
        var opt = Microsoft.Extensions.Options.Options.Create(new EmbeddedProcessDefinitionOptions
        {
            Assembly = typeof(EmbeddedResourceProcessDefinitionProvider).Assembly,
            BaseNamespace = "Cross.Identity.ProcessEngine.Definitions"
        });

        var sut = new EmbeddedResourceProcessDefinitionProvider(opt);
        var json = sut.GetJson("license", FlowOperationEnum.Register);

        json.Should().Contain("\"start\"");
    }

    [Test]
    public void EmbeddedProvider_GetJson_WhenMissingOrInvalidArgs_ShouldThrow()
    {
        var opt = Microsoft.Extensions.Options.Options.Create(new EmbeddedProcessDefinitionOptions
        {
            Assembly = typeof(EmbeddedResourceProcessDefinitionProvider).Assembly,
            BaseNamespace = "Cross.Identity.ProcessEngine.Definitions"
        });

        var sut = new EmbeddedResourceProcessDefinitionProvider(opt);

        Action a1 = () => sut.GetJson("", FlowOperationEnum.Token);
        Action a2 = () => sut.GetJson("missing-flow", FlowOperationEnum.Token);
        a1.Should().Throw<ArgumentException>();
        a2.Should().Throw<KeyNotFoundException>();
    }

    [Test]
    public void EmbeddedProvider_GetTemplate_WhenMissingOrInvalidArgs_ShouldThrow()
    {
        var opt = Microsoft.Extensions.Options.Options.Create(new EmbeddedProcessDefinitionOptions
        {
            Assembly = typeof(EmbeddedResourceProcessDefinitionProvider).Assembly,
            BaseNamespace = "Cross.Identity.ProcessEngine.Definitions"
        });

        var sut = new EmbeddedResourceProcessDefinitionProvider(opt);

        Action bad1 = () => sut.GetTemplate("", "en", "txt");
        Action bad2 = () => sut.GetTemplate("welcome", "", "txt");
        Action bad3 = () => sut.GetTemplate("welcome", "en", "");
        Action missing = () => sut.GetTemplate("missing", "en", "txt");

        bad1.Should().Throw<ArgumentException>();
        bad2.Should().Throw<ArgumentException>();
        bad3.Should().Throw<ArgumentException>();
        missing.Should().Throw<KeyNotFoundException>();
    }

    [Test]
    public void EmbeddedOptions_EmptyAssemblyName_ShouldFallbackToExecutingAssembly()
    {
        var opt = new EmbeddedProcessDefinitionOptions();
        opt.AssemblyName = "";
        opt.Assembly.Should().NotBeNull();
    }
}
