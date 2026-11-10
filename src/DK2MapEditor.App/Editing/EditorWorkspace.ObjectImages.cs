using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private ObjectImagePickerWindow? workspaceImagePicker;
    private BitmapSource? TemplateChoiceIcon(TemplateView? template, ValueChoice choice)
    {
        var kind = PaintingKind(template);
        if (kind is null || !uint.TryParse(choice.Value, out var id)) return null;
        if (template?.Template?.Structure == "GAMEOBJECT" && id is >= 117 and <= 129) kind = PlacedThingKind.SpecialItem;
        return OriginalObjectImages.For(kind.Value, id, catalogueVersion);
    }
    private BitmapSource? FieldChoiceIcon(FieldView field, ValueChoice choice)
        => uint.TryParse(choice.Value, out var id) && OriginalObjectImages.FieldKind(field.Record, field.Field, id) is { } kind
            ? OriginalObjectImages.For(kind, id, catalogueVersion) : null;
    private void CloseWorkspaceImagePicker() { workspaceImagePicker?.Cancel(); workspaceImagePicker = null; }
    private ObjectImagePickerWindow KeepWorkspaceImagePicker(ObjectImagePickerWindow window)
    {
        workspaceImagePicker = window;
        window.Closed += (_, _) => { if (ReferenceEquals(workspaceImagePicker, window)) workspaceImagePicker = null; };
        return window;
    }
    private bool SameImageField(FieldView field) => namedField is { } current && current.Record.File == field.Record.File
        && current.Record.Offset == field.Record.Offset && current.Record.Length == field.Record.Length
        && current.Field.Name == field.Field.Name && current.Field.Offset == field.Field.Offset;
    private static bool IsBandImageField(FieldView field) => field.Record.Category.StartsWith("BandHero", StringComparison.Ordinal)
        || field.Record.File.Signature == NativeMapDocument.LegacyBands;

    public ObjectImagePickerWindow? CreateNamedImagePicker()
    {
        if (document is null || !CommitFields() || namedField is not { } field
            || OriginalObjectImages.FieldKind(field.Record, field.Field, 0) is not { } kind) return null;
        var source = document; CloseWorkspaceImagePicker();
        uint.TryParse(field.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var current);
        var combined = field.Field.Name == "bObjectID";
        return KeepWorkspaceImagePicker(new(kind, catalogueVersion, current,
            () => ReferenceEquals(source, document) && SameImageField(field) && CommitFields() && SameImageField(field),
            id =>
            {
                // Only choose a draft. The existing Apply named value button performs the map edit.
                NamedSearch.Text = ""; FilterNamedValues(id.ToString(CultureInfo.InvariantCulture));
            }, bandMember: kind == PlacedThingKind.Creature && IsBandImageField(field), includeSpecialItems: combined));
    }
    public ObjectImagePickerWindow? CreateBandCreatureImagePicker()
    {
        if (document is null || !CommitFields() || RecordList.SelectedItem is not RecordView view || !NativeMapDocument.IsBand(view.Record)) return null;
        var source = document; var band = view.Record; CloseWorkspaceImagePicker();
        uint.TryParse((BandCreaturePicker.SelectedItem as NamedChoiceView)?.Choice.Value, out var current);
        return KeepWorkspaceImagePicker(new(PlacedThingKind.Creature, catalogueVersion, current,
            () => ReferenceEquals(source, document) && RecordList.SelectedItem is RecordView selected
                && selected.Record.File == band.File && selected.Record.Offset == band.Offset && selected.Record.Length == band.Length && CommitFields(),
            id => BandCreaturePicker.SelectedItem = BandCreaturePicker.Items.Cast<NamedChoiceView>().Single(choice => choice.Choice.Value == id.ToString(CultureInfo.InvariantCulture)),
            bandMember: true));
    }
    public ObjectImagePickerWindow? CreateElementImagePicker()
    {
        if (document is null || !CommitFields() || TemplatePicker.SelectedItem is not TemplateView template || PaintingKind(template) is not { } kind) return null;
        var source = document; var identity = template.LegacyKind ?? template.Template?.Structure; CloseWorkspaceImagePicker();
        uint.TryParse((ElementPicker.SelectedItem as NamedChoiceView)?.Choice.Value, out var current);
        return KeepWorkspaceImagePicker(new(kind, catalogueVersion, current,
            () => ReferenceEquals(source, document) && TemplatePicker.SelectedItem is TemplateView selected
                && (selected.LegacyKind ?? selected.Template?.Structure) == identity && CommitFields(),
            id => ElementPicker.SelectedItem = ElementPicker.Items.Cast<NamedChoiceView>().Single(choice => choice.Choice.Value == id.ToString(CultureInfo.InvariantCulture)),
            includeSpecialItems: identity == "GAMEOBJECT"));
    }
    private void ShowWorkspaceImagePicker(ObjectImagePickerWindow? window)
    { if (window is null) return; window.Owner = Window.GetWindow(this); window.ShowDialog(); }
    private void NamedImagePicker_Click(object sender, RoutedEventArgs e) => ShowWorkspaceImagePicker(CreateNamedImagePicker());
    private void BandImagePicker_Click(object sender, RoutedEventArgs e) => ShowWorkspaceImagePicker(CreateBandCreatureImagePicker());
    private void ElementImagePicker_Click(object sender, RoutedEventArgs e) => ShowWorkspaceImagePicker(CreateElementImagePicker());
}
