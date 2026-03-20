using Microsoft.AspNetCore.Authentication;

namespace GatewayHost.Extensions;

public static class AuthExtensions
{
    public static IServiceCollection AddGatewayAuthentication(
        this IServiceCollection services,
        IConfiguration config,
        string authority = "https://localhost:8443/realms/aspire-template-dev",
        string clientId = "gatewayhost",
        string cookieName = "__Host-gateway")
    {
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = "cookie";
            options.DefaultChallengeScheme = "oidc";
        })
        .AddCookie("cookie", options =>
        {
            options.Cookie.Name = cookieName;
            options.SlidingExpiration = true;
            options.Events.OnSigningOut = async ctx =>
            {
                // Clear local cookie
                await ctx.HttpContext.SignOutAsync("cookie");
                // Trigger remote logout
                await ctx.HttpContext.SignOutAsync("oidc");
            };
        })
        .AddOpenIdConnect("oidc", options =>
        {
            options.Authority = authority;
            options.ClientId = clientId;
            options.ClientSecret = config["Keycloak:ClientSecret"];
            options.ResponseType = "code";
            options.SaveTokens = true;

            options.GetClaimsFromUserInfoEndpoint = true;
            options.MapInboundClaims = false;

            // Keycloak logout endpoint
            options.SignedOutRedirectUri = "/";
        });

        return services;
    }

    public static void MapGatewayAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Manual login endpoint
        endpoints.MapGet("/login", async ctx =>
        {
            await ctx.ChallengeAsync("oidc");
        });

        // Manual logout endpoint
        endpoints.MapGet("/logout", async ctx =>
        {
            await ctx.SignOutAsync("cookie");
            await ctx.SignOutAsync("oidc");
        });
    }
}

