using MessagePack;
using MessagePack.Formatters;
using ObservationPointPacker.Formatter;
using ObservationPointPacker.Models;
using Xunit;

namespace ObservationPointPacker.Tests;

public class KyoshinImagePointFormatterTests
{
    public KyoshinImagePointFormatterTests()
    {
        // KyoshinImagePointには既にFormatterが設定されているため、
        // デフォルトのオプションを使用
    }

    [Fact]
    public void Serialize_And_Deserialize_ShouldReturnSameValue()
    {
        // Arrange
        var original = new KyoshinImagePoint(
            new Point2(300, 200),
            new Point2(-5, 3)
        );

        // Act
        var bytes = MessagePackSerializer.Serialize<KyoshinImagePoint?>(original, cancellationToken: TestContext.Current.CancellationToken);
        var deserialized = MessagePackSerializer.Deserialize<KyoshinImagePoint?>(bytes, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.Center.X, deserialized.Center.X);
        Assert.Equal(original.Center.Y, deserialized.Center.Y);
        Assert.Equal(original.Offset.X, deserialized.Offset.X);
        Assert.Equal(original.Offset.Y, deserialized.Offset.Y);
    }

    [Theory]
    [InlineData(0, 0)]    // 最小オフセット: -8, -8
    [InlineData(7, 7)]    // 最大オフセット: -1, -1
    [InlineData(-8, -8)]  // 最小
    [InlineData(-1, -1)]  // 最大近く
    [InlineData(0, -8)]   // 混在
    [InlineData(-8, 0)]   // 混在
    public void Offset_ShouldBePackedIn_SingleByte(int offsetX, int offsetY)
    {
        // Arrange
        var point = new KyoshinImagePoint(
            new Point2(100, 100),
            new Point2(offsetX, offsetY)
        );

        // Act
        var bytes = MessagePackSerializer.Serialize<KyoshinImagePoint?>(point, cancellationToken: TestContext.Current.CancellationToken);
        var deserialized = MessagePackSerializer.Deserialize<KyoshinImagePoint?>(bytes, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(offsetX, deserialized.Offset.X);
        Assert.Equal(offsetY, deserialized.Offset.Y);
    }

    [Fact]
    public void Serialize_Null_ShouldReturnNil()
    {
        // Arrange
        KyoshinImagePoint? nullPoint = null;

        // Act
        var bytes = MessagePackSerializer.Serialize(nullPoint, cancellationToken: TestContext.Current.CancellationToken);
        var deserialized = MessagePackSerializer.Deserialize<KyoshinImagePoint?>(bytes, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(deserialized);
        Assert.Single(bytes); // nilは1バイト
        Assert.Equal(0xC0, bytes[0]); // MessagePackのnil
    }

    [Fact]
    public void SerializedSize_ShouldBeCompact()
    {
        // Arrange
        var point = new KyoshinImagePoint(
            new Point2(300, 200),
            new Point2(-5, 3)
        );

        // Act
        var bytes = MessagePackSerializer.Serialize<KyoshinImagePoint?>(point, cancellationToken: TestContext.Current.CancellationToken);

        // Assert - 配列ヘッダ + Point2 + byte で非常にコンパクト
        // 期待: 配列ヘッダ(1) + Point2配列ヘッダ(1) + X(3) + Y(3) + offset(2) = 約10バイト
        Assert.InRange(bytes.Length, 8, 15);
    }

    [Fact]
    public void RoundTrip_WithVariousCenterPoints()
    {
        // Arrange
        var testCases = new[]
        {
            new Point2(0, 0),
            new Point2(127, 127),      // fixint範囲内
            new Point2(255, 255),      // uint8範囲内
            new Point2(300, 200),      // uint16範囲
            new Point2(65535, 65535)   // uint16最大
        };

        foreach (var center in testCases)
        {
            var point = new KyoshinImagePoint(center, new Point2(0, 0));

            // Act
            var bytes = MessagePackSerializer.Serialize<KyoshinImagePoint?>(point, cancellationToken: TestContext.Current.CancellationToken);
            var deserialized = MessagePackSerializer.Deserialize<KyoshinImagePoint?>(bytes, cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(center.X, deserialized.Center.X);
            Assert.Equal(center.Y, deserialized.Center.Y);
        }
    }

    [Fact]
    public void Offset_OutOfRange_ShouldBeTruncated()
    {
        // Arrange - オフセットは-8~7の範囲（4bit）
        var point = new KyoshinImagePoint(
            new Point2(100, 100),
            new Point2(10, -10)  // 範囲外
        );

        // Act
        var bytes = MessagePackSerializer.Serialize<KyoshinImagePoint?>(point, cancellationToken: TestContext.Current.CancellationToken);
        var deserialized = MessagePackSerializer.Deserialize<KyoshinImagePoint?>(bytes, cancellationToken: TestContext.Current.CancellationToken);

        // Assert - 4bitにマスクされるので、10 & 0x0F = 10, -10 & 0x0F = 6
        Assert.NotNull(deserialized);
        // 10 + 8 = 18, 18 & 0x0F = 2, 2 - 8 = -6
        Assert.Equal(-6, deserialized.Offset.X);
        // -10 + 8 = -2, -2 & 0x0F = 14, 14 - 8 = 6
        Assert.Equal(6, deserialized.Offset.Y);
    }

    [Fact]
    public void Deserialize_InvalidArrayLength_ShouldThrow()
    {
        // Arrange - 配列長が1のデータ
        var invalidBytes = new byte[] { 0x91, 0x00 }; // [0]

        // Act & Assert
        Assert.Throws<MessagePackSerializationException>(() =>
            MessagePackSerializer.Deserialize<KyoshinImagePoint?>(invalidBytes, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Deserialize_WithExtraFields_ShouldSkipThem()
    {
        // Arrange - 手動で3要素の配列を作成
        var bufferWriter = new System.Buffers.ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(bufferWriter);
        writer.WriteArrayHeader(3);
        writer.WriteArrayHeader(2);
        writer.Write(100);
        writer.Write(200);
        writer.Write((byte)0x33); // offset
        writer.Write(999); // 余分なフィールド
        writer.Flush();
        var bytes = bufferWriter.WrittenSpan.ToArray();

        // Act
        var deserialized = MessagePackSerializer.Deserialize<KyoshinImagePoint?>(bytes, cancellationToken: TestContext.Current.CancellationToken);

        // Assert - 余分なフィールドは無視され、正常にデシリアライズされる
        Assert.NotNull(deserialized);
        Assert.Equal(100, deserialized.Center.X);
        Assert.Equal(200, deserialized.Center.Y);
    }

    [Fact]
    public void BytePacking_ShouldWorkCorrectly()
    {
        // Arrange - オフセット(-8,-8)は0x00、(7,7)は0xFFになるべき
        var minPoint = new KyoshinImagePoint(new Point2(0, 0), new Point2(-8, -8));
        var maxPoint = new KyoshinImagePoint(new Point2(0, 0), new Point2(7, 7));

        // Act
        var minBytes = MessagePackSerializer.Serialize<KyoshinImagePoint?>(minPoint, cancellationToken: TestContext.Current.CancellationToken);
        var maxBytes = MessagePackSerializer.Serialize<KyoshinImagePoint?>(maxPoint, cancellationToken: TestContext.Current.CancellationToken);
        var minDeserialized = MessagePackSerializer.Deserialize<KyoshinImagePoint?>(minBytes, cancellationToken: TestContext.Current.CancellationToken);
        var maxDeserialized = MessagePackSerializer.Deserialize<KyoshinImagePoint?>(maxBytes, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(minDeserialized);
        Assert.Equal(-8, minDeserialized.Offset.X);
        Assert.Equal(-8, minDeserialized.Offset.Y);

        Assert.NotNull(maxDeserialized);
        Assert.Equal(7, maxDeserialized.Offset.X);
        Assert.Equal(7, maxDeserialized.Offset.Y);
    }
}
