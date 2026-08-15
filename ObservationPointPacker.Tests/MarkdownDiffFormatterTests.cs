using ObservationPointPacker.Diff;
using ObservationPointPacker.Models;
using Xunit;

using static ObservationPointPacker.Tests.ObservationPointDifferTests;

namespace ObservationPointPacker.Tests;

public class MarkdownDiffFormatterTests
{
    /// <summary>
    /// GitHub のコメントの文字数上限
    /// </summary>
    private const int GitHubCommentLimit = 65536;

    [Fact]
    public void Format_ShouldContain_MarkerAndTables()
    {
        // Arrange
        CommonObservationPoint[] before = [CreatePoint("AAA001"), CreatePoint("AAA003")];
        CommonObservationPoint[] after = [CreatePoint("AAA001", name: "変更後"), CreatePoint("AAA002")];
        var diff = ObservationPointDiffer.Compare(before, after);

        // Act
        var markdown = MarkdownDiffFormatter.Format(diff, "abcdef1234567890", "1234567890abcdef");

        // Assert
        Assert.StartsWith(MarkdownDiffFormatter.Marker, markdown);
        Assert.Contains("### ➕ 追加された観測点 (1 件)", markdown);
        Assert.Contains("### ➖ 削除された観測点 (1 件)", markdown);
        Assert.Contains("### ✏️ 変更された観測点 (1 件)", markdown);
        Assert.Contains("| `AAA002` | テスト観測点 | K-NET | 東京都 | 東京都23区 | 35, 139 | (100, 100) | 稼働中 |", markdown);
        Assert.Contains("| `AAA001` | 変更後 | 観測点名 | テスト観測点 | 変更後 |", markdown);
        Assert.Contains("<sub>比較対象: `abcdef1` → `1234567`</sub>", markdown);
    }

    [Fact]
    public void Format_WithNoChanges_ShouldNotContainTable()
    {
        // Arrange
        CommonObservationPoint[] points = [CreatePoint("AAA001")];
        var diff = ObservationPointDiffer.Compare(points, points);

        // Act
        var markdown = MarkdownDiffFormatter.Format(diff);

        // Assert
        Assert.Contains("内容の変更はありません", markdown);
        Assert.DoesNotContain("###", markdown);
    }

    [Fact]
    public void Format_ShouldEscape_PipeCharacter()
    {
        // Arrange
        CommonObservationPoint[] before = [];
        CommonObservationPoint[] after = [CreatePoint("AAA001", name: "パイプ|入りの名前")];
        var diff = ObservationPointDiffer.Compare(before, after);

        // Act
        var markdown = MarkdownDiffFormatter.Format(diff);

        // Assert
        Assert.Contains(@"パイプ\|入りの名前", markdown);
    }

    [Fact]
    public void Format_WithManyRows_ShouldCollapseIntoDetails()
    {
        // Arrange (折りたたみのしきい値である 25 行を超える)
        CommonObservationPoint[] before = [];
        var after = Enumerable.Range(0, 30).Select(i => CreatePoint($"AAA{i:000}")).ToArray();
        var diff = ObservationPointDiffer.Compare(before, after);

        // Act
        var markdown = MarkdownDiffFormatter.Format(diff);

        // Assert
        Assert.Contains("<details>", markdown);
        Assert.Contains("</details>", markdown);
    }

    [Fact]
    public void Format_WithHugeDiff_ShouldFitInCommentLimit()
    {
        // Arrange (すべての観測点で複数項目が変わる)
        var before = Enumerable.Range(0, 3000).Select(i => CreatePoint($"AAA{i:0000}")).ToArray();
        var after = before
            .Select(p => CreatePoint(p.Code, name: $"{p.Name}変更後", latitude: 36f, point: new(new(200, 200), new(1, 1))))
            .ToArray();
        var diff = ObservationPointDiffer.Compare(before, after);

        // Act
        var markdown = MarkdownDiffFormatter.Format(diff);

        // Assert
        Assert.True(
            markdown.Length <= GitHubCommentLimit,
            $"コメントの文字数({markdown.Length})が上限({GitHubCommentLimit})を超えています");
        Assert.Contains("省略しました", markdown);
        // 件数自体は省略せずに伝える
        Assert.Contains("### ✏️ 変更された観測点 (3000 件)", markdown);
    }

    [Fact]
    public void Format_WithDuplicatedCode_ShouldContainWarning()
    {
        // Arrange
        CommonObservationPoint[] before = [CreatePoint("AAA001")];
        CommonObservationPoint[] after = [CreatePoint("AAA001"), CreatePoint("AAA001")];
        var diff = ObservationPointDiffer.Compare(before, after);

        // Act
        var markdown = MarkdownDiffFormatter.Format(diff);

        // Assert
        Assert.Contains("観測点コードが重複しています: `AAA001`", markdown);
    }
}
