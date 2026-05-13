using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using ProtoBuf;
using System.Text.Json;
using System.Xml.Serialization;
using test4.Benchmarks;
using test4.Models;

if (args.Contains("generate"))
{
    var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    var xmlSerializer = new XmlSerializer(typeof(List<FerryAnnouncement>));

    string sourceJson = File.ReadAllText("data/data4-source.json");
    var root = JsonSerializer.Deserialize<JsonRoot>(sourceJson, jsonOptions);
    var data = root?.RESPONSE?.RESULT?
        .SelectMany(r => r.FerryAnnouncement)
        .ToList() ?? [];

    var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(data);
    File.WriteAllBytes("data/data4.json", jsonBytes);
    Console.WriteLine($"Sparade data/data4.json       ({jsonBytes.Length,10:N0} bytes)");

    using (var ms = new MemoryStream())
    {
        xmlSerializer.Serialize(ms, data);
        File.WriteAllBytes("data/data4.xml", ms.ToArray());
        Console.WriteLine($"Sparade data/data4.xml        ({ms.Length,10:N0} bytes)");
    }

    using (var ms = new MemoryStream())
    {
        Serializer.Serialize(ms, data);
        File.WriteAllBytes("data/data4.pb", ms.ToArray());
        Console.WriteLine($"Sparade data/data4.pb         ({ms.Length,10:N0} bytes)");
    }

    Console.WriteLine($"{data.Count:N0} objekt totalt.");
    return;
}

var resultsDir = Path.GetFullPath("Results");
var csvPath = Path.Combine(resultsDir, "raw-iterations.csv");
Environment.SetEnvironmentVariable("BENCH_RESULTS_DIR", resultsDir);
var config = DefaultConfig.Instance.AddExporter(new RawCsvExporter(csvPath));
BenchmarkRunner.Run<SerializationBenchmarks>(config);
