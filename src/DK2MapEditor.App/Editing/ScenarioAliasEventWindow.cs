using System.Windows;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public sealed class ScenarioAliasEventWindow : Window
{
    private readonly ScenarioAliasEventEditor editor;
    private bool closed;
    public bool HasPendingInputs => editor.HasPendingInputs;
    public ScenarioAliasEventWindow(NativeMapDocument document, NativeRecord record, Func<bool> ownerCurrent, Func<bool> commitFields)
    {
        NameScope.SetNameScope(this, new NameScope()); SetResourceReference(TitleProperty, "AliasEvent.Title");
        SetResourceReference(BackgroundProperty, "Brush.Background"); SetResourceReference(ForegroundProperty, "Brush.Text");
        Width = 780; Height = 640; MinWidth = 660; MinHeight = 480; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        editor = new(document, record, () => !closed && ownerCurrent(), commitFields);
        Content = editor; editor.RegisterControls(RegisterName); editor.CloseRequested += Close;
        Closed += (_, _) => { closed = true; editor.CloseEditor(); };
    }
    public void ResetDraft() => editor.ResetDraft();
    public bool ApplyDraft() => editor.ApplyDraft();
}
