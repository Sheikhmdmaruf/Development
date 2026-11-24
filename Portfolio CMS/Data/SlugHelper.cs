using System.Text.RegularExpressions;

namespace PortfolioCms.Data;

public static partial class SlugHelper
{
    /// <summary>Same rule as slug() in the website's src/lib.ts: "Food Delivery App" → "food-delivery-app".</summary>
    public static string Make(string title)
    {
        var s = title.ToLowerInvariant().Replace("&", "and");
        s = NonAlnum().Replace(s, "-");
        return s.Trim('-');
    }

    [GeneratedRegex("[^a-z0-9]+")] private static partial Regex NonAlnum();
}
