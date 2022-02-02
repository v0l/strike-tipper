using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks.Dataflow;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using StrikeTipWidget.Strike;

namespace StrikeTipWidget;

public class WebsocketHandler : IDisposable
{
    private readonly ILogger<WebsocketHandler> _logger;
    private readonly PartnerApi _api;
    private readonly Broker _broker;
    private readonly WebSocket _ws;
    private readonly HashSet<Guid> _deliveries = new();
    private readonly BufferBlock<WebhookEvent> _eventQueue = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly TaskCompletionSource _tcs = new();
    
    public WebsocketHandler(WebSocket ws, CancellationToken token, Broker broker, ILogger<WebsocketHandler> logger, PartnerApi api)
    {
        _ws = ws;
        _broker = broker;
        _logger = logger;
        _api = api;

        token.Register(() => _cts.Cancel());
        _cts.Token.Register(() => _tcs.SetResult());
        _broker.Handlehook += OnWebhook;
        _ = ReadTask();
        _ = WriteTask();
    }

    public Task WaitForExit => _tcs.Task;
    
    private async Task WriteTask()
    {
        while (!_cts.IsCancellationRequested)
        {
            var msg = await _eventQueue.ReceiveAsync(_cts.Token);
            await SendWidgetEvent(msg);
        }
    }

    private async Task SendWidgetEvent(WebhookEvent ev)
    {
        if (ev.EventType == "invoice.updated" && ev.Data?.EntityId != default)
        {
            string? from = null;
            var inv = await _api.GetInvoice(ev.Data.EntityId!.Value);
            //if (inv is not {State: InvoiceState.PAID}) return;
            
            if (inv.PayerId != default)
            {
                var profile = await _api.GetProfile(inv.PayerId.Value);
                if (profile != null)
                {
                    from = profile.Handle;
                }
            }

            var widgetEvent = new WidgetEvent()
            {
                Type = WidgetEvents.InvoicePaid,
                Data = new WidgetPaidEvent() {
                    Amount = decimal.Parse(inv?.Amount?.Amount ?? "0"),
                    Currency = inv?.Amount?.Currency.ToString() ?? "USD",
                    From = from,
                    Paid = ev.Created ?? DateTimeOffset.UtcNow
                }
            };

            await SendJson(new ClientResponse()
            {
                Type = ClientResponseTypes.WidgetEvent,
                Data = widgetEvent
            });
        }
    }
    
    private async Task ReadTask()
    {
        var readOffset = 0;
        using var mem = MemoryPool<byte>.Shared.Rent();
        
        while (!_cts.IsCancellationRequested)
        {
            var read = await _ws.ReceiveAsync(mem.Memory[readOffset..], _cts.Token);
            if (read.EndOfMessage)
            {
                try
                {
                    var len = readOffset + read.Count;
                    var json = Encoding.UTF8.GetString(mem.Memory[..len].Span);
                    _logger.LogDebug(json);

                    var cmd = JsonConvert.DeserializeObject<ClientCommand>(json);
                    if (cmd != default)
                    {
                        var response = await HandleCommand(cmd);
                        if (response != null)
                        {
                            await SendJson(response);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                finally
                {
                    readOffset = 0;
                }
            }
            else
            {
                readOffset += read.Count;
            }
        }
    }

    private Task SendJson<T>(T val)
    {
        var json = JsonConvert.SerializeObject(val);
        return _ws.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, _cts.Token);
    }
    
    private async Task<ClientResponse?> HandleCommand(ClientCommand msg)
    {
        switch (msg.Command)
        {
            case "listen" when msg.Args.Length == 1:
            {
                if (Guid.TryParse(msg.Args[0], out var g0))
                {
                    _deliveries.Add(g0);
                    return new()
                    {
                        Type = ClientResponseTypes.CommandResponse
                    };
                }
                break;
            }
            case "unlisten" when msg.Args.Length == 1:
            {
                if (Guid.TryParse(msg.Args[0], out var g0))
                {
                    _deliveries.Remove(g0);
                    return new()
                    {
                        Type = ClientResponseTypes.CommandResponse
                    };
                }
                break;
            }
        }

        return null;
    }
    
    private Task OnWebhook(WebhookEvent hookEvent)
    {
        if (hookEvent.Data?.EntityId != null)
        {
            if (_deliveries.Contains(hookEvent.Data.EntityId.Value))
            {
                _eventQueue.Post(hookEvent);
            }
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _broker.Handlehook -= OnWebhook;
    }
}

public sealed record ClientCommand
{
    [JsonProperty("cmd")]
    public string Command { get; init; }
    
    [JsonProperty("args")]
    public string[] Args { get; init; }
}

public sealed record ClientResponse
{
    [JsonProperty("type")]
    [JsonConverter(typeof(StringEnumConverter))]
    public ClientResponseTypes Type { get; init; }
    
    [JsonProperty("data")]
    public object? Data { get; init; }
}

public enum ClientResponseTypes
{
    Unknown,
    CommandResponse,
    WidgetEvent
}

public enum WidgetEvents
{
    Unknown,
    InvoicePaid,
    InvoiceCreated
}

public sealed record WidgetEvent
{
    [JsonConverter(typeof(StringEnumConverter))]
    [JsonProperty("type")]
    public WidgetEvents Type { get; init; }
    
    [JsonProperty("data")]
    public object? Data { get; init; }
}

public sealed record WidgetPaidEvent
{
    [JsonProperty("amount")]
    public decimal Amount { get; init; }
    
    [JsonProperty("currency")]
    public string? Currency { get; init; }
        
    [JsonProperty("from")]
    public string? From { get; init; }
        
    [JsonProperty("paid")]
    public DateTimeOffset Paid { get; init; }
}