using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace PortfolioAdmin.Web.Services;

public static partial class Slug
{
    /// <summary>Same rule as the portfolio: "Food Delivery App" -> "food-delivery-app".</summary>
    public static string From(string text) =>
        NonAlnum().Replace(text.ToLowerInvariant().Replace("&", "and"), "-").Trim('-');

    [GeneratedRegex("[^a-z0-9]+")] private static partial Regex NonAlnum();
}

public static class Keys
{
    /// <summary>Random URL-safe token with a prefix, e.g. pk_live_3fA9...</summary>
    public static string New(string prefix, int bytes = 24) =>
        prefix + Convert.ToBase64String(RandomNumberGenerator.GetBytes(bytes)).Replace('+', 'A').Replace('/', 'B').TrimEnd('=');

    public static string Hash(string key) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));

    public static bool FixedTimeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
}

/// <summary>
/// Service icons are inner SVG markup rendered as HTML on the portfolio and here.
/// Only simple shape elements and presentation attributes survive, so no script or event handler can get through.
/// </summary>
public static class SvgSanitizer
{
    static readonly HashSet<string> Elements = ["path", "rect", "circle", "ellipse", "line", "polyline", "polygon", "g"];
    static readonly HashSet<string> Attributes =
    [
        "d", "x", "y", "width", "height", "rx", "ry", "cx", "cy", "r", "x1", "y1", "x2", "y2", "points",
        "fill", "stroke", "stroke-width", "stroke-linecap", "stroke-linejoin", "opacity", "transform"
    ];

    public static string Clean(string? markup)
    {
        if (string.IsNullOrWhiteSpace(markup)) return "";
        XElement root;
        try { root = XElement.Parse($"<svg>{markup}</svg>"); }
        catch { return ""; }
        var sb = new StringBuilder();
        foreach (var el in root.Elements()) Write(el, sb);
        return sb.ToString();
    }

    static void Write(XElement el, StringBuilder sb)
    {
        var name = el.Name.LocalName;
        if (!Elements.Contains(name)) return;
        sb.Append('<').Append(name);
        foreach (var a in el.Attributes())
        {
            var an = a.Name.LocalName;
            if (!Attributes.Contains(an) || a.Value.Contains("url(", StringComparison.OrdinalIgnoreCase)) continue;
            sb.Append(' ').Append(an).Append("=\"").Append(System.Net.WebUtility.HtmlEncode(a.Value)).Append('"');
        }
        if (!el.HasElements) { sb.Append("/>"); return; }
        sb.Append('>');
        foreach (var c in el.Elements()) Write(c, sb);
        sb.Append("</").Append(name).Append('>');
    }
}

/// <summary>
/// Article body format used in the editor (one block per paragraph, separated by blank lines):
///   ## Heading      ### Small heading      > Quote      - list item (one per line)      anything else = paragraph
/// Converted to the portfolio's ArticleBlock array.
/// </summary>
public static class ArticleFormat
{
    public static List<object> ToBlocks(string? text)
    {
        var blocks = new List<object>();
        if (string.IsNullOrWhiteSpace(text)) return blocks;
        var paras = text.Replace("\r\n", "\n").Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var p in paras)
        {
            var lines = p.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (lines.All(l => l.StartsWith("- ")))
                blocks.Add(new Dictionary<string, object> { ["list"] = lines.Select(l => l[2..].Trim()).ToList() });
            else if (p.StartsWith("### ")) blocks.Add(new Dictionary<string, object> { ["h3"] = p[4..].Trim() });
            else if (p.StartsWith("## ")) blocks.Add(new Dictionary<string, object> { ["h2"] = p[3..].Trim() });
            else if (p.StartsWith("> ")) blocks.Add(new Dictionary<string, object> { ["quote"] = string.Join(" ", lines.Select(l => l.TrimStart('>', ' '))) });
            else blocks.Add(string.Join(" ", lines));
        }
        return blocks;
    }

    /// <summary>Reverse of <see cref="ToBlocks"/>, used when importing the portfolio's existing articles.</summary>
    public static string FromBlocks(System.Text.Json.JsonElement body)
    {
        var parts = new List<string>();
        foreach (var b in body.EnumerateArray())
        {
            if (b.ValueKind == System.Text.Json.JsonValueKind.String) { parts.Add(b.GetString()!); continue; }
            if (b.TryGetProperty("h2", out var h2)) parts.Add("## " + h2.GetString());
            else if (b.TryGetProperty("h3", out var h3)) parts.Add("### " + h3.GetString());
            else if (b.TryGetProperty("quote", out var q)) parts.Add("> " + q.GetString());
            else if (b.TryGetProperty("list", out var l)) parts.Add(string.Join("\n", l.EnumerateArray().Select(i => "- " + i.GetString())));
        }
        return string.Join("\n\n", parts);
    }
}

public static class Time
{
    public static string Ago(DateTime utc)
    {
        var d = DateTime.UtcNow - utc;
        if (d.TotalSeconds < 60) return "just now";
        if (d.TotalMinutes < 60) return $"{(int)d.TotalMinutes}m ago";
        if (d.TotalHours < 24) return $"{(int)d.TotalHours}h ago";
        if (d.TotalDays < 7) return $"{(int)d.TotalDays}d ago";
        return utc.ToString("d MMM yyyy");
    }
}
