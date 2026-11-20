using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using PortfolioAdmin.Web.Authorization;
using PortfolioAdmin.Web.Data;
using PortfolioAdmin.Web.Data.Entities;

namespace PortfolioAdmin.Web.Services;

/// <summary>What a website sends to POST /api/v1/submissions.</summary>
public class SubmissionInput
{
    [Required, MaxLength(150)] public string Name { get; set; } = "";
    [Required, MaxLength(256), EmailAddress] public string Email { get; set; } = "";
    [MaxLength(40)] public string? Phone { get; set; }
    [MaxLength(150)] public string? Company { get; set; }
    [MaxLength(200)] public string? Subject { get; set; }
    /// <summary>Inquiry type / service, e.g. "Website design".</summary>
    [MaxLength(80)] public string? InquiryType { get; set; }
    [Required, MaxLength(10000)] public string Message { get; set; } = "";
    [MaxLength(400)] public string? PageUrl { get; set; }
    /// <summary>Honeypot. Real visitors never fill this hidden field.</summary>
    public string? Gotcha { get; set; }
    /// <summary>Any other fields (budget, timeline...). Stored as JSON.</summary>
    public Dictionary<string, string>? Extra { get; set; }
}

public enum SubmissionOutcome { Accepted, Blocked, Duplicate }

public partial class SubmissionService(AppDbContext db, ActivityService activity, NotificationService notifications, ILogger<SubmissionService> log)
{
    public sealed record AuthResult(Website? Site, string? Error, int Status);

    /// <summary>
    /// Browser forms use the public site key (X-Site-Key) and must come from an allowed origin.
    /// Servers use the secret key (Authorization: Bearer sk_...) and can call from anywhere.
    /// </summary>
    public async Task<AuthResult> AuthenticateAsync(HttpRequest req, bool requireOrigin = true)
    {
        var auth = req.Headers.Authorization.ToString();
        if (auth.StartsWith("Bearer sk_", StringComparison.Ordinal))
        {
            var hash = Keys.Hash(auth["Bearer ".Length..].Trim());
            var bySecret = await db.Websites.FirstOrDefaultAsync(w => w.SecretKeyHash == hash);
            if (bySecret is null) return new(null, "Invalid API key.", 401);
            return bySecret.IsActive ? new(bySecret, null, 200) : new(null, "This website is disabled.", 403);
        }

        var key = req.Headers["X-Site-Key"].ToString();
        if (string.IsNullOrEmpty(key)) key = req.Query["key"].ToString();
        if (string.IsNullOrEmpty(key) || !key.StartsWith("pk_")) return new(null, "Missing site key.", 401);

        var site = await db.Websites.FirstOrDefaultAsync(w => w.PublicKey == key);
        if (site is null) return new(null, "Invalid site key.", 401);
        if (!site.IsActive) return new(null, "This website is disabled.", 403);

        if (requireOrigin)
        {
            var origin = req.Headers.Origin.ToString().TrimEnd('/').ToLowerInvariant();
            if (string.IsNullOrEmpty(origin) || !site.OriginList.Contains(origin))
                return new(null, "This origin is not allowed for the site key. Add it under Websites → Allowed origins.", 403);
        }
        return new(site, null, 200);
    }

    public async Task<(SubmissionOutcome Outcome, Contact? Contact)> SubmitAsync(Website site, SubmissionInput input, HttpContext http)
    {
        var email = input.Email.Trim().ToLowerInvariant();
        var message = input.Message.Trim();

        if (IsSpam(input, message))
        {
            site.BlockedSubmissions++;
            await db.SaveChangesAsync();
            log.LogInformation("Blocked spam submission for website {Site}", site.Identifier);
            return (SubmissionOutcome.Blocked, null);
        }

        // Same email + same message within 10 minutes = double click / resend.
        var since = DateTime.UtcNow.AddMinutes(-10);
        if (await db.ContactSubmissions.AnyAsync(s => s.Email == email && s.CreatedAt > since && s.Message == message))
            return (SubmissionOutcome.Duplicate, null);

        var contact = await db.Contacts.Where(c => c.Email == email).OrderByDescending(c => c.LastActivityAt).FirstOrDefaultAsync();
        var isNew = contact is null;
        if (contact is null)
        {
            contact = new Contact
            {
                Name = input.Name.Trim(), Email = email, Phone = input.Phone?.Trim(), Company = input.Company?.Trim(),
                InquiryType = input.InquiryType?.Trim(), Source = site.Name, FirstWebsiteId = site.Id,
            };
            db.Contacts.Add(contact);
        }
        else
        {
            contact.Phone ??= input.Phone?.Trim();
            contact.Company ??= input.Company?.Trim();
            contact.InquiryType ??= input.InquiryType?.Trim();
            if (contact.IsArchived) { contact.IsArchived = false; contact.ArchivedAt = null; }
        }
        contact.IsUnread = true;
        contact.LastActivityAt = contact.UpdatedAt = DateTime.UtcNow;

        var submission = new ContactSubmission
        {
            Contact = contact, WebsiteId = site.Id, Name = input.Name.Trim(), Email = email, Phone = input.Phone?.Trim(),
            Subject = input.Subject?.Trim(), InquiryType = input.InquiryType?.Trim(), Message = message,
            ExtraJson = input.Extra is { Count: > 0 } ? JsonSerializer.Serialize(input.Extra.Take(30).ToDictionary(k => k.Key[..Math.Min(60, k.Key.Length)], v => v.Value[..Math.Min(1000, v.Value.Length)])) : null,
            IpAddress = http.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Cut(http.Request.Headers.UserAgent.ToString(), 400),
            PageUrl = Cut(input.PageUrl ?? http.Request.Headers.Referer.ToString(), 400),
        };
        db.ContactSubmissions.Add(submission);

        site.TotalSubmissions++;
        site.LastSubmissionAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        activity.Log("submission.received", $"New inquiry from {contact.Name} via {site.Name}", "Contact", contact.Id, userId: null);
        var recipients = await notifications.UsersWithPermissionAsync(Permissions.Contacts.View);
        if (contact.AssignedToId is not null) recipients.Add(contact.AssignedToId);
        await notifications.NotifyAsync(recipients, NotificationTypes.NewInquiry,
            isNew ? $"New inquiry from {contact.Name}" : $"{contact.Name} sent another message",
            $"{site.Name} · {submission.Subject ?? submission.InquiryType ?? Cut(message, 80)}", $"/contacts/{contact.Id}");
        await db.SaveChangesAsync();

        return (SubmissionOutcome.Accepted, contact);
    }

    static bool IsSpam(SubmissionInput input, string message)
    {
        if (!string.IsNullOrEmpty(input.Gotcha)) return true;
        if (Links().Matches(message).Count > 5) return true;
        if (Links().IsMatch(input.Name)) return true;
        return false;
    }

    static string? Cut(string? s, int max) => string.IsNullOrEmpty(s) ? null : s.Length <= max ? s : s[..max];

    [GeneratedRegex(@"https?://|www\.", RegexOptions.IgnoreCase)] private static partial Regex Links();
}
