using System.Net;
using System.Xml;
using System.Xml.Serialization;
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
        LoadConfiguration();

        _cookieContainer = new();
        
        _handler = new() { CookieContainer = _cookieContainer};
        _client = new(_handler);
    }

    private void LoadConfiguration()
    {
        var contents = File.ReadAllText("configuration.xml");
        
        var document = new XmlDocument();
        document.LoadXml(contents);

        var node = document.SelectSingleNode("//maps");
        if (node is not null)
        {
            foreach (XmlNode kvset in node.ChildNodes)
            {
                var id = kvset.Attributes?["id"]?.Value;
                var val = kvset.Attributes?["value"]?.Value;

                if (id is null || val is null) continue;
                
                _routingMap.Add(id, val);
            }
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