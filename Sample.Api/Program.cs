var builder = WebApplication.CreateBuilder(args);

// DbContext for Cross.Identity (in-memory for the sample)
builder.Services.AddDbContext<IdentityContext>(options =>
    options
        .UseInMemoryDatabase("CrossIdentity")
        .AddInterceptors(new ConcurrencyStampInterceptor()));

// HttpContextAccessor for JwtTokenService
builder.Services.AddHttpContextAccessor();

// HeadersContextAccessor for UserService (language, region, etc.)
builder.Services.AddScoped<IHeadersContextAccessor, HeadersContextAccessor>();

// Pepper from appsettings (Sample.Api local dev — no AUTH_PEPPERS_JSON required)
builder.Services.AddPepperOptions<EnvProviderOptions, EnvProviderOptionsValidator>(builder.Configuration);
builder.Services.TryAddScoped<IPepperVaultProvider, EnvPepperProvider>();

// Register Cross.Identity
builder.Services.AddCrossIdentity(builder.Configuration);

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Minimal API endpoint equivalent to IdentityController.RunAsync
app.MapPost(
        "/api/identity/{flow}/{operation}",
        async (
            string flow,
            FlowOperationEnum operation,
            Dictionary<string, object?> body,
            IFlowExecutor flowExecutor,
            CancellationToken cancellation) =>
        {
            var input = FlowInputNormalizer.Normalize(body);
            var result = await flowExecutor.ExecuteAsync(input, flow, operation, cancellation).ConfigureAwait(false);
            return Results.Ok(result.Data);
        })
    .AllowAnonymous()
    .WithName("RunIdentityFlow")
    .WithOpenApi();

app.Run();
