using System.Globalization;
using System.Text;
using BTCPayServer.Lightning;
using LNURL;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using QRCoder;
using StrikeTipWidget.Strike;

namespace StrikeTipWidget.Controllers;

[Route("pay/{user}")]
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
    public async Task<IActionResult> GetPayConfig([FromRoute]string user)
    {
        var baseUrl = _config?.BaseUrl ?? new Uri($"{Request.Scheme}://{Request.Host}");

        var profile = await _api.GetProfile(user);
        if (profile != default)
        {
            var lnpayUri = new Uri(baseUrl, $"pay/{user}/lnurl/payRequest");
            var lnurl = LNURL.LNURL.EncodeBech32(lnpayUri).ToUpper();

            using var qrData = QRCoder.QRCodeGenerator.GenerateQrCode(
                Encoding.UTF8.GetBytes(lnurl),
                QRCodeGenerator.ECCLevel.M);
            using var qrCode = new QRCoder.PngByteQRCode(qrData);
            return new JsonResult(new
            {
                url = lnurl,
                qr = qrCode.GetGraphic(20),
                profile
            });
        }

        return NotFound();
    }

    [HttpGet]
    [Route("lnurl/payRequest")]
    public IActionResult GetLNURLConfig([FromRoute] string user)
    {
        var baseUrl = _config?.BaseUrl ?? new Uri($"{Request.Scheme}://{Request.Host}");
        var id = Guid.NewGuid();

        var metadata = new List<string[]>()
        {
            new[] {"text/plain", "tip"}
        };
        
        var req = new LNURLPayRequest()
        {
            Callback = new Uri(baseUrl, $"pay/{user}/lnurl/payRequest/invoice?id={id}"),
            MaxSendable = 100_000_000,
            MinSendable = 100,
            Metadata = JsonConvert.SerializeObject(metadata),
            Tag = "payRequest"
        };

        return Content(JsonConvert.SerializeObject(req), "application/json");
    }

    [HttpGet]
    [Route("lnurl/payRequest/invoice")]
    public async Task<IActionResult> GetInvoice([FromRoute] string user, [FromQuery] Guid id, [FromQuery] long amount)
    {
        try
        {
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