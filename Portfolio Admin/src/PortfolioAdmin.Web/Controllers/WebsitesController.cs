using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortfolioAdmin.Web.Authorization;
using PortfolioAdmin.Web.Data;
using PortfolioAdmin.Web.Data.Entities;
using PortfolioAdmin.Web.Infrastructure;
using PortfolioAdmin.Web.Services;

namespace PortfolioAdmin.Web.Controllers;

public class WebsiteForm
{
    public int? Id { get; set; }
    [Required, MaxLength(120)] public string Name { get; set; } = "";
    [Required, MaxLength(300), Url, Display(Name = "Website URL")] public string Url { get; set; } = "";
    [Required, MaxLength(60), RegularExpression("^[a-z0-9][a-z0-9-]*$", ErrorMessage = "Use lowercase letters, numbers and dashes.")]
    public string Identifier { get; set; } = "";
    [MaxLength(2000), Display(Name = "Allowed origins")] public string? AllowedOrigins { get; set; }
    [Display(Name = "Can read portfolio content")] public bool CanReadContent { get; set; }
    public bool IsActive { get; set; } = true;
}

[HasPermission(Permissions.Websites.View)]
public partial class WebsitesController(AppDbContext db, ActivityService activity) : Controller
{
    [HttpGet("/websites")]
    public async Task<IActionResult> Index()
    {
        var sites = await db.Websites.OrderByDescending(w => w.IsActive).ThenBy(w => w.Name).ToListAsync();
        var since = DateTime.UtcNow.AddDays(-30);
        ViewBag.Last30 = await db.ContactSubmissions.Where(s => s.CreatedAt >= since && s.WebsiteId != null)
            .GroupBy(s => s.WebsiteId!.Value).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N);
        return View(sites);
    }

    [HttpGet("/websites/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var w = await db.Websites.FindAsync(id);
        if (w is null) return NotFound();
        ViewBag.Recent = await db.ContactSubmissions.Include(s => s.Contact).Where(s => s.WebsiteId == id)
            .OrderByDescending(s => s.CreatedAt).Take(8).ToListAsync();
        ViewBag.ApiBase = $"{Request.Scheme}://{Request.Host}";
        ViewBag.NewSecret = TempData["new.secret"] as string;
        return View(w);
    }

    [HttpGet("/websites/new"), HasPermission(Permissions.Websites.Manage)]
    public IActionResult Create() => View("Form", new WebsiteForm());

    [HttpPost("/websites/new"), HasPermission(Permissions.Websites.Manage)]
    public async Task<IActionResult> Create(WebsiteForm m)
    {
        m.Identifier = m.Identifier.Trim().ToLowerInvariant();
        if (await db.Websites.AnyAsync(w => w.Identifier == m.Identifier))
            ModelState.AddModelError(nameof(m.Identifier), "Another website already uses this identifier.");
        var origins = NormaliseOrigins(m.AllowedOrigins, m.Url, out var bad);
        if (bad is not null) ModelState.AddModelError(nameof(m.AllowedOrigins), $"\"{bad}\" isn't a valid origin. Use e.g. https://example.com");
        if (!ModelState.IsValid) return View("Form", m);

        var secret = Keys.New("sk_live_", 32);
        var w = new Website
        {
            Name = m.Name.Trim(), Url = m.Url.Trim(), Identifier = m.Identifier, AllowedOrigins = origins, CanReadContent = m.CanReadContent,
            IsActive = m.IsActive, PublicKey = Keys.New("pk_live_"), SecretKeyHash = Keys.Hash(secret), SecretKeyPrefix = secret[..12],
        };
        db.Websites.Add(w);
        await db.SaveChangesAsync();
        activity.Log("website.created", $"Connected website {w.Name}", "Website", w.Id, audit: true);
        await db.SaveChangesAsync();

        TempData["new.secret"] = secret;
        this.Success("Website added. Copy the secret key now — it won't be shown again.");
        return Redirect($"/websites/{w.Id}");
    }

    [HttpGet("/websites/{id:int}/edit"), HasPermission(Permissions.Websites.Manage)]
    public async Task<IActionResult> Edit(int id)
    {
        var w = await db.Websites.FindAsync(id);
        if (w is null) return NotFound();
        return View("Form", new WebsiteForm
        {
            Id = w.Id, Name = w.Name, Url = w.Url, Identifier = w.Identifier, AllowedOrigins = w.AllowedOrigins,
            CanReadContent = w.CanReadContent, IsActive = w.IsActive,
        });
    }

    [HttpPost("/websites/{id:int}/edit"), HasPermission(Permissions.Websites.Manage)]
    public async Task<IActionResult> Edit(int id, WebsiteForm m)
    {
        var w = await db.Websites.FindAsync(id);
        if (w is null) return NotFound();
        m.Id = id;
        m.Identifier = m.Identifier.Trim().ToLowerInvariant();
        if (await db.Websites.AnyAsync(x => x.Identifier == m.Identifier && x.Id != id))
            ModelState.AddModelError(nameof(m.Identifier), "Another website already uses this identifier.");
        var origins = NormaliseOrigins(m.AllowedOrigins, m.Url, out var bad);
        if (bad is not null) ModelState.AddModelError(nameof(m.AllowedOrigins), $"\"{bad}\" isn't a valid origin. Use e.g. https://example.com");
        if (!ModelState.IsValid) return View("Form", m);

        w.Name = m.Name.Trim(); w.Url = m.Url.Trim(); w.Identifier = m.Identifier; w.AllowedOrigins = origins;
        w.CanReadContent = m.CanReadContent; w.IsActive = m.IsActive;
        activity.Log("website.updated", $"Updated website {w.Name}", "Website", w.Id, audit: true);
        await db.SaveChangesAsync();
        this.Success("Website saved.");
        return Redirect($"/websites/{id}");
    }

    [HttpPost("/websites/{id:int}/rotate-public"), HasPermission(Permissions.Websites.Manage)]
    public async Task<IActionResult> RotatePublic(int id)
    {
        var w = await db.Websites.FindAsync(id);
        if (w is null) return NotFound();
        w.PublicKey = Keys.New("pk_live_");
        activity.Log("website.key_rotated", $"Regenerated the site key of {w.Name}", "Website", w.Id, audit: true);
        await db.SaveChangesAsync();
        this.Success("New site key created. Update it on your website — the old key stopped working.");
        return Redirect($"/websites/{id}");
    }

    [HttpPost("/websites/{id:int}/rotate-secret"), HasPermission(Permissions.Websites.Manage)]
    public async Task<IActionResult> RotateSecret(int id)
    {
        var w = await db.Websites.FindAsync(id);
        if (w is null) return NotFound();
        var secret = Keys.New("sk_live_", 32);
        w.SecretKeyHash = Keys.Hash(secret); w.SecretKeyPrefix = secret[..12];
        activity.Log("website.secret_rotated", $"Regenerated the secret API key of {w.Name}", "Website", w.Id, audit: true);
        await db.SaveChangesAsync();
        TempData["new.secret"] = secret;
        this.Success("New secret key created. Copy it now — it won't be shown again.");
        return Redirect($"/websites/{id}");
    }

    [HttpPost("/websites/{id:int}/revoke-secret"), HasPermission(Permissions.Websites.Manage)]
    public async Task<IActionResult> RevokeSecret(int id)
    {
        var w = await db.Websites.FindAsync(id);
        if (w is null) return NotFound();
        w.SecretKeyHash = null; w.SecretKeyPrefix = null;
        activity.Log("website.secret_revoked", $"Revoked the secret API key of {w.Name}", "Website", w.Id, audit: true);
        await db.SaveChangesAsync();
        this.Success("Secret key revoked.");
        return Redirect($"/websites/{id}");
    }

    [HttpPost("/websites/{id:int}/toggle"), HasPermission(Permissions.Websites.Manage)]
    public async Task<IActionResult> Toggle(int id)
    {
        var w = await db.Websites.FindAsync(id);
        if (w is null) return NotFound();
        w.IsActive = !w.IsActive;
        activity.Log("website.toggled", $"{(w.IsActive ? "Enabled" : "Disabled")} website {w.Name}", "Website", w.Id, audit: true);
        await db.SaveChangesAsync();
        this.Success(w.IsActive ? "Website enabled." : "Website disabled. Its keys are rejected until you enable it again.");
        return Redirect($"/websites/{id}");
    }

    [HttpPost("/websites/{id:int}/delete"), HasPermission(Permissions.Websites.Manage)]
    public async Task<IActionResult> Delete(int id)
    {
        var w = await db.Websites.FindAsync(id);
        if (w is null) return NotFound();
        if (await db.ContactSubmissions.AnyAsync(s => s.WebsiteId == id))
        {
            this.Error("This website has received inquiries, so it can't be deleted (that would lose their source). Disable it instead.");
            return Redirect($"/websites/{id}");
        }
        db.Websites.Remove(w);
        activity.Log("website.deleted", $"Deleted website {w.Name}", "Website", id, audit: true);
        await db.SaveChangesAsync();
        this.Success("Website deleted.");
        return Redirect("/websites");
    }

    [HttpGet("/integrations")]
    public async Task<IActionResult> Integrations()
    {
        ViewBag.ApiBase = $"{Request.Scheme}://{Request.Host}";
        return View(await db.Websites.Where(w => w.Identifier != "demo-seed").OrderBy(w => w.Name).ToListAsync());
    }

    /// <summary>One origin per line, scheme + host (+ port). The website's own URL origin is always included.</summary>
    static string NormaliseOrigins(string? raw, string siteUrl, out string? invalid)
    {
        invalid = null;
        var list = new List<string>();
        if (Uri.TryCreate(siteUrl.Trim(), UriKind.Absolute, out var site)) list.Add(site.GetLeftPart(UriPartial.Authority).ToLowerInvariant());
        foreach (var o in (raw ?? "").Split(['\n', '\r', ',', ' '], StringSplitOptions.RemoveEmptyEntries))
        {
            var t = o.Trim().TrimEnd('/');
            if (!OriginRx().IsMatch(t)) { invalid = t; continue; }
            list.Add(t.ToLowerInvariant());
        }
        return string.Join("\n", list.Distinct());
    }

    [GeneratedRegex(@"^https?://[a-zA-Z0-9.-]+(:\d{1,5})?$")] private static partial Regex OriginRx();
}
