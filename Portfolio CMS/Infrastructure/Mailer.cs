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

        // Built with MailKit/MimeKit: .NET's old System.Net.Mail.SmtpClient mishandles the Gmail login
        // and fails with "connection was closed" instead of sending or giving a real error.
        var msg = new MimeKit.MimeMessage();
        msg.From.Add(new MimeKit.MailboxAddress(string.IsNullOrWhiteSpace(s.FromName) ? "" : s.FromName, s.FromEmail));
        foreach (var a in to) msg.To.Add(new MimeKit.MailboxAddress(a.DisplayName ?? "", a.Address));
        foreach (var a in cc) msg.Cc.Add(new MimeKit.MailboxAddress(a.DisplayName ?? "", a.Address));
        msg.Subject = subject;
        var builder = new MimeKit.BodyBuilder { TextBody = body };
        var count = 0;
        foreach (var f in files ?? [])
        {
            if (!File.Exists(f.Path)) throw new InvalidOperationException($"The attachment “{f.FileName}” is missing. Remove it and attach it again.");
            builder.Attachments.Add(f.FileName, await File.ReadAllBytesAsync(f.Path), MimeKit.ContentType.Parse(f.ContentType));
            count++;
        }
        msg.Body = builder.ToMessageBody();

        var pw = Unprotect(s.PasswordProtected);
        if (!string.IsNullOrWhiteSpace(s.UserName) && pw is null)
            throw new InvalidOperationException("The email password is missing. Type it again in Mail settings.");

        // 465 = secure from the start (SSL); otherwise STARTTLS when "Use TLS" is on.
        var security = s.Port == 465 ? MailKit.Security.SecureSocketOptions.SslOnConnect
            : s.UseTls ? MailKit.Security.SecureSocketOptions.StartTls
            : MailKit.Security.SecureSocketOptions.None;

        using var smtp = new MailKit.Net.Smtp.SmtpClient { Timeout = count > 0 ? 300000 : 60000 }; // big files need longer
        try
        {
            await smtp.ConnectAsync(s.Host, s.Port, security);
            if (!string.IsNullOrWhiteSpace(s.UserName))
            {
                // Gmail shows app passwords with spaces; they aren't part of the password.
                var cred = new NetworkCredential(s.UserName, s.Host.Contains("gmail", StringComparison.OrdinalIgnoreCase) ? pw!.Replace(" ", "") : pw);
                // Log in with ONE method. If MailKit tries a second method after a refusal, Gmail hangs up
                // and the real reason ("username and password not accepted") is lost.
                if (smtp.AuthenticationMechanisms.Contains("PLAIN")) await smtp.AuthenticateAsync(new MailKit.Security.SaslMechanismPlain(cred));
                else if (smtp.AuthenticationMechanisms.Contains("LOGIN")) await smtp.AuthenticateAsync(new MailKit.Security.SaslMechanismLogin(cred));
                else await smtp.AuthenticateAsync(cred);
            }
            await smtp.SendAsync(new MailKit.SendRequest(msg));
            await smtp.DisconnectAsync(true);
        }
        catch (MailKit.Security.AuthenticationException ex)
        {
            throw new InvalidOperationException(s.Host.Contains("gmail", StringComparison.OrdinalIgnoreCase)
                ? "Gmail didn't accept the username or password. Use your full Gmail address as Username and a 16-letter App Password (not your normal Gmail password). Create a new one at Google Account → Security → App passwords, then paste it in Mail settings."
                : "The mail server didn't accept the username or password. Check them in Mail settings.", ex);
        }
        catch (MailKit.Net.Smtp.SmtpCommandException ex) when (ex.StatusCode == MailKit.Net.Smtp.SmtpStatusCode.ExceededStorageAllocation || ex.Message.Contains("5.3.4") || ex.Message.Contains("size", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The email is too big for your mail provider. Gmail allows about 25 MB per email (attachments grow by about a third when sent), so remove or shrink a file and try again.", ex);
        }
        catch (MailKit.Net.Smtp.SmtpCommandException ex)
        {
            throw new InvalidOperationException($"The mail server refused the email: {ex.Message}", ex);
        }
        catch (MailKit.Security.SslHandshakeException ex)
        {
            throw new InvalidOperationException("Couldn't make a secure connection. For Gmail use port 587 with “Use TLS” on (or port 465).", ex);
        }
        catch (Exception ex) when (ex is System.Net.Sockets.SocketException or TimeoutException or OperationCanceledException or IOException or MailKit.Net.Smtp.SmtpProtocolException)
        {
            throw new InvalidOperationException($"Couldn't talk to the mail server {s.Host}:{s.Port}. Check the server and port in Mail settings, and your internet connection. ({ex.Message})", ex);
        }
    }
}
