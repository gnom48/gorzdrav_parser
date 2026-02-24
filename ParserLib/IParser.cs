using AngleSharp.Html.Dom;

namespace ParserLib;

public interface IParser<T> where T : class
{
    T Parse(IHtmlDocument document);
}
