using PortfolioCms.Models;
using static PortfolioCms.Infrastructure.Ui;

namespace PortfolioCms.Infrastructure;

/// <summary>HTML for one row of each repeater list. i is the index, or "__i__" for the add-row template.</summary>
public static class Rows
{
    private static string Row(string fields) => $"""<div class="rep-row">{RowTools}<div class="rep-fields">{fields}</div></div>""";
    private static string Text(string cls, string label, string name, string? value, int max, string ph = "") =>
        $"""<div class="{cls}"><span class="label">{label}</span><input type="text" name="{name}" value="{E(value)}" maxlength="{max}" placeholder="{E(ph)}" /></div>""";
    private static string Area(string cls, string label, string name, string? value, int rows, string hint = "") =>
        $"""<div class="{cls}"><span class="label">{label}{(hint == "" ? "" : $""" <span class="hint">— {hint}</span>""")}</span><textarea name="{name}" rows="{rows}">{E(value)}</textarea></div>""";
    private static string Select(string cls, string label, string name, IEnumerable<(string, string)> items, string selected) =>
        $"""<div class="{cls}"><span class="label">{label}</span><select name="{name}">{Options(items, selected)}</select></div>""";

    public static string ProjectMetric(string i, ProjectMetric? m) => Row(
        Text("c3", "Label", $"Metrics[{i}].Label", m?.Label, 60, "Checkout") +
        Text("c3", "Value", $"Metrics[{i}].Value", m?.Value, 60, "-35%") +
        Text("c4", "Caption", $"Metrics[{i}].Caption", m?.Caption, 200, "fewer steps to place an order") +
        Select("c3", "Icon", $"Metrics[{i}].Icon", MetricIcons, m?.Icon ?? "star"));

    public static string ProjectStep(string i, ProjectStep? s) => Row(
        Text("", "Title", $"Steps[{i}].Title", s?.Title, 100, "Challenge") +
        Area("", "Text", $"Steps[{i}].Body", s?.Body, 5, "leave an empty line between paragraphs"));

    public static string ProjectImage(string i, ProjectImage? g) => Row(
        $"""<div>{Image($"Gallery[{i}].Url", g?.Url)}</div>""");

    public static string ServiceMetric(string i, ServiceMetric? m) => Row(
        Text("c4", "Label", $"Metrics[{i}].Label", m?.Label, 60, "Duration") +
        Text("c4", "Value", $"Metrics[{i}].Value", m?.Value, 80, "1–3 weeks") +
        Select("c4", "Icon", $"Metrics[{i}].Icon", ServiceIcons, m?.Icon ?? "clock"));

    public static string ServiceIncluded(string i, ServiceIncluded? m) => Row(
        Select("c3", "Icon", $"Included[{i}].Icon", ServiceIcons, m?.Icon ?? "check") +
        Text("c9", "Title", $"Included[{i}].Title", m?.Title, 100) +
        Area("", "Text", $"Included[{i}].Text", m?.Text, 2));

    public static string ServiceStep(string i, ServiceStep? m) => Row(
        Text("c4", "Title", $"Steps[{i}].Title", m?.Title, 100, "Discover") +
        Text("c8", "Text", $"Steps[{i}].Text", m?.Text, 400));

    public static string PostBlock(string i, PostBlock? b) => Row(
        Select("c3", "Block", $"Blocks[{i}].Kind", BlockKinds, b?.Kind ?? "p") +
        Area("c9", "Text", $"Blocks[{i}].Text", b?.Text, 3));
}
