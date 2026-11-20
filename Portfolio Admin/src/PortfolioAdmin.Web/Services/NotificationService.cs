using Microsoft.EntityFrameworkCore;
using PortfolioAdmin.Web.Authorization;
using PortfolioAdmin.Web.Data;
using PortfolioAdmin.Web.Data.Entities;

namespace PortfolioAdmin.Web.Services;

public static class NotificationTypes
{
    public const string NewInquiry = "contact.new";
    public const string ContactAssigned = "contact.assigned";
    public const string ContactNote = "contact.note";

    public static readonly IReadOnlyList<(string Type, string Label, string Help)> All =
    [
        (NewInquiry, "New contact inquiry", "Someone submits a contact form on one of your websites."),
        (ContactAssigned, "Contact assigned to me", "A contact or lead is assigned to you."),
        (ContactNote, "Notes on my contacts", "Someone adds a note to a contact assigned to you."),
    ];
}

/// <summary>Creates in-app notifications, honouring each user's preferences.</summary>
public class NotificationService(AppDbContext db)
{
    public async Task NotifyAsync(IEnumerable<string> userIds, string type, string title, string? body = null, string? url = null, string? exceptUserId = null)
    {
        var ids = userIds.Where(id => id != exceptUserId).Distinct().ToList();
        if (ids.Count == 0) return;

        var muted = await db.NotificationPreferences
            .Where(p => ids.Contains(p.UserId) && p.Type == type && !p.InApp)
            .Select(p => p.UserId).ToListAsync();

        foreach (var id in ids.Except(muted))
            db.Notifications.Add(new Notification
            {
                UserId = id, Type = type, Title = ActivityService.Truncate(title, 200),
                Body = body is null ? null : ActivityService.Truncate(body, 500), Url = url
            });
    }

    /// <summary>Active users whose roles grant <paramref name="permission"/> (Super Admins always included).</summary>
    public async Task<List<string>> UsersWithPermissionAsync(string permission)
    {
        var roleIds = await db.RoleClaims
            .Where(c => c.ClaimType == Permissions.ClaimType && c.ClaimValue == permission)
            .Select(c => c.RoleId)
            .Union(db.Roles.Where(r => r.Name == Permissions.SuperAdminRole).Select(r => r.Id))
            .ToListAsync();

        return await db.UserRoles.Where(ur => roleIds.Contains(ur.RoleId))
            .Join(db.Users.Where(u => u.IsActive), ur => ur.UserId, u => u.Id, (ur, u) => u.Id)
            .Distinct().ToListAsync();
    }
}
