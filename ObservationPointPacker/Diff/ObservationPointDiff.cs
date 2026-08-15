using ObservationPointPacker.Models;

namespace ObservationPointPacker.Diff;

/// <summary>
/// 観測点の 1 項目分の変更内容
/// </summary>
/// <param name="Label">項目名</param>
/// <param name="Before">変更前の表示内容</param>
/// <param name="After">変更後の表示内容</param>
/// <param name="Note">変更後に付記する補足 (移動距離など)</param>
public record FieldChange(string Label, string Before, string After, string? Note = null);

/// <summary>
/// 変更のあった観測点
/// </summary>
/// <param name="Before">変更前の観測点</param>
/// <param name="After">変更後の観測点</param>
/// <param name="Changes">変更のあった項目</param>
public record ChangedObservationPoint(
    CommonObservationPoint Before,
    CommonObservationPoint After,
    IReadOnlyList<FieldChange> Changes);

/// <summary>
/// 観測点データの差分
/// </summary>
/// <param name="Added">追加された観測点</param>
/// <param name="Removed">削除された観測点</param>
/// <param name="Changed">変更された観測点</param>
/// <param name="DuplicatedCodes">変更後のデータで重複していた観測点コード</param>
/// <param name="BeforeCount">変更前の観測点数</param>
/// <param name="AfterCount">変更後の観測点数</param>
public record ObservationPointDiff(
    IReadOnlyList<CommonObservationPoint> Added,
    IReadOnlyList<CommonObservationPoint> Removed,
    IReadOnlyList<ChangedObservationPoint> Changed,
    IReadOnlyList<string> DuplicatedCodes,
    int BeforeCount,
    int AfterCount)
{
    /// <summary>
    /// 内容に変更があるかどうか
    /// </summary>
    public bool HasChanges => Added.Count > 0 || Removed.Count > 0 || Changed.Count > 0;
}
