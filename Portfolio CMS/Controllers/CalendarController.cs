using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortfolioCms.Data;
using PortfolioCms.Models;

namespace PortfolioCms.Controllers;

/// <summary>
/// Google-Calendar-style planner. The page (Views/Calendar/Index) is one screen driven by wwwroot/js/calendar.js,
/// which talks to the small JSON endpoints below. Times are local wall-clock times ("2026-11-25T14:30:00").
/// </summary>
public partial class CalendarController(CmsDbContext db) : Controller
{
    public static readonly string[] RepeatKinds = ["none", "daily", "weekdays", "weekly", "monthly", "yearly"];
    const string Fmt = "yyyy-MM-dd'T'HH:mm:ss";

    public record EventForm(int? Id, int CalendarId, string? Title, string? Description, string? Location,
        DateTime Start, DateTime End, bool AllDay, string? Color, string? Repeat, DateTime? RepeatUntil);
    public record MoveForm(int Id, int StartMinutes, int EndMinutes);
    public record CalendarForm(int? Id, string? Name, string? Color);
    public record ToggleForm(int Id, bool Visible);
    public record IdForm(int Id);

    public async Task<IActionResult> Index()
    {
        if (!await db.CalendarLists.AnyAsync())
        {
            db.CalendarLists.AddRange(
                new CalendarList { Name = "Personal", Color = "#3d7bff", SortOrder = 0 },
                new CalendarList { Name = "Work", Color = "#ff6b3d", SortOrder = 1 },
                new CalendarList { Name = "Client meetings", Color = "#2bd197", SortOrder = 2 });
            await db.SaveChangesAsync();
        }
        ViewBag.Calendars = await Calendars();
        return View();
    }

    // ───────── events ─────────

    /// <summary>Every occurrence that overlaps [from, to). Repeating events are expanded here.</summary>
    [HttpGet]
    public async Task<IActionResult> Events(DateTime from, DateTime to)
    {
        if (to <= from || (to - from).TotalDays > 400) return BadRequest("Bad range.");
        var list = await db.CalendarEvents.AsNoTracking().Include(e => e.Calendar)
            .Where(e => e.Start < to && (e.Repeat != "none" || e.End > from))
            .ToListAsync();
        var result = list.SelectMany(e => Occurrences(e, from, to).Select(o => ToJson(e, o.Start, o.End))).ToList();
        return Json(result);
    }

