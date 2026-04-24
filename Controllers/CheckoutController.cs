using Jazmin.Data;
using Jazmin.Models;
using Jazmin.Models.ViewModels;
using Jazmin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jazmin.Controllers;

[Authorize]
public class CheckoutController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ICartService _cart;
    private readonly IShippingService _shipping;
    private readonly IMercadoPagoService _mp;
    private readonly IEmailService _email;
    private readonly UserManager<ApplicationUser> _userMgr;
    private readonly IConfiguration _config;

    public CheckoutController(
        ApplicationDbContext db,
        ICartService cart,
        IShippingService shipping,
        IMercadoPagoService mp,
        IEmailService email,
        UserManager<ApplicationUser> userMgr,
        IConfiguration config)
    {
        _db = db;
        _cart = cart;
        _shipping = shipping;
        _mp = mp;
        _email = email;
        _userMgr = userMgr;
        _config = config;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var cart = await _cart.GetAsync(HttpContext);
        if (cart.Lines.Count == 0)
        {
            TempData["CartMessage"] = "Tu carrito está vacío";
            return RedirectToAction("Index", "Cart");
        }

        var user = await _userMgr.GetUserAsync(User);
        var zone = _shipping.InferZone(user?.Department);

        var vm = new CheckoutViewModel
        {
            Cart = cart,
            CustomerName = user?.FullName ?? "",
            CustomerEmail = user?.Email ?? "",
            CustomerPhone = user?.Phone ?? "",
            ShippingAddress = user?.Address,
            ShippingCity = user?.City,
            ShippingDepartment = user?.Department,
            ShippingPostalCode = user?.PostalCode,
            ShippingZone = zone,
            ShippingCost = _shipping.GetCost(zone, cart.Subtotal),
            PaymentMethod = _mp.IsEnabled ? PaymentMethod.MercadoPago : PaymentMethod.CashOnDelivery
        };

        ViewBag.Departments = _shipping.GetDepartments();
        ViewBag.MpEnabled = _mp.IsEnabled;
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Place(CheckoutViewModel vm)
    {
        var cart = await _cart.GetAsync(HttpContext);
        if (cart.Lines.Count == 0)
        {
            TempData["CartMessage"] = "Tu carrito está vacío";
            return RedirectToAction("Index", "Cart");
        }

        vm.Cart = cart;

        // Validate shipping fields when not pickup
        if (vm.ShippingZone != ShippingZone.Pickup)
        {
            if (string.IsNullOrWhiteSpace(vm.ShippingAddress)) ModelState.AddModelError(nameof(vm.ShippingAddress), "Requerido");
            if (string.IsNullOrWhiteSpace(vm.ShippingCity)) ModelState.AddModelError(nameof(vm.ShippingCity), "Requerido");
            if (string.IsNullOrWhiteSpace(vm.ShippingDepartment)) ModelState.AddModelError(nameof(vm.ShippingDepartment), "Requerido");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Departments = _shipping.GetDepartments();
            ViewBag.MpEnabled = _mp.IsEnabled;
            vm.ShippingCost = _shipping.GetCost(vm.ShippingZone, cart.Subtotal);
            return View(nameof(Index), vm);
        }

        var uid = _userMgr.GetUserId(User)!;
        var shippingCost = _shipping.GetCost(vm.ShippingZone, cart.Subtotal);

        // Stock check
        foreach (var l in cart.Lines)
        {
            if (l.Quantity > l.Stock)
            {
                TempData["CartMessage"] = $"No hay suficiente stock de {l.ProductName}";
                return RedirectToAction("Index", "Cart");
            }
        }

        var order = new Order
        {
            OrderNumber = OrderNumberHelper.Generate(),
            UserId = uid,
            CustomerName = vm.CustomerName,
            CustomerEmail = vm.CustomerEmail,
            CustomerPhone = vm.CustomerPhone,
            ShippingAddress = vm.ShippingAddress,
            ShippingCity = vm.ShippingCity,
            ShippingDepartment = vm.ShippingDepartment,
            ShippingPostalCode = vm.ShippingPostalCode,
            ShippingZone = vm.ShippingZone,
            ShippingCost = shippingCost,
            Subtotal = cart.Subtotal,
            Total = cart.Subtotal + shippingCost,
            PaymentMethod = vm.PaymentMethod,
            Notes = vm.Notes,
            Status = OrderStatus.Pending,
            Items = cart.Lines.Select(l => new OrderItem
            {
                ProductId = l.ProductId,
                ProductName = l.ProductName,
                Size = l.Size,
                UnitPrice = l.UnitPrice,
                Quantity = l.Quantity
            }).ToList()
        };

        _db.Orders.Add(order);

        // Reserve stock
        foreach (var l in cart.Lines)
        {
            var p = await _db.Products.FindAsync(l.ProductId);
            if (p != null) p.Stock = Math.Max(0, p.Stock - l.Quantity);
        }

        await _db.SaveChangesAsync();
        await _cart.ClearAsync(HttpContext);

        // Send confirmation email (non-blocking behavior)
        _ = SendOrderCreatedEmailAsync(order);

        // Redirect depending on payment method
        if (vm.PaymentMethod == PaymentMethod.MercadoPago && _mp.IsEnabled)
        {
            var baseUrl = _config["Site:Url"]?.TrimEnd('/') ?? $"{Request.Scheme}://{Request.Host}";
            var (initPoint, sandbox, _) = await _mp.CreatePreferenceAsync(order, baseUrl);
            var url = string.IsNullOrWhiteSpace(initPoint) ? sandbox : initPoint;
            if (!string.IsNullOrWhiteSpace(url)) return Redirect(url);
        }

        return RedirectToAction("Confirm", "Order", new { id = order.Id });
    }

    private async Task SendOrderCreatedEmailAsync(Order order)
    {
        var items = string.Join("", order.Items.Select(i =>
            $"<tr><td style='padding:6px 0'>{i.ProductName}{(string.IsNullOrEmpty(i.Size) ? "" : $" · Talla {i.Size}")}</td>" +
            $"<td style='text-align:center'>{i.Quantity}</td>" +
            $"<td style='text-align:right'>$ {i.UnitPrice:N0}</td></tr>"));

        var html = $@"
<div style='font-family:Arial,sans-serif;max-width:560px;margin:auto;color:#1E2A1A;background:#FFF8F8;padding:32px'>
    <h1 style='font-family:Georgia,serif;color:#3A5230;letter-spacing:4px'>JAZMÍN</h1>
    <h2 style='color:#3A5230'>¡Gracias por tu compra, {order.CustomerName}!</h2>
    <p>Recibimos tu pedido <strong>{order.OrderNumber}</strong>.</p>
    <table style='width:100%;border-collapse:collapse;margin:16px 0'>
        <thead><tr style='border-bottom:1px solid #F0D5DC'>
            <th align='left'>Producto</th><th>Cant.</th><th align='right'>Precio</th>
        </tr></thead>
        <tbody>{items}</tbody>
    </table>
    <p style='text-align:right'>Subtotal: $ {order.Subtotal:N0}<br>
    Envío: $ {order.ShippingCost:N0}<br>
    <strong>Total: $ {order.Total:N0}</strong></p>
    <p style='color:#9A6070;font-size:13px;margin-top:32px'>Te vamos a contactar pronto para coordinar el envío.</p>
</div>";
        await _email.SendAsync(order.CustomerEmail, $"Confirmación de pedido {order.OrderNumber}", html);
    }
}
