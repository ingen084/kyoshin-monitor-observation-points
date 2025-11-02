using System.Text;
using System.Text.Json;
using MessagePack;

namespace ObservationPointPacker.Models;

[MessagePackObject]
public class ObservationPointV1 : IComparable
{
    /// <summary>
    /// 観測点情報をmpk形式で保存します。失敗した場合は例外がスローされます。
    /// </summary>
    /// <param name="path">書き込むmpkファイルのパス</param>
    /// <param name="points">書き込む観測点情報の配列</param>
    /// <param name="useLz4">lz4で圧縮させるかどうか(させる場合は拡張子を.mpk.lz4にすることをおすすめします)</param>
    public static void SaveToMpk(string path, IEnumerable<ObservationPointV1> points, bool useLz4 = false)
    {
        using var stream = new FileStream(path, FileMode.Create);
        MessagePackSerializer.Serialize(stream, points.ToArray(), options: useLz4 ? MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4Block) : null);
    }

    /// <summary>
    /// 観測点情報をJson形式で保存します。失敗した場合は例外がスローされます。
    /// </summary>
    /// <param name="path">書き込むJsonファイルのパス</param>
    /// <param name="points">書き込む観測点情報の配列</param>
    public static void SaveToJson(string path, IEnumerable<ObservationPointV1> points)
    {
        using var stream = new FileStream(path, FileMode.Create);
        var data = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(points.ToArray()));
        stream.Write(data, 0, data.Length);
    }

    /// <summary>
    /// 観測点情報をcsvに保存します。失敗した場合は例外がスローされます。
    /// </summary>
    /// <param name="path">書き込むcsvファイルのパス</param>
    /// <param name="points">書き込む観測点情報の配列</param>
    public static void SaveToCsv(string path, IEnumerable<ObservationPointV1> points)
    {
        using var stream = File.OpenWrite(path);
        using var writer = new StreamWriter(stream);
        foreach (var point in points)
            writer.WriteLine($"{(int)point.Type},{point.Code},{point.IsSuspended},{point.Name},{point.Region},{point.Location.Latitude},{point.Location.Longitude},{point.Point?.X.ToString() ?? ""},{point.Point?.Y.ToString() ?? ""},{point.ClassificationId?.ToString() ?? ""},{point.PrefectureClassificationId?.ToString() ?? ""},{point.OldLocation?.Latitude.ToString() ?? ""},{point.OldLocation?.Longitude.ToString() ?? ""}");
    }

    // シリアライザ用コンストラクタのため警告を無効化する
#nullable disable
    /// <summary>
    /// ObservationPointを初期化します。
    /// </summary>
    public ObservationPointV1()
    {
    }
#nullable restore

    /// <summary>
    /// 観測地点のネットワークの種類
    /// </summary>
    [Key(0)]
    public ObservationPointType Type { get; set; }

    /// <summary>
    /// 観測点コード
    /// </summary>
    [Key(1)]
    public string Code { get; set; }

    /// <summary>
    /// 観測点名
    /// </summary>
    [Key(2)]
    public string Name { get; set; }

    /// <summary>
    /// 観測点広域名
    /// </summary>
    [Key(3)]
    public string Region { get; set; }

    /// <summary>
    /// 観測地点が休止状態(無効)かどうか
    /// </summary>
    [Key(4)]
    public bool IsSuspended { get; set; }

    /// <summary>
    /// 地理座標
    /// </summary>
    [Key(5)]
    public OldFormatterLocation Location { get; set; }

    /// <summary>
    /// 地理座標(日本座標系)
    /// </summary>
    [Key(9)]
    public OldFormatterLocation? OldLocation { get; set; }

    /// <summary>
    /// 強震モニタ画像上での座標
    /// </summary>
    [Key(6)]
    public Point2? Point { get; set; }

    /// <summary>
    /// 緊急地震速報や震度速報で用いる区域のID(EqWatchインポート用)
    /// </summary>
    [Key(7)]
    public int? ClassificationId { get; set; }

    /// <summary>
    /// 緊急地震速報で用いる府県予報区のID(EqWatchインポート用)
    /// </summary>
    [Key(8)]
    public int? PrefectureClassificationId { get; set; }

    /// <summary>
    /// ObservationPoint同士を比較します。
    /// </summary>
    /// <param name="obj">比較対象のObservationPoint</param>
    /// <returns></returns>
    public int CompareTo(object? obj)
    {
        if (obj is not ObservationPointV1 ins)
            throw new ArgumentException("比較対象はObservationPointにキャストできる型でなければなりません。");
        return Code.CompareTo(ins.Code);
    }
}
