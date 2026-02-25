using System.Net;
using System.Text;

namespace ParserLib;

/// <summary>
/// DEPRECATED:
/// </summary>
/// <param name="settings"></param>
class HtmlLoader(IParserSettings settings): IDisposable
{
    private readonly Lazy<HttpClient> client = new Lazy<HttpClient>(() =>
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            UseCookies = true,
            CookieContainer = new CookieContainer(),
            Proxy = ProxyRotator.GetRandom()
        };

        var _client = new HttpClient(handler);

        #region headers
        
        _client.DefaultRequestHeaders.Add("User-Agent", UserAgentRotator.GetRandom());
        _client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
        _client.DefaultRequestHeaders.Add("Accept-Language", "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7");
        _client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
        _client.DefaultRequestHeaders.Add("Connection", "keep-alive");
        _client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
        _client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
        _client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
        _client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "none");
        _client.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
        _client.DefaultRequestHeaders.Add("Cache-Control", "max-age=0");

        #endregion

        return _client;
    });

    public async Task<Stream?> GetSourceByPageId(int id, CancellationToken cancellationToken)
    {
        var currentUrl = new StringBuilder(settings.BaseUrl);
        currentUrl.Append(settings.Path);
        currentUrl.Replace("{CurrentId}", id.ToString());

        var response = await client.Value.GetAsync(currentUrl.ToString(), cancellationToken);

        if(response != null && response.StatusCode == HttpStatusCode.OK)
            return await response.Content.ReadAsStreamAsync(cancellationToken)
                ?? throw new IOException();

        Dispose();

        return null;
    }

    public void Dispose()
    {
        client.Value?.Dispose();
        GC.SuppressFinalize(this);
    }
}