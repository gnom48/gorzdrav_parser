using AngleSharp.Html.Parser;
using ParserLib.Gorzdrav;

namespace ParserLib;

public class ParserWorker<T>(
    SeleniumManager seleniumManager,
    IParser<T> parser, 
    IParserSettings parserSettings) where T : class
{
    public event Action<object, T>? OnNewData;
    public event Action<object, Exception>? OnError;

    public async Task StartAsync()
    {
        seleniumManager.OnNewPage += ParseAsync;
        await seleniumManager.NavigatePagesAsync(parserSettings.BaseUrl + parserSettings.Path);
    }

    private async Task ParseAsync(Stream sourceStream, CancellationToken cancellationToken)
    {
        try
        {            
            var domParser = new HtmlParser();
            var document = await domParser.ParseDocumentAsync(sourceStream, cancellationToken);
            
            var result = parser.Parse(document);
            
            OnNewData?.Invoke(this, result);
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, new Exception("Fatal error in parser worker", ex));
        }
    }
}