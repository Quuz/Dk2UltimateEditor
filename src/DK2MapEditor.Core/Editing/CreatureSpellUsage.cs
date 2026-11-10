using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public sealed record CreatureSpellUsage(IReadOnlyList<GlobalDefinitionRow> Users,bool Complete);

public sealed partial class NativeMapDocument
{
    /// <summary>Read-only source page6868/control123. A creature appears once
    /// when any of its three attack spell IDs matches the selected spell ID.
    /// Unknown layouts do not acquire inferred values or default users.</summary>
    public CreatureSpellUsage ReadCreatureSpellUsage(GlobalDefinitionRow spell)
    {
        ArgumentNullException.ThrowIfNull(spell);
        if(spell.Family!="CrSpells")throw new ArgumentException("Choose a creature spell definition.",nameof(spell));
        var rows=GlobalDefinitionRows();
        var selected=rows.SingleOrDefault(r=>r.Family=="CrSpells"&&r.Index==spell.Index)
            ??throw new ArgumentException("Unknown creature spell row.",nameof(spell));
        if(!selected.Editable)return new(Array.AsReadOnly(Array.Empty<GlobalDefinitionRow>()),false);
        var creatures=rows.Where(r=>r.Family=="Creatures").ToArray();var users=new List<GlobalDefinitionRow>();
        var id=selected.ID.ToString(CultureInfo.InvariantCulture);
        var attackIDs=GlobalDefinitionDefaults.Rules("Creatures").Where(r=>r.Name is "Shot1ID" or "Shot2ID" or "Shot3ID").ToArray();
        foreach(var creature in creatures.Where(r=>r.Editable))
        {
            // Rows/layouts were verified once above. Read only the three IDs;
            // rebuilding the entire global catalogue per creature is unnecessary.
            if(attackIDs.Any(rule=>(creature.Record is { } record?record.Read(record.Fields.Single(f=>f.Name==rule.Name))
                :GlobalDefinitionDefaults.ProjectDefault(rule,creature.Index))==id))users.Add(creature);
        }
        return new(users.AsReadOnly(),creatures.Length==45&&creatures.All(r=>r.Editable));
    }
}
