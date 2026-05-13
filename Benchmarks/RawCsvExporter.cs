using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace test4.Benchmarks
{
    public class RawCsvExporter : IExporter
    {
        private readonly string outputPath;

        public RawCsvExporter(string outputPath) => this.outputPath = outputPath;

        public string Name => nameof(RawCsvExporter);

        public void ExportToLog(Summary summary, ILogger logger) { }

        public IEnumerable<string> ExportToFiles(Summary summary, ILogger consoleLogger)
        {
            var payloadInfoPath = Path.Combine(Path.GetDirectoryName(outputPath)!, "payload-info.json");
            var jsonBytes = 0;
            var xmlBytes = 0;
            var protobufBytes = 0;
            var objectCount = 0;

            if (File.Exists(payloadInfoPath))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(payloadInfoPath));
                jsonBytes     = doc.RootElement.GetProperty("JsonBytes").GetInt32();
                xmlBytes      = doc.RootElement.GetProperty("XmlBytes").GetInt32();
                protobufBytes = doc.RootElement.GetProperty("ProtobufBytes").GetInt32();
                objectCount   = doc.RootElement.GetProperty("ObjectCount").GetInt32();
            }

            int GetPayload(string method) => method switch
            {
                "Json_Serialize"       or "Json_Deserialize"     => jsonBytes,
                "Xml_Serialize"        or "Xml_Deserialize"      => xmlBytes,
                "Protobuf_Serialize"   or "Protobuf_Deserialize" => protobufBytes,
                _ => 0
            };

            string os = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows"
                      : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)     ? "macOS"
                      : "Linux";
            string runtime = $"net{Environment.Version.Major}.0";
            string arch = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();

            var lines = new List<string> { "OS,Runtime,Arch,Method,Iteration,ElapsedMs,PayloadBytes,ObjectCount" };

            foreach (var report in summary.Reports)
            {
                if (!report.Success) continue;

                var methodName = report.BenchmarkCase.Descriptor.WorkloadMethod.Name;
                var measurements = report.AllMeasurements
                    .Where(m => m.Is(IterationMode.Workload, IterationStage.Actual))
                    .ToList();

                for (int i = 0; i < measurements.Count; i++)
                {
                    var m = measurements[i];
                    double elapsedMs = m.Nanoseconds / (m.Operations * 1_000_000.0);
                    lines.Add(string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "{0},{1},{2},{3},{4},{5:F6},{6},{7}",
                        os, runtime, arch, methodName, i + 1, elapsedMs, GetPayload(methodName), objectCount));
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            File.WriteAllLines(outputPath, lines);
            consoleLogger.WriteLine($"Raw iterations saved to: {outputPath}");
            return new[] { outputPath };
        }
    }
}
