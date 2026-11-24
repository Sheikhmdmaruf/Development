using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PortfolioCms.Data;
using PortfolioCms.Models;

namespace PortfolioCms.Controllers;

public class HomeController(CmsDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewBag.Projects = await db.Projects.CountAsync();
        ViewBag.Services = await db.Services.CountAsync();
        ViewBag.Posts = await db.Posts.CountAsync();
        ViewBag.Reviews = await db.Reviews.CountAsync();
        ViewBag.Unread = await db.Submissions.CountAsync(s => !s.IsRead);
        ViewBag.Latest = await db.Submissions.AsNoTracking().OrderByDescending(s => s.CreatedAt).Take(5).ToListAsync();
        ViewBag.Recent = (await db.Projects.AsNoTracking().Select(p => new { Kind = "Project", Id = p.Id, Title = p.Title, p.UpdatedAt, Ctrl = "Projects" }).ToListAsync())
            .Concat(await db.Posts.AsNoTracking().Select(p => new { Kind = "Blog post", Id = p.Id, Title = p.Title, p.UpdatedAt, Ctrl = "Posts" }).ToListAsync())
            .Concat(await db.Services.AsNoTracking().Select(p => new { Kind = "Service", Id = p.Id, Title = p.Title, p.UpdatedAt, Ctrl = "Services" }).ToListAsync())
            .OrderByDescending(x => x.UpdatedAt).Take(6)
            .Select(x => (x.Kind, x.Id, x.Title, x.UpdatedAt, x.Ctrl)).ToList();
        return View();
    }

    [AllowAnonymous]
    public IActionResult Error() => View();
}

public class AccountController(CmsDbContext db) : Controller
{
    private static readonly PasswordHasher<AdminUser> Hasher = new();

    public record LoginForm(string UserName, string Password, string? ReturnUrl);
    public record SetupForm(string UserName, string Password, string ConfirmPassword);
    public record PasswordForm(string CurrentPassword, string NewPassword, string ConfirmPassword);

    [AllowAnonymous]
    public async Task<IActionResult> Login(string? returnUrl)
    {
        if (!await db.AdminUsers.AnyAsync()) return RedirectToAction(nameof(Setup));
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [AllowAnonymous, HttpPost, EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginForm form)
    {
        var user = await db.AdminUsers.FirstOrDefaultAsync(u => u.UserName == form.UserName);
        if (user is null || Hasher.VerifyHashedPassword(user, user.PasswordHash, form.Password ?? "") == PasswordVerificationResult.Failed)
        {
            ViewBag.Error = "Wrong username or password.";
            ViewBag.ReturnUrl = form.ReturnUrl;
            return View();
        }
        await SignInAsync(user);
        return Url.IsLocalUrl(form.ReturnUrl) ? Redirect(form.ReturnUrl!) : RedirectToAction("Index", "Home");
    }

    /// <summary>First run only: create the admin account.</summary>
    [AllowAnonymous]
    public async Task<IActionResult> Setup() =>
        await db.AdminUsers.AnyAsync() ? RedirectToAction(nameof(Login)) : View();

    [AllowAnonymous, HttpPost, EnableRateLimiting("login")]
    public async Task<IActionResult> Setup(SetupForm form)
    {
        if (await db.AdminUsers.AnyAsync()) return RedirectToAction(nameof(Login));
        var error = string.IsNullOrWhiteSpace(form.UserName) ? "Choose a username."
            : (form.Password?.Length ?? 0) < 8 ? "The password needs at least 8 characters."
            : form.Password != form.ConfirmPassword ? "The two passwords are different." : null;
        if (error is not null) { ViewBag.Error = error; return View(); }

        var user = new AdminUser { UserName = form.UserName.Trim() };
        user.PasswordHash = Hasher.HashPassword(user, form.Password!);
        db.AdminUsers.Add(user);
        await db.SaveChangesAsync();
        await SignInAsync(user);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    public IActionResult Password() => View();

    [HttpPost]
    public async Task<IActionResult> Password(PasswordForm form)
    {
        var user = await db.AdminUsers.FirstAsync(u => u.UserName == User.Identity!.Name);
        string? error = Hasher.VerifyHashedPassword(user, user.PasswordHash, form.CurrentPassword ?? "") == PasswordVerificationResult.Failed ? "Your current password is wrong."
            : (form.NewPassword?.Length ?? 0) < 8 ? "The new password needs at least 8 characters."
            : form.NewPassword != form.ConfirmPassword ? "The two new passwords are different." : null;
        if (error is not null) { ViewBag.Error = error; return View(); }
        user.PasswordHash = Hasher.HashPassword(user, form.NewPassword!);
        await db.SaveChangesAsync();
        TempData["ok"] = "Password changed.";
        return RedirectToAction("Index", "Home");
    }

    private Task SignInAsync(AdminUser user) => HttpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, user.UserName)], CookieAuthenticationDefaults.AuthenticationScheme)),
        new AuthenticationProperties { IsPersistent = true });
}

public class InboxController(CmsDbContext db) : Controller
{
    public async Task<IActionResult> Index() =>
        View(await db.Submissions.AsNoTracking().OrderByDescending(s => s.CreatedAt).ToListAsync());

    public async Task<IActionResult> Open(int id)
    {
        var s = await db.Submissions.FindAsync(id);
        if (s is null) return NotFound();
        if (!s.IsRead) { s.IsRead = true; await db.SaveChangesAsync(); }
        return View("Message", s);
    }

