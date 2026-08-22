namespace Cross.Identity.ProcessEngine.Core;

/// <summary>
/// HTTP client metadata carried through flows and service APIs.
/// </summary>
/// <remarks>
/// <para><b>Trusted pipeline (host responsibility).</b> Cross.Identity is a library: it does not read
/// <c>HttpContext</c> and does not validate where <see cref="IpAddress"/>,
/// <see cref="UserAgent"/>, or <see cref="DeviceFingerprint"/> came from. The <b>host</b> must
/// implement a trusted pipeline — overwrite <c>collectForm.*</c> from server-side metadata
/// (for example <c>RemoteIpAddress</c>, request <c>User-Agent</c>, host-computed device fingerprint)
/// before <c>IFlowExecutor.ExecuteAsync</c>, and pass the same values into direct service APIs.
/// The library treats <see cref="ClientContext"/> as already trusted for audit and revoke metadata.</para>
/// <para>Flow steps read bag values via <see cref="Read"/> from <c>collectForm.IpAddress</c>,
/// <c>collectForm.UserAgent</c>, and <c>collectForm.DeviceFingerprint</c>.
/// See <c>FLOWS.md</c> — Client context (host).</para>
/// </remarks>
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
