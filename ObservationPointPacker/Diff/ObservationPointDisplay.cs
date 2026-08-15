using ObservationPointPacker.Models;

namespace ObservationPointPacker.Diff;

/// <summary>
/// 観測点の内容を人が読める文字列にします。
/// </summary>
public static class ObservationPointDisplay
{
    /// <summary>
    /// 値が未設定の場合に使用する文字列
    /// </summary>
    public const string Empty = "-";

    /// <summary>
    /// 文字列を表示用にします。
    /// </summary>
    public static string Text(string? value)
        => string.IsNullOrEmpty(value) ? Empty : value;

    /// <summary>
    /// 観測点の種別を表示用にします。
    /// </summary>
    public static string TypeName(ObservationPointType type)
        => type switch
        {
            ObservationPointType.KiK_net => "KiK-net",
            ObservationPointType.K_NET => "K-NET",
            _ => "不明",
        };

    /// <summary>
    /// 休止状態を表示用にします。
    /// </summary>
    public static string SuspendedText(bool isSuspended)
        => isSuspended ? "⏸️ 休止中" : "稼働中";

    /// <summary>
    /// 地理座標を表示用にします。
    /// </summary>
    public static string LocationText(Location? location)
        => location is null ? Empty : $"{location.Latitude:0.######}, {location.Longitude:0.######}";

    /// <summary>
    /// 強震モニタ画像上での座標を表示用にします。
    /// </summary>
    public static string ImagePointText(KyoshinImagePoint? point)
    {
        if (point is null)
            return Empty;
        var center = $"({point.Center.X}, {point.Center.Y})";
        if (point.Offset.X == 0 && point.Offset.Y == 0)
            return center;
        return $"{center} ずれ {Signed(point.Offset.X)}, {Signed(point.Offset.Y)}";
    }

    /// <summary>
    /// 符号付きの数値にします。(0 には符号を付けません)
    /// </summary>
    public static string Signed(int value)
        => value > 0 ? $"+{value}" : value.ToString();

    /// <summary>
    /// 2 地点間の距離を求めます。
    /// </summary>
    /// <returns>距離(メートル)</returns>
    public static double DistanceMeters(Location before, Location after)
    {
        const double earthRadius = 6371000;
        static double ToRadian(double degree) => degree * Math.PI / 180;

        var latitude1 = ToRadian(before.Latitude);
        var latitude2 = ToRadian(after.Latitude);
        var deltaLatitude = ToRadian((double)after.Latitude - before.Latitude);
        var deltaLongitude = ToRadian((double)after.Longitude - before.Longitude);

        var h = Math.Pow(Math.Sin(deltaLatitude / 2), 2)
            + Math.Cos(latitude1) * Math.Cos(latitude2) * Math.Pow(Math.Sin(deltaLongitude / 2), 2);
        return 2 * earthRadius * Math.Asin(Math.Min(1, Math.Sqrt(h)));
    }

    /// <summary>
    /// 座標の移動距離を表示用にします。
    /// </summary>
    /// <returns>付記する文字列 (移動していない場合や座標が欠けている場合は null)</returns>
    public static string? DistanceText(Location? before, Location? after)
    {
        if (before is null || after is null)
            return null;
        var meters = DistanceMeters(before, after);
        // 表示するほどの差がない
        if (meters < 1)
            return null;
        return meters < 1000
            ? $"約 {Math.Round(meters)}m"
            : $"約 {(meters / 1000).ToString(meters < 10000 ? "0.0" : "0")}km";
    }

    /// <summary>
    /// 画像座標の移動量を表示用にします。
    /// </summary>
    /// <returns>付記する文字列 (座標が欠けている場合は null)</returns>
    public static string? ImagePointDeltaText(KyoshinImagePoint? before, KyoshinImagePoint? after)
    {
        if (before is null || after is null)
            return null;
        // 実際に描画される位置は 中心 + ずれ で決まる
        var delta = (after.Center + after.Offset) - (before.Center + before.Offset);
        if (delta.X == 0 && delta.Y == 0)
            return "描画位置は変化なし";
        return $"Δ {Signed(delta.X)}, {Signed(delta.Y)}";
    }
}
