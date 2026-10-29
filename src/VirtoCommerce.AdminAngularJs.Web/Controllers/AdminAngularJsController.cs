using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using VirtoCommerce.AdminAngularJs.Web.Models;
using VirtoCommerce.Platform.Core;

namespace VirtoCommerce.AdminAngularJs.Web.Controllers;

/// <summary>
/// Serves the AngularJS admin page at "/". The platform routes "/" here when this admin UI owns the site root
/// (VirtoCommerce:PlatformUI:RootApp), and "/apps/platform/" in any case: it mounts that path as the path base, so the
/// page and every URL relative to it work there too.
/// </summary>
[ApiExplorerSettings(IgnoreApi = true)]
public class AdminAngularJsController(IOptions<PlatformUIOptions> platformUIOptions, IAdminUIContextProvider contextProvider) : Controller
{
    private static readonly JsonSerializerSettings _jsonSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        DateTimeZoneHandling = DateTimeZoneHandling.Utc,
    };

    [HttpGet("/")]
    [AdminUIPage(Module.AppId)]
    [AllowAnonymous]
    public async Task<ActionResult> Index()
    {
        if (!platformUIOptions.Value.Enable)
        {
            return Forbid();
        }

        var context = await contextProvider.GetContextAsync(HttpContext.RequestAborted);

        var model = new AdminAngularJsIndexModel
        {
            PlatformVersion = new HtmlString(context.PlatformVersion),
            DemoCredentials = new HtmlString(string.IsNullOrEmpty(context.DemoCredentials) ? "''" : context.DemoCredentials),
            DemoResetTime = new HtmlString(context.DemoResetTime.HasValue ? ToJavaScript(context.DemoResetTime.Value) : "''"),
            License = context.License == null ? null : new HtmlString(ToJavaScript(context.License)),
            SendDiagnosticData = context.SendDiagnosticData,
            RefreshProbingFolder = context.AllowInstallModules,
            ForceWebSockets = context.ForceWebSockets,
            ShowEnvironmentBanner = context.EnvironmentBanner?.Enable ?? false,
            EnvironmentName = new HtmlString(context.EnvironmentBanner?.Name),
            EnvironmentTheme = new HtmlString(context.EnvironmentBanner?.ThemePreset),
        };

        return View(model);
    }

    // The values go into an ng-init attribute delimited by double quotes, so the JSON uses single ones
    private static string ToJavaScript(object value)
    {
        return JsonConvert.SerializeObject(value, _jsonSettings).Replace("\"", "'");
    }
}
