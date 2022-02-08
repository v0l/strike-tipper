namespace StrikeTipWidget;

public class TipperConfig
{
    public Uri? BaseUrl { get; init; }

    public IEnumerable<Uri>? CORSOrigins { get; init; }
    
    public string? WebhookSecret { get; init; }
}