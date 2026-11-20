using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using PortfolioAdmin.Web.Data.Entities;

namespace PortfolioAdmin.Web.Infrastructure;

/// <summary>Adds display name and the "must change password" flag to the sign-in cookie. Role permission claims are added by the base class.</summary>
public class AppClaimsFactory(UserManager<AppUser> users, RoleManager<AppRole> roles, IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<AppUser, AppRole>(users, roles, options)
{
    public const string FullName = "full_name";
    public const string MustChangePassword = "must_change_pw";

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user)
    {
        var id = await base.GenerateClaimsAsync(user);
        id.AddClaim(new Claim(FullName, user.FullName));
        if (user.MustChangePassword) id.AddClaim(new Claim(MustChangePassword, "1"));
        return id;
    }
}

public static class UserDisplay
{
    public static string DisplayName(this ClaimsPrincipal user) => user.FindFirstValue(AppClaimsFactory.FullName) ?? user.Identity?.Name ?? "";
    public static string Initials(this ClaimsPrincipal user) =>
        string.Concat(user.DisplayName().Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(p => char.ToUpperInvariant(p[0])));
}

/// <summary>Users created with a temporary password must set their own before using the dashboard.</summary>
public class MustChangePasswordFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var user = context.HttpContext.User;
        if (!user.HasClaim(AppClaimsFactory.MustChangePassword, "1")) return;
        var controller = context.RouteData.Values["controller"]?.ToString();
        if (controller is "Account" or "Error") return;
        context.Result = new RedirectResult("/account/password?required=1");
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}

public class SecurityHeadersMiddleware(RequestDelegate next)
{
    const string Csp =
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
        "font-src 'self' https://fonts.gstatic.com; img-src 'self' data: https: http:; connect-src 'self'; " +
        "frame-ancestors 'none'; base-uri 'self'; form-action 'self'; object-src 'none'";

    public Task Invoke(HttpContext ctx)
    {
        var h = ctx.Response.Headers;
        h.XContentTypeOptions = "nosniff";
        h["Referrer-Policy"] = "strict-origin-when-cross-origin";
        h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        if (ctx.Request.Path.StartsWithSegments("/uploads"))
            h["Cross-Origin-Resource-Policy"] = "cross-origin"; // portfolio images load from another host
        else
        {
            h.XFrameOptions = "DENY";
            h.ContentSecurityPolicy = Csp;
        }
        return next(ctx);
    }
}

/// <summary>Short-lived message shown as a toast after a redirect.</summary>
public static class Toast
{
    public static void Success(this Controller c, string message) => c.TempData["toast.success"] = message;
    public static void Error(this Controller c, string message) => c.TempData["toast.error"] = message;
}
