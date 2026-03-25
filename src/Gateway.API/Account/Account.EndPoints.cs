using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.API.Account;

public static class AccountEndPoints
{
    public static WebApplication MapAccountEndpoints(this WebApplication app)
    {
        var frontendUrl = app.Configuration["FrontendUrl"]
            ?? throw new InvalidOperationException("FrontendUrl configuration is required.");

        // Root entry point: requires authentication.
        // Unauthenticated users are automatically challenged via OIDC; after login
        // the OIDC handler returns them here and they are redirected to the frontend.
        app.MapGet("/", () => Results.Redirect(frontendUrl))
            .WithName("FrontendRoot")
            .WithTags("Account")
            .RequireAuthorization();

        var api = app.MapGroup("/api");

        // Public endpoint - cacheable for a short duration
        api.MapGet("/account/public", () => Results.Ok("Welcome to API Gateway"))
            .Produces<string>(StatusCodes.Status200OK)
            .WithName("AccountPublic")
            .WithTags("Account")
            .WithMetadata(new ResponseCacheAttribute { Duration = 60, Location = ResponseCacheLocation.Any, NoStore = false });

        // Login triggers the OIDC challenge flow. Allow anonymous access.
        RouteHandlerBuilder loginEndpoint = (RouteHandlerBuilder)api.MapGet("/account/login", async (HttpContext http) =>
        {
            await http.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties
            {
                RedirectUri = "/account/info"
            });
        });

        loginEndpoint.Produces(StatusCodes.Status302Found)
            .WithName("AccountLogin")
            .WithTags("Account")
            .AllowAnonymous();

        // Info requires authentication and returns only the required user fields
        api.MapGet("/account/info", (HttpContext http) =>
        {
            var user = http.User;
            if (user?.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

            return Results.Ok(new
            {
                username = user.FindFirst("preferred_username")?.Value,
                email = user.FindFirst(ClaimTypes.Email)?.Value,
                roles = user.FindAll("roles").Select(c => c.Value)
            });
        })
        .Produces<object>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .WithName("AccountInfo")
        .WithTags("Account")
        .RequireAuthorization();

        // Logout signs out from OIDC and cookie schemes and redirects to the public page
        api.MapPost("/account/logout", async (HttpContext http) =>
        {
            var prop = new AuthenticationProperties
            {
                RedirectUri = "/account/public"
            };

            await http.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, prop);
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        })
        .WithName("AccountLogout")
        .WithTags("Account")
        .RequireAuthorization();

        return app;
    }
}
