using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortfolioAdmin.Web.Authorization;
using PortfolioAdmin.Web.Data;
using PortfolioAdmin.Web.Models;

namespace PortfolioAdmin.Web.Controllers;

/// <summary>Global search (Ctrl+K). Each group only searches modules the user can see.</summary>
public class SearchController(AppDbContext db) : Controller
{
    [HttpGet("/search")]
    public async Task<IActionResult> Index(string q)
    {
        q = (q ?? "").Trim();
        if (q.Length < 2) return Json(Array.Empty<SearchGroup>());
        var groups = new List<SearchGroup>();

        if (User.Has(Permissions.Contacts.View))
            groups.Add(new("Contacts", await db.Contacts
                .Where(c => c.Name.Contains(q) || c.Email.Contains(q) || (c.Company != null && c.Company.Contains(q)) || c.Submissions.Any(s => s.Message.Contains(q) || (s.Subject != null && s.Subject.Contains(q))))
                .OrderByDescending(c => c.LastActivityAt).Take(6)
                .Select(c => new SearchItem(c.Name, c.Email + (c.IsArchived ? " · archived" : ""), $"/contacts/{c.Id}", "user-round")).ToListAsync()));

        if (User.Has(Permissions.Websites.View))
            groups.Add(new("Websites", await db.Websites.Where(w => w.Name.Contains(q) || w.Url.Contains(q) || w.Identifier.Contains(q)).OrderBy(w => w.Name).Take(4)
                .Select(w => new SearchItem(w.Name, w.Url, $"/websites/{w.Id}", "globe")).ToListAsync()));

        if (User.Has(Permissions.Content.View))
        {
            groups.Add(new("Portfolio projects", await db.CmsProjects.Where(p => p.Title.Contains(q) || p.Category.Contains(q) || p.Summary.Contains(q)).OrderBy(p => p.SortOrder).Take(4)
                .Select(p => new SearchItem(p.Title, p.Category + " · " + p.Year, $"/content/projects/{p.Id}", "briefcase")).ToListAsync()));
            groups.Add(new("Blog posts", await db.CmsPosts.Where(p => p.Title.Contains(q) || p.Category.Contains(q)).OrderBy(p => p.SortOrder).Take(4)
                .Select(p => new SearchItem(p.Title, p.Category, $"/content/posts/{p.Id}", "pen-line")).ToListAsync()));
            groups.Add(new("Services", await db.CmsServices.Where(p => p.Title.Contains(q) || p.Tags.Contains(q)).OrderBy(p => p.SortOrder).Take(3)
                .Select(p => new SearchItem(p.Title, p.Tags, $"/content/services/{p.Id}", "layers")).ToListAsync()));
        }

        if (User.Has(Permissions.Team.View))
            groups.Add(new("Team", await db.Users.Where(u => u.FullName.Contains(q) || u.Email!.Contains(q)).OrderBy(u => u.FullName).Take(4)
                .Select(u => new SearchItem(u.FullName, u.Email ?? "", $"/team/{u.Id}", "users")).ToListAsync()));

        return Json(groups);
    }
}
