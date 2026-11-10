using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    // Prepared for later manual execution. Never run this as a CLI check.
    private static void CheckPlayerGoldNotice(bool native,string language)
    {
        NativeMapDocument New()=>NativeMapDocument.CreateNewMap(new("GoldUI","GoldUI",24,20,NativeFormat:native));
        var document=New();var workspace=new EditorWorkspace{Document=document};var host=new Window{Content=workspace};ShowManagerCallerWindow(host);
        Control<TabControl>(workspace,"WorkspaceTabs").SelectedIndex=7;
        void Select(EditorWorkspace target)
        {
            var picker=Control<ComboBox>(target,"PlayerPicker");picker.SelectedItem=picker.Items.Cast<object>().Single(item=>Get<PlayerSettingsRow>(item,"Player").ID==3);
        }
        void Draft(EditorWorkspace target,int gold,string? name=null)
        {
            Select(target);Control<TextBox>(target,"PlayerGold").Text=gold.ToString();
            if(name is not null)Control<TextBox>(target,"PlayerName").Text=name;
        }
        void Submit(EditorWorkspace target)
        {
            Control<CheckBox>(target,"PlayerConfirmed").IsChecked=true;
            typeof(EditorWorkspace).GetMethod("PlayerEdit_Click",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(target,[new Button{Tag="General"},new RoutedEventArgs()]);
        }
        void Reset()=>typeof(EditorWorkspace).GetMethod("PlayerGoldReset_Click",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(workspace,[workspace,new RoutedEventArgs()]);
        long Gold(NativeMapDocument map)=>map.PlayerSettingsRows().Single(row=>row.ID==3).Gold;
        var original=document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());var prompts=0;
        Draft(workspace,4001,"Close still saves");
        workspace.PlayerGoldPresenter=window=>
        {
            prompts++;Expect(window.Title==LocalizationService.Get("PlayerGold.Title")&&((Button)window.FindName("PlayerGoldNoticeOK")).IsDefault,"Gold information lacks translated title/default OK");
            ((CheckBox)window.FindName("PlayerGoldNoticeRemember")).IsChecked=true;window.Close();
        };
        Submit(workspace);Expect(prompts==1&&Gold(document)==4001,"Checked close vetoed Save or clamped gold");document.Undo();Expect(original.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Gold Save Undo lost bytes");
        Draft(workspace,4002,"Remember");workspace.PlayerGoldPresenter=window=>
        {
            prompts++;LocalizationService.SetLanguage(language=="en"?"ru":"en");Expect(((TextBlock)window.FindName("PlayerGoldNoticeMessage")).Text==LocalizationService.Get("PlayerGold.Message"),"Open gold notice did not translate");
            ((CheckBox)window.FindName("PlayerGoldNoticeRemember")).IsChecked=true;window.Acknowledge();
        };
        Submit(workspace);Expect(prompts==2&&Gold(document)==4002,"Checked close accidentally remembered or OK failed");LocalizationService.SetLanguage(language);
        Draft(workspace,4003);workspace.PlayerGoldPresenter=window=>{prompts++;window.Close();};Submit(workspace);Expect(prompts==2&&Gold(document)==4003,"Remembered information asked again");
        document.Undo();document.Undo();Expect(original.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Remembered preference changed map Undo");

        // A private map-properties child shares the root preference store, even
        // after its outer Cancel. Its informational window is not an approval.
        Reset();document.Paint([new(2,2),new(2,3)],new(10,3,1));document.SetMapVariables([new(0x18,100)]);
        var liveRow=document.PlayerSettingsRows().Single(row=>row.ID==3);document.SetPlayerSettings([new(3,liveRow.Name,500,liveRow.Computer,liveRow.Health is { } h?(int)h:null)]);
        var parent=workspace.CreateMapPropertiesWindow()!;ShowManagerCallerWindow(parent);parent.WorkingDocument.SetMapVariables([new(0x18,500)]);var child=parent.ChildWorkspace;Select(child);
        Control<TabControl>(child,"WorkspaceTabs").SelectedIndex=7;
        child.PlayerGoldPresenter=window=>
        {
            prompts++;Expect(window.Notice.Plan.Capacity.Capacity==200,"Hosted notice read changed private variables instead of source live map");
            Expect(!parent.ApplyProperties(),"Parent applied while an equal child Save had an information window open");
            ((CheckBox)window.FindName("PlayerGoldNoticeRemember")).IsChecked=true;window.Acknowledge();
        };
        Submit(child);Expect(prompts==3&&Gold(document)==500&&document.MapVariableCells().Single(cell=>cell.Definition.ID==0x18).Bits==100,"Private child notice modified the live map");parent.Close();
        Draft(workspace,6000,"Shared child");Submit(workspace);Expect(prompts==3&&Gold(document)==6000,"Private child remember did not survive outer Cancel");
        Reset();Draft(workspace,7000);workspace.PlayerGoldPresenter=window=>
        {
            prompts++;var gold=Control<TextBox>(workspace,"PlayerGold");var old=gold.Text;gold.Text="7001";gold.Text=old;window.Acknowledge();
        };
        Submit(workspace);Expect(Gold(document)==6000&&Control<TextBlock>(workspace,"PlayerStatus").Text==LocalizationService.Get("PlayerGold.Changed"),"Restored inputs revived an old information/save plan");
        Draft(workspace,8000);workspace.PlayerGoldPresenter=_=>throw new IOException("information presenter unavailable");Submit(workspace);
        Expect(Gold(document)==8000&&Control<TextBlock>(workspace,"PlayerStatus").Text==LocalizationService.Get("PlayerGold.CommittedFailure"),"Information delivery error vetoed a valid Save");
        var other=New();var untouched=other.Files.ToDictionary(file=>file,file=>file.Data.ToArray());Draft(workspace,9000);
        workspace.PlayerGoldPresenter=window=>{workspace.Document=other;window.Acknowledge();};Submit(workspace);
        Expect(Gold(document)==8000&&untouched.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Changed document saved into an old/new target");
        workspace.Document=null;LocalizationService.SetLanguage(language);host.Close();
        Console.WriteLine($"PASS WPF keeper-gold OK/close/remember/hosted-parent/lifetime {native} {language}; physical focus/audio/DPI remains manual");
    }
}
