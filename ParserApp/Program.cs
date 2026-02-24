using ParserLib.Gorzdrav;
using ParserLib;
using ParserLib.Gorzdrav.Models;
using System.Text;

namespace ParserApp;

internal class Program
{
    static void Main(string[] args)
    {
        var settings = new GorzdravSettings();
        var pw = new ParserWorker<IEnumerable<Drug>>(
            seleniumManager: new SeleniumManager(),
            parser: new GorzdravParser(settings),
            parserSettings: settings
        );

        pw.OnError += (object o, Exception e) =>
        {
            Console.WriteLine(e);
        };

        pw.OnNewData += async (object o, IEnumerable<Drug> drugs) =>
        {
            Console.WriteLine($"Получено {drugs.Count()} карточек");
            await WriteDrugsToCsvAsync(drugs);
        };

        Task.Run(async () => await pw.StartAsync());
    }

    private static readonly object _csvLock = new object();
    private static bool _headerWritten = false;
    private const string CsvFilePath = "drugs.csv";

    private static async Task WriteDrugsToCsvAsync(IEnumerable<Drug> drugs)
    {
        lock (_csvLock)
        {
            using (var writer = new StreamWriter(CsvFilePath, append: true, Encoding.UTF8))
            {
                if (!_headerWritten && new FileInfo(CsvFilePath).Length == 0)
                {
                    writer.WriteLine("PrescriptionStatus,ImageUrl,Name,Manufacturer,ActiveSubstance,Price,OldPrice,DrugUrl");
                    _headerWritten = true;
                }

                foreach (var drug in drugs)
                {
                    var line = string.Join(",", 
                        EscapeCsvField(drug.PrescriptionStatus),
                        EscapeCsvField(drug.ImageUrl),
                        EscapeCsvField(drug.Name),
                        EscapeCsvField(drug.Manufacturer),
                        EscapeCsvField(drug.ActiveSubstance),
                        drug.Price.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        drug.OldPrice?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "",
                        EscapeCsvField(drug.DrugUrl)
                    );
                    writer.WriteLine(line);
                }
            }
        }
        await Task.CompletedTask; // lock синхронный, но метод async для совместимости
    }

    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field)) return "";
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }
}
