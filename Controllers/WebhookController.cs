using Jazmin.Data;
using Jazmin.Models;
using MercadoPago.Client.Payment;
using MercadoPago.Config;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jazmin.Controllers;

public class WebhookController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<WebhookController> _log;

    public WebhookController(ApplicationDbContext db, IConfiguration config, ILogger<WebhookController> log)
    {
        _db = db;
        _config = config;
        _log = log;
    }

    // POST /Webhook/MercadoPago
    [HttpPost]
    public async Task<IActionResult> MercadoPago()
    {
        try
        {
            var token = _config["MercadoPago:AccessToken"];
            if (string.IsNullOrEmpty(token) || token.Contains("REPLACE"))
            {
                _log.LogWarning("Webhook MP llegado pero MP no está configurado.");
                return Ok();
            }
            MercadoPagoConfig.AccessToken = token;

            var type = Request.Query["type"].ToString();
            if (string.IsNullOrEmpty(type)) type = Request.Query["topic"].ToString();

            string? idStr = Request.Query["data.id"].ToString();
            if (string.IsNullOrEmpty(idStr)) idStr = Request.Query["id"].ToString();

            if (type == "payment" && long.TryParse(idStr, out var paymentId))
            {
                var client = new PaymentClient();
                var payment = await client.GetAsync(paymentId);
                if (payment == null) return Ok();

                var extRef = payment.ExternalReference;
                var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderNumber == extRef);
                if (order == null)
                {
                    _log.LogWarning("Webhook MP: orden no encontrada para ref {Ref}", extRef);
                    return Ok();
                }

                order.PaymentReference = paymentId.ToString();
                order.UpdatedAt = DateTime.UtcNow;

                order.Status = payment.Status switch
                {
                    "approved" => OrderStatus.Paid,
                    "rejected" or "cancelled" => OrderStatus.Cancelled,
                    _ => order.Status
                };
                await _db.SaveChangesAsync();
                _log.LogInformation("Webhook MP: orden {Num} status {Status}", order.OrderNumber, order.Status);
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error procesando webhook MP");
            return Ok(); // always return 200 to MP so it doesn't retry forever
        }
    }
}