    [HttpPost]
    public async Task<IActionResult> MarkUnread(int id)
    {
        var s = await db.Submissions.FindAsync(id);
        if (s is not null) { s.IsRead = false; await db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var s = await db.Submissions.FindAsync(id);
        if (s is not null) { db.Submissions.Remove(s); await db.SaveChangesAsync(); TempData["ok"] = "Message deleted."; }
        return RedirectToAction(nameof(Index));
    }
}

public class SettingsController(CmsDbContext db, IConfiguration config) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewBag.SiteKey = (await db.SiteSettings.FindAsync(Seeder.SiteKeySetting))?.Value;
        ViewBag.Origins = config.GetSection("Cms:AllowedOrigins").Get<string[]>() ?? [];
        var order = Seeder.SettingKeys.Select(k => k.Key).ToList();
        var list = (await db.SiteSettings.AsNoTracking().Where(s => !s.Key.StartsWith("__")).ToListAsync())
            .Where(s => !Seeder.HeroKeys.Contains(s.Key)); // hero fields have their own page
        return View(list.OrderBy(s => order.IndexOf(s.Key) is var i && i < 0 ? 999 : i).ToList());
    }

    [HttpPost]
    public async Task<IActionResult> Index(Dictionary<string, string> values)
    {
        foreach (var s in await db.SiteSettings.Where(s => !s.Key.StartsWith("__")).ToListAsync())
            if (!Seeder.HeroKeys.Contains(s.Key) && values.TryGetValue(s.Key, out var v)) s.Value = (v ?? "").Trim();
        await db.SaveChangesAsync();
        TempData["ok"] = "Settings saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> RegenerateKey()
    {
        var s = await db.SiteSettings.FindAsync(Seeder.SiteKeySetting);
        s!.Value = Seeder.NewSiteKey();
        await db.SaveChangesAsync();
        TempData["ok"] = "New site key created. Put it in the website's .env.local (VITE_SITE_KEY) and restart the website.";
        return RedirectToAction(nameof(Index));
    }
}

/// <summary>Two dashboard menus: "Home page" (/hero) and "About page" (/hero/about). Values are site settings.</summary>
public class HeroController(CmsDbContext db) : Controller
{
    public Task<IActionResult> Index() => Show("home");
    public Task<IActionResult> About() => Show("about");

    [HttpPost]
    public Task<IActionResult> Index(Dictionary<string, string> values) => Save("home", values);
    [HttpPost]
    public Task<IActionResult> About(Dictionary<string, string> values) => Save("about", values);

    private static HashSet<string> KeysOf(string page) =>
        Seeder.HeroGroups.Where(g => g.Page == page).SelectMany(g => g.Fields).Select(f => f.Key).ToHashSet();

    private async Task<IActionResult> Show(string page)
    {
        var keys = KeysOf(page);
        ViewBag.Page = page;
        var values = (await db.SiteSettings.AsNoTracking().ToListAsync())
            .Where(s => keys.Contains(s.Key)).ToDictionary(s => s.Key, s => s.Value);
        return View("Index", values);
    }

    private async Task<IActionResult> Save(string page, Dictionary<string, string> values)
    {
        var keys = KeysOf(page); // only this page's fields are changed
        foreach (var s in (await db.SiteSettings.ToListAsync()).Where(s => keys.Contains(s.Key)))
            if (values.TryGetValue(s.Key, out var v)) s.Value = (v ?? "").Trim();
        await db.SaveChangesAsync();
        TempData["ok"] = page == "home" ? "Home page updated successfully." : "About page updated successfully.";
        return RedirectToAction(page == "home" ? nameof(Index) : nameof(About));
    }
}

public class MediaController(IWebHostEnvironment env) : Controller
{
    private static readonly string[] Allowed = [".jpg", ".jpeg", ".png", ".webp", ".gif", ".avif"];
    private const long MaxBytes = 10 * 1024 * 1024;

    private string Folder => Path.Combine(env.WebRootPath, "uploads");

    public IActionResult Index()
    {
        Directory.CreateDirectory(Folder);
        var files = new DirectoryInfo(Folder).GetFiles()
            .Where(f => Allowed.Contains(f.Extension.ToLowerInvariant()))
            .OrderByDescending(f => f.CreationTimeUtc).ToList();
        return View(files);
    }

    /// <summary>Uploads one image. Returns JSON {url} for the image pickers, or redirects for the Media page.</summary>
    [HttpPost, RequestSizeLimit(MaxBytes + 1024 * 64)]
    public async Task<IActionResult> Upload(IFormFile? file, bool json = false)
    {
        string? error = file is null || file.Length == 0 ? "Choose an image first."
            : file.Length > MaxBytes ? "Images must be 10 MB or smaller."
            : !Allowed.Contains(Path.GetExtension(file.FileName).ToLowerInvariant()) ? "Only JPG, PNG, WebP, GIF or AVIF images."
            : null;
        if (error is not null)
        {
            if (json) return BadRequest(new { error });
            TempData["err"] = error; return RedirectToAction(nameof(Index));
        }

        Directory.CreateDirectory(Folder);
        var ext = Path.GetExtension(file!.FileName).ToLowerInvariant();
        var baseName = SlugHelper.Make(Path.GetFileNameWithoutExtension(file.FileName));
        if (baseName.Length == 0) baseName = "image";
        if (baseName.Length > 60) baseName = baseName[..60];
        var name = $"{baseName}-{Guid.NewGuid().ToString("N")[..6]}{ext}";
        await using (var stream = System.IO.File.Create(Path.Combine(Folder, name)))
            await file.CopyToAsync(stream);

        var url = "/uploads/" + name;
        if (json) return Json(new { url });
        TempData["ok"] = $"Uploaded. Path to use: {url}";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult Delete(string name)
    {
        var safe = Path.GetFileName(name);
        var path = Path.Combine(Folder, safe);
        if (System.IO.File.Exists(path)) { System.IO.File.Delete(path); TempData["ok"] = $"Deleted {safe}."; }
        return RedirectToAction(nameof(Index));
    }
}
