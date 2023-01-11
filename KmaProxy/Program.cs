using KmaProxy;

var client = new KmaClient();

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseKestrel();

builder.Services.AddSingleton(client);
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();

var app = builder.Build();

app.UseStaticFiles(new StaticFileOptions { RequestPath = "/static" });

app.MapControllerRoute(name: "default", pattern: "{controller}/{*path}");

app.Run();