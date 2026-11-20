using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace PortfolioAdmin.Web.Infrastructure;

/// <summary>
/// The built-in form tag helper only adds the CSRF token when a form has no literal action="...".
/// Every POST form here uses action="/path", so this adds the token to those forms.
/// </summary>
[HtmlTargetElement("form", Attributes = "action")]
public class AntiforgeryFormTagHelper(IHtmlGenerator generator) : TagHelper
{
    public override int Order => 1000; // after the built-in FormTagHelper

    [HtmlAttributeNotBound, ViewContext] public ViewContext ViewContext { get; set; } = null!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var method = output.Attributes["method"]?.Value?.ToString();
        if (!string.Equals(method, "post", StringComparison.OrdinalIgnoreCase)) return;
        output.PostContent.AppendHtml(generator.GenerateAntiforgery(ViewContext));
    }
}
