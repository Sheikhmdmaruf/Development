using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortfolioCms.Data;
using PortfolioCms.Infrastructure;
using PortfolioCms.Models;

namespace PortfolioCms.Controllers;

/// <summary>One row in the middle (message list) pane.</summary>
public record InboxItem(string Kind, int Id, string Name, string Email, string Subject, string Preview, DateTime At,
    bool Unread, bool Flagged, bool Replied, string? Status);

/// <summary>The reply / forward / new-mail form shown in the reading pane.</summary>
public record ComposeVm(string Mode, int? SubmissionId, string To, string Cc, string Subject, string Body);

public record InboxVm(string Folder, string? Q, List<InboxItem> Items, Submission? Message, OutgoingMail? Sent,
    List<OutgoingMail> Thread, Dictionary<string, int> Counts, MailSettings Settings, ComposeVm? Compose);

/// <summary>Outlook-style inbox: folders · message list · reading pane. Reply to website messages and send new mail.</summary>
public class InboxController(CmsDbContext db, Mailer mailer) : Controller
{
    public static readonly string[] Folders = ["inbox", "unread", "flagged", "sent", "archive", "deleted"];

    public record SendForm(int? SubmissionId, string? To, string? Cc, string? Subject, string? Body, string? Mode);
    public record SettingsForm(string? Host, int Port, bool UseTls, string? UserName, string? Password, bool ClearPassword,
        string? FromEmail, string? FromName, string? Signature);

    static string SubjectOf(Submission s) =>
        !string.IsNullOrWhiteSpace(s.Subject) ? s.Subject! : !string.IsNullOrWhiteSpace(s.InquiryType) ? s.InquiryType! : "Message from your website";

