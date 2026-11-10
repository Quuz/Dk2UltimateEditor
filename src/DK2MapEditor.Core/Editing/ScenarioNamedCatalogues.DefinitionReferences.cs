namespace DK2MapEditor.Core.Editing;

public static partial class ScenarioNamedCatalogues
{
    internal static (string Catalogue, IEnumerable<string> Values)? DefinitionReferenceChoices(NativeRecord record, NativeField field)
    {
        if (field.BitMask != 0 || record.File.Signature == 0xD2 && (record.File.UInt32(4) != 4 || record.Length != 24)
            || record.File.Signature == NativeMapDocument.LegacyEvents && record.File.UInt32(4) is not (2 or 3)) return null;
        var type = EntityReferences.EventType(record);
        if (type is null) return null;
        var prefix = record.Category is "Project.TRIGGER" or "Project.ACTION" ? "param" : "Param";
        if (!field.Name.StartsWith(prefix, StringComparison.Ordinal) || !int.TryParse(field.Name[prefix.Length..], out var parameter)) return null;
        if (type.Name is "TRIGGER_CLICKELEMENT" or "ACTION_SHOWELEMENT" or "ACTION_ADDELEMENT")
        {
            var primary = type.Name == "ACTION_ADDELEMENT" ? 2 : 1;
            if (parameter != primary + 1) return null;
            var parent = record.Fields.SingleOrDefault(row => row.Name == prefix + primary);
            if (parent is null || record.Read(parent) == "0") return null; // Interface panels are not definitions.
            var interfaceChoices = ForField(record, field);
            return interfaceChoices.Count == 0 ? null : ("scenario interface definition category", interfaceChoices.Select(choice => choice.Value));
        }
        if (!contexts.TryGetValue((type.Name, parameter), out var context)
            || context.Kind is not ("Creatures" or "Rooms" or "Doors" or "Traps" or "Destroyed" or "Spells" or "Terrain")) return null;
        if (context.Kind == "Creatures")
        {
            // Imported elites are valid species even in source menus that omit
            // them as suggestions. Any is a sentinel only in its own context.
            var values = ValueCatalogues.ForTemplate("Creature").Select(choice => choice.Value);
            return ("scenario creature species", context.Any ? values.Prepend("0") : values);
        }
        var choices = ForField(record, field);
        return choices.Count == 0 ? null : ("scenario " + context.Kind, choices.Select(choice => choice.Value));
    }
}
