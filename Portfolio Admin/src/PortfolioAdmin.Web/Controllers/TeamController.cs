using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortfolioAdmin.Web.Authorization;
using PortfolioAdmin.Web.Data;
using PortfolioAdmin.Web.Data.Entities;
using PortfolioAdmin.Web.Infrastructure;
using PortfolioAdmin.Web.Services;

namespace PortfolioAdmin.Web.Controllers;

public class MemberForm
{
    public string? Id { get; set; }
    [Required, MaxLength(120), Display(Name = "Full name")] public string FullName { get; set; } = "";
    [Required, EmailAddress, MaxLength(256)] public string Email { get; set; } = "";
    [MaxLength(120), Display(Name = "Job title")] public string? JobTitle { get; set; }
    [Required] public string Role { get; set; } = "Team Member";
    public bool IsActive { get; set; } = true;
}

public record MemberRow(AppUser User, string Role, int AssignedContacts);

[HasPermission(Permissions.Team.View)]
public class TeamController(AppDbContext db, UserManager<AppUser> users, RoleManager<AppRole> roles, SignInManager<AppUser> signIn, ActivityService activity) : Controller
{
    [HttpGet("/team")]
    public async Task<IActionResult> Index(string? q, bool inactive = false)
    {
        var query = db.Users.Where(u => u.IsActive != inactive);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(u => u.FullName.Contains(q) || u.Email!.Contains(q) || (u.JobTitle != null && u.JobTitle.Contains(q)));
        var list = await query.OrderBy(u => u.FullName).ToListAsync();
        var roleMap = await db.UserRoles.Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name }).ToListAsync();
        var assigned = await db.Contacts.Where(c => c.AssignedToId != null && !c.IsArchived).GroupBy(c => c.AssignedToId!)
            .Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N);

        ViewBag.Q = q; ViewBag.Inactive = inactive;
        ViewBag.InactiveCount = await db.Users.CountAsync(u => !u.IsActive);
        return View(list.Select(u => new MemberRow(u, string.Join(", ", roleMap.Where(r => r.UserId == u.Id).Select(r => r.Name)), assigned.GetValueOrDefault(u.Id))).ToList());
    }

    [HttpGet("/team/{id}")]
    public async Task<IActionResult> Details(string id)
    {
        var u = await users.FindByIdAsync(id);
        if (u is null) return NotFound();
        ViewBag.Roles = await users.GetRolesAsync(u);
        ViewBag.Contacts = await db.Contacts.Where(c => c.AssignedToId == id && !c.IsArchived).OrderByDescending(c => c.LastActivityAt).Take(10).ToListAsync();
        ViewBag.ContactStats = await db.Contacts.Where(c => c.AssignedToId == id).GroupBy(c => c.Status).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N);
        ViewBag.Activity = User.Has(Permissions.Team.ViewActivity)
            ? await db.ActivityLogs.Where(a => a.UserId == id && !a.IsAudit).OrderByDescending(a => a.CreatedAt).Take(15).ToListAsync()
            : new List<ActivityLog>();
        ViewBag.NewPassword = TempData["new.password"] as string;
        return View(u);
    }

    [HttpGet("/team/new"), HasPermission(Permissions.Team.Manage)]
    public async Task<IActionResult> Create()
    {
        ViewBag.Roles = await AssignableRoles();
        return View("Form", new MemberForm());
    }

    [HttpPost("/team/new"), HasPermission(Permissions.Team.Manage)]
    public async Task<IActionResult> Create(MemberForm m)
    {
        var assignable = await AssignableRoles();
        if (!assignable.Any(r => r.Name == m.Role)) ModelState.AddModelError(nameof(m.Role), "You can't give this role.");
        if (await users.FindByEmailAsync(m.Email.Trim()) is not null) ModelState.AddModelError(nameof(m.Email), "Someone with this email is already in the team.");
        if (!ModelState.IsValid) { ViewBag.Roles = assignable; return View("Form", m); }

        var password = TempPassword();
        var user = new AppUser
        {
            UserName = m.Email.Trim(), Email = m.Email.Trim(), EmailConfirmed = true, FullName = m.FullName.Trim(),
            JobTitle = m.JobTitle?.Trim(), MustChangePassword = true, IsActive = true,
        };
        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
            ViewBag.Roles = assignable; return View("Form", m);
        }
        await users.AddToRoleAsync(user, m.Role);
        activity.Log("team.invited", $"Added {user.FullName} to the team as {m.Role}", "User", user.Id, audit: true);
        await db.SaveChangesAsync();

        TempData["new.password"] = password;
        this.Success($"{user.FullName} was added. Share the temporary password below with them.");
        return Redirect($"/team/{user.Id}");
    }

    [HttpGet("/team/{id}/edit"), HasPermission(Permissions.Team.Manage)]
    public async Task<IActionResult> Edit(string id)
    {
        var u = await users.FindByIdAsync(id);
        if (u is null) return NotFound();
        if (!await CanManage(u)) return Forbid();
        ViewBag.Roles = await AssignableRoles();
        return View("Form", new MemberForm
        {
            Id = u.Id, FullName = u.FullName, Email = u.Email ?? "", JobTitle = u.JobTitle, IsActive = u.IsActive,
            Role = (await users.GetRolesAsync(u)).FirstOrDefault() ?? "Team Member",
        });
    }

    [HttpPost("/team/{id}/edit"), HasPermission(Permissions.Team.Manage)]
    public async Task<IActionResult> Edit(string id, MemberForm m)
    {
        var u = await users.FindByIdAsync(id);
        if (u is null) return NotFound();
        if (!await CanManage(u)) return Forbid();
        var assignable = await AssignableRoles();
        if (!assignable.Any(r => r.Name == m.Role)) ModelState.AddModelError(nameof(m.Role), "You can't give this role.");
        var other = await users.FindByEmailAsync(m.Email.Trim());
        if (other is not null && other.Id != id) ModelState.AddModelError(nameof(m.Email), "Someone else already uses this email.");
        if (id == User.Id() && !m.IsActive) ModelState.AddModelError(nameof(m.IsActive), "You can't deactivate yourself.");
        if (!ModelState.IsValid) { m.Id = id; ViewBag.Roles = assignable; return View("Form", m); }

        var oldRoles = await users.GetRolesAsync(u);
        if (oldRoles.Contains(Permissions.SuperAdminRole) && m.Role != Permissions.SuperAdminRole && await LastSuperAdmin(u))
        {
            ModelState.AddModelError(nameof(m.Role), "This is the last Super Admin. Make someone else Super Admin first.");
            m.Id = id; ViewBag.Roles = assignable; return View("Form", m);
        }

        u.FullName = m.FullName.Trim(); u.JobTitle = m.JobTitle?.Trim();
        if (!string.Equals(u.Email, m.Email.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            await users.SetEmailAsync(u, m.Email.Trim()); await users.SetUserNameAsync(u, m.Email.Trim());
            u.EmailConfirmed = true;
        }
        var wasActive = u.IsActive; u.IsActive = m.IsActive;
        await users.UpdateAsync(u);

        if (!oldRoles.SequenceEqual([m.Role]))
        {
            await users.RemoveFromRolesAsync(u, oldRoles);
            await users.AddToRoleAsync(u, m.Role);
            activity.Log("team.role_changed", $"Changed {u.FullName}'s role: {string.Join(", ", oldRoles)} → {m.Role}", "User", u.Id, audit: true);
        }
        if (wasActive != u.IsActive)
        {
            activity.Log(u.IsActive ? "team.reactivated" : "team.deactivated", $"{(u.IsActive ? "Reactivated" : "Deactivated")} {u.FullName}", "User", u.Id, audit: true);
            if (!u.IsActive) await db.Contacts.Where(c => c.AssignedToId == u.Id).ExecuteUpdateAsync(s => s.SetProperty(c => c.AssignedToId, (string?)null));
        }
        activity.Log("team.updated", $"Updated {u.FullName}'s profile", "User", u.Id);
        if (!u.IsActive) await users.UpdateSecurityStampAsync(u); // signs a deactivated member out within a minute
        await db.SaveChangesAsync();
        if (id == User.Id()) await signIn.RefreshSignInAsync(u);
        this.Success("Member saved.");
        return Redirect($"/team/{id}");
    }

    [HttpPost("/team/{id}/reset-password"), HasPermission(Permissions.Team.Manage)]
    public async Task<IActionResult> ResetPassword(string id)
    {
        var u = await users.FindByIdAsync(id);
        if (u is null) return NotFound();
        if (!await CanManage(u) || id == User.Id()) return Forbid();
        var password = TempPassword();
        var token = await users.GeneratePasswordResetTokenAsync(u);
        var result = await users.ResetPasswordAsync(u, token, password);
        if (!result.Succeeded) { this.Error(string.Join(" ", result.Errors.Select(e => e.Description))); return Redirect($"/team/{id}"); }
        u.MustChangePassword = true;
        await users.UpdateAsync(u);
        await users.SetLockoutEndDateAsync(u, null);
        activity.Log("team.password_reset", $"Reset {u.FullName}'s password", "User", u.Id, audit: true);
        await db.SaveChangesAsync();
        TempData["new.password"] = password;
        this.Success("Password reset. Share the temporary password below.");
        return Redirect($"/team/{id}");
    }

    [HttpPost("/team/{id}/delete"), HasPermission(Permissions.Team.Manage)]
    public async Task<IActionResult> Delete(string id)
    {
        var u = await users.FindByIdAsync(id);
        if (u is null) return NotFound();
        if (id == User.Id()) { this.Error("You can't remove yourself."); return Redirect($"/team/{id}"); }
        if (!await CanManage(u)) return Forbid();
        if ((await users.GetRolesAsync(u)).Contains(Permissions.SuperAdminRole) && await LastSuperAdmin(u))
        { this.Error("You can't remove the last Super Admin."); return Redirect($"/team/{id}"); }

        // Assigned contacts become unassigned; notes and activity keep the history without the author.
        await db.Contacts.Where(c => c.AssignedToId == id).ExecuteUpdateAsync(s => s.SetProperty(c => c.AssignedToId, (string?)null));
        activity.Log("team.removed", $"Removed {u.FullName} ({u.Email}) from the team", "User", id, audit: true);
        await db.SaveChangesAsync();
        await users.DeleteAsync(u);
        this.Success($"{u.FullName} was removed.");
        return Redirect("/team");
    }

    [HttpGet("/team/activity"), HasPermission(Permissions.Team.ViewActivity)]
    public async Task<IActionResult> Activity(string? user, string? type, int page = 1)
    {
        var q = db.ActivityLogs.Include(a => a.User).Where(a => !a.IsAudit);
        if (!string.IsNullOrEmpty(user)) q = q.Where(a => a.UserId == user);
        if (!string.IsNullOrEmpty(type)) q = q.Where(a => a.Action.StartsWith(type + "."));
        var total = await q.CountAsync();
        page = Math.Max(1, page);
        var items = await q.OrderByDescending(a => a.CreatedAt).Skip((page - 1) * 40).Take(40).ToListAsync();
        ViewBag.Members = await db.Users.OrderBy(u => u.FullName).ToListAsync();
        ViewBag.User = user; ViewBag.Type = type;
        return View(new Paged<ActivityLog>(items, total, page, 40));
    }

    // ------------------------------------------------------------ helpers

    /// <summary>Only Super Admins can create or manage Super Admins.</summary>
    async Task<List<AppRole>> AssignableRoles()
    {
        var all = await roles.Roles.OrderBy(r => r.SortOrder).ThenBy(r => r.Name).ToListAsync();
        return User.IsSuperAdmin() ? all : all.Where(r => r.Name != Permissions.SuperAdminRole).ToList();
    }

    async Task<bool> CanManage(AppUser target) =>
        User.IsSuperAdmin() || !(await users.GetRolesAsync(target)).Contains(Permissions.SuperAdminRole);

    async Task<bool> LastSuperAdmin(AppUser u) =>
        (await users.GetUsersInRoleAsync(Permissions.SuperAdminRole)).Count(x => x.IsActive) <= 1;

    /// <summary>Readable random password that meets the policy, e.g. "Kite-7f3a-Moon-92".</summary>
    static string TempPassword()
    {
        string[] words = ["Kite", "Moon", "River", "Stone", "Cloud", "Maple", "Ocean", "Tiger", "Pixel", "Amber", "Cedar", "Comet"];
        var hex = Convert.ToHexString(RandomNumberGenerator.GetBytes(3)).ToLowerInvariant();
        return $"{words[RandomNumberGenerator.GetInt32(words.Length)]}-{hex}-{words[RandomNumberGenerator.GetInt32(words.Length)]}-{RandomNumberGenerator.GetInt32(10, 99)}";
    }
}

