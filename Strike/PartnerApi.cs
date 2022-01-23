using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace StrikeTipWidget.Strike;

public class PartnerApi
{
    private readonly HttpClient _client;
    private readonly PartnerApiSettings _settings;

    public PartnerApi(PartnerApiSettings settings)
    {
        _client = new HttpClient
        {
            BaseAddress = settings.Uri ?? new Uri("https://api.strike.me/")
        };
        _settings = settings;

        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {settings.ApiKey}");
    }

    public Task<Invoice?> GenerateInvoice(CreateInvoiceRequest invoiceRequest)
    {
        var path = !string.IsNullOrEmpty(invoiceRequest.Handle)
            ? $"/v1/invoices/handle/{invoiceRequest.Handle}"
            : "/v1/invoices";
        return SendRequest<Invoice>(HttpMethod.Post, path, invoiceRequest);
    }

    public Task<Profile?> GetProfile(string handle)
    {
        return SendRequest<Profile>(HttpMethod.Get, $"/v1/accounts/handle/{handle}/profile");
    }
    
    public Task<Invoice?> GetInvoice(Guid id)
    {
        return SendRequest<Invoice>(HttpMethod.Get, $"/v1/invoices/{id}");
    }

    public Task<InvoiceQuote?> GetInvoiceQuote(Guid id)
    {
        return SendRequest<InvoiceQuote>(HttpMethod.Post, $"/v1/invoices/{id}/quote");
    }
    
    private async Task<TReturn?> SendRequest<TReturn>(HttpMethod method, string path, object? bodyObj = default)
        where TReturn : class
    {
        var request = new HttpRequestMessage(method, path);
        if (bodyObj != default)
        {
            var reqJson = JsonConvert.SerializeObject(bodyObj);
            request.Content = new StringContent(reqJson, Encoding.UTF8, "application/json");
        }

        var rsp = await _client.SendAsync(request);
        var okResponse = method.Method switch
        {
            "POST" => HttpStatusCode.Created,
            _ => HttpStatusCode.OK
        };

        var json = await rsp.Content.ReadAsStringAsync();
        return rsp.StatusCode == okResponse ? JsonConvert.DeserializeObject<TReturn>(json) : default;
    }
}

public class Profile
{
    [JsonProperty("handle")]
    public string? Handle { get; init; }
    
    [JsonProperty("avatarUrl")]
    public string? AvatarUrl { get; init; }
}

public class InvoiceQuote
{
    [JsonProperty("quoteId")]
    public Guid QuoteId { get; init; }
    
    [JsonProperty("description")]
    public string? Description { get; init; }
    
    [JsonProperty("lnInvoice")]
    public string? LnInvoice { get; init; }
    
    [JsonProperty("onchainAddress")]
    public string? OnChainAddress { get; init; }
    
    [JsonProperty("expiration")]
    public DateTimeOffset Expiration { get; init; }
    
    [JsonProperty("expirationInSec")]
    public ulong ExpirationSec { get; init; }
    
    [JsonProperty("targetAmount")]
    public CurrencyAmount? TargetAmount { get; init; }
    
    [JsonProperty("sourceAmount")]
    public CurrencyAmount? SourceAmount { get; init; }
    
    [JsonProperty("conversionRate")]
    public ConversionRate? ConversionRate { get; init; }
}

public class ConversionRate
{
    [JsonProperty("amount")]
    public string? Amount { get; init; }
    
    [JsonProperty("sourceCurrency")]
    [JsonConverter(typeof(StringEnumConverter))]
    public Currencies Source { get; init; }
    
    [JsonProperty("targetCurrency")]
    [JsonConverter(typeof(StringEnumConverter))]
    public Currencies Target { get; init; }
}

public class ErrorResponse : Exception
{
    public ErrorResponse(string message) : base(message)
    {
    }
}

public class CreateInvoiceRequest
{
    public string? CorrelationId { get; init; }
    public string? Description { get; init; }
    public CurrencyAmount? Amount { get; init; }
    public string? Handle { get; init; }
}

public class CurrencyAmount
{
    [JsonProperty("amount")]
    public string? Amount { get; init; }

    [JsonProperty("currency")]
    [JsonConverter(typeof(StringEnumConverter))]
    public Currencies? Currency { get; init; }
}

public enum Currencies
{
    BTC,
    USD,
    EUR,
    GBP,
    USDT
}

public class Invoice
{
    [JsonProperty("invoiceId")]
    public Guid InvoiceId { get; init; }
    
    [JsonProperty("amount")]
    public CurrencyAmount? Amount { get; init; }

    [JsonProperty("state")]
    [JsonConverter(typeof(StringEnumConverter))]
    public InvoiceState State { get; set; }

    [JsonProperty("created")]
    public DateTimeOffset? Created { get; init; }
    
    [JsonProperty("correlationId")]
    public string? CorrelationId { get; init; }
    
    [JsonProperty("description")]
    public string? Description { get; init; }
    
    [JsonProperty("issuerId")]
    public string? IssuerId { get; init; }
    
    [JsonProperty("receiverId")]
    public string? ReceiverId { get; init; }
    
    [JsonProperty("payerId")]
    public string? PayerId { get; init; }
}

public enum InvoiceState
{
    UNPAID,
    PENDING,
    PAID,
    CANCELLED
}

public class PartnerApiSettings
{
    public Uri? Uri { get; init; }
    public string ApiKey { get; init; }
}