using Microsoft.EntityFrameworkCore;
using PortfolioCms.Models;

namespace PortfolioCms.Data;

public class CmsDbContext(DbContextOptions<CmsDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMetric> ProjectMetrics => Set<ProjectMetric>();
    public DbSet<ProjectStep> ProjectSteps => Set<ProjectStep>();
    public DbSet<ProjectImage> ProjectImages => Set<ProjectImage>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<ServiceMetric> ServiceMetrics => Set<ServiceMetric>();
    public DbSet<ServiceIncluded> ServiceIncluded => Set<ServiceIncluded>();
    public DbSet<ServiceStep> ServiceSteps => Set<ServiceStep>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<PostBlock> PostBlocks => Set<PostBlock>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Faq> Faqs => Set<Faq>();
    public DbSet<Tool> Tools => Set<Tool>();
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Project>().HasIndex(p => p.Slug).IsUnique().HasFilter("[Slug] IS NOT NULL");
        b.Entity<Service>().HasIndex(s => s.Slug).IsUnique().HasFilter("[Slug] IS NOT NULL");
        b.Entity<Post>().HasIndex(p => p.Slug).IsUnique().HasFilter("[Slug] IS NOT NULL");
        b.Entity<AdminUser>().HasIndex(u => u.UserName).IsUnique();
        b.Entity<Submission>().HasIndex(s => s.CreatedAt);
    }
}
