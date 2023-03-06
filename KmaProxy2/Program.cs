using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.InteropServices.ComTypes;
using System.Security.AccessControl;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;

namespace KmaProxy2;

public static class Program
{
    private static Configuration? configuration;
    private static X509Certificate? certificate;

    private static void ElevatePrivileges()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new NotSupportedException();
        }
        
        var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);

        if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
        {
            throw new PrivilegeNotHeldException();
        }
    }
    private static async Task Main(string[] args)
    {
        if (OperatingSystem.IsWindows())
        {
            ElevatePrivileges();
        }
        
        configuration = Configuration.Load();

        if (configuration is null)
        {
            return;
        }

        var listener = new HttpListener();
        
        if (configuration.Tls.Enabled)
        {
            certificate = X509Certificate2.CreateFromPemFile(
                configuration.Tls.Cert.Href, configuration.Tls.Key.Href);
            listener.Prefixes.Add($"https://+:{configuration.Tls.Port}/");
        }
        else
        {
            listener.Prefixes.Add($"http://+:{configuration.Tls.Port}/");
        }

        Console.WriteLine("Starting server at 0.0.0.0:{0}...", configuration.Tls.Port);
        
        await BeginServer(listener, configuration.Tls.Enabled);
    }

    private static async Task BeginServer(HttpListener listener, bool tlsEnabled = false)
    {
        listener.Start();

        while (true)
        {
            var context = await listener.GetContextAsync();
            _ = HandleContext(context);
        }
    }

    private static async Task HandleContext(HttpListenerContext context)
    {
        var clientMessage = CreateClientMessage(context.Request);

        if (clientMessage.PayloadStream == Stream.Null)
        {
            await ResponseWithStatic(context, clientMessage);
        }
        else
        {
            await RelayContext(context, clientMessage);
        }

        context.Response.Close();
    }

    private static async Task ReplyNotFound(HttpListenerContext context)
    {
        context.Response.StatusCode = 404;

        var outputStream = context.Response.OutputStream;
        using var streamWriter = new StreamWriter(outputStream);

        await streamWriter.WriteAsync("404 Not Found");
    }

    private static async Task ResponseWithStatic(HttpListenerContext context, ClientMessage message)
    {
        if (message.Endpoint is null)
        {
            await ReplyNotFound(context);
            return;
        }

        try
        {
            var staticStream = new StaticDelivery(message.Endpoint);

            context.Response.ContentType = context.Response.ContentType;
            staticStream.ContentStream.CopyTo(context.Response.OutputStream);
        }
        catch
        {
            await ReplyNotFound(context);
        }

    }

    private static async Task RelayContext(HttpListenerContext context, ClientMessage message)
    {
        var relayResponse = RelayClientMessage(message);
        var response = context.Response;

        response.Headers.Clear();

        foreach (var keyValuePair in relayResponse.Headers)
        {
            response.Headers.Add($"{keyValuePair.Key}: {keyValuePair.Value.FirstOrDefault()}\r\n");
        }

        foreach (var keyValuePair in relayResponse.Headers)
        {
            response.Headers.Add($"{keyValuePair.Key}: {keyValuePair.Value.FirstOrDefault()}\r\n");
        }

        await relayResponse.Content.ReadAsStream()
            .CopyToAsync(response.OutputStream);
    }

    private static ClientMessage CreateClientMessage(HttpListenerRequest request)
    {
        var message = new ClientMessage();
        
        var requestUri = request.RawUrl;
        var urlFrags = requestUri?.Split('/', 3, StringSplitOptions.TrimEntries);
        
        if (urlFrags is null || urlFrags.Length != 3)
        {
            throw new ArgumentException("Wrongfully formed url format.");
        }

        var reqName = urlFrags[1];

        if (configuration?.Static.Href == reqName && request.HttpMethod == "GET")
        {
            message.Endpoint = reqName + '/' + urlFrags[2];
            message.PayloadStream = Stream.Null;

            return message;
        }
        else if (configuration?.Maps.Route.Any(r => r.Id == reqName) == true)
        {
            var target = configuration.Maps.Route.First(r => r.Id == reqName).Value;

            message.Method = request.HttpMethod;
            message.BaseAddress = target;
            message.Endpoint = urlFrags[2];
            
            foreach (string headerName in request.Headers)
            {
                var headerValue = request.Headers[headerName];

                if (headerValue is null)
                {
                    continue;
                }
                else if (headerName.Equals("host", StringComparison.InvariantCultureIgnoreCase))
                {
                    var hostMatch = Regex.Match(target, "[a-zA-Z]://([a-zA-Z0-9가-힣.-:]+)/.*");
                    var hostName = hostMatch.Groups[1].Value;

                    message.Headers.Add(headerName, hostName);  
                }
                else
                {
                    message.Headers.Add(headerName, headerValue);
                }
            }

            message.PayloadStream = request.InputStream;
            return message;
        }

        throw new NotSupportedException("Client message cannot be generated.");
    }
    
    private static HttpResponseMessage RelayClientMessage(ClientMessage message)
    {
        var httpClientHandler = new HttpClientHandler();
        var httpClient = new HttpClient(httpClientHandler)
        {
            BaseAddress = new Uri(message.BaseAddress ?? "")
        };

        var httpMessage = new HttpRequestMessage(
            new HttpMethod(message.Method ?? "GET"), message.Endpoint);
        
        foreach (var keyValuePair in message.Headers)
        {
            httpMessage.Headers.Add(keyValuePair.Key, keyValuePair.Value);
        }

        var httpContent = new StreamContent(message.PayloadStream ?? Stream.Null);
        httpMessage.Content = httpContent;
        
        var response = httpClient.Send(httpMessage);
        return response;
    }

}