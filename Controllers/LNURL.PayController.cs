using System.Globalization;
using BTCPayServer.Lightning;
using LNURL;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using StrikeTipWidget.Strike;

namespace StrikeTipWidget.Controllers;

[Route("lnurlpay/{user}")]
public class PayController : Controller
{
    private readonly PartnerApi _api;
    private readonly TipperConfig _config;
    private readonly IMemoryCache _cache;

    public PayController(PartnerApi api, TipperConfig config, IMemoryCache cache)
    {
        _api = api;
        _config = config;
        _cache = cache;
    }

    [HttpGet]
    public async Task<IActionResult> GetPayConfig([FromRoute] string user, [FromQuery] string? description)
    {
        var baseUrl = _config?.BaseUrl ?? new Uri($"{Request.Scheme}://{Request.Host}");

        var profile = await _api.GetProfile(user);
        if (profile != default)
        {
            var lnpayUri = new Uri(baseUrl, $"lnurlpay/{user}/payRequest?description={Uri.EscapeDataString(description)}");
            var lnurl = LNURL.LNURL.EncodeBech32(lnpayUri);

            return new JsonResult(new
            {
                url = lnurl,
                profile
            });
        }

        return NotFound();
    }

    [HttpGet("payRequest")]
    public IActionResult GetLNURLConfig([FromRoute] string user, [FromQuery] string? description)
    {
        var baseUrl = _config?.BaseUrl ?? new Uri($"{Request.Scheme}://{Request.Host}");
        var id = Guid.NewGuid();

        var metadata = new List<string[]>()
        {
            new[] {"text/plain", description ?? string.Empty}
        };

        var req = new LNURLPayRequest()
        {
            Callback = new Uri(baseUrl, $"lnurlpay/{user}/payRequest/invoice?id={id}"),
            MaxSendable = LightMoney.Satoshis(10_000_000),
            MinSendable = LightMoney.Satoshis(1_000),
            Metadata = JsonConvert.SerializeObject(metadata),
            Tag = "payRequest"
        };

        _cache.Set(id, req, TimeSpan.FromMinutes(10));
        return Content(JsonConvert.SerializeObject(req), "application/json");
    }

    [HttpGet("payRequest/invoice")]
    public async Task<IActionResult> GetInvoice([FromRoute] string user, [FromQuery] Guid id, [FromQuery] long amount)
    {
        try
        {
            var profile = await _api.GetProfile(user);
            if (!(profile?.CanReceive ?? false))
            {
                throw new InvalidOperationException("Account cannot receive!");
            }

            var invoiceRequest = _cache.Get<LNURLPayRequest>(id);
            if (invoiceRequest == default)
            {
                throw new InvalidOperationException($"Cannot find request for invoice {id}");
            }

            var invoice = await _api.GenerateInvoice(new()
            {
                Amount = new()
                {
                    Amount = LightMoney.MilliSatoshis(amount)
                        .ToUnit(LightMoneyUnit.BTC)
                        .ToString(CultureInfo.InvariantCulture),
                    Currency = Currencies.BTC
                },
                CorrelationId = id.ToString(),
                Description = invoiceRequest.Metadata,
                Handle = user
            });
            if (invoice == null) throw new Exception("Failed to get invoice!");

            var quote = await _api.GetInvoiceQuote(invoice.InvoiceId, true);
            var rsp = new LNURLPayRequest.LNURLPayRequestCallbackResponse()
            {
                Pr = quote.LnInvoice
            };

            return Content(JsonConvert.SerializeObject(rsp), "application/json");
        }
        catch (Exception ex)
        {
            return Content(JsonConvert.SerializeObject(new
            {
                Status = "ERROR",
                Reason = ex.Message
            }));
        }
    }
}