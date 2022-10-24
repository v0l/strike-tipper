using StrikeTipWidget;
using StrikeTipWidget.Strike;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
var configuration = builder.Configuration;

var strikeApiConfig = configuration.GetSection("StrikeApi").Get<PartnerApiSettings>();
services.AddSingleton(strikeApiConfig);

var mainConfig = configuration.GetSection("Tipper").Get<TipperConfig>();
services.AddSingleton(mainConfig);

var seqSettings = configuration.GetSection("Seq");
builder.Logging.AddSeq(seqSettings);

services.AddTransient<PartnerApi>();
services.AddHostedService<WebhookSetupService>();
services.AddSingleton<Broker>();
services.AddMemoryCache();

services.AddCors();
services.AddControllers().AddNewtonsoftJson();
services.AddRouting();

var app = builder.Build();

app.UseCors(options =>
{
    if (mainConfig.CORSOrigins != default)
    {
        options.WithOrigins(mainConfig.CORSOrigins.Select(a => a.ToString()).ToArray());
    }
    else
    {
        options.AllowAnyOrigin();
    }
    options.AllowAnyHeader();
    options.AllowAnyMethod();
    options.AllowCredentials();
});
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var headers = context.Request.Headers
        .Where(a => !a.Key.Equals("cookie", StringComparison.InvariantCultureIgnoreCase))
        .GroupBy(a => a.Key)
        .ToDictionary(a => a.Key, b => b.Select(c => c.Value.First()));
    logger.LogDebug("Handeling request {path} {headers}", context.Request.Path, headers);

    try
    {
        await next();
    }
    catch (Exception ex)
    {
        logger.LogError("Error handeling request {path} {exception} {headers}", context.Request.Path, ex, headers);
    }
});

app.UseWebSockets();
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/events")
    {
        var broker = context.RequestServices.GetRequiredService<Broker>();
        var logger = context.RequestServices.GetRequiredService<ILogger<WebsocketHandler>>();
        var api = context.RequestServices.GetRequiredService<PartnerApi>();

        if (context.WebSockets.IsWebSocketRequest)
        {
            var sock = await context.WebSockets.AcceptWebSocketAsync();
            using var handler = new WebsocketHandler(sock, context.RequestAborted,
                broker, logger, api);

            await handler.WaitForExit;
        }
        else
        {
            context.Response.StatusCode = (int)HttpStatusCode.UpgradeRequired;
        }
    }
    else
    {
        await next();
    }
});

app.UseStaticFiles();
app.UseRouting();
app.UseEndpoints(ep =>
{
    app.MapControllers();
    ep.MapFallbackToFile("index.html");
});
app.Run();