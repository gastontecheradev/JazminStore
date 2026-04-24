using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Jazmin.Models.ViewModels;

public class AdminDashboardViewModel
{
    public decimal RevenueTotal { get; set; }
    public decimal RevenueMonth { get; set; }
    public int OrdersTotal { get; set; }
    public int OrdersPending { get; set; }
    public int OrdersMonth { get; set; }
    public int ProductsActive { get; set; }
    public int ProductsOutOfStock { get; set; }
    public int CustomersCount { get; set; }
    public double AverageRating { get; set; }
    public int ReviewsCount { get; set; }
    public List<Order> RecentOrders { get; set; } = new();
    public List<(Product product, int sold)> TopProducts { get; set; } = new();
    public List<(string month, decimal revenue)> RevenueByMonth { get; set; } = new();
}

public class ProductEditViewModel
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    [Display(Name = "Nombre")]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    [Display(Name = "Descripción")]
    public string? Description { get; set; }

    [StringLength(300)]
    [Display(Name = "Descripción corta")]
    public string? ShortDescription { get; set; }

    [Required, Range(0, 9999999)]
    [Display(Name = "Precio (UYU)")]
    public decimal Price { get; set; }

    [Range(0, 9999999)]
    [Display(Name = "Precio anterior (opcional, para mostrar descuento)")]
    public decimal? ComparePrice { get; set; }

    [Range(0, 999999)]
    [Display(Name = "Stock")]
    public int Stock { get; set; }

    [Display(Name = "Activo")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Destacado en home")]
    public bool IsFeatured { get; set; }

    [Display(Name = "Marcar como NUEVO")]
    public bool IsNew { get; set; }

    [Required]
    [Display(Name = "Categoría")]
    public int CategoryId { get; set; }

    [StringLength(100)]
    [Display(Name = "Tallas (separadas por coma)")]
    public string SizesCsv { get; set; } = "XS,S,M,L";

    [StringLength(200)]
    [Display(Name = "Colores (separados por coma, opcional)")]
    public string? ColorsCsv { get; set; }

    [Display(Name = "Imágenes nuevas")]
    public List<IFormFile>? NewImages { get; set; }

    public List<ProductImage> ExistingImages { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
}

public class CategoryEditViewModel
{
    public int Id { get; set; }

    [Required, StringLength(60)]
    [Display(Name = "Nombre")]
    public string Name { get; set; } = string.Empty;

    [StringLength(250)]
    [Display(Name = "Descripción")]
    public string? Description { get; set; }

    [Display(Name = "Orden")]
    public int SortOrder { get; set; }

    [Display(Name = "Activa")]
    public bool IsActive { get; set; } = true;
}
