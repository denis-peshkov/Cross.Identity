namespace Cross.Identity.UnitTests.Core;

[TestFixture]
public sealed class CoreInfrastructureBehaviorTests
{
    [Test]
    public void ProcessBuilder_Build_WithoutStart_ShouldThrow()
    {
        var sut = new ProcessBuilder();
        var act = () => sut.Build();
        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void ProcessBuilder_DuplicateKind_ShouldThrow()
    {
        var sut = new ProcessBuilder()
            .StartWith(new FakeStep("first", null))
            .Then(new FakeStep("second", null));

        var act = () => sut.Then(new FakeStep("SECOND", null));

        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public async Task ProcessBuilder_Build_AndRun_ShouldPassThroughSteps()
    {
        var sut = new ProcessBuilder()
            .StartWith(new FakeStep("start", "next"))
            .Then(new FakeStep("next", null));

        var process = sut.Build();
        var bag = new Bag();

        await process.RunAsync(bag, CancellationToken.None);

        bag.Get<string>("start.executed").Should().Be("yes");
        bag.Get<string>("next.executed").Should().Be("yes");
    }

    [Test]
    public void CompositeProvider_EmptyProviders_ShouldThrow()
    {
        var act = () => new CompositeProcessDefinitionProvider(Array.Empty<IProcessDefinitionProvider>());
        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void CompositeProvider_GetJson_ShouldFallbackAndCache()
    {
        var first = new CountingProvider(throwJson: true, throwTemplate: true);
        var second = new CountingProvider(json: "{\"ok\":1}", template: "tpl");
        var sut = new CompositeProcessDefinitionProvider(new IProcessDefinitionProvider[] { first, second });

        var v1 = sut.GetJson("License", FlowOperationEnum.Token);
        var v2 = sut.GetJson("License", FlowOperationEnum.Token);

        v1.Should().Be("{\"ok\":1}");
        v2.Should().Be("{\"ok\":1}");
        first.JsonCalls.Should().Be(1);
        second.JsonCalls.Should().Be(1);
    }

    [Test]
    public void CompositeProvider_GetTemplate_ShouldFallbackAndCache()
    {
        var first = new CountingProvider(throwJson: true, throwTemplate: true);
        var second = new CountingProvider(json: "{\"ok\":1}", template: "html");
        var sut = new CompositeProcessDefinitionProvider(new IProcessDefinitionProvider[] { first, second });

        var v1 = sut.GetTemplate("welcome", "en", "html");
        var v2 = sut.GetTemplate("welcome", "en", "html");

        v1.Should().Be("html");
        v2.Should().Be("html");
        first.TemplateCalls.Should().Be(1);
        second.TemplateCalls.Should().Be(1);
    }

    [Test]
    public void CompositeProvider_GetJson_WhenNotFound_ShouldThrow()
    {
        var sut = new CompositeProcessDefinitionProvider(new[] { new CountingProvider(throwJson: true, throwTemplate: true) });
        var act = () => sut.GetJson("none", FlowOperationEnum.Token);
        act.Should().Throw<KeyNotFoundException>();
    }

    [Test]
    public void InMemoryFormSchemaProvider_ShouldGetCaseInsensitive_AndThrowForMissing()
    {
        var schema = new FormSchema("Registration", new List<FieldDescriptor>());
        var sut = new InMemoryFormSchemaProvider(new[] { schema });

        sut.Get("registration").Should().BeSameAs(schema);
        var act = () => sut.Get("missing");
        act.Should().Throw<KeyNotFoundException>();
    }

    [Test]
    public void DbContextReadOnlyAdapter_Query_ShouldRespectTrackChangesFlag()
    {
        var options = new DbContextOptionsBuilder<IdentityContext>()
            .UseInMemoryDatabase($"read-adapter-{Guid.NewGuid()}")
            .Options;

        using var ctx = new IdentityContext(options);
        var id = Guid.NewGuid();
        ctx.UsersAccounts.Add(new UserAccountEntity { Id = id, NormalizedEmail = "u@a.b", CreatedAt = DateTime.UtcNow, SecurityStamp = Guid.NewGuid(), ConcurrencyStamp = Guid.NewGuid() });
        ctx.SaveChanges();
        ctx.ChangeTracker.Clear();

        var sut = new DbContextReadOnlyAdapter(ctx);

        var tracked = sut.Query<UserAccountEntity>(trackChanges: true).First(x => x.Id == id);
        ctx.Entry(tracked).State.Should().Be(EntityState.Unchanged);

        ctx.ChangeTracker.Clear();
        var notTracked = sut.Query<UserAccountEntity>(trackChanges: false).First(x => x.Id == id);
        ctx.Entry(notTracked).State.Should().Be(EntityState.Detached);
    }

    [Test]
    public void FileSystemProvider_MissingFlowDirectory_ShouldThrow()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new FileSystemProcessDefinitionOptions
        {
            Directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")),
            ReloadOnChange = false
        });

        var act = () => new FileSystemProcessDefinitionProvider(options);
        act.Should().Throw<DirectoryNotFoundException>();
    }

    [Test]
    public void FileSystemProvider_ShouldReadIndexedAndLazyFiles_ForFlowsAndTemplates()
    {
        var root = Path.Combine(Path.GetTempPath(), $"flows-{Guid.NewGuid():N}");
        var templates = Path.Combine(root, "Templates");
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(templates);

        try
        {
            var indexedFlowFile = Path.Combine(root, "license.token.json");
            File.WriteAllText(indexedFlowFile, "{\"flow\":\"indexed\"}");

            var indexedTemplateFile = Path.Combine(templates, "welcome.en.html");
            File.WriteAllText(indexedTemplateFile, "<h1>Hello</h1>");

            var options = Microsoft.Extensions.Options.Options.Create(new FileSystemProcessDefinitionOptions { Directory = root, ReloadOnChange = false });
            using var sut = new FileSystemProcessDefinitionProvider(options);

            var indexedFlow = sut.GetJson("license", FlowOperationEnum.Token);
            indexedFlow.Should().Contain("indexed");

            var indexedTemplate = sut.GetTemplate("welcome", "en", "html");
            indexedTemplate.Should().Contain("Hello");

            var lazyFlowFile = Path.Combine(root, "license.refreshtoken.json");
            File.WriteAllText(lazyFlowFile, "{\"flow\":\"lazy\"}");
            var lazyFlow = sut.GetJson("license", FlowOperationEnum.RefreshToken);
            lazyFlow.Should().Contain("lazy");

            var lazyTemplateFile = Path.Combine(templates, "reset.en.txt");
            File.WriteAllText(lazyTemplateFile, "reset-text");
            var lazyTemplate = sut.GetTemplate("reset", "en", "txt");
            lazyTemplate.Should().Be("reset-text");

            var missingFlow = () => sut.GetJson("none", FlowOperationEnum.Token);
            missingFlow.Should().Throw<KeyNotFoundException>();

            var missingTemplate = () => sut.GetTemplate("none", "en", "html");
            missingTemplate.Should().Throw<KeyNotFoundException>();
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Test]
    public void ServiceCollectionExtensions_AddIfNotNull_ShouldAddOnlyNonEmpty()
    {
        var claims = new List<Claim>();

        claims.AddIfNotNull("a", "x")
            .AddIfNotNull("b", "")
            .AddIfNotNull("c", "   ")
            .AddIfNotNull("d", null);

        claims.Should().ContainSingle(c => c.Type == "a" && c.Value == "x");
        claims.Should().HaveCount(1);
    }

    [Test]
    public void ServiceCollectionExtensions_AddFlowDefinitionsComposite_WithoutDescriptors_ShouldThrow()
    {
        var services = new ServiceCollection();
        var act = () => services.AddFlowDefinitionsComposite();
        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void ServiceCollectionExtensions_AddFlowDefinitionsComposite_ShouldRegisterComposite()
    {
        var services = new ServiceCollection();
        services.AddFlowDefinitionsComposite(
            ServiceDescriptor.Singleton<IProcessDefinitionProvider, StubProcessDefinitionProvider>(),
            ServiceDescriptor.Singleton<IProcessDefinitionProvider, StubProcessDefinitionProvider2>());

        using var sp = services.BuildServiceProvider();
        var providers = sp.GetServices<IProcessDefinitionProvider>().ToList();

        providers.Should().HaveCount(2);
        sp.GetRequiredService<CompositeProcessDefinitionProvider>();
    }

    [Test]
    public void ServiceCollectionExtensions_AddFlowDefinitionsCompositeFromDirectoryAndEmbedded_And_AddCrossIdentity_ShouldRegister()
    {
        var root = Path.Combine(Path.GetTempPath(), $"svc-flow-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "Templates"));

        try
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FileSystemProcessDefinition:Directory"] = root,
                    ["FileSystemProcessDefinition:ReloadOnChange"] = "false",
                    ["EmbeddedProcessDefinition:ResourceFolder"] = "none",
                    ["Authentication:Jwt:Key"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
                    ["Authentication:Jwt:EncryptionKey"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
                    ["Authentication:Jwt:Issuer"] = "issuer",
                    ["Authentication:Jwt:Audience"] = "aud"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddHttpContextAccessor();
            services.AddLogging();
            services.AddDbContext<IdentityContext>(o => o.UseInMemoryDatabase($"cross-identity-di-{Guid.NewGuid()}"));
            services.AddScoped<IHeadersContextAccessor>(_ => new HeadersContextAccessor());
            services.AddScoped<IPepperVaultProvider>(_ =>
            {
                var mock = new Mock<IPepperVaultProvider>();
                return mock.Object;
            });

            services.AddFlowDefinitionsCompositeFromDirectoryAndEmbedded(config);
            services.AddCrossIdentity(config);

            services.Any(x => x.ServiceType == typeof(CompositeProcessDefinitionProvider)).Should().BeTrue();
            services.Any(x => x.ServiceType == typeof(IJwtTokenService)).Should().BeTrue();
            services.Any(x => x.ServiceType == typeof(Microsoft.IdentityModel.Tokens.RsaSecurityKey)).Should().BeTrue();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void FileSystemProvider_PrivateHelpers_ShouldBeInvokable()
    {
        var resolve = typeof(FileSystemProcessDefinitionProvider).GetMethod(
            "ResolveTemplatesRoot",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        var flowRoot = Path.Combine(Path.GetTempPath(), $"resolve-{Guid.NewGuid():N}");
        var parentTemplates = Path.Combine(Directory.GetParent(flowRoot)!.FullName, "TemplatesX");
        Directory.CreateDirectory(flowRoot);
        Directory.CreateDirectory(parentTemplates);
        try
        {
            var resolved = (string)resolve.Invoke(null, new object[] { flowRoot, "TemplatesX" })!;
            resolved.Should().Be(parentTemplates);
        }
        finally
        {
            Directory.Delete(flowRoot, recursive: true);
            Directory.Delete(parentTemplates, recursive: true);
        }

        var isFlowAcceptable = typeof(FileSystemProcessDefinitionProvider).GetMethod("IsFlowAcceptable", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        ((bool)isFlowAcceptable.Invoke(null, new object?[] { "a.token.json" })!).Should().BeTrue();
        ((bool)isFlowAcceptable.Invoke(null, new object?[] { "a.txt" })!).Should().BeFalse();
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

        public ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
        {
            ctx.Set($"{Kind}.executed", "yes");
            return ValueTask.FromResult(StepResult.Ok(Next));
        }
    }

    private sealed class CountingProvider : IProcessDefinitionProvider
    {
        private readonly bool _throwJson;
        private readonly bool _throwTemplate;
        private readonly string _json;
        private readonly string _template;

        public CountingProvider(bool throwJson = false, bool throwTemplate = false, string json = "{}", string template = "t")
        {
            _throwJson = throwJson;
            _throwTemplate = throwTemplate;
            _json = json;
            _template = template;
        }

        public int JsonCalls { get; private set; }
        public int TemplateCalls { get; private set; }

        public string GetJson(string flow, FlowOperationEnum operation)
        {
            JsonCalls++;
            if (_throwJson)
            {
                throw new KeyNotFoundException();
            }

            return _json;
        }

        public string GetTemplate(string name, string languageCode, string format)
        {
            TemplateCalls++;
            if (_throwTemplate)
            {
                throw new KeyNotFoundException();
            }

            return _template;
        }
    }

    private sealed class StubProcessDefinitionProvider : IProcessDefinitionProvider
    {
        public string GetJson(string flow, FlowOperationEnum operation) => "{}";
        public string GetTemplate(string name, string languageCode, string format) => "t";
    }

    private sealed class StubProcessDefinitionProvider2 : IProcessDefinitionProvider
    {
        public string GetJson(string flow, FlowOperationEnum operation) => "{}";
        public string GetTemplate(string name, string languageCode, string format) => "t";
    }

}
