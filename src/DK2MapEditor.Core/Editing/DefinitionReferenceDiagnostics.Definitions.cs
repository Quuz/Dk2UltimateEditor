namespace DK2MapEditor.Core.Editing;

public static partial class DefinitionReferenceDiagnostics
{
    private static void AppendGlobalDefinitions(NativeMapDocument document, Action<NativeRecord, string, string, IEnumerable<string>> check)
    {
        foreach (var row in document.GlobalDefinitionRows().Where(row => row.Editable && row.Record is not null))
        {
            var record = row.Record!;
            void Reference(string field, string catalogue, IEnumerable<ValueChoice> choices)
            {
                // Original definition buffers retain zero for absent/unused links,
                // including fields whose editing combo has no selectable zero row.
                if (Read(record, field) is null or "0") return;
                check(record, field, catalogue, choices.Select(choice => choice.Value));
            }
            if (GlobalDefinitionDecorations.Families.Contains(row.Family))
                foreach (var field in GlobalDefinitionDecorations.Fields(row.Family).Where(field => field != "ArmourType"))
                    if (GlobalDefinitionDecorations.Enabled(row.Family, row.Index, field))
                        Reference(field, field.StartsWith("Objects[", StringComparison.Ordinal) ? "room objects" : "effects", GlobalDefinitionDecorations.Choices(row.Family, field));
            if (row.Family == "Terrains")
                foreach (var field in new[] { "MaxHealthType", "DestroyedType" })
                    if (GlobalTerrainControls.Enabled(row.Index, field))
                        Reference(field, "terrain transitions", GlobalTerrainControls.Choices(row.Index, field));
            if (row.Family == "Doors")
                foreach (var field in new[] { "Crate", "Key" }) Reference(field, "door objects / " + field, GlobalDefinitionRules.Choices("Doors", field));
            if (row.Family == "Traps" && GlobalTrapControls.Enabled(row.Index, "Crate"))
                Reference("Crate", "trap crate objects", GlobalDefinitionRules.Choices("Traps", "Crate"));
            if (row.Family == "Creatures")
            {
                Reference("LairObject", "creature lair objects", CreaturePageControls.Choices("LairObject"));
                foreach (var prefix in new[] { "Attract1", "Attract2" })
                {
                    var type = Read(record, prefix + "Type") ?? "0";
                    if (CreaturePageControls.Enabled(prefix + "ID", type))
                        Reference(prefix + "ID", "creature attraction / " + type, CreaturePageControls.Choices(prefix + "ID", type));
                }
                foreach (var slot in CreatureAttacks.Slots)
                    Reference(CreatureAttacks.TypeField(slot), "creature spells", CreatureAttacks.Options(slot).Select(option => option.Name));
            }
            foreach (var slot in GlobalProjectiles.Slots(row.Family))
            {
                var fields = GlobalProjectiles.Fields(row.Family, slot);
                Reference(fields.Type, "projectile types", GlobalProjectiles.Options.Select(option => option.Name));
                if (GlobalProjectiles.Find(Read(record, fields.Type) ?? "") is not { Form: 2 or 3 or 4 } option) continue;
                // Numeric damage/chance/level arguments are never definition IDs.
                check(record, fields.Data1, option.Form == 4 ? "projectile object targets" : "projectile creature targets",
                    GlobalProjectiles.Targets(option).Select(choice => choice.Value));
            }
        }
    }
}
