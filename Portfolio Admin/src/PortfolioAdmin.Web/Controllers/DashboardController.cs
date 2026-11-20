using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortfolioAdmin.Web.Authorization;
using PortfolioAdmin.Web.Data;
using PortfolioAdmin.Web.Data.Entities;

namespace PortfolioAdmin.Web.Controllers;

public class OverviewModel
{
    public int TotalContacts, NewInquiries30, NewInquiriesPrev30, Unread, Converted, OpenLeads, Websites, ActiveWebsites, TeamMembers, Submissions30;
    public int Projects, Posts, Services;
    public Dictionary<LeadStatus, int> Pipeline = [];
    public List<(string Label, int Count)> Monthly = [];
    public List<(string Label, int Count)> BySite = [];
    public List<(string Label, int Count)> ByType = [];
    public List<Contact> RecentContacts = [];
    public List<ActivityLog> Activity = [];
    public bool CanContacts, CanContent, CanTeam, CanWebsites;
}

public class DashboardController(AppDbContext db) : Controller
{
    [HttpGet("/"), HasPermission(Permissions.Dashboard.View)]
    public async Task<IActionResult> Index()
    {
        var now = DateTime.UtcNow;
        var m = new OverviewModel
        {
            CanContacts = User.Has(Permissions.Contacts.View), CanContent = User.Has(Permissions.Content.View),
            CanTeam = User.Has(Permissions.Team.View), CanWebsites = User.Has(Permissions.Websites.View),
        };

        if (m.CanContacts)
        {
            var live = db.Contacts.Where(c => !c.IsArchived);
            m.TotalContacts = await live.CountAsync();
            m.Unread = await live.CountAsync(c => c.IsUnread);
            m.Converted = await live.CountAsync(c => c.Status == LeadStatus.Converted);
            m.Pipeline = await live.GroupBy(c => c.Status).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N);
            m.OpenLeads = m.Pipeline.Where(p => p.Key is not (LeadStatus.Converted or LeadStatus.Rejected)).Sum(p => p.Value);
            m.NewInquiries30 = await db.Contacts.CountAsync(c => c.CreatedAt >= now.AddDays(-30));
            m.NewInquiriesPrev30 = await db.Contacts.CountAsync(c => c.CreatedAt >= now.AddDays(-60) && c.CreatedAt < now.AddDays(-30));
            m.Submissions30 = await db.ContactSubmissions.CountAsync(s => s.CreatedAt >= now.AddDays(-30));

            var start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-11);
            var monthly = await db.ContactSubmissions.Where(s => s.CreatedAt >= start)
                .GroupBy(s => new { s.CreatedAt.Year, s.CreatedAt.Month }).Select(g => new { g.Key.Year, g.Key.Month, N = g.Count() }).ToListAsync();
            for (var d = start; d <= now; d = d.AddMonths(1))
                m.Monthly.Add((d.ToString("MMM"), monthly.FirstOrDefault(x => x.Year == d.Year && x.Month == d.Month)?.N ?? 0));

            m.BySite = (await db.ContactSubmissions.Where(s => s.CreatedAt >= now.AddDays(-365))
                    .GroupBy(s => s.Website != null ? s.Website.Name : "Added manually").Select(g => new { g.Key, N = g.Count() })
                    .OrderByDescending(x => x.N).Take(6).ToListAsync())
                .Select(x => (x.Key, x.N)).ToList();

            m.ByType = (await live.GroupBy(c => c.InquiryType ?? "Not set").Select(g => new { g.Key, N = g.Count() })
                    .OrderByDescending(x => x.N).Take(6).ToListAsync())
                .Select(x => (x.Key, x.N)).ToList();

            m.RecentContacts = await live.Include(c => c.FirstWebsite).OrderByDescending(c => c.LastActivityAt).Take(6).ToListAsync();
        }

        if (m.CanWebsites)
        {
            m.Websites = await db.Websites.CountAsync(w => w.Identifier != "demo-seed");
            m.ActiveWebsites = await db.Websites.CountAsync(w => w.IsActive && w.Identifier != "demo-seed");
        }
        if (m.CanContent)
        {
            m.Projects = await db.CmsProjects.CountAsync(p => p.IsPublished);
            m.Posts = await db.CmsPosts.CountAsync(p => p.IsPublished);
            m.Services = await db.CmsServices.CountAsync(p => p.IsPublished);
        }
        if (m.CanTeam) m.TeamMembers = await db.Users.CountAsync(u => u.IsActive);

        var activity = db.ActivityLogs.Include(a => a.User).Where(a => !a.IsAudit);
        if (!User.Has(Permissions.Team.ViewActivity)) { var me = User.Id(); activity = activity.Where(a => a.UserId == me); }
        m.Activity = await activity.OrderByDescending(a => a.CreatedAt).Take(8).ToListAsync();

        return View(m);
    }

    [HttpGet("/workspace")]
    public async Task<IActionResult> Workspace()
    {
        var me = User.Id();
        ViewBag.Assigned = await db.Contacts.Include(c => c.FirstWebsite)
            .Where(c => c.AssignedToId == me && !c.IsArchived).OrderByDescending(c => c.LastActivityAt).Take(20).ToListAsync();
        ViewBag.Notifications = await db.Notifications.Where(n => n.UserId == me).OrderByDescending(n => n.CreatedAt).Take(8).ToListAsync();
        ViewBag.Activity = await db.ActivityLogs.Include(a => a.User).Where(a => a.UserId == me && !a.IsAudit).OrderByDescending(a => a.CreatedAt).Take(10).ToListAsync();
        return View();
    }
}
