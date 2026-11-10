using System.Buffers.Binary;
using System.Globalization;
using DK2MapEditor.Core.Formats;

namespace DK2MapEditor.Core.Editing;

public enum EditorFilePreviewKind{Map,Area,Bitmap}
public sealed record EditorFilePreviewCounts(long Creatures,long Doors,long Traps,long Objects,long SpecialItems);
public sealed record EditorFilePreviewSnapshot(string Path,int Width,int Height,ThumbnailRaster? Raster,bool Bitmap,EditorFilePreviewCounts? Counts);

/// <summary>Detached reads for the source common-dialog preview pane. No current
/// document, recovery consent, path acceptance or writer participates.</summary>
public static class EditorFilePreview
{
    public static EditorFilePreviewSnapshot Read(string path,EditorFilePreviewKind kind,CancellationToken cancellation=default)
    {
        cancellation.ThrowIfCancellationRequested();path=System.IO.Path.GetFullPath(path);
        if(new FileInfo(path).Length>64*1024*1024)throw new InvalidDataException("The preview input exceeds 64 MiB.");
        if(kind==EditorFilePreviewKind.Bitmap)return ReadBitmap(path);
        if(kind==EditorFilePreviewKind.Area||System.IO.Path.GetExtension(path).Equals(".dkz",StringComparison.OrdinalIgnoreCase))
        {
            var area=SelectionFile.Read(path);var counts=new long[5];
            foreach(var item in area.Things)
            {
                var type=-1;
                if(item.FileSignature==NativeMapDocument.LegacyThings&&item.Fields.FirstOrDefault(field=>field.Name=="node.type") is { } field)type=int.Parse(field.Read(item.Data),CultureInfo.InvariantCulture);
                else type=item.Category switch{"GAMEHERO" or "GAMENEUTRAL" or "GAMEKEEPER"=>0,"GAMEDOOR"=>1,"GAMETRAP"=>2,"GAMEOBJECT"=>3,_=>-1};
                if(type==3&&item.Category=="GAMEOBJECT"&&item.Fields.FirstOrDefault(field=>field.Name=="bObjectID") is { } id&&int.Parse(id.Read(item.Data),CultureInfo.InvariantCulture) is >=117 and <=129)type=4;
                if(type is >=0 and <=4)counts[type]++;
            }
            return new(path,area.Tiles.Width,area.Tiles.Height,Raster(area.Tiles.Width,area.Tiles.Height,(x,y)=>area.Tiles.Tiles[y*area.Tiles.Width+x],cancellation),false,new(counts[0],counts[1],counts[2],counts[3],counts[4]));
        }
        if(kind!=EditorFilePreviewKind.Map)throw new ArgumentOutOfRangeException(nameof(kind));
        var map=NativeMapDocument.Load(path);cancellation.ThrowIfCancellationRequested();
        var raster=Raster(map.Width,map.Height,map.GetTile,cancellation);EditorFilePreviewCounts? statistics=null;
        try{var things=map.CalculateStatistics().Things;statistics=new(things.Creatures,things.Doors,things.Traps,things.Objects,things.SpecialItems);}
        catch(Exception error)when(error is ArgumentException or InvalidDataException or InvalidOperationException or FormatException or OverflowException){/* Terrain preview survives unavailable object metadata. */}
        cancellation.ThrowIfCancellationRequested();return new(path,map.Width,map.Height,raster,false,statistics);
    }
    private static ThumbnailRaster Raster(int width,int height,Func<int,int,MapTile> tile,CancellationToken cancellation)
    {
        if(width<1||height<1||(long)width*height>4*1024*1024)throw new InvalidDataException("The preview raster is too large.");
        var pixels=new byte[checked(width*height*4)];
        for(var y=0;y<height;y++){cancellation.ThrowIfCancellationRequested();for(var x=0;x<width;x++)
        {var colour=MapOverviewPalette.ColorRef(tile(x,y));var offset=(y*width+x)*4;pixels[offset]=(byte)(colour>>16);pixels[offset+1]=(byte)(colour>>8);pixels[offset+2]=(byte)colour;pixels[offset+3]=255;}}
        return new(width,height,pixels);
    }
    private static EditorFilePreviewSnapshot ReadBitmap(string path)
    {
        using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);Span<byte> header=stackalloc byte[54];stream.ReadExactly(header[..26]);
        if(header[0]!='B'||header[1]!='M')throw new InvalidDataException("Not a BMP file.");
        var size=BinaryPrimitives.ReadUInt32LittleEndian(header[14..]);int width,height;ushort planes,bits;
        if(size==12){width=BinaryPrimitives.ReadUInt16LittleEndian(header[18..]);height=BinaryPrimitives.ReadUInt16LittleEndian(header[20..]);planes=BinaryPrimitives.ReadUInt16LittleEndian(header[22..]);bits=BinaryPrimitives.ReadUInt16LittleEndian(header[24..]);}
        else
        {
            if(size is not (40 or 52 or 56 or 108 or 124))throw new InvalidDataException("Unsupported BMP header.");stream.ReadExactly(header[26..]);
            width=BinaryPrimitives.ReadInt32LittleEndian(header[18..]);var h=BinaryPrimitives.ReadInt32LittleEndian(header[22..]);if(h==int.MinValue)throw new InvalidDataException("Invalid BMP height.");height=Math.Abs(h);planes=BinaryPrimitives.ReadUInt16LittleEndian(header[26..]);bits=BinaryPrimitives.ReadUInt16LittleEndian(header[28..]);
        }
        var offset=BinaryPrimitives.ReadUInt32LittleEndian(header[10..]);
        if(width<1||height<1||(long)width*height>16*1024*1024||planes!=1||bits is not (1 or 4 or 8 or 16 or 24 or 32)||offset<14+size||offset>=stream.Length)throw new InvalidDataException("Invalid BMP dimensions or pixel offset.");
        return new(path,width,height,null,true,null); // Windows LoadImage validates/decodes the selected BMP in the real dialog.
    }
}
