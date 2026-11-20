using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortfolioAdmin.Web.Authorization;
using PortfolioAdmin.Web.Data;
using PortfolioAdmin.Web.Data.Entities;
using PortfolioAdmin.Web.Infrastructure;
using PortfolioAdmin.Web.Models;
using PortfolioAdmin.Web.Services;

namespace PortfolioAdmin.Web.Controllers;

[HasPermission(Permissions.Contacts.View)]
public class ContactsController(AppDbContext db, ActivityService activity, NotificationService notifications) : Controller
{
    const int PageSize = 25;

    // ------------------------------------------------------------ list

    [HttpGet("/contacts")]
    public async Task<IActionResult> Index([FromQuery] ContactFilter f)
    {
        var q = Filtered(f);
        var total = await q.CountAsync();
        f.Page = Math.Clamp(f.Page, 1, Math.Max(1, (int)Math.Ceiling(total / (double)PageSize)));
        var items = await q.Include(c => c.FirstWebsite).Include(c => c.AssignedTo).Include(c => c.Tags).ThenInclude(t => t.Tag)
            .Skip((f.Page - 1) * PageSize).Take(PageSize).AsSplitQuery().ToListAsync();

        return View(new ContactListModel
        {
            Filter = f,
            Result = new Paged<Contact>(items, total, f.Page, PageSize),
            Websites = await db.Websites.OrderBy(w => w.Name).ToListAsync(),
            Members = await Members(),
            Tags = await db.Tags.OrderBy(t => t.Name).ToListAsync(),
            Types = await db.Contacts.Where(c => c.InquiryType != null).Select(c => c.InquiryType!).Distinct().OrderBy(t => t).ToListAsync(),
            ActiveCount = await db.Contacts.CountAsync(c => !c.IsArchived),
            ArchivedCount = await db.Contacts.CountAsync(c => c.IsArchived),
        });
    }

    IQueryable<Contact> Filtered(ContactFilter f)
    {
        var q = db.Contacts.Where(c => c.IsArchived == f.Archived);
        if (!string.IsNullOrWhiteSpace(f.Q))
        {
            var s = f.Q.Trim();
            q = q.Where(c => c.Name.Contains(s) || c.Email.Contains(s) || (c.Phone != null && c.Phone.Contains(s)) ||
                             (c.Company != null && c.Company.Contains(s)) ||
                             c.Submissions.Any(x => x.Message.Contains(s) || (x.Subject != null && x.Subject.Contains(s))));
        }
        if (f.Status is { } st) q = q.Where(c => c.Status == st);
        if (!string.IsNullOrEmpty(f.Type)) q = q.Where(c => c.InquiryType == f.Type);
        if (f.Website is { } w) q = q.Where(c => c.Submissions.Any(s => s.WebsiteId == w));
        if (f.Tag is { } t) q = q.Where(c => c.Tags.Any(x => x.TagId == t));
        if (f.Unread) q = q.Where(c => c.IsUnread);
        q = f.Assigned switch
        {
            null or "" => q,
            "me" => q.Where(c => c.AssignedToId == User.Id()),
            "none" => q.Where(c => c.AssignedToId == null),
            var id => q.Where(c => c.AssignedToId == id),
        };
        return f.Sort switch
        {
            "newest" => q.OrderByDescending(c => c.CreatedAt),
            "oldest" => q.OrderBy(c => c.CreatedAt),
            "name" => q.OrderBy(c => c.Name),
            _ => q.OrderByDescending(c => c.LastActivityAt),
        };
    }

    Task<List<AppUser>> Members() => db.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();

    // ------------------------------------------------------------ details

