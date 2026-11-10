using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public sealed class PatrolPauseWindow : Window
{
    private readonly NativeMapDocument document;
    private readonly EditorPatrolPauseSession session;
    private readonly Func<bool> ownerCurrent;
    private readonly TextBox pause=new(){TextAlignment=TextAlignment.Right,Margin=new(4,12,4,8)};
    private readonly Button accept=new(){IsDefault=true};
    private readonly TextBlock status=new(){TextWrapping=TextWrapping.Wrap,Margin=new(4)};
    private Window? rangeNotice;
    private bool closed,closing,applying;
    public EditorPatrolManagerRoute? Result {get;private set;}
    public Exception? CommitFailure {get;private set;}
    public PatrolPauseWindow(NativeMapDocument document,EditorPatrolPauseSession session,Func<bool> ownerCurrent)
    {
        this.document=document;this.session=session;this.ownerCurrent=ownerCurrent;
        NameScope.SetNameScope(this,new NameScope());SetResourceReference(TitleProperty,"Editor.PatrolPauseTitle");
        SetResourceReference(BackgroundProperty,"Brush.Background");SetResourceReference(ForegroundProperty,"Brush.Text");
        Width=510;MinWidth=400;SizeToContent=SizeToContent.Height;ResizeMode=ResizeMode.NoResize;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var panel=new StackPanel{Margin=new(16)};Content=panel;
        panel.Children.Add(Message("Legacy.Request.Question","Editor.PatrolPausePrompt"));
        pause.Text=session.Text;pause.PreviewTextInput+=(_,args)=>args.Handled=!args.Text.All(char.IsAsciiDigit);
        pause.TextChanged+=(_,_)=>{if(Current){session.SetText(pause.Text);status.ClearValue(TextBlock.TextProperty);}};
        panel.Children.Add(pause);
        var bounds=new Grid{Margin=new(4)};bounds.ColumnDefinitions.Add(new());bounds.ColumnDefinitions.Add(new());panel.Children.Add(bounds);
        var min=new StackPanel{Margin=new(0,0,8,0)};
        var max=new StackPanel{Margin=new(8,0,0,0)};
        var minLabel=new TextBlock();var maxLabel=new TextBlock();
        minLabel.SetResourceReference(TextBlock.TextProperty,"Editor.PatrolPauseMinimum");maxLabel.SetResourceReference(TextBlock.TextProperty,"Editor.PatrolPauseMaximum");
        var minValue=new TextBox{Text="0",IsReadOnly=true,TextAlignment=TextAlignment.Right};
        var maxValue=new TextBox{Text="250",IsReadOnly=true,TextAlignment=TextAlignment.Right};
        min.Children.Add(minLabel);min.Children.Add(minValue);max.Children.Add(maxLabel);max.Children.Add(maxValue);
        Grid.SetColumn(max,1);bounds.Children.Add(min);bounds.Children.Add(max);
        panel.Children.Add(status);
        var buttons=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};panel.Children.Add(buttons);
        accept.SetResourceReference(ContentControl.ContentProperty,"Editor.PatrolWarningOK");accept.Click+=(_,_)=>TryAccept();buttons.Children.Add(accept);
        var cancel=new Button{IsCancel=true};cancel.SetResourceReference(ContentControl.ContentProperty,"Editor.Cancel");cancel.Click+=(_,_)=>Close();buttons.Children.Add(cancel);
        RegisterName("PatrolPauseText",pause);RegisterName("PatrolPauseAccept",accept);RegisterName("PatrolPauseCancel",cancel);
        PreviewKeyDown+=(_,args)=>{if(args.Key==Key.Escape){args.Handled=true;Close();}};
        document.Changed+=DocumentChanged;
        Loaded+=(_,_)=>{PlaceAtMouse();Refresh();pause.Focus();pause.SelectAll();};
        Closing+=(_,_)=>{closing=true;rangeNotice?.Close();};
        Closed+=(_,_)=>{closed=true;session.Dispose();document.Changed-=DocumentChanged;};
        Refresh();
    }
    private bool Current=>!closed&&!closing&&!applying&&ownerCurrent()&&session.IsCurrent;
    private void Refresh()
    {
        var current=Current;accept.IsEnabled=pause.IsEnabled=current;
        if(!current&&!closed&&!applying)status.SetResourceReference(TextBlock.TextProperty,"Editor.PatrolPauseStale");
    }
    private void DocumentChanged(object? sender,EventArgs args){if(closed||applying)return;rangeNotice?.Close();Refresh();}
    public bool TryAccept()
    {
        if(!Current){Refresh();return false;}
        if(session.Range!=EditorPatrolPauseRange.Valid)
        {
            var window=new Window{Owner=this,Width=440,MinWidth=350,SizeToContent=SizeToContent.Height,
                ResizeMode=ResizeMode.NoResize,WindowStartupLocation=WindowStartupLocation.CenterOwner};
            window.SetResourceReference(TitleProperty,"Editor.PatrolPauseTitle");window.SetResourceReference(BackgroundProperty,"Brush.Background");window.SetResourceReference(ForegroundProperty,"Brush.Text");
            var panel=new StackPanel{Margin=new(16)};window.Content=panel;
            panel.Children.Add(Message("Legacy.Request.Warning",session.Range==EditorPatrolPauseRange.BelowMinimum?"Editor.PatrolPauseTooSmall":"Editor.PatrolPauseTooLarge"));
            var ok=new Button{IsDefault=true,IsCancel=true,HorizontalAlignment=HorizontalAlignment.Right};ok.SetResourceReference(ContentControl.ContentProperty,"Editor.PatrolWarningOK");ok.Click+=(_,_)=>window.Close();panel.Children.Add(ok);
            window.Loaded+=(_,_)=>{MessageBeep(0x30);ok.Focus();};
            rangeNotice=window;try{window.ShowDialog();}finally{if(ReferenceEquals(rangeNotice,window))rangeNotice=null;}
            if(Current){pause.Focus();pause.SelectAll();}return false;
        }
        try{applying=true;Result=session.Accept();if(!closed)Close();return true;}
        catch(EditorHistoryMemoryException error)
        {
            if(session.IsAccepted){CommitFailure=error;if(!closed)Close();return true;}
            if(!closed)Close();throw;
        }
        catch(Exception error)when(error is ArgumentException or InvalidOperationException or InvalidDataException or OverflowException)
        {
            if(session.IsAccepted){CommitFailure=error;if(!closed)Close();return true;}
            status.SetResourceReference(TextBlock.TextProperty,"Editor.PatrolFailed");status.ToolTip=error.Message;return false;
        }
        finally{applying=false;if(!closed)Refresh();}
    }
    private static DockPanel Message(string iconName,string textKey)
    {
        var panel=new DockPanel();
        using(var stream=typeof(PatrolPauseWindow).Assembly.GetManifestResourceStream(iconName)??throw new InvalidDataException("Original request icon is missing."))
        {var icon=BitmapFrame.Create(stream,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad);icon.Freeze();panel.Children.Add(new Image{Source=icon,Width=32,Height=32,Margin=new(4,4,14,4),VerticalAlignment=VerticalAlignment.Top});}
        var text=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new(4)};text.SetResourceReference(TextBlock.TextProperty,textKey);panel.Children.Add(text);return panel;
    }
    private void PlaceAtMouse()
    {
        var handle=new WindowInteropHelper(this).Handle;
        if(handle==IntPtr.Zero||!GetCursorPos(out var point)||!GetWindowRect(handle,out var bounds))return;
        var info=new MonitorInfo{Size=Marshal.SizeOf<MonitorInfo>()};if(!GetMonitorInfo(MonitorFromPoint(point,2),ref info))return;
        var position=EditorPatrolWarningPolicy.PlaceAtCursor(point.X,point.Y,bounds.Right-bounds.Left,bounds.Bottom-bounds.Top,
            info.Work.Left,info.Work.Top,info.Work.Right-info.Work.Left,info.Work.Bottom-info.Work.Top);
        SetWindowPos(handle,IntPtr.Zero,position.X,position.Y,0,0,0x0001|0x0004|0x0010);
    }
    [StructLayout(LayoutKind.Sequential)] private struct NativePoint {public int X,Y;}
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect {public int Left,Top,Right,Bottom;}
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo {public int Size;public NativeRect Monitor,Work;public uint Flags;}
    [DllImport("user32.dll")] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool GetCursorPos(out NativePoint point);
    [DllImport("user32.dll")] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool GetWindowRect(IntPtr window,out NativeRect bounds);
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromPoint(NativePoint point,uint flags);
    [DllImport("user32.dll",CharSet=CharSet.Auto)] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool GetMonitorInfo(IntPtr monitor,ref MonitorInfo info);
    [DllImport("user32.dll")] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
    [DllImport("user32.dll")] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool MessageBeep(uint type);
}
