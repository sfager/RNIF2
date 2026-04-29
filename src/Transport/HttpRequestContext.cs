using System.Net;

namespace RNIF2.Transport;

public sealed class HttpRequestContext
{
    public Stream RequestBody { get; }
    public string ContentType { get; }
    public string HttpMethod { get; }
    public IPAddress? RemoteAddress { get; }
    public HttpListenerResponse Response { get; }

    private HttpRequestContext(
        Stream requestBody,
        string contentType,
        string httpMethod,
        IPAddress? remoteAddress,
        HttpListenerResponse response)
    {
        RequestBody = requestBody;
        ContentType = contentType;
        HttpMethod = httpMethod;
        RemoteAddress = remoteAddress;
        Response = response;
    }

    public static HttpRequestContext From(HttpListenerContext ctx) =>
        new(
            ctx.Request.InputStream,
            ctx.Request.ContentType ?? string.Empty,
            ctx.Request.HttpMethod,
            ctx.Request.RemoteEndPoint?.Address,
            ctx.Response);
}
