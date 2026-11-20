using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortfolioAdmin.Web.Authorization;
using PortfolioAdmin.Web.Data;
using PortfolioAdmin.Web.Data.Entities;
using PortfolioAdmin.Web.Infrastructure;
using PortfolioAdmin.Web.Models;
using PortfolioAdmin.Web.Services;

namespace PortfolioAdmin.Web.Controllers;

/// <summary>Portfolio CMS: projects, services, blog posts, reviews, FAQ and site settings.</summary>
[HasPermission(Permissions.Content.View)]
public class ContentController(AppDbContext db, ActivityService activity, ContentService content) : Controller
{
    // ============================================================ shared: publish / delete / reorder

    [HttpPost("/content/{kind}/{id:int}/toggle"), HasPermission(Permissions.Content.Edit)]
    public Task<IActionResult> Toggle(string kind, int id) => kind switch
    {
        "projects" => ToggleItem(db.CmsProjects, kind, id),
        "services" => ToggleItem(db.CmsServices, kind, id),
        "posts" => ToggleItem(db.CmsPosts, kind, id),
        "reviews" => ToggleItem(db.CmsReviews, kind, id),
        "faq" => ToggleItem(db.CmsFaqs, kind, id),
        _ => Task.FromResult<IActionResult>(NotFound()),
    };

    [HttpPost("/content/{kind}/{id:int}/delete"), HasPermission(Permissions.Content.Delete)]
    public Task<IActionResult> Delete(string kind, int id) => kind switch
    {
        "projects" => DeleteItem(db.CmsProjects, kind, id),
        "services" => DeleteItem(db.CmsServices, kind, id),
        "posts" => DeleteItem(db.CmsPosts, kind, id),
        "reviews" => DeleteItem(db.CmsReviews, kind, id),
        "faq" => DeleteItem(db.CmsFaqs, kind, id),
        _ => Task.FromResult<IActionResult>(NotFound()),
    };

    [HttpPost("/content/{kind}/reorder"), HasPermission(Permissions.Content.Edit)]
    public Task<IActionResult> Reorder(string kind, [FromBody] ReorderRequest req) => kind switch
    {
        "projects" => ReorderItems(db.CmsProjects, kind, req.Ids),
        "services" => ReorderItems(db.CmsServices, kind, req.Ids),
        "posts" => ReorderItems(db.CmsPosts, kind, req.Ids),
        "reviews" => ReorderItems(db.CmsReviews, kind, req.Ids),
        "faq" => ReorderItems(db.CmsFaqs, kind, req.Ids),
        _ => Task.FromResult<IActionResult>(NotFound()),
    };

    async Task<IActionResult> ToggleItem<T>(DbSet<T> set, string kind, int id) where T : class, ICmsItem
    {
        var item = await set.FindAsync(id);
        if (item is null) return NotFound();
        item.IsPublished = !item.IsPublished;
        activity.Log("content.published", $"{(item.IsPublished ? "Published" : "Unpublished")} {item.Label}", kind, id);
        await db.SaveChangesAsync();
        this.Success(item.IsPublished ? "Published — it's live on the portfolio." : "Hidden from the portfolio.");
        return Redirect(Back($"/content/{kind}"));
    }

    async Task<IActionResult> DeleteItem<T>(DbSet<T> set, string kind, int id) where T : class, ICmsItem
    {
        var item = await set.FindAsync(id);
        if (item is null) return NotFound();
        set.Remove(item);
        activity.Log("content.deleted", $"Deleted {item.Label}", kind, id, audit: true);
        await db.SaveChangesAsync();
        this.Success("Deleted.");
        return Redirect($"/content/{kind}");
    }

    async Task<IActionResult> ReorderItems<T>(DbSet<T> set, string kind, int[] ids) where T : class, ICmsItem
    {
        var items = await set.Where(x => ids.Contains(x.Id)).ToListAsync();
        foreach (var i in items) i.SortOrder = Array.IndexOf(ids, i.Id);
        activity.Log("content.reordered", $"Reordered {kind}", kind);
        await db.SaveChangesAsync();
        return Json(new { ok = true });
    }

    string Back(string fallback)
    {
        var r = Request.Headers.Referer.ToString();
        return Uri.TryCreate(r, UriKind.Absolute, out var u) && u.Host == Request.Host.Host && Url.IsLocalUrl(u.PathAndQuery) ? u.PathAndQuery : fallback;
    }

