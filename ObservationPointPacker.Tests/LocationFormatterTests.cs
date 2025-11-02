using MessagePack;
using MessagePack.Formatters;
using ObservationPointPacker.Formatter;
using ObservationPointPacker.Models;
using Xunit;

namespace ObservationPointPacker.Tests;

// テスト用のクラス：実際の使用ケースを想定
[MessagePackObject]
public class TestObservationPoint
{
    [Key(0)]
    public string? Name { get; set; }

    [Key(1)]
    public Location? Location { get; set; }
}

// LocationFormatterを使わない通常のクラス
[MessagePackObject]
public class StandardObservationPoint
{
    [Key(0)]
    public string? Name { get; set; }

    [Key(1)]
    public OldFormatterLocation? Location { get; set; }
}

public class LocationFormatterTests
{

    [Fact]
    public void Serialize_And_Deserialize_ShouldReturnSameValue()
    {
        // Arrange
        var original = new TestObservationPoint
        {
            Name = "東京",
            Location = new Location(35.681236f, 139.767125f)
        };

        // Act
        var bytes = MessagePackSerializer.Serialize(original, cancellationToken: TestContext.Current.CancellationToken);
        var deserialized = MessagePackSerializer.Deserialize<TestObservationPoint>(bytes, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(deserialized.Location);
        Assert.Equal(original.Location.Latitude, deserialized.Location.Latitude, precision: 3);
        Assert.Equal(original.Location.Longitude, deserialized.Location.Longitude, precision: 3);
    }

    [Fact]
    public void Serialize_ShouldReduceSize_ComparedToStandard()
    {
        // Arrange
        var location = new Location(35.681236f, 139.767125f);
        var withFormatter = new TestObservationPoint { Name = "東京", Location = location };
        var withoutFormatter = new StandardObservationPoint { Name = "東京", Location = new(location.Latitude, location.Longitude) };

        // Act
        var standardBytes = MessagePackSerializer.Serialize(withoutFormatter, cancellationToken: TestContext.Current.CancellationToken);
        var compactBytes = MessagePackSerializer.Serialize(withFormatter, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.True(compactBytes.Length < standardBytes.Length,
            $"Compact: {compactBytes.Length} bytes, Standard: {standardBytes.Length} bytes");
    }

    [Theory]
    [InlineData(35.681236f, 139.767125f)]  // 東京
    [InlineData(43.064167f, 141.346944f)]  // 札幌
    [InlineData(26.2125f, 127.679167f)]    // 那覇
    [InlineData(-90.0f, -180.0f)]          // 最小値
    [InlineData(90.0f, 180.0f)]            // 最大値
    [InlineData(0.0f, 0.0f)]               // ゼロ
    public void RoundTrip_ShouldMaintainPrecision(float latitude, float longitude)
    {
        // Arrange
        var original = new TestObservationPoint
        {
            Name = "Test",
            Location = new Location(latitude, longitude)
        };

        // Act
        var bytes = MessagePackSerializer.Serialize(original, cancellationToken: TestContext.Current.CancellationToken);
        var deserialized = MessagePackSerializer.Deserialize<TestObservationPoint>(bytes, cancellationToken: TestContext.Current.CancellationToken);

        // Assert - 10^4倍なので、0.0001度（約10m）の精度
        Assert.NotNull(deserialized.Location);
        Assert.NotNull(original.Location);
        Assert.Equal(original.Location.Latitude, deserialized.Location.Latitude, precision: 3);
        Assert.Equal(original.Location.Longitude, deserialized.Location.Longitude, precision: 3);
    }

    [Fact]
    public void Precision_ShouldBe_0_001_Degrees()
    {
        // Arrange - 0.0001度未満の差
        var point1 = new TestObservationPoint
        {
            Name = "Test1",
            Location = new Location(35.681236f, 139.767125f)
        };
        var point2 = new TestObservationPoint
        {
            Name = "Test2",
            Location = new Location(35.681237f, 139.767126f)
        };

        // Act
        var bytes1 = MessagePackSerializer.Serialize(point1, cancellationToken: TestContext.Current.CancellationToken);
        var bytes2 = MessagePackSerializer.Serialize(point2, cancellationToken: TestContext.Current.CancellationToken);
        var deserialized1 = MessagePackSerializer.Deserialize<TestObservationPoint>(bytes1, cancellationToken: TestContext.Current.CancellationToken);
        var deserialized2 = MessagePackSerializer.Deserialize<TestObservationPoint>(bytes2, cancellationToken: TestContext.Current.CancellationToken);

        // Assert - 0.0001度の精度なので、小数点以下5桁目は失われる
        Assert.NotNull(deserialized1.Location);
        Assert.NotNull(deserialized2.Location);
        Assert.Equal(deserialized1.Location.Latitude, deserialized2.Location.Latitude, precision: 4);
        Assert.Equal(deserialized1.Location.Longitude, deserialized2.Location.Longitude, precision: 4);
    }

    [Fact]
    public void Serialize_NullLocation_ShouldWork()
    {
        // Arrange
        var point = new TestObservationPoint
        {
            Name = "Test",
            Location = null
        };

        // Act
        var bytes = MessagePackSerializer.Serialize(point, cancellationToken: TestContext.Current.CancellationToken);
        var deserialized = MessagePackSerializer.Deserialize<TestObservationPoint>(bytes, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(deserialized.Location);
    }

    [Fact]
    public void Deserialize_InvalidData_ShouldThrow()
    {
        // Arrange - 配列長が1のデータ
        var invalidBytes = new byte[] { 0x91, 0x00 }; // [0]

        // Act & Assert
        Assert.ThrowsAny<Exception>(() =>
            MessagePackSerializer.Deserialize<TestObservationPoint>(invalidBytes, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Deserialize_CorruptedData_ShouldThrow()
    {
        // Arrange - 完全に壊れたデータ
        var corruptedBytes = new byte[] { 0xFF, 0xFF, 0xFF };

        // Act & Assert
        Assert.ThrowsAny<Exception>(() =>
            MessagePackSerializer.Deserialize<TestObservationPoint>(corruptedBytes, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public void SerializedSize_ShouldBeReduced()
    {
        // Arrange
        var withFormatter = new TestObservationPoint
        {
            Name = "Test",
            Location = new(35.681236f, 139.767125f)
        };
        var withoutFormatter = new StandardObservationPoint
        {
            Name = "Test",
            Location = new(35.681236f, 139.767125f)
        };

        // Act
        var compactBytes = MessagePackSerializer.Serialize(withFormatter, cancellationToken: TestContext.Current.CancellationToken);
        var standardBytes = MessagePackSerializer.Serialize(withoutFormatter, cancellationToken: TestContext.Current.CancellationToken);

        // Assert - カスタムフォーマッターを使った方が小さい
        Assert.True(compactBytes.Length < standardBytes.Length,
            $"Compact: {compactBytes.Length} bytes should be less than Standard: {standardBytes.Length} bytes");
    }
}
