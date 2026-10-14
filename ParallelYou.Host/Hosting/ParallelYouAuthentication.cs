using System.Security.Claims;
using AethericForge.Runtime.Abstractions.Interfaces.Identity.Authentication;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using ParallelYou.Web.Hosting;

namespace ParallelYou.Host.Hosting;

public static class ParallelYouAuthentication
{
    public static IServiceCollection AddParallelYouAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = InstitutionServiceConfiguration.Resolve(configuration, "Registry", "Keycloak").GetSection("Keycloak");
        if (string.IsNullOrWhiteSpace(settings["Authority"]) || string.IsNullOrWhiteSpace(settings["Realm"]))
            throw new InvalidOperationException("Registry Keycloak Authority and Realm are required.");
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        }).AddCookie(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.LoginPath = "/account/login";
            options.SlidingExpiration = true;
        }).AddOpenIdConnect(options =>
        {
            options.MapInboundClaims = false;
            options.Authority = $"{settings["Authority"]!.TrimEnd('/')}/realms/{settings["Realm"]}";
            options.ClientId = settings["ClientId"];
            options.ClientSecret = settings["ClientSecret"];
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.UsePkce = true;
            options.SaveTokens = false;
            options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;
            options.GetClaimsFromUserInfoEndpoint = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                NameClaimType = "preferred_username",
                RoleClaimType = ClaimTypes.Role
            };
            options.Events.OnTokenValidated = context =>
            {
                // OIDC has already validated the token; use its authoritative subject.
                var subject = context.Principal?.FindFirst("sub")?.Value;
                if (context.Principal?.Identity is not ClaimsIdentity identity || string.IsNullOrWhiteSpace(subject))
                {
                    context.Fail("Keycloak did not produce an authenticated subject.");
                    return Task.CompletedTask;
                }
                foreach (var claim in identity.FindAll("forge:identity-scheme").Concat(identity.FindAll(ClaimTypes.NameIdentifier)).ToArray())
                    identity.RemoveClaim(claim);
                identity.AddClaim(new Claim("forge:identity-scheme", IdentityScheme.OpenIdConnect.ToString()));
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, subject));
                return Task.CompletedTask;
            };
        });
        return services;
    }

    public static IEndpointRouteBuilder MapParallelYouAuthentication(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/account/login", (HttpContext context) => Results.Challenge(
            new AuthenticationProperties { RedirectUri = SafeReturnUrl(context.Request.Query["returnUrl"]) },
            [OpenIdConnectDefaults.AuthenticationScheme])).AllowAnonymous();
        endpoints.MapPost("/account/logout", async (HttpContext context, IAntiforgery antiforgery) =>
        {
            await antiforgery.ValidateRequestAsync(context);
            var form = await context.Request.ReadFormAsync(context.RequestAborted);
            return Results.SignOut(new AuthenticationProperties { RedirectUri = SafeReturnUrl(form["returnUrl"]) },
                [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]);
        }).RequireAuthorization();
        return endpoints;
    }

    public static string SafeReturnUrl(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.StartsWith('/') && !value.Contains('\\')
            && !value.StartsWith("//") && !value.Any(char.IsControl) ? value : "/parallel-you";
}
