using ObservationPointPacker.Models;

namespace ObservationPointPacker.Diff;

/// <summary>
/// 観測点データ同士を比較します。
/// </summary>
public static class ObservationPointDiffer
{
    /// <summary>
    /// 比較する項目
    /// </summary>
    /// <param name="Label">項目名</param>
    /// <param name="Format">表示内容を取得します</param>
    /// <param name="Annotate">変更後に付記する補足を取得します</param>
    private record ComparisonField(
        string Label,
        Func<CommonObservationPoint, string> Format,
        Func<CommonObservationPoint, CommonObservationPoint, string?>? Annotate = null);

    /// <summary>
    /// 比較する項目の一覧
    /// 表示内容が変わったものを変更として扱うため、表示と差分の判定は常に一致する
    /// </summary>
    private static readonly ComparisonField[] Fields = [
        new("観測点名", p => ObservationPointDisplay.Text(p.Name)),
        new("種別", p => ObservationPointDisplay.TypeName(p.Type)),
        new("地域", p => ObservationPointDisplay.Text(p.Region)),
        new("詳細地域", p => ObservationPointDisplay.Text(p.SubRegion)),
        new("状態", p => ObservationPointDisplay.SuspendedText(p.IsSuspended)),
        new(
            "座標",
            p => ObservationPointDisplay.LocationText(p.Location),
            (before, after) => ObservationPointDisplay.DistanceText(before.Location, after.Location)),
        new(
            "座標(日本測地系)",
            p => ObservationPointDisplay.LocationText(p.OldLocation),
            (before, after) => ObservationPointDisplay.DistanceText(before.OldLocation, after.OldLocation)),
        new(
            "画像座標",
            p => ObservationPointDisplay.ImagePointText(p.Point),
            (before, after) => ObservationPointDisplay.ImagePointDeltaText(before.Point, after.Point)),
    ];

    /// <summary>
    /// 観測点データを比較して差分を求めます。
    /// </summary>
    /// <param name="before">変更前の観測点</param>
    /// <param name="after">変更後の観測点</param>
    /// <returns>差分</returns>
    public static ObservationPointDiff Compare(
        IReadOnlyList<CommonObservationPoint> before,
        IReadOnlyList<CommonObservationPoint> after)
    {
        var beforePoints = IndexByCode(before, out _);
        var afterPoints = IndexByCode(after, out var duplicatedCodes);

        var added = new List<CommonObservationPoint>();
        var changed = new List<ChangedObservationPoint>();
        foreach (var (code, afterPoint) in afterPoints)
        {
            if (!beforePoints.TryGetValue(code, out var beforePoint))
            {
                added.Add(afterPoint);
                continue;
            }
            var changes = CompareFields(beforePoint, afterPoint);
            if (changes.Length > 0)
                changed.Add(new(beforePoint, afterPoint, changes));
        }

        var removed = beforePoints
            .Where(p => !afterPoints.ContainsKey(p.Key))
            .Select(p => p.Value)
            .ToList();

        added.Sort(CompareByCode);
        removed.Sort(CompareByCode);
        changed.Sort((x, y) => CompareByCode(x.After, y.After));

        return new(added, removed, changed, duplicatedCodes, before.Count, after.Count);
    }

    /// <summary>
    /// 観測点コードをキーにした辞書を作成します。
    /// </summary>
    /// <param name="points">観測点</param>
    /// <param name="duplicatedCodes">重複していた観測点コード</param>
    private static Dictionary<string, CommonObservationPoint> IndexByCode(
        IReadOnlyList<CommonObservationPoint> points,
        out string[] duplicatedCodes)
    {
        var result = new Dictionary<string, CommonObservationPoint>(StringComparer.Ordinal);
        var duplicated = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var point in points)
        {
            // 重複していた場合は先に見つかったものを比較対象とする
            if (!result.TryAdd(point.Code ?? "", point))
                duplicated.Add(point.Code ?? "");
        }
        duplicatedCodes = [.. duplicated];
        return result;
    }

    /// <summary>
    /// 観測点の各項目を比較します。
    /// </summary>
    private static FieldChange[] CompareFields(CommonObservationPoint before, CommonObservationPoint after)
    {
        var changes = new List<FieldChange>();
        foreach (var field in Fields)
        {
            var beforeText = field.Format(before);
            var afterText = field.Format(after);
            if (beforeText == afterText)
                continue;
            changes.Add(new(field.Label, beforeText, afterText, field.Annotate?.Invoke(before, after)));
        }
        return [.. changes];
    }

    /// <summary>
    /// 観測点コードで比較します。
    /// </summary>
    private static int CompareByCode(CommonObservationPoint x, CommonObservationPoint y)
        => string.CompareOrdinal(x.Code, y.Code);
}
