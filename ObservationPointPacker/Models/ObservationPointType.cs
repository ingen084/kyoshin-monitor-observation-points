namespace ObservationPointPacker.Models;

/// <summary>
/// 観測点のタイプ
/// </summary>
public enum ObservationPointType
{
    /// <summary>
    /// 不明(なるべく使用しないように)
    /// </summary>
    Unknown,

    /// <summary>
    /// KiK-net
    /// </summary>
    KiK_net,

    /// <summary>
    /// K-NET
    /// </summary>
    K_NET,
}
