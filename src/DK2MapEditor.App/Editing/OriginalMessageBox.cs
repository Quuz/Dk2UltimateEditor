using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace DK2MapEditor.App.Editing;

/// <summary>Shared source 32px request images with the existing caller results.
/// Constructed only when a caller actually presents a modal message.</summary>
public static class OriginalMessageBox
{
    public static MessageBoxResult Show(string message,string caption="",MessageBoxButton buttons=MessageBoxButton.OK,MessageBoxImage image=MessageBoxImage.None,MessageBoxResult defaultResult=MessageBoxResult.None)
        =>ShowCore(null,message,caption,buttons,image,defaultResult,null);
    public static MessageBoxResult Show(Window? owner,string message,string caption="",MessageBoxButton buttons=MessageBoxButton.OK,MessageBoxImage image=MessageBoxImage.None,MessageBoxResult defaultResult=MessageBoxResult.None)
        =>ShowCore(owner,message,caption,buttons,image,defaultResult,null);
    public static MessageBoxResult ShowWithIcon(int resource,Window? owner,string message,string caption,MessageBoxButton buttons,MessageBoxImage image)
        =>ShowCore(owner,message,caption,buttons,image,MessageBoxResult.None,resource);
    private static MessageBoxResult ShowCore(Window? owner,string message,string caption,MessageBoxButton buttons,MessageBoxImage image,MessageBoxResult defaultResult,int? resource)
    {
        var window=new RequestWindow(message,caption,buttons,image,defaultResult,resource);if(owner is not null)window.Owner=owner;
        window.ShowDialog();return window.Result;
    }
    private sealed class RequestWindow:Window
    {
        public MessageBoxResult Result{get;private set;}
        internal RequestWindow(string message,string caption,MessageBoxButton buttons,MessageBoxImage image,MessageBoxResult defaultResult,int? resource)
        {
            var kind=buttons.ToString();var results=OriginalMessagePolicy.Buttons(kind);var selected=OriginalMessagePolicy.Default(kind,defaultResult.ToString());
            Result=Enum.Parse<MessageBoxResult>(OriginalMessagePolicy.Close(kind));
            Title=caption;Width=560;MaxHeight=720;MinWidth=340;SizeToContent=SizeToContent.Height;ShowInTaskbar=false;WindowStartupLocation=WindowStartupLocation.CenterOwner;ResizeMode=ResizeMode.CanResize;
            SetResourceReference(BackgroundProperty,"Brush.Background");SetResourceReference(ForegroundProperty,"Brush.Text");
            var panel=new DockPanel{Margin=new(16)};Content=panel;var footer=new WrapPanel{HorizontalAlignment=HorizontalAlignment.Right,Margin=new(0,12,0,0)};DockPanel.SetDock(footer,Dock.Bottom);panel.Children.Add(footer);
            Button? focus=null;
            foreach(var result in results)
            {
                var button=new Button{MinWidth=84,IsDefault=result==selected};button.SetResourceReference(ContentControl.ContentProperty,OriginalMessagePolicy.Label(result));
                button.Click+=(_,_)=>{Result=Enum.Parse<MessageBoxResult>(result);Close();};footer.Children.Add(button);if(result==selected)focus=button;
            }
            var body=new DockPanel();panel.Children.Add(body);var iconID=resource??OriginalMessagePolicy.Icon(image.ToString());
            if(iconID is { } id)
            {
                var name=id switch{75=>"Legacy.MessageBox.75",80=>"Legacy.Request.Information",81=>"Legacy.Request.Warning",82=>"Legacy.Request.Error",83=>"Legacy.Request.Question",_=>throw new ArgumentOutOfRangeException(nameof(resource))};
                using var stream=typeof(OriginalMessageBox).Assembly.GetManifestResourceStream(name)??throw new InvalidOperationException("Missing source request icon: "+name);
                var bitmap=BitmapDecoder.Create(stream,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad).Frames.First(frame=>frame.PixelWidth==32&&frame.PixelHeight==32);bitmap.Freeze();
                var icon=new Image{Source=bitmap,Width=32,Height=32,Margin=new(0,4,14,0),VerticalAlignment=VerticalAlignment.Top};DockPanel.SetDock(icon,Dock.Left);body.Children.Add(icon);
            }
            body.Children.Add(new ScrollViewer{VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Content=new TextBlock{Text=message,TextWrapping=TextWrapping.Wrap,Margin=new(0,4,0,4)}});
            PreviewKeyDown+=(_,e)=>{if(e.Key==Key.Escape){e.Handled=true;Close();}};
            Loaded+=(_,_)=>{MessageBeep(image==MessageBoxImage.None?0u:(uint)image);focus?.Focus();};
        }
    }
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]private static extern bool MessageBeep(uint type);
}