    /// <summary>Search titles, places and notes. Returns the next matches from today (and recent past ones).</summary>
    [HttpGet]
    public async Task<IActionResult> Search(string? q)
    {
        q = q?.Trim();
        if (string.IsNullOrEmpty(q)) return Json(Array.Empty<object>());
        var list = await db.CalendarEvents.AsNoTracking().Include(e => e.Calendar)
            .Where(e => e.Title.Contains(q) || (e.Location != null && e.Location.Contains(q)) || (e.Description != null && e.Description.Contains(q)))
            .Take(200).ToListAsync();
        var from = DateTime.Today.AddMonths(-3);
        var to = DateTime.Today.AddYears(1);
        var result = list.SelectMany(e => Occurrences(e, from, to).Take(10).Select(o => ToJson(e, o.Start, o.End)))
            .OrderBy(x => x.start).Take(100).ToList();
        return Json(result);
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] EventForm f)
    {
        var title = f.Title?.Trim();
        if (string.IsNullOrEmpty(title)) title = "(No title)";
        var start = f.Start; var end = f.End;
        if (f.AllDay) { start = start.Date; end = end.Date; if (end <= start) end = start.AddDays(1); }
        if (end <= start) return BadRequest(new { error = "The end time must be after the start time." });
        if ((end - start).TotalDays > 366) return BadRequest(new { error = "An event can't be longer than a year." });
        if (!await db.CalendarLists.AnyAsync(c => c.Id == f.CalendarId)) return BadRequest(new { error = "Pick a calendar." });
        var repeat = RepeatKinds.Contains(f.Repeat) ? f.Repeat! : "none";
        if (f.Color is { Length: > 0 } && !IsColor(f.Color)) return BadRequest(new { error = "That colour isn't valid." });

        CalendarEvent? e;
        if (f.Id is int id)
        {
            e = await db.CalendarEvents.FindAsync(id);
            if (e is null) return NotFound(new { error = "This event no longer exists." });
        }
        else
        {
            e = new CalendarEvent();
            db.CalendarEvents.Add(e);
        }
        e.CalendarListId = f.CalendarId;
        e.Title = Cut(title, 200)!;
        e.Description = Cut(f.Description?.Trim(), 4000);
        e.Location = Cut(f.Location?.Trim(), 300);
        e.Start = start; e.End = end; e.AllDay = f.AllDay;
        e.Color = string.IsNullOrEmpty(f.Color) ? null : f.Color;
        e.Repeat = repeat;
        e.RepeatUntil = repeat == "none" || f.RepeatUntil is null ? null : f.RepeatUntil.Value.Date;
        e.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Json(new { id = e.Id });
    }

    /// <summary>Drag &amp; drop / resize: shift the start and end by a number of minutes (the whole series for repeating events).</summary>
    [HttpPost]
    public async Task<IActionResult> Move([FromBody] MoveForm f)
    {
        var e = await db.CalendarEvents.FindAsync(f.Id);
        if (e is null) return NotFound(new { error = "This event no longer exists." });
        var start = e.Start.AddMinutes(f.StartMinutes);
        var end = e.End.AddMinutes(f.EndMinutes);
        if (end <= start) return BadRequest(new { error = "The end time must be after the start time." });
        e.Start = start; e.End = end; e.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Json(new { id = e.Id });
    }

    [HttpPost]
    public async Task<IActionResult> Delete([FromBody] IdForm f)
    {
        var e = await db.CalendarEvents.FindAsync(f.Id);
        if (e is not null) { db.CalendarEvents.Remove(e); await db.SaveChangesAsync(); }
        return Json(new { ok = true });
    }

    // ───────── calendars ("My calendars" list) ─────────

    [HttpPost]
    public async Task<IActionResult> SaveCalendar([FromBody] CalendarForm f)
    {
        var name = f.Name?.Trim();
        if (string.IsNullOrEmpty(name)) return BadRequest(new { error = "Give the calendar a name." });
        if (!IsColor(f.Color)) return BadRequest(new { error = "Pick a colour." });
        CalendarList? c;
        if (f.Id is int id)
        {
            c = await db.CalendarLists.FindAsync(id);
            if (c is null) return NotFound(new { error = "This calendar no longer exists." });
        }
        else
        {
            c = new CalendarList { SortOrder = (await db.CalendarLists.MaxAsync(x => (int?)x.SortOrder) ?? 0) + 1 };
            db.CalendarLists.Add(c);
        }
        c.Name = Cut(name, 60)!; c.Color = f.Color!;
        await db.SaveChangesAsync();
        return Json(await Calendars());
    }

    [HttpPost]
    public async Task<IActionResult> ToggleCalendar([FromBody] ToggleForm f)
    {
        var c = await db.CalendarLists.FindAsync(f.Id);
        if (c is not null) { c.IsVisible = f.Visible; await db.SaveChangesAsync(); }
        return Json(new { ok = true });
    }

    /// <summary>Deletes a calendar and all of its events. The last calendar can't be deleted.</summary>
    [HttpPost]
    public async Task<IActionResult> DeleteCalendar([FromBody] IdForm f)
    {
        if (await db.CalendarLists.CountAsync() <= 1) return BadRequest(new { error = "You need at least one calendar." });
        var c = await db.CalendarLists.FindAsync(f.Id);
        if (c is not null) { db.CalendarLists.Remove(c); await db.SaveChangesAsync(); }
        return Json(await Calendars());
    }

    // ───────── helpers ─────────

    async Task<List<object>> Calendars() =>
        (await db.CalendarLists.AsNoTracking().OrderBy(c => c.SortOrder).ThenBy(c => c.Id)
            .Select(c => new { id = c.Id, name = c.Name, color = c.Color, visible = c.IsVisible, count = c.Events.Count() })
            .ToListAsync()).Cast<object>().ToList();

    record EventJson(int id, int calendarId, string title, string? description, string? location, string start, string end,
        bool allDay, string color, string? ownColor, string repeat, string? repeatUntil, string seriesStart, string seriesEnd);

    static EventJson ToJson(CalendarEvent e, DateTime s, DateTime en) => new(
        e.Id, e.CalendarListId, e.Title, e.Description, e.Location,
        s.ToString(Fmt, CultureInfo.InvariantCulture), en.ToString(Fmt, CultureInfo.InvariantCulture),
        e.AllDay, e.Color ?? e.Calendar?.Color ?? "#3d7bff", e.Color, e.Repeat,
        e.RepeatUntil?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        e.Start.ToString(Fmt, CultureInfo.InvariantCulture), e.End.ToString(Fmt, CultureInfo.InvariantCulture));

    /// <summary>The occurrences of an event (one for non-repeating) that overlap [from, to).</summary>
    static IEnumerable<(DateTime Start, DateTime End)> Occurrences(CalendarEvent e, DateTime from, DateTime to)
    {
        var len = e.End - e.Start;
        if (e.Repeat == "none")
        {
            if (e.Start < to && e.End > from) yield return (e.Start, e.End);
            yield break;
        }
        var until = e.RepeatUntil?.Date.AddDays(1) ?? DateTime.MaxValue; // occurrences must START before this

        // Jump close to the range so long-running series stay fast.
        long n = 0;
        var gap = (from - len - e.Start).TotalDays;
        if (gap > 0)
            n = e.Repeat switch
            {
                "daily" => (long)gap,
                "weekdays" => (long)(gap / 7) * 5,
                "weekly" => (long)(gap / 7),
                "monthly" => (long)(gap / 31),
                "yearly" => (long)(gap / 366),
                _ => 0,
            };

        DateTime? Nth(long i)
        {
            try
            {
                return e.Repeat switch
                {
                    "daily" => e.Start.AddDays(i),
                    "weekly" => e.Start.AddDays(i * 7),
                    "monthly" => e.Start.AddMonths((int)i),
                    "yearly" => e.Start.AddYears((int)i),
                    "weekdays" => WeekdayN(e.Start, i),
                    _ => null,
                };
            }
            catch (ArgumentOutOfRangeException) { return null; }
        }

        for (var guard = 0; guard < 2000; guard++, n++)
        {
            if (Nth(n) is not DateTime s || s >= to || s >= until) yield break;
            var en = s + len;
            if (en > from) yield return (s, en);
        }
    }

    /// <summary>The i-th Monday–Friday on or after start (start itself counts if it's a weekday).</summary>
    static DateTime WeekdayN(DateTime start, long i)
    {
        var d = start;
        while (d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) d = d.AddDays(1);
        var weeks = i / 5; var rest = i % 5;
        d = d.AddDays(weeks * 7);
        while (rest > 0)
        {
            d = d.AddDays(1);
            if (d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)) rest--;
        }
        return d;
    }

    static string? Cut(string? s, int max) => s is null ? null : s.Length <= max ? s : s[..max];
    static bool IsColor(string? c) => c is not null && ColorRx().IsMatch(c);

    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    private static partial Regex ColorRx();
}
