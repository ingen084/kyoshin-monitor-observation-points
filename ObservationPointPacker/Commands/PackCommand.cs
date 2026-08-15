using ObservationPointPacker.Models;

namespace ObservationPointPacker.Commands;

/// <summary>
/// 観測点データをリリース用の各形式に変換して出力します。
/// </summary>
public static class PackCommand
{
    /// <summary>
    /// パッケージングを実行します。
    /// </summary>
    /// <param name="args">dataVersion, outputDir</param>
    /// <returns>終了コード</returns>
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("使用方法: ObservationPointPacker pack <dataVersion> <outputDir>");
            Console.Error.WriteLine("  dataVersion: データバージョン (例: v1.0.0)");
            Console.Error.WriteLine("  outputDir: 出力先ディレクトリ");
            return 1;
        }

        var dataVersion = args[0];
        var outputDir = args[1];

        // 出力ディレクトリを作成
        Directory.CreateDirectory(outputDir);

        // intensity-points.jsonを読み込み
        Console.WriteLine("intensity-points.jsonを読み込んでいます...");
        var points = await ObservationPointJsonLoader.LoadAsync("intensity-points.json");
        if (points.Length == 0)
            throw new InvalidOperationException("intensity-points.json が存在しないか、観測点が含まれていません");

        // V1形式に変換
        var v1Points = points.Select(p => p.ToV1()).ToArray();

        // V2形式に変換 (休止中または画像座標なしの観測点は除外)
        var v2Points = points
            .Where(p => !p.IsSuspended && p.Point is not null)
            .Select(p => p.ToV2())
            .ToArray();
        Console.WriteLine($"  {points.Length}件中 {v2Points.Length}件をパッケージに含めます (除外: 休止中または画像座標なし)");

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
    }
}
