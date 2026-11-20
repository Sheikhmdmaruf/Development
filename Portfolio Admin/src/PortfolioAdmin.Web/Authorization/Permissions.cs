using System.Reflection;
using System.Security.Claims;

namespace PortfolioAdmin.Web.Authorization;

/// <summary>
/// Every permission in the system. A role holds a set of these as role claims of type <see cref="ClaimType"/>.
/// Modules for later phases (projects, tasks, email, reports) are defined now so roles can be configured once.
/// </summary>
public static class Permissions
{
    public const string ClaimType = "perm";
    public const string SuperAdminRole = "Super Admin";

    public static class Dashboard { public const string View = "dashboard.view"; }

    public static class Contacts
    {
        public const string View = "contacts.view";
        public const string Create = "contacts.create";
        public const string Edit = "contacts.edit";
        public const string Assign = "contacts.assign";
        public const string Delete = "contacts.delete";
        public const string Export = "contacts.export";
    }

    public static class Websites
    {
        public const string View = "websites.view";
        public const string Manage = "websites.manage";
    }

    public static class Content
    {
        public const string View = "content.view";
        public const string Edit = "content.edit";
        public const string Delete = "content.delete";
    }

    public static class Projects
    {
        public const string View = "projects.view";
        public const string Create = "projects.create";
        public const string Edit = "projects.edit";
        public const string Delete = "projects.delete";
    }

    public static class Tasks
    {
        public const string ViewAll = "tasks.view_all";
        public const string Create = "tasks.create";
        public const string Edit = "tasks.edit";
        public const string Assign = "tasks.assign";
        public const string Delete = "tasks.delete";
        public const string Review = "tasks.review";
    }

    public static class Emails
    {
        public const string View = "emails.view";
        public const string Send = "emails.send";
        public const string ManageAccounts = "emails.manage_accounts";
    }

    public static class Team
    {
        public const string View = "team.view";
        public const string Manage = "team.manage";
        public const string ViewActivity = "team.activity";
    }

    public static class Reports { public const string View = "reports.view"; }

    public static class Settings
    {
        public const string Roles = "settings.roles";
        public const string Audit = "settings.audit";
    }

    /// <summary>Permissions grouped by module, with a label for each — used by the role editor.</summary>
    public static readonly IReadOnlyList<(string Module, IReadOnlyList<(string Key, string Label)> Items)> Groups =
    [
        ("Dashboard", [(Dashboard.View, "View overview")]),
        ("Contacts", [(Contacts.View, "View contacts"), (Contacts.Create, "Add contacts"), (Contacts.Edit, "Edit, notes, tags, status"),
                      (Contacts.Assign, "Assign to members"), (Contacts.Delete, "Archive & delete"), (Contacts.Export, "Export CSV")]),
        ("Websites", [(Websites.View, "View websites"), (Websites.Manage, "Add, edit, API keys")]),
        ("Portfolio content", [(Content.View, "View content"), (Content.Edit, "Create & edit"), (Content.Delete, "Delete")]),
        ("Projects", [(Projects.View, "View all projects"), (Projects.Create, "Create"), (Projects.Edit, "Edit"), (Projects.Delete, "Delete")]),
        ("Tasks", [(Tasks.ViewAll, "View all tasks"), (Tasks.Create, "Create"), (Tasks.Edit, "Edit any task"), (Tasks.Assign, "Assign"),
                   (Tasks.Delete, "Delete"), (Tasks.Review, "Review & approve")]),
        ("Emails", [(Emails.View, "Read inbox"), (Emails.Send, "Send & reply"), (Emails.ManageAccounts, "Connect accounts")]),
        ("Team", [(Team.View, "View members"), (Team.Manage, "Invite, edit, deactivate"), (Team.ViewActivity, "View team activity")]),
        ("Reports", [(Reports.View, "View reports")]),
        ("Settings", [(Settings.Roles, "Roles & permissions"), (Settings.Audit, "Audit log")]),
    ];

    public static IEnumerable<string> All => Groups.SelectMany(g => g.Items.Select(i => i.Key));

    public static bool IsValid(string permission) => All.Contains(permission);

    /// <summary>Default permissions for the built-in roles. Super Admin implicitly has everything.</summary>
    public static readonly IReadOnlyDictionary<string, string[]> Defaults = new Dictionary<string, string[]>
    {
        [SuperAdminRole] = [.. All],
        ["Admin"] = [.. All.Where(p => p != Settings.Roles)],
        ["Project Manager"] =
        [
            Dashboard.View, Contacts.View, Contacts.Create, Contacts.Edit, Contacts.Assign, Websites.View, Content.View,
            Projects.View, Projects.Create, Projects.Edit, Tasks.ViewAll, Tasks.Create, Tasks.Edit, Tasks.Assign, Tasks.Review,
            Emails.View, Emails.Send, Team.View, Team.ViewActivity, Reports.View
        ],
        ["Team Member"] = [Dashboard.View, Tasks.Create, Team.View],
    };
}

public static class PermissionPrincipalExtensions
{
    public static bool IsSuperAdmin(this ClaimsPrincipal user) => user.IsInRole(Permissions.SuperAdminRole);

    public static bool Has(this ClaimsPrincipal user, string permission) =>
        user.Identity?.IsAuthenticated == true &&
        (user.IsSuperAdmin() || user.HasClaim(Permissions.ClaimType, permission));

    public static bool HasAny(this ClaimsPrincipal user, params string[] permissions) => permissions.Any(user.Has);

    public static string Id(this ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
}
