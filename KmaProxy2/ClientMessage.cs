using System.Net;
using System.Text;

namespace KmaProxy2;

public ref struct ClientMessage
{
    public ClientMessage()
    {
        Method = null;
        Endpoint = null;
        BaseAddress = null;
        PayloadStream = null;
    }

    public string? Method { get; set; }
    public string? Endpoint { get; set; }
    public string? BaseAddress { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public Stream PayloadStream { get; set; }

    public bool Valid => Method is not null &&
                         Endpoint is not null &&
                         BaseAddress is not null;
}