using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VirtoCommerce.AdminAngularJs.Web;
using VirtoCommerce.AdminAngularJs.Web.Controllers;
using VirtoCommerce.Platform.Core;
using VirtoCommerce.Platform.Core.Modularity;
using Xunit;

namespace VirtoCommerce.AdminAngularJs.Tests;

/// <summary>
/// The page as the browser gets it: the controller and the compiled Razor views of the module, in an MVC pipeline,
/// with the platform's admin UI routing reduced to what the page depends on (the request feature at /apps/platform/).
/// </summary>
[Trait("Category", "Unit")]
public sealed class AdminAngularJsPageTests : IDisposable
{
    private readonly string _moduleRoot = Path.Combine(Path.GetTempPath(), "admin-angularjs-page-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Get_SiteRoot_RendersTheAdminWithItsBundlesAndValues()
    {
        using var host = await CreateHostAsync(new AdminUIContext { PlatformVersion = "3.1093.0", EnvironmentBanner = new EnvironmentBannerOptions { Enable = true, Name = "QA", ThemePreset = "orange" } });

        var html = await GetStringAsync(host, "/");

        Assert.Contains("<base href=\"/\" />", html);
        Assert.Contains("ng-app=\"platformWebApp\"", html);
        Assert.Contains("src=\"/dist/vendor.js", html);
        Assert.Contains("src=\"/dist/app.js", html);
        Assert.Contains("src=\"/scripts/platformWebApp.pluginLoader.js", html);
        Assert.Contains("href=\"/dist/style.css", html);
        Assert.Contains("platformVersion ='3.1093.0'", html);
        Assert.Contains("environmentName='QA'; environmentTheme='orange'; showEnvironmentBanner=true", html);
        Assert.Contains("var AppDependencies = [", html);
        Assert.Contains("window.forceWebSockets = false;", html);
    }

    [Fact]
    public async Task Get_UnderTheAppPath_BaseHrefKeepsRelativeUrlsThere()
    {
        using var host = await CreateHostAsync(new AdminUIContext(), mountPath: "/apps/platform");

        var html = await GetStringAsync(host, "/");

        Assert.Contains("<base href=\"/apps/platform/\" />", html);
    }

    [Fact]
    public async Task Head_SiteRoot_IsAnsweredForLoadBalancerProbes()
    {
        using var host = await CreateHostAsync(new AdminUIContext());
        using var client = host.GetTestClient();

        using var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, "/"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_PlatformScripts_ServesTheSpaSourcesNoCacheWhileDeveloping()
    {
        WriteFile(Path.Combine(_moduleRoot, "Content", "wwwroot", "js", "app", "workspace.tpl.html"), "<div>workspace</div>");
        using var host = await CreateHostAsync(new AdminUIContext(), environment: Environments.Development);
        using var client = host.GetTestClient();

        using var response = await client.GetAsync("/$(Platform)/Scripts/app/workspace.tpl.html", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("<div>workspace</div>", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal("no-cache", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Get_PlatformScripts_InProduction_KeepsTheDefaultCaching()
    {
        WriteFile(Path.Combine(_moduleRoot, "Content", "wwwroot", "js", "app", "workspace.tpl.html"), "<div>workspace</div>");
        using var host = await CreateHostAsync(new AdminUIContext(), environment: Environments.Production);
        using var client = host.GetTestClient();

        using var response = await client.GetAsync("/$(Platform)/Scripts/app/workspace.tpl.html", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(response.Headers.CacheControl);
    }

    [Fact]
    public async Task Get_PlatformScripts_ModuleWithoutSources_IsNotFound()
    {
        using var host = await CreateHostAsync(new AdminUIContext());
        using var client = host.GetTestClient();

        using var response = await client.GetAsync("/$(Platform)/Scripts/app/workspace.tpl.html", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    public void Dispose()
    {
        if (Directory.Exists(_moduleRoot))
        {
            Directory.Delete(_moduleRoot, recursive: true);
        }
    }

    private Task<IHost> CreateHostAsync(AdminUIContext context, string mountPath = null, string environment = null)
    {
        Directory.CreateDirectory(_moduleRoot);

        return new HostBuilder()
            .UseEnvironment(environment ?? Environments.Production)
            .ConfigureWebHost(webBuilder => webBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IAdminUIContextProvider>(new FakeContextProvider(context));
                    services.AddControllersWithViews()
                        .ConfigureApplicationPartManager(manager => manager.ApplicationParts.Clear())
                        .AddApplicationPart(typeof(AdminAngularJsController).Assembly);
                })
                .Configure(app =>
                {
                    var module = new Module
                    {
                        ModuleInfo = new ManifestModuleInfo { FullPhysicalPath = _moduleRoot },
                        HostEnvironment = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>(),
                    };

                    // What the platform does for a request under /apps/platform/
                    if (mountPath != null)
                    {
                        app.Use((httpContext, next) =>
                        {
                            httpContext.Features.Set(new AdminUIRequestFeature(Module.AppId, mountPath));
                            return next(httpContext);
                        });
                    }

                    module.PostInitialize(app);
                    app.UseRouting();
                    app.UseEndpoints(endpoints => endpoints.MapControllers());
                }))
            .StartAsync();
    }

    private static async Task<string> GetStringAsync(IHost host, string url)
    {
        using var client = host.GetTestClient();
        using var response = await client.GetAsync(url, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }

    private static void WriteFile(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, content);
    }

    private sealed class FakeContextProvider(AdminUIContext context) : IAdminUIContextProvider
    {
        public Task<AdminUIContext> GetContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(context);
    }
}
