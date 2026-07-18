using System.Security.Claims;
using AethericForge.Runtime.Abstractions.Interfaces.Identity.Authentication;
using AethericForge.Runtime.Institutions.Campus;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace ParallelYou.Web.Hosting;

public static class ForgeAuthenticationExtensions
{
    public static IServiceCollection AddForgeCampusAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var keycloakSection = configuration.GetRequiredSection("Keycloak");

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.SlidingExpiration = true;
            })
            .AddOpenIdConnect(options =>
            {
                options.Authority = keycloakSection["Authority"];
                options.ClientId = keycloakSection["ClientId"];
                options.ClientSecret = keycloakSection["ClientSecret"];
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.UsePkce = true;
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "preferred_username",
                    RoleClaimType = ClaimTypes.Role
                };
                options.Events.OnTokenValidated = RegisterPrincipalAsync;
            });

        services.AddAuthorization();
        services.AddCascadingAuthenticationState();

        return services;
    }

    public static IEndpointRouteBuilder MapForgeCampusAuthentication(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/account/login", LoginAsync);
        endpoints.MapPost("/account/logout", LogoutAsync);

        return endpoints;
    }

    private static async Task<IResult> LoginAsync(
        HttpContext context,
        IAntiforgery antiforgery,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(context);
        var form = await context.Request.ReadFormAsync(cancellationToken);
        var returnUrl = GetSafeReturnUrl(form["returnUrl"].ToString());

        return Results.Challenge(
            new AuthenticationProperties
            {
                RedirectUri = returnUrl
            },
            [OpenIdConnectDefaults.AuthenticationScheme]);
    }

    private static async Task<IResult> LogoutAsync(
        HttpContext context,
        IAntiforgery antiforgery,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(context);
        var form = await context.Request.ReadFormAsync(cancellationToken);
        var returnUrl = GetSafeReturnUrl(form["returnUrl"].ToString());

        return Results.SignOut(
            new AuthenticationProperties
            {
                RedirectUri = returnUrl
            },
            [
                CookieAuthenticationDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme
            ]);
    }

    private static async Task RegisterPrincipalAsync(TokenValidatedContext context)
    {
        var accessToken = context.TokenEndpointResponse?.AccessToken;
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            context.Fail("Keycloak did not return an access token.");
            return;
        }

        var campus = context.HttpContext.RequestServices.GetRequiredService<ICampus>();
        var principalIdentity = await campus.Registry.Registrar.AuthenticateAsync(
            IdentityScheme.OpenIdConnect,
            new Dictionary<string, string>
            {
                ["token"] = accessToken
            },
            context.HttpContext.RequestAborted);

        if (principalIdentity is null || !principalIdentity.IsAuthenticated)
        {
            context.Fail("Forge Authentication rejected the Keycloak principal.");
            return;
        }

        if (context.Principal?.Identity is not ClaimsIdentity identity)
        {
            context.Fail("Keycloak did not produce a claims identity.");
            return;
        }

        identity.AddClaim(new Claim(
            "forge:identity-scheme",
            principalIdentity.Scheme.ToString()));

        if (!identity.HasClaim(claim => claim.Type == ClaimTypes.NameIdentifier))
        {
            identity.AddClaim(new Claim(
                ClaimTypes.NameIdentifier,
                principalIdentity.SubjectId));
        }

        if (!identity.HasClaim(claim => claim.Type == identity.NameClaimType))
        {
            identity.AddClaim(new Claim(
                identity.NameClaimType,
                principalIdentity.DisplayName ?? principalIdentity.SubjectId));
        }
    }

    private static string GetSafeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl) ||
            !returnUrl.StartsWith('/') ||
            returnUrl.StartsWith("//", StringComparison.Ordinal))
        {
            return "/";
        }

        return returnUrl;
    }
}
