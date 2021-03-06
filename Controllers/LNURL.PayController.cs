using System.Globalization;
using BTCPayServer.Lightning;
using LNURL;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using StrikeTipWidget.Strike;

namespace StrikeTipWidget.Controllers;

[Route("lnurlpay/{user}")]
public class PayController : Controller
{
    private readonly PartnerApi _api;
    private readonly TipperConfig _config;

    public PayController(PartnerApi api, TipperConfig config)
    {
        _api = api;
        _config = config;
    }

    [HttpGet]
    public async Task<IActionResult> GetPayConfig([FromRoute] string user, [FromQuery] string? description,
        [FromQuery] ulong? amount = null)
    {
        var baseUrl = _config?.BaseUrl ?? new Uri($"{Request.Scheme}://{Request.Host}");

        var profile = await _api.GetProfile(user);
        if (profile != default)
        {
            var lnpayUri = new Uri(baseUrl, $"lnurlpay/{user}/payRequest?description={description}&amount={amount}");
            var lnurl = LNURL.LNURL.EncodeBech32(lnpayUri).ToUpper();

            return new JsonResult(new
            {
                url = lnurl,
                profile
            });
        }

        return NotFound();
    }

    [HttpGet]
    [Route("payRequest")]
    public IActionResult GetLNURLConfig([FromRoute] string user, [FromQuery] string? description,
        [FromQuery] ulong? amount = null)
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
            MaxSendable = amount ?? 100_000_000,
            MinSendable = amount ?? 100,
            Metadata = JsonConvert.SerializeObject(metadata),
            Tag = "payRequest"
        };

        return Content(JsonConvert.SerializeObject(req), "application/json");
    }

    [HttpGet]
    [Route("payRequest/invoice")]
    public async Task<IActionResult> GetInvoice([FromRoute] string user, [FromQuery] Guid id, [FromQuery] long amount)
    {
        try
        {
            var profile = await _api.GetProfile(user);
            if (!(profile?.CanReceive ?? false) ||
                profile.Currencies.Where(a => a.IsAvailable).All(a => a.Currency != Currencies.BTC))
            {
                throw new InvalidOperationException("Account cannot receive BTC");
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
                Description = "tip",
                Handle = user
            });
            if (invoice == null) throw new Exception("Failed to get invoice!");

            var quote = await _api.GetInvoiceQuote(invoice.InvoiceId);
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