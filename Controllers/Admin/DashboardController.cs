using Jazmin.Data;
using Jazmin.Models;
using Jazmin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jazmin.Controllers.Admin;

[Area("Admin")]
[Authorize(Roles = DbSeeder.AdminRole)]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _db;

    public DashboardController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1);

        var paidOrDelivered = new[] { OrderStatus.Paid, OrderStatus.Preparing, OrderStatus.Shipped, OrderStatus.Delivered };

        var paidTotals = await _db.Orders
            .Where(o => paidOrDelivered.Contains(o.Status))
            .Select(o => new { o.Total, o.CreatedAt })
            .ToListAsync();

        var revenueTotal = paidTotals.Sum(o => o.Total);
        var revenueMonth = paidTotals.Where(o => o.CreatedAt >= monthStart).Sum(o => o.Total);

        var ordersTotal = await _db.Orders.CountAsync();
        var ordersPending = await _db.Orders.CountAsync(o => o.Status == OrderStatus.Pending);
        var ordersMonth = await _db.Orders.CountAsync(o => o.CreatedAt >= monthStart);

        var productsActive = await _db.Products.CountAsync(p => p.IsActive);
        var productsOutOfStock = await _db.Products.CountAsync(p => p.Stock == 0);

        var customersCount = await _db.Users.CountAsync();

        var reviews = await _db.Reviews.Where(r => r.IsApproved).ToListAsync();
        var avgRating = reviews.Count > 0 ? reviews.Average(r => (double)r.Rating) : 0;

        var recentOrders = await _db.Orders
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .Take(8)
            .ToListAsync();

        var topProductsData = await _db.OrderItems
            .Include(i => i.Product).ThenInclude(p => p!.Images)
            .Where(i => i.Order!.Status != OrderStatus.Cancelled && i.Order.Status != OrderStatus.Pending)
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Sold = g.Sum(x => x.Quantity) })
            .OrderByDescending(x => x.Sold)
            .Take(5)
            .ToListAsync();

        var topProductIds = topProductsData.Select(t => t.ProductId).ToList();
        var topProdEntities = await _db.Products
            .Include(p => p.Images)
            .Where(p => topProductIds.Contains(p.Id))
            .ToListAsync();

        var topProducts = topProductsData
            .Select(t => (product: topProdEntities.First(p => p.Id == t.ProductId), sold: t.Sold))
            .ToList();

        // Revenue by last 6 months
        var sixMonthsAgo = new DateTime(now.Year, now.Month, 1).AddMonths(-5);
        var paidOrdersLast6 = await _db.Orders
            .Where(o => paidOrDelivered.Contains(o.Status) && o.CreatedAt >= sixMonthsAgo)
            .Select(o => new { o.CreatedAt, o.Total })
            .ToListAsync();

        var revenueByMonth = new List<(string month, decimal revenue)>();
        for (int i = 5; i >= 0; i--)
        {
            var m = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
            var rev = paidOrdersLast6
                .Where(o => o.CreatedAt.Year == m.Year && o.CreatedAt.Month == m.Month)
                .Sum(o => o.Total);
            revenueByMonth.Add((m.ToString("MMM yy"), rev));
        }

        var vm = new AdminDashboardViewModel
        {
            RevenueTotal = revenueTotal,
            RevenueMonth = revenueMonth,
            OrdersTotal = ordersTotal,
            OrdersPending = ordersPending,
            OrdersMonth = ordersMonth,
            ProductsActive = productsActive,
            ProductsOutOfStock = productsOutOfStock,
            CustomersCount = customersCount,
            AverageRating = avgRating,
            ReviewsCount = reviews.Count,
            RecentOrders = recentOrders,
            TopProducts = topProducts,
            RevenueByMonth = revenueByMonth
        };

        return View(vm);
    }
}
