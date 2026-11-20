using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortfolioAdmin.Web.Authorization;
using PortfolioAdmin.Web.Data;
using PortfolioAdmin.Web.Data.Entities;

namespace PortfolioAdmin.Web.ViewComponents;

public record NavItem(string Label, string Href, string Icon, int? Count = null, bool SoftCount = false);
public record NavGroup(string Label, List<NavItem> Items);

/// <summary>Sidebar navigation. Only shows modules the user has permission for (and modules that exist).</summary>
public class SidebarViewComponent(AppDbContext db) : ViewComponent
{
    /// <param name="variant">"side" (dock / sidebar), "top" (top navigation) or "cmd" (bottom command bar).</param>
    public async Task<IViewComponentResult> InvokeAsync(string variant = "side")
    {
        var u = UserClaimsPrincipal;
        var groups = new List<NavGroup>();

        var main = new List<NavItem> { new("Overview", "/", "layout-dashboard") };
        main.Add(new("My Workspace", "/workspace", "user-round-check"));
        groups.Add(new("Main", main));

        if (u.Has(Permissions.Contacts.View))
        {
            var unread = await db.Contacts.CountAsync(c => c.IsUnread && !c.IsArchived);
            var open = await db.Contacts.CountAsync(c => !c.IsArchived && c.Status != LeadStatus.Converted && c.Status != LeadStatus.Rejected);
            var crm = new List<NavItem>
            {
                new("Contacts", "/contacts", "contact", unread > 0 ? unread : null),
                new("Leads", "/leads", "kanban", open > 0 ? open : null, SoftCount: true),
            };
            if (u.Has(Permissions.Websites.View)) crm.Add(new("Websites", "/websites", "globe"));
            groups.Add(new("CRM", crm));
        }
        else if (u.Has(Permissions.Websites.View))
            groups.Add(new("CRM", [new("Websites", "/websites", "globe")]));

        if (u.Has(Permissions.Content.View))
            groups.Add(new("Portfolio Content", [
                new("Projects", "/content/projects", "briefcase"),
                new("Services", "/content/services", "layers"),
                new("Blog Posts", "/content/posts", "pen-line"),
                new("Reviews", "/content/reviews", "quote"),
                new("FAQ", "/content/faq", "circle-help"),
                new("Site Settings", "/content/settings", "sliders-horizontal"),
            ]));

        groups.Add(new("Communication", [new("Notifications", "/notifications", "bell")]));

        var team = new List<NavItem>();
        if (u.Has(Permissions.Team.View)) team.Add(new("Team Members", "/team", "users"));
        if (u.Has(Permissions.Team.ViewActivity)) team.Add(new("Team Activity", "/team/activity", "activity"));
        if (team.Count > 0) groups.Add(new("Team", team));

        var system = new List<NavItem>();
        if (u.Has(Permissions.Websites.View)) system.Add(new("Integrations", "/integrations", "plug"));
        if (u.Has(Permissions.Settings.Roles)) system.Add(new("Roles & Permissions", "/settings/roles", "shield-check"));
        if (u.Has(Permissions.Settings.Audit)) system.Add(new("Audit Log", "/settings/audit", "scroll-text"));
        system.Add(new("Settings", "/account/profile", "settings"));
        groups.Add(new("System", system));

        return variant switch
        {
            "top" => View("Top", groups),
            "cmd" => View("Cmd", groups),
            _ => View(groups),
        };
    }
}

public class UnreadDotViewComponent(AppDbContext db) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var id = UserClaimsPrincipal.Id();
        var n = await db.Notifications.CountAsync(x => x.UserId == id && !x.IsRead);
        return new Microsoft.AspNetCore.Mvc.ViewComponents.HtmlContentViewComponentResult(
            new Microsoft.AspNetCore.Html.HtmlString(n > 0 ? $"<span class=\"dot\">{(n > 99 ? "99+" : n.ToString())}</span>" : ""));
    }
}
