using System.Text;
using ObservationPointPacker.Models;

namespace ObservationPointPacker.Diff;

/// <summary>
/// 差分を PR コメント用の Markdown にします。
/// </summary>
public static class MarkdownDiffFormatter
{
    /// <summary>
    /// 投稿済みのコメントを特定するためのマーカー (ワークフロー側と一致させること)
    /// </summary>
    public const string Marker = "<!-- observation-points-diff -->";

    /// <summary>
    /// GitHub のコメント上限(65536文字)に対する安全マージン
    /// </summary>
    private const int CommentLimit = 60000;

    /// <summary>
    /// この行数を超えるテーブルは折りたたんで表示する
    /// </summary>
    private const int DetailsThreshold = 25;

    /// <summary>
    /// 1 セクションあたりの最大行数 (コメントが上限を超える場合は順に切り下げる)
    /// </summary>
    private static readonly int[] RowLimitSteps = [1000, 500, 250, 100, 40, 15, 5];

    private static readonly string[] PointHeaders = ["コード", "観測点名", "種別", "地域", "詳細地域", "座標", "画像座標", "状態"];
    private static readonly string[] PointAligns = [":--", ":--", ":--", ":--", ":--", ":--", ":--", ":--"];
    private static readonly string[] ChangeHeaders = ["コード", "観測点名", "項目", "変更前", "変更後"];
    private static readonly string[] ChangeAligns = [":--", ":--", ":--", ":--", ":--"];

    /// <summary>
    /// 差分を Markdown にします。
    /// </summary>
    /// <param name="diff">差分</param>
    /// <param name="beforeRef">変更前のコミット</param>
    /// <param name="afterRef">変更後のコミット</param>
    /// <returns>コメントの本文</returns>
    public static string Format(ObservationPointDiff diff, string? beforeRef = null, string? afterRef = null)
    {
        var body = "";
        // コメントの文字数上限に収まるまで表示行数を切り下げる
        foreach (var rowLimit in RowLimitSteps)
        {
            body = Build(diff, rowLimit, beforeRef, afterRef);
            if (body.Length <= CommentLimit)
                return body;
        }
        return string.Concat(body.AsSpan(0, CommentLimit), "\n\n> ⚠️ 文字数上限のため以降を省略しました。\n");
    }

    /// <summary>
    /// 指定した行数でコメントの本文を組み立てます。
    /// </summary>
    private static string Build(ObservationPointDiff diff, int rowLimit, string? beforeRef, string? afterRef)
    {
        var sb = new StringBuilder();
        sb.Append(Marker).Append("\n## 📍 観測点データの差分\n\n");

        if (!diff.HasChanges)
        {
            sb.Append("`intensity-points.json` に内容の変更はありません。(整形や並び順のみの変更です)\n\n");
        }
        else
        {
            var delta = diff.AfterCount - diff.BeforeCount;
            AppendTable(sb, ["区分", "件数"], [":--", "--:"], [
                ["➕ 追加", diff.Added.Count.ToString()],
                ["➖ 削除", diff.Removed.Count.ToString()],
                ["✏️ 変更", diff.Changed.Count.ToString()],
                ["観測点数", $"{diff.BeforeCount} → {diff.AfterCount} ({ObservationPointDisplay.Signed(delta)})"],
            ]);
            sb.Append('\n');
        }

        if (diff.DuplicatedCodes.Count > 0)
            sb.Append("> ⚠️ 観測点コードが重複しています: ")
                .AppendJoin(", ", diff.DuplicatedCodes.Select(c => $"`{c}`"))
                .Append("\n> 重複した観測点は最初の 1 件のみを比較対象としています。\n\n");

        AppendSection(sb, "➕ 追加された観測点", diff.Added.Count, PointHeaders, PointAligns, [.. diff.Added.Select(PointRow)], rowLimit);
        AppendSection(sb, "➖ 削除された観測点", diff.Removed.Count, PointHeaders, PointAligns, [.. diff.Removed.Select(PointRow)], rowLimit);
        AppendSection(sb, "✏️ 変更された観測点", diff.Changed.Count, ChangeHeaders, ChangeAligns, ChangeRows(diff.Changed), rowLimit);

        if (!string.IsNullOrEmpty(beforeRef) && !string.IsNullOrEmpty(afterRef))
            sb.Append($"<sub>比較対象: `{ShortenRef(beforeRef)}` → `{ShortenRef(afterRef)}`</sub>\n");

        return sb.ToString();
    }

