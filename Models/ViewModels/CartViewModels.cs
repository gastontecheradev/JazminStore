using System.ComponentModel.DataAnnotations;

namespace Jazmin.Models.ViewModels;

public class CartViewModel
{
    public List<CartLine> Lines { get; set; } = new();
    public decimal Subtotal => Lines.Sum(l => l.LineTotal);
    public int ItemCount => Lines.Sum(l => l.Quantity);
}

public class CartLine
{
    public int CartItemId { get; set; } // 0 for session-based guest cart
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSlug { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public string? Size { get; set; }
    public int Quantity { get; set; }
    public int Stock { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
}

public class CheckoutViewModel
{
    public CartViewModel Cart { get; set; } = new();

    [Required(ErrorMessage = "Ingresá tu nombre")]
    [StringLength(120)]
    [Display(Name = "Nombre completo")]
    public string CustomerName { get; set; } = string.Empty;

    [Required, EmailAddress]
    [Display(Name = "Email")]
    public string CustomerEmail { get; set; } = string.Empty;

    [Required, Phone]
    [Display(Name = "Teléfono")]
    public string CustomerPhone { get; set; } = string.Empty;

    [Display(Name = "Zona de envío")]
    public Models.ShippingZone ShippingZone { get; set; } = Models.ShippingZone.Montevideo;

    [Display(Name = "Dirección")]
    public string? ShippingAddress { get; set; }

    [Display(Name = "Ciudad / Localidad")]
    public string? ShippingCity { get; set; }

    [Display(Name = "Departamento")]
    public string? ShippingDepartment { get; set; }

    [Display(Name = "Código postal")]
    public string? ShippingPostalCode { get; set; }

    [StringLength(500)]
    [Display(Name = "Notas para la vendedora (opcional)")]
    public string? Notes { get; set; }

    [Display(Name = "Método de pago")]
    public Models.PaymentMethod PaymentMethod { get; set; } = Models.PaymentMethod.MercadoPago;

    public decimal ShippingCost { get; set; }
    public decimal Total => Cart.Subtotal + ShippingCost;
}
