namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    /// <summary>Commit an inline alias page and its parent's repeat setting in
    /// one edit. Both drafts must belong to the same unchanged event. The alias
    /// page remains the sole authority for operands; the parent supplies repeat.</summary>
    public void ApplyScenarioAliasParameterDraft(ScenarioParameterDraft parameters, ScenarioAliasEventDraft aliases)
    {
        VerifyScenarioParameterDraft(parameters);
        VerifyScenarioAliasEvent(aliases);
        var record = parameters.Record;
        if (parameters.Type != aliases.Type || !ReferenceEquals(record.File, aliases.Record.File)
            || record.Offset != aliases.Record.Offset || record.Length != aliases.Record.Length
            || record.Category != aliases.Record.Category)
            throw new ArgumentException("Compose drafts for the same scenario event.");

        var repeat = parameters.Fields.Where(field => field.Name is "repeat" or "Repeat")
            .Select(field => (Field: field, Bytes: field.Encode(parameters.Values[field.Name])))
            .Where(item => !item.Bytes.AsSpan().SequenceEqual(record.File.Data.Span.Slice(record.Offset + item.Field.Offset, item.Field.Size)))
            .ToArray();
        foreach (var field in parameters.Fields.Where(field => field.Name is not ("repeat" or "Repeat")))
            if (!field.Encode(parameters.Values[field.Name]).AsSpan().SequenceEqual(field.Encode(parameters.Baseline[field.Name])))
                throw new ArgumentException("Edit alias-page operands through their source-specific draft.");

        var operands = aliases.Values.Where(value => value.Value != aliases.Baseline[value.Key]).Select(value =>
        {
            var field = ScenarioEventField(aliases.Record, value.Key);
            return (Field: field, Bytes: ScenarioEventEncode(field, aliases.Type, value.Key, value.Value));
        }).ToArray();
        var names = aliases.Aliases.Select(value =>
            (Location: ScenarioAliasLocation(aliases.Kind, value.Key), Bytes: ScenarioAlias.Encode(value.Value))).ToArray();
        Edit("Edit scenario parameters and aliases", () =>
        {
            foreach (var operand in operands) operand.Bytes.CopyTo(record.File.Bytes, record.Offset + operand.Field.Offset);
            foreach (var name in names) name.Bytes.CopyTo(name.Location.File.Bytes, name.Location.Offset);
            foreach (var value in repeat) value.Bytes.CopyTo(record.File.Bytes, record.Offset + value.Field.Offset);
        });
    }
}
