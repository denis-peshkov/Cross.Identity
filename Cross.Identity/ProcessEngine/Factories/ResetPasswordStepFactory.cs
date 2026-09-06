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
        var loggerFactory             = sp.GetRequiredService<ILoggerFactory>();
        var userService               = sp.GetRequiredService<IUserService>();
        var emailSenderService        = sp.GetRequiredService<IEmailSenderService>();
        var smsSenderService          = sp.GetRequiredService<ISmsSenderService>();
        var processDefinitionProvider = sp.GetRequiredService<IProcessDefinitionProvider>();

        return new ResetPasswordStep
        {
            Kind                       = Kind,
            Selector                   = new Selector(),
            PasswordKey                = cfg.Str("passwordKey"),
            Template                   = cfg.Str("template"),
            Subject                    = cfg.Str("subject"),
            UserService                = userService,
            EmailSenderService         = emailSenderService,
            SmsSenderService           = smsSenderService,
            CommunicationEndpoints     = sp.GetRequiredService<ICommunicationEndpointService>(),
            ProcessDefinitionProvider  = processDefinitionProvider,
            Logger                     = loggerFactory.CreateLogger<ResetPasswordStep>(),
            Next                       = cfg.StrOpt("next"),
        };
    }
}
