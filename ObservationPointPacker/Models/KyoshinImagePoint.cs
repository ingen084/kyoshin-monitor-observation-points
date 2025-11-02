using System.Text.Json.Serialization;
using MessagePack;
using ObservationPointPacker.Formatter;

namespace ObservationPointPacker.Models;

[MessagePackFormatter(typeof(KyoshinImagePointFormatter))]
public record class KyoshinImagePoint(
    [property: JsonPropertyName("center_point")] Point2 Center,
    [property: JsonPropertyName("offset")] Point2 Offset);
