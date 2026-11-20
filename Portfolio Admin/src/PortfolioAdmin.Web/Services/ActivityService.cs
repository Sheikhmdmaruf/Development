using System.Security.Claims;
using PortfolioAdmin.Web.Authorization;
using PortfolioAdmin.Web.Data;
using PortfolioAdmin.Web.Data.Entities;

namespace PortfolioAdmin.Web.Services;

/// <summary>Writes activity / audit entries. Entries are added to the current DbContext and saved with the caller's SaveChanges.</summary>
public class ActivityService(AppDbContext db, IHttpContextAccessor http)
{
    public ActivityLog Log(string action, string summary, string? entityType = null, object? entityId = null,
        bool audit = false, string? userId = null)
    {
        var ctx = http.HttpContext;
        var entry = new ActivityLog
        {
            Action = action,
            Summary = Truncate(summary, 500),
            EntityType = entityType,
            EntityId = entityId?.ToString(),
            IsAudit = audit,
            UserId = userId ?? (ctx?.User.Identity?.IsAuthenticated == true ? ctx.User.Id() : null),
            IpAddress = ctx?.Connection.RemoteIpAddress?.ToString(),
        };
        db.ActivityLogs.Add(entry);
        return entry;
    }

    public static string Truncate(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";

    /// <summary>Icon + colour shown in activity feeds.</summary>
    public static (string Icon, string Tone) Visual(string action) => action.Split('.')[0] switch
    {
        "contact" => ("user-round", "blue"),
        "submission" => ("inbox", "coral"),
        "website" => ("globe", "teal"),
        "content" => ("layout-template", "violet"),
        "team" => ("users", "amber"),
        "role" => ("shield-check", "amber"),
        "auth" => ("key-round", "slate"),
        "media" => ("image", "violet"),
        _ => ("activity", "slate"),
    };
}
