namespace PortfolioCms.Infrastructure;

/// <summary>World time zones for the "Location note" dropdown in Site settings.</summary>
public static class TimeZones
{
    public const string Prefix = "Working worldwide · ";

    /// <summary>"GMT+6", "GMT+5:30", "GMT-3:30", "GMT+0".</summary>
    public static string Gmt(TimeSpan offset)
    {
        var sign = offset < TimeSpan.Zero ? "-" : "+";
        var o = offset.Duration();
        return o.Minutes == 0 ? $"GMT{sign}{o.Hours}" : $"GMT{sign}{o.Hours}:{o.Minutes:00}";
    }

    /// <summary>One option per UTC offset, west to east. Value = the text shown on the website.</summary>
    public static List<(string Value, string Label)> Options()
    {
        return TimeZoneInfo.GetSystemTimeZones()
            .GroupBy(z => z.BaseUtcOffset)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                // "(UTC+06:00) Dhaka" -> "Dhaka"; join a few city names per offset
                var cities = g.Select(z => z.DisplayName.Contains(") ") ? z.DisplayName[(z.DisplayName.IndexOf(") ") + 2)..] : z.DisplayName)
                              .Distinct().ToList();
                var names = string.Join(", ", cities);
                if (names.Length > 70) names = names[..67].TrimEnd(',', ' ') + "…";
                var gmt = Gmt(g.Key);
                return (Prefix + gmt, $"{gmt} — {names}");
            })
            .ToList();
    }
}
