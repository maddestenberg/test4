using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Xml.Serialization;
using ProtoBuf;
using test4.Models;

namespace test4.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(iterationCount: 30)]
public class SerializationBenchmarks
{
    private List<FerryAnnouncement> _data = new();

    private byte[] _jsonBytes = Array.Empty<byte>();
    private byte[] _xmlBytes = Array.Empty<byte>();
    private byte[] _protobufBytes = Array.Empty<byte>();

    private readonly JsonSerializerOptions _jsonOptions =
        new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

    [GlobalSetup]
    public void Setup()
    {
        string json = File.ReadAllText("Data/data4.json");

        var root = JsonSerializer.Deserialize<JsonRoot>(json, _jsonOptions);

        _data = root?.RESPONSE?.RESULT?
            .SelectMany(r => r.FerryAnnouncement)
            .Take(2000)
            .ToList()
            ?? new List<FerryAnnouncement>();

        Console.WriteLine($"Loaded objects: {_data.Count}");

        // JSON
        _jsonBytes = JsonSerializer.SerializeToUtf8Bytes(_data);

        // XML
        var xmlSerializer = new XmlSerializer(typeof(List<FerryAnnouncement>));

        using (var ms = new MemoryStream())
        {
            xmlSerializer.Serialize(ms, _data);
            _xmlBytes = ms.ToArray();
        }

        // Protobuf
        using (var ms = new MemoryStream())
        {
            Serializer.Serialize(ms, _data);
            _protobufBytes = ms.ToArray();
        }

        Console.WriteLine("=== PAYLOAD SIZE ===");
        Console.WriteLine($"JSON: {_jsonBytes.Length:N0} bytes");
        Console.WriteLine($"XML: {_xmlBytes.Length:N0} bytes");
        Console.WriteLine($"PROTOBUF: {_protobufBytes.Length:N0} bytes");
    }

    [Benchmark]
    public byte[] Json_Serialize()
    {
        return JsonSerializer.SerializeToUtf8Bytes(_data);
    }

    [Benchmark]
    public List<FerryAnnouncement>? Json_Deserialize()
    {
        return JsonSerializer.Deserialize<List<FerryAnnouncement>>(_jsonBytes);
    }

    [Benchmark]
    public byte[] Xml_Serialize()
    {
        var serializer = new XmlSerializer(typeof(List<FerryAnnouncement>));

        using var ms = new MemoryStream();

        serializer.Serialize(ms, _data);

        return ms.ToArray();
    }

    [Benchmark]
    public List<FerryAnnouncement>? Xml_Deserialize()
    {
        var serializer = new XmlSerializer(typeof(List<FerryAnnouncement>));

        using var ms = new MemoryStream(_xmlBytes);

        return serializer.Deserialize(ms) as List<FerryAnnouncement>;
    }

    [Benchmark]
    public byte[] Protobuf_Serialize()
    {
        using var ms = new MemoryStream();

        Serializer.Serialize(ms, _data);

        return ms.ToArray();
    }

    [Benchmark]
    public List<FerryAnnouncement> Protobuf_Deserialize()
    {
        using var ms = new MemoryStream(_protobufBytes);

        return Serializer.Deserialize<List<FerryAnnouncement>>(ms);
    }
}