    [HttpGet("/contacts/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var c = await db.Contacts
            .Include(x => x.FirstWebsite).Include(x => x.AssignedTo)
            .Include(x => x.Submissions).ThenInclude(s => s.Website)
            .Include(x => x.Notes).ThenInclude(n => n.Author)
            .Include(x => x.Tags).ThenInclude(t => t.Tag)
            .AsSplitQuery().FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();

        if (c.IsUnread) { c.IsUnread = false; await db.SaveChangesAsync(); }

        var logs = await db.ActivityLogs.Include(a => a.User)
            .Where(a => a.EntityType == "Contact" && a.EntityId == id.ToString() && a.Action != "submission.received" && a.Action != "contact.note_added")
            .ToListAsync();

        var timeline = c.Submissions.Select(s => new TimelineEntry(s.CreatedAt, "submission", s))
            .Concat(c.Notes.Select(n => new TimelineEntry(n.CreatedAt, "note", n)))
            .Concat(logs.Select(l => new TimelineEntry(l.CreatedAt, "log", l)))
            .OrderByDescending(t => t.At).ToList();

        return View(new ContactDetailsModel
        {
            Contact = c, Timeline = timeline, Members = await Members(), AllTags = await db.Tags.OrderBy(t => t.Name).ToListAsync(),
        });
    }

    // ------------------------------------------------------------ create / edit

    [HttpGet("/contacts/new"), HasPermission(Permissions.Contacts.Create)]
    public async Task<IActionResult> Create()
    {
        ViewBag.Members = await Members();
        return View("Form", new ContactFormModel { Source = "Added manually" });
    }

    [HttpPost("/contacts/new"), HasPermission(Permissions.Contacts.Create)]
    public async Task<IActionResult> Create(ContactFormModel m)
    {
        if (!User.Has(Permissions.Contacts.Assign)) m.AssignedToId = null;
        if (!ModelState.IsValid) { ViewBag.Members = await Members(); return View("Form", m); }

        var email = m.Email.Trim().ToLowerInvariant();
        if (await db.Contacts.FirstOrDefaultAsync(c => c.Email == email) is { } existing)
        {
            this.Error($"A contact with {email} already exists — opened it instead.");
            return Redirect($"/contacts/{existing.Id}");
        }

        var c = new Contact
        {
            Name = m.Name.Trim(), Email = email, Phone = m.Phone?.Trim(), Company = m.Company?.Trim(), InquiryType = m.InquiryType?.Trim(),
            Status = m.Status, AssignedToId = await ValidMember(m.AssignedToId), Source = string.IsNullOrWhiteSpace(m.Source) ? "Added manually" : m.Source.Trim(),
            IsUnread = false,
        };
        if (!string.IsNullOrWhiteSpace(m.Message))
            c.Submissions.Add(new ContactSubmission { Name = c.Name, Email = email, Phone = c.Phone, Subject = m.Subject?.Trim(), InquiryType = c.InquiryType, Message = m.Message.Trim() });
        db.Contacts.Add(c);
        await db.SaveChangesAsync();

        activity.Log("contact.created", $"Added contact {c.Name}", "Contact", c.Id);
        if (c.AssignedToId is not null) await NotifyAssigned(c);
        await db.SaveChangesAsync();
        this.Success("Contact added.");
        return Redirect($"/contacts/{c.Id}");
    }

    [HttpGet("/contacts/{id:int}/edit"), HasPermission(Permissions.Contacts.Edit)]
    public async Task<IActionResult> Edit(int id)
    {
        var c = await db.Contacts.FindAsync(id);
        if (c is null) return NotFound();
        ViewBag.Members = await Members();
        return View("Form", new ContactFormModel
        {
            Id = c.Id, Name = c.Name, Email = c.Email, Phone = c.Phone, Company = c.Company, InquiryType = c.InquiryType,
            Status = c.Status, AssignedToId = c.AssignedToId, Source = c.Source,
        });
    }

    [HttpPost("/contacts/{id:int}/edit"), HasPermission(Permissions.Contacts.Edit)]
    public async Task<IActionResult> Edit(int id, ContactFormModel m)
    {
        var c = await db.Contacts.FindAsync(id);
        if (c is null) return NotFound();
        if (!ModelState.IsValid) { m.Id = id; ViewBag.Members = await Members(); return View("Form", m); }

        var oldStatus = c.Status; var oldAssignee = c.AssignedToId;
        c.Name = m.Name.Trim(); c.Email = m.Email.Trim().ToLowerInvariant(); c.Phone = m.Phone?.Trim(); c.Company = m.Company?.Trim();
        c.InquiryType = m.InquiryType?.Trim(); c.Source = m.Source?.Trim(); c.Status = m.Status;
        if (User.Has(Permissions.Contacts.Assign)) c.AssignedToId = await ValidMember(m.AssignedToId);
        c.UpdatedAt = DateTime.UtcNow;

        activity.Log("contact.updated", $"Updated details of {c.Name}", "Contact", c.Id);
        if (oldStatus != c.Status) activity.Log("contact.status_changed", $"Status of {c.Name}: {oldStatus.Label()} → {c.Status.Label()}", "Contact", c.Id);
        if (oldAssignee != c.AssignedToId && c.AssignedToId is not null) await NotifyAssigned(c);
        await db.SaveChangesAsync();
        this.Success("Contact saved.");
        return Redirect($"/contacts/{id}");
    }

    // ------------------------------------------------------------ quick actions on the profile

    [HttpPost("/contacts/{id:int}/status"), HasPermission(Permissions.Contacts.Edit)]
    public async Task<IActionResult> Status(int id, LeadStatus status)
    {
        var c = await db.Contacts.FindAsync(id);
        if (c is null) return NotFound();
        if (c.Status != status)
        {
            activity.Log("contact.status_changed", $"Status of {c.Name}: {c.Status.Label()} → {status.Label()}", "Contact", c.Id);
            c.Status = status; c.UpdatedAt = c.LastActivityAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            this.Success($"Status changed to {status.Label()}.");
        }
        return Redirect($"/contacts/{id}");
    }

    [HttpPost("/contacts/{id:int}/assign"), HasPermission(Permissions.Contacts.Assign)]
    public async Task<IActionResult> Assign(int id, string? userId)
    {
        var c = await db.Contacts.Include(x => x.AssignedTo).FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();
        var newId = await ValidMember(userId);
        if (c.AssignedToId != newId)
        {
            c.AssignedToId = newId; c.UpdatedAt = DateTime.UtcNow;
            var name = newId is null ? null : (await db.Users.FindAsync(newId))?.FullName;
            activity.Log("contact.assigned", name is null ? $"Unassigned {c.Name}" : $"Assigned {c.Name} to {name}", "Contact", c.Id);
            if (newId is not null) await NotifyAssigned(c);
            await db.SaveChangesAsync();
            this.Success(name is null ? "Contact unassigned." : $"Assigned to {name}.");
        }
        return Redirect($"/contacts/{id}");
    }

    [HttpPost("/contacts/{id:int}/notes"), HasPermission(Permissions.Contacts.Edit)]
    public async Task<IActionResult> AddNote(int id, string body)
    {
        var c = await db.Contacts.FindAsync(id);
        if (c is null) return NotFound();
        body = (body ?? "").Trim();
        if (body.Length is 0 or > 4000) { this.Error("A note must be between 1 and 4000 characters."); return Redirect($"/contacts/{id}#notes"); }

        db.ContactNotes.Add(new ContactNote { ContactId = id, AuthorId = User.Id(), Body = body });
        c.LastActivityAt = c.UpdatedAt = DateTime.UtcNow;
        activity.Log("contact.note_added", $"Added a note to {c.Name}", "Contact", c.Id);
        if (c.AssignedToId is not null)
            await notifications.NotifyAsync([c.AssignedToId], NotificationTypes.ContactNote, $"{User.DisplayName()} added a note to {c.Name}",
                ActivityService.Truncate(body, 120), $"/contacts/{c.Id}", exceptUserId: User.Id());
        await db.SaveChangesAsync();
        this.Success("Note added.");
        return Redirect($"/contacts/{id}");
    }

    [HttpPost("/contacts/{id:int}/notes/{noteId:int}/delete"), HasPermission(Permissions.Contacts.Edit)]
    public async Task<IActionResult> DeleteNote(int id, int noteId)
    {
        var n = await db.ContactNotes.FirstOrDefaultAsync(x => x.Id == noteId && x.ContactId == id);
        if (n is null) return NotFound();
        if (n.AuthorId != User.Id() && !User.Has(Permissions.Contacts.Delete)) return Forbid();
        db.ContactNotes.Remove(n);
        activity.Log("contact.note_deleted", "Deleted a note", "Contact", id);
        await db.SaveChangesAsync();
        this.Success("Note deleted.");
        return Redirect($"/contacts/{id}");
    }

    [HttpPost("/contacts/{id:int}/tags"), HasPermission(Permissions.Contacts.Edit)]
    public async Task<IActionResult> AddTag(int id, string name)
    {
        var c = await db.Contacts.Include(x => x.Tags).FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();
        name = (name ?? "").Trim();
        if (name.Length is 0 or > 40) { this.Error("Tags must be 1–40 characters."); return Redirect($"/contacts/{id}"); }

        var tag = await db.Tags.FirstOrDefaultAsync(t => t.Name == name);
        if (tag is null)
        {
            tag = new Tag { Name = name, Color = Ui.TagTones[await db.Tags.CountAsync() % Ui.TagTones.Length] };
            db.Tags.Add(tag);
            await db.SaveChangesAsync();
        }
        if (c.Tags.All(t => t.TagId != tag.Id))
        {
            c.Tags.Add(new ContactTag { TagId = tag.Id });
            activity.Log("contact.tagged", $"Tagged {c.Name} as \"{tag.Name}\"", "Contact", c.Id);
            await db.SaveChangesAsync();
        }
        return Redirect($"/contacts/{id}");
    }

    [HttpPost("/contacts/{id:int}/tags/{tagId:int}/remove"), HasPermission(Permissions.Contacts.Edit)]
    public async Task<IActionResult> RemoveTag(int id, int tagId)
    {
        var link = await db.ContactTags.Include(t => t.Tag).FirstOrDefaultAsync(t => t.ContactId == id && t.TagId == tagId);
        if (link is not null)
        {
            db.ContactTags.Remove(link);
            activity.Log("contact.untagged", $"Removed tag \"{link.Tag.Name}\"", "Contact", id);
            await db.SaveChangesAsync();
        }
        return Redirect($"/contacts/{id}");
    }

    [HttpPost("/contacts/{id:int}/unread")]
    public async Task<IActionResult> MarkUnread(int id)
    {
        await db.Contacts.Where(c => c.Id == id).ExecuteUpdateAsync(s => s.SetProperty(c => c.IsUnread, true));
        return Redirect("/contacts");
    }

    [HttpPost("/contacts/{id:int}/archive"), HasPermission(Permissions.Contacts.Delete)]
    public async Task<IActionResult> Archive(int id) => await SetArchived(id, true);

    [HttpPost("/contacts/{id:int}/restore"), HasPermission(Permissions.Contacts.Delete)]
    public async Task<IActionResult> Restore(int id) => await SetArchived(id, false);

    async Task<IActionResult> SetArchived(int id, bool archived)
    {
        var c = await db.Contacts.FindAsync(id);
        if (c is null) return NotFound();
        c.IsArchived = archived; c.ArchivedAt = archived ? DateTime.UtcNow : null;
        activity.Log(archived ? "contact.archived" : "contact.restored", $"{(archived ? "Archived" : "Restored")} {c.Name}", "Contact", c.Id);
        await db.SaveChangesAsync();
        this.Success(archived ? "Contact archived. You can restore it from the Archived tab." : "Contact restored.");
        return Redirect(archived ? "/contacts" : $"/contacts/{id}");
    }

    [HttpPost("/contacts/{id:int}/delete"), HasPermission(Permissions.Contacts.Delete)]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await db.Contacts.FindAsync(id);
        if (c is null) return NotFound();
        db.Contacts.Remove(c);
        activity.Log("contact.deleted", $"Permanently deleted contact {c.Name} ({c.Email})", "Contact", id, audit: true);
        await db.SaveChangesAsync();
        this.Success("Contact deleted permanently.");
        return Redirect("/contacts?archived=true");
    }

