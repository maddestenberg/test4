using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Exporters;
using System.Text.Json;
using System.Xml.Serialization;
using ProtoBuf;
using test4.Models;

#nullable enable

namespace test4.Benchmarks;

[MemoryDiagnoser]
[CsvMeasurementsExporter]
[SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 30)]
public class SerializationBenchmarks
{
    private List<FerryAnnouncement> _data = new();

    private byte[] _jsonBytes = Array.Empty<byte>();
    private byte[] _xmlBytes = Array.Empty<byte>();
    private byte[] _protobufBytes = Array.Empty<byte>();

    // Static properties for CSV export to reflect actual benchmark data.
    public static int JsonPayloadBytes { get; private set; }
    public static int XmlPayloadBytes { get; private set; }
    public static int ProtobufPayloadBytes { get; private set; }
    public static int ObjectCount { get; private set; }

    // Reused serializers to ensure fair comparisons:
    // - XmlSerializer is created once during setup, not per-invocation.
    // - JsonSerializerOptions is reused for all JSON operations.
    // This ensures JSON and Protobuf don't incur serializer initialization costs that XML would if created per-invocation.
    private XmlSerializer _xmlSerializer = null!;

    private readonly JsonSerializerOptions _jsonOptions =
        new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

    [GlobalSetup]
    public void Setup()
    {
        // GlobalSetup should only initialize shared test data and state, not perform benchmark execution.
        // Performing timing measurements here contaminates BenchmarkDotNet's measurement environment,
        // introduces unequal cold-start/JIT effects across formats, and creates systematic bias.
        // BenchmarkDotNet already handles warm-up, tiered JIT stabilization, iteration calibration,
        // statistical analysis, outlier filtering, and repeated internal invocations.
        // Custom stopwatch loops are significantly noisier and invalidate benchmark consistency.
        //
        // CPU time measurements (Process.TotalProcessorTime) have been removed because they measure
        // entire .NET process CPU usage (GC, ThreadPool, finalizer threads, runtime overhead, etc.),
        // not the serialization operation itself. Only wall-clock elapsed time is measured via BenchmarkDotNet.

        string json = File.ReadAllText("Data/data4.json");

        var root = JsonSerializer.Deserialize<JsonRoot>(json, _jsonOptions);

        _data = root?.RESPONSE?.RESULT?
            .SelectMany(r => r.FerryAnnouncement)
            .ToList()
            ?? new List<FerryAnnouncement>();

        using (var ms = new MemoryStream())
        {
            JsonSerializer.Serialize(ms, _data, _jsonOptions);
            _jsonBytes = ms.ToArray();
        }

        // Initialize XmlSerializer once to avoid per-invocation allocation costs during benchmarking.
        // This ensures a fair comparison where all formats reuse their serializer/options.
        _xmlSerializer = new XmlSerializer(typeof(List<FerryAnnouncement>));
        using (var ms = new MemoryStream())
        {
            _xmlSerializer.Serialize(ms, _data);
            _xmlBytes = ms.ToArray();
        }

        using (var ms = new MemoryStream())
        {
            Serializer.Serialize(ms, _data);
            _protobufBytes = ms.ToArray();
        }

        // Set static properties for CSV export.
        ObjectCount = _data.Count;
        JsonPayloadBytes = _jsonBytes.Length;
        XmlPayloadBytes = _xmlBytes.Length;
        ProtobufPayloadBytes = _protobufBytes.Length;

        // Write metadata to a file for CSV export.
        var metadataFile = Path.Combine("BenchmarkDotNet.Artifacts", "results", ".benchmark-metadata.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(metadataFile) ?? ".");
        File.WriteAllText(metadataFile, $"{ObjectCount}\n{JsonPayloadBytes}\n{XmlPayloadBytes}\n{ProtobufPayloadBytes}");

        Console.WriteLine();
        Console.WriteLine("=== OBJECT COUNT ===");
        Console.WriteLine($"Loaded objects: {ObjectCount:N0}");

        Console.WriteLine();
        Console.WriteLine("=== PAYLOAD SIZE ===");
        Console.WriteLine($"JSON: {JsonPayloadBytes:N0} bytes");
        Console.WriteLine($"XML: {XmlPayloadBytes:N0} bytes");
        Console.WriteLine($"PROTOBUF: {ProtobufPayloadBytes:N0} bytes");

        ValidateRoundTrips();
    }

    private void ValidateRoundTrips()
    {
        ValidateJsonRoundTrip();
        ValidateXmlRoundTrip();
        ValidateProtobufRoundTrip();

        Console.WriteLine();
        Console.WriteLine("Round-trip validation passed for JSON, XML, and Protobuf.");
    }

    private void ValidateJsonRoundTrip()
    {
        using var ms = new MemoryStream();
        JsonSerializer.Serialize(ms, _data, _jsonOptions);
        ms.Position = 0;
        var deserialized = JsonSerializer.Deserialize<List<FerryAnnouncement>>(ms, _jsonOptions);
        AssertEquivalent(_data, deserialized, "JSON");
    }

    private void ValidateXmlRoundTrip()
    {
        using var ms = new MemoryStream();
        _xmlSerializer.Serialize(ms, _data);
        ms.Position = 0;
        var deserialized = _xmlSerializer.Deserialize(ms) as List<FerryAnnouncement>;
        AssertEquivalent(_data, deserialized, "XML");
    }

    private void ValidateProtobufRoundTrip()
    {
        using var ms = new MemoryStream();
        Serializer.Serialize(ms, _data);
        ms.Position = 0;
        var deserialized = Serializer.Deserialize<List<FerryAnnouncement>>(ms);
        AssertEquivalent(_data, deserialized, "Protobuf");
    }

    private static void AssertEquivalent(List<FerryAnnouncement> original, List<FerryAnnouncement>? deserialized, string formatName)
    {
        if (deserialized is null)
            throw new InvalidOperationException($"{formatName} round-trip deserialized result is null.");

        if (original.Count != deserialized.Count)
            throw new InvalidOperationException($"{formatName} round-trip count mismatch: original={original.Count}, deserialized={deserialized.Count}.");

        for (int i = 0; i < original.Count; i++)
        {
            AssertEquivalent(original[i], deserialized[i], formatName, i);
        }
    }

    private static void AssertEquivalent(FerryAnnouncement expected, FerryAnnouncement actual, string formatName, int index)
    {
        void Fail(string fieldName, object? expectedValue, object? actualValue)
        {
            throw new InvalidOperationException($"{formatName} round-trip mismatch at index {index}, field '{fieldName}': expected='{expectedValue}', actual='{actualValue}'.");
        }

        if (expected.Deleted != actual.Deleted)
            Fail(nameof(FerryAnnouncement.Deleted), expected.Deleted, actual.Deleted);
        if (expected.DepartureTime != actual.DepartureTime)
            Fail(nameof(FerryAnnouncement.DepartureTime), expected.DepartureTime, actual.DepartureTime);
        if (expected.DeviationId != actual.DeviationId)
            Fail(nameof(FerryAnnouncement.DeviationId), expected.DeviationId, actual.DeviationId);
        if (expected.Id != actual.Id)
            Fail(nameof(FerryAnnouncement.Id), expected.Id, actual.Id);
        AssertEquivalent(expected.FromHarbor, actual.FromHarbor, formatName, index, nameof(FerryAnnouncement.FromHarbor));
        AssertEquivalent(expected.ToHarbor, actual.ToHarbor, formatName, index, nameof(FerryAnnouncement.ToHarbor));
        AssertEquivalent(expected.Route, actual.Route, formatName, index, nameof(FerryAnnouncement.Route));
        if (expected.ModifiedTime != actual.ModifiedTime)
            Fail(nameof(FerryAnnouncement.ModifiedTime), expected.ModifiedTime, actual.ModifiedTime);
    }

    private static void AssertEquivalent(Harbor expected, Harbor actual, string formatName, int index, string parentField)
    {
        void Fail(string fieldName, object? expectedValue, object? actualValue)
        {
            throw new InvalidOperationException($"{formatName} round-trip mismatch at index {index}, field '{parentField}.{fieldName}': expected='{expectedValue}', actual='{actualValue}'.");
        }

        if (expected.Id != actual.Id)
            Fail(nameof(Harbor.Id), expected.Id, actual.Id);
        if (expected.Name != actual.Name)
            Fail(nameof(Harbor.Name), expected.Name, actual.Name);
    }

    private static void AssertEquivalent(Route expected, Route actual, string formatName, int index, string parentField)
    {
        void Fail(string fieldName, object? expectedValue, object? actualValue)
        {
            throw new InvalidOperationException($"{formatName} round-trip mismatch at index {index}, field '{parentField}.{fieldName}': expected='{expectedValue}', actual='{actualValue}'.");
        }

        if (expected.Id != actual.Id)
            Fail(nameof(Route.Id), expected.Id, actual.Id);
        if (expected.Name != actual.Name)
            Fail(nameof(Route.Name), expected.Name, actual.Name);
        if (expected.Shortname != actual.Shortname)
            Fail(nameof(Route.Shortname), expected.Shortname, actual.Shortname);
        AssertEquivalent(expected.Type, actual.Type, formatName, index, nameof(Route.Type));
    }

    private static void AssertEquivalent(RouteType expected, RouteType actual, string formatName, int index, string parentField)
    {
        void Fail(string fieldName, object? expectedValue, object? actualValue)
        {
            throw new InvalidOperationException($"{formatName} round-trip mismatch at index {index}, field '{parentField}.{fieldName}': expected='{expectedValue}', actual='{actualValue}'.");
        }

        if (expected.Id != actual.Id)
            Fail(nameof(RouteType.Id), expected.Id, actual.Id);
        if (expected.Name != actual.Name)
            Fail(nameof(RouteType.Name), expected.Name, actual.Name);
    }

    [Benchmark]
    public byte[] Json_Serialize()
    {
        // Measures elapsed time only. No CPU time collection.
        // Use a byte-oriented serialization path equivalent to XML and Protobuf.
        using var ms = new MemoryStream();
        JsonSerializer.Serialize(ms, _data, _jsonOptions);
        return ms.ToArray();
    }

    [Benchmark]
    public List<FerryAnnouncement>? Json_Deserialize()
    {
        // Measures elapsed time only. No CPU time collection.
        // Use a stream-based input path to be more equivalent to XML and Protobuf.
        using var ms = new MemoryStream(_jsonBytes);
        return JsonSerializer.Deserialize<List<FerryAnnouncement>>(ms, _jsonOptions);
    }

    [Benchmark]
    public byte[] Xml_Serialize()
    {
        // Measures elapsed time only. No CPU time collection.
        // Use reused _xmlSerializer instance (initialized in GlobalSetup) to ensure fair comparison
        // with JSON and Protobuf which reuse their serializer/options.
        using var ms = new MemoryStream();

        _xmlSerializer.Serialize(ms, _data);

        return ms.ToArray();
    }

    [Benchmark]
    public List<FerryAnnouncement>? Xml_Deserialize()
    {
        // Measures elapsed time only. No CPU time collection.
        // Use reused _xmlSerializer instance (initialized in GlobalSetup) to ensure fair comparison
        // with JSON and Protobuf which reuse their serializer/options.
        using var ms = new MemoryStream(_xmlBytes);

        return _xmlSerializer.Deserialize(ms) as List<FerryAnnouncement>;
    }

    [Benchmark]
    public byte[] Protobuf_Serialize()
    {
        // Measures elapsed time only. No CPU time collection.
        using var ms = new MemoryStream();

        Serializer.Serialize(ms, _data);

        return ms.ToArray();
    }

    [Benchmark]
    public List<FerryAnnouncement> Protobuf_Deserialize()
    {
        // Measures elapsed time only. No CPU time collection.
        using var ms = new MemoryStream(_protobufBytes);

        return Serializer.Deserialize<List<FerryAnnouncement>>(ms);
    }
}