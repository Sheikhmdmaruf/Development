using Microsoft.AspNetCore.Mvc;
using PortfolioAdmin.Web.Authorization;
using PortfolioAdmin.Web.Data;
using PortfolioAdmin.Web.Services;

namespace PortfolioAdmin.Web.Controllers;

public class MediaController(MediaStorage storage, AppDbContext db, ActivityService activity) : Controller
{
    /// <summary>Image upload used by every image field (CMS images, profile photos). Returns the stored path.</summary>
    [HttpPost("/media/upload"), RequestSizeLimit(MediaStorage.MaxBytes + 64 * 1024)]
    public async Task<IActionResult> Upload(IFormFile? file)
    {
        if (file is null) return BadRequest(new { error = "Choose an image to upload." });
        try
        {
            var media = await storage.SaveImageAsync(file, User.Id());
            activity.Log("media.uploaded", $"Uploaded image {media.OriginalName}", "Media");
            await db.SaveChangesAsync();
            return Json(new { path = media.Path, id = media.Id });
        }
        catch (MediaException ex) { return BadRequest(new { error = ex.Message }); }
    }
}
