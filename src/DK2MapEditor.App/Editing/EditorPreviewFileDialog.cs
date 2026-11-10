using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;
using N=DK2MapEditor.App.Editing.NativePreviewDialogInterop;

namespace DK2MapEditor.App.Editing;

/// <summary>Single native Explorer-style chooser with the source preview pane.
/// Selection notifications only read detached files; OK is the sole path result.</summary>
public sealed class EditorPreviewFileDialog(EditorFileDialogKind kind,bool save=false)
{
    public string Title{get;set;}="";public string Filter{get;set;}="All files|*.*";public string FileName{get;set;}="";public string? InitialDirectory{get;set;}public string? DefaultExt{get;set;}
    public bool CheckFileExists{get;set;}public bool AddExtension{get;set;}=true;public bool OverwritePrompt{get;set;}=true;public bool Multiselect{get;set;}public int FilterIndex{get;set;}=1;
    public bool? ShowDialog(Window? owner=null)
    {
        if(Multiselect)throw new NotSupportedException("This preview chooser selects one file.");
        const int capacity=32768;var buffer=Marshal.AllocHGlobal(capacity*2);var template=IntPtr.Zero;
        try
        {
            if(FileName.Length>=capacity)throw new ArgumentException("The initial filename is too long.");Marshal.Copy(new byte[capacity*2],0,buffer,capacity*2);Marshal.Copy((FileName+'\0').ToCharArray(),0,buffer,FileName.Length+1);
            var data=FilePreviewDialogPlan.Template();template=N.GlobalAlloc(0x40,(UIntPtr)data.Length);if(template==IntPtr.Zero)throw new OutOfMemoryException();Marshal.Copy(data,0,template,data.Length);
            using var pane=new PreviewPane(kind,Filter,DefaultExt,AddExtension);N.Hook callback=pane.Callback;
            var request=new N.OpenFileName{Size=(uint)Marshal.SizeOf<N.OpenFileName>(),Owner=owner is null?IntPtr.Zero:new WindowInteropHelper(owner).Handle,Instance=template,Filter=FilePreviewDialogPlan.NativeFilter(Filter),FilterIndex=(uint)Math.Max(1,FilterIndex),File=buffer,MaxFile=capacity,InitialDirectory=InitialDirectory,Title=Title,Flags=FilePreviewDialogPlan.Flags(save,CheckFileExists,OverwritePrompt),DefaultExtension=AddExtension?FilePreviewDialogPlan.Extension(Filter,FilterIndex,DefaultExt):null,Callback=callback};
            bool accepted;uint error;ComponentDispatcher.PushModal();try{accepted=save?N.GetSaveFileNameW(ref request):N.GetOpenFileNameW(ref request);error=accepted?0:N.CommDlgExtendedError();}finally{ComponentDispatcher.PopModal();GC.KeepAlive(callback);}
            if(!accepted){if(error!=0)throw new InvalidOperationException("File chooser failed: 0x"+error.ToString("X",CultureInfo.InvariantCulture));return false;}
            FileName=Marshal.PtrToStringUni(buffer)??"";FilterIndex=(int)request.FilterIndex;return true;
        }
        finally{if(template!=IntPtr.Zero)N.GlobalFree(template);Marshal.FreeHGlobal(buffer);}
    }
    private sealed class PreviewPane:IDisposable
    {
        private readonly EditorFilePreviewKind kind;private readonly string filter;private readonly string? fallback;private readonly bool extension;
        private readonly FilePreviewEpoch epoch=new();private readonly SemaphoreSlim reader=new(1,1);private readonly ConcurrentQueue<(long Ticket,EditorFilePreviewSnapshot? Data,string? Error,bool Missing)> completed=new();
        private readonly List<IntPtr> strings=[];private CancellationTokenSource? pending;private IntPtr window,title,image,details,bitmap;private EditorFilePreviewSnapshot? snapshot;private byte[]? pixels;private bool disposed;
        internal PreviewPane(EditorFileDialogKind kind,string filter,string? fallback,bool extension)
        {this.kind=kind switch{EditorFileDialogKind.Map=>EditorFilePreviewKind.Map,EditorFileDialogKind.Area=>EditorFilePreviewKind.Area,EditorFileDialogKind.Thumbnail=>EditorFilePreviewKind.Bitmap,_=>throw new ArgumentOutOfRangeException(nameof(kind))};this.filter=filter;this.fallback=fallback;this.extension=extension;}
        internal UIntPtr Callback(IntPtr hwnd,uint message,UIntPtr wParam,IntPtr lParam)
        {
            try
            {
                if(message==0x110){window=hwnd;title=Control(9001,0);image=Control(9002,13);details=Control(9003,0);N.SetWindowTextW(title,LocalizationService.Get("FilePreview.Title"));N.SetWindowTextW(details,LocalizationService.Get("FilePreview.Empty"));Layout();}
                else if(message==5&&window!=IntPtr.Zero)Layout();
                else if(message==0x4E&&lParam!=IntPtr.Zero)
                {
                    var notification=Marshal.PtrToStructure<N.Notification>(lParam);var code=unchecked((int)notification.Header.Code);
                    if(code==-607&&extension&&notification.FileName!=IntPtr.Zero)
                    {var index=Marshal.ReadInt32(notification.FileName,(int)Marshal.OffsetOf<N.OpenFileName>(nameof(N.OpenFileName.FilterIndex)));var value=FilePreviewDialogPlan.Extension(filter,index,fallback);if(value is not null){var text=Marshal.StringToHGlobalUni(value);strings.Add(text);N.SendMessageW(N.GetParent(hwnd),0x46A,UIntPtr.Zero,text);}}
                    if(code is -601 or -602 or -603 or -607)ReadSelection();
                }
                else if(message==0x8001)ApplyCompleted();
                else if(message==0x2B&&lParam!=IntPtr.Zero){var item=Marshal.PtrToStructure<N.DrawItem>(lParam);if(item.ID==9002){Draw(item);return (UIntPtr)1;}}
                else if(message==2){epoch.Dispose();pending?.Cancel();window=IntPtr.Zero;}
            }
            catch(Exception error)when(error is ArgumentException or InvalidOperationException or System.IO.IOException or OverflowException)
            {if(details!=IntPtr.Zero)N.SetWindowTextW(details,LocalizationService.Get("FilePreview.Unavailable")+"\r\n"+error.Message);}
            return UIntPtr.Zero;
        }
        private IntPtr Control(int id,uint style)
        {
            var control=N.CreateWindowExW(0,"STATIC","",0x50000000|style,0,0,1,1,window,(IntPtr)id,IntPtr.Zero,IntPtr.Zero);if(control==IntPtr.Zero)throw new InvalidOperationException("Cannot create preview control.");
            var font=N.SendMessageW(N.GetParent(window),0x31,UIntPtr.Zero,IntPtr.Zero);if(font==IntPtr.Zero)font=N.GetStockObject(17);N.SendMessageW(control,0x30,(UIntPtr)font,IntPtr.Zero);return control;
        }
        private void Layout()
        {
            if(title==IntPtr.Zero||!N.GetClientRect(window,out var bounds))return;var unit=new N.Rect{Left=8,Top=4,Right=100,Bottom=100};N.MapDialogRect(window,ref unit);
            var heading=new N.Rect{Bottom=12};N.MapDialogRect(window,ref heading);
            var x=unit.Left;var y=unit.Top;var top=y*2+heading.Bottom;var square=Math.Max(1,Math.Min(unit.Right-x,bounds.Bottom-top-y*2));
            N.MoveWindow(title,x,y,Math.Max(1,bounds.Right-x*2),heading.Bottom,true);N.MoveWindow(image,x,top,square,square,true);N.MoveWindow(details,x*2+square,top,Math.Max(1,bounds.Right-square-x*3),Math.Max(1,bounds.Bottom-top-y),true);
        }
        private void ClearImage(){snapshot=null;pixels=null;if(bitmap!=IntPtr.Zero){N.DeleteObject(bitmap);bitmap=IntPtr.Zero;}if(image!=IntPtr.Zero)N.InvalidateRect(image,IntPtr.Zero,true);}
        private void ReadSelection()
        {
            var path=new StringBuilder(32768);var length=N.SendMessageW(N.GetParent(window),0x465,(UIntPtr)path.Capacity,path).ToInt64();var ticket=epoch.Begin();pending?.Cancel();pending?.Dispose();pending=null;ClearImage();
            if(length<=1||length>path.Capacity){N.SetWindowTextW(details,LocalizationService.Get("FilePreview.Empty"));return;}
            var selected=path.ToString();N.SetWindowTextW(details,LocalizationService.Get("FilePreview.Loading"));var cancel=new CancellationTokenSource();pending=cancel;var token=cancel.Token;
            _=Task.Run(async()=>
            {
                try
                {
                    await reader.WaitAsync(token);EditorFilePreviewSnapshot? data=null;string? error=null;var missing=false;
                    try{token.ThrowIfCancellationRequested();missing=!System.IO.File.Exists(selected);if(!missing)data=EditorFilePreview.Read(selected,kind,token);}
                    catch(Exception failure)when(failure is System.IO.IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or FormatException or OverflowException){error=failure.Message;}
                    finally{reader.Release();}
                    if(epoch.IsCurrent(ticket)&&!token.IsCancellationRequested){completed.Enqueue((ticket,data,error,missing));var target=Volatile.Read(ref window);if(target!=IntPtr.Zero)N.PostMessageW(target,0x8001,UIntPtr.Zero,IntPtr.Zero);}
                }
                catch(OperationCanceledException){}
            });
        }
        private void ApplyCompleted()
        {
            while(completed.TryDequeue(out var result))
            {
                if(!epoch.IsCurrent(result.Ticket))continue;ClearImage();snapshot=result.Data;
                if(snapshot is null){N.SetWindowTextW(details,LocalizationService.Get(result.Missing?"FilePreview.NotExisting":"FilePreview.Unavailable")+(result.Error is null?"":"\r\n"+result.Error));continue;}
                if(snapshot.Bitmap){bitmap=N.LoadImageW(IntPtr.Zero,snapshot.Path,0,0,0,0x10);if(bitmap==IntPtr.Zero){N.SetWindowTextW(details,LocalizationService.Get("FilePreview.Unavailable"));snapshot=null;continue;}}
                else pixels=snapshot.Raster?.Bgra.ToArray();
                var text=string.Format(CultureInfo.CurrentCulture,LocalizationService.Get("FilePreview.Dimensions"),snapshot.Width,snapshot.Height);
                if(!snapshot.Bitmap)
                {
                    text+="\r\n\r\n";
                    if(snapshot.Counts is not { } counts)text+=LocalizationService.Get("FilePreview.CountsUnavailable");
                    else text+=string.Join("\r\n",new[]{("Creatures",counts.Creatures),("Doors",counts.Doors),("Traps",counts.Traps),("Objects",counts.Objects),("SpecialItems",counts.SpecialItems)}.Select(row=>LocalizationService.Get("Editor.Statistics."+row.Item1)+": "+row.Item2.ToString(CultureInfo.CurrentCulture)));
                }
                N.SetWindowTextW(details,text);N.InvalidateRect(image,IntPtr.Zero,true);
            }
        }
        private void Draw(N.DrawItem item)
        {
            var rect=item.Rect;N.DrawFrameControl(item.DC,ref rect,4,0x210);rect.Left+=4;rect.Top+=4;rect.Right-=4;rect.Bottom-=4;N.FillRect(item.DC,ref rect,N.GetStockObject(4));
            if(snapshot is null)return;var size=Math.Min(rect.Right-rect.Left,rect.Bottom-rect.Top);if(size<=0)return;
            var width=size;var height=size;if(snapshot.Width<snapshot.Height)width=Math.Max(1,(int)((long)size*snapshot.Width/snapshot.Height));else height=Math.Max(1,(int)((long)size*snapshot.Height/snapshot.Width));var x=rect.Left+(rect.Right-rect.Left-width)/2;var y=rect.Top+(rect.Bottom-rect.Top-height)/2;N.SetStretchBltMode(item.DC,3);
            if(bitmap!=IntPtr.Zero)
            {var dc=N.CreateCompatibleDC(item.DC);if(dc==IntPtr.Zero)return;var old=N.SelectObject(dc,bitmap);try{if(old!=IntPtr.Zero&&old!=(IntPtr)(-1))N.StretchBlt(item.DC,x,y,width,height,dc,0,0,snapshot.Width,snapshot.Height,0x00CC0020);}finally{if(old!=IntPtr.Zero&&old!=(IntPtr)(-1))N.SelectObject(dc,old);N.DeleteDC(dc);}}
            else if(pixels is not null){var info=new N.BitmapInfo{Size=40,Width=snapshot.Width,Height=-snapshot.Height,Planes=1,Bits=32};N.StretchDIBits(item.DC,x,y,width,height,0,0,snapshot.Width,snapshot.Height,pixels,ref info,0,0x00CC0020);}
        }
        public void Dispose(){if(disposed)return;disposed=true;epoch.Dispose();pending?.Cancel();pending?.Dispose();pending=null;window=IntPtr.Zero;ClearImage();foreach(var text in strings)Marshal.FreeHGlobal(text);}
    }
}
