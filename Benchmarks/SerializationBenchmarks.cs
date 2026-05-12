using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System.Diagnostics;
using System.Globalization;
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

        _jsonBytes = JsonSerializer.SerializeToUtf8Bytes(_data);

        var xmlSerializer = new XmlSerializer(typeof(List<FerryAnnouncement>));
        using (var ms = new MemoryStream())
        {
            xmlSerializer.Serialize(ms, _data);
            _xmlBytes = ms.ToArray();
        }

        using (var ms = new MemoryStream())
        {
            Serializer.Serialize(ms, _data);
            _protobufBytes = ms.ToArray();
        }

        int objectCount = _data.Count;

        Console.WriteLine();
        Console.WriteLine("=== OBJECT COUNT ===");
        Console.WriteLine($"Loaded objects: {objectCount:N0}");

        Console.WriteLine();
        Console.WriteLine("=== PAYLOAD SIZE ===");
        Console.WriteLine($"JSON: {_jsonBytes.Length:N0} bytes");
        Console.WriteLine($"XML: {_xmlBytes.Length:N0} bytes");
        Console.WriteLine($"PROTOBUF: {_protobufBytes.Length:N0} bytes");

        var csvLines = new List<string>
        {
            "Method,Iteration,ElapsedTimeMs,CpuTimeMs,PayloadBytes,ObjectCount"
        };

        Console.WriteLine();
        Console.WriteLine("=== RAW 30 ITERATIONS ===");

        MeasureRaw30("Json_Serialize", Json_Serialize, _jsonBytes.Length, objectCount, csvLines);
        MeasureRaw30("Xml_Serialize", Xml_Serialize, _xmlBytes.Length, objectCount, csvLines);
        MeasureRaw30("Protobuf_Serialize", Protobuf_Serialize, _protobufBytes.Length, objectCount, csvLines);

        MeasureRaw30("Json_Deserialize", Json_Deserialize, _jsonBytes.Length, objectCount, csvLines);
        MeasureRaw30("Xml_Deserialize", Xml_Deserialize, _xmlBytes.Length, objectCount, csvLines);
        MeasureRaw30("Protobuf_Deserialize", Protobuf_Deserialize, _protobufBytes.Length, objectCount, csvLines);

        var resultPath = "/Users/mads/Desktop/SYSTEMVET/T6 SYSTEMVET/examensarbete/test4/Results/raw-iterations.csv";

        Directory.CreateDirectory(Path.GetDirectoryName(resultPath)!);
        File.WriteAllLines(resultPath, csvLines);

        Console.WriteLine($"Saved CSV to: {resultPath}");
    }

    private static void MeasureRaw30(
        string methodName,
        Func<object?> action,
        int payloadBytes,
        int objectCount,
        List<string> csvLines)
    {
        Console.WriteLine();
        Console.WriteLine($"--- {methodName} ---");

        for (int i = 1; i <= 30; i++)
        {
            var process = Process.GetCurrentProcess();

            var cpuBefore = process.TotalProcessorTime;
            var stopwatch = Stopwatch.StartNew();

            var result = action();

            stopwatch.Stop();
            var cpuAfter = process.TotalProcessorTime;

            GC.KeepAlive(result);

            double elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            double cpuMs = (cpuAfter - cpuBefore).TotalMilliseconds;

            Console.WriteLine(
                $"Iteration {i}: elapsed={elapsedMs.ToString("F6", CultureInfo.InvariantCulture)} ms, cpu={cpuMs.ToString("F6", CultureInfo.InvariantCulture)} ms"
            );

            csvLines.Add(
                $"{methodName},{i},{elapsedMs.ToString("F6", CultureInfo.InvariantCulture)},{cpuMs.ToString("F6", CultureInfo.InvariantCulture)},{payloadBytes},{objectCount}"
            );
        }
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