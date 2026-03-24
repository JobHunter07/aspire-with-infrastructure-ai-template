using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.API.Account;

public static class AccountEndPoints
{
    public static WebApplication MapAccountEndpoints(this WebApplication app)
    {
        // Public endpoint - cacheable for a short duration
        app.MapGet("/account/public", () => Results.Ok("Welcome to API Gateway"))
            .Produces<string>(StatusCodes.Status200OK)
            .WithName("AccountPublic")
            .WithTags("Account")
            .WithMetadata(new ResponseCacheAttribute { Duration = 60, Location = ResponseCacheLocation.Any, NoStore = false });

        // Login triggers the OIDC challenge flow. Allow anonymous access.
        RouteHandlerBuilder loginEndpoint = (RouteHandlerBuilder)app.MapGet("/account/login", async (HttpContext http) =>
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

        // Info requires authentication and returns the user's claims
        app.MapGet("/account/info", (HttpContext http) =>
        {
            var claims = http.User?.Claims
                .Select(c => (object)new { c.Type, c.Value })
                .ToList() ?? new List<object>();

            return Results.Ok(claims);
        })
        .Produces<IEnumerable<object>>(StatusCodes.Status200OK)
        .WithName("AccountInfo")
        .WithTags("Account")
        .RequireAuthorization();

        // Logout signs out from OIDC and cookie schemes and redirects to the public page
        app.MapGet("/account/logout", async (HttpContext http) =>
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
