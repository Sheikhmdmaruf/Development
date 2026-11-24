using Microsoft.EntityFrameworkCore;
using PortfolioCms.Data;
using PortfolioCms.Models;

namespace PortfolioCms.Controllers;

public class ProjectsController(CmsDbContext db) : CrudController<Project>(db)
{
    protected override string Describe(Project p) => p.Title;
    protected override string Noun => "Project";

    protected override IQueryable<Project> WithChildren(IQueryable<Project> q) =>
        q.Include(p => p.Metrics.OrderBy(m => m.SortOrder))
         .Include(p => p.Steps.OrderBy(s => s.SortOrder))
         .Include(p => p.Gallery.OrderBy(g => g.SortOrder));

    protected override async Task PrepareAsync(Project m)
    {
        await FixSlugAsync(m, m.Title, x => x.Slug, (x, s) => x.Slug = s, Db.Projects, x => x.Id);
        m.Metrics = CleanRows(m.Metrics, r => string.IsNullOrWhiteSpace(r.Label + r.Value + r.Caption), (r, i) => r.SortOrder = i);
        m.Steps = CleanRows(m.Steps, r => string.IsNullOrWhiteSpace(r.Title + r.Body), (r, i) => r.SortOrder = i);
        m.Gallery = CleanRows(m.Gallery, r => string.IsNullOrWhiteSpace(r.Url), (r, i) => r.SortOrder = i);
    }

    protected override void ReplaceChildren(Project e, Project p)
    {
        Replace(e.Metrics, p.Metrics, r => { r.Id = 0; r.ProjectId = e.Id; });
        Replace(e.Steps, p.Steps, r => { r.Id = 0; r.ProjectId = e.Id; });
        Replace(e.Gallery, p.Gallery, r => { r.Id = 0; r.ProjectId = e.Id; });
    }
}

public class ServicesController(CmsDbContext db) : CrudController<Service>(db)
{
    protected override string Describe(Service s) => s.Title;
    protected override string Noun => "Service";

    protected override IQueryable<Service> WithChildren(IQueryable<Service> q) =>
        q.Include(s => s.Metrics.OrderBy(m => m.SortOrder))
         .Include(s => s.Included.OrderBy(m => m.SortOrder))
         .Include(s => s.Steps.OrderBy(m => m.SortOrder));

    protected override async Task PrepareAsync(Service m)
    {
        await FixSlugAsync(m, m.Title, x => x.Slug, (x, s) => x.Slug = s, Db.Services, x => x.Id);
        m.Metrics = CleanRows(m.Metrics, r => string.IsNullOrWhiteSpace(r.Label + r.Value), (r, i) => r.SortOrder = i);
        m.Included = CleanRows(m.Included, r => string.IsNullOrWhiteSpace(r.Title + r.Text), (r, i) => r.SortOrder = i);
        m.Steps = CleanRows(m.Steps, r => string.IsNullOrWhiteSpace(r.Title + r.Text), (r, i) => r.SortOrder = i);
    }

    protected override void ReplaceChildren(Service e, Service p)
    {
        Replace(e.Metrics, p.Metrics, r => { r.Id = 0; r.ServiceId = e.Id; });
        Replace(e.Included, p.Included, r => { r.Id = 0; r.ServiceId = e.Id; });
        Replace(e.Steps, p.Steps, r => { r.Id = 0; r.ServiceId = e.Id; });
    }
}

public class PostsController(CmsDbContext db) : CrudController<Post>(db)
{
    protected override string Describe(Post p) => p.Title;
    protected override string Noun => "Blog post";

    // Newest first, like the blog.
    protected override IQueryable<Post> Ordered(IQueryable<Post> q) => q.OrderByDescending(p => p.PublishedOn).ThenBy(p => p.SortOrder);

    protected override IQueryable<Post> WithChildren(IQueryable<Post> q) => q.Include(p => p.Blocks.OrderBy(b => b.SortOrder));

    protected override async Task PrepareAsync(Post m)
    {
        await FixSlugAsync(m, m.Title, x => x.Slug, (x, s) => x.Slug = s, Db.Posts, x => x.Id);
        m.Blocks = CleanRows(m.Blocks, r => string.IsNullOrWhiteSpace(r.Text), (r, i) => r.SortOrder = i);
    }

    protected override void ReplaceChildren(Post e, Post p) =>
        Replace(e.Blocks, p.Blocks, r => { r.Id = 0; r.PostId = e.Id; });
}

public class ReviewsController(CmsDbContext db) : CrudController<Review>(db)
{
    protected override string Describe(Review r) => r.Name;
    protected override string Noun => "Review";
}

public class FaqController(CmsDbContext db) : CrudController<Faq>(db)
{
    protected override string Describe(Faq f) => f.Question;
    protected override string Noun => "Question";
}

public class ToolsController(CmsDbContext db) : CrudController<Tool>(db)
{
    protected override string Describe(Tool t) => t.Name;
    protected override string Noun => "Tool";
}

public class ExperienceController(CmsDbContext db) : CrudController<Experience>(db)
{
    protected override string Describe(Experience e) => e.Title;
    protected override string Noun => "Experience";
}
