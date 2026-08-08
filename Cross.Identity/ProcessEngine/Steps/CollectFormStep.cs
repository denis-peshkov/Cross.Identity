namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Form data collection step.
/// <list type="bullet">
/// <item>Reads incoming data from <see cref="FetchIncoming"/> (usually <see cref="IRequestInput.GetAsync"/>).</item>
/// <item>Validates it via <see cref="Validator"/> (FluentValidation).</item>
/// <item>Stores values in <see cref="Bag"/> with the <b>step name</b> prefix: <c>"{Name}.{FieldKey}"</c>.</item>
/// <item>Fields of type <c>PhoneNumber</c> are stored via <see cref="PhoneE164.Require"/> (E.164 only).</item>
/// </list>
/// The form schema is defined in the step configuration (see the factory).
/// </summary>
internal sealed class CollectFormStep : IStep
{
    /// <inheritdoc />
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    /// <summary>Form schema.</summary>
    public required FormSchema Schema { get; init; }

    /// <summary>Form validator (FluentValidation), built by the factory.</summary>
    public required IValidator<IDictionary<string, object?>> Validator { get; init; }

    /// <summary>Function that fetches incoming data (usually from <see cref="IRequestInput"/>).</summary>
    public required Func<CancellationToken, Task<IDictionary<string, object?>>> FetchIncoming { get; init; }

    /// <inheritdoc />
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        // 1) incoming data
        var data = await FetchIncoming(cancellationToken).ConfigureAwait(false);

        // 2) validation
        var res = await Validator.ValidateAsync(data, cancellationToken).ConfigureAwait(false);
        if (!res.IsValid)
            return StepResult.Fail(new ValidationException(res.Errors));

        // 3) write to Bag with the step name prefix; PhoneNumber → PhoneE164
        var phoneNumberKeys = new HashSet<string>(
            Schema.Fields.Where(f => f.Type == FieldTypeEnum.PhoneNumber).Select(f => f.Key),
            StringComparer.Ordinal);

        foreach (var (k, v) in data)
        {
            var bagKey = BagKey.Qualify(Kind, k); // "{Kind}.{k}" when the key is relative
            var value = v;
            if (phoneNumberKeys.Contains(k) && v is string phoneNumber && !string.IsNullOrWhiteSpace(phoneNumber))
            {
                value = PhoneE164.Require(phoneNumber);
            }

            ctx.Set(bagKey, value);
        }

        return StepResult.Ok(Next);
    }
}
