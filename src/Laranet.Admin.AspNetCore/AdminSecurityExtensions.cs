using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Laranet.Admin.AspNetCore;

/// <summary>
/// Adds security headers to Admin responses.
/// </summary>
public static class AdminSecurityExtensions
{
    /// <summary>
    /// Adds HSTS and browser security headers to requests under <c>/admin</c>.
    /// Call this after exception handling and before serving the Admin pages.
    /// </summary>
    public static IApplicationBuilder UseLaranetAdminSecurityHeaders(
        this IApplicationBuilder application)
    {
        ArgumentNullException.ThrowIfNull(application);

        return application.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/admin"))
            {
                var headers = context.Response.Headers;
                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                headers["Permissions-Policy"] =
                    "camera=(), geolocation=(), microphone=()";
                headers["Content-Security-Policy"] =
                    "default-src 'self'; " +
                    "base-uri 'self'; " +
                    "form-action 'self'; " +
                    "frame-ancestors 'none'; " +
                    "img-src 'self' data:; " +
                    "script-src 'self'; " +
                    "style-src 'self' 'unsafe-inline';";

                if (context.Request.IsHttps)
                {
                    headers["Strict-Transport-Security"] =
                        "max-age=31536000";
                }
            }

            await next();
        });
    }
}
