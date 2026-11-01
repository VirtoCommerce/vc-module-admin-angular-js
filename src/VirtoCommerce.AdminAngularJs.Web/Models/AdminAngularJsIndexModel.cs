using Microsoft.AspNetCore.Html;

namespace VirtoCommerce.AdminAngularJs.Web.Models;

/// <summary>
/// The values the admin page writes into the AngularJS application's <c>ng-init</c>. The string values
/// are JavaScript expressions, which is why they are <see cref="HtmlString"/>.
/// </summary>
public class AdminAngularJsIndexModel
{
    /// <summary>
    /// The page's base href: the site root, or /apps/platform/ when the page is served there (relative URLs resolve against it).
    /// </summary>
    public string BaseHref { get; set; } = "/";

    public HtmlString PlatformVersion { get; set; } = HtmlString.Empty;
    public HtmlString DemoCredentials { get; set; } = new("''");
    public HtmlString DemoResetTime { get; set; } = new("''");
    public HtmlString License { get; set; }
    public bool SendDiagnosticData { get; set; } = true;

    /// <summary>
    /// Whether modules can be installed from the UI. When false, the module install feature is disabled at the client.
    /// </summary>
    public bool RefreshProbingFolder { get; set; } = true;

    public bool ForceWebSockets { get; set; }

    public HtmlString EnvironmentName { get; set; } = HtmlString.Empty;
    public bool ShowEnvironmentBanner { get; set; }
    public HtmlString EnvironmentTheme { get; set; } = HtmlString.Empty;
}
