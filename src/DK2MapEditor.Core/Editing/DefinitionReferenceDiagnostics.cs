using DK2MapEditor.Core.Diagnostics;

namespace DK2MapEditor.Core.Editing;

/// <summary>Read-only notices about IDs outside verified original editor
/// catalogues. These catalogues describe editor choices, not every definition
/// a modified game can load; an unknown ID is never a missing-game-target error.</summary>
public static partial class DefinitionReferenceDiagnostics
{
    private static readonly Lazy<HashSet<string>> creatures = new(() => ValueCatalogues.ForTemplate("Creature").Select(row => row.Value).ToHashSet(StringComparer.Ordinal));
    // The narrower scenario Terrain list omits real boundary/special cells.
    private static readonly Lazy<HashSet<byte>> terrain = new(() => GlobalDefinitionDefaults.Catalogue("Terrains")
        .Select(row => checked((byte)row.ID)).ToHashSet());

    public static IReadOnlyList<MapDiagnostic> Validate(NativeMapDocument document, bool includeScripts = true)
    {
        ArgumentNullException.ThrowIfNull(document);
        var records = document.Records(); var result = new List<MapDiagnostic>();
        var nativeMembers = records.Where(row => row.Category.StartsWith("BandHero", StringComparison.Ordinal)).ToDictionary(row => (row.File, row.Offset));
        var version = ValueCatalogues.GameVersion(document);
        void Check(NativeRecord record, string name, string catalogue, IEnumerable<string> choices)
        {
            var field = record.Fields.SingleOrDefault(row => row.Name == name && row.BitMask == 0);
            if (field is null) return;
            var value = record.Read(field);
            if (choices.Contains(value, StringComparer.Ordinal)) return;
            result.Add(new(DiagnosticSeverity.Information, "DK2N039",
                $"ID is outside the verified original editor catalogue {catalogue}: {record.Category}.{name}={value}, 0x{record.Offset + field.Offset:X}. Imported/custom definitions may still be valid; bytes are retained.", record.File.SourcePath));
        }
        void Field(NativeRecord record, string name, string catalogue)
        {
            var field = record.Fields.SingleOrDefault(row => row.Name == name);
            if (field is null) return;
            var choices = ValueCatalogues.ForField(record, field, version);
            // An unknown parent category is not a reason to invent a target list.
            if (choices.Count != 0) Check(record, name, catalogue, choices.Select(row => row.Value));
        }
        foreach (var record in records)
        {
            var projectThing = record.File.Signature == NativeMapDocument.LegacyThings
                && record.File.UInt32(4) is >= 1 and <= 6 && record.Category.StartsWith("Project.MAPTHING", StringComparison.Ordinal);
            var native = record.File.UInt32(4) == 4 && RecordTemplate.All.FirstOrDefault(row => row.Structure == record.Category) is { } template
                && template.FileSignature == record.File.Signature && template.Size == record.Length
                && record.Fields.Count == LegacySchemas.Fields(record.Category).Count;
            if (projectThing)
            {
                Field(record, "id", "placed things / stored source IDs");
                if (Read(record, "node.type") == "3" && Read(record, "id") == "4")
                    Check(record, "dataAll", "keeper spells", ValueCatalogues.Elements("Spells").Select(row => row.Value));
            }
            if (native)
            {
                switch (record.Category)
                {
                    case "GAMEKEEPER": case "GAMENEUTRAL": case "GAMEHERO":
                        Check(record, "bGlobalCreatureID", "creature species", creatures.Value); break;
                    case "GAMEOBJECT":
                        Field(record, "bObjectID", "objects and specials");
                        if (Read(record, "bObjectID") == "4") Field(record, "dwObjectSpell", "keeper spells");
                        break;
                    case "GAMEDOOR": Field(record, "bDoorID", "doors"); break;
                    case "GAMETRAP": Field(record, "bTrapID", "traps"); break;
                    case "GAMEVARCREATURECOUNT": case "GAMEVARCREATURE":
                        Check(record, "dwGlobalCreatureID", "creature species", creatures.Value); break;
                    case "GAMEVARTHINGSTATUS": Field(record, "dwThingID", "availability category / variable IDs"); break;
                }
            }
            if (EffectGenerators.Supported(record) && (native || record.File.Signature == NativeMapDocument.LegacyAreas && record.File.UInt32(4) == 1))
                foreach (var slot in EffectGenerators.Fields.Take(4))
                    Check(record, EffectGenerators.StorageField(record, slot), "effects (zero means none)", EffectCatalogue.Choices.Select(row => row.Value));
            if (!NativeMapDocument.IsBand(record)) continue;
            if (record.Category == "GAMEBAND")
            {
                if (!native) continue;
                for (var index = 0; index < 16; index++)
                {
                    var member = nativeMembers[(record.File, record.Offset + 51 + index * 32)];
                    if (Read(member, "bGlobalCreatureID") == "0" || Read(member, "bGlobalBehavior") == "0") break;
                    Check(member, "bGlobalCreatureID", "creature species", creatures.Value);
                }
            }
            else if (record.File.UInt32(4) is >= 1 and <= 5)
            {
                for (var index = 0; index < 16; index++)
                {
                    var active = Read(record, $"heros[{index}].isvalid");
                    if (active is null or "0") break;
                    Check(record, $"heros[{index}].id", "creature species", creatures.Value);
                }
            }
        }
        AppendGlobalDefinitions(document, Check);
        if (includeScripts)
            foreach (var record in records.Where(ScriptGraph.IsEvent))
                foreach (var field in record.Fields)
                    if (ScenarioNamedCatalogues.DefinitionReferenceChoices(record, field) is { } reference)
                        Check(record, field.Name, reference.Catalogue, reference.Values);
        if (document.TileFile.Signature == 0x64 && document.TileFile.UInt32(4) == 4)
        {
            var unknown = new Dictionary<byte, (int Count, int X, int Y)>();
            for (var y = 0; y < document.Height; y++)
                for (var x = 0; x < document.Width; x++)
                {
                    var id = document.GetTile(x, y).Terrain;
                    if (terrain.Value.Contains(id)) continue;
                    var previous = unknown.GetValueOrDefault(id, (Count: 0, X: x, Y: y));
                    unknown[id] = (previous.Count + 1, previous.X, previous.Y);
                }
            foreach (var (id, location) in unknown)
                result.Add(new(DiagnosticSeverity.Information, "DK2N040",
                    $"Terrain ID {id} is outside the verified original editor terrain catalogue: {location.Count} cells; first at ({location.X}, {location.Y}). Imported/custom definitions may still be valid; bytes are retained.",
                    document.TileFile.SourcePath, new(location.X, location.Y, 1, 1)));
        }
        return result.AsReadOnly();
    }
    private static string? Read(NativeRecord record, string name) => record.Fields.SingleOrDefault(row => row.Name == name && row.BitMask == 0) is { } field ? record.Read(field) : null;
}
