using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DK2MapEditor.App.Editing;

/// <summary>One source reward glyph; an empty reward retains its source text indentation.</summary>
internal sealed class FontSizedRewardImage : Image
{
    private static readonly Dictionary<int,BitmapSource> originals=[];
    public static readonly DependencyProperty ImageResourceProperty=DependencyProperty.Register(nameof(ImageResource),typeof(int?),typeof(FontSizedRewardImage),
        new FrameworkPropertyMetadata(null,FrameworkPropertyMetadataOptions.AffectsMeasure,(sender,_)=>((FontSizedRewardImage)sender).RefreshSource()));
    public static readonly DependencyProperty FontFamilyProperty=TextElement.FontFamilyProperty.AddOwner(typeof(FontSizedRewardImage),
        new FrameworkPropertyMetadata(SystemFonts.MessageFontFamily,FrameworkPropertyMetadataOptions.Inherits|FrameworkPropertyMetadataOptions.AffectsMeasure));
    public static readonly DependencyProperty FontSizeProperty=TextElement.FontSizeProperty.AddOwner(typeof(FontSizedRewardImage),
        new FrameworkPropertyMetadata(SystemFonts.MessageFontSize,FrameworkPropertyMetadataOptions.Inherits|FrameworkPropertyMetadataOptions.AffectsMeasure));
    public int? ImageResource {get=>(int?)GetValue(ImageResourceProperty);set=>SetValue(ImageResourceProperty,value);}
    public FontFamily FontFamily {get=>(FontFamily)GetValue(FontFamilyProperty);set=>SetValue(FontFamilyProperty,value);}
    public double FontSize {get=>(double)GetValue(FontSizeProperty);set=>SetValue(FontSizeProperty,value);}
    public FontSizedRewardImage()
    {Stretch=Stretch.Fill;SnapsToDevicePixels=true;RenderOptions.SetBitmapScalingMode(this,BitmapScalingMode.NearestNeighbor);}
    private void RefreshSource()
    {
        if(ImageResource is not {} resource){Source=null;return;}
        resource=OriginalObjectImages.CanonicalResource(resource);
        lock(originals)
        {
            if(!originals.TryGetValue(resource,out var image))
            {
                var assembly=typeof(FontSizedRewardImage).Assembly;var name=resource.ToString(CultureInfo.InvariantCulture);
                using var stream=assembly.GetManifestResourceStream("Legacy.ObjectImage."+name)
                    ??assembly.GetManifestResourceStream("Legacy.OverviewIcon."+name)
                    ??assembly.GetManifestResourceStream("Legacy.TerrainImage."+name)
                    ??assembly.GetManifestResourceStream("Legacy.DefinitionImage."+name)
                    ??assembly.GetManifestResourceStream("Legacy.ScenarioImage."+name)
                    ??throw new InvalidOperationException("Missing original reward image: "+resource);
                image=BitmapFrame.Create(stream,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad);image.Freeze();originals.Add(resource,image);
            }
            Source=image;
        }
    }
    protected override Size MeasureOverride(Size constraint)
    {
        var dpi=VisualTreeHelper.GetDpi(this);
        var size=RewardImageSizing.Size(FontFamily.LineSpacing*FontSize,dpi.DpiScaleY);
        base.MeasureOverride(new(size,size));return new(size,size);
    }
    protected override void OnDpiChanged(DpiScale oldDpi,DpiScale newDpi)
    {base.OnDpiChanged(oldDpi,newDpi);InvalidateMeasure();}
}
