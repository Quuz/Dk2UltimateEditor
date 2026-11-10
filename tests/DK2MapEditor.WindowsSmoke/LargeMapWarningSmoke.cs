using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    // Prepared and compiled only: this harness constructs real WPF windows.
    private static void CheckLargeMapSharing(bool native,string language)
    {
        var shared=new EditorRequestPreferences();var initial=new NewMapOptions("LargeUI","LargeUI",121,121,NativeFormat:native);
        var dialog=new NewMapDialog(null,initial,shared);ShowManagerCallerWindow(dialog);var prompts=0;
        try
        {
            dialog.LargeMapPresenter=window=>
            {
                prompts++;Expect(window.Title==LocalizationService.Get("LargeMap.Title")&&window.Request.Width==121&&window.Request.Height==121,"Large-map request lost translated title or dimensions");
                ((CheckBox)window.FindName("LargeMapRemember")).IsChecked=true;
                Expect(!((Button)window.FindName("LargeMapNo")).IsEnabled,"Checked default left No enabled");window.Close();
            };
            Expect(!dialog.TryAcceptOptions()&&shared.Flags==0,"Closing checked large-map question remembered Yes");
            dialog.LargeMapPresenter=window=>
            {
                prompts++;LocalizationService.SetLanguage(language=="en"?"ru":"en");
                Expect(((TextBlock)window.FindName("LargeMapMessage")).Text==LocalizationService.Get("LargeMap.Question"),"Open information question translation failed");
                ((CheckBox)window.FindName("LargeMapRemember")).IsChecked=true;Expect(window.Accept(),"Large-map Yes refused");
            };
            Expect(dialog.TryAcceptOptions()&&prompts==2&&shared.Flags==0x20,"New-map remembered approval failed");LocalizationService.SetLanguage(language);
            var source=NativeMapDocument.CreateNewMap(new("LargeUI","LargeUI",16,16,NativeFormat:native));var before=source.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
            var parent=new MapPropertiesWindow(source,()=>true,()=>true,new(),shared);ShowManagerCallerWindow(parent);
            ((TextBox)parent.FindName("MapPropertiesWidth")).Text="130";((TextBox)parent.FindName("MapPropertiesHeight")).Text="130";
            parent.LargeMapPresenter=_=>throw new Exception("New-map remembered reply did not reach map properties");
            Expect(parent.ApplyProperties()&&source.Width==130,"Shared resize approval did not apply");source.Undo();
            Expect(before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value))&&shared.Flags==0x20,"Undo changed process preference or map bytes");
            ((Button)dialog.FindName("NewLargeReset")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Expect(shared.Flags==0,"New-map reset did not clear the shared bit");
            dialog.LargeMapPresenter=window=>
            {
                prompts++;var width=(TextBox)dialog.FindName("NewWidth");var old=width.Text;width.Text="122";width.Text=old;
                Expect(!window.Accept(),"Restoring dimensions revived an old question");window.Close();
            };
            Expect(!dialog.TryAcceptOptions()&&shared.Flags==0,"Changed new-map input reused approval");
            var invalidated=false;var guarded=new NewMapDialog(null,initial,shared,()=>!invalidated);ShowManagerCallerWindow(guarded);
            guarded.LargeMapPresenter=window=>{invalidated=true;Expect(!window.Accept(),"Old caller accepted after document context changed");window.Close();};
            Expect(!guarded.TryAcceptOptions(),"Closed caller produced accepted new-map options");guarded.Close();
            Console.WriteLine($"PASS WPF shared large-map information request/reset/lifetime {native} {language}; physical cursor/audio/DPI acceptance remains open");
        }
        finally{LocalizationService.SetLanguage(language);dialog.Close();}
    }
}
