# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This repository shares observation point data from the Kyoshin Monitor (強震モニタ), a seismic monitoring system in Japan that tracks K-NET and KiK-net observation stations.

The primary asset is [intensity-points.json](intensity-points.json), which contains the canonical observation point data. The ObservationPointPacker is a utility used during release preparation to convert this JSON data into compressed MessagePack format (.mpk) with optional LZ4 compression for distribution.

**Language**: Communicate with users in Japanese. Source code comments should also be written in Japanese.

## Build and Test Commands

```bash
# Build the solution
dotnet build

# Run the main application
dotnet run --project ObservationPointPacker/ObservationPointPacker.csproj

# Run all tests
dotnet test

# Run a specific test
dotnet test --filter "FullyQualifiedName~KyoshinImagePointFormatterTests.Serialize_And_Deserialize_ShouldReturnSameValue"

# Run tests in a specific test class
dotnet test --filter "FullyQualifiedName~KyoshinImagePointFormatterTests"
```

## Repository Structure

- [intensity-points.json](intensity-points.json): The canonical source of observation point data
- [ObservationPointPacker/](ObservationPointPacker/): Release preparation tool for converting JSON to compressed formats
- [ObservationPointPacker.Tests/](ObservationPointPacker.Tests/): Tests for the packer utility

## Packer Architecture

The ObservationPointPacker is used during releases to package observation data.

### Data Flow

1. **Input**: [intensity-points.json](intensity-points.json) containing `CommonObservationPoint[]` data
2. **Processing**: Data is converted between different versioned formats (V1, V2)
3. **Output**: Serialized to MessagePack (.mpk), JSON, or CSV formats via `ObservationPointsFile<T>`

### Key Architectural Patterns

**Custom MessagePack Formatters for Compression:**

The project uses custom MessagePack formatters to minimize serialized data size:

- [KyoshinImagePointFormatter.cs](ObservationPointPacker/Formatter/KyoshinImagePointFormatter.cs): Packs X/Y offsets (range -8 to 7) into a single byte using 4-bit nibbles
- [LocationFormatter.cs](ObservationPointPacker/Formatter/LocationFormatter.cs): Stores latitude/longitude as int32 (multiplied by 1000) instead of float, saving space while maintaining ~100m precision

**Version-Agnostic Serialization:**

The `ISerializableObservationPoint<T>` interface defines CSV serialization contracts, allowing different observation point versions to implement their own CSV format while sharing the same file container logic.

**Type Hierarchy:**

- `CommonObservationPoint`: Intermediate format with both old coordinate system support and V1/V2 conversion methods
- `ObservationPointV1`: Legacy format with merged center+offset point
- `ObservationPointV2`: Current format with separated `KyoshinImagePoint` (center + offset) and network type field

### Critical Implementation Details

**Offset Packing in KyoshinImagePoint:**
- The offset range is constrained to -8 to 7 for both X and Y
- Packing formula: `((offsetX + 8) & 0x0F) << 4 | ((offsetY + 8) & 0x0F)`
- Out-of-range values will be truncated to fit 4 bits
- See [KyoshinImagePointFormatterTests.cs](ObservationPointPacker.Tests/KyoshinImagePointFormatterTests.cs) for validation

**Future-Proof Deserialization:**
Both custom formatters skip extra array elements during deserialization to support forward compatibility if fields are added later.

## Testing

The project uses xUnit v3 with Visual Studio test runner. Tests are in [ObservationPointPacker.Tests/](ObservationPointPacker.Tests/).

Key test characteristics:
- Uses `TestContext.Current.CancellationToken` for all async MessagePack operations
- Tests validate both serialization correctness and binary size constraints
- Includes edge case testing for offset range boundaries and invalid data
