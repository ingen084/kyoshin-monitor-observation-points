using System.Text.Json.Serialization;
using MessagePack;
using ObservationPointPacker.Formatter;

namespace ObservationPointPacker.Models;

/// <summary>
/// 地理座標
/// </summary>
[MessagePackObject]
public class OldFormatterLocation
{
    /// <summary>
    /// Locationを初期化します。
    /// </summary>
    public OldFormatterLocation()
    {
    }

    /// <summary>
    /// 初期値を指定してLocationを初期化します。
    /// </summary>
    /// <param name="latitude">緯度</param>
    /// <param name="longitude">経度</param>
    public OldFormatterLocation(float latitude, float longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    /// <summary>
    /// 緯度
    /// </summary>
    [Key(0), JsonPropertyName("latitude")]
    public float Latitude { get; set; }

    /// <summary>
    /// 経度
    /// </summary>
    [Key(1), JsonPropertyName("longitude")]
    public float Longitude { get; set; }

    /// <summary>
    /// 文字化
    /// </summary>
    /// <returns>文字</returns>
    public override string ToString()
        => $"Lat:{Latitude} Lng:{Longitude}";
}
