using System.ComponentModel.DataAnnotations;

namespace Jazmin.Models;

public enum OrderStatus
{
    Pending = 0,       // recién creada, esperando confirmación de pago
    Paid = 1,          // pagada / confirmada por vendedora
    Preparing = 2,     // preparando el envío
    Shipped = 3,       // enviada
    Delivered = 4,     // entregada
    Cancelled = 5
}

public enum PaymentMethod
{
    MercadoPago = 0,
    CashOnDelivery = 1,
    BankTransfer = 2
}

public enum ShippingZone
{
    Montevideo = 0,
    Interior = 1,
    Pickup = 2
}

public class Order
{
    public int Id { get; set; }

    [StringLength(20)]
    public string OrderNumber { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    [Required, StringLength(120)]
    public string CustomerName { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string CustomerEmail { get; set; } = string.Empty;

    [Required, StringLength(30)]
    public string CustomerPhone { get; set; } = string.Empty;

    [StringLength(200)]
    public string? ShippingAddress { get; set; }

    [StringLength(80)]
    public string? ShippingCity { get; set; }

    [StringLength(80)]
    public string? ShippingDepartment { get; set; }

    [StringLength(20)]
    public string? ShippingPostalCode { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public ShippingZone ShippingZone { get; set; }
    public decimal ShippingCost { get; set; }

    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }

    public PaymentMethod PaymentMethod { get; set; }

    [StringLength(120)]
    public string? PaymentReference { get; set; } // MP payment id

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    // Overall order rating (1-5), after delivery
    public int? Rating { get; set; }

    [StringLength(1000)]
    public string? RatingComment { get; set; }

    public DateTime? RatedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<OrderItem> Items { get; set; } = new();
}
