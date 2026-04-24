namespace Cross.Identity.UnitTests.Core;

[TestFixture]
public sealed class Registry_Loader_And_Validator_Tests
{
    [Test]
    public void StepRegistry_RegisterCreateAndHelpers_ShouldWork()
    {
        var reg = new StepRegistry();
        reg.Register(new FakeFactory("alpha"));
        reg.RegisterRange(new[] { new FakeFactory("beta") });

        var step = reg.Create("alpha", JsonDocument.Parse("""{"kind":"alpha"}""").RootElement, new ServiceCollection().BuildServiceProvider());
        step.Kind.Should().Be("alpha");
        reg.Has("beta").Should().BeTrue();
        reg.Kinds.Should().Contain(new[] { "alpha", "beta" });

        reg.Clear();
        reg.Kinds.Should().BeEmpty();
    }

    [Test]
    public void StepRegistry_Errors_ShouldThrow()
    {
        var reg = new StepRegistry();
        var sp = new ServiceCollection().BuildServiceProvider();

        var nullFactory = () => reg.Register(null!);
        nullFactory.Should().Throw<ArgumentNullException>();

        var badFactory = () => reg.Register(new FakeFactory(""));
        badFactory.Should().Throw<ArgumentException>();

        var unknown = () => reg.Create("missing", JsonDocument.Parse("""{"kind":"missing"}""").RootElement, sp);
        unknown.Should().Throw<InvalidOperationException>();

        var noKind = () => reg.Create(JsonDocument.Parse("""{"x":1}""").RootElement, sp);
        noKind.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void ProcessLoader_FromJson_ShouldBuildAndValidate()
    {
        var reg = new StepRegistry(new[]
        {
            new FakeFactory("s1"),
            new FakeFactory("s2")
        });
        var sp = new ServiceCollection().BuildServiceProvider();

        var process = ProcessLoader.FromJson(
            """
            {
              "start":"s1",
              "steps":[
                {"kind":"s1","next":"s2"},
                {"kind":"s2"}
              ]
            }
            """,
            reg, sp);

        process.Should().NotBeNull();
    }

    [Test]
    public void ProcessLoader_FromJson_Errors_ShouldThrow()
    {
        var reg = new StepRegistry(new[] { new FakeFactory("s1") });
        var sp = new ServiceCollection().BuildServiceProvider();

        Action a1 = () => ProcessLoader.FromJson("", reg, sp);
        Action a2 = () => ProcessLoader.FromJson("""{"steps":[]}""", reg, sp);
        Action a3 = () => ProcessLoader.FromJson("""{"start":"","steps":[]}""", reg, sp);
        Action a4 = () => ProcessLoader.FromJson("""{"start":"s1"}""", reg, sp);
        Action a5 = () => ProcessLoader.FromJson("""{"start":"s2","steps":[{"kind":"s1"}]}""", reg, sp);
        Action a6 = () => ProcessLoader.FromJson("""{"start":"s1","steps":[{"kind":"s1"},{"kind":"s1"}]}""", reg, sp);

        a1.Should().Throw<ArgumentException>();
        a2.Should().Throw<InvalidOperationException>();
        a3.Should().Throw<InvalidOperationException>();
        a4.Should().Throw<InvalidOperationException>();
        a5.Should().Throw<InvalidOperationException>();
        a6.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public async Task UnifiedValidatorFactory_ShouldCoverFieldAndCrossRules()
    {
        var schema = new FormSchema(
            "reg",
            new List<FieldDescriptor>
            {
                new("Email", FieldTypeEnum.Email, Required: true, Min: 5, Max: 100, Regex: @"^[^@]+@[^@]+\.[^@]+$"),
                new("Phone", FieldTypeEnum.Phone, Required: false),
                new("Age", FieldTypeEnum.Int, Required: true),
                new("IsActive", FieldTypeEnum.Bool, Required: true),
                new("BirthDate", FieldTypeEnum.Date, Required: true),
                new("Password", FieldTypeEnum.Password, Required: true, Min: 4, Max: 20),
                new("ConfirmPassword", FieldTypeEnum.Password, Required: false)
            },
            new List<IFormSchemaRule>
            {
                new EqualFieldsRule("Password", "ConfirmPassword", "eq"),
                new NotEqualFieldsRule("Email", "Phone", "neq"),
                new OneOfRule("Age", new []{ "18", "30" }, "one"),
                new RequiredIfRule(("Email", "trigger@t.t"), ("Phone", true), "reqif"),
                new RequiredIfRule(("ConfirmPassword", ""), ("Phone", true), "reqif-empty"),
                new RequiredIfRule(("Phone", null), ("ConfirmPassword", true), "reqif-not-empty")
            });

        var validator = new UnifiedFormValidatorFactory().Create(schema);

        var bad = new Dictionary<string, object?>
        {
            ["Email"] = "bad",
            ["Phone"] = "bad-phone",
            ["Age"] = "17",
            ["IsActive"] = "not-bool",
            ["BirthDate"] = "bad-date",
            ["Password"] = "1",
            ["ConfirmPassword"] = "2"
        };

        var badResult = await validator.ValidateAsync(bad);
        badResult.IsValid.Should().BeFalse();
        badResult.Errors.Should().NotBeEmpty();

        var good = new Dictionary<string, object?>
        {
            ["Email"] = "trigger@t.t",
            ["Phone"] = "+12345678901",
            ["Age"] = "18",
            ["IsActive"] = "true",
            ["BirthDate"] = DateTime.UtcNow.ToString("O"),
            ["Password"] = "abcd",
            ["ConfirmPassword"] = "abcd"
        };

        var goodResult = await validator.ValidateAsync(good);
        goodResult.IsValid.Should().BeTrue();
    }

    [Test]
    public async Task UnifiedValidatorFactory_EmptyMap_ShouldTriggerRequired()
    {
        var schema = new FormSchema(
            "x",
            new List<FieldDescriptor> { new("A", FieldTypeEnum.String, Required: true) },
            new List<IFormSchemaRule>());

        var validator = new UnifiedFormValidatorFactory().Create(schema);
        var result = await validator.ValidateAsync(new Dictionary<string, object?>());

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "A");
    }

    private sealed class FakeFactory : IStepFactory
    {
        public FakeFactory(string kind) => Kind = kind;
        public string Kind { get; }
        public IStep Create(JsonElement cfg, IServiceProvider sp) => new FakeStep(Kind, cfg.StrOpt("next"));
    }

    private sealed class FakeStep : IStep
    {
        public FakeStep(string kind, string? next)
        {
            Kind = kind;
            Next = next;
        }

        public string Kind { get; init; }
        public string? Next { get; init; }
        public ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken) => ValueTask.FromResult(StepResult.Ok(Next));
    }
}
