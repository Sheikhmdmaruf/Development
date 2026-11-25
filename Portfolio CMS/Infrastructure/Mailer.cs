using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using PortfolioCms.Data;
using PortfolioCms.Models;

namespace PortfolioCms.Infrastructure;

/// <summary>Which files can be attached to an email, and where they are kept (private: App_Data, not wwwroot).</summary>
public static class MailFiles
{
    public const long MaxBytes = 30L * 1024 * 1024; // per email (and so per file)

    public static readonly Dictionary<string, string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg", [".png"] = "image/png", [".gif"] = "image/gif",
        [".doc"] = "application/msword", [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xls"] = "application/vnd.ms-excel", [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".zip"] = "application/zip", [".rar"] = "application/vnd.rar",
        [".mp4"] = "video/mp4", [".mov"] = "video/quicktime", [".avi"] = "video/x-msvideo", [".mkv"] = "video/x-matroska",
        [".webm"] = "video/webm", [".wmv"] = "video/x-ms-wmv", [".m4v"] = "video/x-m4v",
    };

    /// <summary>Shown in the composer and used for the file picker's accept list.</summary>
    public static string AcceptList => string.Join(",", Allowed.Keys);
    public const string Friendly = "JPG, JPEG, PNG, GIF, Word (DOC, DOCX), Excel (XLS, XLSX), ZIP, RAR and video (MP4, MOV, AVI, MKV, WEBM, WMV, M4V)";

    public static string Folder(IWebHostEnvironment env) => Path.Combine(env.ContentRootPath, "App_Data", "mail-attachments");

    public static string Size(long bytes) => bytes >= 1024 * 1024 ? $"{bytes / 1024d / 1024:0.#} MB" : $"{Math.Max(1, bytes / 1024)} KB";
}

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

    /// <summary>Sends a plain-text email (with optional attachments). Throws with a friendly message when it fails.</summary>
    public async Task SendAsync(MailSettings s, IEnumerable<MailAddress> to, IEnumerable<MailAddress> cc, string subject, string body,
        IEnumerable<(string Path, string FileName, string ContentType)>? files = null)
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
        foreach (var f in files ?? [])
        {
            if (!File.Exists(f.Path)) throw new InvalidOperationException($"The attachment “{f.FileName}” is missing. Remove it and attach it again.");
            var att = new Attachment(f.Path, f.ContentType);
            att.ContentDisposition!.FileName = f.FileName;
            att.Name = f.FileName;
            att.NameEncoding = System.Text.Encoding.UTF8;
            msg.Attachments.Add(att);
        }

        using var smtp = new SmtpClient(s.Host, s.Port)
        {
            EnableSsl = s.UseTls,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = msg.Attachments.Count > 0 ? 300000 : 30000, // big files need longer
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
            var all = ex.Message + " " + inner;
            if (ex.StatusCode == SmtpStatusCode.ExceededStorageAllocation || all.Contains("5.3.4") || all.Contains("size", StringComparison.OrdinalIgnoreCase) && all.Contains("exceed", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The email is too big for your mail provider. Gmail allows about 25 MB per email (attachments grow by about a third when sent), so remove or shrink a file and try again.", ex);
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
