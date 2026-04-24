using Jazmin.Data;
using Jazmin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jazmin.Controllers;

[Authorize]
public class FavoritesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userMgr;

    public FavoritesController(ApplicationDbContext db, UserManager<ApplicationUser> userMgr)
    {
        _db = db;
        _userMgr = userMgr;
    }

    public async Task<IActionResult> Index()
    {
        var uid = _userMgr.GetUserId(User);
        var favs = await _db.Favorites
            .Include(f => f.Product).ThenInclude(p => p!.Images)
            .Include(f => f.Product).ThenInclude(p => p!.Category)
            .Where(f => f.UserId == uid && f.Product!.IsActive)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

        var productIds = favs.Select(f => f.ProductId).ToList();
        var ratings = await _db.Reviews
            .Where(r => r.IsApproved && productIds.Contains(r.ProductId))
            .GroupBy(r => r.ProductId)
            .Select(g => new { ProductId = g.Key, Avg = g.Average(r => (double)r.Rating), Count = g.Count() })
            .ToDictionaryAsync(x => x.ProductId, x => (x.Avg, x.Count));

        ViewBag.Ratings = ratings;
        return View(favs.Select(f => f.Product!).Where(p => p != null).ToList());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int productId, string? returnUrl = null)
    {
        var uid = _userMgr.GetUserId(User);
        if (string.IsNullOrEmpty(uid)) return Challenge();

        var existing = await _db.Favorites.FirstOrDefaultAsync(f => f.UserId == uid && f.ProductId == productId);
        bool isFavNow;
        if (existing != null)
        {
            _db.Favorites.Remove(existing);
            isFavNow = false;
        }
        else
        {
            _db.Favorites.Add(new Favorite { UserId = uid, ProductId = productId });
            isFavNow = true;
        }
        await _db.SaveChangesAsync();

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return Json(new { ok = true, isFavorite = isFavNow });

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction(nameof(Index));
    }
}
