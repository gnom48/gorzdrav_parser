namespace ParserLib.Gorzdrav;

public class GorzdravSettings : IParserSettings
{
    public string BaseUrl { get; set; } = "https://gorzdrav.org/";

    public string Path { get; set; } = "category/sredstva-ot-diabeta";
}