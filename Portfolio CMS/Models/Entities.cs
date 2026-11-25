using System.ComponentModel.DataAnnotations;

namespace PortfolioCms.Models;

/// <summary>Shared fields for anything that can be ordered and hidden from the website.</summary>
public abstract class Orderable
{
    public int Id { get; set; }
    [Display(Name = "Sort order")] public int SortOrder { get; set; }
    [Display(Name = "Published")] public bool IsPublished { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

// ───────────── Work / projects ─────────────

public class Project : Orderable
{
    [Required, StringLength(150)] public string Title { get; set; } = "";
    [StringLength(160), Display(Name = "Slug (URL)")] public string? Slug { get; set; }
    [Required, StringLength(10)] public string Type { get; set; } = "web";
    [Required, StringLength(60)] public string Category { get; set; } = "Website";
    [Required, StringLength(10)] public string Year { get; set; } = DateTime.Now.Year.ToString();
    [StringLength(80), Display(Name = "Highlight pill")] public string Highlight { get; set; } = "";
    [StringLength(300)] public string Summary { get; set; } = "";
    [StringLength(500), Display(Name = "Dribbble / live link")] public string Url { get; set; } = "";
    [StringLength(20), Display(Name = "Colour 1")] public string Color1 { get; set; } = "#7f7bff";
    [StringLength(20), Display(Name = "Colour 2")] public string Color2 { get; set; } = "#3b2bd6";
    [StringLength(40)] public string? Badge { get; set; }
    [StringLength(500), Display(Name = "Card image")] public string? ImageUrl { get; set; }
    [StringLength(500), Display(Name = "Cover image")] public string? CoverUrl { get; set; }

    // Case study
    [StringLength(150)] public string? Client { get; set; }
    [StringLength(150)] public string? Role { get; set; }
    [StringLength(80)] public string? Duration { get; set; }
    [StringLength(200)] public string? Tools { get; set; }
    [StringLength(200)] public string? Team { get; set; }

    public List<ProjectMetric> Metrics { get; set; } = new();
    public List<ProjectStep> Steps { get; set; } = new();
    public List<ProjectImage> Gallery { get; set; } = new();
}

public class ProjectMetric
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int SortOrder { get; set; }
    [StringLength(60)] public string Label { get; set; } = "";
    [StringLength(60)] public string Value { get; set; } = "";
    [StringLength(200)] public string Caption { get; set; } = "";
    [StringLength(30)] public string Icon { get; set; } = "star";
}

public class ProjectStep
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int SortOrder { get; set; }
    [StringLength(100)] public string Title { get; set; } = "";
    /// <summary>Paragraphs separated by a blank line.</summary>
    public string Body { get; set; } = "";
}

public class ProjectImage
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int SortOrder { get; set; }
    [StringLength(500)] public string Url { get; set; } = "";
}

// ───────────── Services ─────────────

public class Service : Orderable
{
    [Required, StringLength(120)] public string Title { get; set; } = "";
    [StringLength(140), Display(Name = "Slug (URL)")] public string? Slug { get; set; }
    [Required, StringLength(400), Display(Name = "Card text")] public string Summary { get; set; } = "";
    [StringLength(300), Display(Name = "Tags (comma separated)")] public string Tags { get; set; } = "";
    [Display(Name = "Icon (SVG inner markup)")] public string Icon { get; set; } = "";
    [StringLength(60), Display(Name = "Price (e.g. From $300)")] public string? Price { get; set; }

    // Detail page
    [StringLength(10), Display(Name = "Related work type")] public string? DetailType { get; set; }
    [StringLength(1000), Display(Name = "Detail page summary")] public string? DetailSummary { get; set; }

    public List<ServiceMetric> Metrics { get; set; } = new();
    public List<ServiceIncluded> Included { get; set; } = new();
    public List<ServiceStep> Steps { get; set; } = new();
}

public class ServiceMetric
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public int SortOrder { get; set; }
    [StringLength(60)] public string Label { get; set; } = "";
    [StringLength(80)] public string Value { get; set; } = "";
    [StringLength(30)] public string Icon { get; set; } = "clock";
}

public class ServiceIncluded
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public int SortOrder { get; set; }
    [StringLength(30)] public string Icon { get; set; } = "check";
    [StringLength(100)] public string Title { get; set; } = "";
    [StringLength(400)] public string Text { get; set; } = "";
}

public class ServiceStep
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public int SortOrder { get; set; }
    [StringLength(100)] public string Title { get; set; } = "";
    [StringLength(400)] public string Text { get; set; } = "";
}

// ───────────── Blog ─────────────

public class Post : Orderable
{
    [Required, StringLength(200)] public string Title { get; set; } = "";
    [StringLength(220), Display(Name = "Slug (URL)")] public string? Slug { get; set; }
    [Required, StringLength(40)] public string Category { get; set; } = "UI/UX";
    [DataType(DataType.Date), Display(Name = "Publish date")] public DateTime PublishedOn { get; set; } = DateTime.Today;
    [StringLength(20), Display(Name = "Read time (e.g. 6 min)")] public string ReadTime { get; set; } = "5 min";
    [StringLength(20), Display(Name = "Colour 1")] public string Color1 { get; set; } = "#7f7bff";
    [StringLength(20), Display(Name = "Colour 2")] public string Color2 { get; set; } = "#3b2bd6";
    [StringLength(10), Display(Name = "Placeholder art")] public string? Type { get; set; } = "web";
    [StringLength(500), Display(Name = "Cover image")] public string? ImageUrl { get; set; }
    [StringLength(500), Display(Name = "Outside link (Medium, LinkedIn…)")] public string? ExternalUrl { get; set; }
    [StringLength(400), Display(Name = "Article summary")] public string? ArticleSummary { get; set; }

