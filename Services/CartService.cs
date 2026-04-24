using System.Text.Json;
using Jazmin.Data;
using Jazmin.Models;
using Jazmin.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Jazmin.Services;

public interface ICartService
{
    Task<CartViewModel> GetAsync(HttpContext http);
    Task AddAsync(HttpContext http, int productId, string? size, int qty = 1);
    Task UpdateQtyAsync(HttpContext http, int productId, string? size, int qty);
    Task RemoveAsync(HttpContext http, int productId, string? size);
    Task ClearAsync(HttpContext http);
    Task MergeGuestCartOnLoginAsync(HttpContext http, string userId);
    Task<int> GetItemCountAsync(HttpContext http);
}

public class CartService : ICartService
{
    private const string SessionKey = "jazmin_cart";
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userMgr;

    public CartService(ApplicationDbContext db, UserManager<ApplicationUser> userMgr)
    {
        _db = db;
        _userMgr = userMgr;
    }

    private record GuestLine(int ProductId, string? Size, int Quantity);

    private List<GuestLine> LoadGuest(HttpContext http)
    {
        var json = http.Session.GetString(SessionKey);
        if (string.IsNullOrEmpty(json)) return new();
        try { return JsonSerializer.Deserialize<List<GuestLine>>(json) ?? new(); }
        catch { return new(); }
    }

    private void SaveGuest(HttpContext http, List<GuestLine> lines) =>
        http.Session.SetString(SessionKey, JsonSerializer.Serialize(lines));

    public async Task<CartViewModel> GetAsync(HttpContext http)
    {
        var userId = _userMgr.GetUserId(http.User);
        if (!string.IsNullOrEmpty(userId))
        {
            var items = await _db.CartItems
                .Include(c => c.Product).ThenInclude(p => p!.Images)
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return new CartViewModel
            {
                Lines = items.Where(i => i.Product != null).Select(i => new CartLine
                {
                    CartItemId = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.Product!.Name,
                    ProductSlug = i.Product.Slug,
                    ImageUrl = i.Product.PrimaryImageUrl,
                    UnitPrice = i.Product.Price,
                    Size = i.Size,
                    Quantity = i.Quantity,
                    Stock = i.Product.Stock
                }).ToList()
            };
        }

        var guest = LoadGuest(http);
        if (guest.Count == 0) return new CartViewModel();

        var ids = guest.Select(g => g.ProductId).Distinct().ToList();
        var products = await _db.Products.Include(p => p.Images)
            .Where(p => ids.Contains(p.Id)).ToListAsync();

        return new CartViewModel
        {
            Lines = guest
                .Select(g =>
                {
                    var p = products.FirstOrDefault(x => x.Id == g.ProductId);
                    if (p == null) return null;
                    return new CartLine
                    {
                        CartItemId = 0,
                        ProductId = p.Id,
                        ProductName = p.Name,
                        ProductSlug = p.Slug,
                        ImageUrl = p.PrimaryImageUrl,
                        UnitPrice = p.Price,
                        Size = g.Size,
                        Quantity = g.Quantity,
                        Stock = p.Stock
                    };
                })
                .Where(l => l != null)!
                .Cast<CartLine>()
                .ToList()
        };
    }

    public async Task AddAsync(HttpContext http, int productId, string? size, int qty = 1)
    {
        if (qty < 1) qty = 1;
        var product = await _db.Products.FindAsync(productId);
        if (product == null || !product.IsActive) return;

        var userId = _userMgr.GetUserId(http.User);
        if (!string.IsNullOrEmpty(userId))
        {
            var existing = await _db.CartItems.FirstOrDefaultAsync(c =>
                c.UserId == userId && c.ProductId == productId && c.Size == size);
            if (existing != null) existing.Quantity += qty;
            else _db.CartItems.Add(new CartItem { UserId = userId, ProductId = productId, Size = size, Quantity = qty });
            await _db.SaveChangesAsync();
            return;
        }

        var guest = LoadGuest(http);
        var g = guest.FirstOrDefault(x => x.ProductId == productId && x.Size == size);
        if (g != null)
        {
            guest.Remove(g);
            guest.Add(new GuestLine(productId, size, g.Quantity + qty));
        }
        else guest.Add(new GuestLine(productId, size, qty));
        SaveGuest(http, guest);
    }

    public async Task UpdateQtyAsync(HttpContext http, int productId, string? size, int qty)
    {
        if (qty <= 0) { await RemoveAsync(http, productId, size); return; }

        var userId = _userMgr.GetUserId(http.User);
        if (!string.IsNullOrEmpty(userId))
        {
            var item = await _db.CartItems.FirstOrDefaultAsync(c =>
                c.UserId == userId && c.ProductId == productId && c.Size == size);
            if (item != null) { item.Quantity = qty; await _db.SaveChangesAsync(); }
            return;
        }

        var guest = LoadGuest(http);
        guest.RemoveAll(x => x.ProductId == productId && x.Size == size);
        guest.Add(new GuestLine(productId, size, qty));
        SaveGuest(http, guest);
    }

    public async Task RemoveAsync(HttpContext http, int productId, string? size)
    {
        var userId = _userMgr.GetUserId(http.User);
        if (!string.IsNullOrEmpty(userId))
        {
            var item = await _db.CartItems.FirstOrDefaultAsync(c =>
                c.UserId == userId && c.ProductId == productId && c.Size == size);
            if (item != null) { _db.CartItems.Remove(item); await _db.SaveChangesAsync(); }
            return;
        }
        var guest = LoadGuest(http);
        guest.RemoveAll(x => x.ProductId == productId && x.Size == size);
        SaveGuest(http, guest);
    }

    public async Task ClearAsync(HttpContext http)
    {
        var userId = _userMgr.GetUserId(http.User);
        if (!string.IsNullOrEmpty(userId))
        {
            var items = await _db.CartItems.Where(c => c.UserId == userId).ToListAsync();
            _db.CartItems.RemoveRange(items);
            await _db.SaveChangesAsync();
        }
        http.Session.Remove(SessionKey);
    }

    public async Task MergeGuestCartOnLoginAsync(HttpContext http, string userId)
    {
        var guest = LoadGuest(http);
        if (guest.Count == 0) return;

        foreach (var g in guest)
        {
            var existing = await _db.CartItems.FirstOrDefaultAsync(c =>
                c.UserId == userId && c.ProductId == g.ProductId && c.Size == g.Size);
            if (existing != null) existing.Quantity += g.Quantity;
            else _db.CartItems.Add(new CartItem { UserId = userId, ProductId = g.ProductId, Size = g.Size, Quantity = g.Quantity });
        }
        await _db.SaveChangesAsync();
        http.Session.Remove(SessionKey);
    }

    public async Task<int> GetItemCountAsync(HttpContext http)
    {
        var userId = _userMgr.GetUserId(http.User);
        if (!string.IsNullOrEmpty(userId))
            return await _db.CartItems.Where(c => c.UserId == userId).SumAsync(c => (int?)c.Quantity) ?? 0;
        return LoadGuest(http).Sum(g => g.Quantity);
    }
}
