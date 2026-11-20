using System.ComponentModel.DataAnnotations;
using System.Reflection;
using PortfolioAdmin.Web.Data.Entities;

namespace PortfolioAdmin.Web.Infrastructure;

/// <summary>Small view helpers shared by many pages.</summary>
public static class Ui
{
    static readonly (string A, string B)[] Gradients =
    [
        ("#6d7cff", "#3949d6"), ("#ff8a65", "#e0482a"), ("#34d3a6", "#0f8f72"), ("#f6b93b", "#d17a00"),
        ("#b28bff", "#6a3fdc"), ("#4fc3f7", "#1a7fc4"), ("#f472b6", "#be2f78"), ("#94a3b8", "#4b5875"),
    ];

    /// <summary>Stable avatar colours from a name/email.</summary>
    public static string AvatarStyle(string? seed)
    {
        var h = 0;
        foreach (var c in seed ?? "") h = unchecked(h * 31 + c);
        var (a, b) = Gradients[Math.Abs(h % Gradients.Length)];
        return $"--av1:{a};--av2:{b}";
    }

    public static string Initials(string? name) =>
        string.Concat((name ?? "?").Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(p => char.ToUpperInvariant(p[0])));

    public static string Label(this Enum value) =>
        value.GetType().GetField(value.ToString())?.GetCustomAttribute<DisplayAttribute>()?.Name ?? value.ToString();

    public static readonly LeadStatus[] Statuses = Enum.GetValues<LeadStatus>();

    public static string StatusIcon(LeadStatus s) => s switch
    {
        LeadStatus.New => "sparkles",
        LeadStatus.Contacted => "send",
        LeadStatus.Interested => "star",
        LeadStatus.InDiscussion => "messages-square",
        LeadStatus.Converted => "badge-check",
        _ => "circle-x",
    };

    public static readonly string[] TagTones = ["blue", "coral", "green", "amber", "violet", "teal", "red", "slate"];

    public static readonly string[] InquiryTypes = ["Website design", "Mobile app design", "Dashboard & SaaS UI", "Brand & graphic design", "Something else"];

    public static string FileSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:0.#} KB",
        _ => $"{bytes / 1024.0 / 1024.0:0.#} MB",
    };
}

/// <summary>A page of results plus the info the pager needs.</summary>
public class Paged<T>(List<T> items, int total, int page, int size)
{
    public List<T> Items { get; } = items;
    public int Total { get; } = total;
    public int Page { get; } = page;
    public int Size { get; } = size;
    public int Pages => Math.Max(1, (int)Math.Ceiling(Total / (double)Size));
    public int From => Total == 0 ? 0 : (Page - 1) * Size + 1;
    public int To => Math.Min(Page * Size, Total);
}
