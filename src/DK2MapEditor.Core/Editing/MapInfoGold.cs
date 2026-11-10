namespace DK2MapEditor.Core.Editing;

/// <summary>Immutable, read-only source Info-panel gold query. Gems and gold
/// objects are excluded. Prefix counts keep selection/cursor updates constant-time.</summary>
public sealed class MapInfoGoldSnapshot
{
    private readonly int width,height;
    private readonly int[] counts;
    private readonly long? value;
    internal MapInfoGoldSnapshot(int width,int height,int[] counts,long? value)
    {this.width=width;this.height=height;this.counts=counts;this.value=value;}
    public long? Total=>Amount(counts[^1]);
    private long? Amount(int count)=>count==0?0:count*value;
    public long? Within(TileRect rectangle)
    {
        if(rectangle.Width<=0||rectangle.Height<=0)return 0;
        var left=Math.Clamp(rectangle.X,0,width);var top=Math.Clamp(rectangle.Y,0,height);
        var right=(int)Math.Clamp((long)rectangle.X+rectangle.Width,0,width);var bottom=(int)Math.Clamp((long)rectangle.Y+rectangle.Height,0,height);
        if(right<=left||bottom<=top)return 0;
        var stride=width+1;return Amount(counts[bottom*stride+right]-counts[top*stride+right]-counts[bottom*stride+left]+counts[top*stride+left]);
    }
}

public sealed partial class NativeMapDocument
{
    public MapInfoGoldSnapshot CaptureMapInfoGold()
    {
        var values=StatisticsTerrainValues(Records(),new HashSet<string>(StringComparer.Ordinal));
        var stride=checked(Width+1);var counts=new int[checked(stride*(Height+1))];
        for(var y=0;y<Height;y++)for(var x=0;x<Width;x++)
        {
            var tile=GetTile(x,y);var at=(y+1)*stride+x+1;
            // World Gold is packed ID 0x06020101; its display owner does not
            // change the source query. Invalid/other variants receive no value.
            var gold=tile.Terrain==6&&tile.Variant==1?1:0;
            counts[at]=gold+counts[at-1]+counts[at-stride]-counts[at-stride-1];
        }
        return new(Width,Height,counts,values[6].Gold);
    }
}
