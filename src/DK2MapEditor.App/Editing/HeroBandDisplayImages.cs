using System.Windows.Media.Imaging;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

internal sealed record HeroBandDisplayImageRow(int Index,string Label,BitmapSource Icon);

internal static class HeroBandDisplayImages
{
    internal static HeroBandDisplayImageRow For(int index,EditorHeroMemberValues values,string language,Func<string,string> text)
    {
        var row=HeroBandDisplayRows.Create(index,values,language,text);
        return new(row.Index,row.Label,OriginalObjectImages.Get(row.ImageResource));
    }
}
