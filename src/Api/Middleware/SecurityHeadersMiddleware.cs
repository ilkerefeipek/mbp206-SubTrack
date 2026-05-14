namespace SubTrack.Api.Middleware;

/// <summary>
/// Sets OWASP-recommended security headers on every response (CSP, X-Frame-Options,
/// X-Content-Type-Options, Referrer-Policy, Permissions-Policy, HSTS on HTTPS).
/// Registered early in the pipeline so that even error responses carry the headers.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

            // CSP — gevsek bir baseline (Blazor WASM 'wasm-unsafe-eval' istiyor, Swagger UI inline style).
            // Production'da nonce-based script-src ile sikilastirilmali.
            headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "script-src 'self' 'wasm-unsafe-eval'; " +
                "style-src 'self' 'unsafe-inline'; " +
                "img-src 'self' data:; " +
                "connect-src 'self' http://localhost:5000 https://localhost:7001 ws: wss:; " +
                "font-src 'self' data:; " +
                "frame-ancestors 'none';";

            if (context.Request.IsHttps)
            {
                headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
            }

            return Task.CompletedTask;
        });

        return next(context);
    }
}
