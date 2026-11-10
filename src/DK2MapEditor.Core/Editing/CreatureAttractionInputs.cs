namespace DK2MapEditor.Core.Editing;

public static partial class CreaturePageControls
{
    /// <summary>Original CBN_SELCHANGE for control 124/127, before page Save.
    /// Properties_GlobalSetAttractions reads quantity from the private row,
    /// discarding unsaved quantity text. It does not write the displayed fallback
    /// ID to that row; only an explicit ID selection or page Save can do so.</summary>
    public static IReadOnlyList<CreaturePageInput> ChangeAttractionType(CreaturePageDraft baseline,
        IEnumerable<CreaturePageInput> inputs, string field, string type)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(inputs);
        if (baseline.Section != "Parameters" || !baseline.Editable)
            throw new InvalidOperationException("Choose an editable creature Parameters page.");
        if (field is not ("Attract1Type" or "Attract2Type") || !Choices(field).Any(choice=>choice.Value==type))
            throw new ArgumentException("Choose an original attraction type.");
        var current=inputs.ToArray();
        if (current.Any(item=>item is null || item.Value is null)
            || !current.Select(item=>item.Field).Order().SequenceEqual(Fields("Parameters").Order()))
            throw new ArgumentException("Provide every creature parameter input exactly once.");
        return Array.AsReadOnly(current.Select(item=>item.Field==field ? item with {Value=type}
            : field=="Attract2Type" && item.Field=="Attract2Data" ? item with {Value=baseline.Display("Attract2Data")}
            : item).ToArray());
    }

    /// <summary>Visible ID selection after rebuilding the source list. Disabled
    /// original lists are empty; a missing ID displays the first entry without
    /// changing the stored input. Unknown imported type layouts retain their
    /// existing diagnostic representation rather than guessing a source branch.</summary>
    public static string? AttractionSelection(string field, string storedID, string type)
    {
        ArgumentNullException.ThrowIfNull(storedID);
        if (field is not ("Attract1ID" or "Attract2ID"))
            throw new ArgumentException("Choose an attraction ID control.");
        var typeField=field=="Attract1ID"?"Attract1Type":"Attract2Type";
        if (!Choices(typeField).Any(choice=>choice.Value==type)) return storedID;
        if (!Enabled(field,type)) return null;
        var choices=Choices(field,type);
        return choices.Any(choice=>choice.Value==storedID)?storedID:choices[0].Value;
    }

    public static string AttractionAmountText(string storedAmount, string type)
    {
        ArgumentNullException.ThrowIfNull(storedAmount);
        return type=="0"?"":storedAmount;
    }
}