    // ------------------------------------------------------------ bulk

    [HttpPost("/contacts/bulk")]
    public async Task<IActionResult> Bulk(int[] ids, string action, LeadStatus? status, string? userId, string? returnUrl)
    {
        var back = !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/contacts";
        if (ids.Length == 0) { this.Error("Select at least one contact."); return Redirect(back); }
        var contacts = await db.Contacts.Where(c => ids.Contains(c.Id)).ToListAsync();

        string? need = action switch
        {
            "status" => Permissions.Contacts.Edit, "assign" => Permissions.Contacts.Assign,
            "archive" or "restore" or "delete" => Permissions.Contacts.Delete, "read" or "unread" => Permissions.Contacts.View, _ => null,
        };
        if (need is null) return BadRequest();
        if (!User.Has(need)) return Forbid();

        switch (action)
        {
            case "status" when status is { } s:
                foreach (var c in contacts.Where(c => c.Status != s))
                {
                    activity.Log("contact.status_changed", $"Status of {c.Name}: {c.Status.Label()} → {s.Label()}", "Contact", c.Id);
                    c.Status = s; c.UpdatedAt = DateTime.UtcNow;
                }
                break;
            case "assign":
                var member = await ValidMember(userId);
                foreach (var c in contacts) { c.AssignedToId = member; activity.Log("contact.assigned", member is null ? $"Unassigned {c.Name}" : $"Assigned {c.Name}", "Contact", c.Id); }
                if (member is not null)
                    await notifications.NotifyAsync([member], NotificationTypes.ContactAssigned, $"{contacts.Count} contacts were assigned to you", null, "/contacts?assigned=me", User.Id());
                break;
            case "archive" or "restore":
                foreach (var c in contacts) { c.IsArchived = action == "archive"; c.ArchivedAt = c.IsArchived ? DateTime.UtcNow : null; }
                activity.Log($"contact.{action}d", $"{(action == "archive" ? "Archived" : "Restored")} {contacts.Count} contacts", "Contact");
                break;
            case "delete":
                db.Contacts.RemoveRange(contacts);
                activity.Log("contact.deleted", $"Permanently deleted {contacts.Count} contacts: {string.Join(", ", contacts.Select(c => c.Email).Take(10))}", "Contact", audit: true);
                break;
            case "read" or "unread":
                foreach (var c in contacts) c.IsUnread = action == "unread";
                break;
            default: return BadRequest();
        }
        await db.SaveChangesAsync();
        this.Success($"Updated {contacts.Count} contact{(contacts.Count == 1 ? "" : "s")}.");
        return Redirect(back);
    }

