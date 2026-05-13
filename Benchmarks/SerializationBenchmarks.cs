using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System.Text.Json;
using System.Xml.Serialization;
using ProtoBuf;
using test4.Models;

namespace test4.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 30)]
public class SerializationBenchmarks
{
    private List<FerryAnnouncement> _data = new();

    private byte[] _jsonBytes = Array.Empty<byte>();
    private byte[] _xmlBytes = Array.Empty<byte>();
    private byte[] _protobufBytes = Array.Empty<byte>();

    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly XmlSerializer _xmlSerializer = new(typeof(List<FerryAnnouncement>));

    [GlobalSetup]
    public void Setup()
    {
        _jsonBytes     = File.ReadAllBytes("data/data4.json");
        _xmlBytes      = File.ReadAllBytes("data/data4.xml");
        _protobufBytes = File.ReadAllBytes("data/data4.pb");

        _data = JsonSerializer.Deserialize<List<FerryAnnouncement>>(_jsonBytes)!;

        int objectCount = _data.Count;

        List<FerryAnnouncement> jsonRt;
        using (var ms = new MemoryStream(_jsonBytes))
            jsonRt = JsonSerializer.Deserialize<List<FerryAnnouncement>>(ms, _jsonOptions)!;
        AssertEquivalent(_data, jsonRt, "JSON");

        List<FerryAnnouncement> xmlRt;
        using (var ms = new MemoryStream(_xmlBytes))
            xmlRt = (_xmlSerializer.Deserialize(ms) as List<FerryAnnouncement>)!;
        AssertEquivalent(_data, xmlRt, "XML");

        List<FerryAnnouncement> protoRt;
        using (var ms = new MemoryStream(_protobufBytes))
            protoRt = Serializer.Deserialize<List<FerryAnnouncement>>(ms);
        AssertEquivalent(_data, protoRt, "Protobuf");

        Console.WriteLine();
        Console.WriteLine("=== ROUND-TRIP VALIDATION ===");
        Console.WriteLine($"All three formats passed round-trip validation ({objectCount:N0} objects, all fields).");
        Console.WriteLine();
        Console.WriteLine($"Objects: {objectCount:N0}  |  JSON: {_jsonBytes.Length:N0} B  |  XML: {_xmlBytes.Length:N0} B  |  Protobuf: {_protobufBytes.Length:N0} B");

        var resultsDir = Environment.GetEnvironmentVariable("BENCH_RESULTS_DIR");
        if (resultsDir != null)
        {
            Directory.CreateDirectory(resultsDir);
            File.WriteAllText(
                Path.Combine(resultsDir, "payload-info.json"),
                JsonSerializer.Serialize(new Dictionary<string, int>
                {
                    ["JsonBytes"]     = _jsonBytes.Length,
                    ["XmlBytes"]      = _xmlBytes.Length,
                    ["ProtobufBytes"] = _protobufBytes.Length,
                    ["ObjectCount"]   = objectCount
                }));
        }
    }

    private static void AssertEquivalent(List<FerryAnnouncement> original, List<FerryAnnouncement> deserialized, string formatName)
    {
        if (original.Count != deserialized.Count)
            throw new InvalidOperationException($"{formatName} round-trip count mismatch: expected={original.Count}, actual={deserialized.Count}.");
        for (int i = 0; i < original.Count; i++)
            AssertEquivalent(original[i], deserialized[i], formatName, i);
    }

    private static void AssertEquivalent(FerryAnnouncement expected, FerryAnnouncement actual, string formatName, int index)
    {
        void Fail(string field, object? exp, object? act) =>
            throw new InvalidOperationException($"{formatName} round-trip mismatch at index {index}, field '{field}': expected='{exp}', actual='{act}'.");

        if (expected.Deleted != actual.Deleted) Fail(nameof(FerryAnnouncement.Deleted), expected.Deleted, actual.Deleted);
        if (expected.DepartureTime != actual.DepartureTime) Fail(nameof(FerryAnnouncement.DepartureTime), expected.DepartureTime, actual.DepartureTime);
        if (expected.DeviationId != actual.DeviationId) Fail(nameof(FerryAnnouncement.DeviationId), expected.DeviationId, actual.DeviationId);
        if (expected.Id != actual.Id) Fail(nameof(FerryAnnouncement.Id), expected.Id, actual.Id);
        AssertEquivalent(expected.FromHarbor, actual.FromHarbor, formatName, index, nameof(FerryAnnouncement.FromHarbor));
        AssertEquivalent(expected.ToHarbor, actual.ToHarbor, formatName, index, nameof(FerryAnnouncement.ToHarbor));
        AssertEquivalent(expected.Route, actual.Route, formatName, index, nameof(FerryAnnouncement.Route));
        if (expected.ModifiedTime != actual.ModifiedTime) Fail(nameof(FerryAnnouncement.ModifiedTime), expected.ModifiedTime, actual.ModifiedTime);
    }

    private static void AssertEquivalent(Harbor expected, Harbor actual, string formatName, int index, string parentField)
    {
        void Fail(string field, object? exp, object? act) =>
            throw new InvalidOperationException($"{formatName} round-trip mismatch at index {index}, field '{parentField}.{field}': expected='{exp}', actual='{act}'.");

        if (expected.Id != actual.Id) Fail(nameof(Harbor.Id), expected.Id, actual.Id);
        if (expected.Name != actual.Name) Fail(nameof(Harbor.Name), expected.Name, actual.Name);
    }

    private static void AssertEquivalent(Route expected, Route actual, string formatName, int index, string parentField)
    {
        void Fail(string field, object? exp, object? act) =>
            throw new InvalidOperationException($"{formatName} round-trip mismatch at index {index}, field '{parentField}.{field}': expected='{exp}', actual='{act}'.");

        if (expected.Id != actual.Id) Fail(nameof(Route.Id), expected.Id, actual.Id);
        if (expected.Name != actual.Name) Fail(nameof(Route.Name), expected.Name, actual.Name);
        if (expected.Shortname != actual.Shortname) Fail(nameof(Route.Shortname), expected.Shortname, actual.Shortname);
        AssertEquivalent(expected.Type, actual.Type, formatName, index, nameof(Route.Type));
    }

    private static void AssertEquivalent(RouteType expected, RouteType actual, string formatName, int index, string parentField)
    {
        void Fail(string field, object? exp, object? act) =>
            throw new InvalidOperationException($"{formatName} round-trip mismatch at index {index}, field '{parentField}.{field}': expected='{exp}', actual='{act}'.");

        if (expected.Id != actual.Id) Fail(nameof(RouteType.Id), expected.Id, actual.Id);
        if (expected.Name != actual.Name) Fail(nameof(RouteType.Name), expected.Name, actual.Name);
    }

    [Benchmark]
    public byte[] Json_Serialize()
    {
        using var ms = new MemoryStream();
        JsonSerializer.Serialize(ms, _data, _jsonOptions);
        return ms.ToArray();
    }

    [Benchmark]
    public List<FerryAnnouncement>? Json_Deserialize()
    {
        using var ms = new MemoryStream(_jsonBytes);
        return JsonSerializer.Deserialize<List<FerryAnnouncement>>(ms, _jsonOptions);
    }

    [Benchmark]
    public byte[] Xml_Serialize()
    {
        using var ms = new MemoryStream();
        _xmlSerializer.Serialize(ms, _data);
        return ms.ToArray();
    }

    [Benchmark]
    public List<FerryAnnouncement>? Xml_Deserialize()
    {
        using var ms = new MemoryStream(_xmlBytes);
        return _xmlSerializer.Deserialize(ms) as List<FerryAnnouncement>;
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
