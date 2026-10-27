using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;

namespace SklepInternetowyDocker.Services;

public static class ApplicationSecurity
{
    public static void AddApplicationSecurity(this WebApplicationBuilder builder)
    {
        var production = !builder.Environment.IsDevelopment();
        var origin = builder.Configuration["Security:PublicOrigin"];
        var keyPath = builder.Configuration["Security:DataProtectionPath"];
        if (production)
        {
            var hosts = (builder.Configuration["AllowedHosts"] ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var publicOrigin)
                || publicOrigin.Scheme != "https" || publicOrigin.UserInfo.Length != 0
                || publicOrigin.PathAndQuery != "/" || publicOrigin.Fragment.Length != 0
                || hosts.Length == 0 || hosts.Any(host => host.Contains('*'))
                || !hosts.Contains(publicOrigin.Host, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Production requires an HTTPS Security:PublicOrigin and explicit matching AllowedHosts.");
            }

            if (string.IsNullOrWhiteSpace(keyPath) || !Path.IsPathFullyQualified(keyPath))
            {
                throw new InvalidOperationException("Production requires an absolute persistent Security:DataProtectionPath.");
            }

            if (builder.Configuration.GetValue<bool>("Database:Initialize"))
            {
                throw new InvalidOperationException("Use --initialize-database separately; production runtime must not run migrations.");
            }

            builder.Services.AddHttpsRedirection(options => options.HttpsPort = publicOrigin.Port);
        }

        builder.Services.AddHsts(options => options.MaxAge = TimeSpan.FromDays(365));

        keyPath ??= Path.Combine(builder.Environment.ContentRootPath, ".keys");
        Directory.CreateDirectory(keyPath);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(keyPath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        builder.Services.AddDataProtection().SetApplicationName("RP-bit")
            .PersistKeysToFileSystem(new DirectoryInfo(keyPath));
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.AddServerHeader = false;
            options.Limits.MaxRequestBodySize = 131072;
            options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(15);
        });
        builder.Services.Configure<FormOptions>(options =>
        {
            options.ValueCountLimit = 64;
            options.KeyLengthLimit = 128;
            options.ValueLengthLimit = 16384;
            options.MultipartBodyLengthLimit = 131072;
        });
        builder.Services.Configure<MvcOptions>(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "__Host-RpBit.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = false;
        });
        builder.Services.AddAntiforgery(options =>
        {
            options.Cookie.Name = "__Host-RpBit.Antiforgery";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
        });
        builder.Services.Configure<Microsoft.AspNetCore.Identity.DataProtectionTokenProviderOptions>(options =>
            options.TokenLifespan = TimeSpan.FromHours(2));
        builder.Services.Configure<Microsoft.AspNetCore.Identity.SecurityStampValidatorOptions>(options =>
            options.ValidationInterval = TimeSpan.FromMinutes(5));
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;
            options.RequireHeaderSymmetry = true;
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();
            foreach (var address in builder.Configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [])
            {
                options.KnownProxies.Add(IPAddress.Parse(address));
            }
        });
    }

    public static void UseApplicationSecurity(this WebApplication app)
    {
        if (app.Configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() is { Length: > 0 })
        {
            app.UseForwardedHeaders();
        }

        app.Use(async (context, next) =>
        {
            if (!app.Environment.IsDevelopment())
            {
                context.Request.Host = new HostString(new Uri(app.Configuration["Security:PublicOrigin"]!).Authority);
            }

            var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            context.Items["CspNonce"] = nonce;
            context.Response.Headers.ContentSecurityPolicy = $"default-src 'self'; script-src 'self' 'nonce-{nonce}'; style-src 'self' https://fonts.googleapis.com; style-src-attr 'none'; font-src 'self' https://fonts.gstatic.com; img-src 'self' data:; connect-src 'self'; object-src 'none'; base-uri 'self'; frame-ancestors 'none'; form-action 'self'";
            context.Response.Headers.XContentTypeOptions = "nosniff";
            context.Response.Headers.XFrameOptions = "DENY";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
            context.Response.OnStarting(() =>
            {
                if (context.Response.ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) == true)
                {
                    context.Response.Headers.CacheControl = "no-store";
                    context.Response.Headers.Pragma = "no-cache";
                }

                return Task.CompletedTask;
            });
            await next();
        });
    }
}