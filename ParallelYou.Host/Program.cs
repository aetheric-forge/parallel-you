using Forge.Primitives.MongoDb;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using ParallelYou.Host.Components;
using ParallelYou.Host.Hosting;
using ParallelYou.Web.Hosting;

MongoBsonSetup.EnsureGuidRepresentationRegistered();
var builder = WebApplication.CreateBuilder(args);
ProvisionedParallelYouConfiguration.Apply(builder.Configuration);
if (!builder.Configuration.GetValue("ParallelYou:Enabled", true))
    throw new InvalidOperationException("The standalone host requires ParallelYou:Enabled=true.");
var keyDirectory = builder.Configuration["ParallelYou:ProtectionKeyDirectory"];
if (string.IsNullOrWhiteSpace(keyDirectory))
    throw new InvalidOperationException("ParallelYou:ProtectionKeyDirectory is required for persistent authentication keys.");
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keyDirectory))
    .SetApplicationName("ParallelYou");
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorization();
builder.Services.AddParallelYouAuthentication(builder.Configuration);
builder.Services.AddStandaloneParallelYou(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddCheck<MongoDbHealthCheck>("mongodb", tags: ["ready"])
    .AddCheck<RedisHealthCheck>("redis", tags: ["ready"]);
var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapParallelYouAuthentication();
app.MapGet("/", () => Results.LocalRedirect("/parallel-you")).AllowAnonymous();
app.MapGet("/error", () => Results.Problem("The request could not be completed.")).AllowAnonymous();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = x => x.Tags.Contains("ready") }).AllowAnonymous();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(ParallelYou.Web.Components.Pages.Home).Assembly);
app.Run();

public partial class Program;
