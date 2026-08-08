namespace Cross.Identity.Tests.Identity.StepTests;

[TestFixture]
public class CollectForm_StepTests
{
    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenValidE164Phone_WhenExecuteAsync_ThenStoresPhoneViaPhoneE164Async()
    {
        const string phone = "+79161234567";
        var schema = new FormSchema("collectForm", new[]
        {
            new FieldDescriptor("PhoneNumber", FieldTypeEnum.PhoneNumber, Required: false, Max: 16),
        });
        var step = new CollectFormStep
        {
            Kind = "collectForm",
            Next = null,
            Schema = schema,
            Validator = new UnifiedFormValidatorFactory().Create(schema),
            FetchIncoming = _ => Task.FromResult<IDictionary<string, object?>>(
                new Dictionary<string, object?> { ["PhoneNumber"] = phone }),
        };

        var bag = new Bag();
        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        bag.Get<string>("collectForm.PhoneNumber").Should().Be(phone);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenInvalidPhone_WhenExecuteAsync_ThenFailsValidationAsync()
    {
        var schema = new FormSchema("collectForm", new[]
        {
            new FieldDescriptor("PhoneNumber", FieldTypeEnum.PhoneNumber, Required: true, Max: 16),
        });
        var step = new CollectFormStep
        {
            Kind = "collectForm",
            Next = null,
            Schema = schema,
            Validator = new UnifiedFormValidatorFactory().Create(schema),
            FetchIncoming = _ => Task.FromResult<IDictionary<string, object?>>(
                new Dictionary<string, object?> { ["PhoneNumber"] = "89161234567" }),
        };

        var result = await step.ExecuteAsync(new Bag(), CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Fail);
        result.Error.Should().BeOfType<ValidationException>();
    }
}
