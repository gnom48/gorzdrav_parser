namespace ParserLib.Gorzdrav;

public class GorzdravSettings : IParserSettings
{
    // public GorzdravSettings(int startPage, int endPage)
    // {
    //     StartPoint = startPage;
    //     EndPoint = endPage;
    // }

    public string BaseUrl { get; set; } = "https://gorzdrav.org/";

    public string Prefix { get; set; } = "category/sredstva-ot-diabeta";

    public int StartPoint { get; set; }

    public int EndPoint { get; set; }
}