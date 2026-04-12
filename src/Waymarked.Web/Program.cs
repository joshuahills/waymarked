var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapReverseProxy();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.Run();