    // ============================================================ projects

    [HttpGet("/content/projects")]
    public async Task<IActionResult> Projects() =>
        View(await db.CmsProjects.OrderBy(p => p.SortOrder).ThenBy(p => p.Id).ToListAsync());

    [HttpGet("/content/projects/new"), HasPermission(Permissions.Content.Edit)]
    public IActionResult NewProject() => View("ProjectForm", new CmsProject { Year = DateTime.UtcNow.Year.ToString() });

    [HttpGet("/content/projects/{id:int}")]
    public async Task<IActionResult> EditProject(int id) =>
        await db.CmsProjects.FindAsync(id) is { } p ? View("ProjectForm", p) : NotFound();

    [HttpPost("/content/projects/new"), HasPermission(Permissions.Content.Edit)]
    public Task<IActionResult> CreateProject(CmsProject m) => SaveProject(null, m);

    [HttpPost("/content/projects/{id:int}"), HasPermission(Permissions.Content.Edit)]
    public Task<IActionResult> UpdateProject(int id, CmsProject m) => SaveProject(id, m);

    async Task<IActionResult> SaveProject(int? id, CmsProject m)
    {
        m.Slug = Slug.From(string.IsNullOrWhiteSpace(m.Slug) ? m.Title ?? "" : m.Slug);
        ModelState.Remove(nameof(m.Slug));
        if (string.IsNullOrEmpty(m.Slug)) ModelState.AddModelError(nameof(m.Slug), "Enter a title.");
        if (m.Type is not ("web" or "app" or "dash")) ModelState.AddModelError(nameof(m.Type), "Choose a project type.");
        if (await db.CmsProjects.AnyAsync(p => p.Slug == m.Slug && p.Id != (id ?? 0))) ModelState.AddModelError(nameof(m.Slug), "Another project already uses this URL slug.");
        CheckPath(m.ImagePath, nameof(m.ImagePath)); CheckPath(m.CoverPath, nameof(m.CoverPath)); CheckUrl(m.ExternalUrl, nameof(m.ExternalUrl));
        m.MetricsJson = CheckJson(m.MetricsJson, nameof(m.MetricsJson));
        m.StepsJson = CheckJson(m.StepsJson, nameof(m.StepsJson));
        m.GalleryJson = CheckJson(m.GalleryJson, nameof(m.GalleryJson));
        if (m.GalleryJson is not null)
            foreach (var g in JsonSerializer.Deserialize<string[]>(m.GalleryJson) ?? []) CheckPath(g, nameof(m.GalleryJson));
        if (!ModelState.IsValid) { m.Id = id ?? 0; return View("ProjectForm", m); }

        var p = id is null ? new CmsProject { SortOrder = await NextOrder(db.CmsProjects) } : await db.CmsProjects.FindAsync(id);
        if (p is null) return NotFound();
        p.Title = m.Title.Trim(); p.Slug = m.Slug; p.Type = m.Type; p.Category = m.Category?.Trim() ?? ""; p.Year = m.Year?.Trim() ?? "";
        p.Highlight = m.Highlight?.Trim() ?? ""; p.Summary = m.Summary?.Trim() ?? ""; p.ExternalUrl = Clean(m.ExternalUrl);
        p.Color1 = m.Color1; p.Color2 = m.Color2; p.Badge = Clean(m.Badge); p.ImagePath = Clean(m.ImagePath); p.CoverPath = Clean(m.CoverPath);
        p.IsPublished = m.IsPublished;
        p.Client = Clean(m.Client); p.Role = Clean(m.Role); p.Duration = Clean(m.Duration); p.Tools = Clean(m.Tools); p.Team = Clean(m.Team);
        p.MetricsJson = m.MetricsJson; p.StepsJson = m.StepsJson; p.GalleryJson = m.GalleryJson; p.UpdatedAt = DateTime.UtcNow;
        if (id is null) db.CmsProjects.Add(p);
        await db.SaveChangesAsync();
        activity.Log(id is null ? "content.created" : "content.updated", $"{(id is null ? "Created" : "Updated")} project {p.Title}", "projects", p.Id);
        await db.SaveChangesAsync();
        this.Success(id is null ? "Project created." : "Project saved.");
        return Redirect($"/content/projects/{p.Id}");
    }

    // ============================================================ services

    [HttpGet("/content/services")]
    public async Task<IActionResult> Services() => View(await db.CmsServices.OrderBy(p => p.SortOrder).ThenBy(p => p.Id).ToListAsync());

