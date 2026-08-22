namespace Cross.Identity.ProcessEngine.Core;

/// <summary>
/// HTTP client metadata carried through flows and service APIs.
/// Flow steps read values via <see cref="Read"/> from <c>collectForm.IpAddress</c>,
/// <c>collectForm.UserAgent</c>, and <c>collectForm.DeviceFingerprint</c>.
/// The host must populate these from trusted server-side sources (for example
/// <c>HttpContext.Connection.RemoteIpAddress</c> and request headers), not from
/// unvalidated client request bodies. See <c>FLOWS.md</c> — Client context.
/// </summary>
public sealed record ClientContext(string? IpAddress, string? UserAgent, string? DeviceFingerprint)
{
    /// <summary>Empty client metadata (all fields null).</summary>
    public static ClientContext Empty { get; } = new(null, null, null);

    /// <summary>Bag prefix of collectForm (<c>CollectFormStepFactory.GetKind</c>).</summary>
    public const string CollectFormKind = "collectForm";

    public const string IpAddressField = "IpAddress";
    public const string UserAgentField = "UserAgent";
    public const string DeviceFingerprintField = "DeviceFingerprint";

    public static ClientContext Read(Bag bag)
    {
        ArgumentNullException.ThrowIfNull(bag);

        return new ClientContext(
            ReadField(bag, IpAddressField),
            ReadField(bag, UserAgentField),
            ReadField(bag, DeviceFingerprintField));
    }

    private static string? ReadField(Bag bag, string field)
    {
        var key = BagKey.Qualify(CollectFormKind, field);
        return bag.TryGet<string?>(key, out var value) ? value : null;
    }
}
