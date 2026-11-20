using System.ComponentModel.DataAnnotations;
using PortfolioAdmin.Web.Data.Entities;
using PortfolioAdmin.Web.Infrastructure;

namespace PortfolioAdmin.Web.Models;

public class ContactFilter
{
    public string? Q { get; set; }
    public LeadStatus? Status { get; set; }
    public string? Type { get; set; }
    public int? Website { get; set; }
    /// <summary>"me", "none" or a user id.</summary>
    public string? Assigned { get; set; }
    public int? Tag { get; set; }
    public bool Archived { get; set; }
    public bool Unread { get; set; }
    /// <summary>activity (default) | newest | oldest | name</summary>
    public string? Sort { get; set; }
    public int Page { get; set; } = 1;

    public bool Any => !string.IsNullOrEmpty(Q) || Status.HasValue || !string.IsNullOrEmpty(Type) || Website.HasValue ||
                       !string.IsNullOrEmpty(Assigned) || Tag.HasValue || Unread;
}

public class ContactListModel
{
    public ContactFilter Filter { get; set; } = new();
    public Paged<Contact> Result { get; set; } = null!;
    public List<Website> Websites { get; set; } = [];
    public List<AppUser> Members { get; set; } = [];
    public List<Tag> Tags { get; set; } = [];
    public List<string> Types { get; set; } = [];
    public int ActiveCount { get; set; }
    public int ArchivedCount { get; set; }
}

public class ContactFormModel
{
    public int? Id { get; set; }
    [Required, MaxLength(150)] public string Name { get; set; } = "";
    [Required, MaxLength(256), EmailAddress] public string Email { get; set; } = "";
    [MaxLength(40), Phone] public string? Phone { get; set; }
    [MaxLength(150)] public string? Company { get; set; }
    [MaxLength(80), Display(Name = "Inquiry type")] public string? InquiryType { get; set; }
    public LeadStatus Status { get; set; } = LeadStatus.New;
    [Display(Name = "Assigned to")] public string? AssignedToId { get; set; }
    [MaxLength(80)] public string? Source { get; set; }
    // Only for new contacts: the first message (e.g. from a phone call or email)
    [MaxLength(200)] public string? Subject { get; set; }
    [MaxLength(10000)] public string? Message { get; set; }
}

public class ContactDetailsModel
{
    public Contact Contact { get; set; } = null!;
    public List<TimelineEntry> Timeline { get; set; } = [];
    public List<AppUser> Members { get; set; } = [];
    public List<Tag> AllTags { get; set; } = [];
    public List<Contact> SameEmail { get; set; } = [];
}

public record TimelineEntry(DateTime At, string Kind, object Item);

public record BoardMove(int Id, LeadStatus Status, int[] Order);
