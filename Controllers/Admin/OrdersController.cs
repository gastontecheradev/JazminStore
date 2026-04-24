using Jazmin.Data;
using Jazmin.Models;
using Jazmin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jazmin.Controllers.Admin;

[Area("Admin")]
[Authorize(Roles = DbSeeder.AdminRole)]
public class OrdersController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IEmailService _email;

    public OrdersController(ApplicationDbContext db, IEmailService email)
    {
        _db = db;
        _email = email;
    }

    public async Task<IActionResult> Index(OrderStatus? status, string? search)
    {
        var q = _db.Orders
            .Include(o => o.Items)
            .AsQueryable();

        if (status.HasValue) q = q.Where(o => o.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            q = q.Where(o =>
                o.OrderNumber.Contains(search) ||
                o.CustomerName.Contains(search) ||
                o.CustomerEmail.Contains(search));
        }

        ViewBag.Status = status;
        ViewBag.Search = search;

        return View(await q.OrderByDescending(o => o.CreatedAt).ToListAsync());
    }

    public async Task<IActionResult> Detail(int id)
    {
        var order = await _db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product).ThenInclude(p => p!.Images)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound();
        return View(order);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, OrderStatus status)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null) return NotFound();

        var previous = order.Status;
        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        if (previous != status)
        {
            _ = SendStatusEmailAsync(order);
        }

        TempData["AdminMessage"] = "Estado actualizado ✓";
        return RedirectToAction(nameof(Detail), new { id });
    }

    private async Task SendStatusEmailAsync(Order order)
    {
        var label = order.Status switch
        {
            OrderStatus.Paid => "Pago recibido",
            OrderStatus.Preparing => "Preparando tu pedido",
            OrderStatus.Shipped => "¡Tu pedido fue enviado!",
            OrderStatus.Delivered => "¡Pedido entregado!",
            OrderStatus.Cancelled => "Pedido cancelado",
            _ => "Actualización de pedido"
        };

        var body = $@"
<div style='font-family:Arial,sans-serif;max-width:520px;margin:auto;color:#1E2A1A;background:#FFF8F8;padding:32px'>
    <h1 style='font-family:Georgia,serif;color:#3A5230;letter-spacing:4px'>JAZMÍN</h1>
    <h2 style='color:#3A5230'>{label}</h2>
    <p>Hola {order.CustomerName},</p>
    <p>El estado de tu pedido <strong>{order.OrderNumber}</strong> es ahora: <strong>{order.Status}</strong>.</p>
    {(order.Status == OrderStatus.Delivered ? "<p>¡Esperamos que disfrutes tu compra! Si podés, calificá tu pedido desde Mi cuenta.</p>" : "")}
</div>";
        await _email.SendAsync(order.CustomerEmail, $"{label} — {order.OrderNumber}", body);
    }
}
