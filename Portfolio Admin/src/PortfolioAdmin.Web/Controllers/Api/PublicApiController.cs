using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PortfolioAdmin.Web.Services;

namespace PortfolioAdmin.Web.Controllers.Api;

/// <summary>
/// Endpoints called by your websites. Not protected by the dashboard cookie: every request
/// authenticates with a website key instead (see SubmissionService.AuthenticateAsync).
/// </summary>
[ApiController, AllowAnonymous, EnableCors("public-api"), IgnoreAntiforgeryToken]
[Route("api/v1")]
public class PublicApiController(SubmissionService submissions, ContentService content) : ControllerBase
{
    static readonly HashSet<string> Known = new(StringComparer.OrdinalIgnoreCase)
        { "name", "email", "phone", "company", "subject", "inquiryType", "service", "message", "pageUrl", "gotcha", "_gotcha", "extra" };

    /// <summary>Receive a contact form submission (JSON or form-encoded).</summary>
    [HttpPost("submissions"), EnableRateLimiting("submissions"), RequestSizeLimit(64 * 1024)]
    public async Task<IActionResult> Submit()
    {
        var auth = await submissions.AuthenticateAsync(Request);
        if (auth.Site is null) return Problem(auth.Error, statusCode: auth.Status);

        SubmissionInput input;
        try { input = await ReadInputAsync(); }
        catch (JsonException) { return Problem("Body must be JSON or form data.", statusCode: 400); }

        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(input, new ValidationContext(input), errors, validateAllProperties: true))
            return ValidationProblem(new ValidationProblemDetails(errors
                .SelectMany(e => e.MemberNames.DefaultIfEmpty("").Select(m => (m, e.ErrorMessage ?? "Invalid")))
                .GroupBy(x => char.ToLowerInvariant(x.m.FirstOrDefault()) + x.m[Math.Min(1, x.m.Length)..])
                .ToDictionary(g => g.Key, g => g.Select(x => x.Item2).ToArray())));

        await submissions.SubmitAsync(auth.Site, input, HttpContext);
        // Spam and duplicates get the same reply as real submissions so bots learn nothing.
        return Ok(new { ok = true, message = "Thanks! Your message was received." });
    }

    /// <summary>Published portfolio content for sites allowed to read it.</summary>
    [HttpGet("content"), EnableRateLimiting("content")]
    public async Task<IActionResult> Content()
    {
        var auth = await submissions.AuthenticateAsync(Request, requireOrigin: false);
        if (auth.Site is null) return Problem(auth.Error, statusCode: auth.Status);
        if (!auth.Site.CanReadContent) return Problem("This website isn't allowed to read content. Turn on \"Can read portfolio content\" for it.", statusCode: 403);
        Response.Headers.CacheControl = "public, max-age=60";
        return Ok(await content.BuildPublicPayloadAsync(Request));
    }

    [HttpGet("health"), EnableRateLimiting("content")]
    public async Task<IActionResult> Health()
    {
        var auth = await submissions.AuthenticateAsync(Request, requireOrigin: false);
        if (auth.Site is null) return Problem(auth.Error, statusCode: auth.Status);
        return Ok(new { ok = true, website = auth.Site.Name, canReadContent = auth.Site.CanReadContent });
    }

    async Task<SubmissionInput> ReadInputAsync()
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var extra = new Dictionary<string, string>();

        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync();
            foreach (var (k, v) in form) values[k] = v.ToString();
        }
        else
        {
            using var doc = await JsonDocument.ParseAsync(Request.Body);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) throw new JsonException();
            foreach (var p in doc.RootElement.EnumerateObject())
            {
                if (p.NameEquals("extra") && p.Value.ValueKind == JsonValueKind.Object)
                    foreach (var e in p.Value.EnumerateObject()) extra[e.Name] = e.Value.ToString();
                else values[p.Name] = p.Value.ValueKind == JsonValueKind.String ? p.Value.GetString() ?? "" : p.Value.ToString();
            }
        }

        foreach (var (k, v) in values.Where(kv => !Known.Contains(kv.Key) && !string.IsNullOrWhiteSpace(kv.Value)))
            extra[k] = v;

        string? V(params string[] keys) => keys.Select(k => values.GetValueOrDefault(k)).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();
        return new SubmissionInput
        {
            Name = V("name") ?? "", Email = V("email") ?? "", Phone = V("phone"), Company = V("company"), Subject = V("subject"),
            InquiryType = V("inquiryType", "service"), Message = V("message") ?? "", PageUrl = V("pageUrl"),
            Gotcha = V("gotcha", "_gotcha"), Extra = extra.Count > 0 ? extra : null,
        };
    }
}
