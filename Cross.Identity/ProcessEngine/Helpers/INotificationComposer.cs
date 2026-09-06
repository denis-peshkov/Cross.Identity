namespace Cross.Identity.ProcessEngine.Helpers;

/// <summary>
/// Plain-text and HTML bodies after template load and placeholder substitution.
/// </summary>
/// <param name="TextBody">Rendered <c>txt</c> template.</param>
/// <param name="HtmlBody">Rendered <c>html</c> template.</param>
internal readonly record struct ComposedNotificationBodies(string TextBody, string HtmlBody);

/// <summary>
/// Loads localized notification templates and applies branding + caller placeholders.
/// </summary>
internal interface INotificationComposer
{
    /// <summary>
    /// Loads <paramref name="templateName"/> (<c>txt</c>/<c>html</c>) using
    /// <see cref="HostSuppliedLanguageContext"/> from <paramref name="bag"/>,
    /// applies <see cref="NotificationOptions"/> branding, then
    /// <paramref name="placeholders"/> (keys must include braces, e.g. <c>{{code}}</c>).
    /// </summary>
    ComposedNotificationBodies Compose(
        Bag bag,
        string templateName,
        IReadOnlyDictionary<string, string>? placeholders = null);
}
