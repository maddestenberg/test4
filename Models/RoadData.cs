using ProtoBuf;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace test4.Models
{
    public class JsonRoot
    {
        public JsonResponse RESPONSE { get; set; }
    }

    public class JsonResponse
    {
        public List<JsonResult> RESULT { get; set; }
    }

    public class JsonResult
    {
        public List<FerryAnnouncement> FerryAnnouncement { get; set; }
    }

    [ProtoContract]
    [XmlRoot("RESPONSE")]
    public class Root
    {
        [ProtoMember(1)]
        [XmlElement("RESULT")]
        public List<Result> RESULT { get; set; }
    }

    [ProtoContract]
    public class Result
    {
        [ProtoMember(1)]
        [XmlElement("FerryAnnouncement")]
        [JsonPropertyName("FerryAnnouncement")]
        public List<FerryAnnouncement> FerryAnnouncement { get; set; }
    }

    [ProtoContract]
    public class FerryAnnouncement
    {
        [ProtoMember(1)]
        public bool Deleted { get; set; }

        [ProtoMember(2)]
        public string DepartureTime { get; set; }

        [ProtoMember(3)]
        public string DeviationId { get; set; }

        [ProtoMember(4)]
        public long Id { get; set; }

        [ProtoMember(5)]
        public Harbor FromHarbor { get; set; }

        [ProtoMember(6)]
        public Harbor ToHarbor { get; set; }

        [ProtoMember(7)]
        public Route Route { get; set; }

        [ProtoMember(8)]
        public string ModifiedTime { get; set; }
    }

    [ProtoContract]
    public class Harbor
    {
        [ProtoMember(1)]
        public int Id { get; set; }

        [ProtoMember(2)]
        public string Name { get; set; }
    }

    [ProtoContract]
    public class Route
    {
        [ProtoMember(1)]
        public int Id { get; set; }

        [ProtoMember(2)]
        public string Name { get; set; }

        [ProtoMember(3)]
        public string Shortname { get; set; }

        [ProtoMember(4)]
        public RouteType Type { get; set; }
    }

    [ProtoContract]
    public class RouteType
    {
        [ProtoMember(1)]
        public int Id { get; set; }

        [ProtoMember(2)]
        public string Name { get; set; }
    }
}