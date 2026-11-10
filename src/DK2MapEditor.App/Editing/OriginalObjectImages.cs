using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

/// <summary>Preserved source bitmap identities, distinct from game IDs and list indices.</summary>
public static class OriginalObjectImages
{
    private static readonly Dictionary<(int Resource, int Size), BitmapSource> images = [];
    public static IReadOnlyList<string> Groups(PlacedThingKind kind, int version = 17) => kind switch
    {
        PlacedThingKind.Creature => ["BadCreatures", "BadElites", "Heros"],
        PlacedThingKind.Door => ["Doors"], PlacedThingKind.Trap => ["Traps"],
        PlacedThingKind.Object => ["Objects"], PlacedThingKind.SpecialItem => [version >= 17 ? "MagicsV17" : "Magics"],
        _ => []
    };
    public static int? ResourceID(PlacedThingKind kind, uint id, int version = 17)
        => Groups(kind, version).SelectMany(LegacyCompilerRules.Elements).FirstOrDefault(item => item.ID == id)?.Icon;
    // Source category defaults from CrIcons/DtIcons/ObIcons, not invented game IDs.
    public static int Fallback(PlacedThingKind kind) => kind switch
    {
        PlacedThingKind.Creature => 1115, PlacedThingKind.Door => 1301,
        PlacedThingKind.Trap => 1409, PlacedThingKind.Object => 2007, PlacedThingKind.SpecialItem => 1508,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
    public static BitmapSource For(PlacedThingKind kind, uint id, int version = 17, bool chooser = false)
        => Get(ResourceID(kind, id, version) ?? Fallback(kind), chooser && kind == PlacedThingKind.Object ? 64 : 32);
    public static PlacedThingKind? FieldKind(NativeRecord record, NativeField field, uint id)
    {
        if (field.Name is "bGlobalCreatureID" or "dwGlobalCreatureID" || System.Text.RegularExpressions.Regex.IsMatch(field.Name, @"^heros\[\d+\]\.id$")) return PlacedThingKind.Creature;
        if (field.Name == "bObjectID") return id is >= 117 and <= 129 ? PlacedThingKind.SpecialItem : PlacedThingKind.Object;
        if (field.Name == "bDoorID") return PlacedThingKind.Door;
        if (field.Name == "bTrapID") return PlacedThingKind.Trap;
        if (field.Name == "id" && record.File.Signature == NativeMapDocument.LegacyThings)
        {
            var type = record.Fields.FirstOrDefault(item => item.Name == "node.type");
            if (type is not null && int.TryParse(record.Read(type), out var value) && value is >= 0 and <= 4) return (PlacedThingKind)value;
        }
        return null;
    }
    public static BitmapSource Get(int resource, int size = 32)
    {
        if (size is not (32 or 64)) throw new ArgumentOutOfRangeException(nameof(size));
        // RC aliases deliberately reuse the same physical source file.
        resource = CanonicalResource(resource);
        lock (images)
        {
            if (images.TryGetValue((resource, size), out var cached)) return cached;
            using var stream = typeof(OriginalObjectImages).Assembly.GetManifestResourceStream("Legacy.ObjectImage." + resource.ToString(CultureInfo.InvariantCulture))
                ?? typeof(OriginalObjectImages).Assembly.GetManifestResourceStream("Legacy.OverviewIcon." + resource.ToString(CultureInfo.InvariantCulture))
                ?? throw new InvalidOperationException("Missing original object image: " + resource);
            var original = BitmapFrame.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            BitmapSource result = original.PixelWidth == size && original.PixelHeight == size ? original
                : new TransformedBitmap(original, new ScaleTransform((double)size / original.PixelWidth, (double)size / original.PixelHeight));
            result.Freeze(); images.Add((resource, size), result); return result;
        }
    }
    internal static int CanonicalResource(int resource)
        =>resource switch { 1214 or 1215 => 1213,1306=>1300,1412=>1400,_=>resource };
    internal static DataTemplate NamedTemplate(int imageSize = 32)
    {
        var panel = new FrameworkElementFactory(typeof(StackPanel)); panel.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
        var picture = new FrameworkElementFactory(typeof(Image)); picture.SetBinding(Image.SourceProperty, new Binding("Icon"));
        picture.SetValue(FrameworkElement.WidthProperty, (double)imageSize); picture.SetValue(FrameworkElement.HeightProperty, (double)imageSize);
        picture.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 1, 6, 1));
        var imageStyle = new Style(typeof(Image)); var missing = new DataTrigger { Binding = new Binding("Icon"), Value = null };
        missing.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Collapsed)); imageStyle.Triggers.Add(missing);
        picture.SetValue(FrameworkElement.StyleProperty, imageStyle); panel.AppendChild(picture);
        var label = new FrameworkElementFactory(typeof(TextBlock)); label.SetBinding(TextBlock.TextProperty, new Binding("Label"));
        label.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center); panel.AppendChild(label);
        return new DataTemplate { VisualTree = panel };
    }
}
