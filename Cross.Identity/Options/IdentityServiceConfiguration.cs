namespace Cross.Identity.Options;

/// <summary>
/// Configuration options for Cross.Identity registration.
/// </summary>
public sealed class IdentityServiceConfiguration
{
    /// <summary>
    /// Configuration section name for binding Cross.Identity options.
    /// </summary>
    public const string SectionName = "CrossIdentity";

    /// <summary>
    /// License key for Peshkov software Cross.Identity.
    /// </summary>
    public string? LicenseKey { get; set; }
}
