using ObservationPointPacker.Diff;
using ObservationPointPacker.Models;
using Xunit;

namespace ObservationPointPacker.Tests;

public class ObservationPointDifferTests
{
    /// <summary>
    /// テスト用の観測点を作成します。
    /// </summary>
    internal static CommonObservationPoint CreatePoint(
        string code,
        string name = "テスト観測点",
        ObservationPointType type = ObservationPointType.K_NET,
        string region = "東京都",
        string? subRegion = "東京都23区",
        bool isSuspended = false,
        float latitude = 35f,
        float longitude = 139f,
        KyoshinImagePoint? point = null)
        => new()
        {
            Code = code,
            Name = name,
            Type = type,
            Region = region,
            SubRegion = subRegion,
            IsSuspended = isSuspended,
            Location = new(latitude, longitude),
            Point = point ?? new(new(100, 100), new(0, 0)),
        };

    [Fact]
    public void Compare_ShouldDetect_AddedRemovedAndChanged()
    {
        // Arrange
        CommonObservationPoint[] before = [CreatePoint("AAA001"), CreatePoint("AAA002"), CreatePoint("AAA003")];
        CommonObservationPoint[] after = [CreatePoint("AAA001"), CreatePoint("AAA002", name: "変更後"), CreatePoint("AAA004")];

        // Act
        var diff = ObservationPointDiffer.Compare(before, after);

        // Assert
        Assert.True(diff.HasChanges);
        Assert.Equal("AAA004", Assert.Single(diff.Added).Code);
        Assert.Equal("AAA003", Assert.Single(diff.Removed).Code);
        Assert.Equal(3, diff.BeforeCount);
        Assert.Equal(3, diff.AfterCount);

        var changed = Assert.Single(diff.Changed);
        Assert.Equal("AAA002", changed.After.Code);
        var change = Assert.Single(changed.Changes);
        Assert.Equal("観測点名", change.Label);
        Assert.Equal("テスト観測点", change.Before);
        Assert.Equal("変更後", change.After);
    }

    [Fact]
    public void Compare_WithSameData_ShouldHaveNoChanges()
    {
        // Arrange
        CommonObservationPoint[] before = [CreatePoint("AAA001"), CreatePoint("AAA002")];
        CommonObservationPoint[] after = [CreatePoint("AAA001"), CreatePoint("AAA002")];

        // Act
        var diff = ObservationPointDiffer.Compare(before, after);

        // Assert
        Assert.False(diff.HasChanges);
        Assert.Empty(diff.Added);
        Assert.Empty(diff.Removed);
        Assert.Empty(diff.Changed);
    }

    [Fact]
    public void Compare_ShouldDetect_AllFieldChanges()
    {
        // Arrange
        CommonObservationPoint[] before = [CreatePoint("AAA001")];
        var afterPoint = CreatePoint(
            "AAA001",
            name: "変更後",
            type: ObservationPointType.KiK_net,
            region: "神奈川県",
            subRegion: null,
            isSuspended: true,
            latitude: 36f,
            point: new(new(200, 200), new(1, 1)));
        afterPoint.OldLocation = new(35.99f, 139f);

        // Act
        var diff = ObservationPointDiffer.Compare(before, [afterPoint]);

        // Assert
        var changed = Assert.Single(diff.Changed);
        Assert.Equal(
            ["観測点名", "種別", "地域", "詳細地域", "状態", "座標", "座標(日本測地系)", "画像座標"],
            changed.Changes.Select(c => c.Label));
    }

    [Fact]
    public void Compare_ShouldDetect_DuplicatedCode()
    {
        // Arrange
        CommonObservationPoint[] before = [CreatePoint("AAA001")];
        CommonObservationPoint[] after = [CreatePoint("AAA001"), CreatePoint("AAA001", name: "重複した観測点")];

        // Act
        var diff = ObservationPointDiffer.Compare(before, after);

        // Assert
        Assert.Equal("AAA001", Assert.Single(diff.DuplicatedCodes));
        // 重複していても先に見つかったものが比較対象となるため差分にはならない
        Assert.False(diff.HasChanges);
    }

    [Fact]
    public void Compare_WhenLocationMoved_ShouldAnnotateDistance()
    {
        // Arrange (緯度 0.01 度 ≒ 1.1km)
        CommonObservationPoint[] before = [CreatePoint("AAA001", latitude: 35f)];
        CommonObservationPoint[] after = [CreatePoint("AAA001", latitude: 35.01f)];

        // Act
        var diff = ObservationPointDiffer.Compare(before, after);

        // Assert
        var change = Assert.Single(Assert.Single(diff.Changed).Changes);
        Assert.Equal("座標", change.Label);
        Assert.Equal("約 1.1km", change.Note);
    }

    [Fact]
    public void Compare_WhenImagePointMoved_ShouldAnnotateDelta()
    {
        // Arrange
        CommonObservationPoint[] before = [CreatePoint("AAA001", point: new(new(100, 100), new(0, 0)))];
        CommonObservationPoint[] after = [CreatePoint("AAA001", point: new(new(102, 100), new(0, -1)))];

        // Act
        var diff = ObservationPointDiffer.Compare(before, after);

        // Assert
        var change = Assert.Single(Assert.Single(diff.Changed).Changes);
        Assert.Equal("画像座標", change.Label);
        Assert.Equal("(100, 100)", change.Before);
        Assert.Equal("(102, 100) ずれ 0, -1", change.After);
        Assert.Equal("Δ +2, -1", change.Note);
    }

    [Fact]
    public void Compare_WhenOnlyOffsetChanged_ShouldReportSameDrawPosition()
    {
        // Arrange (中心とずれの内訳だけが変わり、描画される位置は同じ)
        CommonObservationPoint[] before = [CreatePoint("AAA001", point: new(new(100, 100), new(0, 0)))];
        CommonObservationPoint[] after = [CreatePoint("AAA001", point: new(new(99, 100), new(1, 0)))];

        // Act
        var diff = ObservationPointDiffer.Compare(before, after);

        // Assert
        var change = Assert.Single(Assert.Single(diff.Changed).Changes);
        Assert.Equal("描画位置は変化なし", change.Note);
    }

    [Fact]
    public void Compare_ShouldSortResults_ByCode()
    {
        // Arrange
        CommonObservationPoint[] before = [CreatePoint("CCC003"), CreatePoint("AAA001")];
        CommonObservationPoint[] after = [CreatePoint("ZZZ009"), CreatePoint("BBB002")];

        // Act
        var diff = ObservationPointDiffer.Compare(before, after);

        // Assert
        Assert.Equal(["BBB002", "ZZZ009"], diff.Added.Select(p => p.Code));
        Assert.Equal(["AAA001", "CCC003"], diff.Removed.Select(p => p.Code));
    }

    [Fact]
    public void Compare_WhenImagePointRemoved_ShouldNotAnnotate()
    {
        // Arrange
        CommonObservationPoint[] before = [CreatePoint("AAA001")];
        var afterPoint = CreatePoint("AAA001");
        afterPoint.Point = null;

        // Act
        var diff = ObservationPointDiffer.Compare(before, [afterPoint]);

        // Assert
        var change = Assert.Single(Assert.Single(diff.Changed).Changes);
        Assert.Equal("画像座標", change.Label);
        Assert.Equal("-", change.After);
        Assert.Null(change.Note);
    }
}
