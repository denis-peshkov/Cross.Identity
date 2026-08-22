namespace Cross.Identity.Tests.Identity.StepFactoryTests;

[TestFixture]
public class CollectForm_StepFactoryTests
{
    private ServiceProvider _sp = null!;
    private Faker _faker = null!;

    [SetUp]
    public void SetUp()
    {
        var sc = new ServiceCollection();

        // Real form validator
        sc.AddSingleton<IFormValidatorFactory, UnifiedFormValidatorFactory>();

        // Fake IRequestInput — we will manually Set(...) before step execution
        sc.AddScoped<IRequestInput, RequestInput>();

        sc.AddSingleton<ICommunicationEndpointService>(_ => Mock.Of<ICommunicationEndpointService>());
        _sp = sc.BuildServiceProvider();
        _faker = new Faker();
    }

    [TearDown]
    public void TearDown() => _sp.Dispose();

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenSchemaDefAndPatch_WhenCreateAndExecuteAsync_ThenAppliesPatchAndValidatesAsync()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "collectForm",
              "schemaDef": {
                "fields": [
                  { "key": "Email", "type": "Email", "required": true },
                  { "key": "Password", "type": "Password", "required": true, "min": 8, "max": 32 },
                  { "key": "Legacy", "type": "String", "required": false }
                ]
              },
              "schemaPatch": {
                "add": [
                  { "key": "OtpCode", "type": "String", "required": true, "min": 4, "max": 8 }
                ],
                "override": [
                  { "key": "Password", "min": 12 }
                ],
                "remove": [ "Legacy" ],
                "rename": [ { "from": "Email", "to": "Login" } ]
              },
              "next": "next-step"
            }
            """);

        var cfg = json.RootElement;
        var factory = new CollectFormStepFactory();

        // act
        var step = (CollectFormStep)factory.Create(cfg, _sp);

        // assert: check fields only (schema no longer has a name)
        var keys = step.Schema.Fields.Select(f => f.Key).ToArray();
        keys.Should().Contain(new[] { "Login", "Password", "OtpCode" });
        keys.Should().NotContain("Email");
        keys.Should().NotContain("Legacy");

        var pwd = step.Schema.Fields.First(f => f.Key == "Password");
        pwd.Min.Should().Be(12);

        // input data
        var input = _sp.GetRequiredService<IRequestInput>();
        input.Set(new Dictionary<string, object?>
        {
            ["Login"]    = _faker.Internet.Email(),
            ["Password"] = "P@ssw0rd_long", // >=12
            ["OtpCode"]  = "123456"
        });

        // run
        var bag = new Bag();
        var res = await step.ExecuteAsync(bag, CancellationToken.None);

        res.Status.Should().Be(StepStatusEnum.Ok);
        res.Next.Should().Be("next-step");

        // prefix — step kind: "collectForm"
        bag.Get<string>("collectForm.Login").Should().NotBeNullOrEmpty();
        bag.Get<string>("collectForm.OtpCode").Should().Be("123456");
        bag.Get<string>("collectForm.Password").Should().Be("P@ssw0rd_long");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenRequiredFieldMissing_WhenExecuteAsync_ThenReturnsValidationFailureAsync()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "collectForm",
              "name": "reg",
              "schemaDef": {
                "name": "registration",
                "fields": [
                  { "key": "Email", "type": "Email", "required": true },
                  { "key": "UserName", "type": "String", "required": true, "min": 3 }
                ]
              },
              "next": "null"
            }
            """);

        var step = (CollectFormStep)new CollectFormStepFactory().Create(json.RootElement, _sp);

        // input: omit UserName
        var input = _sp.GetRequiredService<IRequestInput>();
        input.Set(new Dictionary<string, object?> { ["Email"] = "user@example.com" });

        var bag = new Bag();

        var res = await step.ExecuteAsync(bag, CancellationToken.None);

        res.Status.Should().Be(StepStatusEnum.Fail);
        res.Error.Should().BeOfType<ValidationException>();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenMismatchedPasswords_WhenExecuteAsync_ThenReturnsValidationFailureAsync()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "collectForm",
              "schemaDef": {
                "fields": [
                  { "key": "Email", "type": "Email", "required": true },
                  { "key": "Password", "type": "Password", "required": true, "min": 8, "max": 32 },
                  { "key": "ConfirmPassword", "type": "Password", "required": true, "min": 8, "max": 32 }
                ],
                "validators": [
                  { "kind": "equal", "left": "Password", "right": "ConfirmPassword", "message": "Passwords do not match." }
                ]
              },
              "next": null
            }
            """);

        var step = (CollectFormStep)new CollectFormStepFactory().Create(json.RootElement, _sp);

        // input: omit UserName
        var input = _sp.GetRequiredService<IRequestInput>();
        input.Set(new Dictionary<string, object?>
        {
            ["Email"] = "user@example.com",
            ["Password"] = "ghdsd1234",
            ["ConfirmPassword"] = "ghdsd1234-", // do not match
        });

        var bag = new Bag();

        var res = await step.ExecuteAsync(bag, CancellationToken.None);

        res.Status.Should().Be(StepStatusEnum.Fail);
        res.Error.Should().BeOfType<ValidationException>();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenKindMismatch_WhenCreate_ThenThrowsInvalidOperationException()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "otherKind",
              "schemaDef": {
                "fields": [ { "key": "Email", "type": "Email", "required": true } ]
              }
            }
            """);

        var act = () => new CollectFormStepFactory().Create(json.RootElement, _sp);
        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenSchemaNameWithoutProvider_WhenCreate_ThenThrowsInvalidOperationException()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "collectForm",
              "schema": "registration",
              "next": "n"
            }
            """);

        var act = () => new CollectFormStepFactory().Create(json.RootElement, _sp);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*IFormSchemaProvider is not registered*");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenSchemaDefWithoutFields_WhenCreate_ThenThrowsInvalidOperationException()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "collectForm",
              "schemaDef": { "validators": [] },
              "next": "n"
            }
            """);

        var act = () => new CollectFormStepFactory().Create(json.RootElement, _sp);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*requires 'fields' array*");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenUnknownFieldType_WhenCreate_ThenThrowsArgumentOutOfRangeException()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "collectForm",
              "schemaDef": {
                "fields": [ { "key": "X", "type": "UnknownType", "required": false } ]
              },
              "next": "n"
            }
            """);

        var act = () => new CollectFormStepFactory().Create(json.RootElement, _sp);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
