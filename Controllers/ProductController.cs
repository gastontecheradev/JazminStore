using Jazmin.Data;
using Jazmin.Models;
using Jazmin.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jazmin.Controllers;

public class ProductController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userMgr;

    public ProductController(ApplicationDbContext db, UserManager<ApplicationUser> userMgr)
    {
        _db = db;
        _userMgr = userMgr;
    }

    [HttpGet("producto/{slug}")]
    public async Task<IActionResult> Detail(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return NotFound();

        var product = await _db.Products
            .Include(p => p.Category)
            .Include(p => p.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder))
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive);

        if (product == null) return NotFound();

        var reviews = await _db.Reviews
            .Include(r => r.User)
            .Where(r => r.ProductId == product.Id && r.IsApproved)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        double avg = reviews.Count > 0 ? reviews.Average(r => (double)r.Rating) : 0;

        var related = await _db.Products
            .Include(p => p.Images)
            .Where(p => p.IsActive && p.CategoryId == product.CategoryId && p.Id != product.Id)
            .OrderByDescending(p => p.CreatedAt)
            .Take(4)
            .ToListAsync();

        var relatedIds = related.Select(r => r.Id).ToList();
        var relatedRatings = await _db.Reviews
            .Where(r => r.IsApproved && relatedIds.Contains(r.ProductId))
            .GroupBy(r => r.ProductId)
            .Select(g => new { ProductId = g.Key, Avg = g.Average(r => (double)r.Rating), Count = g.Count() })
            .ToDictionaryAsync(x => x.ProductId, x => (x.Avg, x.Count));

        bool canReview = false;
        int? purchasedOrderId = null;
        bool isFav = false;

        if (User.Identity?.IsAuthenticated ?? false)
        {
            var uid = _userMgr.GetUserId(User);

            isFav = await _db.Favorites.AnyAsync(f => f.UserId == uid && f.ProductId == product.Id);

            var deliveredOrder = await _db.Orders
                .Include(o => o.Items)
                .Where(o => o.UserId == uid &&
                            o.Status == OrderStatus.Delivered &&
                            o.Items.Any(i => i.ProductId == product.Id))
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (deliveredOrder != null)
            {
                purchasedOrderId = deliveredOrder.Id;
                var alreadyReviewed = await _db.Reviews.AnyAsync(r =>
                    r.UserId == uid && r.ProductId == product.Id && r.OrderId == deliveredOrder.Id);
                canReview = !alreadyReviewed;
            }
        }

        var vm = new ProductDetailViewModel
        {
            Product = product,
            AverageRating = avg,
            ReviewCount = reviews.Count,
            Reviews = reviews,
            Related = related,
            RelatedRatings = relatedRatings,
            IsFavorite = isFav,
            CanReview = canReview,
            PurchasedOrderId = purchasedOrderId
        };

        return View(vm);
    }
}
