namespace DK2MapEditor.Core.Editing;

public enum NewMapProblem { None, FileName, DisplayName, Dimensions, Players, Version, Fill, StarterSpace }

/// <summary>Original new-map settings plus explicit modern format/starter options.
/// File names and display text are separate; historical projects use Windows-1252.</summary>
public sealed record NewMapOptions(string FileName = "NewDungeon", string DisplayName = "NewDungeon",
    int Width = 80, int Height = 80, int Players = 1, byte Version = 17, uint FillID = 0x02020101,
    bool PlaceHearts = false, bool NativeFormat = false)
{
    public NewMapProblem Validate()
    {
        if (string.IsNullOrWhiteSpace(FileName) || FileName.Length > 32 || FileName.Any(character => character > 127 || Path.GetInvalidFileNameChars().Contains(character))
            || FileName.Contains('\\') || FileName.Contains('/') || FileName is "." or "..") return NewMapProblem.FileName;
        var device = FileName.Split('.')[0].ToUpperInvariant();
        if (device is "CON" or "PRN" or "AUX" or "NUL" || device.Length == 4 && (device.StartsWith("COM", StringComparison.Ordinal) || device.StartsWith("LPT", StringComparison.Ordinal)) && device[3] is >= '1' and <= '9') return NewMapProblem.FileName;
        if (string.IsNullOrWhiteSpace(DisplayName)) return NewMapProblem.DisplayName;
        try
        {
            var field = NativeFormat ? LegacySchemas.Field("GAMEINFO", "wstrMainName") : LegacyProjectSchemas.Fields("MAPPROPERTIES").Single(field => field.Name == "Name");
            _ = field.Encode(DisplayName);
        }
        catch (ArgumentException) { return NewMapProblem.DisplayName; }
        if (Width is < 7 or > 1024 || Height is < 7 or > 1024) return NewMapProblem.Dimensions;
        if (Players is < 1 or > 5) return NewMapProblem.Players;
        if (Version is not (13 or 16 or 17)) return NewMapProblem.Version;
        if (!LegacyCompilerRules.Elements("NewMap").Any(entry => entry.ID == FillID)) return NewMapProblem.Fill;
        if (PlaceHearts && (Width - 1) / 6 * ((Height - 1) / 6) < Players) return NewMapProblem.StarterSpace;
        return NewMapProblem.None;
    }

    public bool NeedsLargeMapWarning => Width > 300 || Height > 300 || (long)Width * Height > 120 * 120;

    public static NewMapOptions ForDocument(NativeMapDocument? document)
    {
        if (document is null) return new();
        var records = document.Records();
        var info = records.FirstOrDefault(record => record.File.Signature == NativeMapDocument.LegacyInfo);
        string? Read(string name) => info?.Fields.FirstOrDefault(field => field.Name == name) is { } field ? info.Read(field) : null;
        var nativeInfo = records.FirstOrDefault(record => record.Category == "Map");
        var displayName = Read("Name") ?? (nativeInfo?.Fields.FirstOrDefault(field => field.Name == "wstrMainName") is { } nativeName ? nativeInfo.Read(nativeName) : "NewDungeon");
        var playerCount = int.TryParse(Read("Players"), out var declared) ? declared : records.Count(record => record.Category == "GAMEPLAYER" && byte.Parse(record.Read(record.Fields.Single(field => field.Name == "bGlobalPlayer"))) >= 3);
        var fill = int.TryParse(Read("FillID"), out var packed) ? unchecked((uint)packed) : 0x02020101;
        if (!LegacyCompilerRules.Elements("NewMap").Any(entry => entry.ID == fill)) fill = 0x02020101;
        return new(DisplayName: displayName, Width: Math.Max(7, document.Width), Height: Math.Max(7, document.Height),
            Players: Math.Clamp(playerCount, 1, 5), Version: (byte)ValueCatalogues.GameVersion(document), FillID: fill, NativeFormat: !document.IsLegacyProject);
    }
}
