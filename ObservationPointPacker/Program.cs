using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using ObservationPointPacker;
using ObservationPointPacker.Models;

// コマンドライン引数を解析
if (args.Length < 2)
{
    Console.WriteLine("使用方法: ObservationPointPacker <dataVersion> <outputDir>");
    Console.WriteLine("  dataVersion: データバージョン (例: v1.0.0)");
    Console.WriteLine("  outputDir: 出力先ディレクトリ");
    return 1;
}

var dataVersion = args[0];
var outputDir = args[1];

// 出力ディレクトリを作成
Directory.CreateDirectory(outputDir);

// intensity-points.jsonを読み込み
Console.WriteLine("intensity-points.jsonを読み込んでいます...");
using var stream = File.OpenRead("intensity-points.json");
var points = await JsonSerializer.DeserializeAsync<CommonObservationPoint[]>(stream, new JsonSerializerOptions
{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		PropertyNameCaseInsensitive = true,
		Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
		Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
		WriteIndented = true
}) ?? throw new InvalidOperationException("データの読み込みに失敗しました");

// V1形式に変換
var v1Points = points.Select(p => p.ToV1()).ToArray();

// V2形式に変換
var v2Points = points.Select(p => p.ToV2()).ToArray();

var packedAt = DateTime.UtcNow;

// V1形式の出力
Console.WriteLine("V1形式で出力しています...");
ObservationPointV1.SaveToMpk(Path.Combine(outputDir, "intensity-points-v1.mpk"), v1Points, useLz4: false);
ObservationPointV1.SaveToMpk(Path.Combine(outputDir, "intensity-points-v1.mpk.lz4"), v1Points, useLz4: true);
ObservationPointV1.SaveToJson(Path.Combine(outputDir, "intensity-points-v1.json"), v1Points);
ObservationPointV1.SaveToCsv(Path.Combine(outputDir, "intensity-points-v1.csv"), v1Points);

// V2形式の出力 (KMOP形式)
Console.WriteLine("V2形式 (KMOP) で出力しています...");
var v2Header = new ObservationPointsFileHeader
{
    Version = 0,
    DataVersion = dataVersion,
    PackedAt = packedAt,
    Source = "https://github.com/ingen084/kyoshin-monitor-observation-points",
    CompressionMode = ObservationPointsCompressionMode.MessagePackCSharpLz4BlockArray
};

using (var kmopStream = File.Create(Path.Combine(outputDir, "intensity-points-v2.kmop")))
{
    using var writer = new ObservationPointsFileReader(kmopStream);
    await writer.WriteHeader(v2Header);
    await writer.WriteData(v2Points, v2Header.CompressionMode);
}

// 元のJSONファイルもコピー
Console.WriteLine("元のJSONファイルをコピーしています...");
File.Copy("intensity-points.json", Path.Combine(outputDir, "intensity-points.json"), overwrite: true);

Console.WriteLine($"パッケージング完了: {outputDir}");
return 0;
