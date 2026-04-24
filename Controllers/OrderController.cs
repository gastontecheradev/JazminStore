using Jazmin.Data;
using Jazmin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jazmin.Controllers;

[Authorize]
public class OrderController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userMgr;

    public OrderController(ApplicationDbContext db, UserManager<ApplicationUser> userMgr)
    {
        _db = db;
        _userMgr = userMgr;
    }

    public async Task<IActionResult> Index()
    {
        var uid = _userMgr.GetUserId(User);
        var orders = await _db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product).ThenInclude(p => p!.Images)
            .Where(o => o.UserId == uid)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
        return View(orders);
    }

    public async Task<IActionResult> Confirm(int id)
    {
        var order = await LoadOwnedOrder(id);
        if (order == null) return NotFound();
        return View(order);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var order = await LoadOwnedOrder(id);
        if (order == null) return NotFound();
        return View(order);
    }

    public async Task<IActionResult> Pending(int id) => await Confirm(id);

    public async Task<IActionResult> Failed(int id)
    {
        var order = await LoadOwnedOrder(id);
        if (order == null) return NotFound();
        return View(order);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Rate(int orderId, int rating, string? comment)
    {
        var uid = _userMgr.GetUserId(User);
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == uid);
        if (order == null) return NotFound();
        if (order.Status != OrderStatus.Delivered)
        {
            TempData["OrderMessage"] = "Solo podés calificar pedidos entregados";
            return RedirectToAction(nameof(Detail), new { id = orderId });
        }

        rating = Math.Clamp(rating, 1, 5);
        order.Rating = rating;
        order.RatingComment = comment;
        order.RatedAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        TempData["OrderMessage"] = "¡Gracias por calificar tu compra!";
        return RedirectToAction(nameof(Detail), new { id = orderId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewProduct(int orderId, int productId, int rating, string? comment)
    {
        var uid = _userMgr.GetUserId(User);
        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == uid);

        if (order == null) return NotFound();
        if (order.Status != OrderStatus.Delivered)
        {
            TempData["OrderMessage"] = "Solo podés reseñar productos de pedidos entregados";
            return RedirectToAction(nameof(Detail), new { id = orderId });
        }
        if (!order.Items.Any(i => i.ProductId == productId))
        {
            TempData["OrderMessage"] = "Este producto no estaba en el pedido";
            return RedirectToAction(nameof(Detail), new { id = orderId });
        }

        var existing = await _db.Reviews.FirstOrDefaultAsync(r =>
            r.UserId == uid && r.ProductId == productId && r.OrderId == orderId);

        rating = Math.Clamp(rating, 1, 5);
        if (existing != null)
        {
            existing.Rating = rating;
            existing.Comment = comment;
        }
        else
        {
            _db.Reviews.Add(new Review
            {
                UserId = uid!,
                ProductId = productId,
                OrderId = orderId,
                Rating = rating,
                Comment = comment,
                IsApproved = true
            });
        }
        await _db.SaveChangesAsync();

        TempData["OrderMessage"] = "¡Gracias por tu reseña!";
        return RedirectToAction(nameof(Detail), new { id = orderId });
    }

    private async Task<Order?> LoadOwnedOrder(int id)
    {
        var uid = _userMgr.GetUserId(User);
        return await _db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product).ThenInclude(p => p!.Images)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == uid);
    }
}
