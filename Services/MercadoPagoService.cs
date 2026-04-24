using Jazmin.Models;
using MercadoPago.Client.Preference;
using MercadoPago.Config;
using MercadoPago.Resource.Preference;

namespace Jazmin.Services;

public interface IMercadoPagoService
{
    bool IsEnabled { get; }
    Task<(string initPoint, string sandboxInitPoint, string preferenceId)> CreatePreferenceAsync(Order order, string baseUrl);
}

public class MercadoPagoOptions
{
    public string AccessToken { get; set; } = "";
    public string PublicKey { get; set; } = "";
    public string WebhookSecret { get; set; } = "";
    public bool Enabled { get; set; }
}

public class MercadoPagoService : IMercadoPagoService
{
    private readonly MercadoPagoOptions _opts;
    private readonly ILogger<MercadoPagoService> _log;

    public MercadoPagoService(IConfiguration config, ILogger<MercadoPagoService> log)
    {
        _opts = config.GetSection("MercadoPago").Get<MercadoPagoOptions>() ?? new MercadoPagoOptions();
        _log = log;
        if (!string.IsNullOrWhiteSpace(_opts.AccessToken) &&
            !_opts.AccessToken.Contains("REPLACE", StringComparison.OrdinalIgnoreCase))
        {
            MercadoPagoConfig.AccessToken = _opts.AccessToken;
        }
    }

    public bool IsEnabled => _opts.Enabled
        && !string.IsNullOrWhiteSpace(_opts.AccessToken)
        && !_opts.AccessToken.Contains("REPLACE", StringComparison.OrdinalIgnoreCase);

    public async Task<(string initPoint, string sandboxInitPoint, string preferenceId)> CreatePreferenceAsync(Order order, string baseUrl)
    {
        if (!IsEnabled)
        {
            _log.LogWarning("MercadoPago no está configurado. Devolviendo URL local de confirmación.");
            var fallback = $"{baseUrl}/Order/Confirm/{order.Id}";
            return (fallback, fallback, "");
        }

        var items = order.Items.Select(i => new PreferenceItemRequest
        {
            Id = i.ProductId.ToString(),
            Title = string.IsNullOrWhiteSpace(i.Size) ? i.ProductName : $"{i.ProductName} (Talla {i.Size})",
            Quantity = i.Quantity,
            CurrencyId = "UYU",
            UnitPrice = i.UnitPrice
        }).ToList();

        if (order.ShippingCost > 0)
        {
            items.Add(new PreferenceItemRequest
            {
                Id = "shipping",
                Title = "Envío",
                Quantity = 1,
                CurrencyId = "UYU",
                UnitPrice = order.ShippingCost
            });
        }

        var request = new PreferenceRequest
        {
            Items = items,
            Payer = new PreferencePayerRequest
            {
                Name = order.CustomerName,
                Email = order.CustomerEmail,
                Phone = new MercadoPago.Client.Common.PhoneRequest { Number = order.CustomerPhone }
            },
            ExternalReference = order.OrderNumber,
            BackUrls = new PreferenceBackUrlsRequest
            {
                Success = $"{baseUrl}/Order/Confirm/{order.Id}",
                Failure = $"{baseUrl}/Order/Failed/{order.Id}",
                Pending = $"{baseUrl}/Order/Pending/{order.Id}"
            },
            AutoReturn = "approved",
            NotificationUrl = $"{baseUrl}/Webhook/MercadoPago",
            StatementDescriptor = "JAZMIN"
        };

        try
        {
            var client = new PreferenceClient();
            Preference pref = await client.CreateAsync(request);
            return (pref.InitPoint ?? "", pref.SandboxInitPoint ?? "", pref.Id ?? "");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error al crear preferencia MP");
            var fallback = $"{baseUrl}/Order/Confirm/{order.Id}";
            return (fallback, fallback, "");
        }
    }
}
