using Jazmin.Services;
using Microsoft.AspNetCore.Mvc;

namespace Jazmin.Controllers;

public class CartController : Controller
{
    private readonly ICartService _cart;

    public CartController(ICartService cart) => _cart = cart;

    public async Task<IActionResult> Index()
    {
        var vm = await _cart.GetAsync(HttpContext);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId, string? size, int qty = 1, string? returnUrl = null)
    {
        await _cart.AddAsync(HttpContext, productId, size, qty);
        TempData["CartMessage"] = "Producto agregado al carrito ✓";

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            var count = await _cart.GetItemCountAsync(HttpContext);
            return Json(new { ok = true, count });
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQty(int productId, string? size, int qty)
    {
        await _cart.UpdateQtyAsync(HttpContext, productId, size, qty);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int productId, string? size)
    {
        await _cart.RemoveAsync(HttpContext, productId, size);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Clear()
    {
        await _cart.ClearAsync(HttpContext);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Count()
    {
        var count = await _cart.GetItemCountAsync(HttpContext);
        return Json(new { count });
    }
}
