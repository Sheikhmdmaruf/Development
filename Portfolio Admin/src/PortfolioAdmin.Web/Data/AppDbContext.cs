using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PortfolioAdmin.Web.Data.Entities;

namespace PortfolioAdmin.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser, AppRole, string>(options)
{
    public DbSet<Website> Websites => Set<Website>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<ContactSubmission> ContactSubmissions => Set<ContactSubmission>();
    public DbSet<ContactNote> ContactNotes => Set<ContactNote>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ContactTag> ContactTags => Set<ContactTag>();

    public DbSet<CmsProject> CmsProjects => Set<CmsProject>();
    public DbSet<CmsService> CmsServices => Set<CmsService>();
    public DbSet<CmsPost> CmsPosts => Set<CmsPost>();
    public DbSet<CmsReview> CmsReviews => Set<CmsReview>();
    public DbSet<CmsFaq> CmsFaqs => Set<CmsFaq>();
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();
    public DbSet<MediaFile> MediaFiles => Set<MediaFile>();

    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<AppUser>(e => { e.Property(x => x.FullName).IsRequired(); e.HasIndex(x => x.IsActive); });

        b.Entity<Website>(e =>
        {
            e.HasIndex(x => x.Identifier).IsUnique();
            e.HasIndex(x => x.PublicKey).IsUnique();
            e.HasIndex(x => x.SecretKeyHash).IsUnique().HasFilter("[SecretKeyHash] IS NOT NULL");
        });

        b.Entity<Contact>(e =>
        {
            e.HasIndex(x => x.Email);
            e.HasIndex(x => new { x.IsArchived, x.Status, x.LastActivityAt });
            e.HasIndex(x => x.CreatedAt);
            e.Property(x => x.Status).HasConversion<int>();
            e.HasOne(x => x.AssignedTo).WithMany().HasForeignKey(x => x.AssignedToId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.FirstWebsite).WithMany().HasForeignKey(x => x.FirstWebsiteId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<ContactSubmission>(e =>
        {
            e.HasIndex(x => x.CreatedAt);
            e.HasOne(x => x.Contact).WithMany(c => c.Submissions).HasForeignKey(x => x.ContactId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Website).WithMany(w => w.Submissions).HasForeignKey(x => x.WebsiteId).OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<ContactNote>(e =>
        {
            e.HasOne(x => x.Contact).WithMany(c => c.Notes).HasForeignKey(x => x.ContactId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<Tag>().HasIndex(x => x.Name).IsUnique();
        b.Entity<ContactTag>(e =>
        {
            e.HasKey(x => new { x.ContactId, x.TagId });
            e.HasOne(x => x.Contact).WithMany(c => c.Tags).HasForeignKey(x => x.ContactId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Tag).WithMany(t => t.Contacts).HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<CmsProject>().HasIndex(x => x.Slug).IsUnique();
        b.Entity<CmsService>().HasIndex(x => x.Slug).IsUnique();
        b.Entity<CmsPost>().HasIndex(x => x.Slug).IsUnique();

        b.Entity<MediaFile>().HasOne(x => x.UploadedBy).WithMany().HasForeignKey(x => x.UploadedById).OnDelete(DeleteBehavior.SetNull);

        b.Entity<ActivityLog>(e =>
        {
            e.HasIndex(x => x.CreatedAt);
            e.HasIndex(x => new { x.EntityType, x.EntityId });
            e.HasIndex(x => new { x.IsAudit, x.CreatedAt });
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<Notification>(e =>
        {
            e.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt });
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<NotificationPreference>(e =>
        {
            e.HasKey(x => new { x.UserId, x.Type });
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