/// <summary>Roles & permissions editor and the audit log.</summary>
public class SettingsController(AppDbContext db, RoleManager<AppRole> roles, UserManager<AppUser> users, SignInManager<AppUser> signIn, ActivityService activity) : Controller
{
    [HttpGet("/settings/roles"), HasPermission(Permissions.Settings.Roles)]
    public async Task<IActionResult> Roles()
    {
        var list = await roles.Roles.OrderBy(r => r.SortOrder).ThenBy(r => r.Name).ToListAsync();
        var counts = await db.UserRoles.GroupBy(ur => ur.RoleId).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N);
        var perms = await db.RoleClaims.Where(c => c.ClaimType == Permissions.ClaimType).GroupBy(c => c.RoleId)
            .Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N);
        ViewBag.Counts = counts; ViewBag.Perms = perms;
        return View(list);
    }

    [HttpGet("/settings/roles/{id}"), HasPermission(Permissions.Settings.Roles)]
    public async Task<IActionResult> Role(string id)
    {
        var role = await roles.FindByIdAsync(id);
        if (role is null) return NotFound();
        ViewBag.Granted = (await roles.GetClaimsAsync(role)).Where(c => c.Type == Permissions.ClaimType).Select(c => c.Value).ToHashSet();
        ViewBag.Members = await users.GetUsersInRoleAsync(role.Name!);
        return View(role);
    }

    [HttpPost("/settings/roles/new"), HasPermission(Permissions.Settings.Roles)]
    public async Task<IActionResult> CreateRole(string name, string? description)
    {
        name = (name ?? "").Trim();
        if (name.Length is < 2 or > 60) { this.Error("Role names must be 2–60 characters."); return Redirect("/settings/roles"); }
        if (await roles.RoleExistsAsync(name)) { this.Error("A role with this name already exists."); return Redirect("/settings/roles"); }
        var role = new AppRole(name) { Description = description?.Trim(), SortOrder = 100 };
        await roles.CreateAsync(role);
        foreach (var p in Permissions.Defaults["Team Member"]) await roles.AddClaimAsync(role, new Claim(Permissions.ClaimType, p));
        activity.Log("role.created", $"Created role {name}", "Role", role.Id, audit: true);
        await db.SaveChangesAsync();
        this.Success("Role created. Choose its permissions below.");
        return Redirect($"/settings/roles/{role.Id}");
    }

    [HttpPost("/settings/roles/{id}"), HasPermission(Permissions.Settings.Roles)]
    public async Task<IActionResult> Role(string id, string? description, string[] permissions)
    {
        var role = await roles.FindByIdAsync(id);
        if (role is null) return NotFound();
        if (role.Name == Permissions.SuperAdminRole) { this.Error("Super Admin always has every permission."); return Redirect($"/settings/roles/{id}"); }

        var wanted = permissions.Where(Permissions.IsValid).ToHashSet();
        var current = (await roles.GetClaimsAsync(role)).Where(c => c.Type == Permissions.ClaimType).ToList();
        foreach (var c in current.Where(c => !wanted.Contains(c.Value))) await roles.RemoveClaimAsync(role, c);
        foreach (var p in wanted.Except(current.Select(c => c.Value))) await roles.AddClaimAsync(role, new Claim(Permissions.ClaimType, p));
        role.Description = description?.Trim();
        await roles.UpdateAsync(role);

        // No sign-out needed: the security stamp validator rebuilds every session's claims each minute.

        var added = wanted.Except(current.Select(c => c.Value)).ToList();
        var removed = current.Select(c => c.Value).Except(wanted).ToList();
        activity.Log("role.permissions_changed",
            $"Changed permissions of {role.Name}" + (added.Count > 0 ? $" · added {string.Join(", ", added)}" : "") + (removed.Count > 0 ? $" · removed {string.Join(", ", removed)}" : ""),
            "Role", role.Id, audit: true);
        await db.SaveChangesAsync();
        if (await users.GetUserAsync(User) is { } me && await users.IsInRoleAsync(me, role.Name!)) await signIn.RefreshSignInAsync(me);
        this.Success("Permissions saved. Members get them within a minute.");
        return Redirect($"/settings/roles/{id}");
    }

    [HttpPost("/settings/roles/{id}/delete"), HasPermission(Permissions.Settings.Roles)]
    public async Task<IActionResult> DeleteRole(string id)
    {
        var role = await roles.FindByIdAsync(id);
        if (role is null) return NotFound();
        if (role.IsSystem) { this.Error("Built-in roles can't be deleted."); return Redirect($"/settings/roles/{id}"); }
        if ((await users.GetUsersInRoleAsync(role.Name!)).Count > 0) { this.Error("Move the members of this role to another role first."); return Redirect($"/settings/roles/{id}"); }
        await roles.DeleteAsync(role);
        activity.Log("role.deleted", $"Deleted role {role.Name}", "Role", id, audit: true);
        await db.SaveChangesAsync();
        this.Success("Role deleted.");
        return Redirect("/settings/roles");
    }

    [HttpGet("/settings/audit"), HasPermission(Permissions.Settings.Audit)]
    public async Task<IActionResult> Audit(string? q, int page = 1)
    {
        var query = db.ActivityLogs.Include(a => a.User).Where(a => a.IsAudit);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(a => a.Summary.Contains(q) || a.Action.Contains(q) || (a.IpAddress != null && a.IpAddress.Contains(q)));
        var total = await query.CountAsync();
        page = Math.Max(1, page);
        var items = await query.OrderByDescending(a => a.CreatedAt).Skip((page - 1) * 50).Take(50).ToListAsync();
        ViewBag.Q = q;
        return View(new Paged<ActivityLog>(items, total, page, 50));
    }
}
