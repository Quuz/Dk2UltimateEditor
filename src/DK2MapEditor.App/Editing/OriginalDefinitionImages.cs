using System.Globalization;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

/// <summary>Source global-list bitmap IDs, distinct from row indices and game IDs.
/// No invented fallback picture for an unsupported/opaque extra definition row.</summary>
public static class OriginalDefinitionImages
{
    private static readonly Dictionary<int,BitmapSource> images=[];
    public static int? ResourceID(string family,int index)
    {
        var catalogue=GlobalDefinitionDefaults.Catalogue(family);
        return index>=0&&index<catalogue.Count&&catalogue[index].Icon>0?catalogue[index].Icon:null;
    }
    public static BitmapSource? For(string family,int index)
    {
        if(ResourceID(family,index) is not { } resource)return null;
        lock(images)
        {
            if(images.TryGetValue(resource,out var cached))return cached;
            var name=OriginalObjectImages.CanonicalResource(resource).ToString(CultureInfo.InvariantCulture);
            var assembly=typeof(OriginalDefinitionImages).Assembly;
            using var stream=assembly.GetManifestResourceStream("Legacy.DefinitionImage."+name)
                ??assembly.GetManifestResourceStream("Legacy.ObjectImage."+name)
                ??assembly.GetManifestResourceStream("Legacy.TerrainImage."+name)
                ??assembly.GetManifestResourceStream("Legacy.OverviewIcon."+name)
                ??throw new InvalidOperationException("Missing original definition image: "+resource);
            var original=BitmapFrame.Create(stream,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad);
            // Misc_CreateImageList loads every source image at 32x32, including
            // the preserved 48/64-pixel bitmap originals. Physical scaling QA
            // remains separate from raw embedded-bitmap identity verification.
            BitmapSource result=original.PixelWidth==32&&original.PixelHeight==32?original
                :new TransformedBitmap(original,new ScaleTransform(32d/original.PixelWidth,32d/original.PixelHeight));
            result.Freeze();images.Add(resource,result);return result;
        }
    }
    public static BitmapSource? ProjectileTarget(GlobalProjectileOption? option,string value)
    {
        if(!uint.TryParse(value,NumberStyles.Integer,CultureInfo.InvariantCulture,out var id))return null;
        if(option?.Form is 2 or 3)
        {
            var catalogue=GlobalDefinitionDefaults.Catalogue("Creatures");
            for(var index=0;index<catalogue.Count;index++)if(catalogue[index].ID==id)return For("Creatures",index);
        }
        else if(option?.Form==4)
        {
            var resource=LegacyCompilerRules.Elements("Objects").FirstOrDefault(item=>item.ID==id)?.Icon;
            if(resource is >0)return OriginalObjectImages.Get(resource.Value);
        }
        return null;
    }
}
