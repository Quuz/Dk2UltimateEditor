using System.ComponentModel;
using System.Windows.Media.Imaging;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private sealed class PaletteChoice(int id,string label,Func<BitmapSource?> image) : INotifyPropertyChanged
    {
        public int ID { get; } = id;
        public string Label { get; } = label;
        public BitmapSource? Icon => image();
        public event PropertyChangedEventHandler? PropertyChanged;
        internal void RefreshImage() => PropertyChanged?.Invoke(this,new(nameof(Icon)));
    }
    private BitmapSource? PaletteTerrainImage(int terrain) => OriginalScenarioImages.For(MapPaletteImageResources.ForTerrain((byte)terrain,
        (byte)(OwnerPicker.SelectedValue is int owner?owner:2),(byte)(VariantPicker.SelectedIndex==1?2:1)));
    private void RefreshPaletteImages()
    {
        foreach(var row in TerrainPicker.Items.OfType<PaletteChoice>())row.RefreshImage();
    }
}
