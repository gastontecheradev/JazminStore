using Jazmin.Data;
using Jazmin.Models;
using Jazmin.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jazmin.Controllers;

public class CatalogController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userMgr;

    public CatalogController(ApplicationDbContext db, UserManager<ApplicationUser> userMgr)
    {
        _db = db;
        _userMgr = userMgr;
    }

    public async Task<IActionResult> Index(
        int? categoryId,
        string? search,
        string? sort,
        decimal? minPrice,
        decimal? maxPrice,
        int page = 1)
    {
        sort ??= "new";
        page = page < 1 ? 1 : page;
        const int pageSize = 12;

        var query = _db.Products
            .Include(p => p.Images)
            .Include(p => p.Category)
            .Where(p => p.IsActive);

        if (categoryId.HasValue && categoryId.Value > 0)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(p =>
                p.Name.Contains(s) ||
                (p.Description ?? "").Contains(s) ||
                (p.ShortDescription ?? "").Contains(s) ||
                p.Category!.Name.Contains(s));
        }

        if (minPrice.HasValue) query = query.Where(p => p.Price >= minPrice.Value);
        if (maxPrice.HasValue) query = query.Where(p => p.Price <= maxPrice.Value);

        query = sort switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "name" => query.OrderBy(p => p.Name),
            _ => query.OrderByDescending(p => p.IsNew).ThenByDescending(p => p.CreatedAt)
        };

        var totalCount = await query.CountAsync();
        var products = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var categories = await _db.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

        var categoryCounts = await _db.Products
            .Where(p => p.IsActive)
            .GroupBy(p => p.CategoryId)
            .Select(g => new { CatId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CatId, x => x.Count);

        var productIds = products.Select(p => p.Id).ToList();
        var ratings = await _db.Reviews
            .Where(r => r.IsApproved && productIds.Contains(r.ProductId))
            .GroupBy(r => r.ProductId)
            .Select(g => new { ProductId = g.Key, Avg = g.Average(r => (double)r.Rating), Count = g.Count() })
            .ToDictionaryAsync(x => x.ProductId, x => (x.Avg, x.Count));

        var favs = new HashSet<int>();
        if (User.Identity?.IsAuthenticated ?? false)
        {
            var uid = _userMgr.GetUserId(User);
            favs = (await _db.Favorites.Where(f => f.UserId == uid).Select(f => f.ProductId).ToListAsync()).ToHashSet();
        }

        var vm = new CatalogViewModel
        {
            Products = products,
            Categories = categories,
            SelectedCategoryId = categoryId,
            Search = search,
            Sort = sort,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            CategoryCounts = categoryCounts,
            Ratings = ratings,
            FavoriteIds = favs,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return View(vm);
    }
}
