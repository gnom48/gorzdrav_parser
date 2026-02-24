using System.Text.Json.Serialization;

namespace ParserLib.Gorzdrav.Models;

public class AdditionalProperties
{
    [JsonPropertyName("bundle")]
    public bool Bundle { get; set; }

    [JsonPropertyName("prescription")]
    public bool Prescription { get; set; }

    [JsonPropertyName("deliveryAvailable")]
    public bool DeliveryAvailable { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("manufacturer")]
    public string? Manufacturer { get; set; }
}

public class Attribute
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

public class Links
{
    [JsonPropertyName("S")]
    public string? S { get; set; }

    [JsonPropertyName("L_RETINA")]
    public string? LRETINA { get; set; }

    [JsonPropertyName("S_RETINA")]
    public string? SRETINA { get; set; }

    [JsonPropertyName("M_RETINA")]
    public string? MRETINA { get; set; }

    [JsonPropertyName("XS_RETINA")]
    public string? XSRETINA { get; set; }

    [JsonPropertyName("ORIGINAL")]
    public string? ORIGINAL { get; set; }

    [JsonPropertyName("XS")]
    public string? XS { get; set; }

    [JsonPropertyName("L")]
    public string? L { get; set; }

    [JsonPropertyName("M")]
    public string? M { get; set; }
}

public class MainImage
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("links")]
    public Links? Links { get; set; }
}

public class Price
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("price")]
    public double PriceD { get; set; }
}

public class Root
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("extId")]
    public string? ExtId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("aiSummary")]
    public string? AiSummary { get; set; }

    [JsonPropertyName("popularity")]
    public double Popularity { get; set; }

    [JsonPropertyName("prices")]
    public List<Price>? Prices { get; set; }

    [JsonPropertyName("stocks")]
    public List<string>? Stocks { get; set; }

    [JsonPropertyName("additionalProperties")]
    public AdditionalProperties? AdditionalProperties { get; set; }

    [JsonPropertyName("mainImage")]
    public MainImage? MainImage { get; set; }

    [JsonPropertyName("bonuses")]
    public int Bonuses { get; set; }

    [JsonPropertyName("labels")]
    public List<object>? Labels { get; set; }

    [JsonPropertyName("attributes")]
    public List<Attribute>? Attributes { get; set; }

    [JsonPropertyName("unionProduct")]
    public UnionProduct? UnionProduct { get; set; }

    [JsonPropertyName("pickupStoresCount")]
    public int PickupStoresCount { get; set; }

    [JsonPropertyName("shipToStoresCount")]
    public int ShipToStoresCount { get; set; }

    [JsonPropertyName("inPickupStore")]
    public bool InPickupStore { get; set; }

    [JsonPropertyName("shipToStore")]
    public bool ShipToStore { get; set; }
}

public class UnionProduct
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

