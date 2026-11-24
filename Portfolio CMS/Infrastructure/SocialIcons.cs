namespace PortfolioCms.Infrastructure;

/// <summary>
/// Built-in icons for Social links (24×24 line icons). The website has the same list in
/// src/components/Icons.tsx (SOCIAL_ICONS) — keep the keys in step.
/// </summary>
public static class SocialIcons
{
    public static readonly (string Key, string Label, string Svg)[] All =
    [
        ("dribbble", "Dribbble", "<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M5 6.5c4 3.5 10 4 15.5 2.5M3.5 14c5-2 11-1.5 15 3.5M8.5 3.7c3.5 4.5 6 10.5 6.8 16.7\"/>"),
        ("github", "GitHub", "<path d=\"M9 19c-4 1.3-4-2-6-2.5M15 21v-3.5c0-1 .1-1.4-.5-2 2.8-.3 5.5-1.4 5.5-6a4.6 4.6 0 0 0-1.3-3.2 4.2 4.2 0 0 0-.1-3.2s-1.1-.3-3.5 1.3a12 12 0 0 0-6.2 0C6.5 2.8 5.4 3.1 5.4 3.1a4.2 4.2 0 0 0-.1 3.2A4.6 4.6 0 0 0 4 9.5c0 4.6 2.7 5.7 5.5 6-.6.6-.6 1.2-.5 2V21\"/>"),
        ("linkedin", "LinkedIn", "<rect x=\"3\" y=\"3\" width=\"18\" height=\"18\" rx=\"3\"/><path d=\"M8 10v7M8 7v.01M12 17v-4a2 2 0 0 1 4 0v4M12 10v7\"/>"),
        ("behance", "Behance", "<path d=\"M3 6h5a3 3 0 0 1 0 6H3zM3 12h6a3 3 0 0 1 0 6H3zM14.5 7.5h5M14 14h7a3.5 3.5 0 1 0-1 2.5\"/>"),
        ("instagram", "Instagram", "<rect x=\"3\" y=\"3\" width=\"18\" height=\"18\" rx=\"5\"/><circle cx=\"12\" cy=\"12\" r=\"4\"/><path d=\"M17.5 6.5h.01\"/>"),
        ("x", "X (Twitter)", "<path d=\"M4 4h4.5L20 20h-4.5zM20 4l-6.5 7.2M4 20l6.5-7.2\"/>"),
        ("facebook", "Facebook", "<path d=\"M15 3h-2.5A3.5 3.5 0 0 0 9 6.5V10H6v4h3v7h4v-7h3l1-4h-4V7a1 1 0 0 1 1-1h2z\"/>"),
        ("youtube", "YouTube", "<rect x=\"2.5\" y=\"5\" width=\"19\" height=\"14\" rx=\"4\"/><path d=\"m10 9 5 3-5 3z\"/>"),
        ("website", "Website", "<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M3 12h18M12 3a14 14 0 0 1 0 18M12 3a14 14 0 0 0 0 18\"/>"),
        ("email", "Email", "<rect x=\"3\" y=\"5\" width=\"18\" height=\"14\" rx=\"2\"/><path d=\"m3 7 9 6 9-6\"/>"),
        ("link", "Other link", "<path d=\"M10 14a4 4 0 0 0 5.7 0l3-3a4 4 0 0 0-5.7-5.7l-1 1M14 10a4 4 0 0 0-5.7 0l-3 3a4 4 0 0 0 5.7 5.7l1-1\"/>"),
    ];

    public static string Svg(string? key) => All.FirstOrDefault(i => i.Key == key).Svg ?? All[^1].Svg;

    public static IEnumerable<(string, string)> Options => All.Select(i => (i.Key, i.Label));
}
