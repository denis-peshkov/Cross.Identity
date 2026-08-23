namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Step for authenticating a user (by password or code)
/// and issuing a JWT token pair (access + refresh).
/// </summary>
internal sealed class TokenStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    /// <summary>Identity selector (bag keys for field name + value).</summary>
    public required Selector Selector { get; init; }

    /// <summary>Key in <see cref="Bag"/> to read the password from. May be relative or absolute.</summary>
    public string? PasswordKey { get; init; }

    /// <summary>Key in <see cref="Bag"/> to read the code from. May be relative or absolute.</summary>
    public string? CodeKey { get; init; }

    /// <summary>Step logger.</summary>
    public ILogger Logger { get; set; }

    /// <summary>Token issuance service.</summary>
    public IJwtTokenService JwtTokenService { get; set; }

    /// <summary>Service for validating credentials and reading the user.</summary>
    public IUserService UserService { get; set; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {

        var selector = Selector.Resolve(ctx);

        // 1) email/login + password or code (absolute keys such as collectForm.Email are not prefixed with the token step Kind)
        var passwordValue = PasswordKey is null ? null : ctx.Get<string?>(BagKey.Qualify(Kind, PasswordKey));
        var codeValue = CodeKey is null ? null : ctx.Get<string?>(BagKey.Qualify(Kind, CodeKey));

        // 2) validation: when PasswordKey is absent from JSON, "" must not be treated as "password provided" — otherwise the code branch does not run.
        var validated = false;
        if (PasswordKey != null && !string.IsNullOrEmpty(passwordValue))
        {
            validated = await UserService.ValidatePasswordAsync(selector.Field, selector.Value, passwordValue, cancellationToken).ConfigureAwait(false);
        }
        else if (CodeKey != null && !string.IsNullOrEmpty(codeValue))
        {
            validated = await UserService.ValidateCodeAsync(selector.Field, selector.Value, codeValue, cancellationToken).ConfigureAwait(false);
        }
        if (!validated)
        {
            if (CodeKey != null && !string.IsNullOrEmpty(codeValue))
                return StepResult.Fail(new NotAuthorizedException("Invalid or expired verification code."));

            return StepResult.Fail(new NotAuthorizedException("Invalid credentials."));
        }

        // 3) get user data
        var userAccount = await UserService.GetUserByAsync(selector.Field, selector.Value, cancellationToken).ConfigureAwait(false);
        ArgumentNullException.ThrowIfNull(userAccount);

        var hostSuppliedClientContext = HostSuppliedClientContext.Read(ctx);
        await TokenPairIssuer
            .IssueTokenPairAsync(JwtTokenService, ctx, Kind, userAccount, Guid.NewGuid(), hostSuppliedClientContext, cancellationToken)
            .ConfigureAwait(false);

        return StepResult.Ok(Next);
    }
}
