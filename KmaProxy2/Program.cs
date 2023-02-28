using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace KmaProxy2;

public static class Program
{
    private static Configuration configuration;
    private static X509Certificate certificate;
    
    private static async Task Main(string[] args)
    {
        configuration = Configuration.Load();

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
        
        await BeginServer(listener, configuration.Tls.Enabled);
    }

    private static async Task BeginServer(HttpListener listener, bool tlsEnabled = false)
    {
        listener.Start();

        while (true)
        {
            var context = await listener.GetContextAsync();
            HandleContext(context);
        }
    }

    private static async Task HandleContext(HttpListenerContext context)
    {
        var relayResponse = RelayClientMessage(CreateClientMessage(context.Request));
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
        
        context.Response.Close();
    }

    private static ClientMessage CreateClientMessage(HttpListenerRequest request)
    {
        var message = new ClientMessage();
        
        var requestUri = request.RawUrl;
        var urlFrags = requestUri.Split('/', 3, StringSplitOptions.TrimEntries);
        
        var reqName = urlFrags[1];
        
        if (configuration.Maps.Route.Any(r => r.Id == reqName))
        {
            var target = configuration.Maps.Route.First(r => r.Id == reqName).Value;

            message.Method = request.HttpMethod;
            message.BaseAddress = target;
            message.Endpoint = urlFrags[2];
            
            foreach (string? headerName in request.Headers)
            {
                var headerValue = request.Headers[headerName];

                if (headerName.Equals("host", StringComparison.InvariantCultureIgnoreCase))
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

        return default;
    }
    
    private static HttpResponseMessage RelayClientMessage(ClientMessage message)
    {
        var httpClientHandler = new HttpClientHandler();
        var httpClient = new HttpClient(httpClientHandler);

        httpClient.BaseAddress = new Uri(message.BaseAddress);

        var httpMessage = new HttpRequestMessage(
            new HttpMethod(message.Method), message.Endpoint);
        
        foreach (var keyValuePair in message.Headers)
        {
            httpMessage.Headers.Add(keyValuePair.Key, keyValuePair.Value);
        }

        var httpContent = new StreamContent(message.PayloadStream);
        httpMessage.Content = httpContent;
        
        var response = httpClient.Send(httpMessage);
        return response;
    }

}