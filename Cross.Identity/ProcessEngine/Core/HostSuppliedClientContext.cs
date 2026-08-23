namespace Cross.Identity.ProcessEngine.Core;

/// <summary>
/// Request metadata supplied by the host (Web API), carried through flows and service APIs.
/// </summary>
/// <remarks>
/// <para>
/// Cross.Identity is a library: it does not read <c>HttpContext</c>. The <b>host</b> builds
/// <c>collectForm.IpAddress</c>, <c>collectForm.UserAgent</c>, and <c>collectForm.DeviceFingerprint</c>
/// in the flow bag (or passes <c>new HostSuppliedClientContext(...)</c> into direct APIs) before calling the library.
/// Flow steps read them via <see cref="Read"/>.
/// </para>
/// <para>
/// <b>Trusted pipeline (host responsibility):</b> set metadata from server-side sources — for example
/// <c>RemoteIpAddress</c> after <c>ForwardedHeaders</c>, request <c>User-Agent</c>, host-computed
/// device fingerprint (cookie, validated SDK id, server session). Do <b>not</b> copy
/// <c>IpAddress</c> / <c>UserAgent</c> blindly from the client request body.
/// </para>
/// <para>
/// <b>Session binding (refresh):</b> non-empty values are stored as <c>Created*</c> on the refresh-token
/// family anchor at login. On rotation the library compares the current <see cref="HostSuppliedClientContext"/> with
/// that anchor. A dimension is checked only if it was captured at family start; a missing or different
/// value on refresh is a mismatch (<c>DEVICE_MISMATCH</c>, <c>IP_MISMATCH</c>,
/// <c>USER_AGENT_MISMATCH</c>, or <c>TOKEN_STOLEN</c>). Use the <b>same host-derived sources</b> on
/// login and every refresh.
/// </para>
/// <para>
/// When <c>Authentication:Jwt:SessionBindingCheckIp</c> is <c>true</c> and the family anchor captured session
/// metadata, refresh must <b>not</b> use <see cref="Empty"/> — pass <c>IpAddress</c>, <c>UserAgent</c>, and
/// <c>DeviceFingerprint</c> from the trusted pipeline (same as Token). Otherwise the library throws
/// <see cref="ValidationException"/> before comparing bindings (avoids accidental family revoke).
/// </para>
/// <para>
/// <see cref="Empty"/> is allowed on logout, password change, token revoke, and other non-rotation APIs when
/// the host has no metadata to pass.
/// </para>
/// <para>
/// The same values are written to token audit rows on issue/revoke and to notification text
/// (e.g. <c>ResetPasswordStep</c>). See <c>FLOWS.md</c> — Host-supplied client context.
/// </para>
/// </remarks>
public sealed record HostSuppliedClientContext(string? IpAddress, string? UserAgent, string? DeviceFingerprint)
{
    /// <summary>All fields <c>null</c> or whitespace — allowed on logout/revoke/password APIs; not on refresh when <c>SessionBindingCheckIp</c> is enabled and session binding was captured at login.</summary>
    public static HostSuppliedClientContext Empty { get; } = new(null, null, null);

    /// <summary><c>true</c> when <see cref="IpAddress"/>, <see cref="UserAgent"/>, and <see cref="DeviceFingerprint"/> are all null or whitespace.</summary>
    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(IpAddress)
        && string.IsNullOrWhiteSpace(UserAgent)
        && string.IsNullOrWhiteSpace(DeviceFingerprint);

    /// <summary>Bag prefix of collectForm (<c>CollectFormStepFactory.GetKind</c>).</summary>
    public const string CollectFormKind = "collectForm";

    /// <summary><c>collectForm.IpAddress</c> (max 64).</summary>
    public const string IpAddressField = "IpAddress";

    /// <summary><c>collectForm.UserAgent</c> (max 512).</summary>
    public const string UserAgentField = "UserAgent";

    /// <summary><c>collectForm.DeviceFingerprint</c> (max 128).</summary>
    public const string DeviceFingerprintField = "DeviceFingerprint";

    /// <summary>Reads host-supplied metadata from <c>collectForm.*</c> keys in the flow bag.</summary>
    public static HostSuppliedClientContext Read(Bag bag)
    {
        ArgumentNullException.ThrowIfNull(bag);

        return new HostSuppliedClientContext(
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
