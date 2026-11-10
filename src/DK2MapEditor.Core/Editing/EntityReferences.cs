using System.Text.RegularExpressions;

namespace DK2MapEditor.Core.Editing;

public enum EntityKind { Area, Band, Gate, Effect }
public sealed record EntityIdentity(EntityKind Kind, NativeRecord Record, NativeField Field);
public sealed record EntityReference(EntityKind Kind, NativeRecord Record, NativeField Field, bool ZeroIsNone = true, bool MaximumIsNone = false);
public sealed record EventReferenceRule(LegacyEventSchemas.EventType Event, EntityKind Kind, string Parameter);

/// <summary>Typed entity identities and references; numeric coincidences are not links.
/// Event bindings are extracted from the original dialog's value-storage code.</summary>
public static class EntityReferences
{
    public static IReadOnlyList<EventReferenceRule> EventRules { get; } = ReadEventRules();
    private static IReadOnlyList<EventReferenceRule> ReadEventRules()
    {
        var result = new List<EventReferenceRule>();
        foreach (var (resource, prefix, variable, function, next) in new[]
        {
            ("Legacy.Triggers_Actions.c", "ACTION", "TempAction", "int Triggers_ActionDlgParamsStore(", "int Triggers_ActionDlgParamsSave("),
            ("Legacy.Triggers_Triggers.c", "TRIGGER", "TempTrigger", "int Triggers_TriggerDlgParamsStore(", "int Triggers_TriggerDlgParamsSave(")
        })
        {
            using var stream = typeof(EntityReferences).Assembly.GetManifestResourceStream(resource)!;
            using var reader = new StreamReader(stream);
            var source = Regex.Replace(reader.ReadToEnd(), @"//[^\r\n]*|/\*[\s\S]*?\*/", "");
            var start = source.IndexOf(function, StringComparison.Ordinal);
            if (start < 0) throw new InvalidDataException("Original event-reference storage function is missing.");
            var end = source.IndexOf(next, start, StringComparison.Ordinal);
            if (end < 0) throw new InvalidDataException("Original event-reference storage function is missing.");
            foreach (Match branch in Regex.Matches(source[start..end], @"case\s+(?<name>" + prefix + @"_\w+)\s*:(?<body>[\s\S]*?)(?=\bcase\s+" + prefix + @"_|\z)"))
            {
                var type = LegacyEventSchemas.All.SingleOrDefault(item => item.Name == branch.Groups["name"].Value);
                if (type is null) continue;
                foreach (Match assignment in Regex.Matches(branch.Groups["body"].Value, variable + @"\.param(?<number>\d+)\s*=\s*(?<expression>[^;]+);"))
                {
                    var expression = assignment.Groups["expression"].Value;
                    EntityKind? kind = expression.Contains("Misc_GetArea(", StringComparison.Ordinal) ? EntityKind.Area
                        : expression.Contains("Misc_GetEffect(", StringComparison.Ordinal) ? EntityKind.Effect
                        : expression.Contains("Triggers_ActionDlgParamsGetGate(", StringComparison.Ordinal) ? EntityKind.Gate
                        : Regex.IsMatch(expression, @"\.(?:HerosList|CreateHerosList|DeadHerosList|JailHerosList|StunHerosList)\)") ? EntityKind.Band : null;
                    if (kind is not null) result.Add(new(type, kind.Value, "param" + assignment.Groups["number"].Value));
                }
            }
        }
        if (result.Count != 14 || result.DistinctBy(rule => (rule.Event.Name, rule.Parameter)).Count() != result.Count)
            throw new InvalidDataException("The original event-reference catalogue is incomplete or ambiguous.");
        return result.AsReadOnly();
    }

    public static EntityIdentity? Identity(NativeRecord record) => record.Category switch
    {
        "GAMEAREA" => Find(record, EntityKind.Area, "bAreaID"), "GAMEBAND" => Find(record, EntityKind.Band, "bBandID"),
        "GAMEEFFECT" => Find(record, EntityKind.Effect, "bEffectID"),
        _ when record.File.Signature == NativeMapDocument.LegacyBands && NativeMapDocument.IsBand(record) => Find(record, EntityKind.Band, "id"),
        _ when record.File.Signature == NativeMapDocument.LegacyAreas && record.Fields.Any(field => field.Name == "node.type") => Read(record, "node.type") switch
        { "0" => Find(record, EntityKind.Area, "id"), "1" => Find(record, EntityKind.Gate, "id"), "2" => Find(record, EntityKind.Effect, "id"), _ => null },
        _ => null
    };
    private static EntityIdentity? Find(NativeRecord record, EntityKind kind, string name) => record.Fields.FirstOrDefault(field => field.Name == name) is { } field ? new(kind, record, field) : null;
    private static string Read(NativeRecord record, string name) => record.Fields.FirstOrDefault(field => field.Name == name) is { } field ? record.Read(field) : "";

    public static LegacyEventSchemas.EventType? EventType(NativeRecord record)
    {
        if (record.File.Signature == 0xD2 && record.Length == 24)
            return LegacyEventSchemas.Find(record.File.UInt32(record.Offset), record.File.Data.Span[record.Offset + 22]);
        if (record.Category is "Project.TRIGGER" or "Project.ACTION" && byte.TryParse(Read(record, "ctrl"), out var type))
            return LegacyEventSchemas.Find(record.Category == "Project.TRIGGER" ? 0xD5u : 0xD6u, type);
        return null;
    }

