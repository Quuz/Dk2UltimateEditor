using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    // Prepared only; actual execution opens WPF and requires manual acceptance.
    private static void CheckCreatureLightInputs(NativeMapDocument document,string language)
    {
        var before=document.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var count=document.EditorChangeCount;var history=document.GetHistory().Undo.Count;
        var row=document.GlobalDefinitionRows().Single(r=>r.Family=="Creatures"&&r.Index==0);var current=true;
        CreaturePageWindow Open()=>new(document,row,"Light",()=>current,()=>true);
        TextBox Input(CreaturePageWindow w,string name)=>(TextBox)w.FindName("CreatureProperty"+name);
        void Exact()=>Expect(before.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&document.EditorChangeCount==count&&document.GetHistory().Undo.Count==history,"A private light input changed live state.");
        var window=Open();ShowManagerCallerWindow(window);
        try
        {
            foreach(var field in new[]{"LightX","LightY","LightZ"})
            {
                var input=Input(window,field);input.Text="32768.75";Expect(input.Text=="32767.0","Signed light EN_CHANGE did not clamp immediately.");
                input.Text="-32768.75";Expect(input.Text=="-32767.0","Signed light lower clamp differs.");
                input.Text="32767.75";Expect(input.Text=="32767.75","In-range whole part lost its fraction.");
                Expect(window.StepNumber(field,1)&&Input(window,field).Text=="32767.0","Spin did not route through whole-part EN_CHANGE.");
                Input(window,field).Text="";Expect(Input(window,field).Text=="","Empty intermediate input was replaced.");
                Expect(window.StepNumber(field,-1)&&Input(window,field).Text=="-1","Spin from empty did not use source zero.");
            }
            Input(window,"LightRadius").Text="4.25";Expect(Input(window,"LightRadius").Text=="3.0","Radius upper input was not clamped.");
            Input(window,"LightRadius").Text="3.75";Expect(Input(window,"LightRadius").Text=="3.75","Allowed radius fraction was discarded.");
            Input(window,"LightX").Text="-2.75";LocalizationService.SetLanguage(language=="en"?"ru":"en");
            Expect(Input(window,"LightX").Text=="-2.75"&&Input(window,"LightRadius").Text=="3.75","Translation rewrote light draft strings.");
            LocalizationService.SetLanguage(language);Exact();window.Close();Exact();
        }
        finally{window.Close();}
        var parent=new CreatureGlobalsWindow(document,()=>current,()=>true);ShowManagerCallerWindow(parent);
        try
        {
            Expect(parent.Select("Light",0)&&parent.ActiveRowEditor is CreaturePageWindow,"Shared creature parent did not open Light.");
            var page=(CreaturePageWindow)parent.ActiveRowEditor!;Input(page,"LightRadius").Text="4.5";Input(page,"LightX").Text="40000.5";
            Expect(Input(page,"LightRadius").Text=="3.0"&&Input(page,"LightX").Text=="32767.0","Hosted page bypasses normalization.");Exact();
            Expect(parent.ApplyCreatures(),"Common creature Apply rejected clamped private values.");
            var state=document.GlobalDefinitionValues(document.GlobalDefinitionRows().Single(r=>r.Family=="Creatures"&&r.Index==0));
            Expect(state.Single(v=>v.Name=="LightRadius").Value=="12288"&&state.Single(v=>v.Name=="LightX").Value=="134213632"
                &&document.EditorChangeCount==count+1&&document.GetHistory().Undo.Count==history+1,"Hosted clamping did not publish one exact fixed-point Undo.");
            document.Undo();Exact();
        }
        finally{parent.Close();}
        window=Open();ShowManagerCallerWindow(window);
        try
        {current=false;Input(window,"LightX").Text="40000.5";Expect(Input(window,"LightX").Text=="40000.5"&&!window.StepNumber("LightX",1)&&!window.ApplyPage(),"Stale owner retained active clamp/step/Apply authority.");Exact();}
        finally{window.Close();current=true;}
        document.SetGlobalDefinitionValues([new("Creatures",0,"LightX","134217728")]);var imported=document.Files.ToDictionary(f=>f,f=>f.Data.ToArray());
        window=Open();ShowManagerCallerWindow(window);
        try
        {
            Expect(Input(window,"LightX").Text=="32768.0","Opening repaired an imported light value.");
            LocalizationService.SetLanguage(language=="en"?"ru":"en");Expect(Input(window,"LightX").Text=="32768.0","Translation repaired an import.");LocalizationService.SetLanguage(language);
            Expect(window.ApplyPage()&&imported.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"Unchanged imported light input normalized on Apply.");
        }
        finally{window.Close();document.Undo();}Exact();
    }
}
