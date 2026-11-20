using System.ComponentModel.DataAnnotations;

namespace PortfolioAdmin.Web.Data.Entities;

public enum LeadStatus
{
    New = 0,
    Contacted = 1,
    Interested = 2,
    [Display(Name = "In Discussion")] InDiscussion = 3,
    Converted = 4,
    Rejected = 5
}

/// <summary>A website connected to the dashboard. It can send contact form submissions and read CMS content.</summary>
public class Website
{
    public int Id { get; set; }
    [Required, MaxLength(120)] public string Name { get; set; } = "";
    [Required, MaxLength(300), Url] public string Url { get; set; } = "";
    /// <summary>Short unique slug, e.g. "portfolio".</summary>
    [Required, MaxLength(60)] public string Identifier { get; set; } = "";
    /// <summary>Public site key (pk_...). Safe to put in browser code; only works from AllowedOrigins.</summary>
    [Required, MaxLength(80)] public string PublicKey { get; set; } = "";
    /// <summary>SHA-256 hash of the secret key (sk_...), used for server-to-server calls. The key itself is never stored.</summary>
    [MaxLength(128)] public string? SecretKeyHash { get; set; }
    [MaxLength(16)] public string? SecretKeyPrefix { get; set; }
    /// <summary>One origin per line, e.g. https://maruf.design. Browser submissions from other origins are rejected.</summary>
    [MaxLength(2000)] public string AllowedOrigins { get; set; } = "";
    public bool IsActive { get; set; } = true;
    /// <summary>When true, this site key can read the Portfolio CMS content API.</summary>
    public bool CanReadContent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastSubmissionAt { get; set; }
    public int TotalSubmissions { get; set; }
    public int BlockedSubmissions { get; set; }

    public List<ContactSubmission> Submissions { get; set; } = [];

    public IEnumerable<string> OriginList =>
        AllowedOrigins.Split(['\n', '\r', ',', ' '], StringSplitOptions.RemoveEmptyEntries)
            .Select(o => o.Trim().TrimEnd('/').ToLowerInvariant());
}

/// <summary>A person who contacted you. Several submissions with the same email are grouped into one contact.</summary>
public class Contact
{
    public int Id { get; set; }
    [Required, MaxLength(150)] public string Name { get; set; } = "";
    [Required, MaxLength(256), EmailAddress] public string Email { get; set; } = "";
    [MaxLength(40)] public string? Phone { get; set; }
    [MaxLength(150)] public string? Company { get; set; }
    [MaxLength(80)] public string? InquiryType { get; set; }
    public LeadStatus Status { get; set; } = LeadStatus.New;
    /// <summary>Order inside a Leads board column.</summary>
    public int BoardOrder { get; set; }
    [MaxLength(80)] public string? Source { get; set; }
    public int? FirstWebsiteId { get; set; }
    public Website? FirstWebsite { get; set; }
    public string? AssignedToId { get; set; }
    public AppUser? AssignedTo { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAt { get; set; }
    /// <summary>Unread until someone opens the contact.</summary>
    public bool IsUnread { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

    public List<ContactSubmission> Submissions { get; set; } = [];
    public List<ContactNote> Notes { get; set; } = [];
    public List<ContactTag> Tags { get; set; } = [];
}

/// <summary>One message sent through a website contact form (or added by hand).</summary>
public class ContactSubmission
{
    public long Id { get; set; }
    public int ContactId { get; set; }
    public Contact Contact { get; set; } = null!;
    public int? WebsiteId { get; set; }
    public Website? Website { get; set; }
    [MaxLength(150)] public string Name { get; set; } = "";
    [MaxLength(256)] public string Email { get; set; } = "";
    [MaxLength(40)] public string? Phone { get; set; }
    [MaxLength(200)] public string? Subject { get; set; }
    [MaxLength(80)] public string? InquiryType { get; set; }
    [MaxLength(10000)] public string Message { get; set; } = "";
    /// <summary>Any extra form fields as JSON.</summary>
    [MaxLength(4000)] public string? ExtraJson { get; set; }
    [MaxLength(64)] public string? IpAddress { get; set; }
    [MaxLength(400)] public string? UserAgent { get; set; }
    [MaxLength(400)] public string? PageUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ContactNote
{
    public int Id { get; set; }
    public int ContactId { get; set; }
    public Contact Contact { get; set; } = null!;
    public string? AuthorId { get; set; }
    public AppUser? Author { get; set; }
    [Required, MaxLength(4000)] public string Body { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Tag
{
    public int Id { get; set; }
    [Required, MaxLength(40)] public string Name { get; set; } = "";
    [MaxLength(20)] public string Color { get; set; } = "blue";
    public List<ContactTag> Contacts { get; set; } = [];
}

public class ContactTag
{
    public int ContactId { get; set; }
    public Contact Contact { get; set; } = null!;
    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
