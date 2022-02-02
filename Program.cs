using System.Net;
using Microsoft.AspNetCore.WebSockets;
using StrikeTipWidget;
using StrikeTipWidget.Strike;

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

services.AddWebSockets(options =>
{
    options.AllowedOrigins.Add("https://localhost:3000");
    options.AllowedOrigins.Add(mainConfig.BaseUrl!.ToString());
});
services.AddControllers().AddNewtonsoftJson();
services.AddRouting();

var app = builder.Build();

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
            context.Response.StatusCode = (int) HttpStatusCode.UpgradeRequired;
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