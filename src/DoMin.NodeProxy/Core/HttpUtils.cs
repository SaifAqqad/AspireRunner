using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace DoMin.Node.Core;

public static class HttpUtils
{
    private class HttpClientFactory
    {
        private readonly Lazy<HttpMessageHandler> lazyHandler;
        internal HttpClientFactory(Func<HttpClientHandler> handler) => 
            lazyHandler = new Lazy<HttpMessageHandler>(() => handler(), LazyThreadSafetyMode.ExecutionAndPublication);
        public HttpClient CreateClient() => new(lazyHandler.Value, disposeHandler: false);
    }
    public static Func<HttpClientHandler> HttpClientHandlerFactory { get; set; } = () => new() {
        UseDefaultCredentials = true,
        AutomaticDecompression = DecompressionMethods.Brotli | DecompressionMethods.Deflate | DecompressionMethods.GZip,
    };
    private static HttpClientFactory? clientFactory;
    public static Func<HttpClient> CreateClient { get; set; } = () => { 
        try
        {
            clientFactory ??= new(HttpClientHandlerFactory);
            return clientFactory.CreateClient();
        }
        catch (Exception)
        {
            return new HttpClient();
        }
    };
    public static string ReadToEnd(this Stream stream, Encoding encoding)
    {
        if (stream is MemoryStream ms)
            return ms.ReadToEnd();

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        using var reader = new StreamReader(stream, encoding, true, DefaultBufferSize, leaveOpen:true);
        return reader.ReadToEnd();
    }
    public static HttpClient Create() => CreateClient();
    public const int DefaultBufferSize = 8 * 1024;
    public static string ReadToEnd(this MemoryStream ms) => ReadToEnd(ms, Encoding.UTF8);
    public static Encoding UseEncoding { get; set; } = new UTF8Encoding(false);
    public static string GetStringFromUrl(this string url, string accept = "*/*",
        Action<HttpRequestMessage>? requestFilter = null, Action<HttpResponseMessage>? responseFilter = null)
    {
        return SendStringToUrl(url, method:HttpMethods.Get, accept: accept, 
            requestFilter: requestFilter, responseFilter: responseFilter);
    }
    public static string SendStringToUrl(this HttpClient client, string url, string method = HttpMethods.Post,
        string? requestBody = null, string? contentType = null, string accept = "*/*",
        Action<HttpRequestMessage>? requestFilter = null, Action<HttpResponseMessage>? responseFilter = null)
    {
        var httpReq = new HttpRequestMessage(new HttpMethod(method), url);
        httpReq.Headers.Add(HttpHeaders.Accept, accept);

        if (requestBody != null)
        {
            httpReq.Content = new StringContent(requestBody, UseEncoding);
            if (contentType != null)
                httpReq.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        }
        requestFilter?.Invoke(httpReq);

        var httpRes = client.Send(httpReq);
        responseFilter?.Invoke(httpRes);
        httpRes.EnsureSuccessStatusCode();
        return httpRes.Content.ReadAsStream().ReadToEnd(UseEncoding);
    }
    public static string SendStringToUrl(this string url, string method = HttpMethods.Post,
        string? requestBody = null, string? contentType = null, string accept = "*/*",
        Action<HttpRequestMessage>? requestFilter = null, Action<HttpResponseMessage>? responseFilter = null)
    {
        return Create().SendStringToUrl(url, method:method, requestBody:requestBody, 
            contentType:contentType, accept:accept, requestFilter:requestFilter, responseFilter:responseFilter);
    }
}