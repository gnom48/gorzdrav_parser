using System.Net;

namespace ParserLib;

public static class ProxyRotator
{
    private static readonly string[] Ips = 
    {
        "89.109.23.80:3128",
        "185.184.233.152:1080"
    };

    public static WebProxy GetRandom() => new WebProxy
        {
            Address = new Uri(Ips[Random.Shared.Next(Ips.Length)]),
            BypassProxyOnLocal = false,
            UseDefaultCredentials = false
        };
}

