using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace PortfolioCms.Infrastructure;

/// <summary>Helpers to keep the edit forms short.</summary>
public static class Ui
{
    public static string E(string? s) => HtmlEncoder.Default.Encode(s ?? "");

    /// <summary>Text box + colour picker. Name can contain repeater indexes.</summary>
    public static string Color(string name, string? value) =>
        $"""<div class="color"><input type="text" name="{E(name)}" value="{E(value)}" maxlength="20" /><input type="color" tabindex="-1" aria-label="Pick colour" /></div>""";

    /// <summary>Path box + Upload button + preview.</summary>
    public static string Image(string name, string? value, string placeholder = "/uploads/… or /images/…") =>
        $"""<div class="img-field"><input type="text" name="{E(name)}" value="{E(value)}" placeholder="{E(placeholder)}" maxlength="500" /><button type="button" class="btn" data-upload>Upload</button><input type="file" accept="image/*" hidden /></div><img class="img-preview" alt="" />""";

    public static string Options(IEnumerable<(string Value, string Text)> items, string? selected) =>
        string.Concat(items.Select(i => $"""<option value="{E(i.Value)}"{(i.Value == selected ? " selected" : "")}>{E(i.Text)}</option>"""));

    public static readonly (string, string)[] ProjectTypes = [("web", "Website"), ("app", "Mobile app"), ("dash", "Dashboard")];
    public static readonly (string, string)[] MetricIcons = [("star", "Star"), ("cart", "Cart"), ("clock", "Clock"), ("grid", "Grid")];
    public static readonly (string, string)[] ServiceIcons =
        [("check", "check"), ("clock", "clock"), ("grid", "grid"), ("compass", "compass"), ("layout", "layout"), ("phone", "phone"),
         ("chart", "chart"), ("pen", "pen"), ("palette", "palette"), ("play", "play"), ("file", "file"), ("users", "users")];
    public static readonly (string, string)[] BlockKinds = [("p", "Paragraph"), ("h2", "Heading"), ("h3", "Small heading"), ("quote", "Quote"), ("list", "Bullet list (one per line)")];
    public static readonly (string, string)[] BlogCategories = [("UI/UX", "UI/UX"), ("Web Design", "Web Design"), ("Freelancing", "Freelancing")];

    /// <summary>Up / down / remove buttons for a repeater row.</summary>
    public const string RowTools =
        """<div class="rep-tools"><span class="muted small" style="margin-right:auto">#<span data-rep-num></span></span><button type="button" class="btn sm icon" data-rep-act="up" title="Move up" aria-label="Move up"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 19V5M6 11l6-6 6 6"/></svg></button><button type="button" class="btn sm icon" data-rep-act="down" title="Move down" aria-label="Move down"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 5v14M6 13l6 6 6-6"/></svg></button><button type="button" class="btn sm danger" data-rep-act="del">Remove</button></div>""";
}

/// <summary>&lt;field for="Title" span="6" hint="…"&gt; — label + input/textarea + validation, from the model property.</summary>
[HtmlTargetElement("field", TagStructure = TagStructure.WithoutEndTag)]
public class FieldTagHelper : TagHelper
{
    [HtmlAttributeName("for")] public ModelExpression For { get; set; } = null!;
    public int Span { get; set; } = 12;
    public string? Hint { get; set; }
    public string? Kind { get; set; } // text (default), area, code, color, image, number, date, check, url
    public int Rows { get; set; } = 3;
    public string? Placeholder { get; set; }
    public bool Slug { get; set; }
    public bool SlugFrom { get; set; }
    [ViewContext, HtmlAttributeNotBound] public ViewContext ViewContext { get; set; } = null!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", Span == 12 ? "f" : $"f s{Span}");

        var name = For.Name;
        var value = For.Model?.ToString() ?? "";
        var label = For.Metadata.DisplayName ?? For.Metadata.Name ?? name;
        var required = For.Metadata.IsRequired && For.Metadata.ModelType == typeof(string);
        var max = For.Metadata.ValidatorMetadata.OfType<System.ComponentModel.DataAnnotations.StringLengthAttribute>().FirstOrDefault()?.MaximumLength;
        var extra = $"{(required ? " required" : "")}{(max is int m ? $" maxlength=\"{m}\"" : "")}{(Placeholder is null ? "" : $" placeholder=\"{Ui.E(Placeholder)}\"")}{(Slug ? " data-slug" : "")}{(SlugFrom ? " data-slug-from" : "")}";
        var hasError = ViewContext.ViewData.ModelState.TryGetValue(name, out var st) && st.Errors.Count > 0;
        var cls = hasError ? " class=\"input-validation-error\"" : "";

        string control = Kind switch
        {
            "area" or "code" => $"""<textarea id="{name}" name="{name}" rows="{Rows}"{(Kind == "code" ? " class=\"code\"" : cls)}{extra}>{Ui.E(value)}</textarea>""",
            "color" => Ui.Color(name, value),
            "image" => Ui.Image(name, value),
            "number" => $"""<input type="number" id="{name}" name="{name}" value="{Ui.E(value)}"{cls} />""",
            "date" => $"""<input type="date" id="{name}" name="{name}" value="{(For.Model is DateTime d ? d.ToString("yyyy-MM-dd") : "")}"{cls} />""",
            "check" => $"""<label class="check"><input type="checkbox" name="{name}" value="true"{(For.Model is true ? " checked" : "")} /> {Ui.E(label)}</label><input type="hidden" name="{name}" value="false" />""",
            _ => $"""<input type="{(Kind == "url" ? "url" : Kind == "email" ? "email" : "text")}" id="{name}" name="{name}" value="{Ui.E(value)}"{cls}{extra} />""",
        };

        var errors = hasError ? $"""<span class="field-validation-error">{Ui.E(string.Join(" ", st!.Errors.Select(e => e.ErrorMessage)))}</span>""" : "";
        var labelHtml = Kind == "check" ? "" : $"""<label for="{name}">{Ui.E(label)}{(Hint is null ? "" : $""" <span class="hint">— {Ui.E(Hint)}</span>""")}</label>""";
        output.Content.SetHtmlContent(labelHtml + control + errors);
    }
}
