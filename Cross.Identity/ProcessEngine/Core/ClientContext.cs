namespace Cross.Identity.ProcessEngine.Core;

/// <summary>
/// Client metadata from the request, carried through flows and service APIs.
/// </summary>
/// <remarks>
/// <para>
/// Fields map to optional <c>collectForm</c> keys:
/// <see cref="IpAddressField"/>, <see cref="UserAgentField"/>, <see cref="DeviceFingerprintField"/>.
/// The host forwards what the <b>client sent</b> in the request body into the bag before
/// <c>IFlowExecutor.ExecuteAsync</c>. Flow steps read them via <see cref="Read"/>.
/// For direct JWT / password / unlink APIs pass <c>new ClientContext(...)</c> or <see cref="Empty"/>.
/// </para>
/// <para>
/// <b>Session binding (refresh):</b> non-empty values are stored as <c>Created*</c> on the refresh-token
/// family anchor when the session starts. On rotation, the library compares the current
/// <see cref="ClientContext"/> with that anchor. A dimension is checked only if it was captured
/// at family start; a missing or different value on refresh is a mismatch
/// (<c>DEVICE_MISMATCH</c>, <c>IP_MISMATCH</c>, <c>USER_AGENT_MISMATCH</c>, or <c>TOKEN_STOLEN</c>).
/// Forward the <b>same client fields</b> on login and every refresh.
/// </para>
/// <para>
/// The same values are written to token audit rows on issue and revoke. Cross.Identity does not read
/// <c>HttpContext</c> and does not substitute server IP or User-Agent — that is the host handler's job
/// when building <c>collectForm</c>.
/// See <c>FLOWS.md</c> — Client context (host).
/// </para>
/// </remarks>
public sealed record ClientContext(string? IpAddress, string? UserAgent, string? DeviceFingerprint)
{
    /// <summary>All fields <c>null</c> — use when the client sent no metadata.</summary>
    public static ClientContext Empty { get; } = new(null, null, null);

    /// <summary>Bag prefix of collectForm (<c>CollectFormStepFactory.GetKind</c>).</summary>
    public const string CollectFormKind = "collectForm";

    /// <summary><c>collectForm.IpAddress</c> (max 64).</summary>
    public const string IpAddressField = "IpAddress";

    /// <summary><c>collectForm.UserAgent</c> (max 512).</summary>
    public const string UserAgentField = "UserAgent";

    /// <summary><c>collectForm.DeviceFingerprint</c> (max 128).</summary>
    public const string DeviceFingerprintField = "DeviceFingerprint";

    /// <summary>Reads client metadata from <c>collectForm.*</c> keys in the flow bag.</summary>
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
