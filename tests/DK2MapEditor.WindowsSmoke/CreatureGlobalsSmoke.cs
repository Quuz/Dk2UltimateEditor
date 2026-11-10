using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void CheckCreatureGlobals(NativeMapDocument document,string language)
    {
        var workspace=new EditorWorkspace{Document=document};Control<TabControl>(workspace,"WorkspaceTabs").SelectedIndex=9;
        CreatureGlobalsWindow? active=null;workspace.CreatureGlobalsPresenter=window=>active=window;
        void Layout(Window window,int width=800,int height=800)
        {var content=(FrameworkElement)window.Content;content.Measure(new Size(width,height));content.Arrange(new Rect(0,0,width,height));content.UpdateLayout();}
        CreatureGlobalsWindow Open()
        {
            active?.Close();active=null;workspace.Measure(new Size(1200,850));workspace.Arrange(new Rect(0,0,1200,850));workspace.UpdateLayout();
            var button=Control<Button>(workspace,"DefinitionCreatureGlobals");Expect(button.IsEnabled&&button.Visibility==Visibility.Visible,"Creature parent command unavailable.");
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Expect(active is not null,"Actual workspace creature command did not open.");
            active!.JobWarningsConfirm=_=>true;Layout(active);return active;
        }
        Dictionary<NativeFile,byte[]> Snapshot()=>document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
        void Exact(Dictionary<NativeFile,byte[]> bytes)=>Expect(document.Files.Count==bytes.Count&&document.Files.All(file=>bytes[file].AsSpan().SequenceEqual(file.Data.Span)),"Shared creature UI changed private/cancel/no-op/Undo bytes.");
        string Value(string family,int index,string field)=>document.GlobalDefinitionValues(document.GlobalDefinitionRows().Single(row=>row.Family==family&&row.Index==index)).Single(value=>value.Name==field).Value;
        T Field<T>(CreatureGlobalsWindow window,string field)where T:Control=>window.ActiveRowEditor is CreatureWorkspacePageWindow hosted?(T)hosted.FindControl("Definition"+field)!:(T)window.ActiveRowEditor!.FindName((window.SelectedSection=="CrSpells"?"CreatureSpellProperty":"CreatureProperty")+field);
        void Set(CreatureGlobalsWindow window,string field,string value)
        {
            var control=Field<Control>(window,field);
            if(control is Slider slider)slider.Value=double.Parse(value,CultureInfo.InvariantCulture);else ((TextBox)control).Text=value;
        }
        string InputText(CreatureGlobalsWindow window,string field)=>Field<Control>(window,field) is Slider slider?slider.Value.ToString(CultureInfo.InvariantCulture):Field<TextBox>(window,field).Text;
        void Choose(CreatureGlobalsWindow window,string field,string value)
        {var combo=Field<ComboBox>(window,field);combo.SelectedItem=combo.Items.Cast<object>().Single(item=>Get<string>(item,"ID")==value);}
        ScrollViewer Scroll(CreatureGlobalsWindow window)=>window.ActiveRowEditor is CreatureWorkspacePageWindow?
            (ScrollViewer)((ContentControl)window.FindName("CreatureGlobalsEditor")).Content:
            (ScrollViewer)window.ActiveRowEditor!.FindName(window.SelectedSection=="CrSpells"?"CreatureSpellPropertiesScroll":"CreaturePagesScroll");
        void Render(CreatureGlobalsWindow window,string state)
        {
            if(Environment.GetEnvironmentVariable("DK2_CREATURE_PARENT_UI_ARTIFACTS") is not {Length:>0} folder)return;Directory.CreateDirectory(folder);
            foreach(var width in new[]{580,800})foreach(var bottom in new[]{false,true})
            {
                var height=width==580?540:800;Layout(window,width,height);var scroll=Scroll(window);if(bottom)scroll.ScrollToEnd();else scroll.ScrollToHome();((FrameworkElement)window.Content).UpdateLayout();
                Expect(scroll.ViewportHeight>40&&scroll.ExtentWidth<=scroll.ViewportWidth+1,"Creature parent body unreachable/horizontal overflow:"+window.SelectedSection);
                if(bottom)
                {
                    FrameworkElement last=window.ActiveRowEditor is CreatureWorkspacePageWindow hosted?(FrameworkElement)hosted.FindControl(window.SelectedSection switch{"Attacks"=>"DefinitionAttackHint","Jobs"=>"DefinitionJobsHint",_=>"DefinitionOptionHint"})!:
                        window.SelectedSection=="CrSpells"?Field<Control>(window,CreatureSpellControls.MainFields.Last()):Field<Control>(window,window.SelectedSection switch{"Flags"=>"ImmuneLightning","Light"=>"LightColour",_=>CreaturePageControls.Fields(window.SelectedSection).Last()});
                    var lastBounds=last.TransformToAncestor(scroll).TransformBounds(new Rect(new Point(),last.RenderSize));
                    Expect(lastBounds.Top>=-1&&lastBounds.Bottom<=scroll.ActualHeight+1,"Last source control/hint unreachable:"+window.SelectedSection);
                }
                var apply=(Button)window.FindName("CreatureGlobalsApply");var bounds=apply.TransformToAncestor((FrameworkElement)window.Content).TransformBounds(new Rect(new Point(),apply.RenderSize));
                Expect(apply.Visibility==Visibility.Visible&&apply.RenderSize.Height>0&&bounds.Top>=0&&bounds.Bottom<=height+1,"Common creature footer unreachable:"+window.SelectedSection+"/"+width+"/"+bounds+"/"+apply.Visibility+"/"+apply.RenderSize);
                var bitmap=new RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32);bitmap.Render((FrameworkElement)window.Content);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var output=File.Create(Path.Combine(folder,$"creatures-{state}-{window.SelectedSection.ToLowerInvariant()}-{language}-{(document.IsLegacyProject?"project":"native")}-{width}-{bottom}.png"));encoder.Save(output);
            }
        }
        var initial=Snapshot();var history=document.GetHistory().Undo.Count;var window=Open();
        Expect(((TabControl)window.FindName("CreatureGlobalsPages")).Items.Count==9,"Original creature pages incomplete.");
        for(var index=0;index<45;index++)Expect(window.Select("Attributes",index)&&((ComboBox)window.FindName("CreatureGlobalsRows")).Items.Count==45,"Shared creature row list incomplete.");
        for(var index=0;index<41;index++)Expect(window.Select("CrSpells",index)&&((ComboBox)window.FindName("CreatureGlobalsRows")).Items.Count==41,"Independent creature-spell list incomplete.");
        foreach(var page in CreatureGlobalsSession.Sections)
        {
            var last=page.Family=="CrSpells"?40:44;Expect(window.Select(page.Name,last),"Last source row/page failed:"+page.Name);Render(window,"last");
            Expect(window.Select(page.Name,0),"First source row/page failed:"+page.Name);Render(window,"first");
            if(window.ActiveRowEditor is not CreatureWorkspacePageWindow)
            {
                var prefix=page.Name=="CrSpells"?"CreatureSpellProperties":"CreaturePages";
                Expect(((Button)window.ActiveRowEditor!.FindName(prefix+"Apply")).Visibility==Visibility.Collapsed&&((Button)window.ActiveRowEditor.FindName(prefix+"Cancel")).Visibility==Visibility.Collapsed,"Row page exposed separate commit/cancel.");
            }
        }
        Expect(window.ApplyCreatures(),"Complete default navigation/no-op failed.");Exact(initial);Expect(document.GetHistory().Undo.Count==history,"Default nine-page navigation materialized live history.");
        Expect(window.Select("Attributes",2),"Creature row2 unavailable.");Set(window,"Health","123");
        Expect(window.Select("Parameters",2),"Parameters page unavailable.");Choose(window,"FightStyle","4");
        Expect(window.Select("Flags",2),"Flags page unavailable.");Field<CheckBox>(window,"DoNotTrain").IsChecked=Value("Creatures",2,"DoNotTrain")=="1";
        Expect(window.Select("Attacks",2),"Attacks page unavailable.");Choose(window,"AttackSlot","Melee");Set(window,"AttackDammage","123");
        Expect(window.Select("Resistances",2),"Resistances page unavailable.");Choose(window,"OptionRes1",CreatureOptions.Choices("Res1").First(choice=>choice.Value!="0").Value);Set(window,"OptionRes1Rate","12");
        Expect(window.Select("Possession",2),"Possession page unavailable.");Set(window,"OptionWaddleScale","0.25");
        Expect(window.Select("Jobs",2),"Jobs page unavailable.");var hosted=(CreatureWorkspacePageWindow)window.ActiveRowEditor!;
        Choose(window,"JobsGroup","Happy");var table=Field<DataGrid>(window,"JobsTable");table.SelectAll();Field<Button>(window,"JobsRemove").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Choose(window,"JobsType",CreatureJobs.Allowed("Happy").First().Name.Value);Set(window,"JobsChance","37");Set(window,"JobsMood","-9");Set(window,"JobsMana","11");Field<Button>(window,"JobsAdd").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        var warnings=0;window.JobWarningsConfirm=items=>{warnings++;Expect(items.Any(item=>item.Index==2&&item.Group=="Happy"&&item.Total==37),"Source navigation warning lacks pending tuple total.");Expect(!window.Select("Light",2)&&!window.Reset(true)&&!window.ApplyCreatures(),"Job warning allowed reentrant navigation/reset/Apply.");return false;};
        Expect(!window.Select("Light",2)&&!window.Select("Jobs",3)&&window.SelectedSection=="Jobs"&&window.SelectedIndex==2&&warnings==2,"Refused source warning changed creature/page selection.");Exact(initial);
        window.JobWarningsConfirm=_=>{Set(window,"JobsChance","38");return true;};
        Expect(!window.Select("Light",2)&&Field<TextBox>(window,"JobsChance").Text=="38","Job warning overwrote newer pending input/accepted stale consent.");Set(window,"JobsChance","37");
        window.JobWarningsConfirm=_=>true;Expect(window.Select("Light",2),"Accepted Jobs warning did not release navigation.");
        Set(window,"LightR","17");Set(window,"LightG","34");Set(window,"LightB","51");
        window.ColourSelector=_=>{Expect(!window.Select("Attributes",0)&&!window.Reset(true)&&!window.ApplyCreatures(),"Parent colour dialog allowed navigation.");return 0x332211;};Expect(((CreaturePageWindow)window.ActiveRowEditor!).ChooseColour(),"Shared light picker failed.");
        Expect(window.Select("CrSpells",3),"Independent spell page unavailable.");Set(window,"Range","1.25");
        var two=GlobalProjectiles.Options.First(option=>option.Form==0);Choose(window,"Shot",two.ID.ToString(CultureInfo.InvariantCulture));Window? nested=null;
        window.OptionsPresenter=child=>nested=child;Expect(((CreatureSpellWindow)window.ActiveRowEditor!).OpenOptions("Shot")&&nested is CreatureSpellOptionsWindow,"Private shared spell child missing.");
        Expect(!window.Select("Attributes",2)&&!window.Reset(true)&&!window.ApplyCreatures(),"Shared nested child allowed navigation/reset/Apply.");
        var options=(CreatureSpellOptionsWindow)nested!;((TextBox)options.FindName("CreatureSpellOptionsData1")).Text=two.Signed1?"-9":"111";((TextBox)options.FindName("CreatureSpellOptionsData2")).Text=two.Signed2?"-7":"222";
        Expect(options.AcceptOptions(),"Private child OK failed.");Exact(initial);
        Expect(window.Select("Attributes",2)&&Field<TextBox>(window,"Health").Text=="123","Shared creature selection lost row draft.");
        Expect(window.Select("Attributes",1),"Second creature row unavailable.");Set(window,"Health","234");Expect(window.Select("CrSpells",3)&&Field<TextBox>(window,"Range").Text=="1.25","Independent spell selection lost row draft.");
        LocalizationService.SetLanguage(language=="en"?"ru":"en");Expect(Field<TextBox>(window,"Range").Text=="1.25","Common language switch lost pending spell value.");LocalizationService.SetLanguage(language);
        foreach(var page in CreatureGlobalsSession.Sections)
        {
            Expect(window.Select(page.Name,page.Family=="CrSpells"?3:2),"Edited page return failed:"+page.Name);
            if(page.Name=="Attacks")Choose(window,"AttackSlot","Melee");
            var input=page.Name switch{"Attributes"=>"Health","Attacks"=>"AttackDammage","Resistances"=>"OptionRes1Rate","Possession"=>"OptionWaddleScale","Jobs"=>"JobsChance","Light"=>"LightR","CrSpells"=>"Range",_=>null};
            var retained=input is null?null:InputText(window,input);
            LocalizationService.SetLanguage(language=="en"?"ru":"en");LocalizationService.SetLanguage(language);
            if(input is not null)Expect(InputText(window,input)==retained,"Hosted page language switch lost pending input:"+page.Name);
            Render(window,"selected");Exact(initial);
        }
        Set(window,"Range","bad");Expect(!window.Select("Attributes",0)&&!window.ApplyCreatures()&&window.SelectedSection=="CrSpells","Invalid pending page partly applied/navigated.");
        Expect(window.Reset(false),"Reset did not discard invalid current page.");Set(window,"Range","1.25");Choose(window,"Shot",two.ID.ToString(CultureInfo.InvariantCulture));nested=null;((CreatureSpellWindow)window.ActiveRowEditor!).OpenOptions("Shot");options=(CreatureSpellOptionsWindow)nested!;
        ((TextBox)options.FindName("CreatureSpellOptionsData1")).Text=two.Signed1?"-9":"111";((TextBox)options.FindName("CreatureSpellOptionsData2")).Text=two.Signed2?"-7":"222";Expect(options.AcceptOptions(),"Reset/reopen private child failed.");
        var notices=0;void Notice(object? sender,EventArgs args)=>notices++;document.Changed+=Notice;
        Expect(window.ApplyCreatures(),"Nine-page/multiple-creature/spell/nested common Apply failed.");document.Changed-=Notice;
        Expect(notices==1&&document.GetHistory().Undo.Count==history+1,"Common creature Apply split notification/history.");
        foreach(var (field,value) in new[]{("Health","123"),("FightStyle","4"),("Res1Rate","12"),("Dammage","123"),("WaddleScale","1024"),("LightR","17"),("LightG","34"),("LightB","51"),("HJobsChance[0]","37")})Expect(Value("Creatures",2,field)==value,"Common Apply lost page field:"+field);
        Expect(Value("Creatures",1,"Health")=="234"&&Value("CrSpells",3,"Range")=="5120"&&Value("CrSpells",3,"ShotData1")== (two.Signed1?"-9":"111"),"Common Apply lost other creature/spell child.");
        var applied=Snapshot();Expect(window.ApplyCreatures(),"Second common no-op failed.");Exact(applied);Expect(document.GetHistory().Undo.Count==history+1,"Second common Apply consumed history.");window.Close();document.Undo();Exact(initial);document.Redo();Exact(applied);document.Undo();Exact(initial);
        var cancel=Open();Set(cancel,"Health","111");Expect(cancel.Select("Possession",0)&&cancel.Reset(true),"Private all-row reset failed.");((Button)cancel.FindName("CreatureGlobalsCancel")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Exact(initial);Expect(!cancel.ApplyCreatures(),"Closed parent accepted Apply.");
        var stale=Open();Set(stale,"Health","111");document.SetGlobalDefinitionValues([new("Creatures",1,"Health","777")]);var changed=Snapshot();Expect(!stale.ApplyCreatures()&&!stale.Reset(true)&&!stale.Select("Light",0),"Stale parent accepted editing.");Exact(changed);stale.Close();document.Undo();Exact(initial);
        foreach(var guard in new[]{"refused","input","owner","source","closed"})
        {
            var owner=true;CreatureGlobalsWindow guarded=null!;
            guarded=new(document,()=>owner,()=>
            {
                Expect(!guarded.ApplyCreatures()&&!guarded.Select("Light",0)&&!guarded.Reset(true),"Common pending commit allowed reentry.");
                if(guard=="input")Set(guarded,"Health","222");else if(guard=="owner")owner=false;else if(guard=="source")document.SetGlobalDefinitionValues([new("Creatures",1,"Health","777")]);else if(guard=="closed")guarded.Close();return guard!="refused";
            });Set(guarded,"Health","111");Expect(!guarded.ApplyCreatures(),"Common Apply accepted invalid pending commit:"+guard);
            if(guard=="source"){Expect(Value("Creatures",1,"Health")=="777","Common Apply overwrote workspace pending edit.");document.Undo();}
            if(guard=="input")Expect(Field<TextBox>(guarded,"Health").Text=="222","Common Apply lost newer input.");guarded.Close();Exact(initial);
        }
        var late=Open();Expect(late.Select("CrSpells",0),"Late nested page unavailable.");Choose(late,"Shot",two.ID.ToString(CultureInfo.InvariantCulture));Window? lateChild=null;late.OptionsPresenter=child=>lateChild=child;((CreatureSpellWindow)late.ActiveRowEditor!).OpenOptions("Shot");workspace.Document=null;
        Expect(lateChild is CreatureSpellOptionsWindow&&!((CreatureSpellOptionsWindow)lateChild).AcceptOptions()&&!late.ApplyCreatures(),"Workspace owner close accepted late parent/child.");Exact(initial);
        Console.WriteLine($"PASS {language} {(document.IsLegacyProject?"project":"native")} complete shared creature parent:9 typed pages/45 creatures/41 spells, shared selection/private navigation, source job warnings, invalid/current/all reset, multiple rows/nested colour/projectile, one Apply/Undo, no-op/Cancel/translation and owner/pending-commit guards");
    }
}