    // ------------------------------------------------------------ export

    [HttpGet("/contacts/export"), HasPermission(Permissions.Contacts.Export)]
    public async Task<IActionResult> Export([FromQuery] ContactFilter f)
    {
        var rows = await Filtered(f).Include(c => c.FirstWebsite).Include(c => c.AssignedTo).Include(c => c.Tags).ThenInclude(t => t.Tag)
            .Include(c => c.Submissions).AsSplitQuery().ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Name,Email,Phone,Company,Inquiry type,Status,Source website,Assigned to,Tags,Inquiries,Latest subject,Latest message,Created (UTC),Last activity (UTC)");
        foreach (var c in rows)
        {
            var last = c.Submissions.OrderByDescending(s => s.CreatedAt).FirstOrDefault();
            sb.AppendJoin(',', new[]
            {
                c.Name, c.Email, c.Phone, c.Company, c.InquiryType, c.Status.Label(), c.FirstWebsite?.Name ?? c.Source, c.AssignedTo?.FullName,
                string.Join("; ", c.Tags.Select(t => t.Tag.Name)), c.Submissions.Count.ToString(), last?.Subject, last?.Message,
                c.CreatedAt.ToString("yyyy-MM-dd HH:mm"), c.LastActivityAt.ToString("yyyy-MM-dd HH:mm"),
            }.Select(Csv)).AppendLine();
        }
        activity.Log("contact.exported", $"Exported {rows.Count} contacts to CSV", "Contact", audit: true);
        await db.SaveChangesAsync();
        // UTF-8 BOM so Excel opens accents/Bangla correctly.
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"contacts-{DateTime.UtcNow:yyyy-MM-dd}.csv");
    }

    /// <summary>CSV-escapes a value and neutralises spreadsheet formulas (CSV injection).</summary>
    static string Csv(string? v)
    {
        v ??= "";
        if (v.Length > 0 && "=+-@\t\r".Contains(v[0])) v = "'" + v;
        return "\"" + v.Replace("\"", "\"\"") + "\"";
    }

    // ------------------------------------------------------------ leads board

    [HttpGet("/leads")]
    public async Task<IActionResult> Leads(string? assigned, string? q)
    {
        var query = db.Contacts.Include(c => c.AssignedTo).Include(c => c.FirstWebsite).Where(c => !c.IsArchived);
        if (assigned == "me") query = query.Where(c => c.AssignedToId == User.Id());
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(c => c.Name.Contains(q) || c.Email.Contains(q) || (c.Company != null && c.Company.Contains(q)));
        // Converted / rejected columns only show the last 90 days so the board stays usable.
        var cutoff = DateTime.UtcNow.AddDays(-90);
        query = query.Where(c => (c.Status != LeadStatus.Converted && c.Status != LeadStatus.Rejected) || c.UpdatedAt >= cutoff);
        ViewBag.Assigned = assigned; ViewBag.Q = q;
        return View(await query.OrderBy(c => c.BoardOrder).ThenByDescending(c => c.LastActivityAt).ToListAsync());
    }

    [HttpPost("/leads/move"), HasPermission(Permissions.Contacts.Edit)]
    public async Task<IActionResult> Move([FromBody] BoardMove? move)
    {
        if (move is null) return BadRequest(new { error = "Invalid request." });
        var c = await db.Contacts.FindAsync(move.Id);
        if (c is null) return NotFound(new { error = "Contact not found." });
        if (!Enum.IsDefined(move.Status)) return BadRequest(new { error = "Unknown status." });
        if (c.Status != move.Status)
        {
            activity.Log("contact.status_changed", $"Status of {c.Name}: {c.Status.Label()} → {move.Status.Label()}", "Contact", c.Id);
            c.Status = move.Status; c.UpdatedAt = c.LastActivityAt = DateTime.UtcNow;
        }
        var order = move.Order.Take(500).ToArray();
        var inColumn = await db.Contacts.Where(x => order.Contains(x.Id)).ToListAsync();
        foreach (var x in inColumn) x.BoardOrder = Array.IndexOf(order, x.Id);
        await db.SaveChangesAsync();
        return Json(new { ok = true });
    }

    // ------------------------------------------------------------ helpers

    async Task<string?> ValidMember(string? id) =>
        string.IsNullOrEmpty(id) ? null : await db.Users.AnyAsync(u => u.Id == id && u.IsActive) ? id : null;

    async Task NotifyAssigned(Contact c) =>
        await notifications.NotifyAsync([c.AssignedToId!], NotificationTypes.ContactAssigned, $"{User.DisplayName()} assigned {c.Name} to you",
            c.InquiryType, $"/contacts/{c.Id}", exceptUserId: User.Id());
}
