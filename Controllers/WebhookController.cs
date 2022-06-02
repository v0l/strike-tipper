using System.Text;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using StrikeTipWidget.Strike;
using HMACSHA256 = System.Security.Cryptography.HMACSHA256;

namespace StrikeTipWidget.Controllers;

[Route("webhook")]
public class WebhookController : Controller
{
    private readonly ILogger<WebhookController> _logger;
    private readonly TipperConfig _config;
    private readonly Broker _broker;

    public WebhookController(TipperConfig config, ILogger<WebhookController> logger, Broker broker)
    {
        _config = config;
        _logger = logger;
        _broker = broker;
    }

    [HttpPost]
    public async Task<IActionResult> HandleWebhook()
    {
        using var sr = new StreamReader(Request.Body);
        var json = await sr.ReadToEndAsync();

        _logger.LogInformation("Got webhook event: {event}", json);
        
        var key = Encoding.UTF8.GetBytes(_config.WebhookSecret!);
        var hmac = HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(json));

        var hmacCaller = Request.Headers["X-Webhook-Signature"][0];

        var hmacHex = BitConverter.ToString(hmac).Replace("-", "");
        if (hmacCaller.Equals(hmacHex, 
                StringComparison.InvariantCultureIgnoreCase))
        {
            _logger.LogInformation("HMAC verify success!");

            var ev = JsonConvert.DeserializeObject<WebhookEvent>(json);
            if (ev != null)
            {
                await _broker.FireEvent(ev);
            }
        }
        else
        {
            _logger.LogWarning("HMAC verify failed! {expected} {got}", hmacCaller, hmacHex);
        }

        return Ok();
    }
}