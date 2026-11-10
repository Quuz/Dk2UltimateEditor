using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DK2MapEditor.Core.Editing;

public sealed record ValueChoice(string Value, string English, string Russian)
{
    public string Name(string language) => language == "ru" ? Russian : English;
}

/// <summary>Named, verified suggestions. Unknown values remain editable numeric values.
/// Source IDs, pool IDs and array indexes are deliberately distinct mappings.</summary>
public static class ValueCatalogues
{
    private static readonly IReadOnlyDictionary<string, (string English, string Russian)> names = ReadNames();
    private static readonly ConcurrentDictionary<(string Group, bool Variable), IReadOnlyList<ValueChoice>> cache = [];
    public static IReadOnlyList<string> Groups { get; } = ["BadCreatures", "BadElites", "Heros", "Doors", "Traps", "Objects", "Magics", "MagicsV17", "Spells", "VarRooms"];
    public static IReadOnlyList<ValueChoice> Elements(string group, bool variable = false) => cache.GetOrAdd((group, variable), key => LegacyCompilerRules.Elements(key.Group)
        .Select(item => Named(item, (key.Variable ? item.Variable : item.ID).ToString(CultureInfo.InvariantCulture))).ToArray());
    public static ValueChoice Named(LegacyCompilerRules.Element item, string value)
    {
        var symbol = item.Name;
        var elite = symbol.EndsWith("Elite", StringComparison.Ordinal);
        if (elite) symbol = symbol[..^5];
        var key = Regex.Replace(symbol, @"^sz(?:Bad|Good)Creature", "Creature.");
        key = Regex.Replace(key, @"^szMapWorldObject", "Object.");
        key = Regex.Replace(key, @"^szMapWorldMaterial(?=[1-7]$)", "Material.");
        key = Regex.Replace(key, @"^szMapWorld", "Room.");
        key = Regex.Replace(key, @"^szSpellCr(?=[0-9A-F]{2}$)", "CreatureSpell.");
        key = Regex.Replace(key, @"^szSpellShot(?=[0-9A-F]{2}$)", "Projectile.");
        key = Regex.Replace(key, @"^szJob(?=[0-9A-F]{2}$)", "CreatureJob.");
        key = Regex.Replace(key, @"^szMelee(?=[1-4]$)", "Melee.");
        key = Regex.Replace(key, @"^sz(Resistance|Ability|Swipe|Gamma)(?=[0-9A-F]{2}$)", "$1.");
        key = Regex.Replace(key, @"^szSpell(Hand|Cast|Target)(?=[0-9A-F]{2}$)", "$1.");
        if (symbol == "szGlobalNoKey") key = "DoorKey.None";
        if (symbol is "szSpellLineOfSight" or "szSpellPressure" or "szSpellTrigger") key = "TrapTrigger." + symbol[7..];
        if (symbol == "szEffect000") key = "Projectile.None";
        key = Regex.Replace(key, @"^sz(Door|Trap|Magic|Spell)", "$1.");
        if (!names.TryGetValue(key, out var label)) throw new InvalidDataException("Missing bilingual catalogue name: " + symbol);
        return new(value, label.English + (elite ? " (Elite)" : ""), label.Russian + (elite ? " (элита)" : ""));
    }
    private static IReadOnlyDictionary<string, (string English, string Russian)> ReadNames()
    {
        using var stream = typeof(ValueCatalogues).Assembly.GetManifestResourceStream("Editor.CatalogNames")!;
        using var reader = new StreamReader(stream);
        var result = new Dictionary<string, (string English, string Russian)>(StringComparer.Ordinal);
        while (reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue;
            var parts = line.Split('|');
            if (parts.Length != 3 || parts.Any(string.IsNullOrWhiteSpace) || !result.TryAdd(parts[0], (parts[1], parts[2]))) throw new InvalidDataException("Invalid or duplicate bilingual catalogue name.");
        }
        return result;
    }
    private static ValueChoice Option(int value, string english, string russian) => new(value.ToString(CultureInfo.InvariantCulture), english, russian);
    public static IReadOnlyList<ValueChoice> Owners { get; } = [Option(1, "Heroes", "Герои"), Option(2, "Neutral", "Нейтральный"), .. Enumerable.Range(3, 5).Select(value => Option(value, "Keeper " + (value - 2), "Хранитель " + (value - 2)))];
    private static readonly Lazy<IReadOnlyList<ValueChoice>> creatures = new(() => [.. Elements("BadCreatures"), .. Elements("BadElites"), .. Elements("Heros")]);
    private static IReadOnlyList<ValueChoice> Creatures => creatures.Value;
    private static readonly Lazy<IReadOnlyList<ValueChoice>> bandCreatures = new(() => [.. Elements("BadCreatures"), .. Elements("Heros")]);
    // Heros_ModifyInit and Things_SelectInit with THING_FLAGFROMBAND omit elites.
    // This is a suggestion catalogue, never a restriction on preserving imported/raw IDs.
    public static IReadOnlyList<ValueChoice> BandCreatures => bandCreatures.Value;
    // Native loading in the original editor selects game 1.70. DKM retains its
    // explicit target version: special item 0x7F changes meaning in 1.70.
    public static int GameVersion(NativeMapDocument? document)
    {
        return document?.TargetGameVersion ?? 17;
    }
    private static IReadOnlyList<ValueChoice> Magics(int gameVersion) => Elements(gameVersion >= 17 ? "MagicsV17" : "Magics");
    public static IReadOnlyList<ValueChoice> ForTemplate(string kind, int gameVersion = 17) => kind switch
    {
        "Creature" or "GAMEKEEPER" or "GAMENEUTRAL" or "GAMEHERO" => Creatures,
        "Object" => Elements("Objects"), "MagicalObject" => Magics(gameVersion), "GAMEOBJECT" => [.. Elements("Objects"), .. Magics(gameVersion)],
        "Door" or "GAMEDOOR" => Elements("Doors"), "Trap" or "GAMETRAP" => Elements("Traps"), _ => []
    };
    public static NativeField? PrimaryField(NativeRecord record)
    {
        var name = record.File.Signature == NativeMapDocument.LegacyThings ? "id" : record.Category switch
        {
            "GAMEOBJECT" => "bObjectID", "GAMETRAP" => "bTrapID", "GAMEDOOR" => "bDoorID",
            "GAMEKEEPER" or "GAMENEUTRAL" or "GAMEHERO" => "bGlobalCreatureID", _ when record.Category.StartsWith("BandHero", StringComparison.Ordinal) => "bGlobalCreatureID", _ => ""
        };
        return record.Fields.FirstOrDefault(field => field.Name == name);
    }
    private static int Number(NativeRecord record, string name) => int.TryParse(record.Fields.FirstOrDefault(field => field.Name == name) is { } field ? record.Read(field) : "", out var value) ? value : -1;
    public static IReadOnlyList<ValueChoice> ForField(NativeRecord record, NativeField field, int gameVersion = 17)
    {
        var scenario = ScenarioParameterChoices.ForField(record, field);
        if (scenario.Count != 0) return scenario;
        var namedScenario = ScenarioNamedCatalogues.ForField(record, field);
        if (namedScenario.Count != 0) return namedScenario;
        var name = field.Name;
        if (EffectGenerators.Supported(record) && (record.Category == "GAMEEFFECT" && name is ("wEffect1" or "wEffect2" or "wEffect3" or "wEffect4")
            || record.Category == "Project.MAPRECTEX" && name is ("effect1" or "effect2" or "effect3" or "effect4"))) return EffectCatalogue.Choices;
        if (field.BitMask != 0) return [Option(0, "Off", "Выключено"), Option(1, "On", "Включено")];
        if (name is "owner" or "ennemy" or "enemy" or "bGlobalPlayer" or "dwGlobalPlayer" or "wGlobalPlayer" or "dwAllieWith" || Regex.IsMatch(name, @"^heros\[\d+\]\.enemy$")) return Owners;
        if (name is "bGlobalLevel" || Regex.IsMatch(name, @"^heros\[\d+\]\.level$")) return Enumerable.Range(1, 10).Select(value => Option(value, "Level " + value, "Уровень " + value)).ToArray();
        if (name == "bDoorLocked" || record.File.Signature == NativeMapDocument.LegacyThings && name == "dataAll" && Number(record, "node.type") == 1)
            return [Option(0, "Unlocked", "Не заперта"), Option(1, "Locked", "Заперта")];
        if (record.File.Signature == NativeMapDocument.LegacyThings && name == "dataAll" && Number(record, "node.type") == 0)
            return Enumerable.Range(1, 10).Select(value => Option(value, "Level " + value, "Уровень " + value)).ToArray();
        if (name is "bGlobalCreatureID" or "dwGlobalCreatureID" || Regex.IsMatch(name, @"^heros\[\d+\]\.id$"))
            return record.Category.StartsWith("BandHero", StringComparison.Ordinal) || record.File.Signature == NativeMapDocument.LegacyBands
                && Regex.IsMatch(name, @"^heros\[\d+\]\.id$") ? BandCreatures : Creatures;
        if (name == "bObjectID") return [.. Elements("Objects"), .. Magics(gameVersion)];
        if (name == "bDoorID") return Elements("Doors");
        if (name == "bTrapID") return Elements("Traps");
        if (name == "dwObjectSpell" && Number(record, "bObjectID") == 4) return Elements("Spells");
        if (name == "id" && record.File.Signature == NativeMapDocument.LegacyThings) return Number(record, "node.type") switch
        { 0 => Creatures, 1 => Elements("Doors"), 2 => Elements("Traps"), 3 => Elements("Objects"), 4 => Magics(gameVersion), _ => [] };
        if (name is "wThingType") return [Option(1, "Room", "Комната"), Option(3, "Trap", "Ловушка"), Option(4, "Door", "Дверь"), Option(5, "Keeper Spell", "Заклинание хранителя")];
        if (name == "dwThingID") return Number(record, "wThingType") switch { 1 => Elements("VarRooms", true), 3 => Elements("Traps", true), 4 => Elements("Doors", true), 5 => Elements("Spells", true), _ => [] };
        if (name == "dwThingState" || Regex.IsMatch(name, @"^(Rooms|Traps|Doors|Spells)\[\d+\]$")) return
            [Option(0, "Not specified (legacy zero)", "Не задано (старое значение 0)"), .. MapRules.Availability];
        if (Regex.IsMatch(name, @"^heros\[\d+\]\.isvalid$")) return [Option(0, "Inactive slot", "Неактивная ячейка"), Option(1, "Active slot", "Активная ячейка")];
        return [];
    }

    /// <summary>Annotates fixed array positions with source catalogue identities.</summary>
    public static ValueChoice? ArrayElement(string name)
    {
        var match = Regex.Match(name, @"^(CreaturesCount|CreaturesParams|Rooms|Doors|Traps|Spells)\[(\d+)\]");
        if (!match.Success) return null;
        var index = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        var values = match.Groups[1].Value switch { "CreaturesCount" or "CreaturesParams" => Creatures, "Rooms" => Elements("VarRooms"), var group => Elements(group) };
        return index < values.Count ? values[index] : null;
    }
}
