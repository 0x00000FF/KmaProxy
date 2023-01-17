using System.Net;
using System.Xml;
using System.Xml.Serialization;
using KmaProxy.Models;
using Microsoft.AspNetCore.Mvc;

namespace KmaProxy;

public class KmaClient
{
    private Dictionary<string, string> _routingMap = new();
    private Dictionary<string, Tuple<Guid, DateTimeOffset>> _cacheList = new();

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
        if (!Directory.Exists("cache"))
            Directory.CreateDirectory("cache");
        
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

    public void CacheContent(string url, int maxAge, Stream stream)
    {
        var cacheId = Guid.NewGuid();
        var cacheExpires = DateTime.Now.AddSeconds(maxAge);
        
        _cacheList.Add(url, new (cacheId, cacheExpires) );

        using var cacheFile = File.Create($"cache/{cacheId}");
        
        stream.CopyTo(cacheFile);
        stream.Seek(0, SeekOrigin.Begin);
    }
    
    public async Task<Stream> ProxyGetHandler(string path)
    {
        var url = BuildUrl(path) ?? "";

        if (_cacheList.ContainsKey(url))
        {
            var id = _cacheList[url];
            var cachedPath = $"cache/{id.Item1}";
            
            if (!File.Exists(cachedPath))
            {
                _cacheList.Remove(url);
            }

            return File.OpenRead(cachedPath);
        }
        else
        {
            var result = await _client.GetAsync(url);
            var content = result.Content;
            var stream = await content.ReadAsStreamAsync();
            
            if (result.Headers.Contains("Cache-Control"))
            {
                var maxAgeValue = result.Headers.GetValues("Cache-Control")
                    .FirstOrDefault(h => h.Contains("max-age"));
                var maxAge = maxAgeValue?.Split("=").Last();

                if (int.TryParse(maxAge, out var maxAgeSecs))
                {
                    CacheContent(url, maxAgeSecs, stream);
                }
            }

            return stream;
        }
    }
}