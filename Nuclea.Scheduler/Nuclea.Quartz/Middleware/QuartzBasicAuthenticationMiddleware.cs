using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Nuclea.Quartz.Middleware;

public class QuartzBasicAuthenticationMiddleware(RequestDelegate next, string username, string password)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/quartz"))
        {
            var credentials = GetCredentialsFromHeader(context);

            if (credentials == null || !ValidateCredentials(credentials.Value.username, credentials.Value.password))
            {
                context.Response.Headers.WWWAuthenticate = "Basic realm=\"Quartz Dashboard\"";
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Unauthorized");
                return;
            }
            
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, credentials.Value.username),
                new Claim(ClaimTypes.Role, "QuartzAdmin")
            };
            var identity = new ClaimsIdentity(claims, "Basic");
            context.User = new ClaimsPrincipal(identity);
        }

        await next(context);
    }

    private (string username, string password)? GetCredentialsFromHeader(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            return null;
        }

        var authHeaderValue = authHeader.ToString();
        if (!authHeaderValue.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            var encodedCredentials = authHeaderValue["Basic ".Length..].Trim();
            var decodedCredentials = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
            var parts = decodedCredentials.Split(':', 2);

            if (parts.Length == 2)
            {
                return (parts[0], parts[1]);
            }
        }
        catch
        {
          // ignored
        }

        return null;
    }

    private bool ValidateCredentials(string inputUsername, string inputPassword)
    {
        return string.Equals(inputUsername, username, StringComparison.Ordinal) &&
               string.Equals(inputPassword, password, StringComparison.Ordinal);
    }
}
