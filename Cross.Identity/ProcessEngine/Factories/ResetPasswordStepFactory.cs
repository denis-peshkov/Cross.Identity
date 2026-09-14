namespace Cross.Identity.ProcessEngine.Factories;

/// <summary>
/// Factory for <see cref="ResetPasswordStep"/>.
/// </summary>
internal sealed class ResetPasswordStepFactory : IStepFactory
{
    /// <inheritdoc />
    public string Kind => ((IStepFactory)this).GetKind;

    /// <inheritdoc />
    public IStep Create(JsonElement cfg, IServiceProvider sp)
    {
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
        var userService   = sp.GetRequiredService<IUserService>();

        return new ResetPasswordStep
        {
            Kind                   = Kind,
            Selector               = new Selector(),
            PasswordKey            = cfg.Str("passwordKey"),
            Template               = cfg.Str("template"),
            Subject                = cfg.Str("subject"),
            UserService            = userService,
            CommunicationEndpoints = sp.GetRequiredService<ICommunicationEndpointService>(),
            NotificationComposer   = sp.GetRequiredService<INotificationComposer>(),
            SecurityNotifier       = sp.GetRequiredService<ISecurityNotifier>(),
            Logger                 = loggerFactory.CreateLogger<ResetPasswordStep>(),
            Next                   = cfg.StrOpt("next"),
        };
    }
}
