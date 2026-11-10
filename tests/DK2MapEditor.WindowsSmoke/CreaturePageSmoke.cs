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
    private static void CheckCreaturePages(NativeMapDocument document,string language)
    {
        GlobalDefinitionRow Row(int index=0)=>document.GlobalDefinitionRows().Single(row=>row.Family=="Creatures"&&row.Index==index);
        Dictionary<NativeFile,byte[]> Snapshot()=>document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
        void Exact(Dictionary<NativeFile,byte[]> bytes)=>Expect(document.Files.Count==bytes.Count&&document.Files.All(file=>bytes[file].AsSpan().SequenceEqual(file.Data.Span)),"Creature page UI changed private/no-op/cancel/undo bytes.");
        T Field<T>(CreaturePageWindow window,string field)where T:Control=>(T)window.FindName("CreatureProperty"+field);
        T Named<T>(CreaturePageWindow window,string name)where T:Control=>(T)window.FindName("CreaturePages"+name);
        void Set(CreaturePageWindow window,string field,string value)=>Field<TextBox>(window,field).Text=value;
        void Choose(CreaturePageWindow window,string field,string value)
        {var combo=Field<ComboBox>(window,field);combo.SelectedItem=combo.Items.Cast<object>().Single(item=>Get<string>(item,"ID")==value);}
        void Layout(CreaturePageWindow window,int width=740,int height=660)
        {var content=(FrameworkElement)window.Content;window.Width=width;window.Height=height;content.Measure(new Size(width,height));content.Arrange(new Rect(0,0,width,height));content.UpdateLayout();}
        CreaturePageWindow Open(string section,int index=0)
        {var window=new CreaturePageWindow(document,Row(index),section,()=>true,()=>true);Layout(window);return window;}
        void Render(CreaturePageWindow window,string state)
        {
            if(Environment.GetEnvironmentVariable("DK2_CREATURE_PAGES_UI_ARTIFACTS") is not {Length:>0} folder)return;Directory.CreateDirectory(folder);
            foreach(var width in new[]{540,740})foreach(var bottom in new[]{false,true})
            {
                var height=width==540?420:660;Layout(window,width,height);var scroll=Named<ScrollViewer>(window,"Scroll");if(bottom)scroll.ScrollToEnd();else scroll.ScrollToHome();((FrameworkElement)window.Content).UpdateLayout();
                Expect(scroll.ExtentWidth<=scroll.ViewportWidth+1,"Creature page overflows horizontally.");
                if(bottom)
                {
                    var last=window.Section switch{"Light"=>(Control)Field<Button>(window,"LightColour"),"Flags"=>Field<CheckBox>(window,"ImmuneLightning"),_=>Field<Control>(window,CreaturePageControls.Fields(window.Section).Last())};
                    var bounds=last.TransformToAncestor(scroll).TransformBounds(new Rect(new Point(),last.RenderSize));Expect(bounds.Top>=0&&bounds.Bottom<=scroll.ActualHeight+1,"Last typed creature control is unreachable.");
                }
                var bitmap=new RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32);bitmap.Render((FrameworkElement)window.Content);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var output=File.Create(Path.Combine(folder,$"creature-{window.Section.ToLowerInvariant()}-{state}-{language}-{(document.IsLegacyProject?"project":"native")}-{width}-{bottom}.png"));encoder.Save(output);
            }
        }
        foreach(var section in CreaturePageControls.Sections)
        {
            var initial=Snapshot();var history=document.GetHistory().Undo.Count;
            foreach(var index in Enumerable.Range(0,45))
            {
                var window=Open(section,index);var draft=document.CreateCreaturePageDraft(Row(index),section);
                Expect(window.Title.StartsWith(LocalizationService.Get("Editor.Definitions."+section),StringComparison.Ordinal),"Creature source page title is untranslated.");
                foreach(var field in CreaturePageControls.Fields(section))
                {
                    var control=Field<Control>(window,field);var type=field=="Attract1ID"?draft.Raw("Attract1Type"):field is "Attract2ID" or "Attract2Data"?draft.Raw("Attract2Type"):"0";
                    Expect(control.IsEnabled==CreaturePageControls.Enabled(field,type),"Typed creature control availability differs:"+field);
                    if(control is CheckBox check)Expect(check.IsChecked==(draft.Display(field)=="1"),"Source/inverted creature checkbox differs.");
                    else if(control is ComboBox combo)
                    {
                        var visible=field is "Attract1ID" or "Attract2ID"?CreaturePageControls.AttractionSelection(field,draft.Display(field),type):draft.Display(field);
                        Expect(visible is null?combo.SelectedItem is null&&combo.Items.Count==0:Get<string>(combo.SelectedItem,"ID")==visible,"Typed creature choice/source fallback differs.");
                    }
                    else Expect(((TextBox)control).Text==(field=="Attract2Data"?CreaturePageControls.AttractionAmountText(draft.Display(field),type):draft.Display(field)),"Typed creature numeric display differs.");
                }
                if(section=="Flags")foreach(var field in new[]{"Emotionless","ImmuneLightning"})Expect(!Field<CheckBox>(window,field).IsEnabled&&Field<CheckBox>(window,field).IsChecked is null,"Original unused checkbox became editable.");
                if(index is 0 or 44)Render(window,index==0?"first":"last");
                Expect(window.ApplyPage(),"Default creature page no-op failed.");window.Close();Exact(initial);
            }
            Expect(document.GetHistory().Undo.Count==history,"Typed creature opening/no-op materialized history.");
            var selected=Open(section);var current=document.CreateCreaturePageDraft(Row(),section);
            if(section=="Parameters")
            {
                Choose(selected,"Attract2Type","2");Set(selected,"Attract2Data","123");
                Choose(selected,"Attract2Type","3");
                Expect(Field<TextBox>(selected,"Attract2Data").Text==current.Display("Attract2Data"),"Actual second attraction combo retained unsaved quantity.");
                Expect(Field<ComboBox>(selected,"Attract2ID").Items.Count==0,"Inactive original attraction ID list shows an imported row.");
                Choose(selected,"Attract2Type","0");Expect(Field<TextBox>(selected,"Attract2Data").Text=="","Actual None amount isn't empty.");
                Choose(selected,"Attract2Type","4");Expect(Field<TextBox>(selected,"Attract2Data").Text==current.Display("Attract2Data"),"Actual creature attraction failed to reload private quantity.");
            }
            var expected=new Dictionary<string,string>();
            foreach(var field in CreaturePageControls.Fields(section))
            {
                var value=CreaturePageControls.Boolean(section,field)?current.Display(field)=="1"?"0":"1":section=="Parameters"?field switch{"Attract1Type"=>"1","Attract2Type"=>"4","Attract1ID"=>"2","Attract2ID"=>CreaturePageControls.Choices(field,"4").Last().Value,"Attract2Data"=>"123",_=>CreaturePageControls.Choices(field).Last().Value}:field is "LightR" or "LightG" or "LightB"?field switch{"LightR"=>"17","LightG"=>"34",_=>"51"}:CreaturePageControls.Format(field,!document.IsLegacyProject).Fractional?"1.25":CreaturePageControls.Format(field,!document.IsLegacyProject).SignedAttribute?"-9":"123";
                expected[field]=value;
                if(CreaturePageControls.Boolean(section,field))Field<CheckBox>(selected,field).IsChecked=value=="1";
                else if(CreaturePageControls.Named(field))Choose(selected,field,value);
                else Set(selected,field,value);
            }
            if(section=="Light")
            {
                selected.ColourSelector=value=>{Expect(value==0x332211,"Colour dialog received reversed channels.");return 0x332211;};Expect(selected.ChooseColour(),"Private source colour picker failed.");
                Expect(selected.StepNumber("LightX",1)&&Field<TextBox>(selected,"LightX").Text=="2","Source fixed-point arrow kept a fraction.");expected["LightX"]="2";
            }
            LocalizationService.SetLanguage(language=="en"?"ru":"en");
            foreach(var field in expected.Keys)
            {
                var control=Field<Control>(selected,field);var actual=control switch{CheckBox check=>check.IsChecked==true?"1":"0",ComboBox combo=>Get<string>(combo.SelectedItem,"ID"),_=>((TextBox)control).Text};
                Expect(actual==expected[field],"Live translation lost typed creature draft:"+field);
            }
            LocalizationService.SetLanguage(language);Render(selected,"selected");Exact(initial);
            Expect(selected.ApplyPage()&&document.GetHistory().Undo.Count==history+1,"Complete typed creature Apply split/lost history.");
            var applied=Snapshot();Expect(selected.ApplyPage(),"Typed creature no-op failed.");Exact(applied);
            document.Undo();Exact(initial);Expect(!selected.ApplyPage(),"Stale typed creature page accepted Apply.");document.Redo();Exact(applied);document.Undo();Exact(initial);selected.Close();
            var cancelled=Open(section);
            if(section=="Flags")Field<CheckBox>(cancelled,"DoNotTrain").IsChecked=!Field<CheckBox>(cancelled,"DoNotTrain").IsChecked;
            else if(section=="Parameters")Choose(cancelled,"FightStyle",current.Raw("FightStyle")=="4"?"0":"4");
            else Set(cancelled,section=="Attributes"?"Health":"LightX","2");
            Named<Button>(cancelled,"Cancel").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Exact(initial);Expect(!cancelled.ApplyPage(),"Closed typed creature page accepted Apply.");
            var invalid=Open(section);
            if(section=="Flags")invalid.Close();
            else
            {
                if(section=="Parameters"){Choose(invalid,"Attract2Type","4");Choose(invalid,"Attract2ID",CreaturePageControls.Choices("Attract2ID","4").First().Value);Set(invalid,"Attract2Data","invalid");}
                else Set(invalid,section=="Attributes"?"Health":"LightRadius","invalid");
                Expect(!invalid.ApplyPage(),"Invalid typed page partly applied other fields.");Exact(initial);invalid.Close();
            }
            if(section=="Light")
            {
                var chooser=Open(section);chooser.ColourSelector=_=>null;Expect(!chooser.ChooseColour()&&!chooser.HasPendingInputs,"Cancelled native colour draft changed values.");
                chooser.ColourSelector=_=>{Expect(!chooser.ApplyPage()&&!chooser.ChooseColour()&&!chooser.StepNumber("LightX",1),"Colour choice allowed reentrant editing.");Set(chooser,"LightX","2");return 0x112233;};
                Expect(!chooser.ChooseColour()&&Field<TextBox>(chooser,"LightX").Text=="2","Stale chooser overwrote later input.");Exact(initial);chooser.Close();
            }
            // A workspace can commit a pending grid edit while this page applies.
            // Refuse stale submissions without discarding the newer input/document edit.
            foreach(var guard in new[]{"refused","owner","input","source","closed"})
            {
                var active=true;CreaturePageWindow guarded=null!;var called=0;
                var field=section switch{"Attributes"=>"Health","Parameters"=>"FightStyle","Flags"=>"DoNotTrain",_=>"LightR"};
                var oldValue=current.Raw(field);var replacement=oldValue=="1"?"0":"1";
                void Pending(string value)
                {
                    if(section=="Flags")Field<CheckBox>(guarded,field).IsChecked=value=="1";
                    else if(section=="Parameters")Choose(guarded,field,value);
                    else Set(guarded,field,value);
                }
                guarded=new CreaturePageWindow(document,Row(),section,()=>active,()=>
                {
                    called++;Expect(!guarded.ApplyPage(),"Pending commit allowed recursive Apply.");
                    if(guard=="owner")active=false;
                    else if(guard=="input")Pending("0");
                    else if(guard=="source")document.SetGlobalDefinitionValues([new("Creatures",0,field,replacement)]);
                    else if(guard=="closed")guarded.Close();
                    return guard!="refused";
                });
                Pending("1");
                Expect(!guarded.ApplyPage()&&called==1,"Pending commit accepted a refused/stale/closed creature submission:"+guard);
                if(guard=="source")
                {
                    Expect(document.CreateCreaturePageDraft(Row(),section).Raw(field)==replacement&&document.GetHistory().Undo.Count==history+1,"Stale Apply lost/split the pending workspace edit.");
                    document.Undo();
                }
                else if(guard=="input")
                {
                    var control=Field<Control>(guarded,field);var actual=control switch{CheckBox check=>check.IsChecked==true?"1":"0",ComboBox combo=>Get<string>(combo.SelectedItem,"ID"),_=>((TextBox)control).Text};
                    Expect(actual=="0","Stale Apply overwrote pending control input.");
                }
                guarded.Close();Exact(initial);Expect(document.GetHistory().Undo.Count==history,"Rejected pending commit changed history.");
            }
            document.Edit("Imported creature UI fixture",()=>
            {
                if(section=="Attributes")document.SetGlobalDefinitionValues([new("Creatures",0,"Health",document.IsLegacyProject?"100000":"65535")]);
                else if(section=="Parameters")document.SetGlobalDefinitionValues([new("Creatures",0,"FightStyle","253"),new("Creatures",0,"Attract1Type","254"),new("Creatures",0,"Attract2Type","254"),new("Creatures",0,"Attract2Data",document.IsLegacyProject?"100000":"65535")]);
                else if(section=="Light")document.SetGlobalDefinitionValues([new("Creatures",0,"LightX","-8192")]);
                else document.SetGlobalDefinitionValues([new("Creatures",0,"DoNotTrain",current.Raw("DoNotTrain")=="0"?"1":"0")]);
                if(document.IsLegacyProject&&section is "Flags" or "Light")
                {var record=Row().Record!;var field=section=="Flags"?"DoNotTrain":"LightPulse";document.SetField(record,record.Fields.Single(item=>item.Name==field),"2");}
            });
            var imported=Snapshot();var retained=Open(section);Render(retained,"imported");Expect(retained.ApplyPage(),"Unknown/wide/BOOL typed page no-op failed.");Exact(imported);retained.Close();document.Undo();Exact(initial);
            var owner=true;var late=new CreaturePageWindow(document,Row(),section,()=>owner,()=>true);owner=false;Expect(!late.ApplyPage(),"Foreign owner accepted typed creature Apply.");late.Close();Exact(initial);
            Console.WriteLine($"PASS {language} {(document.IsLegacyProject?"project":"native")} complete creature {section} page:{CreaturePageControls.Fields(section).Count} fields/45 rows, typed choices/defaults/ranges, source checkbox/attraction/colour rules, private draft Cancel, atomic Apply/Undo, imports/translation and lifetime guards");
        }
    }
}
