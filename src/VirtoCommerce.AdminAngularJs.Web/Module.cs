using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using VirtoCommerce.Platform.Core.Modularity;

namespace VirtoCommerce.AdminAngularJs.Web;

/// <summary>
/// The AngularJS admin (Commerce Manager) on top of the API-only platform. module.manifest declares it as the
/// "platform" app with <c>&lt;adminUI hosting="Endpoint"&gt;</c>:
/// <list type="bullet">
/// <item>The SPA files in <c>Content/wwwroot</c> join the platform web root, so they keep the URLs they always had
/// (<c>/dist/app.js</c>, <c>/images/...</c>, <c>/css/...</c>) and <c>asp-append-version</c> works for them.
/// Files of the platform's own web root take precedence.</item>
/// <item>The SPA sources are served at <c>/$(Platform)/Scripts</c>, where non-bundled templates are loaded from.</item>
/// <item><see cref="Controllers.AdminAngularJsController"/> serves the page at the site root, when this admin UI owns it, and at <c>/apps/platform/</c>.</item>
/// <item>The platform UI strings ship in <c>Localizations</c> and are picked up by the platform translation service.</item>
/// </list>
/// </summary>
public class Module : IModule, IHasHostEnvironment
{
    /// <summary>
    /// The app id the AngularJS admin always had: the host of the modules' AngularJS bundles (<c>api/apps/platform/manifest</c>).
    /// </summary>
    public const string AppId = "platform";

    public const string WebRootFolder = "Content/wwwroot";
    public const string ScriptsFolder = "js";
    public const string ScriptsRequestPath = "/$(Platform)/Scripts";

    public ManifestModuleInfo ModuleInfo { get; set; }
    public IHostEnvironment HostEnvironment { get; set; }

    public void Initialize(IServiceCollection serviceCollection)
    {
        if (HostEnvironment is IWebHostEnvironment webHostEnvironment)
        {
            webHostEnvironment.WebRootFileProvider = new CompositeFileProvider(
                webHostEnvironment.WebRootFileProvider,
                CreateFileProvider(WebRootFolder));
        }
    }

    public void PostInitialize(IApplicationBuilder appBuilder)
    {
        var isDevelopment = HostEnvironment?.IsDevelopment() == true;

        appBuilder.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = CreateFileProvider(Path.Combine(WebRootFolder, ScriptsFolder)),
            RequestPath = new PathString(ScriptsRequestPath),
            // Edited templates show up on reload while developing
            OnPrepareResponse = context =>
            {
                if (isDevelopment)
                {
                    context.Context.Response.Headers.CacheControl = "no-cache";
                }
            },
        });
    }

    public void Uninstall()
    {
        // Nothing to do here
    }

    private IFileProvider CreateFileProvider(string relativePath)
    {
        var path = Path.GetFullPath(Path.Combine(ModuleInfo.FullPhysicalPath, relativePath));

        if (!Directory.Exists(path))
        {
            return new NullFileProvider();
        }

        // Polling instead of a FileSystemWatcher keeps no directory handle open, so the module can be updated or uninstalled at runtime
        return new PhysicalFileProvider(path) { UsePollingFileWatcher = true, UseActivePolling = false };
    }
}
