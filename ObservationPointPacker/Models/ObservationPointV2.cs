using System.Text.Json.Serialization;
using MessagePack;
using ObservationPointPacker.Formatter;

namespace ObservationPointPacker.Models;

/// <summary>
/// NIEDの観測点情報
/// </summary>
[MessagePackObject]
public class ObservationPointV2 : IComparable
{
    // シリアライザ用コンストラクタのため警告を無効化する
#nullable disable
    /// <summary>
    /// ObservationPointを初期化します。
    /// </summary>
    public ObservationPointV2()
    {
    }
#nullable restore

    /// <summary>
    /// 観測点コード
    /// </summary>
    [Key(0), JsonPropertyName("code")]
    public string Code { get; set; }

    /// <summary>
    /// 観測地点のネットワークの種類
    /// </summary>
    [Key(1), JsonPropertyName("type")]
    public ObservationPointType Type { get; set; }

    /// <summary>
    /// 観測点名
    /// </summary>
    [Key(2), JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// 観測点広域名
    /// </summary>
    [Key(3), JsonPropertyName("region")]
    public string Region { get; set; }

    /// <summary>
    /// 観測地点が休止状態(無効)かどうか
    /// </summary>
    [Key(4), JsonPropertyName("is_suspended")]
    public bool IsSuspended { get; set; }

    /// <summary>
    /// 地理座標
    /// </summary>
    [Key(5), JsonPropertyName("location"), MessagePackFormatter(typeof(LocationFormatter))]
    public Location Location { get; set; }

    /// <summary>
    /// 強震モニタ画像上での座標
    /// </summary>
    [Key(6), JsonPropertyName("point")]
    public KyoshinImagePoint? Point { get; set; }
    
    /// <summary>
    /// ObservationPoint同士を比較します。
    /// </summary>
    /// <param name="obj">比較対象のObservationPoint</param>
    /// <returns></returns>
    public int CompareTo(object? obj)
    {
        if (obj is not ObservationPointV2 ins)
            throw new ArgumentException("比較対象はObservationPointにキャストできる型でなければなりません。");
        return Code.CompareTo(ins.Code);
    }

    // public string SerializeCsvLine()
    // => $"{(int)Type},{Code},{IsSuspended},{Name},{Region},{Location.Latitude},{Location.Longitude},{Point?.Center.X.ToString() ?? ""},{Point?.Center.Y.ToString() ?? ""},{Point?.Offset.X.ToString() ?? ""},{Point?.Offset.Y.ToString() ?? ""}";

    // public static ObservationPointV2 DeserializeCsvLine(string line)
    // {
    //     if (line.Split(',') is not string[] strings || strings.Length < 7)
    //         throw new ArgumentException("不正な形式のCSV行です。");

    //     var point = new ObservationPointV2()
    //     {
    //         Type = (ObservationPointType)int.Parse(strings[0]),
    //         Code = strings[1],
    //         IsSuspended = bool.Parse(strings[2]),
    //         Name = strings[3],
    //         Region = strings[4],
    //         Location = new Location(float.Parse(strings[5]), float.Parse(strings[6])),
    //         Point = null
    //     };
    //     if (!string.IsNullOrWhiteSpace(strings[7]) && !string.IsNullOrWhiteSpace(strings[8]) &&
    //         !string.IsNullOrWhiteSpace(strings[9]) && !string.IsNullOrWhiteSpace(strings[10]))
    //         point.Point = new(new(int.Parse(strings[7]), int.Parse(strings[8])), new(int.Parse(strings[9]), int.Parse(strings[10])));

    //     return point;
    // }
}
