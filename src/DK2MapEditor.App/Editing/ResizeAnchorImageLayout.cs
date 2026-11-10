namespace DK2MapEditor.App.Editing;

internal readonly record struct ResizeAnchorImage(int Resource,int Column,int Row);

internal static class ResizeAnchorImageLayout
{
    // Half-cell coordinates reproduce Properties_MapSizeButton, including its
    // diagonal directions when only one dimension shrinks.
    internal static IReadOnlyList<ResizeAnchorImage> Create(int anchor,bool shrinkWidth,bool shrinkHeight)
    {
        if(anchor is <0 or >8)throw new ArgumentOutOfRangeException(nameof(anchor));
        int[] images=!shrinkWidth&&!shrinkHeight?[47,45,46,44,40,43,42,40,41]:[41,shrinkHeight?40:45,42,shrinkWidth?43:44,40,shrinkWidth?44:43,46,shrinkHeight?45:40,47];
        var column=anchor%3;var row=anchor/3;var result=new List<ResizeAnchorImage>();
        for(var y=-1;y<=1;y++)for(var x=-1;x<=1;x++)
        {
            if(x==0&&y==0||column==0&&x<0||column==2&&x>0||row==0&&y<0||row==2&&y>0)continue;
            result.Add(new(images[(y+1)*3+x+1],column*2+1+x,row*2+1+y));
        }
        return result;
    }
}
