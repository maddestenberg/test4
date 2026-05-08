using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ProtoBuf;
using System.Text.Json;
using System.Xml.Serialization;
using test4.Models;
using System.Linq;

namespace test4
{
    [MemoryDiagnoser]
    public class SerializationBenchmarks
    {
        private Root data;

        private string jsonString;
        private string xmlString;
        private byte[] protobufBytes;

        [GlobalSetup]
        public void Setup()
        {
            // =========================
            // LOAD JSON
            // =========================

            var json = File.ReadAllText("data/data4.json");

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var jsonRoot = JsonSerializer.Deserialize<JsonRoot>(json, options);

            data = new Root
            {
                RESULT = jsonRoot.RESPONSE.RESULT.Select(r => new Result
                {
                    FerryAnnouncement = r.FerryAnnouncement
                }).ToList()
            };

            // =========================
            // PREPARE FORMATS
            // =========================

            jsonString = JsonSerializer.Serialize(data);

            var xmlSerializer = new XmlSerializer(typeof(Root));

            using (var sw = new StringWriter())
            {
                xmlSerializer.Serialize(sw, data);
                xmlString = sw.ToString();
            }

            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, data);
                protobufBytes = ms.ToArray();
            }

            // =========================
            // INFO OUTPUT
            // =========================

            Console.WriteLine("=== OBJECT COUNT ===");

            int count = data.RESULT.Sum(r => r.FerryAnnouncement?.Count ?? 0);

            Console.WriteLine($"Number of FerryAnnouncement objects: {count:N0}");

            Console.WriteLine("=== PAYLOAD SIZE (bytes) ===");

            Console.WriteLine($"JSON      : {System.Text.Encoding.UTF8.GetByteCount(jsonString):N0} bytes");
            Console.WriteLine($"XML       : {System.Text.Encoding.UTF8.GetByteCount(xmlString):N0} bytes");
            Console.WriteLine($"Protobuf  : {protobufBytes.Length:N0} bytes");
        }

        // =========================
        // JSON
        // =========================

        [Benchmark]
        public string Json_Serialize()
        {
            return JsonSerializer.Serialize(data);
        }

        [Benchmark]
        public Root Json_Deserialize()
        {
            return JsonSerializer.Deserialize<Root>(jsonString)!;
        }

        // =========================
        // XML
        // =========================

        [Benchmark]
        public string Xml_Serialize()
        {
            var serializer = new XmlSerializer(typeof(Root));

            using var sw = new StringWriter();

            serializer.Serialize(sw, data);

            return sw.ToString();
        }

        [Benchmark]
        public Root Xml_Deserialize()
        {
            var serializer = new XmlSerializer(typeof(Root));

            using var sr = new StringReader(xmlString);

            return (Root)serializer.Deserialize(sr)!;
        }

        // =========================
        // PROTOBUF
        // =========================

        [Benchmark]
        public byte[] Protobuf_Serialize()
        {
            using var ms = new MemoryStream();

            Serializer.Serialize(ms, data);

            return ms.ToArray();
        }

        [Benchmark]
        public Root Protobuf_Deserialize()
        {
            using var ms = new MemoryStream(protobufBytes);

            return Serializer.Deserialize<Root>(ms);
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<SerializationBenchmarks>();
        }
    }
}