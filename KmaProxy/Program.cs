using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Serialization;
using KmaProxy;
using KmaProxy.Models;

var client = new KmaClient();
var config = Configuration.Load();

if (config is null)
    return;

client.Init(config);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(client);
builder.Services.AddSingleton(config);
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();

WebApplication app;

if (config.Tls.Enabled)
{
    var cert = config.Tls.Cert.Href;
    var key = config.Tls.Key.Href;

    var x509Cert = X509Certificate2.CreateFromPemFile(cert, key);
    
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.Listen(IPAddress.Any, config.Tls.Port, listenOptions =>
        {
            listenOptions.UseHttps(x509Cert);
        });
    });
    
    app = builder.Build();
    
    app.UseHsts();
    app.UseHttpsRedirection();
}
else
{
    app = builder.Build();
}

app.UseStaticFiles(new StaticFileOptions { RequestPath = config.Static.Href });

app.MapControllerRoute(name: "default", pattern: "{controller}/{*path}");

app.Run();
