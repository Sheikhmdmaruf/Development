using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortfolioAdmin.Web.Authorization;
using PortfolioAdmin.Web.Data;
using PortfolioAdmin.Web.Data.Entities;
using PortfolioAdmin.Web.Infrastructure;
using PortfolioAdmin.Web.Services;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// ---------------------------------------------------------------- database
services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException("Connection string 'Default' is missing.")));

// ---------------------------------------------------------------- identity
services.AddIdentity<AppUser, AppRole>(o =>
    {
        o.User.RequireUniqueEmail = true;
        o.Password.RequiredLength = 10;
        o.Password.RequireNonAlphanumeric = false;
        o.Lockout.MaxFailedAccessAttempts = 5;
        o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        o.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders()
    .AddClaimsPrincipalFactory<AppClaimsFactory>();

// Re-check the security stamp every minute: role changes and deactivations take effect quickly.
services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(1));

services.ConfigureApplicationCookie(o =>
{
    o.Cookie.Name = "pa.auth";
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    o.LoginPath = "/login";
    o.LogoutPath = "/logout";
    o.AccessDeniedPath = "/access-denied";
    o.ExpireTimeSpan = TimeSpan.FromHours(10);
    o.SlidingExpiration = true;
    // JSON / API calls get status codes instead of login redirects.
    o.Events.OnRedirectToLogin = ctx => Redirect(ctx, 401);
    o.Events.OnRedirectToAccessDenied = ctx => Redirect(ctx, 403);
    static Task Redirect(Microsoft.AspNetCore.Authentication.RedirectContext<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions> ctx, int code)
    {
        if (ctx.Request.Path.StartsWithSegments("/api") || ctx.Request.Headers.Accept.ToString().Contains("application/json"))
            ctx.Response.StatusCode = code;
        else ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    }
});

services.AddAntiforgery(o => { o.HeaderName = "X-CSRF-TOKEN"; o.Cookie.Name = "pa.csrf"; o.Cookie.SameSite = SameSiteMode.Strict; });

// ---------------------------------------------------------------- authorization
services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
services.AddSingleton<IAuthorizationHandler, PermissionHandler>();
services.AddAuthorizationBuilder()
    // Everything requires sign-in unless marked [AllowAnonymous].
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

// ---------------------------------------------------------------- app services
services.AddHttpContextAccessor();
services.AddScoped<ActivityService>();
services.AddScoped<NotificationService>();
services.AddScoped<SubmissionService>();
services.AddScoped<ContentService>();
services.AddScoped<MediaStorage>();

services.AddControllersWithViews(o =>
{
    o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()); // CSRF check on every POST/PUT/DELETE from the dashboard
    o.Filters.Add<MustChangePasswordFilter>();
}).AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

// ---------------------------------------------------------------- rate limiting
services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.OnRejected = async (ctx, token) =>
    {
        ctx.HttpContext.Response.Headers.RetryAfter = "60";
        if (ctx.HttpContext.Request.Path.StartsWithSegments("/api"))
            await ctx.HttpContext.Response.WriteAsJsonAsync(new { title = "Too many requests", status = 429, detail = "Please wait a minute and try again." }, token);
    };
    static string Ip(HttpContext c) => c.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    o.AddPolicy("submissions", c => RateLimitPartition.GetFixedWindowLimiter(Ip(c),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1) }));
    o.AddPolicy("content", c => RateLimitPartition.GetFixedWindowLimiter(Ip(c),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1) }));
    o.AddPolicy("login", c => RateLimitPartition.GetFixedWindowLimiter(Ip(c),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});

// The public API is called from your websites (any origin). Each site key is then checked against its own allowed origins.
services.AddCors(o => o.AddPolicy("public-api", p => p.SetIsOriginAllowed(_ => true)
    .WithMethods("GET", "POST").WithHeaders("Content-Type", "X-Site-Key", "Authorization").SetPreflightMaxAge(TimeSpan.FromHours(1))));

services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Behind a reverse proxy (IIS, nginx) on the same machine these defaults are fine.
    // For a remote proxy add it to o.KnownProxies.
});

services.Configure<RouteOptions>(o => o.LowercaseUrls = true);

var app = builder.Build();

await Seeder.RunAsync(app.Services);

app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
// Friendly error pages for the dashboard; the API keeps its JSON responses.
app.UseWhen(ctx => !ctx.Request.Path.StartsWithSegments("/api"), branch => branch.UseStatusCodePagesWithReExecute("/error/{0}"));
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseStaticFiles();
app.UseRouting();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapControllerRoute("default", "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
