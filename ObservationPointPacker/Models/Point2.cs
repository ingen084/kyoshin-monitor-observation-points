using System.Text.Json.Serialization;
using MessagePack;

namespace ObservationPointPacker.Models;

/// <summary>
/// シリアライズ+四則演算をできるようにしたPointクラス
/// </summary>
/// <param name="x">X</param>
/// <param name="y">Y</param>
[MessagePackObject]
public struct Point2(int x, int y)
{

    /// <summary>
    /// X座標
    /// </summary>
    [Key(0), JsonPropertyName("x")]
    public int X { get; set; } = x;

    /// <summary>
    /// Y座標
    /// </summary>
    [Key(1), JsonPropertyName("y")]
    public int Y { get; set; } = y;

    /// <summary>
    /// 文字にします。
    /// </summary>
    /// <returns>文字</returns>
    public override string ToString()
        => $"X:{X} Y:{Y}";

    /// <summary>
    /// 整数とPoint2を足します。
    /// </summary>
    /// <param name="point"></param>
    /// <param name="num"></param>
    /// <returns></returns>
    public static Point2 operator +(Point2 point, int num)
        => new(point.X + num, point.Y + num);

    /// <summary>
    /// Point2から整数を引きます。
    /// </summary>
    /// <param name="point"></param>
    /// <param name="num"></param>
    /// <returns></returns>
    public static Point2 operator -(Point2 point, int num)
        => new(point.X - num, point.Y - num);

    /// <summary>
    /// Point2同士を足します。
    /// </summary>
    /// <param name="point1"></param>
    /// <param name="point2"></param>
    /// <returns></returns>
    public static Point2 operator +(Point2 point1, Point2 point2)
        => new(point1.X + point2.X, point1.Y + point2.Y);

    /// <summary>
    /// Point2同士を引きます。
    /// </summary>
    /// <param name="point1"></param>
    /// <param name="point2"></param>
    /// <returns></returns>
    public static Point2 operator -(Point2 point1, Point2 point2)
        => new(point1.X - point2.X, point1.Y - point2.Y);

    /// <summary>
    /// Point2を整数で掛けます。
    /// </summary>
    /// <param name="point"></param>
    /// <param name="num"></param>
    /// <returns></returns>
    public static Point2 operator *(Point2 point, int num)
        => new(point.X * num, point.Y * num);

    /// <summary>
    /// Point2を整数で割ります。
    /// </summary>
    /// <param name="point"></param>
    /// <param name="num"></param>
    /// <returns></returns>
    public static Point2 operator /(Point2 point, int num)
        => new(point.X / num, point.Y / num);

    /// <summary>
    /// Point2同士を掛けます。
    /// </summary>
    /// <param name="point1"></param>
    /// <param name="point2"></param>
    /// <returns></returns>
    public static Point2 operator *(Point2 point1, Point2 point2)
        => new(point1.X * point2.X, point1.Y * point2.Y);

    /// <summary>
    /// Point2同士を割ります。
    /// </summary>
    /// <param name="point1"></param>
    /// <param name="point2"></param>
    /// <returns></returns>
    public static Point2 operator /(Point2 point1, Point2 point2)
        => new(point1.X / point2.X, point1.Y / point2.Y);

    /// <summary>
    /// 2つのPoint2が同じ値かどうかを判断します。
    /// </summary>
    /// <param name="point1"></param>
    /// <param name="point2"></param>
    /// <returns></returns>
    public static bool operator ==(Point2 point1, Point2 point2)
        => point1.X == point2.X && point1.Y == point2.Y;

    /// <summary>
    /// 2つのPoint2が違う値かどうかを判断します。
    /// </summary>
    /// <param name="point1"></param>
    /// <param name="point2"></param>
    /// <returns></returns>
    public static bool operator !=(Point2 point1, Point2 point2)
        => point1.X != point2.X || point1.Y != point2.Y;

    /// <summary>
    /// Eq
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object? obj)
        => base.Equals(obj);

    /// <summary>
    /// ハッシュコードを取得します。
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode()
        => base.GetHashCode();
}
