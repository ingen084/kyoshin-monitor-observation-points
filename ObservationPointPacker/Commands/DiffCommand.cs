using System.Text;
using ObservationPointPacker.Diff;

namespace ObservationPointPacker.Commands;

/// <summary>
/// 2 つの観測点データを比較して、PR コメント用の Markdown を出力します。
/// </summary>
public static class DiffCommand
{
    /// <summary>
    /// 差分の出力を実行します。
    /// </summary>
    /// <param name="args">beforeJson, afterJson, outputPath, (beforeRef), (afterRef)</param>
    /// <returns>終了コード</returns>
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine("使用方法: ObservationPointPacker diff <beforeJson> <afterJson> <outputPath> [beforeRef] [afterRef]");
            Console.Error.WriteLine("  beforeJson: 変更前の intensity-points.json");
            Console.Error.WriteLine("  afterJson: 変更後の intensity-points.json");
            Console.Error.WriteLine("  outputPath: Markdown の出力先");
            Console.Error.WriteLine("  beforeRef/afterRef: 比較対象として表示するコミットハッシュ (省略可)");
            return 1;
        }

        var beforePath = args[0];
        var afterPath = args[1];
        var outputPath = args[2];
        var beforeRef = args.Length > 3 ? args[3] : null;
        var afterRef = args.Length > 4 ? args[4] : null;

        var before = await ObservationPointJsonLoader.LoadAsync(beforePath);
        var after = await ObservationPointJsonLoader.LoadAsync(afterPath);
        Console.WriteLine($"観測点数: {before.Length}件 → {after.Length}件");

        var diff = ObservationPointDiffer.Compare(before, after);
        Console.WriteLine($"差分: 追加 {diff.Added.Count}件 / 削除 {diff.Removed.Count}件 / 変更 {diff.Changed.Count}件");

        var markdown = MarkdownDiffFormatter.Format(diff, beforeRef, afterRef);
        await File.WriteAllTextAsync(outputPath, markdown, new UTF8Encoding(false));
        Console.WriteLine($"{outputPath} に出力しました ({markdown.Length}文字)");

        // GitHub Actions のステップ出力に差分の有無を渡す
        if (Environment.GetEnvironmentVariable("GITHUB_OUTPUT") is { Length: > 0 } githubOutput)
            await File.AppendAllLinesAsync(githubOutput, [
                $"has_changes={(diff.HasChanges ? "true" : "false")}",
                $"added={diff.Added.Count}",
                $"removed={diff.Removed.Count}",
                $"changed={diff.Changed.Count}",
            ]);

        return 0;
    }
}
