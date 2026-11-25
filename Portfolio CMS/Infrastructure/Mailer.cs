using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using PortfolioCms.Data;
using PortfolioCms.Models;

namespace PortfolioCms.Infrastructure;

/// <summary>
/// Sends email through the SMTP account saved on Inbox → Mail settings.
/// The SMTP password is stored encrypted (ASP.NET Data Protection; keys live in your Windows user profile).
/// </summary>
public class Mailer(CmsDbContext db, IDataProtectionProvider dp)
{
    readonly IDataProtector protector = dp.CreateProtector("PortfolioCms.Smtp.v1");

    public async Task<MailSettings> SettingsAsync() =>
        await db.MailSettings.FirstOrDefaultAsync(s => s.Id == 1) ?? new MailSettings { Id = 1 };

    public string? Protect(string? password) => string.IsNullOrEmpty(password) ? null : protector.Protect(password);

    string? Unprotect(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;
        try { return protector.Unprotect(value); }
        catch { return null; } // keys changed (e.g. another PC): ask the user to type the password again
    }

    public bool HasPassword(MailSettings s) => Unprotect(s.PasswordProtected) is not null;

    /// <summary>"a@b.com, Name &lt;c@d.com&gt;" → addresses. Returns an error message for anything invalid.</summary>
    public static (List<MailAddress> List, string? Error) ParseAddresses(string? raw, bool required)
    {
        var list = new List<MailAddress>();
        var parts = (raw ?? "").Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var p in parts)
        {
            try { list.Add(new MailAddress(p)); }
            catch { return (list, $"“{p}” isn't a valid email address."); }
        }
        if (required && list.Count == 0) return (list, "Add at least one recipient.");
        if (list.Count > 20) return (list, "You can send to at most 20 people at once.");
        return (list, null);
    }

    /// <summary>Sends a plain-text email. Throws with a friendly message when it fails.</summary>
    public async Task SendAsync(MailSettings s, IEnumerable<MailAddress> to, IEnumerable<MailAddress> cc, string subject, string body)
    {
        if (!s.IsReady) throw new InvalidOperationException("Email isn't set up yet. Open Mail settings first.");
        using var msg = new MailMessage
        {
            From = new MailAddress(s.FromEmail, string.IsNullOrWhiteSpace(s.FromName) ? null : s.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false,
            BodyEncoding = System.Text.Encoding.UTF8,
            SubjectEncoding = System.Text.Encoding.UTF8,
        };
        foreach (var a in to) msg.To.Add(a);
        foreach (var a in cc) msg.CC.Add(a);

        using var smtp = new SmtpClient(s.Host, s.Port)
        {
            EnableSsl = s.UseTls,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 30000,
        };
        var pw = Unprotect(s.PasswordProtected);
        if (!string.IsNullOrWhiteSpace(s.UserName))
        {
            if (pw is null) throw new InvalidOperationException("The email password is missing. Type it again in Mail settings.");
            smtp.UseDefaultCredentials = false;
            smtp.Credentials = new NetworkCredential(s.UserName, pw);
        }
        try
        {
            await smtp.SendMailAsync(msg);
        }
        catch (SmtpException ex)
        {
            var inner = ex.InnerException?.Message;
            var text = ex.StatusCode switch
            {
                SmtpStatusCode.MustIssueStartTlsFirst => "The server needs a secure connection. Turn on “Use TLS” and use port 587.",
                SmtpStatusCode.ClientNotPermitted or SmtpStatusCode.MailboxUnavailable when ex.Message.Contains("auth", StringComparison.OrdinalIgnoreCase)
                    => "The server refused the login. Check the username and password (Gmail needs an App Password).",
                _ => ex.Message.Contains("Authentication", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("5.7.", StringComparison.Ordinal)
                    ? "The server refused the login. Check the username and password (Gmail needs an App Password)."
                    : "Couldn’t send: " + ex.Message + (inner is null ? "" : " (" + inner + ")"),
            };
            throw new InvalidOperationException(text, ex);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException("Couldn’t reach the mail server: " + ex.Message, ex);
        }
    }
}
