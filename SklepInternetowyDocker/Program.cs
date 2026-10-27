using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SklepInternetowyDocker.Data;
using SklepInternetowyDocker.Models;
using SklepInternetowyDocker.Services;
using System.Globalization;
using System.Threading.RateLimiting;

var initializeOnly = args.Contains("--initialize-database", StringComparer.Ordinal);
var builder = WebApplication.CreateBuilder(args.Where(argument => argument != "--initialize-database").ToArray());

// Dodanie serwisów do kontenerów.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDatabaseDeveloperPageExceptionFilter();
}

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.User.RequireUniqueEmail = true;
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Password.RequiredLength = 15;
    options.Password.RequiredUniqueChars = 4;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
})
    .AddErrorDescriber<PolishIdentityErrorDescriber>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.AddApplicationSecurity();
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options =>
        options.DataAnnotationLocalizerProvider = (_, factory) =>
            factory.Create(typeof(SklepInternetowyDocker.SharedResource)))
    .AddMvcOptions(options => options.Filters.Add<LocalizedPageMessagesFilter>());
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var cultures = new[] { new CultureInfo("pl-PL"), new CultureInfo("en-GB") };
    options.DefaultRequestCulture = new RequestCulture("pl-PL");
    options.SupportedCultures = cultures;
    options.SupportedUICultures = cultures;
    options.RequestCultureProviders = [new CookieRequestCultureProvider()];
    options.ApplyCurrentCultureToResponseHeaders = true;
});

builder.Services.Configure<ContactMailOptions>(builder.Configuration.GetSection("ContactMail"));
builder.Services.AddTransient<SmtpMailTransport>();
builder.Services.AddTransient<IEmailSender<ApplicationUser>, SmtpIdentityEmailSender>();
builder.Services.AddTransient<IContactEmailSender, SmtpContactEmailSender>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        context.HttpContext.Response.Headers.RetryAfter = "600";
        context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Security.RateLimit").LogWarning("Request rate limit exceeded for {Path}.", context.HttpContext.Request.Path);
        await context.HttpContext.Response.WriteAsync(
            context.HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Localization.IStringLocalizer<SklepInternetowyDocker.SharedResource>>()
                ["Zbyt wiele prób. Spróbuj ponownie za 10 minut."], cancellationToken);
    };
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
        RateLimitPartition.GetConcurrencyLimiter("application", _ => new ConcurrencyLimiterOptions
        {
            PermitLimit = 32,
            QueueLimit = 0
        }));
    foreach (var policy in new[] { (Name: "accounts", Limit: 20), (Name: "cart", Limit: 60) })
    {
        options.AddPolicy(policy.Name, context => !HttpMethods.IsPost(context.Request.Method)
            ? RateLimitPartition.GetNoLimiter("read")
            : RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.MapToIPv6().ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = policy.Limit,
                    Window = TimeSpan.FromMinutes(10),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
    }
    options.AddPolicy("contact", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(10),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "__Host-RpBit.Session";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment() && !app.Services.GetRequiredService<SmtpMailTransport>().IsConfigured)
{
    throw new InvalidOperationException("Production requires SMTP with TLS and TestMode=false.");
}

if (initializeOnly || (app.Environment.IsDevelopment() && app.Configuration.GetValue("Database:Initialize", true)))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider
        .GetRequiredService<ApplicationDbContext>();

    await dbContext.Database.MigrateAsync();
    await DbInitializer.SeedAsync(dbContext);
}
if (initializeOnly)
{
    return;
}
if (!app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await DatabaseSecurity.VerifyRuntimeRoleAsync(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
}

app.UseApplicationSecurity();
app.UseRequestLocalization();
// Konfiguracja potoku HTTP.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // HSTS = 30dni 
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseRateLimiter();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
    .RequireRateLimiting("accounts")
   .WithStaticAssets();

app.Run();