    public List<PostBlock> Blocks { get; set; } = new();
}

public class PostBlock
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public int SortOrder { get; set; }
    /// <summary>p, h2, h3, quote or list</summary>
    [StringLength(10)] public string Kind { get; set; } = "p";
    /// <summary>For "list", one item per line.</summary>
    public string Text { get; set; } = "";
}

// ───────────── Small content ─────────────

public class Review : Orderable
{
    [Required, StringLength(600)] public string Quote { get; set; } = "";
    [Required, StringLength(80)] public string Name { get; set; } = "";
    [StringLength(80)] public string Role { get; set; } = "";
    [StringLength(80)] public string Company { get; set; } = "";
    [StringLength(20), Display(Name = "Avatar colour 1")] public string ColorA { get; set; } = "#7f7bff";
    [StringLength(20), Display(Name = "Avatar colour 2")] public string ColorB { get; set; } = "#3b2bd6";
}

public class Faq : Orderable
{
    [Required, StringLength(40)] public string Pill { get; set; } = "";
    [Required, StringLength(200)] public string Question { get; set; } = "";
    [Required, StringLength(1000)] public string Answer { get; set; } = "";
}

/// <summary>One row of the Experience timeline on the About page.</summary>
public class Experience : Orderable
{
    [Required, StringLength(40), Display(Name = "Dates (e.g. 2024 — Now)")] public string When { get; set; } = "";
    [Required, StringLength(120), Display(Name = "Job title")] public string Title { get; set; } = "";
    [StringLength(200), Display(Name = "Company / details")] public string Subtitle { get; set; } = "";
    [StringLength(80), Display(Name = "Location (e.g. Remote)")] public string Location { get; set; } = "";
    [Display(Name = "Current job (shows the “Current” tag)")] public bool IsCurrent { get; set; }
}

/// <summary>A "Connect" link (Dribbble, GitHub, LinkedIn…) shown in the website's sidebar and/or footer.</summary>
public class SocialLink : Orderable
{
    [Required, StringLength(60)] public string Name { get; set; } = "";
    [Required, StringLength(500), Display(Name = "Link (https://… or mailto:…)")] public string Url { get; set; } = "";
    [StringLength(20), Display(Name = "Built-in icon")] public string Icon { get; set; } = "link";
    [StringLength(500), Display(Name = "Or upload your own icon")] public string? IconUrl { get; set; }
    [Display(Name = "Show in the sidebar (Connect)")] public bool ShowInSidebar { get; set; } = true;
    [Display(Name = "Show in the footer")] public bool ShowInFooter { get; set; } = true;
}

public class Tool : Orderable
{
    [Required, StringLength(60)] public string Name { get; set; } = "";
    [StringLength(500), Display(Name = "Logo / image")] public string? ImageUrl { get; set; }
}

public class SiteSetting
{
    [Key, StringLength(60)] public string Key { get; set; } = "";
    [StringLength(1000)] public string Value { get; set; } = "";
    [StringLength(120)] public string Label { get; set; } = "";
}

public class Submission
{
    public int Id { get; set; }
    [StringLength(120)] public string Name { get; set; } = "";
    [StringLength(200)] public string Email { get; set; } = "";
    [StringLength(120)] public string? InquiryType { get; set; }
    [StringLength(200)] public string? Subject { get; set; }
    [StringLength(10000)] public string Message { get; set; } = "";
    [StringLength(500)] public string? PageUrl { get; set; }
    [StringLength(64)] public string? IpAddress { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AdminUser
{
    public int Id { get; set; }
    [Required, StringLength(60)] public string UserName { get; set; } = "";
    [Required] public string PasswordHash { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// ───────────── Calendar ─────────────

/// <summary>A colour-coded calendar ("Work", "Personal"…) that events belong to. Like Google's "My calendars".</summary>
public class CalendarList
{
    public int Id { get; set; }
    [Required, StringLength(60)] public string Name { get; set; } = "";
    [Required, StringLength(9)] public string Color { get; set; } = "#3d7bff";
    public bool IsVisible { get; set; } = true;
    public int SortOrder { get; set; }
    public List<CalendarEvent> Events { get; set; } = [];
}

/// <summary>One event. Times are the admin's local wall-clock time. Repeating events store the first occurrence.</summary>
public class CalendarEvent
{
    public int Id { get; set; }
    public int CalendarListId { get; set; }
    public CalendarList? Calendar { get; set; }
    [Required, StringLength(200)] public string Title { get; set; } = "";
    /// <summary>Short line under the title (e.g. "Kick-off with Acme").</summary>
    [StringLength(200)] public string? Subtitle { get; set; }
    /// <summary>Cover image: an /uploads/… path from the Media library, or an https link.</summary>
    [StringLength(500)] public string? ImageUrl { get; set; }
    [StringLength(4000)] public string? Description { get; set; }
    [StringLength(300)] public string? Location { get; set; }
    public DateTime Start { get; set; }
    /// <summary>Exclusive end. For all-day events this is midnight after the last day.</summary>
    public DateTime End { get; set; }
    public bool AllDay { get; set; }
    /// <summary>Optional colour that overrides the calendar's colour.</summary>
    [StringLength(9)] public string? Color { get; set; }
    /// <summary>none, daily, weekdays, weekly, monthly, yearly.</summary>
    [StringLength(12)] public string Repeat { get; set; } = "none";
    /// <summary>Last day a repeating event may occur on (inclusive). Null = forever.</summary>
    public DateTime? RepeatUntil { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
