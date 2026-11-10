using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public sealed record PlayerGeometry(uint ID, NativeRecord Record, TilePoint Camera, TilePoint BuildStart, TilePoint BuildEnd,
    bool DefaultCamera, bool DefaultBuildArea, TilePoint EffectiveCamera, TilePoint EffectiveBuildStart, TilePoint EffectiveBuildEnd,
    bool CameraEditable, bool BuildAreaEditable);

public sealed partial class NativeMapDocument
{
    private static string PlayerCoordinateField(NativeRecord record, bool project, string coordinate) => !project
        ? "bPlayer" + (coordinate.StartsWith("Camera", StringComparison.Ordinal) ? coordinate : "BuildArea" + coordinate[7..])
        : record.Category == "Project.MAPOPTIONSV1" ? coordinate : "Player" + coordinate;

    /// <summary>Read-only coordinates, including malformed imported values. Project sentinels
    /// are not native sentinels: the compiler resolves them to heart/build-area coordinates.</summary>
    public IReadOnlyList<PlayerGeometry> PlayerGeometries()
    {
        var players = PlayerSettingsRows();
        return players.Select(row =>
        {
            var record = row.Record;
            var valid = row.ID is >= 1 and <= 7 && players.Count(other => other.ID == row.ID) == 1;
            var verified = IsLegacyProject ? record.Category is "Project.MAPOPTIONS" or "Project.MAPOPTIONSV2" or "Project.MAPOPTIONSV1"
                : record.Length == 205 && record.File.UInt32(4) == 4;
            bool Has(string name) => record.Fields.Any(field => field.Name == PlayerCoordinateField(record, IsLegacyProject, name));
            int Read(string name) => int.Parse(record.Read(record.Fields.Single(field => field.Name == PlayerCoordinateField(record, IsLegacyProject, name))), CultureInfo.InvariantCulture);
            // Both old project readers discard camera fields; exposing V2's ignored bytes
            // as a functional camera edit would promise a setting the game never receives.
            var cameraSupported = !IsLegacyProject || record.Category == "Project.MAPOPTIONS";
            var camera = cameraSupported && Has("CameraX") && Has("CameraY") ? new TilePoint(Read("CameraX"), Read("CameraY")) : new(-1, -1);
            var areaSupported = new[] { "CPUAreaX1", "CPUAreaY1", "CPUAreaX2", "CPUAreaY2" }.All(Has);
            var start = areaSupported ? new TilePoint(Read("CPUAreaX1"), Read("CPUAreaY1")) : new(-1, -1);
            var end = areaSupported ? new TilePoint(Read("CPUAreaX2"), Read("CPUAreaY2")) : new(-1, -1);
            var defaultCamera = IsLegacyProject && camera.X == -1;
            var defaultArea = IsLegacyProject && start.X < 0;
            return new PlayerGeometry(row.ID, record, camera, start, end, defaultCamera, defaultArea,
                IsLegacyProject && row.ID <= 2 ? new(0, 0) : defaultCamera ? HeartPosition((byte)row.ID) : camera,
                defaultArea ? new(2, 2) : start, defaultArea ? new(Width - 2, Height - 2) : end,
                valid && row.ID >= 3 && verified && cameraSupported && Has("CameraX") && Has("CameraY"), valid && verified && areaSupported);
        }).ToArray();
    }

    private PlayerGeometry SelectedPlayerGeometry(uint id, bool camera, ReadOnlyMemory<byte>? expectedPayload)
    {
        var matches = PlayerGeometries().Where(row => row.ID == id).ToArray();
        if (matches.Length != 1 || !(camera ? matches[0].CameraEditable : matches[0].BuildAreaEditable))
            throw new ArgumentException("Select a verified unambiguous player geometry (upgrade ignored camera fields first).", nameof(id));
        var row = matches[0];
        if (expectedPayload is { } expected && !row.Record.File.Data.Span.Slice(row.Record.Offset, row.Record.Length).SequenceEqual(expected.Span))
            throw new InvalidOperationException("The player changed while its coordinates were being chosen.");
        return row;
    }
    private void WritePlayerCoordinate(NativeRecord record, string name, int value)
        => WritePlayer(record, PlayerCoordinateField(record, IsLegacyProject, name), value.ToString(CultureInfo.InvariantCulture));

    public void SetPlayerCamera(uint id, TilePoint? position, ReadOnlyMemory<byte>? expectedPayload = null)
    {
        var row = SelectedPlayerGeometry(id, true, expectedPayload);
        var point = position ?? (IsLegacyProject ? new(-1, -1) : id <= 2 ? new(0, 0) : HeartPosition((byte)id));
        if (position is not null && (point.X < 0 || point.Y < 0 || point.X >= Width || point.Y >= Height)) throw new ArgumentOutOfRangeException(nameof(position));
        Edit("Change player starting camera", () =>
        { WritePlayerCoordinate(row.Record, "CameraX", point.X); WritePlayerCoordinate(row.Record, "CameraY", point.Y); });
    }
    /// <summary>Endpoints are inclusive. Null restores the source default: project -1
    /// sentinels, or resolved native (2,2)..(width-2,height-2), never 65535.</summary>
    public void SetPlayerBuildArea(uint id, TileRect? bounds, ReadOnlyMemory<byte>? expectedPayload = null)
    {
        var row = SelectedPlayerGeometry(id, false, expectedPayload);
        var rectangle = bounds ?? new TileRect(2, 2, Width - 3, Height - 3);
        if (bounds is not null && (rectangle.X < 0 || rectangle.Y < 0 || rectangle.Width < 1 || rectangle.Height < 1
            || (long)rectangle.X + rectangle.Width > Width || (long)rectangle.Y + rectangle.Height > Height)) throw new ArgumentOutOfRangeException(nameof(bounds));
        var start = bounds is null && IsLegacyProject ? new TilePoint(-1, -1) : new(rectangle.X, rectangle.Y);
        var end = bounds is null && IsLegacyProject ? new TilePoint(-1, -1) : new(rectangle.X + rectangle.Width - 1, rectangle.Y + rectangle.Height - 1);
        // Source defaults are retained even on extended tiny maps, where their
        // resolved endpoints may be inverted. Do not clamp/import new defaults.
        // Preflight native encoding too, before a width-1 default can write -1.
        foreach (var (name, value) in new[] { ("CPUAreaX1", start.X), ("CPUAreaY1", start.Y), ("CPUAreaX2", end.X), ("CPUAreaY2", end.Y) })
            row.Record.Fields.Single(field => field.Name == PlayerCoordinateField(row.Record, IsLegacyProject, name)).Encode(value.ToString(CultureInfo.InvariantCulture));
        Edit("Change player AI build area", () =>
        {
            WritePlayerCoordinate(row.Record, "CPUAreaX1", start.X); WritePlayerCoordinate(row.Record, "CPUAreaY1", start.Y);
            WritePlayerCoordinate(row.Record, "CPUAreaX2", end.X); WritePlayerCoordinate(row.Record, "CPUAreaY2", end.Y);
        });
    }
}
