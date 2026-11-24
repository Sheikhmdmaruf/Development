using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortfolioCms.Data;
using PortfolioCms.Models;

namespace PortfolioCms.Controllers;

/// <summary>List / create / edit / delete / publish for one content type. Subclasses add child lists and slugs.</summary>
public abstract class CrudController<T>(CmsDbContext db) : Controller where T : Orderable, new()
{
    protected readonly CmsDbContext Db = db;

    /// <summary>Query used for the edit page (add .Include for child lists).</summary>
    protected virtual IQueryable<T> WithChildren(IQueryable<T> q) => q;
    protected virtual IQueryable<T> Ordered(IQueryable<T> q) => q.OrderBy(x => x.SortOrder).ThenBy(x => x.Id);
    /// <summary>Clean up and validate posted data (slug, empty rows…).</summary>
    protected virtual Task PrepareAsync(T model) => Task.CompletedTask;
    /// <summary>Swap the stored child rows for the posted ones.</summary>
    protected virtual void ReplaceChildren(T existing, T posted) { }
    protected abstract string Describe(T item);
    /// <summary>Name used in toast messages, e.g. "Project".</summary>
    protected virtual string Noun => "Item";

    public virtual async Task<IActionResult> Index() => View(await Ordered(Db.Set<T>().AsNoTracking()).ToListAsync());

    public async Task<IActionResult> Create()
    {
        var max = await Db.Set<T>().MaxAsync(x => (int?)x.SortOrder) ?? -1;
        return View("Edit", new T { SortOrder = max + 1 });
    }

    [HttpPost]
    public async Task<IActionResult> Create(T model)
    {
        await PrepareAsync(model);
        if (!ModelState.IsValid) return View("Edit", model);
        model.UpdatedAt = DateTime.UtcNow;
        Db.Add(model);
        await Db.SaveChangesAsync();
        TempData["ok"] = $"{Noun} “{Describe(model)}” created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await WithChildren(Db.Set<T>()).AsSplitQuery().FirstOrDefaultAsync(x => x.Id == id);
        return item is null ? NotFound() : View(item);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, T model)
    {
        model.Id = id;
        await PrepareAsync(model);
        if (!ModelState.IsValid) return View(model);

        var existing = await WithChildren(Db.Set<T>()).AsSplitQuery().FirstOrDefaultAsync(x => x.Id == id);
        if (existing is null) return NotFound();
        Db.Entry(existing).CurrentValues.SetValues(model);
        existing.UpdatedAt = DateTime.UtcNow;
        ReplaceChildren(existing, model);
        await Db.SaveChangesAsync();
        TempData["ok"] = $"{Noun} “{Describe(existing)}” updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await WithChildren(Db.Set<T>()).FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();
        Db.Remove(item);
        await Db.SaveChangesAsync();
        TempData["ok"] = $"{Noun} “{Describe(item)}” deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> TogglePublish(int id)
    {
        var item = await Db.Set<T>().FindAsync(id);
        if (item is null) return NotFound();
        item.IsPublished = !item.IsPublished;
        item.UpdatedAt = DateTime.UtcNow;
        await Db.SaveChangesAsync();
        TempData["ok"] = $"“{Describe(item)}” is now {(item.IsPublished ? "published" : "hidden")}.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Moves an item one place up (-1) or down (+1) in the list.</summary>
    [HttpPost]
    public async Task<IActionResult> Move(int id, int dir)
    {
        var list = await Ordered(Db.Set<T>()).ToListAsync();
        var i = list.FindIndex(x => x.Id == id);
        var j = i + Math.Sign(dir);
        if (i >= 0 && j >= 0 && j < list.Count) (list[i], list[j]) = (list[j], list[i]);
        for (var k = 0; k < list.Count; k++) list[k].SortOrder = k;
        await Db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // ── helpers for subclasses ──

    /// <summary>Fills an empty slug from the title and makes sure it is not used twice.</summary>
    protected async Task FixSlugAsync<TS>(TS model, string title, Func<TS, string?> get, Action<TS, string> set,
        IQueryable<TS> all, Func<TS, int> idOf) where TS : class
    {
        var s = SlugHelper.Make(string.IsNullOrWhiteSpace(get(model)) ? title : get(model)!);
        set(model, s);
        var id = idOf(model);
        var taken = (await all.AsNoTracking().ToListAsync()).Any(x => idOf(x) != id && get(x) == s);
        if (taken) ModelState.AddModelError("Slug", $"The slug “{s}” is already used. Change the title or slug.");
    }

    /// <summary>Drops blank repeater rows and numbers the rest in order.</summary>
    protected static List<TC> CleanRows<TC>(List<TC>? rows, Func<TC, bool> isEmpty, Action<TC, int> setOrder)
    {
        var list = (rows ?? []).Where(r => !isEmpty(r)).ToList();
        for (var i = 0; i < list.Count; i++) setOrder(list[i], i);
        return list;
    }

    protected void Replace<TC>(List<TC> existing, List<TC> posted, Action<TC> resetIds) where TC : class
    {
        Db.RemoveRange(existing);
        existing.Clear();
        foreach (var p in posted) { resetIds(p); existing.Add(p); }
    }
}
