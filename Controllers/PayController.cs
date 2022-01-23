using System.Text;
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
        var host = _config?.Host ?? Request.Host.ToString();

        var profile = await _api.GetProfile(user);
        if (profile != default)
        {
            var lnpayUri = $"{Request.Scheme}://{host}/pay/{user}/lnurl/payRequest";
            var lnurl = LNURL.LNURL.EncodeBech32(new Uri(lnpayUri)).ToUpper();

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
        var host = _config?.Host ?? Request.Host.ToString();
        var id = Guid.NewGuid();

        var metadata = new List<string[]>()
        {
            new[] {"text/plain", "tip"}
        };
        
        var req = new LNURLPayRequest()
        {
            Callback = new Uri($"{Request.Scheme}://{host}/pay/{user}/lnurl/payRequest/invoice?id={id}"),
            MaxSendable = 100_000_000,
            MinSendable = 100,
            Metadata = JsonConvert.SerializeObject(metadata)
        };

        return Content(JsonConvert.SerializeObject(req), "application/json");
    }

    [HttpGet]
    [Route("lnurl/payRequest/invoice")]
    public IActionResult GetInvoice([FromRoute] string user, [FromQuery] Guid id, [FromQuery] string amount)
    {
        return NoContent();
    }
}