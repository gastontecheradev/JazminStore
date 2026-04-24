using Jazmin.Models;

namespace Jazmin.Services;

public interface IShippingService
{
    decimal GetCost(ShippingZone zone, decimal subtotal);
    IEnumerable<(ShippingZone zone, string label, decimal cost)> GetOptions(decimal subtotal);
    IReadOnlyList<string> GetDepartments();
    ShippingZone InferZone(string? department);
}

public class ShippingOptions
{
    public decimal MontevideoRate { get; set; } = 150;
    public decimal InteriorRate { get; set; } = 250;
    public decimal FreeShippingFrom { get; set; } = 0; // 0 = disabled
}

public class ShippingService : IShippingService
{
    private readonly ShippingOptions _opts;

    public ShippingService(IConfiguration config)
    {
        _opts = config.GetSection("Shipping").Get<ShippingOptions>() ?? new ShippingOptions();
    }

    public decimal GetCost(ShippingZone zone, decimal subtotal)
    {
        if (zone == ShippingZone.Pickup) return 0;
        if (_opts.FreeShippingFrom > 0 && subtotal >= _opts.FreeShippingFrom) return 0;
        return zone switch
        {
            ShippingZone.Montevideo => _opts.MontevideoRate,
            ShippingZone.Interior => _opts.InteriorRate,
            _ => 0
        };
    }

    public IEnumerable<(ShippingZone zone, string label, decimal cost)> GetOptions(decimal subtotal) => new[]
    {
        (ShippingZone.Montevideo, "Envío a Montevideo", GetCost(ShippingZone.Montevideo, subtotal)),
        (ShippingZone.Interior,   "Envío al Interior",  GetCost(ShippingZone.Interior, subtotal)),
        (ShippingZone.Pickup,     "Retiro en persona",  0m),
    };

    public IReadOnlyList<string> GetDepartments() => new[]
    {
        "Montevideo","Canelones","Maldonado","Rocha","Treinta y Tres","Cerro Largo",
        "Rivera","Artigas","Salto","Paysandú","Río Negro","Soriano","Colonia",
        "San José","Flores","Florida","Durazno","Lavalleja","Tacuarembó"
    };

    public ShippingZone InferZone(string? department) =>
        string.Equals(department, "Montevideo", StringComparison.OrdinalIgnoreCase)
            ? ShippingZone.Montevideo : ShippingZone.Interior;
}
