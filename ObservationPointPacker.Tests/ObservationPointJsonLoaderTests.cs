using System.Text;
using ObservationPointPacker.Models;
using Xunit;

namespace ObservationPointPacker.Tests;

public class ObservationPointJsonLoaderTests
{
    private const string SampleJson = """
        [
          {
            "type": "kiK_net",
            "code": "ABSH01",
            "name": "雄武",
            "region": "北海道",
            "sub_region": "紋別地方",
            "is_suspended": false,
            "location": { "latitude": 44.5276, "longitude": 142.8444 },
            "japanese_coordinate_system_location": { "latitude": 44.5253, "longitude": 142.8483 },
            "point": { "center_point": { "x": 289, "y": 42 }, "offset": { "x": 1, "y": -1 } }
          }
        ]
        """;

    /// <summary>
    /// 一時ファイルに書き出して読み込みます。
    /// </summary>
    private static async Task<CommonObservationPoint[]> LoadFromTextAsync(string text, Encoding encoding)
    {
        var path = Path.Combine(Path.GetTempPath(), $"observation-points-{Guid.NewGuid()}.json");
        try
        {
            await File.WriteAllTextAsync(path, text, encoding, TestContext.Current.CancellationToken);
            return await ObservationPointJsonLoader.LoadAsync(path, TestContext.Current.CancellationToken);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData(true)]  // intensity-points.json は BOM 付きで保存されている
    [InlineData(false)]
    public async Task LoadAsync_ShouldRead_RegardlessOfBom(bool withBom)
    {
        // Act
        var points = await LoadFromTextAsync(SampleJson, new UTF8Encoding(withBom));

        // Assert
        var point = Assert.Single(points);
        Assert.Equal(ObservationPointType.KiK_net, point.Type);
        Assert.Equal("ABSH01", point.Code);
        Assert.Equal("雄武", point.Name);
        Assert.Equal("北海道", point.Region);
        Assert.Equal("紋別地方", point.SubRegion);
        Assert.False(point.IsSuspended);
        Assert.Equal(44.5276f, point.Location.Latitude);
        Assert.Equal(142.8444f, point.Location.Longitude);
        Assert.NotNull(point.OldLocation);
        Assert.Equal(44.5253f, point.OldLocation.Latitude);
        Assert.NotNull(point.Point);
        Assert.Equal(new Point2(289, 42), point.Point.Center);
        Assert.Equal(new Point2(1, -1), point.Point.Offset);
    }

    [Fact]
    public async Task LoadAsync_WithEmptyArray_ShouldReturnEmpty()
    {
        // Act
        var points = await LoadFromTextAsync("[]", new UTF8Encoding(false));

        // Assert
        Assert.Empty(points);
    }

    [Fact]
    public async Task LoadAsync_WithMissingFile_ShouldReturnEmpty()
    {
        // Arrange (差分の比較元にファイルが存在しない場合がある)
        var path = Path.Combine(Path.GetTempPath(), $"observation-points-{Guid.NewGuid()}.json");

        // Act
        var points = await ObservationPointJsonLoader.LoadAsync(path, TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(points);
    }
}
