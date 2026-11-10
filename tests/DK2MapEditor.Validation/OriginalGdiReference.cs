using System.Runtime.InteropServices;
using DK2MapEditor.Core.Editing;

// Independent full-map monochrome DDB/GetPixel oracle. Production uses a
// local top-down 32-bit DIB and copied BGR pixels, never this helper.
internal static class OriginalGdiReference
{
    public static HashSet<TilePoint> Points(EditorPaintShape shape,TilePoint start,TilePoint end,TileRect bounds)
    {
        var dc=CreateCompatibleDC(IntPtr.Zero); var bitmap=CreateBitmap(bounds.X+bounds.Width+2,bounds.Y+bounds.Height+2,1,1,IntPtr.Zero);
        var brush=CreateSolidBrush(0xFFFFFF); var pen=CreatePen(0,0,0xFFFFFF);
        IntPtr oldBitmap=IntPtr.Zero,oldPen=IntPtr.Zero,oldBrush=IntPtr.Zero;
        try
        {
            if(dc==IntPtr.Zero || bitmap==IntPtr.Zero || brush==IntPtr.Zero || pen==IntPtr.Zero) throw new Exception("GDI oracle allocation failed.");
            oldBitmap=SelectObject(dc,bitmap); oldPen=SelectObject(dc,pen); oldBrush=SelectObject(dc,brush);
            var full=new Rect(0,0,bounds.X+bounds.Width+2,bounds.Y+bounds.Height+2); FillRect(dc,ref full,GetStockObject(4));
            var x=bounds.X; var y=bounds.Y; var width=bounds.Width; var height=bounds.Height;
            if(width==1 && height==1) SetPixelV(dc,x,y,0xFFFFFF);
            else if(shape==EditorPaintShape.Line) { MoveToEx(dc,start.X,start.Y,IntPtr.Zero); LineTo(dc,end.X,end.Y); SetPixelV(dc,end.X,end.Y,0xFFFFFF); }
            else if(shape==EditorPaintShape.FilledRectangle) { var rectangle=new Rect(x,y,x+width,y+height); FillRect(dc,ref rectangle,brush); }
            else if(shape==EditorPaintShape.Rectangle)
            {
                width--; height--; if(width==0) width++; if(height==0) height++;
                Polyline(dc,[new(x,y),new(x+width,y),new(x+width,y+height),new(x,y+height),new(x,y)],5);
            }
            else if(width==1) { MoveToEx(dc,x,y,IntPtr.Zero); LineTo(dc,x,y+height); }
            else if(height==1) { MoveToEx(dc,x,y,IntPtr.Zero); LineTo(dc,x+width,y); }
            else { SelectObject(dc,shape==EditorPaintShape.Ellipse ? GetStockObject(4) : brush); Ellipse(dc,x,y,x+width,y+height); }
            var points=new HashSet<TilePoint>();
            for(var row=bounds.Y;row<bounds.Y+bounds.Height;row++) for(var column=bounds.X;column<bounds.X+bounds.Width;column++)
            { var colour=GetPixel(dc,column,row); if(colour==uint.MaxValue) throw new Exception("GDI oracle GetPixel failed."); if(colour!=0) points.Add(new(column,row)); }
            return points;
        }
        finally
        {
            if(oldBitmap!=IntPtr.Zero) SelectObject(dc,oldBitmap); if(oldPen!=IntPtr.Zero) SelectObject(dc,oldPen); if(oldBrush!=IntPtr.Zero) SelectObject(dc,oldBrush);
            if(bitmap!=IntPtr.Zero) DeleteObject(bitmap); if(pen!=IntPtr.Zero) DeleteObject(pen); if(brush!=IntPtr.Zero) DeleteObject(brush); if(dc!=IntPtr.Zero) DeleteDC(dc);
        }
    }
    [StructLayout(LayoutKind.Sequential)] private readonly record struct Point(int X,int Y);
    [StructLayout(LayoutKind.Sequential)] private readonly record struct Rect(int Left,int Top,int Right,int Bottom);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateBitmap(int width,int height,uint planes,uint bits,IntPtr data);
    [DllImport("gdi32.dll")] private static extern IntPtr CreatePen(int style,int width,uint colour);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateSolidBrush(uint colour);
    [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr dc,IntPtr value);
    [DllImport("gdi32.dll")] private static extern IntPtr GetStockObject(int value);
    [DllImport("gdi32.dll")] private static extern uint GetPixel(IntPtr dc,int x,int y);
    [DllImport("gdi32.dll")] private static extern bool MoveToEx(IntPtr dc,int x,int y,IntPtr old);
    [DllImport("gdi32.dll")] private static extern bool LineTo(IntPtr dc,int x,int y);
    [DllImport("gdi32.dll")] private static extern bool SetPixelV(IntPtr dc,int x,int y,uint colour);
    [DllImport("gdi32.dll")] private static extern bool Ellipse(IntPtr dc,int left,int top,int right,int bottom);
    [DllImport("gdi32.dll")] private static extern bool Polyline(IntPtr dc,[In] Point[] points,int count);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr dc);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr value);
    [DllImport("user32.dll")] private static extern int FillRect(IntPtr dc,ref Rect rectangle,IntPtr brush);
}
