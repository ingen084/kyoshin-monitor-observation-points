using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using ObservationPointPacker.Models;

namespace ObservationPointPacker;

/// <summary>
/// intensity-points.json の読み書きを行います。
/// </summary>
public static class ObservationPointJsonLoader
{
    /// <summary>
    /// intensity-points.json のシリアライズオプション
    /// </summary>
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        WriteIndented = true,
    };

    /// <summary>
    /// 観測点情報を読み込みます。
    /// </summary>
    /// <param name="path">読み込むファイルのパス</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>観測点情報 (ファイルが存在しない、または空の場合は空の配列)</returns>
    public static async Task<CommonObservationPoint[]> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
            return [];

        using var stream = File.OpenRead(path);
        if (stream.Length == 0)
            return [];

        return await JsonSerializer.DeserializeAsync<CommonObservationPoint[]>(stream, Options, cancellationToken)
            ?? throw new InvalidOperationException($"{path} の読み込みに失敗しました");
    }
}
