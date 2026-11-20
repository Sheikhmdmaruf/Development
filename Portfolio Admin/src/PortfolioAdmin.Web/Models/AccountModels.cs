using System.ComponentModel.DataAnnotations;

namespace PortfolioAdmin.Web.Models;

public class LoginModel
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}

public class ProfileModel
{
    [Required, MaxLength(120), Display(Name = "Full name")] public string FullName { get; set; } = "";
    [MaxLength(120), Display(Name = "Job title")] public string? JobTitle { get; set; }
    [Phone, MaxLength(40)] public string? PhoneNumber { get; set; }
    public string? AvatarPath { get; set; }
    public string Email { get; set; } = "";
    public List<string> Roles { get; set; } = [];
}

public class ChangePasswordModel
{
    [Required, DataType(DataType.Password), Display(Name = "Current password")] public string CurrentPassword { get; set; } = "";
    [Required, MinLength(10), DataType(DataType.Password), Display(Name = "New password")] public string NewPassword { get; set; } = "";
    [Required, DataType(DataType.Password), Compare(nameof(NewPassword), ErrorMessage = "The passwords don't match."), Display(Name = "Confirm new password")]
    public string ConfirmPassword { get; set; } = "";
    public bool Required { get; set; }
}
