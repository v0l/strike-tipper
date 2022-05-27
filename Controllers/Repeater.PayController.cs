using System.Net;
using Microsoft.AspNetCore.Mvc;
using StrikeTipWidget.Strike;

namespace StrikeTipWidget.Controllers;

[Route("invoice")]
public class RepeaterInvoiceController : Controller
{
    private readonly PartnerApi _api;

    public RepeaterInvoiceController(PartnerApi api)
    {
        _api = api;
    }
    
    [HttpPost]
    [Route("new")]
    public async Task<InvoiceAndQuote?> GetNewQuote([FromBody]CreateInvoiceRequest request)
    {
        var inv = await _api.GenerateInvoice(request);
        if (inv == default) return default;

        var quote = await _api.GetInvoiceQuote(inv.InvoiceId);
        if (quote == default) return default;

        return new(inv, quote);
    }

    [HttpGet]
    [Route("{invoice:guid}")]
    public async Task<InvoiceAndQuote?> GetInvoice([FromRoute]Guid invoice)
    {
        var inv = await _api.GetInvoice(invoice);
        if (inv == default)
        {
            Response.StatusCode = (int)HttpStatusCode.NotFound;
            return null;
        }
        
        var quote = await _api.GetInvoiceQuote(invoice);
        if (quote == default)
        {
            Response.StatusCode = (int)HttpStatusCode.NotFound;
            return null;
        }

        return new(inv, quote);
    }

    public record class InvoiceAndQuote(Invoice Invoice, InvoiceQuote Quote);
}