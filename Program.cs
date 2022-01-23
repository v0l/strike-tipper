using StrikeTipWidget.Strike;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
var configuration = builder.Configuration;

var strikeApiConfig = configuration.GetSection("StrikeApi").Get<PartnerApiSettings>();
services.AddSingleton(strikeApiConfig);

services.AddTransient<PartnerApi>();
services.AddControllers();
services.AddRouting();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseEndpoints(ep =>
{
    ep.MapControllers();
    ep.MapFallbackToFile("index.html");
});
app.Run();