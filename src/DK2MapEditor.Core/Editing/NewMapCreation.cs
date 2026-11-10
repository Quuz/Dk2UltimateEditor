using System.Buffers.Binary;
using System.Globalization;
using DK2MapEditor.Core.Formats;

namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    /// <summary>Create a new independent document. No file is written and no
    /// source definitions are modified. Blank maps intentionally have no hearts.</summary>
    public static NativeMapDocument CreateNewMap(NewMapOptions options, NativeMapDocument? definitionsFrom = null)
    {
        var problem = options.Validate();
        if (problem != NewMapProblem.None) throw new ArgumentException("Invalid new-map setting: " + problem, nameof(options));
        var project = CreateNewProject(options with { NativeFormat = false, DisplayName = options.NativeFormat ? options.FileName : options.DisplayName });
        if (!options.NativeFormat) return project;
        var basis = definitionsFrom is { IsLegacyProject: false } ? definitionsFrom : Create(options.Width, options.Height, options.FileName);
        // A native blank document still exposes all requested keeper defaults.
        // This factory-only path is distinct from the original compiler's rule
        // that only keepers with hearts become active game player records.
        var native = project.CompileLegacyProject(options.FileName, basis, includeDeclaredKeepers: true);
        var name = LegacySchemas.Field("GAMEINFO", "wstrMainName");
        name.Encode(options.DisplayName).CopyTo(native.MainFile.Bytes, name.Offset);
        native.ResetHistory();
        native.InitializeActivityStatistics(newMap: true);
        return native;
    }

    private static NativeMapDocument CreateNewProject(NewMapOptions options)
    {
        byte[] Buffer(string structure) => new byte[LegacyProjectSchemas.Size(structure)];
        void Set(byte[] bytes, string structure, string name, object value)
        {
            var field = LegacyProjectSchemas.Fields(structure).Single(field => field.Name == name);
            var text = Convert.ToString(value, CultureInfo.InvariantCulture)!;
            // Original BYTE arrays use -1 as the 255 "not configured" sentinel.
            if (field.Kind == NativeValueKind.Byte && text == "-1") text = "255";
            field.Encode(text).CopyTo(bytes, field.Offset);
        }
        var info = Buffer("MAPPROPERTIES");
        void Info(string name, object value) => Set(info, "MAPPROPERTIES", name, value);
        Info("Width", options.Width); Info("Height", options.Height); Info("Name", options.DisplayName);
        Info("Players", options.Players); Info("Version", options.Version); Info("FillID", unchecked((int)options.FillID));
        Info("Options", 4); Info("Soundtrack", 1); Info("GameTicks", 4);
        // Properties_MapResetOptions does NOT set Options2 to the ASCII
        // importer's value 12, and does not copy the previous map's options.
        for (var index = 0; index < 7; index++) if (index != 1) { Info($"Allies[1][{index}]", 1); Info($"Allies[{index}][1]", 1); }
        foreach (var item in LegacyCompilerRules.Elements("MapVars").Where(item => item.ID < 256)) Info($"Variables[{item.ID}]", unchecked((int)item.Variable));
        var playerBytes = new byte[7 * LegacyProjectSchemas.Size("MAPOPTIONS")];
        for (var player = 1; player <= 7; player++)
        {
            var bytes = PlayerSettings.Defaults((uint)player);
            bytes.CopyTo(playerBytes, (player - 1) * bytes.Length);
        }
        var tiles = new byte[8 + options.Width * options.Height * 4];
        BinaryPrimitives.WriteInt32LittleEndian(tiles, options.Width); BinaryPrimitives.WriteInt32LittleEndian(tiles.AsSpan(4), options.Height);
        var fill = EditorTerrainRules.DecodeFillPreset(options.FillID);
        void Tile(int x, int y, MapTile tile) => BinaryPrimitives.WriteUInt32LittleEndian(tiles.AsSpan(8 + (y * options.Width + x) * 4), tile.Packed);
        for (var y = 0; y < options.Height; y++) for (var x = 0; x < options.Width; x++)
            Tile(x, y, x == 0 || y == 0 || x == options.Width - 1 || y == options.Height - 1 ? new(30, 2, 1) : fill);
        if (options.PlaceHearts)
        {
            var columns = (options.Width - 1) / 6;
            for (var keeper = 0; keeper < options.Players; keeper++)
                for (var y = 0; y < 5; y++) for (var x = 0; x < 5; x++)
                    Tile(1 + keeper % columns * 6 + x, 1 + keeper / columns * 6 + y, new(14, (byte)(keeper + 3), 1));
        }
        var legacy = LegacySectionFile.Create(new[]
        {
            new LegacySection(LegacySectionFile.TileSectionSignature, 1, tiles),
            new LegacySection(LegacyInfo, 7, info), new LegacySection(LegacyPlayers, 3, playerBytes),
            new LegacySection(LegacyThings, 6, ReadOnlyMemory<byte>.Empty), new LegacySection(LegacyAreas, 1, ReadOnlyMemory<byte>.Empty),
            new LegacySection(LegacyBands, 5, ReadOnlyMemory<byte>.Empty), new LegacySection(LegacyEvents, 3, new byte[12 + 146 * 64])
        });
        var project = FromLegacyProject(options.FileName + ".dkm", legacy);
        project.InitializeActivityStatistics(newMap: true);
        project.originals[project.MainFile] = []; // An unsaved new document.
        return project;
    }
}
