using System.ComponentModel.DataAnnotations;

namespace PortfolioAdmin.Web.Data.Entities;

/// <summary>Who did what and when. Used for timelines, Team Activity and the audit log.</summary>
public class ActivityLog
{
    public long Id { get; set; }
    public string? UserId { get; set; }
    public AppUser? User { get; set; }
    /// <summary>e.g. "contact.status_changed"</summary>
    [Required, MaxLength(80)] public string Action { get; set; } = "";
    [MaxLength(40)] public string? EntityType { get; set; }
    [MaxLength(60)] public string? EntityId { get; set; }
    [Required, MaxLength(500)] public string Summary { get; set; } = "";
    /// <summary>Sensitive actions (logins, roles, keys, deletes) are flagged for the audit view.</summary>
    public bool IsAudit { get; set; }
    [MaxLength(64)] public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Notification
{
    public long Id { get; set; }
    [Required] public string UserId { get; set; } = "";
    public AppUser User { get; set; } = null!;
    /// <summary>e.g. "contact.new", "contact.assigned"</summary>
    [Required, MaxLength(60)] public string Type { get; set; } = "";
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(500)] public string? Body { get; set; }
    [MaxLength(300)] public string? Url { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Per-user notification preferences: which types they want in-app.</summary>
public class NotificationPreference
{
    public string UserId { get; set; } = "";
    public AppUser User { get; set; } = null!;
    [MaxLength(60)] public string Type { get; set; } = "";
    public bool InApp { get; set; } = true;
}
