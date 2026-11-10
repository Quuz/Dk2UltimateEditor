using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    // Prepared only. Building this suite does not execute WPF or input events.
    private static void CheckCreatureResistanceSliders(NativeMapDocument document,string language)
    {
        document.SetGlobalDefinitionValues(CreatureResistanceInputs.Fields.SelectMany(field=>new[]{new GlobalDefinitionChange("Creatures",0,CreatureResistanceInputs.TypeField(field),"1"),new GlobalDefinitionChange("Creatures",0,field,"255")}));
        var original=document.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var count=document.EditorChangeCount;var history=document.GetHistory().Undo.Count;
        void Exact()=>Expect(original.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&document.EditorChangeCount==count&&document.GetHistory().Undo.Count==history,"Resistance UI wrote live state before common Apply or failed Undo.");
        void Choose(ComboBox combo,string id)=>combo.SelectedItem=combo.Items.Cast<object>().Single(item=>Get<string>(item,"ID")==id);
        void PressKey(Slider slider,Key key)
        {
            var source=PresentationSource.FromVisual(slider)!;
            slider.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,source,0,key){RoutedEvent=Keyboard.PreviewKeyDownEvent});
        }
        var workspace=new EditorWorkspace{Document=document};Control<TabControl>(workspace,"WorkspaceTabs").SelectedIndex=9;
        Choose(Control<ComboBox>(workspace,"DefinitionFamily"),"Creatures");Control<ComboBox>(workspace,"DefinitionPicker").SelectedIndex=0;
        Choose(Control<ComboBox>(workspace,"DefinitionSection"),"Resistances");
        Slider Rate(string field)=>Control<Slider>(workspace,"DefinitionOption"+field);
        string Label(string field)=>Control<TextBlock>(workspace,"DefinitionOption"+field+"Value").Text;
        try
        {
            foreach(var field in CreatureResistanceInputs.Fields)
            {
                var slider=Rate(field);Expect(slider.Minimum==0&&slider.Maximum==100&&slider.SmallChange==1&&slider.LargeChange==20&&slider.TickFrequency==5,"Source resistance slider bounds/ticks absent.");
                Expect(slider.Value==100&&Label(field)=="255","Opening normalized imported rate to thumb position.");
            }
            Control<Button>(workspace,"DefinitionOptionApply").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Exact();
            var old=Rate("Res1Rate");old.Value=75.4;Expect(Rate("Res1Rate").Value==75&&Label("Res1Rate")=="75"&&ReferenceEquals(old,Rate("Res1Rate")),"Slider not integer or rebuilt its control.");
            Choose(Control<ComboBox>(workspace,"DefinitionOptionRes1"),"0");Expect(!Rate("Res1Rate").IsEnabled&&!Control<TextBlock>(workspace,"DefinitionOptionRes1RateValue").IsEnabled&&Label("Res1Rate")=="75"&&!workspace.ScrollCreatureResistance("Res1Rate",1),"None erased rate or left slider/readout active.");
            Choose(Control<ComboBox>(workspace,"DefinitionOptionRes1"),"1");Expect(Rate("Res1Rate").Value==75&&Label("Res1Rate")=="75","Re-enabling None lost rate.");
            LocalizationService.SetLanguage(language=="en"?"ru":"en");workspace.RefreshLanguage();
            Expect(Label("Res1Rate")=="75"&&Label("Res2Rate")=="255","Translation normalized imported/pending rate.");
            old.Value=42;Expect(Label("Res1Rate")=="75","Replaced slider callback acquired current field authority.");
            LocalizationService.SetLanguage(language);workspace.RefreshLanguage();Exact();
            var imported=Rate("Res2Rate");Expect(workspace.ScrollCreatureResistance("Res2Rate",-1)&&imported.Value==99&&Label("Res2Rate")=="99","Imported rate did not scroll from thumb100.");
            Expect(!workspace.SetCreatureResistanceRate("Res5Rate",50)&&!workspace.SetCreatureResistanceRate("Res1Rate",double.NaN),"Invalid rate API accepted.");
        }
        finally{workspace.Document=null;}Exact();
        var current=true;var standalone=new CreatureWorkspacePageWindow(document,"Resistances",0,()=>current);ShowManagerCallerWindow(standalone);
        Slider Hosted(string field)=>(Slider)standalone.FindControl("DefinitionOption"+field)!;
        string HostedLabel(string field)=>((TextBlock)standalone.FindControl("DefinitionOption"+field+"Value")!).Text;
        try
        {
            var slider=Hosted("Res1Rate");PressKey(slider,Key.End);Expect(slider.Value==100&&HostedLabel("Res1Rate")=="100","Explicit End at imported thumb endpoint did not submit100.");
            PressKey(slider,Key.PageUp);Expect(slider.Value==80&&HostedLabel("Res1Rate")=="80","Source PageUp direction differs.");
            PressKey(slider,Key.Up);Expect(slider.Value==79,"Source Up arrow must decrement.");PressKey(slider,Key.Down);Expect(slider.Value==80,"Source Down arrow must increment.");
            PressKey(slider,Key.Home);Expect(slider.Value==0&&HostedLabel("Res1Rate")=="0","Source Home endpoint differs.");
            current=false;slider.Value=23;Expect(HostedLabel("Res1Rate")=="0"&&!standalone.ApplyPage(),"Stale hosted owner accepted rate.");Exact();
        }
        finally{standalone.Close();current=true;}Exact();
        var parent=new CreatureGlobalsWindow(document,()=>current,()=>true);ShowManagerCallerWindow(parent);
        try
        {
            Expect(parent.Select("Resistances",0)&&parent.ActiveRowEditor is CreatureWorkspacePageWindow,"Shared resistance route missing.");
            var hosted=(CreatureWorkspacePageWindow)parent.ActiveRowEditor!;
            var sliders=CreatureResistanceInputs.Fields.Select(field=>(Slider)hosted.FindControl("DefinitionOption"+field)!).ToArray();
            sliders[0].Value=23;sliders[1].Value=77;sliders[2].Value=100;sliders[3].Value=0;
            // The third slider starts at100 for raw255: explicit End commits
            //100 even if WPF ValueChanged would otherwise have no event.
            PressKey(sliders[2],Key.End);
            Choose((ComboBox)hosted.FindControl("DefinitionOptionRes2")!,"0");
            LocalizationService.SetLanguage(language=="en"?"ru":"en");LocalizationService.SetLanguage(language);Exact();
            Expect(parent.ApplyCreatures(),"Shared resistance Apply failed.");
            var row=document.GlobalDefinitionRows().Single(r=>r.Family=="Creatures"&&r.Index==0);var actual=document.CreateCreatureOptionsDraft(row,"Resistances");
            Expect(CreatureResistanceInputs.Fields.Select(actual.Raw).SequenceEqual(new[]{"23","77","100","0"})&&actual.Raw("Res2")=="0","Shared sliders/None published wrong values.");
            Expect(document.EditorChangeCount==count+1&&document.GetHistory().Undo.Count==history+1,"Shared resistance split common Undo.");document.Undo();Exact();
        }
        finally{parent.Close();}
    }
}