    /// <summary>
    /// 見出しとテーブルからなるセクションを追加します。
    /// </summary>
    /// <param name="sb">追加先</param>
    /// <param name="heading">見出し</param>
    /// <param name="itemCount">見出しに表示する件数</param>
    /// <param name="headers">テーブルのヘッダー</param>
    /// <param name="aligns">テーブルの各列の寄せ方</param>
    /// <param name="rows">テーブルの行</param>
    /// <param name="rowLimit">表示する最大行数</param>
    private static void AppendSection(
        StringBuilder sb,
        string heading,
        int itemCount,
        string[] headers,
        string[] aligns,
        IReadOnlyList<string[]> rows,
        int rowLimit)
    {
        if (rows.Count == 0)
            return;

        var shownCount = Math.Min(rows.Count, rowLimit);
        var table = new StringBuilder();
        AppendTable(table, headers, aligns, [.. rows.Take(shownCount)]);
        if (rows.Count > shownCount)
            table.Append($"\n> ⚠️ コメントの文字数上限のため、残り {rows.Count - shownCount} 行を省略しました。\n");

        sb.Append($"### {heading} ({itemCount} 件)\n\n");
        if (rows.Count > DetailsThreshold)
            sb.Append("<details>\n<summary>表を開く</summary>\n\n").Append(table).Append("\n</details>\n\n");
        else
            sb.Append(table).Append('\n');
    }

    /// <summary>
    /// Markdown のテーブルを追加します。
    /// </summary>
    private static void AppendTable(StringBuilder sb, string[] headers, string[] aligns, IReadOnlyList<string[]> rows)
    {
        sb.Append("| ").AppendJoin(" | ", headers).Append(" |\n");
        sb.Append("| ").AppendJoin(" | ", aligns).Append(" |\n");
        foreach (var row in rows)
            sb.Append("| ").AppendJoin(" | ", row.Select(EscapeCell)).Append(" |\n");
    }

    /// <summary>
    /// 追加・削除された観測点の行を作成します。
    /// </summary>
    private static string[] PointRow(CommonObservationPoint point) => [
        $"`{point.Code}`",
        ObservationPointDisplay.Text(point.Name),
        ObservationPointDisplay.TypeName(point.Type),
        ObservationPointDisplay.Text(point.Region),
        ObservationPointDisplay.Text(point.SubRegion),
        ObservationPointDisplay.LocationText(point.Location),
        ObservationPointDisplay.ImagePointText(point.Point),
        ObservationPointDisplay.SuspendedText(point.IsSuspended),
    ];

    /// <summary>
    /// 変更された観測点の行を作成します。
    /// 1 つの観測点で複数の項目が変わった場合は、2 行目以降のコードと観測点名を空欄にする
    /// </summary>
    private static List<string[]> ChangeRows(IReadOnlyList<ChangedObservationPoint> changed)
    {
        var rows = new List<string[]>();
        foreach (var item in changed)
            for (var i = 0; i < item.Changes.Count; i++)
            {
                var change = item.Changes[i];
                rows.Add([
                    i == 0 ? $"`{item.After.Code}`" : "",
                    i == 0 ? ObservationPointDisplay.Text(item.After.Name) : "",
                    change.Label,
                    change.Before,
                    change.Note is null ? change.After : $"{change.After} ({change.Note})",
                ]);
            }
        return rows;
    }

    /// <summary>
    /// テーブルのセルに埋め込める形にします。
    /// </summary>
    private static string EscapeCell(string value)
        => value.Replace("|", "\\|").ReplaceLineEndings(" ");

    /// <summary>
    /// コミットハッシュを短縮します。
    /// </summary>
    private static string ShortenRef(string reference)
        => reference.Length > 7 ? reference[..7] : reference;
}
