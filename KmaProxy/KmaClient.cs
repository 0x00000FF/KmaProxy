using System.Net;
using System.Xml;
using System.Xml.Serialization;
using KmaProxy.Models;
using Microsoft.AspNetCore.Mvc;

namespace KmaProxy;

public class KmaClient
{
    private Dictionary<string, string> _routingMap = new();

    private CookieContainer _cookieContainer;
    private HttpClient _client;
    private HttpClientHandler _handler;

    public KmaClient()
    {
        _cookieContainer = new();
        
        _handler = new() { CookieContainer = _cookieContainer};
        _client = new(_handler);
    }

    public void Init(Configuration config)
    {
        foreach (var route in config.Maps.Route)
        {
            _routingMap.Add(route.Id, route.Value);
        }
    }

    public string? BuildUrl(string path)
    {
        var frags = path.Split("/", 2);
        
        var id = frags[0];
        var remainPath = string.Join("/", frags[1..]);

        if (!_routingMap.ContainsKey(id))
        {
            return null;
        }

        return _routingMap[id] + remainPath;
    }
    
    public async Task<HttpContent> ProxyGetHandler(string path)
    {
        var url = BuildUrl(path);
        var result = await _client.GetAsync(url);

        return result.Content;
    }
}