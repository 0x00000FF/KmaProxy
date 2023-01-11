using Microsoft.AspNetCore.Mvc;

namespace KmaProxy.Controllers;

[ApiController]
[Route("[controller]/{*path}")]
public class FetchController : ControllerBase
{
    private KmaClient _client;
    private string _requestedPath;
    private HttpResponse? _response;
    
    public FetchController(KmaClient client, IHttpContextAccessor hca)
    {
        _client = client;

        if (hca.HttpContext is not null)
        {
            _requestedPath = hca.HttpContext.Request.RouteValues["path"]?.ToString() ?? "";
            _response = hca.HttpContext.Response;
        }
        else
        {
            throw new Exception("Path Not Found!");
        }
    }
    
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var result = await _client.ProxyGetHandler(_requestedPath);
        await result.CopyToAsync(_response.Body);
        
        return new EmptyResult();
    }
}