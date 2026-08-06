namespace Cross.Identity.Tests.Core;

[TestFixture]
public sealed class BagAndMapTests
{
    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenBagWithValues_WhenGetTrySetAndReadOnlyMembers_ThenWorks()
    {
        var bag = new Bag()
            .Set("a", 1)
            .Set("b", "2")
            .Set("n", null);

        bag.Get<int>("a").Should().Be(1);
        bag.Get<int>("b").Should().Be(2);
        bag.Get<string?>("n").Should().BeNull();
        bag.Has("a").Should().BeTrue();
        bag.ContainsKey("b").Should().BeTrue();
        bag.Count.Should().Be(3);
        bag.Keys.Should().Contain(new[] { "a", "b", "n" });

        bag.TryGet<int>("b", out var parsed).Should().BeTrue();
        parsed.Should().Be(2);

        bag.TryGet<Guid>("b", out _).Should().BeFalse();
        bag.TryGetValue("a", out var boxed).Should().BeTrue();
        boxed.Should().Be(1);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenMissingOrInvalidKey_WhenGet_ThenThrows()
    {
        var bag = new Bag().Set("x", "abc").Set("n", null);

        var miss = () => bag.Get<int>("missing");
        miss.Should().Throw<KeyNotFoundException>();

        var badCast = () => bag.Get<int>("x");
        badCast.Should().Throw<InvalidCastException>();

        var nullCast = () => bag.Get<int>("n");
        nullCast.Should().Throw<InvalidCastException>();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenBagWithValues_WhenToDictionaryAndAsEnumerable_ThenReturnsData()
    {
        var bag = new Bag().Set("k1", "v1").Set("k2", 2);
        var dict = bag.ToDictionary();
        var list = bag.AsEnumerable().ToList();

        dict["k1"].Should().Be("v1");
        list.Should().Contain(x => x.Key == "k2" && (int)x.Value! == 2);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenRelativeAndAbsoluteKeys_WhenQualify_ThenHandlesBoth()
    {
        BagKey.Qualify("step", "email").Should().Be("step.email");
        BagKey.Qualify("step", "collect.email").Should().Be("collect.email");

        var noStep = () => BagKey.Qualify("", "a");
        noStep.Should().Throw<ArgumentException>();

        var noKey = () => BagKey.Qualify("s", "");
        noKey.Should().Throw<ArgumentException>();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenChannelAndJson_WhenResolveByMethods_ThenWork()
    {
        ResolveBy.DefaultFor(ChannelEnum.Email).Field.Should().Be("Email");
        ResolveBy.DefaultFor(ChannelEnum.Sms).Field.Should().Be("PhoneNumber");
        ResolveBy.DefaultFor(ChannelEnum.Telegram).Field.Should().Be("PhoneNumber");
        ResolveBy.DefaultFor((ChannelEnum)999).Field.Should().Be("UserName");

        using var json = JsonDocument.Parse("""{"field":"Email","required":false,"caseInsensitive":false}""");
        var parsed = ResolveBy.FromJson(json.RootElement);
        parsed.Field.Should().Be("Email");
        parsed.Required.Should().BeFalse();
        parsed.CaseInsensitive.Should().BeFalse();

        using var jsonDefaults = JsonDocument.Parse("""{"field":"UserName"}""");
        var defaults = ResolveBy.FromJson(jsonDefaults.RootElement);
        defaults.Required.Should().BeTrue();
        defaults.CaseInsensitive.Should().BeTrue();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenSampleMap_WhenToBagAndFromBag_ThenMapsSimpleTypesAndAttributes()
    {
        var now = DateTime.UtcNow;
        var dto = new SampleMap
        {
            Name = "John",
            Age = 33,
            State = SampleState.Active,
            Id = Guid.NewGuid(),
            CreatedAt = now,
            SkipMe = "hidden",
            NullableNumber = null
        };

        var bag = dto.ToBag(includeNulls: true, enumAsString: true);
        bag.Should().ContainKey("full_name");
        bag["full_name"].Should().Be("John");
        bag["State"].Should().Be("Active");
        bag.Should().NotContainKey("SkipMe");
        bag.Should().ContainKey("NullableNumber");

        var source = new Dictionary<string, object?>
        {
            ["full_name"] = "Alice",
            ["Age"] = "40",
            ["State"] = "Disabled",
            ["Id"] = dto.Id.ToString(),
            ["CreatedAt"] = now.ToString("O", CultureInfo.InvariantCulture),
            ["NullableNumber"] = "123",
            ["SkipMe"] = "ignored"
        };

        var mapped = source.FromBag<SampleMap>(enumFromString: true);
        mapped.Name.Should().Be("Alice");
        mapped.Age.Should().Be(40);
        mapped.State.Should().Be(SampleState.Disabled);
        mapped.Id.Should().Be(dto.Id);
        mapped.CreatedAt.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
        mapped.NullableNumber.Should().Be(123);
        mapped.SkipMe.Should().BeNull();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenNullAndEnumAsNumber_WhenFromBag_ThenWorks()
    {
        var src = new Dictionary<string, object?>
        {
            ["full_name"] = null,
            ["Age"] = 1L,
            ["State"] = 1,
            ["Id"] = Guid.Empty,
            ["CreatedAt"] = DateTime.UtcNow
        };

        var mapped = src.FromBag<SampleMap>(enumFromString: false);
        mapped.Name.Should().BeNull();
        mapped.Age.Should().Be(1);
        mapped.State.Should().Be(SampleState.Disabled);
    }

    private enum SampleState
    {
        Active = 0,
        Disabled = 1
    }

    private sealed class SampleMap
    {
        [JsonPropertyName("full_name")]
        public string? Name { get; set; }
        public int Age { get; set; }
        public SampleState State { get; set; }
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? NullableNumber { get; set; }
        [JsonIgnore]
        public string? SkipMe { get; set; }
    }
}
