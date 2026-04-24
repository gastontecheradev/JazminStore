using Jazmin.Data;
using Jazmin.Models;
using Jazmin.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jazmin.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userMgr;

    public HomeController(ApplicationDbContext db, UserManager<ApplicationUser> userMgr)
    {
        _db = db;
        _userMgr = userMgr;
    }

    public async Task<IActionResult> Index()
    {
        var featured = await _db.Products
            .Include(p => p.Images)
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.IsFeatured)
            .OrderByDescending(p => p.CreatedAt)
            .Take(8)
            .ToListAsync();

        if (featured.Count < 4)
        {
            var fallback = await _db.Products
                .Include(p => p.Images)
                .Include(p => p.Category)
                .Where(p => p.IsActive && !featured.Select(f => f.Id).Contains(p.Id))
                .OrderByDescending(p => p.CreatedAt)
                .Take(8 - featured.Count)
                .ToListAsync();
            featured.AddRange(fallback);
        }

        var ratings = await _db.Reviews
            .Where(r => r.IsApproved && featured.Select(p => p.Id).Contains(r.ProductId))
            .GroupBy(r => r.ProductId)
            .Select(g => new { ProductId = g.Key, Avg = g.Average(r => (double)r.Rating), Count = g.Count() })
            .ToDictionaryAsync(x => x.ProductId, x => (x.Avg, x.Count));

        var categories = await _db.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .Take(6)
            .ToListAsync();

        var favs = new HashSet<int>();
        if (User.Identity?.IsAuthenticated ?? false)
        {
            var uid = _userMgr.GetUserId(User);
            favs = (await _db.Favorites.Where(f => f.UserId == uid).Select(f => f.ProductId).ToListAsync()).ToHashSet();
        }

        ViewBag.Ratings = ratings;
        ViewBag.Categories = categories;
        ViewBag.Favorites = favs;

        return View(featured);
    }

    public IActionResult About() => View();

    public IActionResult Contact() => View();

    [Route("/Home/Error")]
    public IActionResult Error(int? code = null)
    {
        ViewBag.Code = code;
        return View();
    }
}
