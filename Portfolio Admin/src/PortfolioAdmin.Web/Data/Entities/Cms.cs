using System.ComponentModel.DataAnnotations;

namespace PortfolioAdmin.Web.Data.Entities;

/*  Portfolio CMS — everything the portfolio website shows.
    Lists inside an item (metrics, steps, gallery...) are stored as JSON
    because they are always read and written together with the item.   */

/// <summary>Common shape of every CMS item: ordering, publishing and a display label.</summary>
public interface ICmsItem
{
    int Id { get; }
    int SortOrder { get; set; }
    bool IsPublished { get; set; }
    string Label { get; }
}

public class CmsProject : ICmsItem
{
    public string Label => Title;
    public int Id { get; set; }
    [Required, MaxLength(150)] public string Title { get; set; } = "";
    [Required, MaxLength(160)] public string Slug { get; set; } = "";
    /// <summary>web | app | dash</summary>
    [Required, MaxLength(10)] public string Type { get; set; } = "web";
    [MaxLength(60)] public string Category { get; set; } = "";
    [MaxLength(10)] public string Year { get; set; } = "";
    [MaxLength(80)] public string Highlight { get; set; } = "";
    [MaxLength(300)] public string Summary { get; set; } = "";
    [MaxLength(400)] public string? ExternalUrl { get; set; }
    [MaxLength(20)] public string Color1 { get; set; } = "#7f7bff";
    [MaxLength(20)] public string Color2 { get; set; } = "#3b2bd6";
    [MaxLength(40)] public string? Badge { get; set; }
    [MaxLength(400)] public string? ImagePath { get; set; }
    [MaxLength(400)] public string? CoverPath { get; set; }
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; } = true;

    // Case study
    [MaxLength(150)] public string? Client { get; set; }
    [MaxLength(150)] public string? Role { get; set; }
    [MaxLength(80)] public string? Duration { get; set; }
    [MaxLength(200)] public string? Tools { get; set; }
    [MaxLength(200)] public string? Team { get; set; }
    /// <summary>[{label,value,cap,icon}]</summary>
    public string? MetricsJson { get; set; }
    /// <summary>[{title, body:[...]}]</summary>
    public string? StepsJson { get; set; }
    /// <summary>["/images/a.jpg", ...]</summary>
    public string? GalleryJson { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class CmsService : ICmsItem
{
    public string Label => Title;
    public int Id { get; set; }
    [Required, MaxLength(120)] public string Title { get; set; } = "";
    [Required, MaxLength(140)] public string Slug { get; set; } = "";
    [MaxLength(400)] public string ShortText { get; set; } = "";
    /// <summary>Comma separated tags.</summary>
    [MaxLength(400)] public string Tags { get; set; } = "";
    /// <summary>Inner SVG markup of a 24x24 stroke icon. Only path/rect/circle/line/polyline/polygon/ellipse elements are kept.</summary>
    [MaxLength(4000)] public string IconSvg { get; set; } = "";
    [MaxLength(60)] public string? Price { get; set; }
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; } = true;

    // Service detail page
    [MaxLength(10)] public string? DetailType { get; set; }
    [MaxLength(2000)] public string? Summary { get; set; }
    /// <summary>[[label, value, icon]]</summary>
    public string? MetricsJson { get; set; }
    /// <summary>[[icon, title, text]]</summary>
    public string? IncludedJson { get; set; }
    /// <summary>[[title, text]]</summary>
    public string? StepsJson { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class CmsPost : ICmsItem
{
    public string Label => Title;
    public int Id { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    [Required, MaxLength(220)] public string Slug { get; set; } = "";
    [MaxLength(60)] public string Category { get; set; } = "UI/UX";
    public DateOnly PublishDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    [MaxLength(20)] public string ReadTime { get; set; } = "5 min";
    [MaxLength(20)] public string Color1 { get; set; } = "#7f7bff";
    [MaxLength(20)] public string Color2 { get; set; } = "#3b2bd6";
    [MaxLength(10)] public string? Type { get; set; }
    [MaxLength(400)] public string? ImagePath { get; set; }
    /// <summary>Link to an outside article. When set, the post opens that link instead of an article page.</summary>
    [MaxLength(400)] public string? ExternalUrl { get; set; }
    [MaxLength(400)] public string? Summary { get; set; }
    /// <summary>Simple text format: "## h2", "### h3", "> quote", "- list item", blank line between paragraphs.</summary>
    public string? Body { get; set; }
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class CmsReview : ICmsItem
{
    public string Label => "Review by " + Name;
    public int Id { get; set; }
    [Required, MaxLength(600)] public string Quote { get; set; } = "";
    [Required, MaxLength(100)] public string Name { get; set; } = "";
    [MaxLength(100)] public string Role { get; set; } = "";
    [MaxLength(100)] public string Company { get; set; } = "";
    [MaxLength(20)] public string Color1 { get; set; } = "#7f7bff";
    [MaxLength(20)] public string Color2 { get; set; } = "#3b2bd6";
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; } = true;
}

public class CmsFaq : ICmsItem
{
    public string Label => Question;
    public int Id { get; set; }
    [MaxLength(40)] public string Pill { get; set; } = "";
    [Required, MaxLength(300)] public string Question { get; set; } = "";
    [Required, MaxLength(2000)] public string Answer { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; } = true;
}

/// <summary>Key/value settings (contact email, tools list...).</summary>
public class SiteSetting
{
    [Key, MaxLength(80)] public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>An uploaded image, served from /uploads.</summary>
public class MediaFile
{
    public int Id { get; set; }
    [Required, MaxLength(200)] public string OriginalName { get; set; } = "";
    [Required, MaxLength(300)] public string Path { get; set; } = "";
    [MaxLength(100)] public string ContentType { get; set; } = "";
    public long Size { get; set; }
    public string? UploadedById { get; set; }
    public AppUser? UploadedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
