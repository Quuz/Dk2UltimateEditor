using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DK2MapEditor.Core.Editing;

/// <summary>Original Map_PaintTemporaryElement primitives on a private GDI
/// surface. No HWND, screen access, installed map or historical DLL is used.</summary>
public static class OriginalPaintRaster
{
    public static IReadOnlySet<TilePoint> Points(EditorPaintShape shape, TilePoint start, TilePoint end, TileRect rectangle)
    {
        if (!Enum.IsDefined(shape) || shape == EditorPaintShape.Brush) throw new ArgumentOutOfRangeException(nameof(shape));
        if (rectangle.Width is < 1 or > 1024 || rectangle.Height is < 1 or > 1024) throw new ArgumentOutOfRangeException(nameof(rectangle));
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("The original editor's shape mask requires Windows GDI.");
        var dc = CreateCompatibleDC(IntPtr.Zero); if (dc == IntPtr.Zero) throw Error();
        IntPtr bitmap = IntPtr.Zero, pen = IntPtr.Zero, oldBitmap = IntPtr.Zero, oldPen = IntPtr.Zero, oldBrush = IntPtr.Zero;
        try
        {
            var header = new BitmapInfo { Size = 40, Width = rectangle.Width, Height = -rectangle.Height, Planes = 1, BitCount = 32 };
            bitmap = CreateDIBSection(dc, ref header, 0, out var bits, IntPtr.Zero, 0);
            if (bitmap == IntPtr.Zero || bits == IntPtr.Zero) throw Error();
            oldBitmap = Select(dc, bitmap);
            pen = CreatePen(0, 0, 0xFFFFFF); if (pen == IntPtr.Zero) throw Error();
            oldPen = Select(dc, pen); oldBrush = Select(dc, GetStockObject(0)); // WHITE_BRUSH
            var pixels = new byte[checked(rectangle.Width * rectangle.Height * 4)]; Marshal.Copy(pixels, 0, bits, pixels.Length);
            bool ok;
            if (rectangle.Width == 1 && rectangle.Height == 1) ok = SetPixelV(dc, 0, 0, 0xFFFFFF);
            else switch (shape)
            {
                case EditorPaintShape.Line:
                    ok = MoveToEx(dc, start.X - rectangle.X, start.Y - rectangle.Y, IntPtr.Zero)
                        && LineTo(dc, end.X - rectangle.X, end.Y - rectangle.Y)
                        && SetPixelV(dc, end.X - rectangle.X, end.Y - rectangle.Y, 0xFFFFFF); break;
                case EditorPaintShape.Rectangle:
                    var width = Math.Max(1, rectangle.Width - 1); var height = Math.Max(1, rectangle.Height - 1);
                    ok = Polyline(dc, [new(0,0),new(width,0),new(width,height),new(0,height),new(0,0)],5); break;
                case EditorPaintShape.FilledRectangle:
                    var rect = new Rect(0,0,rectangle.Width,rectangle.Height);
                    ok = FillRect(dc, ref rect, GetStockObject(0)) != 0; break;
                default:
                    if (rectangle.Width == 1) ok = MoveToEx(dc,0,0,IntPtr.Zero) && LineTo(dc,0,rectangle.Height);
                    else if (rectangle.Height == 1) ok = MoveToEx(dc,0,0,IntPtr.Zero) && LineTo(dc,rectangle.Width,0);
                    else
                    {
                        Select(dc,GetStockObject(shape == EditorPaintShape.Ellipse ? 4 : 0)); // BLACK/WHITE_BRUSH
                        ok = Ellipse(dc,0,0,rectangle.Width,rectangle.Height);
                    }
                    break;
            }
            if (!ok || !GdiFlush()) throw Error();
            Marshal.Copy(bits,pixels,0,pixels.Length);
            var result = new HashSet<TilePoint>();
            for(var y=0;y<rectangle.Height;y++) for(var x=0;x<rectangle.Width;x++)
            {
                var index=(y*rectangle.Width+x)*4;
                if ((pixels[index] | pixels[index+1] | pixels[index+2]) != 0) result.Add(new(rectangle.X+x,rectangle.Y+y));
            }
            return result;
        }
        finally
        {
            if(oldBrush != IntPtr.Zero) SelectObject(dc,oldBrush);
            if(oldPen != IntPtr.Zero) SelectObject(dc,oldPen);
            if(oldBitmap != IntPtr.Zero) SelectObject(dc,oldBitmap);
            if(pen != IntPtr.Zero) DeleteObject(pen);
            if(bitmap != IntPtr.Zero) DeleteObject(bitmap);
            DeleteDC(dc);
        }
    }
    private static IntPtr Select(IntPtr dc,IntPtr value)
    {
        var previous=SelectObject(dc,value); if(previous==IntPtr.Zero || previous==new IntPtr(-1)) throw Error(); return previous;
    }
    private static Win32Exception Error()=>new(Marshal.GetLastWin32Error(),"Cannot create the original editor shape mask.");
    [StructLayout(LayoutKind.Sequential)] private struct BitmapInfo
    { public uint Size; public int Width,Height; public ushort Planes,BitCount; public uint Compression,SizeImage; public int XPels,YPels; public uint Used,Important,Colour; }
    [StructLayout(LayoutKind.Sequential)] private readonly record struct Point(int X,int Y);
    [StructLayout(LayoutKind.Sequential)] private readonly record struct Rect(int Left,int Top,int Right,int Bottom);
    [DllImport("gdi32.dll",SetLastError=true)] private static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll",SetLastError=true)] private static extern IntPtr CreateDIBSection(IntPtr dc,ref BitmapInfo info,uint usage,out IntPtr bits,IntPtr section,uint offset);
    [DllImport("gdi32.dll",SetLastError=true)] private static extern IntPtr CreatePen(int style,int width,uint colour);
    [DllImport("gdi32.dll",SetLastError=true)] private static extern IntPtr SelectObject(IntPtr dc,IntPtr value);
    [DllImport("gdi32.dll")] private static extern IntPtr GetStockObject(int index);
    [DllImport("gdi32.dll")] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool DeleteObject(IntPtr value);
    [DllImport("gdi32.dll")] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool DeleteDC(IntPtr dc);
    [DllImport("gdi32.dll",SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool Polyline(IntPtr dc,[In] Point[] points,int count);
    [DllImport("gdi32.dll",SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool MoveToEx(IntPtr dc,int x,int y,IntPtr old);
    [DllImport("gdi32.dll",SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool LineTo(IntPtr dc,int x,int y);
    [DllImport("gdi32.dll",SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool SetPixelV(IntPtr dc,int x,int y,uint colour);
    [DllImport("gdi32.dll",SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool Ellipse(IntPtr dc,int left,int top,int right,int bottom);
    [DllImport("gdi32.dll",SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool GdiFlush();
    [DllImport("user32.dll",SetLastError=true)] private static extern int FillRect(IntPtr dc,ref Rect rect,IntPtr brush);
}
