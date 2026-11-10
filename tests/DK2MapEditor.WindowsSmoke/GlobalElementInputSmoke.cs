using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    // Prepared only; CLI build does not execute any window, renderer or input.
    private static void CheckGlobalElementInputs(NativeMapDocument document,string language)
    {
        document.SetGlobalDefinitionValues([new("Terrains",2,"TerrainLight","1"),new("Terrains",2,"AmbientLight","1")]);
        var initial=document.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var count=document.EditorChangeCount;var history=document.GetHistory().Undo.Count;
        var current=true;var indices=new Dictionary<string,int>{{"Terrains",2},{"Rooms",document.GlobalDefinitionRows().First(r=>r.Family=="Rooms"&&GlobalRoomControls.Enabled(r.Index,"Cost")).Index},
            {"Doors",0},{"Traps",document.GlobalDefinitionRows().First(r=>r.Family=="Traps"&&GlobalElementInputs.Fields("Traps").All(f=>GlobalTrapControls.Enabled(r.Index,f))).Index},{"Spells",0}};
        void Exact()=>Expect(initial.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&document.EditorChangeCount==count&&document.GetHistory().Undo.Count==history,"Private global numeric UI changed live state.");
        GlobalDefinitionRow Row(string family,int? index=null)=>document.GlobalDefinitionRows().Single(r=>r.Family==family&&r.Index==(index??indices[family]));
        Window Open(string family,int? index=null)=>family switch
        {"Terrains"=>new GlobalTerrainWindow(document,Row(family,index),()=>current,()=>true),"Rooms"=>new GlobalRoomWindow(document,Row(family,index),()=>current,()=>true),
         "Doors"=>new GlobalDoorWindow(document,Row(family,index),()=>current,()=>true),"Traps"=>new GlobalTrapWindow(document,Row(family,index),()=>current,()=>true),_=>new GlobalSpellWindow(document,Row(family,index),()=>current,()=>true)};
        string Prefix(string family)=>family switch{"Terrains"=>"TerrainProperty","Rooms"=>"RoomProperty","Doors"=>"DoorProperty","Traps"=>"TrapProperty",_=>"SpellProperty"};
        TextBox Input(Window w,string family,string field)=>(TextBox)w.FindName(Prefix(family)+field);
        bool Step(Window w,string field)=>w switch{GlobalTerrainWindow t=>t.StepNumber(field,1),GlobalRoomWindow r=>r.StepNumber(field,1),GlobalDoorWindow d=>d.StepNumber(field,1),GlobalTrapWindow t=>t.StepNumber(field,1),GlobalSpellWindow s=>s.StepNumber(field,1),_=>false};
        void Press(RepeatButton button)=>button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        foreach(var family in GlobalVariablesSession.Families)
        {
            var w=Open(family);ShowManagerCallerWindow(w);
            try
            {
                foreach(var field in GlobalElementInputs.Fields(family))
                {
                    var input=Input(w,family,field);var spin=(RepeatButton)w.FindName(Prefix(family)+field+"Increase");
                    if(family=="Doors"&&field=="ManaCost")
                    {input.Text="65536";Press(spin);Expect(input.Text=="65536"&&!spin.IsEnabled&&!Step(w,field),"Disabled door buddy acquired numeric authority.");continue;}
                    input.Text=field=="Damage"?"-32769":"4294967295";
                    Expect(input.Text==(field=="Damage"?"-32768":field=="TerrainLightH"?"0.0":"0"),"Global page bypasses immediate source clamp.");
                    input.Text="";Press(spin);Expect(input.Text=="1"&&ReferenceEquals(input,Input(w,family,field))&&ReferenceEquals(spin,w.FindName(spin.Name)),"Global spin failed from empty or rebuilt controls.");
                }
                var first=GlobalElementInputs.Fields(family).First();Input(w,family,first).Text="1";
                LocalizationService.SetLanguage(language=="en"?"ru":"en");Expect(Input(w,family,first).Text=="1","Global translation rewrote numeric draft.");LocalizationService.SetLanguage(language);Exact();
            }
            finally{w.Close();}Exact();
        }
        var parent=new GlobalVariablesWindow(document,()=>current,()=>true);ShowManagerCallerWindow(parent);
        var edits=new[]{("Terrains","GoldValue","65536","65535","65535"),("Rooms","TorchLightH","9.5","8","32768"),
            ("Doors","Health","65536","65535","65535"),("Traps","ChargeTime","16001.5","16000","65536000"),("Spells","RechargeTime","32768.5","32767","134213632")};
        try
        {
            foreach(var (family,field,text,clamped,_) in edits)
            {
                Expect(parent.Select(family,indices[family])&&parent.ActiveRowEditor is Window,"Shared numeric page route missing.");
                var page=parent.ActiveRowEditor!;Input(page,family,field).Text=text;Expect(Input(page,family,field).Text==clamped,"Shared window bypasses numeric callback.");Exact();
            }
            Expect(parent.ApplyVariables(),"Five-page common numeric Apply failed.");
            foreach(var (family,field,_,_,raw) in edits)Expect(document.GlobalDefinitionValues(Row(family)).Single(v=>v.Name==field).Value==raw,"Common numeric Apply lost source bytes.");
            Expect(document.EditorChangeCount==count+1&&document.GetHistory().Undo.Count==history+1,"Five pages split common Undo.");document.Undo();Exact();
        }
        finally{parent.Close();}
        foreach(var (family,field,text,_,_) in edits)
        {
            var w=Open(family);ShowManagerCallerWindow(w);
            try{current=false;Input(w,family,field).Text=text;Expect(Input(w,family,field).Text==text&&!Step(w,field),"Stale global owner retained numeric authority.");Exact();}
            finally{w.Close();current=true;}
        }
        var inactive=document.GlobalDefinitionRows().FirstOrDefault(r=>r.Family=="Traps"&&!GlobalTrapControls.Enabled(r.Index,"GoldCost"));
        if(inactive is not null)
        {
            var w=Open("Traps",inactive.Index);ShowManagerCallerWindow(w);
            try{Input(w,"Traps","GoldCost").Text="65536";Expect(Input(w,"Traps","GoldCost").Text=="65536"&&!Step(w,"GoldCost"),"Inactive Magic Door field acquired numeric authority.");Exact();}
            finally{w.Close();}
        }
        document.SetGlobalDefinitionValues([new("Rooms",indices["Rooms"],"TorchLightH","65535")]);
        var imported=document.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var retained=(GlobalRoomWindow)Open("Rooms");ShowManagerCallerWindow(retained);
        try
        {
            Expect(Input(retained,"Rooms","TorchLightH").Text=="15.999755859375","Opening repaired room import.");
            LocalizationService.SetLanguage(language=="en"?"ru":"en");Expect(Input(retained,"Rooms","TorchLightH").Text=="15.999755859375","Translation repaired room import.");LocalizationService.SetLanguage(language);
            Expect(retained.ApplyRoom()&&imported.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"Room no-op normalized imported height.");
        }
        finally{retained.Close();document.Undo();}Exact();
        foreach(var (section,field) in new[]{("Attributes","Health"),("Light","LightX"),("Parameters","Attract2Data")})
        {
            var creature=new CreaturePageWindow(document,document.GlobalDefinitionRows().Single(r=>r.Family=="Creatures"&&r.Index==0),section,()=>current,()=>true);ShowManagerCallerWindow(creature);
            try
            {
                if(section=="Parameters")
                {
                    var combo=(ComboBox)creature.FindName("CreaturePropertyAttract2Type");combo.SelectedItem=combo.Items.Cast<object>().Single(item=>Get<string>(item,"ID")=="2");
                }
                var input=(TextBox)creature.FindName("CreatureProperty"+field);input.Text="";
                var button=(RepeatButton)creature.FindName("CreatureProperty"+field+"Increase");Press(button);
                Expect(input.Text=="1"&&ReferenceEquals(input,creature.FindName("CreatureProperty"+field))&&ReferenceEquals(button,creature.FindName("CreatureProperty"+field+"Increase")),"Creature page step rebuilt its control/repeat button.");Exact();
            }
            finally{creature.Close();}
        }
    }
}
