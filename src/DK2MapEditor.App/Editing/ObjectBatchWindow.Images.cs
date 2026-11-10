using System.Globalization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public sealed partial class ObjectBatchWindow
{
    private ObjectImagePickerWindow? imagePicker;
    public ObjectImagePickerWindow? CreateImagePicker()
    {
        if (closed || !canApply()) return null;
        imagePicker?.Cancel();
        uint.TryParse(fields[0].Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id);
        var kind = currentKind;
        var picker = new ObjectImagePickerWindow(kind, ValueCatalogues.GameVersion(document), id,
            () => !closed && currentKind == kind && canApply(), chosen => fields[0].Text = chosen.ToString(CultureInfo.InvariantCulture));
        imagePicker = picker;
        picker.Closed += (_, _) => { if (ReferenceEquals(imagePicker, picker)) imagePicker = null; };
        return picker;
    }
}
