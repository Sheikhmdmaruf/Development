using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortfolioAdmin.Web.Authorization;
using PortfolioAdmin.Web.Data;
using PortfolioAdmin.Web.Data.Entities;
using PortfolioAdmin.Web.Infrastructure;

namespace PortfolioAdmin.Web.Controllers;

public class NotificationsController(AppDbContext db) : Controller
{
    [HttpGet("/notifications")]
    public async Task<IActionResult> Index(string? filter, int page = 1)
    {
        var me = User.Id();
        var q = db.Notifications.Where(n => n.UserId == me);
        if (filter == "unread") q = q.Where(n => !n.IsRead);
        var total = await q.CountAsync();
        page = Math.Max(1, page);
        var items = await q.OrderByDescending(n => n.CreatedAt).Skip((page - 1) * 30).Take(30).ToListAsync();
        ViewBag.Filter = filter;
        ViewBag.UnreadCount = await db.Notifications.CountAsync(n => n.UserId == me && !n.IsRead);
        return View(new Paged<Notification>(items, total, page, 30));
    }

    [HttpGet("/notifications/count")]
    public async Task<IActionResult> Count()
    {
        var me = User.Id();
        return Json(new { unread = await db.Notifications.CountAsync(n => n.UserId == me && !n.IsRead) });
    }

    [HttpGet("/notifications/dropdown")]
    public async Task<IActionResult> Dropdown()
    {
        var me = User.Id();
        var items = await db.Notifications.Where(n => n.UserId == me).OrderByDescending(n => n.CreatedAt).Take(8).ToListAsync();
        return PartialView("_NotificationItems", items);
    }

    /// <summary>Opening a notification marks it read, then follows its link.</summary>
    [HttpGet("/notifications/{id:long}/open")]
    public async Task<IActionResult> Open(long id)
    {
        var me = User.Id();
        var n = await db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == me);
        if (n is null) return NotFound();
        n.IsRead = true;
        await db.SaveChangesAsync();
        return LocalRedirect(n.Url is { } u && Url.IsLocalUrl(u) ? u : "/notifications");
    }

    [HttpPost("/notifications/{id:long}/toggle")]
    public async Task<IActionResult> Toggle(long id)
    {
        var me = User.Id();
        var n = await db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == me);
        if (n is null) return NotFound();
        n.IsRead = !n.IsRead;
        await db.SaveChangesAsync();
        return Redirect(Request.Headers.Referer.FirstOrDefault() is { } r && Url.IsLocalUrl(new Uri(r).PathAndQuery) ? new Uri(r).PathAndQuery : "/notifications");
    }

    [HttpPost("/notifications/read-all")]
    public async Task<IActionResult> ReadAll()
    {
        var me = User.Id();
        await db.Notifications.Where(n => n.UserId == me && !n.IsRead).ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
        this.Success("All notifications marked as read.");
        return Redirect("/notifications");
    }

    [HttpPost("/notifications/clear-read")]
    public async Task<IActionResult> ClearRead()
    {
        var me = User.Id();
        var n = await db.Notifications.Where(x => x.UserId == me && x.IsRead).ExecuteDeleteAsync();
        this.Success($"Deleted {n} read notifications.");
        return Redirect("/notifications");
    }
}
