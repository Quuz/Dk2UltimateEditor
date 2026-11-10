using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public sealed class CreatureJobsWarningWindow : Window
{
    private readonly NativeMapDocument document;
    private readonly CreatureJobsWarningRequest request;
    private readonly CheckBox remember=new(){Margin=new(4,14,4,10)};
    private readonly Button proceed=new(){IsDefault=true},cancel=new();
    private readonly TextBlock message=new(){TextWrapping=TextWrapping.Wrap,Margin=new(4)};
    private bool closed,closing;
    public bool Accepted=>request.IsAccepted;
    public CreatureJobsWarningWindow(NativeMapDocument document,CreatureJobsWarningRequest request)
    {
        this.document=document;this.request=request;
        NameScope.SetNameScope(this,new NameScope());SetResourceReference(TitleProperty,"CreatureGlobals.Title");
        SetResourceReference(BackgroundProperty,"Brush.Background");SetResourceReference(ForegroundProperty,"Brush.Text");
        Width=570;MinWidth=440;SizeToContent=SizeToContent.Height;ResizeMode=ResizeMode.NoResize;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var panel=new StackPanel{Margin=new(16)};Content=panel;
        var row=new DockPanel();panel.Children.Add(row);
        using(var stream=typeof(CreatureJobsWarningWindow).Assembly.GetManifestResourceStream("Legacy.Request.Warning")??throw new InvalidDataException("Original request warning icon is missing."))
        {var icon=BitmapFrame.Create(stream,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad);icon.Freeze();row.Children.Add(new Image{Source=icon,Width=32,Height=32,Margin=new(4,4,14,4),VerticalAlignment=VerticalAlignment.Top});}
        row.Children.Add(message);
        var text=new TextBlock{TextWrapping=TextWrapping.Wrap};text.SetResourceReference(TextBlock.TextProperty,"CreatureGlobals.JobsRemember");remember.Content=text;panel.Children.Add(remember);
        remember.Checked+=(_,_)=>{Refresh();proceed.Focus();};remember.Unchecked+=(_,_)=>Refresh();
        var buttons=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};panel.Children.Add(buttons);
        proceed.SetResourceReference(ContentControl.ContentProperty,"CreatureGlobals.JobsContinue");proceed.Click+=(_,_)=>Continue();buttons.Children.Add(proceed);
        cancel.SetResourceReference(ContentControl.ContentProperty,"GlobalVariables.Cancel");cancel.Click+=(_,_)=>{if(Current&&request.Cancel(remember.IsChecked==true))Close();};buttons.Children.Add(cancel);
        RegisterName("CreatureJobsWarningRemember",remember);RegisterName("CreatureJobsWarningContinue",proceed);RegisterName("CreatureJobsWarningCancel",cancel);RegisterName("CreatureJobsWarningMessage",message);
        PreviewKeyDown+=(_,args)=>{if(args.Key==Key.Escape){args.Handled=true;Close();}};
        document.Changed+=Changed;LocalizationService.LanguageChanged+=LanguageChanged;
        Loaded+=(_,_)=>{Refresh();MessageBeep(0x30);proceed.Focus();};
        Closing+=(_,_)=>closing=true;
        Closed+=(_,_)=>{closed=true;request.Dispose();document.Changed-=Changed;LocalizationService.LanguageChanged-=LanguageChanged;};
        Refresh();
    }
    private bool Current=>!closed&&!closing&&request.IsCurrent;
    private void Refresh()
    {
        var current=Current;proceed.IsEnabled=remember.IsEnabled=current;cancel.IsEnabled=current&&request.CanCancel(remember.IsChecked==true);
        var group=LocalizationService.Get("Editor.Definitions.Jobs."+request.Warning.Group);
        message.Text=current?string.Format(CultureInfo.InvariantCulture,LocalizationService.Get("CreatureGlobals.JobWarning"),group,request.Warning.Total):LocalizationService.Get("GlobalVariables.Stale");
    }
    private void Changed(object? sender,EventArgs args){if(!closed)Refresh();}
    private void LanguageChanged(object? sender,EventArgs args){if(!closed)Refresh();}
    public bool Continue()
    {
        if(!Current){Refresh();return false;}
        request.Continue(remember.IsChecked==true);Close();return true;
    }
    [DllImport("user32.dll")] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool MessageBeep(uint type);
}