    static string Preview(string text) { var t = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)); return t.Length > 140 ? t[..140] + "…" : t; }

    // ───────── the 3-pane page ─────────

    public async Task<IActionResult> Index(string? folder, int? id, string? q, string? compose, int? from)
    {
        folder = Folders.Contains(folder) ? folder! : "inbox";
        q = string.IsNullOrWhiteSpace(q) ? null : q.Trim();

        var items = new List<InboxItem>();
        if (folder == "sent")
        {
            var sent = db.OutgoingMails.AsNoTracking();
            if (q is not null) sent = sent.Where(m => m.To.Contains(q) || m.Subject.Contains(q) || m.Body.Contains(q));
            items = (await sent.OrderByDescending(m => m.SentAt).Take(300).ToListAsync())
                .Select(m => new InboxItem("sent", m.Id, "To: " + m.To, m.To, m.Subject, Preview(m.Body), m.SentAt, false, false, false, m.Status))
                .ToList();
        }
        else
        {
            var msgs = db.Submissions.AsNoTracking();
            msgs = folder switch
            {
                "deleted" => msgs.Where(s => s.DeletedAt != null),
                "archive" => msgs.Where(s => s.DeletedAt == null && s.IsArchived),
                "unread" => msgs.Where(s => s.DeletedAt == null && !s.IsArchived && !s.IsRead),
                "flagged" => msgs.Where(s => s.DeletedAt == null && s.IsFlagged),
                _ => msgs.Where(s => s.DeletedAt == null && !s.IsArchived),
            };
            if (q is not null) msgs = msgs.Where(s => s.Name.Contains(q) || s.Email.Contains(q) || s.Message.Contains(q)
                || (s.Subject != null && s.Subject.Contains(q)) || (s.InquiryType != null && s.InquiryType.Contains(q)));
            items = (await msgs.OrderByDescending(s => s.CreatedAt).Take(300).ToListAsync())
                .Select(s => new InboxItem("msg", s.Id, s.Name, s.Email, SubjectOf(s), Preview(s.Message), s.CreatedAt, !s.IsRead, s.IsFlagged, s.RepliedAt != null, null))
                .ToList();
        }

        Submission? message = null; OutgoingMail? sentMail = null; var thread = new List<OutgoingMail>();
        if (id is int mid)
        {
            if (folder == "sent")
            {
                sentMail = await db.OutgoingMails.Include(m => m.Submission).FirstOrDefaultAsync(m => m.Id == mid);
            }
            else
            {
                message = await db.Submissions.FirstOrDefaultAsync(s => s.Id == mid);
                if (message is not null)
                {
                    if (!message.IsRead && compose is null) { message.IsRead = true; await db.SaveChangesAsync(); }
                    thread = await db.OutgoingMails.AsNoTracking().Where(m => m.SubmissionId == mid).OrderBy(m => m.SentAt).ToListAsync();
                    var row = items.FindIndex(i => i.Id == mid);
                    if (row >= 0) items[row] = items[row] with { Unread = !message.IsRead };
                }
            }
        }

        var settings = await mailer.SettingsAsync();
        ComposeVm? form = null;
        if (compose == "new") form = new ComposeVm("new", null, "", "", "", Sig(settings));
        else if (compose is "reply" or "forward" && message is not null)
        {
            var quoted = $"\n\n{Sig(settings)}\n\n________________________________\nFrom: {message.Name} <{message.Email}>\nSent: {message.CreatedAt.ToLocalTime():dddd, d MMMM yyyy h:mm tt}\nSubject: {SubjectOf(message)}\n\n{message.Message}";
            var subj = SubjectOf(message);
            form = compose == "reply"
                ? new ComposeVm("reply", message.Id, message.Email, "", subj.StartsWith("RE:", StringComparison.OrdinalIgnoreCase) ? subj : "RE: " + subj, quoted)
                : new ComposeVm("forward", message.Id, "", "", "FW: " + subj, quoted);
        }
        else if (compose == "resend" && from is int fid && await db.OutgoingMails.AsNoTracking().FirstOrDefaultAsync(m => m.Id == fid) is { } old)
        {
            form = new ComposeVm("resend", old.SubmissionId, old.To, old.Cc ?? "", old.Subject, old.Body);
        }
        else if (compose == "draft" && TempData["draft"] is string json)
        {
            try { form = System.Text.Json.JsonSerializer.Deserialize<ComposeVm>(json); } catch { form = null; }
        }

        var counts = new Dictionary<string, int>
        {
            ["inbox"] = await db.Submissions.CountAsync(s => s.DeletedAt == null && !s.IsArchived && !s.IsRead),
            ["unread"] = await db.Submissions.CountAsync(s => s.DeletedAt == null && !s.IsArchived && !s.IsRead),
            ["flagged"] = await db.Submissions.CountAsync(s => s.DeletedAt == null && s.IsFlagged),
            ["sent"] = await db.OutgoingMails.CountAsync(m => m.Status == "failed"),
            ["archive"] = 0,
            ["deleted"] = await db.Submissions.CountAsync(s => s.DeletedAt != null),
        };

        return View(new InboxVm(folder, q, items, message, sentMail, thread, counts, settings, form));
    }

    static string Sig(MailSettings s) => string.IsNullOrWhiteSpace(s.Signature) ? "" : s.Signature.Trim();

    /// <summary>Old links (dashboard, emails) → the reading pane.</summary>
    public IActionResult Open(int id) => RedirectToAction(nameof(Index), new { id });

    IActionResult Back(string? folder, int? id = null, string? q = null) =>
        RedirectToAction(nameof(Index), new { folder = Folders.Contains(folder) ? folder : "inbox", id, q });

    // ───────── message actions ─────────

    [HttpPost]
    public async Task<IActionResult> MarkUnread(int id, string? folder)
    {
        var s = await db.Submissions.FindAsync(id);
        if (s is not null) { s.IsRead = false; await db.SaveChangesAsync(); }
        return Back(folder); // keep it unread: don't reopen it
    }

    [HttpPost]
    public async Task<IActionResult> MarkRead(int id, string? folder)
    {
        var s = await db.Submissions.FindAsync(id);
        if (s is not null) { s.IsRead = true; await db.SaveChangesAsync(); }
        return Back(folder, id);
    }

    [HttpPost]
    public async Task<IActionResult> MarkAllRead(string? folder)
    {
        await db.Submissions.Where(s => !s.IsRead && s.DeletedAt == null).ExecuteUpdateAsync(u => u.SetProperty(s => s.IsRead, true));
        TempData["ok"] = "All messages marked as read.";
        return Back(folder);
    }

    [HttpPost]
    public async Task<IActionResult> Flag(int id, string? folder, bool stay = true)
    {
        var s = await db.Submissions.FindAsync(id);
        if (s is not null) { s.IsFlagged = !s.IsFlagged; await db.SaveChangesAsync(); }
        return Back(folder, stay ? id : null);
    }

    [HttpPost]
    public async Task<IActionResult> Archive(int id, string? folder)
    {
        var s = await db.Submissions.FindAsync(id);
        if (s is not null)
        {
            s.IsArchived = !s.IsArchived; s.DeletedAt = null; await db.SaveChangesAsync();
            TempData["ok"] = s.IsArchived ? "Moved to Archive." : "Moved back to Inbox.";
        }
        return Back(folder);
    }

    /// <summary>First delete → Deleted folder. Delete again from there → gone for good.</summary>
    [HttpPost]
    public async Task<IActionResult> Delete(int id, string? folder)
    {
        var s = await db.Submissions.FindAsync(id);
        if (s is null) return Back(folder);
        if (s.DeletedAt is null) { s.DeletedAt = DateTime.UtcNow; TempData["ok"] = "Moved to Deleted items."; }
        else { db.Submissions.Remove(s); TempData["ok"] = "Message permanently deleted."; }
        await db.SaveChangesAsync();
        return Back(folder);
    }

    [HttpPost]
    public async Task<IActionResult> Restore(int id, string? folder)
    {
        var s = await db.Submissions.FindAsync(id);
        if (s is not null) { s.DeletedAt = null; s.IsArchived = false; await db.SaveChangesAsync(); TempData["ok"] = "Message restored to Inbox."; }
        return Back("inbox", id);
    }

    [HttpPost]
    public async Task<IActionResult> EmptyDeleted()
    {
        var n = await db.Submissions.Where(s => s.DeletedAt != null).ExecuteDeleteAsync();
        TempData["ok"] = n == 0 ? "Deleted items is already empty." : $"{n} message{(n == 1 ? "" : "s")} permanently deleted.";
        return Back("deleted");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteSent(int id)
    {
        var m = await db.OutgoingMails.FindAsync(id);
        if (m is not null) { db.OutgoingMails.Remove(m); await db.SaveChangesAsync(); TempData["ok"] = "Removed from Sent items."; }
        return Back("sent");
    }

    // ───────── sending ─────────

    [HttpPost, RequestSizeLimit(256 * 1024)]
    public async Task<IActionResult> Send(SendForm f)
    {
        var settings = await mailer.SettingsAsync();
        var (to, toErr) = Mailer.ParseAddresses(f.To, true);
        var (cc, ccErr) = Mailer.ParseAddresses(f.Cc, false);
        var subject = (f.Subject ?? "").Trim();
        var body = f.Body ?? "";
        var error = toErr ?? ccErr
            ?? (subject.Length == 0 ? "Add a subject." : subject.Length > 300 ? "The subject is too long." : null)
            ?? (string.IsNullOrWhiteSpace(body) ? "Write a message first." : body.Length > 50000 ? "The message is too long." : null)
            ?? (!settings.IsReady ? "Email isn't set up yet. Open Mail settings first." : null);
        Submission? original = f.SubmissionId is int sid ? await db.Submissions.FindAsync(sid) : null;
        if (error is not null)
        {
            TempData["err"] = error;
            TempData["draft"] = System.Text.Json.JsonSerializer.Serialize(new ComposeVm(f.Mode ?? "new", f.SubmissionId, f.To ?? "", f.Cc ?? "", subject, body));
            return original is not null
                ? RedirectToAction(nameof(Index), new { id = original.Id, compose = "draft" })
                : RedirectToAction(nameof(Index), new { compose = "draft" });
        }

        var mail = new OutgoingMail
        {
            SubmissionId = original?.Id,
            To = string.Join(", ", to.Select(a => a.ToString())),
            Cc = cc.Count == 0 ? null : string.Join(", ", cc.Select(a => a.ToString())),
            Subject = subject, Body = body, SentAt = DateTime.UtcNow,
        };
        try
        {
            await mailer.SendAsync(settings, to, cc, subject, body);
            mail.Status = "sent";
            if (original is not null && f.Mode == "reply") original.RepliedAt = DateTime.UtcNow;
            TempData["ok"] = $"Email sent to {string.Join(", ", to.Select(a => a.Address))}.";
        }
        catch (InvalidOperationException ex)
        {
            mail.Status = "failed";
            mail.Error = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
            TempData["err"] = ex.Message + " It was saved in Sent items, so you can try again.";
        }
        db.OutgoingMails.Add(mail);
        await db.SaveChangesAsync();
        return mail.Status == "failed" ? RedirectToAction(nameof(Index), new { folder = "sent", id = mail.Id })
            : original is not null ? Back("inbox", original.Id) : RedirectToAction(nameof(Index), new { folder = "sent", id = mail.Id });
    }

    // ───────── mail settings ─────────

    public async Task<IActionResult> Settings()
    {
        var s = await mailer.SettingsAsync();
        ViewBag.HasPassword = mailer.HasPassword(s);
        return View(s);
    }

    [HttpPost]
    public async Task<IActionResult> Settings(SettingsForm f, string? test)
    {
        var s = await db.MailSettings.FirstOrDefaultAsync(x => x.Id == 1);
        if (s is null) { s = new MailSettings { Id = 1 }; db.MailSettings.Add(s); }
        s.Host = (f.Host ?? "").Trim();
        s.Port = f.Port is > 0 and < 65536 ? f.Port : 587;
        s.UseTls = f.UseTls;
        s.UserName = (f.UserName ?? "").Trim();
        if (f.ClearPassword) s.PasswordProtected = null;
        else if (!string.IsNullOrEmpty(f.Password)) s.PasswordProtected = mailer.Protect(f.Password);
        s.FromEmail = (f.FromEmail ?? "").Trim();
        s.FromName = (f.FromName ?? "").Trim();
        s.Signature = (f.Signature ?? "").TrimEnd();
        s.UpdatedAt = DateTime.UtcNow;

        string? error = null;
        if (s.FromEmail.Length > 0 && Mailer.ParseAddresses(s.FromEmail, true).Error is { } e) error = "From email: " + e;
        if (error is not null) { TempData["err"] = error; ViewBag.HasPassword = mailer.HasPassword(s); return View(s); }
        await db.SaveChangesAsync();

        if (test is not null)
        {
            try
            {
                await mailer.SendAsync(s, [new System.Net.Mail.MailAddress(s.FromEmail)], [], "Test email from your Portfolio CMS",
                    $"It works! This test was sent from your Portfolio CMS on {DateTime.Now:dddd, d MMMM yyyy 'at' h:mm tt}.\n\nYou can now reply to website messages from the Inbox.");
                TempData["ok"] = $"Settings saved. A test email was sent to {s.FromEmail}. Check that inbox.";
            }
            catch (InvalidOperationException ex) { TempData["err"] = "Settings saved, but the test failed. " + ex.Message; }
        }
        else TempData["ok"] = "Mail settings saved.";
        return RedirectToAction(nameof(Settings));
    }
}
