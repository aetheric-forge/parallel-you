using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using ParallelYou.Host.Hosting;
using ParallelYou.Web.Hosting;
using Xunit;

namespace ParallelYou.Tests;

public sealed class StandaloneHostTests
{
    private sealed class HostFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            foreach (var setting in new Dictionary<string, string>
            {
                ["ParallelYou:ProtectionKeyDirectory"] = Path.Combine(Path.GetTempPath(), "parallel-you-host-tests", "keys"),
                ["ParallelYou:MongoDb:Host"] = "unused.example",
                ["ParallelYou:MongoDb:DatabaseName"] = "personal-library",
                ["ParallelYou:MongoDb:Username"] = "consumer",
                ["ParallelYou:MongoDb:Password"] = "test-only",
                ["Workbench:Redis:Host"] = "unused.example",
                ["Workbench:Redis:Password"] = "test-only",
                ["Registry:Keycloak:Authority"] = "https://identity.example",
                ["Registry:Keycloak:Realm"] = "members",
                ["Registry:Keycloak:ClientId"] = "parallel-you-web",
                ["Registry:Keycloak:ClientSecret"] = "test-only"
            }) builder.UseSetting(setting.Key, setting.Value);
            builder.ConfigureServices(services => services.PostConfigure<OpenIdConnectOptions>(
                OpenIdConnectDefaults.AuthenticationScheme, options => options.ConfigurationManager = new Microsoft.IdentityModel.Protocols.StaticConfigurationManager<OpenIdConnectConfiguration>(new OpenIdConnectConfiguration
                {
                    Issuer = "https://identity.example/realms/members",
                    AuthorizationEndpoint = "https://identity.example/authorize",
                    TokenEndpoint = "https://identity.example/token"
                })));
        }
    }

    [Fact]
    public async Task Public_home_and_module_assets_render_without_connecting_to_storage()
    {
        await using var factory = new HostFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        var home = await client.GetAsync("/parallel-you");
        Assert.Equal(HttpStatusCode.OK, home.StatusCode);
        Assert.Contains("Notice what is true", await home.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/_content/ParallelYou.Web/app.css")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
    }

    [Theory]
    [InlineData("capture")]
    [InlineData("captures")]
    [InlineData("reflect")]
    [InlineData("track")]
    [InlineData("intentions")]
    [InlineData("plans")]
    [InlineData("recommendations")]
    public async Task Every_working_route_challenges_anonymous_visitors_before_storage_access(string page)
    {
        await using var factory = new HostFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        var response = await client.GetAsync("/parallel-you/" + page);
        Assert.True(response.StatusCode == HttpStatusCode.Redirect, await response.Content.ReadAsStringAsync());
        Assert.StartsWith("https://identity.example/authorize", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Validated_oidc_subject_replaces_untrusted_identity_mapping_claims()
    {
        await using var factory = new HostFactory();
        var options = factory.Services.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get(OpenIdConnectDefaults.AuthenticationScheme);
        var scheme = new AuthenticationScheme(OpenIdConnectDefaults.AuthenticationScheme, null, typeof(OpenIdConnectHandler));
        var identity = new ClaimsIdentity([
            new Claim("sub", "validated-subject"),
            new Claim(ClaimTypes.NameIdentifier, "untrusted-alternative"),
            new Claim("forge:identity-scheme", "untrusted-scheme")], "oidc");
        var context = new TokenValidatedContext(new Microsoft.AspNetCore.Http.DefaultHttpContext(), scheme, options,
            new ClaimsPrincipal(identity), new AuthenticationProperties());
        await options.Events.TokenValidated(context);
        Assert.Equal("validated-subject", Assert.Single(identity.FindAll(ClaimTypes.NameIdentifier)).Value);
        Assert.Equal("OpenIdConnect", Assert.Single(identity.FindAll("forge:identity-scheme")).Value);
    }

    [Theory]
    [InlineData("https://outside.example", "/parallel-you")]
    [InlineData("//outside.example", "/parallel-you")]
    [InlineData("/\\outside.example", "/parallel-you")]
    [InlineData("/parallel-you/plans", "/parallel-you/plans")]
    public void Login_redirects_stay_on_the_local_host(string value, string expected) =>
        Assert.Equal(expected, ParallelYouAuthentication.SafeReturnUrl(value));

    [Fact]
    public void Module_credentials_cannot_fall_back_to_platform_credentials()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MongoDb:Host"] = "mongo.example", ["MongoDb:Username"] = "root", ["MongoDb:Password"] = "root-secret",
            ["ParallelYou:MongoDb:DatabaseName"] = "personal-library"
        }).Build();
        Assert.Throws<InvalidOperationException>(() => InstitutionServiceConfiguration.Resolve(configuration, "ParallelYou", "MongoDb"));
    }
}
