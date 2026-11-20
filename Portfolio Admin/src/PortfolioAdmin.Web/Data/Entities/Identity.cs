using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace PortfolioAdmin.Web.Data.Entities;

/// <summary>A person who can sign in to the dashboard (admin or team member).</summary>
public class AppUser : IdentityUser
{
    [MaxLength(120)] public string FullName { get; set; } = "";
    [MaxLength(120)] public string? JobTitle { get; set; }
    [MaxLength(300)] public string? AvatarPath { get; set; }
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    public string Initials =>
        string.Concat((FullName.Length > 0 ? FullName : Email ?? "?")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(p => char.ToUpperInvariant(p[0])));
}

/// <summary>A role. Its permissions are stored as role claims of type <c>perm</c>.</summary>
public class AppRole : IdentityRole
{
    public AppRole() { }
    public AppRole(string name) : base(name) { }

    [MaxLength(300)] public string? Description { get; set; }
    /// <summary>System roles cannot be deleted or renamed.</summary>
    public bool IsSystem { get; set; }
    public int SortOrder { get; set; }
}
