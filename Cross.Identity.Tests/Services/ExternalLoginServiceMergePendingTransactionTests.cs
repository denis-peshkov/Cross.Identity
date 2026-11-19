namespace Cross.Identity.Tests.Services;

/// <summary>
/// Relational regression: link-conflict pending mint must not enlist in the host's outer transaction.
/// Uses Testcontainers PostgreSQL + repo DDL (EF EnsureCreated is T-SQL-filter incompatible).
/// </summary>
[TestFixture]
[Category(TestCategory.INTEGRATION)]
public sealed class ExternalLoginServiceMergePendingTransactionTests
{
    private const string CallbackUrl = "https://app.example/auth/external-login-callback";
    private const string FormCallbackUrl = "https://api.example/api/v1/Identity/ExternalLogin/OAuthReturn";

    private static readonly string ScriptsRoot = Path.GetFullPath(Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "..", "..", "..", "..",
        "Infrastructure", "Scripts", "PostgreSQL", "2_Initial"));

    private Testcontainers.PostgreSql.PostgreSqlContainer _postgres = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        _postgres = new Testcontainers.PostgreSql.PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .Build();
        await _postgres.StartAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDownAsync()
    {
        if (_postgres is not null)
        {
            await _postgres.DisposeAsync();
        }
    }

    [Test]
    public async Task GivenOuterTransaction_WhenCompleteAsyncLinkConflict_ThenPendingSurvivesRollbackAsync()
    {
        var connectionString = _postgres.GetConnectionString();
        var factory = new NpgsqlIdentityContextFactory(connectionString);

        await ApplyPostgresSchemaAsync(connectionString);

        var currentUserAccountId = Guid.NewGuid();
        var otherUserAccountId = Guid.NewGuid();
        string state;

        await using (var setup = factory.CreateDbContext())
        {
            setup.UsersAccounts.AddRange(
                NewUser(currentUserAccountId, "current@example.com", "current"),
                NewUser(otherUserAccountId, "other@example.com", "other"));

            setup.Providers.Add(new ProviderEntity
            {
                Id = 1,
                Name = "Google",
                Scheme = "google",
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow,
            });
            await setup.SaveChangesAsync();

            setup.UsersExternalLogins.Add(new UserExternalLoginEntity
            {
                Id = Guid.NewGuid(),
                UserAccountId = otherUserAccountId,
                UserAccount = null!,
                ProviderId = 1,
                ProviderEntity = null!,
                ProviderUserId = "google-sub-1",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ConcurrencyStamp = Guid.NewGuid(),
            });

            var payload = new ExternalLoginStatePayload
            {
                Nonce = Guid.NewGuid().ToString("N"),
                Provider = "Google",
                ReturnUrl = "/account/ExternalLogins",
                UserAccountId = currentUserAccountId,
            };
            state = EncodeState(payload);

            var now = DateTime.UtcNow;
            setup.ExternalLoginStates.Add(new ExternalLoginStateEntity
            {
                Id = Guid.NewGuid(),
                Nonce = payload.Nonce,
                Provider = payload.Provider,
                ReturnUrl = payload.ReturnUrl,
                UserAccountId = payload.UserAccountId,
                CreatedAt = now,
                ExpiresAt = now.AddMinutes(10),
                ConcurrencyStamp = Guid.NewGuid(),
            });
            await setup.SaveChangesAsync();
        }

        string? mergeToken;

        // Ambient host TransactionScope: scoped connection enlists; mint must use a fresh factory connection under Suppress.
        using (var ambient = new TransactionScope(
                   TransactionScopeOption.Required,
                   TransactionScopeAsyncFlowOption.Enabled))
        {
            await using var scopedContext = factory.CreateDbContext();
            _ = await scopedContext.UsersAccounts.CountAsync();

            var sut = CreateService(scopedContext, factory);
            var ex = await FluentActions
                .Invoking(() => sut.CompleteAsync("auth-code", state, null, null, CancellationToken.None))
                .Should()
                .ThrowAsync<ConflictException>()
                .WithMessage("*already linked to another user*");

            AccountMergeRequired.TryGetMergeToken(ex.Which, out mergeToken).Should().BeTrue();
            mergeToken.Should().NotBeNullOrWhiteSpace();

            // Do not Complete ambient — dispose rolls back enlisted work.
        }

        await using var verify = factory.CreateDbContext();
        (await verify.AccountMergePendings.CountAsync(x => x.MergeToken == mergeToken && x.ConsumedAt == null))
            .Should()
            .Be(1);
    }

    private static async Task ApplyPostgresSchemaAsync(string connectionString)
    {
        var scripts = new[]
        {
            "2_00_auth.sql",
            "2_01_auth_UsersAccounts.sql",
            "2_01_auth_Providers.sql",
            "2_01_auth_UsersExternalLogins.sql",
            "2_01_auth_ExternalLoginStates.sql",
            "2_02_auth_UsersExternalLogins.sql",
            "2_02_auth_ExternalLoginStates.sql",
            "2_03_auth_AccountMergePendings.sql",
        };

        await using var connection = new Npgsql.NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        foreach (var scriptName in scripts)
        {
            var path = Path.Combine(ScriptsRoot, scriptName);
            File.Exists(path).Should().BeTrue($"missing DDL script: {path}");
            var sql = await File.ReadAllTextAsync(path);
            await using var command = new Npgsql.NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }
    }

    private static ExternalLoginService CreateService(
        IdentityContext context,
        IDbContextFactory<IdentityContext> factory)
    {
        var options = new ExternalLoginOptions
        {
            CallbackUrl = CallbackUrl,
            FormCallbackUrl = FormCallbackUrl,
            StateLifetime = TimeSpan.FromMinutes(10),
            Providers =
            {
                ["Google"] = new ExternalLoginProviderOptions
                {
                    ClientId = "google-client",
                    ClientSecret = "google-secret",
                },
            },
        };

        var optionsMock = new Mock<IOptionsSnapshot<ExternalLoginOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory
            .Setup(f => f.CreateClient(nameof(ExternalLoginService)))
            .Returns(new HttpClient(GoogleSuccessHandler()));

        var audit = new AuditService(context);

        return new ExternalLoginService(
            context,
            factory,
            httpClientFactory.Object,
            optionsMock.Object,
            Mock.Of<ILogger<ExternalLoginService>>(),
            Mock.Of<IJwtTokenService>(),
            new CommunicationEndpointService(context, audit, TestAuthOptions.Snapshot()),
            audit);
    }

    private static UserAccountEntity NewUser(Guid id, string email, string userName)
        => new()
        {
            Id = id,
            Email = email,
            UserName = userName,
            NormalizedUserName = userName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
        };

    private static OAuthTestHttpHandler GoogleSuccessHandler()
        => new(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>
        {
            ["https://oauth2.googleapis.com/token"] = _ =>
                OAuthTestHttpHandler.JsonResponse(HttpStatusCode.OK, new { access_token = "google-token" }),
            ["https://openidconnect.googleapis.com/v1/userinfo"] = _ =>
                OAuthTestHttpHandler.JsonResponse(HttpStatusCode.OK, new
                {
                    sub = "google-sub-1",
                    email = "user@example.com",
                    email_verified = true,
                    name = "Google User",
                    picture = "https://example.com/avatar.png",
                }),
        });

    private static string EncodeState(ExternalLoginStatePayload payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes(json);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private sealed class NpgsqlIdentityContextFactory : IDbContextFactory<IdentityContext>
    {
        private readonly string _connectionString;

        public NpgsqlIdentityContextFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IdentityContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<IdentityContext>()
                .UseNpgsql(_connectionString)
                .Options;
            return new IdentityContext(options);
        }
    }
}
