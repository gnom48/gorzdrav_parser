using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using ParserLib.Gorzdrav.Models;

namespace ParserLib.Gorzdrav;

public class GorzdravParser(IParserSettings parserSettings) : IParser<IEnumerable<Drug>>
{
    public IEnumerable<Drug> Parse(IHtmlDocument document)
    {
        var drugs = new List<Drug>();

        // Находим все карточки товаров
        var cardNodes = document.QuerySelectorAll("div.product-card.product-card--grid.product-card--theme--gz");
        foreach (var card in cardNodes)
        {
            var drug = new Drug();

            // Рецептурность
            var prescriptionChip = card.QuerySelector(".product-card-body__chip-line .ui-chip");
            if (prescriptionChip != null)
            {
                var prescriptionText = prescriptionChip.TextContent.Trim();
                if (!string.IsNullOrEmpty(prescriptionText) && 
                    (prescriptionText.Contains("рецепт", StringComparison.OrdinalIgnoreCase) ||
                     prescriptionText.Contains("Рецепт", StringComparison.OrdinalIgnoreCase)))
                {
                    drug.PrescriptionStatus = prescriptionText;
                }
            }

            // Изображение
            var img = card.QuerySelector(".product-card-image__container img");
            if (img != null)
            {
                var src = img.GetAttribute("src");
                if (!string.IsNullOrEmpty(src))
                {
                    // Если относительный URL, добавляем базовый
                    if (src.StartsWith("/"))
                        drug.ImageUrl = parserSettings.BaseUrl + parserSettings.Prefix + src;
                    else
                        drug.ImageUrl = src;
                }
            }

            // Название и ссылка
            var titleLink = card.QuerySelector("a.product-card-body__title");
            if (titleLink != null)
            {
                drug.Name = titleLink.TextContent.Trim();
                var href = titleLink.GetAttribute("href");
                if (!string.IsNullOrEmpty(href))
                {
                    drug.DrugUrl = href.StartsWith("/") ? parserSettings.BaseUrl + parserSettings.Prefix + href : href;
                }
            }

            // Производитель и действующее вещество (ищем в списке)
            var items = card.QuerySelectorAll(".product-card__list .product-card__item");
            foreach (var item in items)
            {
                var label = item.QuerySelector(".product-card__label")?.TextContent.Trim() ?? "";
                var valueLink = item.QuerySelector(".product-card__value a");
                var valueText = valueLink?.TextContent.Trim() ?? item.QuerySelector(".product-card__value span")?.TextContent.Trim();

                if (label.Contains("Производитель", StringComparison.OrdinalIgnoreCase))
                {
                    drug.Manufacturer = valueText ?? string.Empty;
                }
                else if (label.Contains("Действующее вещество", StringComparison.OrdinalIgnoreCase))
                {
                    drug.ActiveSubstance = valueText ?? string.Empty;
                }
            }

            // Цены
            var priceElement = card.QuerySelector(".ui-price__price");
            if (priceElement != null)
            {
                drug.Price = ParsePrice(priceElement.TextContent);
            }

            var oldPriceElement = card.QuerySelector(".ui-price__discount-value");
            if (oldPriceElement != null)
            {
                drug.OldPrice = ParsePrice(oldPriceElement.TextContent);
            }

            // Добавляем только если есть название
            if (!string.IsNullOrEmpty(drug.Name))
            {
                drugs.Add(drug);
            }
        }

        return drugs;
    }

    private decimal ParsePrice(string priceText)
    {
        if (string.IsNullOrWhiteSpace(priceText))
            return 0;

        // Удаляем все кроме цифр, запятой и точки
        var cleaned = Regex.Replace(priceText, @"[^\d,\.]", "");
        // Заменяем запятую на точку для парсинга
        cleaned = cleaned.Replace(',', '.');

        if (decimal.TryParse(cleaned, System.Globalization.NumberStyles.Any, 
            System.Globalization.CultureInfo.InvariantCulture, out decimal result))
        {
            return result;
        }

        return 0;
    }
}