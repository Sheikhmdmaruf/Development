using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PortfolioAdmin.Web.Authorization;
using PortfolioAdmin.Web.Data;
using PortfolioAdmin.Web.Data.Entities;
using PortfolioAdmin.Web.Infrastructure;
using PortfolioAdmin.Web.Models;
using PortfolioAdmin.Web.Services;

namespace PortfolioAdmin.Web.Controllers;

public class AccountController(
    SignInManager<AppUser> signIn, UserManager<AppUser> users, AppDbContext db, ActivityService activity) : Controller
{
    [AllowAnonymous, HttpGet("/login")]
    public IActionResult Login(string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true) return LocalRedirect(SafeReturn(returnUrl));
        return View(new LoginModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous, HttpPost("/login"), EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await users.FindByEmailAsync(model.Email.Trim());
        if (user is null || !user.IsActive)
        {
            ModelState.AddModelError("", "Wrong email or password.");
            activity.Log("auth.login_failed", $"Failed sign-in for {model.Email.Trim()}", audit: true);
            await db.SaveChangesAsync();
            return View(model);
        }

        var result = await signIn.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await users.UpdateAsync(user);
            activity.Log("auth.login", $"{user.FullName} signed in", "User", user.Id, audit: true, userId: user.Id);
            await db.SaveChangesAsync();
            return LocalRedirect(SafeReturn(model.ReturnUrl));
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError("", "Too many failed attempts. This account is locked for 15 minutes.");
            activity.Log("auth.locked_out", $"{user.Email} was locked out after failed sign-ins", "User", user.Id, audit: true, userId: user.Id);
        }
        else
        {
            ModelState.AddModelError("", "Wrong email or password.");
            activity.Log("auth.login_failed", $"Failed sign-in for {user.Email}", "User", user.Id, audit: true, userId: user.Id);
        }
        await db.SaveChangesAsync();
        return View(model);
    }

    [HttpPost("/logout")]
    public async Task<IActionResult> Logout()
    {
        await signIn.SignOutAsync();
        return Redirect("/login");
    }

    [AllowAnonymous, HttpGet("/access-denied")]
    public IActionResult AccessDenied() => View();

    // ------------------------------------------------------------ profile

    [HttpGet("/account/profile")]
    public async Task<IActionResult> Profile()
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return Challenge();
        return View(new ProfileModel
        {
            FullName = user.FullName, JobTitle = user.JobTitle, PhoneNumber = user.PhoneNumber, AvatarPath = user.AvatarPath,
            Email = user.Email ?? "", Roles = [.. await users.GetRolesAsync(user)]
        });
    }

    [HttpPost("/account/profile")]
    public async Task<IActionResult> Profile(ProfileModel model)
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return Challenge();
        if (!ModelState.IsValid)
        {
            model.Email = user.Email ?? ""; model.Roles = [.. await users.GetRolesAsync(user)];
            return View(model);
        }
        user.FullName = model.FullName.Trim();
        user.JobTitle = model.JobTitle?.Trim();
        user.PhoneNumber = model.PhoneNumber?.Trim();
        user.AvatarPath = model.AvatarPath?.StartsWith("/uploads/") == true ? model.AvatarPath : null;
        await users.UpdateAsync(user);
        await signIn.RefreshSignInAsync(user); // new name in the cookie
        this.Success("Profile saved.");
        return Redirect("/account/profile");
    }

    [HttpGet("/account/password")]
    public IActionResult Password(bool required) => View(new ChangePasswordModel { Required = required || User.HasClaim(AppClaimsFactory.MustChangePassword, "1") });

    [HttpPost("/account/password")]
    public async Task<IActionResult> Password(ChangePasswordModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await users.GetUserAsync(User);
        if (user is null) return Challenge();

        var result = await users.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
            return View(model);
        }
        user.MustChangePassword = false;
        await users.UpdateAsync(user);
        activity.Log("auth.password_changed", $"{user.FullName} changed their password", "User", user.Id, audit: true);
        await db.SaveChangesAsync();
        await signIn.RefreshSignInAsync(user);
        this.Success("Password changed.");
        return Redirect("/");
    }

    // ------------------------------------------------------------ notification preferences

    [HttpGet("/account/notifications")]
    public async Task<IActionResult> Notifications()
    {
        var id = User.Id();
        ViewBag.Muted = await db.NotificationPreferences.Where(p => p.UserId == id && !p.InApp).Select(p => p.Type).ToListAsync();
        return View();
    }

    [HttpPost("/account/notifications")]
    public async Task<IActionResult> Notifications(string[] enabled)
    {
        var id = User.Id();
        db.NotificationPreferences.RemoveRange(db.NotificationPreferences.Where(p => p.UserId == id));
        foreach (var (type, _, _) in NotificationTypes.All.Where(t => !enabled.Contains(t.Type)))
            db.NotificationPreferences.Add(new NotificationPreference { UserId = id, Type = type, InApp = false });
        await db.SaveChangesAsync();
        this.Success("Notification preferences saved.");
        return Redirect("/account/notifications");
    }

    string SafeReturn(string? url) => !string.IsNullOrEmpty(url) && Url.IsLocalUrl(url) ? url : "/";
}

[AllowAnonymous, IgnoreAntiforgeryToken] // re-executed with the original request method
public class ErrorController : Controller
{
    [Route("/error/{code:int?}")]
    public IActionResult Index(int? code)
    {
        ViewBag.Code = code ?? 500;
        Response.StatusCode = code ?? 500;
        var layout = User.Identity?.IsAuthenticated == true ? "_Layout" : "_AuthLayout";
        ViewData["Layout"] = layout;
        return View();
    }
}
