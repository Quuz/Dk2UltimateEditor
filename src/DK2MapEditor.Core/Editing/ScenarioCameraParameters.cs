using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public sealed partial class NativeMapDocument
{
    /// <summary>A library selection changes only a private parent draft.
    /// Apply/Cancel belong to the parent; area and unrelated operands survive.</summary>
    public ScenarioParameterDraft UpdateScenarioCameraLibrary(ScenarioParameterDraft draft, int pathID)
    {
        ArgumentNullException.ThrowIfNull(draft);
        if (draft.Type != "ACTION_CAMERAPATH") throw new ArgumentException("Select a camera-path parent draft.");
        var choice = CameraPaths.Choices.SingleOrDefault(item => item.ID == pathID)
            ?? throw new ArgumentException("Choose an original camera path.");
        string Field(int number) => draft.Fields.Single(item => item.Name == "param" + number || item.Name == "Param" + number).Name;
        return UpdateScenarioControlPage(draft, [new(Field(1), choice.ID.ToString(CultureInfo.InvariantCulture)),
            new(Field(3), choice.CeilingMode.ToString(CultureInfo.InvariantCulture))]);
    }
}
