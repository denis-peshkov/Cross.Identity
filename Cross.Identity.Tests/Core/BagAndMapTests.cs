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
    public void GivenGuidString_WhenGetGuid_ThenParses()
    {
        var id = Guid.NewGuid();
        var bag = new Bag().Set("id", id.ToString());

        bag.Get<Guid>("id").Should().Be(id);
        bag.Get<Guid?>("id").Should().Be(id);
        new Bag().Set("empty", "").Get<Guid?>("empty").Should().BeNull();
        new Bag().Set("empty", null).Get<Guid?>("empty").Should().BeNull();
        new Bag().Set("bad", "not-a-guid").Get<Guid?>("bad").Should().BeNull();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenNullableValueTypes_WhenGetOrTryGet_ThenConvertsViaUnderlyingType()
    {
        var ttl = TimeSpan.FromMinutes(17);
        var bag = new Bag()
            .Set("count", "42")
            .Set("countLong", 42L)
            .Set("ttl", ttl)
            .Set("empty", null);

        bag.Get<int?>("count").Should().Be(42);
        bag.Get<int?>("countLong").Should().Be(42);
        bag.Get<TimeSpan?>("ttl").Should().Be(ttl);
        bag.Get<int?>("empty").Should().BeNull();

        bag.TryGet<int?>("count", out var parsedCount).Should().BeTrue();
        parsedCount.Should().Be(42);

        bag.TryGet<TimeSpan?>("ttl", out var parsedTtl).Should().BeTrue();
        parsedTtl.Should().Be(ttl);

        bag.TryGet<int?>("empty", out var parsedNull).Should().BeTrue();
        parsedNull.Should().BeNull();

        bag.TryGet<int>("empty", out _).Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenMissingOrInvalidKey_WhenGet_ThenThrows()
    {
        var bag = new Bag().Set("x", "abc").Set("n", null);

        var miss = () => bag.Get<int>("missing");
        miss.Should().Throw<KeyNotFoundException>();

        var missRef = () => bag.Get<string?>("missing");
        missRef.Should().Throw<KeyNotFoundException>();

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
    public void GivenSelectorJson_WhenFromJson_ThenParsesCandidatesAndKeepsFixedDefaults()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "required": false,
              "caseInsensitive": false,
              "candidates": ["Email", "PhoneNumber"]
            }
            """);
        var parsed = Selector.FromJson(json.RootElement);
        parsed.FieldKey.Should().Be("collectForm.Field");
        parsed.ValueKey.Should().Be("collectForm.Value");
        // Required / CaseInsensitive are fixed on Selector (JSON flags are ignored).
        parsed.Required.Should().BeTrue();
        parsed.CaseInsensitive.Should().BeTrue();
        parsed.Candidates.Should().BeEquivalentTo("Email", "PhoneNumber");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenEmptySelectorJson_WhenFromJson_ThenUsesDefaults()
    {
        using var json = JsonDocument.Parse("{}");
        var parsed = Selector.FromJson(json.RootElement);
        parsed.FieldKey.Should().Be("collectForm.Field");
        parsed.ValueKey.Should().Be("collectForm.Value");
        parsed.Required.Should().BeTrue();
        parsed.CaseInsensitive.Should().BeTrue();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenBagWithSelectorValues_WhenResolve_ThenReturnsFieldAndValue()
    {
        var bag = new Bag()
            .Set("collectForm.Field", "Email")
            .Set("collectForm.Value", "test@example.com");
        var selector = new Selector();

        var (field, value) = selector.Resolve(bag);

        field.Should().Be("Email");
        value.Should().Be("test@example.com");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenCollectFormSelector_WhenBind_ThenWritesFieldAndValue()
    {
        var bag = new Bag().Set("collectForm.UserAccountId", "abc-123");
        var selector = new Selector
        {
            Candidates = new[] { "UserAccountId" },
        };

        selector.Bind(bag);

        bag.Get<string>("collectForm.Field").Should().Be("UserAccountId");
        bag.Get<string>("collectForm.Value").Should().Be("abc-123");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenCandidatesSelector_WhenBind_ThenUsesFirstNonEmpty()
    {
        var bag = new Bag().Set("collectForm.PhoneNumber", "+1234567890");
        var selector = new Selector
        {
            Candidates = new[] { "Email", "PhoneNumber", "UserName" },
        };

        selector.Bind(bag);

        bag.Get<string>("collectForm.Field").Should().Be("PhoneNumber");
        bag.Get<string>("collectForm.Value").Should().Be("+1234567890");
        Selector.ChannelForField(bag.Get<string>("collectForm.Field")).Should().Be(ChannelEnum.Sms);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenEmailPhoneAndUserName_WhenBind_ThenPrefersEmail()
    {
        var bag = new Bag()
            .Set("collectForm.Email", "a@b.co")
            .Set("collectForm.PhoneNumber", "+79161234567")
            .Set("collectForm.UserName", "alice");
        var selector = new Selector
        {
            Candidates = new[] { "Email", "PhoneNumber", "UserName" },
        };

        selector.Bind(bag);

        bag.Get<string>("collectForm.Field").Should().Be("Email");
        bag.Get<string>("collectForm.Value").Should().Be("a@b.co");
        Selector.ChannelForField(bag.Get<string>("collectForm.Field")).Should().Be(ChannelEnum.Email);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenOnlyUserName_WhenBind_ThenWritesUserNameWithoutChannel()
    {
        var bag = new Bag().Set("collectForm.UserName", "alice");
        var selector = new Selector
        {
            Candidates = new[] { "Email", "PhoneNumber", "UserName" },
        };

        selector.Bind(bag);

        bag.Get<string>("collectForm.Field").Should().Be("UserName");
        bag.Get<string>("collectForm.Value").Should().Be("alice");
        Selector.ChannelForField(bag.Get<string>("collectForm.Field")).Should().BeNull();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenNoCandidateValues_WhenBind_ThenThrowsValidationException()
    {
        var selector = new Selector
        {
            Candidates = new[] { "Email", "PhoneNumber", "UserName" },
        };

        FluentActions.Invoking(() => selector.Bind(new Bag()))
            .Should().Throw<ValidationException>()
            .WithMessage("*email, phone, or user name*");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenChannel_WhenDefaultFor_ThenMapsCandidates()
    {
        Selector.DefaultFor(ChannelEnum.Email).Candidates.Should().Equal("Email");
        Selector.DefaultFor(ChannelEnum.Sms).Candidates.Should().Equal("PhoneNumber");
        Selector.DefaultFor(ChannelEnum.Telegram).Candidates.Should().Equal("PhoneNumber");
        Selector.DefaultFor(ChannelEnum.Viber).Candidates.Should().Equal("PhoneNumber");
        Selector.DefaultFor(ChannelEnum.WhatsApp).Candidates.Should().Equal("PhoneNumber");
        Selector.DefaultFor((ChannelEnum)999).Candidates.Should().Equal("UserName");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenFieldName_WhenChannelForField_ThenMapsChannel()
    {
        Selector.ChannelForField("Email").Should().Be(ChannelEnum.Email);
        Selector.ChannelForField("PhoneNumber").Should().Be(ChannelEnum.Sms);
        Selector.ChannelForField("phone").Should().Be(ChannelEnum.Sms);
        Selector.ChannelForField("UserName").Should().BeNull();
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