    [HttpGet("/content/services/new"), HasPermission(Permissions.Content.Edit)]
    public IActionResult NewService() => View("ServiceForm", new CmsService());

    [HttpGet("/content/services/{id:int}")]
    public async Task<IActionResult> EditService(int id) =>
        await db.CmsServices.FindAsync(id) is { } s ? View("ServiceForm", s) : NotFound();

    [HttpPost("/content/services/new"), HasPermission(Permissions.Content.Edit)]
    public Task<IActionResult> CreateService(CmsService m) => SaveService(null, m);

    [HttpPost("/content/services/{id:int}"), HasPermission(Permissions.Content.Edit)]
    public Task<IActionResult> UpdateService(int id, CmsService m) => SaveService(id, m);

    async Task<IActionResult> SaveService(int? id, CmsService m)
    {
        m.Slug = Slug.From(string.IsNullOrWhiteSpace(m.Slug) ? m.Title : m.Slug);
        ModelState.Remove(nameof(m.Slug));
        if (await db.CmsServices.AnyAsync(p => p.Slug == m.Slug && p.Id != (id ?? 0))) ModelState.AddModelError(nameof(m.Slug), "Another service already uses this URL slug.");
        if (m.DetailType is not (null or "" or "web" or "app" or "dash")) ModelState.AddModelError(nameof(m.DetailType), "Choose a type.");
        m.MetricsJson = CheckJson(m.MetricsJson, nameof(m.MetricsJson));
        m.IncludedJson = CheckJson(m.IncludedJson, nameof(m.IncludedJson));
        m.StepsJson = CheckJson(m.StepsJson, nameof(m.StepsJson));
        var icon = SvgSanitizer.Clean(m.IconSvg);
        if (!string.IsNullOrWhiteSpace(m.IconSvg) && icon.Length == 0) ModelState.AddModelError(nameof(m.IconSvg), "The icon must be SVG shapes like <path d=\"…\"/>.");
        if (!ModelState.IsValid) { m.Id = id ?? 0; return View("ServiceForm", m); }

        var s = id is null ? new CmsService { SortOrder = await NextOrder(db.CmsServices) } : await db.CmsServices.FindAsync(id);
        if (s is null) return NotFound();
        s.Title = m.Title.Trim(); s.Slug = m.Slug; s.ShortText = m.ShortText?.Trim() ?? "";
        s.Tags = string.Join(", ", (m.Tags ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        s.IconSvg = icon; s.Price = Clean(m.Price); s.IsPublished = m.IsPublished;
        s.DetailType = Clean(m.DetailType); s.Summary = Clean(m.Summary); s.MetricsJson = m.MetricsJson; s.IncludedJson = m.IncludedJson; s.StepsJson = m.StepsJson;
        s.UpdatedAt = DateTime.UtcNow;
        if (id is null) db.CmsServices.Add(s);
        await db.SaveChangesAsync();
        activity.Log(id is null ? "content.created" : "content.updated", $"{(id is null ? "Created" : "Updated")} service {s.Title}", "services", s.Id);
        await db.SaveChangesAsync();
        this.Success(id is null ? "Service created." : "Service saved.");
        return Redirect($"/content/services/{s.Id}");
    }

    // ============================================================ blog posts

    [HttpGet("/content/posts")]
    public async Task<IActionResult> Posts() => View(await db.CmsPosts.OrderBy(p => p.SortOrder).ThenByDescending(p => p.PublishDate).ToListAsync());

    [HttpGet("/content/posts/new"), HasPermission(Permissions.Content.Edit)]
    public IActionResult NewPost() => View("PostForm", new CmsPost());

    [HttpGet("/content/posts/{id:int}")]
    public async Task<IActionResult> EditPost(int id) =>
        await db.CmsPosts.FindAsync(id) is { } p ? View("PostForm", p) : NotFound();

    [HttpPost("/content/posts/new"), HasPermission(Permissions.Content.Edit)]
    public Task<IActionResult> CreatePost(CmsPost m) => SavePost(null, m);

    [HttpPost("/content/posts/{id:int}"), HasPermission(Permissions.Content.Edit)]
    public Task<IActionResult> UpdatePost(int id, CmsPost m) => SavePost(id, m);

    async Task<IActionResult> SavePost(int? id, CmsPost m)
    {
        m.Slug = Slug.From(string.IsNullOrWhiteSpace(m.Slug) ? m.Title : m.Slug);
        ModelState.Remove(nameof(m.Slug));
        if (await db.CmsPosts.AnyAsync(p => p.Slug == m.Slug && p.Id != (id ?? 0))) ModelState.AddModelError(nameof(m.Slug), "Another post already uses this URL slug.");
        CheckPath(m.ImagePath, nameof(m.ImagePath)); CheckUrl(m.ExternalUrl, nameof(m.ExternalUrl));
        if (!ModelState.IsValid) { m.Id = id ?? 0; return View("PostForm", m); }

        // New posts go to the top (the portfolio shows the first 3 on the home page).
        if (id is null) await db.CmsPosts.ExecuteUpdateAsync(s => s.SetProperty(p => p.SortOrder, p => p.SortOrder + 1));
        var p = id is null ? new CmsPost { SortOrder = 0 } : await db.CmsPosts.FindAsync(id);
        if (p is null) return NotFound();
        p.Title = m.Title.Trim(); p.Slug = m.Slug; p.Category = m.Category?.Trim() ?? ""; p.PublishDate = m.PublishDate; p.ReadTime = m.ReadTime?.Trim() ?? "";
        p.Color1 = m.Color1; p.Color2 = m.Color2; p.Type = Clean(m.Type); p.ImagePath = Clean(m.ImagePath); p.ExternalUrl = Clean(m.ExternalUrl);
        p.Summary = Clean(m.Summary); p.Body = Clean(m.Body); p.IsPublished = m.IsPublished; p.UpdatedAt = DateTime.UtcNow;
        if (id is null) db.CmsPosts.Add(p);
        await db.SaveChangesAsync();
        activity.Log(id is null ? "content.created" : "content.updated", $"{(id is null ? "Created" : "Updated")} blog post {p.Title}", "posts", p.Id);
        await db.SaveChangesAsync();
        this.Success(id is null ? "Post created." : "Post saved.");
        return Redirect($"/content/posts/{p.Id}");
    }

    // ============================================================ reviews

    [HttpGet("/content/reviews")]
    public async Task<IActionResult> Reviews() => View(await db.CmsReviews.OrderBy(p => p.SortOrder).ThenBy(p => p.Id).ToListAsync());

    [HttpGet("/content/reviews/new"), HasPermission(Permissions.Content.Edit)]
    public IActionResult NewReview() => View("ReviewForm", new CmsReview());

    [HttpGet("/content/reviews/{id:int}")]
    public async Task<IActionResult> EditReview(int id) =>
        await db.CmsReviews.FindAsync(id) is { } r ? View("ReviewForm", r) : NotFound();

    [HttpPost("/content/reviews/new"), HasPermission(Permissions.Content.Edit)]
    public Task<IActionResult> CreateReview(CmsReview m) => SaveReview(null, m);

    [HttpPost("/content/reviews/{id:int}"), HasPermission(Permissions.Content.Edit)]
    public Task<IActionResult> UpdateReview(int id, CmsReview m) => SaveReview(id, m);

    async Task<IActionResult> SaveReview(int? id, CmsReview m)
    {
        if (!ModelState.IsValid) { m.Id = id ?? 0; return View("ReviewForm", m); }
        var r = id is null ? new CmsReview { SortOrder = await NextOrder(db.CmsReviews) } : await db.CmsReviews.FindAsync(id);
        if (r is null) return NotFound();
        r.Quote = m.Quote.Trim(); r.Name = m.Name.Trim(); r.Role = m.Role?.Trim() ?? ""; r.Company = m.Company?.Trim() ?? "";
        r.Color1 = m.Color1; r.Color2 = m.Color2; r.IsPublished = m.IsPublished;
        if (id is null) db.CmsReviews.Add(r);
        await db.SaveChangesAsync();
        activity.Log(id is null ? "content.created" : "content.updated", $"{(id is null ? "Added" : "Updated")} review by {r.Name}", "reviews", r.Id);
        await db.SaveChangesAsync();
        this.Success("Review saved.");
        return Redirect("/content/reviews");
    }

    // ============================================================ FAQ

    [HttpGet("/content/faq")]
    public async Task<IActionResult> Faq() => View(await db.CmsFaqs.OrderBy(p => p.SortOrder).ThenBy(p => p.Id).ToListAsync());

    [HttpGet("/content/faq/new"), HasPermission(Permissions.Content.Edit)]
    public IActionResult NewFaq() => View("FaqForm", new CmsFaq());

    [HttpGet("/content/faq/{id:int}")]
    public async Task<IActionResult> EditFaq(int id) =>
        await db.CmsFaqs.FindAsync(id) is { } f ? View("FaqForm", f) : NotFound();

    [HttpPost("/content/faq/new"), HasPermission(Permissions.Content.Edit)]
    public Task<IActionResult> CreateFaq(CmsFaq m) => SaveFaq(null, m);

    [HttpPost("/content/faq/{id:int}"), HasPermission(Permissions.Content.Edit)]
    public Task<IActionResult> UpdateFaq(int id, CmsFaq m) => SaveFaq(id, m);

    async Task<IActionResult> SaveFaq(int? id, CmsFaq m)
    {
        if (!ModelState.IsValid) { m.Id = id ?? 0; return View("FaqForm", m); }
        var f = id is null ? new CmsFaq { SortOrder = await NextOrder(db.CmsFaqs) } : await db.CmsFaqs.FindAsync(id);
        if (f is null) return NotFound();
        f.Pill = m.Pill?.Trim() ?? ""; f.Question = m.Question.Trim(); f.Answer = m.Answer.Trim(); f.IsPublished = m.IsPublished;
        if (id is null) db.CmsFaqs.Add(f);
        await db.SaveChangesAsync();
        activity.Log(id is null ? "content.created" : "content.updated", $"{(id is null ? "Added" : "Updated")} FAQ \"{f.Question}\"", "faq", f.Id);
        await db.SaveChangesAsync();
        this.Success("Question saved.");
        return Redirect("/content/faq");
    }

    // ============================================================ site settings

    [HttpGet("/content/settings")]
    public async Task<IActionResult> Settings()
    {
        ViewBag.Values = await content.GetSettingsAsync();
        ViewBag.ContentSites = await db.Websites.Where(w => w.CanReadContent && w.IsActive).ToListAsync();
        return View();
    }

    [HttpPost("/content/settings"), HasPermission(Permissions.Content.Edit)]
    public async Task<IActionResult> Settings(IFormCollection form)
    {
        foreach (var (key, _, _, multiline) in SiteKeys.Fields)
        {
            var value = form[key].ToString().Trim();
            if (value.Length > 4000) value = value[..4000];
            if (key == SiteKeys.ContactEmail && value.Length > 0 && !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(value))
            { this.Error("The contact email isn't valid."); return Redirect("/content/settings"); }
            if (key.EndsWith("_url") && value.Length > 0 && !(Uri.TryCreate(value, UriKind.Absolute, out var u) && u.Scheme is "https" or "http"))
            { this.Error($"{key.Replace('_', ' ')} must be a full link starting with https://"); return Redirect("/content/settings"); }
            await content.SetSettingAsync(key, multiline ? value.Replace("\r\n", "\n") : value);
        }
        activity.Log("content.settings", "Updated portfolio site settings", "settings");
        await db.SaveChangesAsync();
        this.Success("Site settings saved.");
        return Redirect("/content/settings");
    }

    // ============================================================ helpers

    static async Task<int> NextOrder<T>(DbSet<T> set) where T : class, ICmsItem =>
        await set.AnyAsync() ? await set.MaxAsync(x => x.SortOrder) + 1 : 0;

    static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    /// <summary>Repeater output must be a JSON array; empty means "not set".</summary>
    string? CheckJson(string? json, string field)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) throw new JsonException();
            return doc.RootElement.GetArrayLength() == 0 ? null : json;
        }
        catch (JsonException) { ModelState.AddModelError(field, "This list couldn't be read. Please re-enter it."); return json; }
    }

    /// <summary>Images may be site paths (/images/x.jpg, /uploads/...) or https links.</summary>
    void CheckPath(string? path, string field)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        var p = path.Trim();
        var ok = (p.StartsWith('/') && !p.StartsWith("//")) || (Uri.TryCreate(p, UriKind.Absolute, out var u) && u.Scheme == "https");
        if (!ok || p.Contains('"') || p.Contains('<')) ModelState.AddModelError(field, $"\"{p}\" must start with / (like /images/photo.jpg) or https://");
    }

    void CheckUrl(string? url, string field)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        if (!(Uri.TryCreate(url.Trim(), UriKind.Absolute, out var u) && u.Scheme is "https" or "http"))
            ModelState.AddModelError(field, "Enter a full link starting with https://");
    }
}