    public static IReadOnlyList<EntityReference> From(NativeRecord record)
    {
        var result = new List<EntityReference>();
        void Add(EntityKind kind, string name, bool zero = true, bool maximum = false)
        { if (record.Fields.FirstOrDefault(field => field.Name == name) is { } field) result.Add(new(kind, record, field, zero, maximum)); }
        if (record.Category == "GAMEAREA") Add(EntityKind.Area, "bAreaNextID");
        if (record.Category == "GAMEHERO" || record.Category.StartsWith("BandHero", StringComparison.Ordinal))
        { Add(EntityKind.Area, "bHeroTargetArea"); Add(EntityKind.Band, "bHeroBandID", false, record.Category == "GAMEHERO"); }
        if (record.File.Signature == NativeMapDocument.LegacyThings && Read(record, "node.type") == "0") Add(EntityKind.Area, "area");
        if (record.File.Signature == NativeMapDocument.LegacyAreas && Read(record, "node.type") == "0") Add(EntityKind.Area, "nextid");
        if (record.File.Signature == NativeMapDocument.LegacyBands)
            foreach (var field in record.Fields.Where(field => Regex.IsMatch(field.Name, @"^heros\[\d+\]\.area$"))) Add(EntityKind.Area, field.Name);
        if (record.Category.StartsWith("Project.TRIGGERLINK", StringComparison.Ordinal))
        {
            if (Read(record, "node.type") == "4") Add(EntityKind.Area, "area");
            if (Read(record, "node.type") == "16") Add(EntityKind.Band, "band", false);
        }
        if (EventType(record) is { } type)
            foreach (var rule in EventRules.Where(rule => rule.Event == type))
                // DESTROYGATE uses coordinates in native data, not a gate ID.
                Add(rule.Kind, record.File.Signature == 0xD2 ? "Param" + rule.Parameter[5..] : rule.Parameter, rule.Kind != EntityKind.Band);
        return result;
    }

    public static bool IsNone(EntityReference reference, string value) => reference.ZeroIsNone && value == "0" || reference.MaximumIsNone && value == "255";

    /// <summary>Single-line region details, following the original RectEx_StrCpy.
    /// Only the displayed text changes; names and comments in the map stay literal.</summary>
    public static string DisplayDetails(string name, string info)
    {
        static string SingleLine(string text) => text.Replace("\r", "", StringComparison.Ordinal)
            .Replace("\n", " | ", StringComparison.Ordinal).Replace('\t', ' ');
        return (name.Length == 0 ? "" : " · " + SingleLine(name))
            + (info.Length == 0 ? "" : " · " + SingleLine(info));
    }
    public static string DisplayDetails(NativeRecord record)
    {
        var name = Read(record, "name");
        // The original band list uses its name literally, without RectEx_StrCpy.
        return Identity(record)?.Kind == EntityKind.Band ? name.Length == 0 ? "" : " · " + name
            : DisplayDetails(name, Read(record, "info"));
    }

    public static string KindCaption(EntityKind kind,string language)=>language=="ru"
        ?kind switch{EntityKind.Area=>"Область",EntityKind.Band=>"Отряд",EntityKind.Gate=>"Проход",EntityKind.Effect=>"Генератор эффектов",_=>kind.ToString()}
        :kind switch{EntityKind.Area=>"Area",EntityKind.Band=>"Band",EntityKind.Gate=>"Gate",EntityKind.Effect=>"Effect generator",_=>kind.ToString()};
    public static string DisplayIdentity(EntityIdentity identity,string language,int matches=1)
    {
        var label=KindCaption(identity.Kind,language)+" "+identity.Record.Read(identity.Field);
        // A numeric link cannot identify one of several records. Do not present
        // the first record's name as though it were the unique destination.
        return matches>1?label+(language=="ru"?$" · неоднозначно (записей: {matches})":$" · ambiguous: {matches} records"):label+DisplayDetails(identity.Record);
    }

    public static IReadOnlyList<ValueChoice>? Choices(IReadOnlyList<NativeRecord> records, NativeRecord record, NativeField field)
    {
        var reference = From(record).FirstOrDefault(reference => reference.Field == field);
        if (reference is null) return null;
        var identities = records.Select(Identity).OfType<EntityIdentity>().Where(identity => identity.Kind == reference.Kind && !IsNone(reference, identity.Record.Read(identity.Field))).ToArray();
        var choices = identities.GroupBy(identity => identity.Record.Read(identity.Field)).Select(group => new ValueChoice(group.Key, DisplayIdentity(group.First(),"en",group.Count()), DisplayIdentity(group.First(),"ru",group.Count()))).ToList();
        if (reference.ZeroIsNone) choices.Insert(0, new("0", "None", "Не задано"));
        if (reference.MaximumIsNone && !choices.Any(choice => choice.Value == "255")) choices.Add(new("255", "Not in a band", "Не состоит в отряде"));
        return choices;
    }
}
