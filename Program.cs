using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using System.Globalization;
using test4.Benchmarks;

var outputPath = args.Length > 0 ? args[0] : Path.Combine("BenchmarkDotNet.Artifacts", "results", "serialization-raw-iterations.csv");
var summary = BenchmarkRunner.Run<SerializationBenchmarks>();
WriteSimplifiedBenchmarkCsv(summary, outputPath);

static void WriteSimplifiedBenchmarkCsv(Summary summary, string outputPath)
{
    var resultsDirectory = Path.Combine("BenchmarkDotNet.Artifacts", "results");
    if (!Directory.Exists(resultsDirectory))
    {
        Console.WriteLine($"Results directory not found: {resultsDirectory}");
        return;
    }

    // Read benchmark metadata (object count and payload sizes).
    var metadataFile = Path.Combine(resultsDirectory, ".benchmark-metadata.txt");
    if (!File.Exists(metadataFile))
    {
        Console.WriteLine("Benchmark metadata file not found.");
        return;
    }

    var metadataLines = File.ReadAllLines(metadataFile);
    if (metadataLines.Length < 4 || !int.TryParse(metadataLines[0], out var objectCount) ||
        !int.TryParse(metadataLines[1], out var jsonPayloadBytes) ||
        !int.TryParse(metadataLines[2], out var xmlPayloadBytes) ||
        !int.TryParse(metadataLines[3], out var protobufPayloadBytes))
    {
        Console.WriteLine("Benchmark metadata file is invalid.");
        return;
    }

    var sourceCsv = Directory.GetFiles(resultsDirectory, "*SerializationBenchmarks*-measurements.csv").FirstOrDefault();
    if (sourceCsv is null)
    {
        Console.WriteLine("Could not find BenchmarkDotNet measurements CSV for SerializationBenchmarks.");
        return;
    }

    Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
    using var reader = new StreamReader(sourceCsv, System.Text.Encoding.UTF8);
    using var writer = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8);

    var headerLine = reader.ReadLine();
    if (headerLine is null)
    {
        Console.WriteLine("Measurements CSV is empty.");
        return;
    }

    var headers = headerLine.Split(';');
    int methodIndex = Array.IndexOf(headers, "Target_Method");
    int iterationIndex = Array.IndexOf(headers, "Measurement_IterationIndex");
    int valueIndex = Array.IndexOf(headers, "Measurement_Value");
    int modeIndex = Array.IndexOf(headers, "Measurement_IterationMode");
    int stageIndex = Array.IndexOf(headers, "Measurement_IterationStage");

    if (methodIndex < 0 || iterationIndex < 0 || valueIndex < 0 || modeIndex < 0 || stageIndex < 0)
    {
        Console.WriteLine("Measurements CSV header did not contain expected BenchmarkDotNet columns.");
        return;
    }

    writer.WriteLine("Method;Operation;Iteration;ElapsedTimeMs;PayloadBytes;ObjectCount");

    while (!reader.EndOfStream)
    {
        var line = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(line))
        {
            continue;
        }

        var cells = line.Split(';');
        if (cells.Length != headers.Length)
        {
            continue;
        }

        if (cells[modeIndex] != "Workload" || cells[stageIndex] != "Actual")
        {
            continue;
        }

        var method = cells[methodIndex];
        var iterationValue = cells[iterationIndex];
        if (!double.TryParse(cells[valueIndex], NumberStyles.Any, CultureInfo.InvariantCulture, out var elapsedNanoseconds))
        {
            continue;
        }

        var elapsedMilliseconds = elapsedNanoseconds / 1_000_000d;
        var operation = method.EndsWith("_Serialize", StringComparison.Ordinal) ? "Serialize" : "Deserialize";
        var payloadBytes = method switch
        {
            "Json_Serialize" or "Json_Deserialize" => jsonPayloadBytes,
            "Xml_Serialize" or "Xml_Deserialize" => xmlPayloadBytes,
            "Protobuf_Serialize" or "Protobuf_Deserialize" => protobufPayloadBytes,
            _ => 0
        };

        writer.WriteLine($"{method};{operation};{iterationValue};{elapsedMilliseconds:F6};{payloadBytes};{objectCount}");
    }

    Console.WriteLine($"Wrote simplified benchmark CSV: {outputPath}");
}